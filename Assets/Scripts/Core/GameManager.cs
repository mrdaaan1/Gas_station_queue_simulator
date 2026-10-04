using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    public enum GameState
    {
        Queueing,    // стоим в очереди (в т.ч. уже первые у колонки)
        OutOfFuel,   // шлагбаум опущен, ждём завоз
        Fueling,     // заправляемся
        DrivingAway, // заправились, уезжаем
        Finished,    // финальный экран
    }

    /// <summary>Главные правила игры: часы очереди, «бензин закончился», завоз, заправка, финал и статистика.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameSettings Settings { get; private set; }
        public QueueManager Queue { get; private set; }
        public PlayerCar Player { get; private set; }
        public CameraRig CameraRig { get; private set; }
        public Radio Radio { get; private set; }
        public PriceBoard PriceBoard { get; private set; }

        public GameState State { get; private set; } = GameState.Queueing;
        public bool FuelRanOut { get; private set; }

        /// <summary>Сколько игрок «стоит в очереди», в игровых секундах.</summary>
        public double QueueSeconds { get; private set; }
        /// <summary>Сколько игровых минут осталось до завоза.</summary>
        public float DeliveryMinutesLeft => Mathf.Max(0f, 60f * (1f - deliveryTimer / Settings.DeliveryDuration));

        // Статистика для финального экрана
        public int Honks { get; private set; }
        public int HonkedAt { get; private set; }
        public int GiveUpsSeen { get; private set; }
        public int Bumps { get; private set; }
        public int RadioSwitches { get; private set; }
        public float LitersFilled { get; private set; }
        public float MoneySpent { get; private set; }

        public string Message { get; private set; }
        public float MessageAlpha => Mathf.Clamp01(messageTimer / 0.5f);

        Barrier barrier;
        System.Action restart;
        float messageTimer;
        float deliveryTimer;
        float priceTimer;
        bool priceWarned;
        float startFuel;

        Transform tanker;
        int tankerPhase; // 0 — нет, 1 — едет к колонке, 2 — сливает, 3 — уезжает
        float tankerSpeed;

        public void Init(GameSettings settings, QueueManager queue, PlayerCar player, CameraRig rig, Radio radio,
            Barrier barrier, PriceBoard board, System.Action restart)
        {
            Instance = this;
            Settings = settings;
            Queue = queue;
            Player = player;
            CameraRig = rig;
            Radio = radio;
            this.barrier = barrier;
            PriceBoard = board;
            this.restart = restart;
            QueueSeconds = settings.startMinutes * 60.0;
            ShowMessage("Вы стоите в очереди уже давно. Подъезжайте, когда очередь двинется (W).", 6f);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool PlayerAtPump => State == GameState.Queueing && Queue.PlayerIndex == 0
                                    && Mathf.Abs(Player.Z - WorldBuilder.PumpZ) < 1.6f && Player.Speed < 0.5f;

        void Update()
        {
            float dt = Time.deltaTime;
            messageTimer -= dt;

            if (State == GameState.Finished)
            {
                if (GameInput.RestartPressed) restart();
                return;
            }

            // Часы идут всегда. Пока ждём завоз — «час» пролетает за время ожидания.
            float rate = State == GameState.OutOfFuel ? 3600f / Settings.DeliveryDuration : Settings.ClockScale;
            if (State != GameState.DrivingAway) QueueSeconds += dt * rate;

            switch (State)
            {
                case GameState.Queueing:
                    if (PlayerAtPump && GameInput.InteractPressed) StartFueling();
                    break;
                case GameState.OutOfFuel:
                    UpdateDelivery(dt);
                    break;
                case GameState.Fueling:
                    UpdateFueling(dt);
                    break;
                case GameState.DrivingAway:
                    if (Player.Z > WorldBuilder.PumpZ + 70f) Finish();
                    break;
            }

            UpdateTanker(dt);
        }

        public void ShowMessage(string text, float seconds = 3.5f)
        {
            Message = text;
            messageTimer = seconds;
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

            if (tankerPhase == 0 && deliveryTimer > d * 0.6f) SpawnTanker();

            if (deliveryTimer >= d)
            {
                barrier.SetDown(false);
                PriceBoard.soldOut = false;
                PriceBoard.price92 += 3.5f;
                PriceBoard.price95 += 3.5f;
                tankerPhase = tanker != null ? 3 : 0;
                State = GameState.Queueing;
                ShowMessage("Бензин привезли! Цены, правда, подросли. Подъезжайте к колонке.", 5f);
            }
        }

        void SpawnTanker()
        {
            tanker = CarFactory.BuildTanker();
            tanker.SetParent(transform.parent, false);
            tanker.position = new Vector3(WorldBuilder.TankerLaneX, 0f, 130f);
            tanker.rotation = Quaternion.Euler(0, 180f, 0);
            tankerPhase = 1;
            tankerSpeed = 10f;
            ShowMessage("Едет бензовоз!");
        }

        void UpdateTanker(float dt)
        {
            if (tanker == null) return;
            var p = tanker.position;
            if (tankerPhase == 1)
            {
                const float stopZ = 2f;
                tankerSpeed = Mathf.Clamp((p.z - stopZ) * 0.8f, 1.5f, 10f);
                p.z = Mathf.Max(stopZ, p.z - tankerSpeed * dt);
                if (p.z <= stopZ + 0.01f) tankerPhase = 2;
            }
            else if (tankerPhase == 3)
            {
                tankerSpeed = Mathf.Min(14f, tankerSpeed + 2f * dt);
                p.z -= tankerSpeed * dt;
                if (p.z < -520f)
                {
                    Destroy(tanker.gameObject);
                    tanker = null;
                    return;
                }
            }
            tanker.position = p;
        }

        // ---------- Заправка ----------

        void StartFueling()
        {
            State = GameState.Fueling;
            Player.controlsEnabled = false;
            startFuel = Player.fuel;
            ShowMessage("Заправляемся... Счётчик ползёт мучительно медленно.", 4f);
        }

        void UpdateFueling(float dt)
        {
            float liters = Settings.LitersPerSecond * dt;
            LitersFilled += liters;
            MoneySpent += liters * PriceBoard.CurrentPrice;
            Player.fuel = Mathf.Min(1f, startFuel + LitersFilled / Settings.tankLiters);

            // Цена на табло меняется прямо во время заправки
            priceTimer += dt;
            if (priceTimer > Settings.ServiceTime * 0.15f)
            {
                priceTimer = 0f;
                PriceBoard.price95 += Random.Range(0.2f, 0.6f);
                PriceBoard.price92 += Random.Range(0.2f, 0.6f);
                if (!priceWarned)
                {
                    priceWarned = true;
                    ShowMessage("Цена на табло растёт прямо во время заправки!");
                }
            }

            if (Player.fuel >= 1f)
            {
                Queue.RemovePlayer();
                Player.controlsEnabled = true;
                State = GameState.DrivingAway;
                ShowMessage("Полный бак! Жмите W и уезжайте отсюда.", 6f);
            }
        }

        void Finish()
        {
            State = GameState.Finished;
            Player.controlsEnabled = false;
            CameraRig.SetCursorLocked(false);
        }

        // ---------- События для статистики и реакций ----------

        public void OnPlayerHonk()
        {
            Honks++;
            Queue.OnPlayerHonk();
            if (State == GameState.OutOfFuel && Random.value < 0.5f)
                ShowMessage("Заправщик: «Бибикай не бибикай — бензина нет».");
        }

        public void OnPlayerHonkedAt()
        {
            HonkedAt++;
            ShowMessage("Сзади бибикают — подъезжайте! (W)");
        }

        public void OnPlayerBump()
        {
            Bumps++;
            ShowMessage("Бум! Аккуратнее, это не автосалон.");
        }

        public void OnSomeoneGaveUp(bool ahead)
        {
            GiveUpsSeen++;
            ShowMessage(ahead ? "Кто-то впереди не выдержал и уехал. Минус одна машина!" : "Кто-то сзади сдался и уехал.");
        }

        public void OnRadioSwitched() => RadioSwitches++;

        public static string FormatQueueTime(double seconds)
        {
            int total = (int)(seconds / 60.0);
            return $"{total / 60} ч {total % 60:00} мин";
        }

        public List<string> Achievements()
        {
            var list = new List<string> { "Отстоял очередь и заправился" };
            if (FuelRanOut) list.Add("Бензин кончился прямо перед носом");
            if (Honks == 0) list.Add("Дзен: ни разу не бибикнул");
            if (Honks >= 15) list.Add("Дирижёр клаксонов");
            if (HonkedAt >= 3) list.Add("Тот самый, кто не подъезжает");
            if (Bumps >= 1) list.Add("Поцеловал бампер");
            if (GiveUpsSeen >= 3) list.Add("Свидетель отчаяния");
            if (RadioSwitches >= 10) list.Add("Меломан поневоле");
            return list;
        }
    }
}
