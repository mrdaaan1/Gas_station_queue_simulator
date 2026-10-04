using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    public enum GameState
    {
        Queueing,    // стоим в очереди, заезжаем, платим, заправляемся
        OutOfFuel,   // шлагбаум опущен, ждём завоз
        DrivingAway, // заправились, уезжаем
        Finished,    // финальный экран
    }

    /// <summary>
    /// Правила игры: часы очереди, «бензин закончился», завоз, выход из машины, касса, заправка, финал, статистика.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameSettings Settings { get; private set; }
        public TrafficManager Traffic { get; private set; }
        public PlayerCar Player { get; private set; }
        public WalkerController Walker { get; private set; }
        public CameraRig CameraRig { get; private set; }
        public Radio Radio { get; private set; }
        public PriceBoard PriceBoard { get; private set; }

        public GameState State { get; private set; } = GameState.Queueing;
        public bool FuelRanOut { get; private set; }
        public bool GaveUp { get; private set; }

        /// <summary>Сколько игрок «стоит в очереди», в игровых секундах.</summary>
        public double QueueSeconds { get; private set; }
        public float DeliveryMinutesLeft => Mathf.Max(0f, 60f * (1f - deliveryTimer / Settings.DeliveryDuration));
        public float ClockRate => State == GameState.OutOfFuel ? 3600f / Settings.DeliveryDuration : Settings.ClockScale;

        // Игрок
        public bool OnFoot { get; private set; }
        public float Money { get; private set; }
        public bool Paid { get; private set; }
        public float PaidLiters { get; private set; }
        public bool NozzleIn { get; private set; }
        public bool PlayerFueled { get; private set; }
        public bool CheatedIn { get; private set; }

        /// <summary>Подсказка действия внизу экрана (или null).</summary>
        public string Prompt { get; private set; }

        // Диалог с кассиром
        public bool DialogOpen { get; private set; }
        public string DialogTitle { get; private set; }
        public readonly List<string> DialogOptions = new List<string>();

        // Статистика для финального экрана
        public int Honks { get; private set; }
        public int HonkedAt { get; private set; }
        public int GiveUpsSeen { get; private set; }
        public int Crashes { get; private set; }
        public int RadioSwitches { get; private set; }
        public int EngineStops { get; private set; }
        public int CutInsSuffered { get; private set; }
        public int CutInsBlocked { get; private set; }
        public int SqueezedIn { get; private set; }
        public int Arguments { get; private set; }
        public int Talks { get; private set; }
        public int FightsWon { get; private set; }
        public int FightsLost { get; private set; }
        public int CarKicks { get; private set; }
        public int CanistersBought { get; private set; }
        public int VipsSeen { get; private set; }
        public int PiesEaten { get; private set; }
        public float LitersFilled { get; private set; }
        public float MoneySpent { get; private set; }

        /// <summary>Уведомление в ленте сбоку экрана.</summary>
        public class FeedItem
        {
            public string text;
            public float shownAt;
            public float duration;
        }

        public readonly List<FeedItem> Feed = new List<FeedItem>();

        static readonly string[] Rumors =
        {
            "Говорят, завоз будет к вечеру. Или завтра.", "На соседней заправке тоже пусто, я звонил.",
            "Я тут с шести утра стою.", "Слышал, по 20 литров в одни руки дают.", "Мне только до дачи доехать...",
            "Если что, у мужика в багажнике канистры по тройной цене.", "Кассирша злая сегодня, не спорь с ней.",
            "А вы за кем занимали?", "Радио говорит, ситуация стабильная. Ага.",
        };
        static readonly string[] PlayerRants =
        {
            "Почему одна касса на всю заправку?!", "Я три часа стоял!", "Дайте жалобную книгу!", "Где бензин, Зин?!",
        };
        static readonly string[] CashierRetorts =
        {
            "Не нравится — езжайте на другую.", "Мужчина, не задерживайте очередь.", "Я тут ни при чём, я на кассе сижу.",
            "Жалобная книга у директора. Директор в отпуске.", "Кричать будете дома.",
        };

        Barrier barrier;
        HumanRig cashier;
        System.Action restart;
        float deliveryTimer;
        float dryTimer = -1f;
        float priceTimer;
        bool priceWarned;
        bool wrongSide;
        bool exitTipShown;
        Pump paidPump;
        Transform hose;

        Transform tanker;
        LanePath tankerPath;
        float tankerS;
        int tankerPhase; // 0 — нет, 1 — едет к заправке, 2 — сливает, 3 — уезжает

        public void Init(GameSettings settings, TrafficManager traffic, PlayerCar player, WalkerController walker,
            CameraRig rig, Radio radio, Barrier barrier, PriceBoard board, HumanRig cashier, System.Action restart)
        {
            Instance = this;
            Settings = settings;
            Traffic = traffic;
            Player = player;
            Walker = walker;
            CameraRig = rig;
            Radio = radio;
            this.barrier = barrier;
            PriceBoard = board;
            this.cashier = cashier;
            this.restart = restart;
            QueueSeconds = settings.startMinutes * 60.0;
            Money = settings.startMoney;

            hose = Shapes.Make(PrimitiveType.Cylinder, transform, Vector3.zero, Vector3.one, Shapes.Hex("#1b1b1b"), name: "Hose").transform;
            hose.gameObject.SetActive(false);

            ShowMessage("Вы в очереди на заправку. Подъезжайте за машиной впереди (W), рулите A/D.", 8f);
            ShowMessage("Не оставляйте дырку впереди — влезут! Esc — пауза, F1 — управление.", 8f);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (State == GameState.Finished)
            {
                if (GameInput.RestartPressed) restart();
                return;
            }
            if (dt <= 0f) return;

            if (State != GameState.DrivingAway) QueueSeconds += dt * ClockRate;
            UpdateDryTank(dt);
            if (State == GameState.OutOfFuel) UpdateDelivery(dt);
            UpdateTanker(dt);
            UpdateFueling(dt);
            UpdateCashier(dt);
            UpdatePlayerFlow();
            UpdateInteractions();
        }

        public void ShowMessage(string text, float seconds = 6f)
        {
            // Одинаковые сообщения подряд не дублируем, а продлеваем
            if (Feed.Count > 0 && Feed[Feed.Count - 1].text == text)
            {
                Feed[Feed.Count - 1].shownAt = Time.time;
                return;
            }
            Feed.Add(new FeedItem { text = text, shownAt = Time.time, duration = seconds });
            if (Feed.Count > 5) Feed.RemoveAt(0);
        }

        // ---------- Где игрок и что делает ----------

        void UpdatePlayerFlow()
        {
            var carPos = Player.Position;
            bool inPumpZone = carPos.x > CityLayout.LotMinX && carPos.x < CityLayout.ShopMinX &&
                              carPos.z > CityLayout.BarrierZ && carPos.z < CityLayout.IslandZ + 8f;

            // Заехал к колонкам, не дождавшись своей очереди
            if (inPumpZone && Traffic.PlayerPump == null && !PlayerFueled && !CheatedIn && State != GameState.DrivingAway)
            {
                CheatedIn = true;
                ShowMessage("Вы заехали к колонкам без очереди! Кассир такого не обслужит.", 8f);
                foreach (var npc in Traffic.Npcs)
                    if (Vector3.Distance(npc.Position, carPos) < 15f && Random.value < 0.5f) npc.Honk();
            }
            if (Traffic.PlayerPump != null) CheatedIn = false;

            // Заправились и отъехали от колонки — едем к выезду
            if (PlayerFueled && State == GameState.Queueing && !OnFoot && paidPump != null &&
                Vector3.Distance(carPos, paidPump.spot) > 8f)
            {
                State = GameState.DrivingAway;
                Traffic.ReleasePlayerPump();
                ShowMessage("Свобода! Выезд — впереди слева, обратно на дорогу.", 6f);
            }

            if (!OnFoot && carPos.z > CityLayout.FinishZ && carPos.x < CityLayout.LotMinX)
            {
                if (PlayerFueled) Finish(false);
                else if (carPos.z > CityLayout.FinishZ + 60f) Finish(true); // уехал, так и не заправившись
            }
        }

        void UpdateInteractions()
        {
            Prompt = null;
            if (DialogOpen)
            {
                int choice = GameInput.DialogChoice;
                if (choice > 0) ChooseDialog(choice);
                else if (GameInput.InteractPressed) CloseDialog();
                else if (Walker.Active && Vector3.Distance(Walker.transform.position, CityLayout.CounterFront) > 2.5f) CloseDialog();
                return;
            }

            // Продавец у окна (в машине) или рядом (пешком)
            var vendor = Traffic.VendorNear(OnFoot ? Walker.transform.position : Player.DriverDoor, OnFoot ? 1.8f : 2.4f);
            if (vendor != null && (OnFoot || vendor.Offering))
            {
                Prompt = vendor.Offer;
                if (GameInput.InteractPressed) Buy(vendor);
                if (!OnFoot)
                {
                    if (GameInput.CarDoorPressed && Mathf.Abs(Player.Speed) < 0.5f) ExitCar();
                    return;
                }
            }

            if (!OnFoot)
            {
                bool slow = Mathf.Abs(Player.Speed) < 0.5f;
                var pump = Traffic.PumpAtPlayerCar();
                if (pump != null && slow && !PlayerFueled)
                    Prompt = Player.Engine != EngineState.Off
                        ? "У колонки: заглушите мотор (I), выйдите (F) и оплатите на кассе"
                        : "F — выйти из машины. Оплата — на кассе в магазине";
                if (GameInput.CarDoorPressed)
                {
                    if (slow) ExitCar();
                    else ShowMessage("На ходу не выпрыгиваем! Сначала остановитесь.");
                }
                return;
            }

            var me = Walker.transform.position;
            bool nearCar = Vector3.Distance(me, Player.DriverDoor) < 1.8f || Player.Box.PushCircle(Walker.Position2, 1.1f, out _);
            bool nearCashier = Vector3.Distance(me, CityLayout.CounterFront) < 1.6f;
            var cap = Player.transform.TransformPoint(Player.visual.fuelCapLocal);
            bool nearCap = Vector3.Distance(new Vector3(me.x, 0, me.z), new Vector3(cap.x, 0, cap.z)) < 1.8f;
            var carPump = Traffic.PumpAtPlayerCar();
            NpcCar nearNpc = null;
            foreach (var npc in Traffic.Npcs)
                if (npc.Speed < 0.2f && npc.visual.driverHead != null && npc.visual.driverHead.gameObject.activeSelf &&
                    Vector3.Distance(me, npc.DriverDoor) < 1.6f)
                {
                    nearNpc = npc;
                    break;
                }

            if (vendor != null)
            {
                // подсказка уже показана выше
            }
            else if (nearCashier)
            {
                Prompt = "E — поговорить с кассиром";
                if (GameInput.InteractPressed) OpenCashierDialog();
            }
            else if (nearCap && carPump != null && Paid && !NozzleIn && !PlayerFueled)
            {
                Prompt = "E — вставить пистолет в бак";
                if (GameInput.InteractPressed) StartFueling(carPump);
            }
            else if (nearCar)
            {
                Prompt = "F — сесть в машину";
            }
            else if (nearNpc != null)
            {
                Prompt = "E — поговорить с водителем";
                if (GameInput.InteractPressed)
                {
                    Talks++;
                    nearNpc.Say(Rumors[Random.Range(0, Rumors.Length)]);
                }
            }

            if (GameInput.CarDoorPressed && nearCar) EnterCar();
        }

        void Buy(Vendor vendor)
        {
            int price = vendor.Price;
            if (Money < price)
            {
                ShowMessage("Денег не хватает.");
                return;
            }
            Money -= price;
            vendor.Sold();
            if (vendor.Kind == VendorKind.Canister)
            {
                CanistersBought++;
                Player.AddFuelLiters(10f);
                ShowMessage($"Мужик перелил вам 10 литров из канистры за {price} руб. Пахнет подозрительно... Но бак не пустой!", 8f);
            }
            else
            {
                PiesEaten++;
                if (Walker.Fighter != null) Walker.Fighter.Heal(60f);
                ShowMessage("Пирожок с капустой и сладкий чай. Силы возвращаются!", 6f);
            }
        }

        void ExitCar()
        {
            OnFoot = true;
            Player.controlsEnabled = false;
            Walker.Appear(Player.DriverDoor, -Player.transform.right);
            Traffic.Walker = Walker;
            CameraRig.SetOnFoot(true);
            if (!exitTipShown)
            {
                exitTipShown = true;
                ShowMessage("Вы вышли из машины. Осторожно: очередь может двинуться без вас! F у двери — сесть обратно.", 8f);
            }
        }

        void EnterCar()
        {
            OnFoot = false;
            CloseDialog();
            Walker.Hide();
            Player.controlsEnabled = true;
            CameraRig.SetOnFoot(false);
        }

        // ---------- Касса ----------

        void OpenCashierDialog()
        {
            DialogOpen = true;
            DialogTitle = "Кассир: «Слушаю вас. Какая колонка?»";
            DialogOptions.Clear();
            DialogOptions.Add(Paid ? "Я уже оплатил(а)..." : $"Оплатить полный бак (АИ-95, {PriceBoard.CurrentPrice:0.00} руб/л)");
            DialogOptions.Add("Поругаться");
            DialogOptions.Add("Уйти");
        }

        void CloseDialog()
        {
            DialogOpen = false;
            DialogOptions.Clear();
        }

        void CashierSays(string text)
        {
            SpeechBubble.Show(cashier.transform, text, 1.5f);
            DialogTitle = $"Кассир: «{text}»";
        }

        void ChooseDialog(int choice)
        {
            switch (choice)
            {
                case 1: TryPay(); break;
                case 2:
                    Arguments++;
                    SpeechBubble.Show(Walker.transform, PlayerRants[Random.Range(0, PlayerRants.Length)], 1.5f);
                    CashierSays(CashierRetorts[Random.Range(0, CashierRetorts.Length)]);
                    cashier.Rage(2f);
                    break;
                default:
                    CloseDialog();
                    break;
            }
        }

        void TryPay()
        {
            if (Paid) { CashierSays("Уже оплачено. Идите вставляйте пистолет."); return; }
            if (State == GameState.OutOfFuel) { CashierSays("Бензина нет! Ждите завоз, как все."); return; }

            var pump = Traffic.PumpAtPlayerCar();
            if (pump == null) { CashierSays("На какую колонку? Сначала подъезжайте к колонке."); return; }
            if (Traffic.PlayerPump == null) { CashierSays("Вы без очереди! Не обслуживаю. Все стоят — и вы стойте."); return; }

            float price = PriceBoard.CurrentPrice;
            float liters = Mathf.Ceil(Settings.tankLiters - Player.FuelLiters);
            if (liters * price > Money) liters = Mathf.Floor(Money / price);
            if (liters < 1f) { CashierSays("Денег не хватает даже на литр. Следующий!"); return; }

            float cost = liters * price;
            Money -= cost;
            MoneySpent += cost;
            Paid = true;
            PaidLiters = liters;
            paidPump = pump;
            bool terminalGlitch = Random.value < 0.3f;
            CashierSays(terminalGlitch
                ? $"Терминал завис... А, прошло. Колонка №{pump.Number}, {liters:0} л. Вставляйте пистолет."
                : $"Колонка №{pump.Number}, {liters:0} литров, {cost:0} руб. Вставляйте пистолет.");
            ShowMessage($"Оплачено: {liters:0} л на колонке №{pump.Number}. Подойдите к лючку бака (справа сзади) и нажмите E.", 8f);
        }

        void UpdateCashier(float dt)
        {
            if (cashier == null) return;
            cashier.Animate(0f, dt);
            if (!OnFoot) return;
            // Кассир провожает игрока взглядом
            var to = Walker.transform.position - cashier.transform.position;
            to.y = 0f;
            if (to.magnitude < 8f && to.sqrMagnitude > 0.01f)
                cashier.transform.rotation = Quaternion.Slerp(cashier.transform.rotation, Quaternion.LookRotation(to.normalized), dt * 3f);
        }

        // ---------- Заправка ----------

        void StartFueling(Pump pump)
        {
            if (Player.Engine != EngineState.Off)
            {
                ShowMessage("Мотор работает! Сядьте (F), заглушите (I) — тогда заправка.");
                return;
            }
            // Лючок бака справа: если колонка слева, шланг еле дотягивается через машину
            var toPump = pump.dispenser - Player.Position;
            wrongSide = Vector3.Dot(toPump, Player.transform.right) < 0f;
            paidPump = pump;
            NozzleIn = true;
            hose.gameObject.SetActive(true);
            ShowMessage(wrongSide
                ? "Бак не с той стороны! Шланг еле дотягивается через крышу — льётся ещё медленнее."
                : "Заправляемся... Счётчик ползёт мучительно медленно.", 7f);
        }

        void UpdateFueling(float dt)
        {
            if (!NozzleIn) return;
            float liters = Settings.LitersPerSecond * dt * (wrongSide ? 0.7f : 1f);
            liters = Mathf.Min(liters, PaidLiters - LitersFilled);
            LitersFilled += liters;
            Player.AddFuelLiters(liters);

            // Шланг от колонки к лючку бака
            var a = paidPump.dispenser + Vector3.up * 1.0f;
            var b = Player.transform.TransformPoint(Player.visual.fuelCapLocal);
            if (wrongSide) b += Vector3.up * 0.9f; // через крышу
            hose.position = (a + b) / 2f;
            hose.up = (b - a).normalized;
            hose.localScale = new Vector3(0.05f, Vector3.Distance(a, b) / 2f, 0.05f);

            // Цена на стеле растёт прямо во время заправки
            priceTimer += dt;
            if (priceTimer > 6f)
            {
                priceTimer = 0f;
                PriceBoard.RaisePrices(Random.Range(0.2f, 0.6f));
                if (!priceWarned)
                {
                    priceWarned = true;
                    ShowMessage("Цена на стеле растёт прямо во время заправки! Хорошо, что предоплата.");
                }
            }

            if (LitersFilled >= PaidLiters - 0.001f)
            {
                NozzleIn = false;
                PlayerFueled = true;
                hose.gameObject.SetActive(false);
                ShowMessage("Бак полный! Садитесь (F), заводите (I) и уезжайте через выезд.", 9f);
            }
        }

        // ---------- Бензин закончился и завоз ----------

        public void TriggerOutOfFuel()
        {
            FuelRanOut = true;
            State = GameState.OutOfFuel;
            barrier.SetDown(true);
            PriceBoard.soldOut = true;
            deliveryTimer = 0f;
        }

        void UpdateDelivery(float dt)
        {
            deliveryTimer += dt;
            float d = Settings.DeliveryDuration;
            if (tankerPhase == 0 && deliveryTimer > d * 0.55f) SpawnTanker();

            if (deliveryTimer >= d)
            {
                barrier.SetDown(false);
                PriceBoard.soldOut = false;
                PriceBoard.RaisePrices(3.5f);
                if (tanker != null)
                {
                    tankerPhase = 3;
                    tankerPath = CityLayout.TankerLeavePath();
                    tankerS = 0f;
                }
                State = GameState.Queueing;
                ShowMessage("Бензин привезли! Цены, правда, подросли.", 6f);
            }
        }

        void SpawnTanker()
        {
            tanker = CarFactory.BuildTanker();
            tanker.SetParent(Traffic.WorldRoot, false);
            tankerPath = CityLayout.TankerArrivePath();
            tankerS = 0f;
            tankerPhase = 1;
            ShowMessage("Едет бензовоз!");
        }

        void UpdateTanker(float dt)
        {
            if (tanker == null) return;
            if (tankerPhase != 1 && tankerPhase != 3) return;
            float remaining = tankerPath.Length - tankerS;
            float speed = tankerPhase == 1 ? Mathf.Clamp(remaining * 0.6f, 1.5f, 10f) : 12f;
            tankerS = Mathf.Min(tankerPath.Length, tankerS + speed * dt);
            tanker.position = tankerPath.PointAt(tankerS);
            tanker.rotation = Quaternion.LookRotation(tankerPath.TangentAt(tankerS));
            if (tankerS < tankerPath.Length - 0.01f) return;
            if (tankerPhase == 1) tankerPhase = 2;
            else
            {
                Destroy(tanker.gameObject);
                tanker = null;
                tankerPhase = 0;
            }
        }

        public void OnPlayerGranted(Pump pump)
        {
            ShowMessage($"Ваша очередь! Свободна колонка №{pump.Number}. Заезжайте.", 9f);
        }

        // ---------- Свой бак пустой ----------

        public void OnPlayerRanDry()
        {
            ShowMessage("Бензин кончился прямо в очереди! Мотор заглох.", 8f);
            dryTimer = 0f;
        }

        /// <summary>Пока толкать машину нельзя, выручает сосед: приносит литр из канистры, чтобы игра не застряла.</summary>
        void UpdateDryTank(float dt)
        {
            if (dryTimer < 0f) return;
            dryTimer += dt;
            if (dryTimer < 10f) return;
            dryTimer = -1f;
            Player.AddFuelLiters(1f);
            ShowMessage("Сосед по очереди поделился литром из канистры. Заводите (I) и больше не жгите зря!", 8f);
        }

        public void OnEngineToggled(bool running)
        {
            if (!running) EngineStops++;
            ShowMessage(running ? "Двигатель заведён." : "Двигатель заглушен. Экономим бензин. I — завести.");
        }

        void Finish(bool gaveUp)
        {
            GaveUp = gaveUp;
            State = GameState.Finished;
            Player.controlsEnabled = false;
            CameraRig.SetCursorLocked(false);
        }

        // ---------- События для статистики и реакций ----------

        public void OnPlayerHonk()
        {
            Honks++;
            Traffic.OnPlayerHonk();
            if (State == GameState.OutOfFuel && Random.value < 0.5f)
                ShowMessage("Заправщик: «Бибикай не бибикай — бензина нет».");
        }

        public void OnPlayerHonkedAt()
        {
            HonkedAt++;
            if (Traffic.PlayerShouldMoveUp)
                ShowMessage(OnFoot ? "Сзади бибикают — очередь ушла, а ваша машина стоит!" : "Сзади бибикают — подъезжайте! (W)");
        }

        public void OnPlayerCrash(float impact, bool hitCar, string obstacle, string report)
        {
            Crashes++;
            if (report != null) ShowMessage(report);
            else if (!hitCar && obstacle != null) ShowMessage($"Бах! Врезались: {obstacle}.");
            else if (hitCar) ShowMessage(impact > 4f ? "Сильный удар! Водитель в ярости." : "Бум! Аккуратнее, это не автосалон.");
        }

        public void OnSomeoneGaveUp(bool ahead)
        {
            GiveUpsSeen++;
            ShowMessage(ahead ? "Кто-то впереди не выдержал и уехал. Минус одна машина!" : "Кто-то сзади сдался и уехал.");
        }

        public void OnPlayerCutIn()
        {
            CutInsSuffered++;
            ShowMessage("Вас подрезали! Машина влезла прямо перед вами. Не оставляйте дырку!", 7f);
        }

        public void OnCutInBlocked()
        {
            CutInsBlocked++;
            ShowMessage("Не пустили наглеца! Так держать.");
        }

        public void OnPlayerSqueezedIn()
        {
            SqueezedIn++;
            ShowMessage("Вы влезли в очередь — соседи недовольны.");
        }

        public void OnRadioSwitched() => RadioSwitches++;

        public void OnBrawlerOut(BrawlReason reason)
        {
            ShowMessage(reason == BrawlReason.CutIn
                ? "Обиженный водитель вышел из машины и идёт к вам! Можно отсидеться или выйти (F) и разобраться."
                : "Водитель вышел разбираться! Сидите в машине или выходите (F).", 8f);
        }

        public void OnVipArrived()
        {
            VipsSeen++;
            ShowMessage("Мигалка! Чёрный «Майбах» летит мимо очереди и заезжает через ВЫЕЗД. Ему нужнее.", 8f);
            foreach (var npc in Traffic.Npcs)
                if (npc.Role == NpcRole.Queue && Random.value < 0.15f) npc.Honk();
        }

        public void OnCarKicked(string report)
        {
            CarKicks++;
            if (report != null) ShowMessage(report);
            else if (CarKicks % 4 == 1) ShowMessage("Вашу машину пинают! Бум! Бдыщ!");
        }

        public void OnPlayerWonFight()
        {
            FightsWon++;
            ShowMessage("Вы победили! Обидчик, хромая, поплёлся к своей машине.", 7f);
        }

        public void OnPlayerLostFight()
        {
            FightsLost++;
            ShowMessage("Вас уложили... Полежите, отдышитесь. Пирожок у продавщицы поможет прийти в себя.", 8f);
        }

        public static string FormatQueueTime(double seconds)
        {
            int total = (int)(seconds / 60.0);
            return $"{total / 60} ч {total % 60:00} мин";
        }

        public List<string> Achievements()
        {
            var list = new List<string> { GaveUp ? "Сдался и уехал без бензина" : "Отстоял очередь и заправился" };
            if (FuelRanOut) list.Add("Бензин кончился прямо перед носом");
            if (Honks == 0) list.Add("Дзен: ни разу не бибикнул");
            if (Honks >= 15) list.Add("Дирижёр клаксонов");
            if (HonkedAt >= 3) list.Add("Тот самый, кто не подъезжает");
            if (Crashes >= 1) list.Add("Поцеловал бампер");
            if (Crashes >= 5) list.Add("Таран");
            if (Player.damage.Front >= 2.8f || Player.damage.Rear >= 2.8f) list.Add("Без бампера, зато с бензином");
            if (CutInsSuffered >= 2) list.Add("Добрая душа (пропустил наглецов)");
            if (CutInsBlocked >= 2) list.Add("Ни сантиметра без очереди");
            if (SqueezedIn >= 1) list.Add("Мне только спросить");
            if (Arguments >= 3) list.Add("Скандалист");
            if (Talks >= 5) list.Add("Душа очереди");
            if (GiveUpsSeen >= 3) list.Add("Свидетель отчаяния");
            if (RadioSwitches >= 10) list.Add("Меломан поневоле");
            if (EngineStops >= 5) list.Add("Эко-водитель");
            if (FightsWon >= 1) list.Add("Чемпион очереди");
            if (CanistersBought >= 1) list.Add("Жертва спекулянта");
            if (VipsSeen >= 1) list.Add("Слуга народа заправился первым");
            if (PiesEaten >= 2) list.Add("Пирожковый марафон");
            if (FightsLost >= 1) list.Add("Получил за дело");
            if (CarKicks >= 5) list.Add("Машина-боксёрская груша");
            return list;
        }
    }
}
