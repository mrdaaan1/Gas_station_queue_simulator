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
        /// <summary>Машина разбита — игра проиграна.</summary>
        public bool CarWrecked { get; private set; }
        float wreckTimer;

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

        /// <summary>Продавец стоит у окна машины — E значит «купить», а не «правый поворотник».</summary>
        public bool VendorAtWindow { get; private set; }

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
        public int CarJumps { get; private set; }
        public int Bribes { get; private set; }
        public bool BribeScammed { get; private set; }
        public bool HitLiterLimit { get; private set; }
        public bool WaitedCashierBreak { get; private set; }
        public int LineCutAttempts { get; private set; }
        public int LinePlacesTaken { get; private set; }
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

        static readonly string[] AttendantIdle =
        {
            "Не толпимся!", "Сначала оплата, потом пистолет!", "Куда без очереди?!", "Завоз? Не знаю ничего.",
            "Глушим моторы у колонок!", "Мне за это не доплачивают.",
        };
        static readonly string[] DeliveryRumors =
        {
            "Завоз обещали к обеду. Какого дня — не сказали.", "Бензовоз в пробке стоит. В очереди на другую заправку.",
            "Директор сказал: ситуация стабильная. Значит, бензина не будет.", "Привезут. Наверное. Я тут вообще стажёр.",
        };
        const int BribePrice = 1000;
        const float LiterLimit = 20f;

        enum DialogWith { Cashier, Attendant }
        DialogWith dialogWith;
        HumanRig attendant;
        float attendantLineTimer = 8f;
        bool literLimitToday;
        bool breakUsed;
        float breakTimer;      // > 0 — кассир на перерыве
        float breakLeaveTimer; // кассир договаривает и уходит
        TextMesh breakSign;

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

            CashierLine.Cashier = cashier.transform;
            literLimitToday = Random.value < 0.35f;
            BuildAttendant();
            breakSign = Fonts.WorldText(traffic.WorldRoot, CityLayout.CounterFront + new Vector3(1.05f, 1.35f, 0f), "ПЕРЕРЫВ\n15 МИН", Shapes.Hex("#d32f2f"), 0.035f);
            breakSign.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            breakSign.gameObject.SetActive(false);

            ShowMessage("Вы в очереди на заправку. Подъезжайте за машиной впереди (W), рулите A/D.", 8f);
            ShowMessage("Не оставляйте дырку впереди — влезут! Esc — пауза, Tab — управление.", 8f);
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

            // Машину добили — даём пару секунд посмотреть на дым и показываем проигрыш
            if (Player.damage.Wrecked)
            {
                wreckTimer += dt;
                if (wreckTimer > 3f)
                {
                    CarWrecked = true;
                    Finish(false);
                    return;
                }
            }
            UpdateDryTank(dt);
            if (State == GameState.OutOfFuel) UpdateDelivery(dt);
            UpdateTanker(dt);
            UpdateFueling(dt);
            UpdateCashier(dt);
            UpdateAttendant(dt);
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
                else if (Walker.Active && Vector3.Distance(Walker.transform.position, DialogAnchor) > 2.5f) CloseDialog();
                return;
            }

            // Продавец у окна (в машине) или рядом (пешком)
            var vendor = Traffic.VendorNear(OnFoot ? Walker.transform.position : Player.DriverDoor, OnFoot ? 1.8f : 2.4f);
            VendorAtWindow = !OnFoot && vendor != null && vendor.Offering;
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
            bool nearAttendant = Vector3.Distance(me, CityLayout.AttendantSpot) < 1.8f;
            bool inShop = me.x > CityLayout.ShopMinX && me.x < CityLayout.ShopMaxX && me.z > CityLayout.ShopMinZ && me.z < CityLayout.ShopMaxZ;
            int lineIndex = UpdatePlayerLinePlace(me);
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
            else if (nearCashier && breakTimer > 0f)
            {
                Prompt = $"Кассир на перерыве. Табличка: «15 минут». Ждать ещё ~{Mathf.CeilToInt(breakTimer)} с";
            }
            else if (nearCashier && (lineIndex == 0 || CashierLine.Count == 0 || (lineIndex < 0 && FrontIsAway())))
            {
                Prompt = "E — поговорить с кассиром";
                if (GameInput.InteractPressed)
                {
                    if (lineIndex != 0) CashierLine.InsertAt(Walker, 0);
                    OpenCashierDialog();
                }
            }
            else if (nearCashier)
            {
                Prompt = "Перед вами очередь. E — всё равно пролезть к кассе";
                if (GameInput.InteractPressed) TryCutCashierLine();
            }
            else if (inShop && lineIndex < 0)
            {
                Prompt = CashierLine.Count == 0 ? "Касса свободна — подойдите к прилавку" : $"E — встать в очередь в кассу (перед вами {CashierLine.Count} чел.)";
                if (CashierLine.Count > 0 && GameInput.InteractPressed)
                {
                    int place = CashierLine.Join(Walker) + 1;
                    ShowMessage($"Вы встали в очередь в кассу. Вы {place}-й. Отойдёте дальше пары метров — место займут.", 7f);
                }
            }
            else if (lineIndex > 0)
            {
                Prompt = $"Очередь в кассу: вы {lineIndex + 1}-й. Стойте на своём месте";
            }
            else if (lineIndex == 0)
            {
                Prompt = "Ваша очередь в кассу! Подойдите к прилавку";
            }
            else if (nearAttendant)
            {
                Prompt = "E — поговорить с заправщиком";
                if (GameInput.InteractPressed) OpenAttendantDialog();
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

        Vector3 DialogAnchor => dialogWith == DialogWith.Cashier ? CityLayout.CounterFront : CityLayout.AttendantSpot;

        void OpenCashierDialog()
        {
            // Один раз за игру кассир уходит на перерыв прямо перед вами
            if (!breakUsed && !Paid && Traffic.PlayerPump != null && Random.value < 0.4f)
            {
                breakUsed = true;
                WaitedCashierBreak = true;
                SpeechBubble.Show(cashier.transform, "Ой, у меня перерыв. Пятнадцать минут!", 1.5f);
                breakLeaveTimer = 2f;
                breakTimer = 45f / Settings.Speedup;
                ShowMessage("Кассир ушла на перерыв прямо перед вами. «15 минут». Вся очередь ждёт.", 8f);
                return;
            }
            dialogWith = DialogWith.Cashier;
            DialogOpen = true;
            DialogTitle = "Кассир: «Слушаю вас. Какая колонка?»";
            DialogOptions.Clear();
            DialogOptions.Add(Paid ? "Я уже оплатил(а)..." : $"Оплатить полный бак (АИ-95, {PriceBoard.CurrentPrice:0.00} руб/л)");
            DialogOptions.Add("Поругаться");
            DialogOptions.Add("Уйти");
        }

        void CloseDialog()
        {
            // Отошли от кассы — место в очереди уже не ваше
            if (DialogOpen && dialogWith == DialogWith.Cashier) CashierLine.Leave(Walker);
            DialogOpen = false;
            DialogOptions.Clear();
        }

        // ---------- Очередь в кассу ----------

        static readonly string[] LineAngry =
        {
            "Мужчина, тут очередь!", "В конец очереди!", "Самый умный, что ли?", "Мы тоже торопимся!", "Совсем обнаглели!",
        };

        /// <summary>Место игрока в очереди в кассу (−1 — не стоит). Отошёл далеко — место потерял.</summary>
        int UpdatePlayerLinePlace(Vector3 me)
        {
            int index = CashierLine.IndexOf(Walker);
            if (index < 0 || DialogOpen) return index;
            float allowed = index == 0 ? 3.5f : 2.5f;
            if (Vector3.Distance(me, CashierLine.Slot(index)) > allowed && Vector3.Distance(me, CityLayout.CounterFront) > 1.6f)
            {
                CashierLine.Leave(Walker);
                ShowMessage("Вы отошли — очередь в кассу сомкнулась. Становитесь в конец.", 6f);
                return -1;
            }
            return index;
        }

        /// <summary>Первый в очереди ещё не дошёл до прилавка (только что встал) — касса фактически свободна.</summary>
        bool FrontIsAway()
        {
            if (CashierLine.Count == 0) return true;
            var front = CashierLine.Members[0];
            return Vector3.Distance(front.transform.position, CityLayout.CounterFront) > 4f && front is PumpCustomer;
        }

        void TryCutCashierLine()
        {
            LineCutAttempts++;
            CashierSays(LineCutAttempts >= 3
                ? "Я вас третий раз прошу: в конец очереди! Или охрану позову."
                : "Мужчина, вы без очереди! Не обслуживаю. Встаньте в конец.");
            cashier.Rage(1.5f);
            if (CashierLine.Count > 0)
            {
                var front = CashierLine.Members[0];
                if (front != null) SpeechBubble.Show(front.transform, LineAngry[Random.Range(0, LineAngry.Length)], 1.5f);
            }
            ShowMessage("Без очереди кассир не обслуживает. Встаньте в конец (E в магазине)... или «убедите» кого-нибудь уступить место.", 7f);
        }

        public int BystandersPunched { get; private set; }

        public void OnPunchedBystander()
        {
            BystandersPunched++;
            if (BystandersPunched == 1) ShowMessage("Вы ударили человека на заправке. Кассир уже тянется к телефону...", 6f);
        }

        /// <summary>Игрок уложил стоявшего в очереди — встаёт на его место, если был рядом.</summary>
        public void OnLineVictimDown(int index, Vector3 at)
        {
            if (!OnFoot || Walker == null || !Walker.Active) return;
            if (Vector3.Distance(Walker.transform.position, at) > 4f) return;
            int mine = CashierLine.IndexOf(Walker);
            if (mine >= 0 && mine <= index) return; // и так стояли впереди
            CashierLine.InsertAt(Walker, index);
            LinePlacesTaken++;
            ShowMessage($"Вы заняли место избитого в очереди. Теперь вы {index + 1}-й. Очередь притихла и смотрит в пол.", 8f);
            for (int i = index + 1; i < CashierLine.Count; i++)
                if (Random.value < 0.5f) SpeechBubble.Show(CashierLine.Members[i].transform, LineAngry[Random.Range(0, LineAngry.Length)], 1.5f);
        }

        void CashierSays(string text)
        {
            SpeechBubble.Show(cashier.transform, text, 1.5f);
            DialogTitle = $"Кассир: «{text}»";
        }

        void ChooseDialog(int choice)
        {
            if (dialogWith == DialogWith.Attendant)
            {
                ChooseAttendant(choice);
                return;
            }
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
            bool limited = literLimitToday && liters > LiterLimit;
            if (limited) liters = LiterLimit;
            if (liters * price > Money) liters = Mathf.Floor(Money / price);
            if (liters < 1f) { CashierSays("Денег не хватает даже на литр. Следующий!"); return; }
            if (limited)
            {
                HitLiterLimit = true;
                ShowMessage("«Лимит — 20 литров в одни руки». Три часа в очереди ради 20 литров.", 8f);
            }

            float cost = liters * price;
            Money -= cost;
            MoneySpent += cost;
            Paid = true;
            PaidLiters = liters;
            paidPump = pump;
            bool terminalGlitch = Random.value < 0.3f;
            CashierSays(limited
                ? $"Больше двадцати не положено, распоряжение сверху. Колонка №{pump.Number}, {liters:0} л."
                : terminalGlitch
                ? $"Терминал завис... А, прошло. Колонка №{pump.Number}, {liters:0} л. Вставляйте пистолет."
                : $"Колонка №{pump.Number}, {liters:0} литров, {cost:0} руб. Вставляйте пистолет.");
            ShowMessage($"Оплачено: {liters:0} л на колонке №{pump.Number}. Подойдите к лючку бака (справа сзади) и нажмите E.", 8f);
        }

        void UpdateCashier(float dt)
        {
            if (cashier == null) return;
            if (breakLeaveTimer > 0f)
            {
                breakLeaveTimer -= dt;
                if (breakLeaveTimer <= 0f)
                {
                    CashierLine.CashierAway = true;
                    cashier.gameObject.SetActive(false);
                    breakSign.gameObject.SetActive(true);
                }
            }
            else if (breakTimer > 0f)
            {
                breakTimer -= dt;
                if (breakTimer <= 0f)
                {
                    CashierLine.CashierAway = false;
                    cashier.gameObject.SetActive(true);
                    breakSign.gameObject.SetActive(false);
                    SpeechBubble.Show(cashier.transform, "Ну, что там у вас? Я с чаем.", 1.5f);
                    ShowMessage("Кассир вернулась с перерыва. Можно платить.", 6f);
                }
            }
            if (!cashier.gameObject.activeSelf) return;
            cashier.Animate(0f, dt);
            if (!OnFoot) return;
            // Кассир провожает игрока взглядом
            var to = Walker.transform.position - cashier.transform.position;
            to.y = 0f;
            if (to.magnitude < 8f && to.sqrMagnitude > 0.01f)
                cashier.transform.rotation = Quaternion.Slerp(cashier.transform.rotation, Quaternion.LookRotation(to.normalized), dt * 3f);
        }

        // ---------- Заправщик ----------

        void BuildAttendant()
        {
            var look = new HumanRig.Look
            {
                shirt = Shapes.Hex("#d32f2f"),
                pants = Shapes.Hex("#2b2f3a"),
                skin = Shapes.Hex("#e0ac85"),
                hair = Shapes.Hex("#2a2a2a"),
                shoes = Shapes.Hex("#1b1b1b"),
            };
            attendant = HumanRig.Build("Attendant", Traffic.WorldRoot, look);
            attendant.transform.position = CityLayout.AttendantSpot;
            attendant.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            // Светоотражающий жилет
            Shapes.Box(attendant.transform, new Vector3(0f, 1.15f, 0f), new Vector3(0.44f, 0.4f, 0.26f), Shapes.Hex("#c6e83a"), name: "Vest");
            Obstacles.AddBox(CityLayout.AttendantSpot, 0.5f, 0.5f, "заправщик");
        }

        void UpdateAttendant(float dt)
        {
            if (attendant == null) return;
            attendant.Animate(0f, dt);
            attendantLineTimer -= dt;
            if (attendantLineTimer <= 0f)
            {
                attendantLineTimer = Random.Range(18f, 30f);
                SpeechBubble.Show(attendant.transform, AttendantIdle[Random.Range(0, AttendantIdle.Length)], 1.5f);
            }
            if (OnFoot)
            {
                var to = Walker.transform.position - attendant.transform.position;
                to.y = 0f;
                if (to.magnitude < 6f && to.sqrMagnitude > 0.01f)
                    attendant.transform.rotation = Quaternion.Slerp(attendant.transform.rotation, Quaternion.LookRotation(to.normalized), dt * 3f);
            }
        }

        void OpenAttendantDialog()
        {
            dialogWith = DialogWith.Attendant;
            DialogOpen = true;
            DialogTitle = "Заправщик: «Чего тебе?»";
            DialogOptions.Clear();
            DialogOptions.Add(Traffic.PlayerBribed ? "Ну что там с колонкой?" : $"Сунуть {BribePrice} руб.: «Пропусти вперёд, брат»");
            DialogOptions.Add("Спросить, когда завоз");
            DialogOptions.Add("Уйти");
        }

        void AttendantSays(string text)
        {
            SpeechBubble.Show(attendant.transform, text, 1.5f);
            DialogTitle = $"Заправщик: «{text}»";
        }

        void ChooseAttendant(int choice)
        {
            if (choice == 2) { AttendantSays(DeliveryRumors[Random.Range(0, DeliveryRumors.Length)]); return; }
            if (choice != 1) { CloseDialog(); return; }

            if (PlayerFueled || Traffic.PlayerPump != null) { AttendantSays($"Тебе ж колонку дали. Езжай давай."); return; }
            if (Traffic.PlayerBribed) { AttendantSays("Жди, говорю. Освободится — махну."); return; }
            if (BribeScammed) { AttendantSays("Какую тысячу? Я тебя первый раз вижу."); return; }
            if (State == GameState.OutOfFuel) { AttendantSays("Бензина нет. Хоть миллион давай — из воздуха не налью."); return; }
            if (Money < BribePrice) { AttendantSays("Ты мне мелочь не суй."); return; }

            float roll = Random.value;
            if (roll < 0.2f)
            {
                AttendantSays("Ты чё, тут камеры! Стой как все.");
                return;
            }
            Money -= BribePrice;
            MoneySpent += BribePrice;
            Bribes++;
            if (roll < 0.45f)
            {
                // Взял и забыл
                BribeScammed = true;
                AttendantSays("Договорились, жди. Всё будет.");
                return;
            }
            Traffic.PlayerBribed = true;
            AttendantSays("Тихо. Как колонка освободится — махну. Объезжай очередь и заезжай.");
            ShowMessage("Заправщик взял тысячу. Ждите сигнала — колонку дадут без очереди.", 8f);
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
            if (Traffic.PlayerBribed && !Traffic.PlayerIsHead)
            {
                if (attendant != null) SpeechBubble.Show(attendant.transform, $"Эй! Сюда, на {pump.Number}-ю! Быстро!", 1.5f);
                ShowMessage($"Заправщик машет: колонка №{pump.Number} ваша. Объезжайте очередь — сзади уже бибикают.", 9f);
                return;
            }
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
            if (reason == BrawlReason.Roof)
            {
                ShowMessage("Хозяин машины вышел и требует слезть с крыши. Сверху он вас не достанет...", 8f);
                return;
            }
            ShowMessage(reason == BrawlReason.CutIn
                ? "Обиженный водитель вышел из машины и идёт к вам! Можно отсидеться или выйти (F) и разобраться."
                : "Водитель вышел разбираться! Сидите в машине или выходите (F).", 8f);
        }

        public void OnJumpedOnCar()
        {
            CarJumps++;
            if (CarJumps == 1) ShowMessage("Вы на крыше чужой машины. Очередь снимает вас на телефоны.");
            if (CarJumps == 10) ShowMessage("Кто-то уже выложил видео «Мужик скачет по машинам в очереди за бензином».", 7f);
        }

        public void OnJumpedOnOwnCar()
        {
            CarJumps++;
            string report = Player.damage.Wear(1.2f, Vector3.zero);
            if (report != null) ShowMessage(report);
            else if (Random.value < 0.3f) ShowMessage("Крыша родной «семёрки» жалобно хрустнула.");
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
            var list = new List<string> { CarWrecked ? "Металлолом: машина не дожила до заправки" : GaveUp ? "Сдался и уехал без бензина" : "Отстоял очередь и заправился" };
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
            if (CarJumps >= 1) list.Add("Паркур в очереди");
            if (CarJumps >= 10) list.Add("Король крыш");
            if (Bribes >= 1 && !BribeScammed) list.Add("Всё решается");
            if (BribeScammed) list.Add("Кинули на тысячу");
            if (HitLiterLimit) list.Add("20 литров в одни руки");
            if (WaitedCashierBreak) list.Add("Перерыв 15 минут");
            if (LinePlacesTaken >= 1) list.Add("Очередь по понятиям");
            if (LineCutAttempts >= 3) list.Add("Я только чек спросить");
            return list;
        }
    }
}
