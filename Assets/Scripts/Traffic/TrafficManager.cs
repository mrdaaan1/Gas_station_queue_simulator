using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>Колонка: место для машины, маршруты заезда и выезда, кто сейчас стоит.</summary>
    public class Pump
    {
        public int index;
        public Vector3 spot;
        public Vector3 dispenser;
        public LanePath enterPath;
        public LanePath exitPath;
        public NpcCar Occupant { get; private set; }
        public bool reservedForPlayer;

        public int Number => index + 1;
        public void Occupy(NpcCar car) => Occupant = car;
        public void Release(NpcCar car)
        {
            if (Occupant == car) Occupant = null;
        }
    }

    /// <summary>
    /// Всё движение: очередь, колонки, поток машин по дороге, те, кто вклинивается и кто сдаётся.
    /// Обновляется раньше машин, чтобы они видели свежую картину.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class TrafficManager : MonoBehaviour
    {
        struct PathEntry
        {
            public Vehicle v;
            public float s;
            public float offset;
        }

        struct QueueEntry
        {
            public Vehicle v;
            public float s;
        }

        static readonly string[] AnswersToHonk =
        {
            "Да куда я поеду?!", "Сам бибикай!", "Очередь для всех одна!", "Не нервничай, брат",
            "Ещё раз бибикнешь — выйду!", "Я тоже тороплюсь!", "Тише, тише...", "И что?",
        };
        static readonly string[] GiveUpPhrases =
        {
            "Да ну вас всех!", "Поеду на другую...", "Пешком дойду!", "Всё, я пас", "Завтра приеду",
        };
        static readonly string[] AngryAtCutter = { "Куда без очереди?!", "Э, я тут стою!", "Совсем обнаглели!" };

        public GameSettings Settings { get; private set; }
        public PlayerCar Player { get; private set; }
        public WalkerController Walker { get; set; }
        public Transform WorldRoot { get; private set; }
        public Barrier Barrier { get; private set; }

        public LanePath QueuePath { get; private set; }
        public LanePath MiddlePath { get; private set; }
        public LanePath GasInPath { get; private set; }
        public LanePath GasOutPath { get; private set; }
        /// <summary>Где на маршруте очереди газовые машины сворачивают к пропану (у въезда на территорию).</summary>
        public float GasBranchS { get; private set; }
        public readonly List<Pump> Pumps = new List<Pump>();
        public readonly List<NpcCar> Npcs = new List<NpcCar>();

        /// <summary>Где «второй ряд» ждёт у въезда (s на средней полосе).</summary>
        public float SecondRowWaitS { get; private set; }
        /// <summary>До какого s очередь идёт по прямой дороге (дальше — поворот на заправку).</summary>
        public float QueueRoadEndS { get; private set; }

        // Очередь и место игрока в ней
        public bool PlayerInQueue { get; private set; }
        public int PlayerQueueIndex { get; private set; } = -1;
        public float PlayerQueueS { get; private set; }
        public bool PlayerIsHead { get; private set; }
        public bool PlayerShouldMoveUp { get; private set; }
        public Pump PlayerPump { get; private set; }

        /// <summary>Игрок «договорился» с заправщиком: следующая свободная колонка — его, без очереди.</summary>
        public bool PlayerBribed;

        readonly List<LanePath> roadPaths = new List<LanePath>();
        readonly List<LanePath> allPaths = new List<LanePath>();
        readonly Dictionary<LanePath, List<PathEntry>> lanes = new Dictionary<LanePath, List<PathEntry>>();
        readonly List<QueueEntry> queue = new List<QueueEntry>();
        readonly List<NpcCar> despawnList = new List<NpcCar>();

        LanePath leftPath;
        public LanePath LeftPath => leftPath;
        readonly List<LanePath> oncoming = new List<LanePath>();
        readonly List<float> throughTimers = new List<float>();

        // ---------- Режим «Самая быстрая гонка» ----------

        /// <summary>Гонка: старт на трассе к югу от города, потом все едут в очередь на заправку.</summary>
        public bool RaceMode { get; private set; }
        /// <summary>Светофор дал зелёный — соперники поехали.</summary>
        public bool RaceStarted { get; private set; }
        public LanePath RaceLeft { get; private set; }
        public LanePath RaceRight { get; private set; }
        public LanePath RaceTraffic { get; private set; }
        public readonly List<NpcCar> Racers = new List<NpcCar>();
        /// <summary>Кто уже финишировал (имена по порядку; игрок — «Вы»).</summary>
        public readonly List<string> FinishOrder = new List<string>();
        /// <summary>Машины соперников (номер → внешний вид). В игре — спорткары, в симуляторе не задано.</summary>
        public static System.Func<int, CarVisual> RacerVisual;
        public event System.Action<NpcCar> RacerFinished;
        SpeedProfile profileLeft, profileRight;

        static readonly string[] RacerNames =
        {
            "Доминик", "Брайан", "Летти", "Суки", "Хан", "Роман", "Тедж", "Мия",
        };

        int carCounter;
        float tailTimer, giveUpTimer, cutterTimer;
        bool wasInQueue = true;
        NpcCar pendingBrawler;
        float pendingBrawlerTimer;

        /// <summary>Пешеходы на дороге (продавцы, возмущённые водители) — машины их объезжают… то есть ждут.</summary>
        public readonly List<Transform> Pedestrians = new List<Transform>();
        public readonly List<Vendor> Vendors = new List<Vendor>();

        // Слух «на соседней заправке есть бензин»
        bool rumorDone;
        float rumorTimer;
        float rumorReturnTimer = -1f;
        int rumorReturnCount;
        float rumorSpawnTimer;
        readonly List<(NpcCar car, float at)> rumorQuitters = new List<(NpcCar, float)>();
        float canisterTimer = 25f, pieTimer = 50f;
        readonly HashSet<Vehicle> aheadWhenLeft = new HashSet<Vehicle>();

        GameManager Gm => GameManager.Instance;

        public void Init(GameSettings settings, PlayerCar player, Barrier barrier, Transform worldRoot, bool race = false)
        {
            RaceMode = race;
            Settings = settings;
            Player = player;
            Barrier = barrier;
            WorldRoot = worldRoot;

            QueuePath = CityLayout.QueuePath();
            MiddlePath = CityLayout.MiddleLanePath();
            leftPath = CityLayout.LeftLanePath();
            foreach (float x in new[] { -1.75f, -5.25f, -8.75f }) oncoming.Add(CityLayout.OncomingPath(x));

            roadPaths.Add(QueuePath);
            roadPaths.Add(MiddlePath);
            roadPaths.Add(leftPath);
            roadPaths.AddRange(oncoming);
            allPaths.AddRange(roadPaths);

            for (int i = 0; i < CityLayout.PumpSpots.Length; i++)
            {
                var pump = new Pump
                {
                    index = i,
                    spot = CityLayout.PumpSpots[i],
                    dispenser = CityLayout.P(CityLayout.IslandX[i / 2], CityLayout.IslandZ),
                    enterPath = CityLayout.PumpEnterPath(i),
                    exitPath = CityLayout.PumpExitPath(i),
                };
                Pumps.Add(pump);
                allPaths.Add(pump.enterPath);
                allPaths.Add(pump.exitPath);
            }
            vipPath = CityLayout.VipInPath();
            VipOutPath = CityLayout.VipOutPath();
            allPaths.Add(vipPath);
            allPaths.Add(VipOutPath);
            GasInPath = CityLayout.GasInPath();
            GasOutPath = CityLayout.GasOutPath();
            allPaths.Add(GasInPath);
            allPaths.Add(GasOutPath);
            GasBranchS = QueuePath.Project(CityLayout.GasBranch, out _);
            if (race)
            {
                RaceLeft = RaceLayout.Lane(false);
                RaceRight = RaceLayout.Lane(true);
                RaceTraffic = RaceLayout.TrafficLane();
                profileLeft = new SpeedProfile(RaceLeft, RaceLayout.TopSpeed, RaceLayout.CornerGrip, RaceLayout.Braking);
                profileRight = new SpeedProfile(RaceRight, RaceLayout.TopSpeed, RaceLayout.CornerGrip, RaceLayout.Braking);
                allPaths.Add(RaceLeft);
                allPaths.Add(RaceRight);
                allPaths.Add(RaceTraffic);
            }
            foreach (var p in allPaths) lanes[p] = new List<PathEntry>();

            QueueRoadEndS = QueuePath.Project(CityLayout.P(CityLayout.LaneQueue, -44f), out _);
            SecondRowWaitS = MiddlePath.Project(CityLayout.P(CityLayout.LaneMiddle, -58f), out _);

            SpawnInitial();
            for (int i = 0; i < 1 + oncoming.Count; i++) throughTimers.Add(Random.Range(0f, 3f));
        }

        // ---------- Создание машин ----------

        NpcCar Wrap(CarVisual visual, ServiceKind service)
        {
            visual.transform.SetParent(transform, false);
            var npc = visual.gameObject.AddComponent<NpcCar>();
            npc.visual = visual;
            npc.traffic = this;
            npc.damage = visual.gameObject.AddComponent<CarDamage>();
            npc.damage.Init(visual, WorldRoot);
            npc.Service = service;
            Npcs.Add(npc);
            return npc;
        }

        NpcCar SpawnNpc(string tag)
        {
            var model = CarModels.Random(out bool taxi);
            if (tag == "Cutter" && Random.value < 0.35f) { model = CarModel.Rio; taxi = true; } // таксисты наглее всех
            var service = tag == "Queue" ? RollService() : ServiceKind.None;
            var paint = CarModels.RandomPaint(model, taxi);
            if (service != ServiceKind.None)
            {
                model = service == ServiceKind.Ambulance ? CarModel.Gazelle : CarModel.Rio;
                taxi = false;
                paint = new Color(0.95f, 0.95f, 0.93f);
            }
            var visual = CarFactory.Build($"{(service != ServiceKind.None ? service.ToString() : taxi ? "Taxi" : tag)} {++carCounter}", paint, model, false, taxi, service);
            return Wrap(visual, service);
        }

        // ---------- Газ ----------

        static readonly string[] GasQueueLines =
        {
            "Пропустите, у меня газ!", "Мне только на газ, я быстро!", "Чего стоим? У меня пропан!",
            "Газ — это вам не бензин.", "Мужики, я не за бензином, пустите!",
        };
        static readonly string[] GasReplies =
        {
            "Ага, щас! Стой как все.", "Газовщик хренов...", "Вот гад, а.", "У всех газ, когда очередь.", "Тут один въезд на всех!",
        };

        float gasBanterTimer = 30f;
        NpcCar gasReplyFrom;
        float gasReplyTimer;
        bool gasAnnounced;

        NpcCar SpawnGasCar()
        {
            var npc = SpawnNpc("Gas");
            npc.IsGas = true;
            CarModels.GasSticker(npc.visual);
            return npc;
        }

        public void OnGasCarBranched(NpcCar npc)
        {
            if (gasAnnounced || Gm == null || Vector3.Distance(npc.Position, Player.Position) > 60f) return;
            gasAnnounced = true;
            Gm.ShowMessage("Машина на газу свернула к пропановой колонке справа. Там очереди нет — на газу мало кто ездит.", 8f);
        }

        /// <summary>Газовые машины в очереди просят их пропустить, им огрызаются (по очереди, не хором).</summary>
        void UpdateGasBanter(float dt)
        {
            if (gasReplyFrom != null)
            {
                gasReplyTimer -= dt;
                if (gasReplyTimer <= 0f)
                {
                    if (gasReplyFrom != null) gasReplyFrom.Say(GasReplies[Random.Range(0, GasReplies.Length)]);
                    gasReplyFrom = null;
                }
                return;
            }
            gasBanterTimer -= dt;
            if (gasBanterTimer > 0f) return;
            gasBanterTimer = Random.Range(20f, 35f);
            for (int i = 1; i < queue.Count; i++)
            {
                if (!(queue[i].v is NpcCar gasCar) || !gasCar.IsGas) continue;
                if (Vector3.Distance(gasCar.Position, Player.Position) > 35f) continue;
                gasCar.Say(GasQueueLines[Random.Range(0, GasQueueLines.Length)]);
                gasCar.Honk();
                gasReplyFrom = queue[i - 1].v as NpcCar; // отвечает тот, кто впереди (если это не игрок)
                gasReplyTimer = 3.6f;
                return;
            }
        }

        // ---------- Скорая и ДПС в общей очереди ----------

        static readonly string[] AmbulanceLines =
        {
            "Мы на вызов вообще-то. Но без бензина — не вызов.", "Пациент подождёт, у нас очередь.",
            "Колонка «для своих»? Мы не свои, мы врачи.", "Скорая тоже стоит. Как все.", "Держитесь там. И мы держимся.",
        };
        static readonly string[] PoliceLines =
        {
            "Стоим как все, граждане.", "Мигалку включили — не помогает. Бензина-то нет.",
            "Служебная колонка? Это не для нас, мы просто полиция.", "Документики... Шучу. Стоим.", "Без нарушений, без бензина.",
        };
        static readonly string[] AmbulanceHonk = { "Тише! У нас пациент спит.", "Бибикнете ещё — и вам скорая понадобится.", "Мы бы рады, да некуда." };
        static readonly string[] PoliceHonk = { "Гражданин, не нарушаем!", "Ещё раз — оформлю за шум.", "Сигнал подавать без необходимости запрещено!" };

        float serviceLineTimer = 20f;
        bool serviceAnnounced;

        ServiceKind RollService()
        {
            bool hasAmb = false, hasPol = false;
            foreach (var n in Npcs)
            {
                if (n.Service == ServiceKind.Ambulance) hasAmb = true;
                if (n.Service == ServiceKind.Police) hasPol = true;
            }
            float r = Random.value;
            if (r < 0.05f && !hasAmb) return ServiceKind.Ambulance;
            if (r > 0.95f && !hasPol) return ServiceKind.Police;
            return ServiceKind.None;
        }

        /// <summary>Скорая и ДПС рядом с игроком иногда комментируют происходящее.</summary>
        void UpdateServices(float dt)
        {
            serviceLineTimer -= dt;
            if (serviceLineTimer > 0f) return;
            serviceLineTimer = Random.Range(25f, 40f);
            foreach (var n in Npcs)
            {
                if (n.Service == ServiceKind.None || Vector3.Distance(n.Position, Player.Position) > 35f) continue;
                if (!serviceAnnounced && Gm != null)
                {
                    serviceAnnounced = true;
                    Gm.ShowMessage(n.Service == ServiceKind.Ambulance
                        ? "В очереди стоит скорая. С мигалкой. Колонка «для своих» — только для депутатов."
                        : "Даже ДПС стоит в общей очереди. Служебная колонка — не для них.", 8f);
                }
                var lines = n.Service == ServiceKind.Ambulance ? AmbulanceLines : PoliceLines;
                n.Say(lines[Random.Range(0, lines.Length)]);
                return;
            }
        }

        void SpawnInitial()
        {
            if (RaceMode)
            {
                SpawnRace();
                return;
            }
            float spacing = Settings.carSpacing;
            float s = QueuePath.Length;
            for (int i = 0; i < Settings.carsAhead; i++, s -= spacing)
            {
                // Среди тех, кто впереди, есть газовые — увидите, как они свернут к пропану у въезда
                var npc = i > 2 && Random.value < 0.15f ? SpawnGasCar() : SpawnNpc("Queue");
                npc.Setup(NpcRole.Queue, QueuePath, s, true);
            }

            Player.PlaceOnPath(QueuePath, s);
            s -= spacing;

            for (int i = 0; i < Settings.carsBehind && s > 10f; i++, s -= spacing)
                SpawnNpc("Queue").Setup(NpcRole.Queue, QueuePath, s, true);

            // На колонках уже кто-то заправляется
            foreach (var pump in Pumps)
                SpawnNpc("Pump").PlaceAtPump(pump, Random.Range(0.1f, 0.9f));

            // Немного машин на дороге с самого начала
            foreach (var lane in new[] { leftPath }.Concat(oncoming))
                for (float ls = Random.Range(10f, 60f); ls < lane.Length - 20f; ls += Random.Range(60f, 140f))
                    SpawnNpc("Car").Setup(NpcRole.Through, lane, ls, false);
        }

        // ---------- Гонка ----------

        /// <summary>Номер места игрока на стартовой решётке (0 — поул).</summary>
        public const int PlayerGridSlot = 5;
        public const int GridSize = 8;

        void SpawnRace()
        {
            // В очереди уже стоят обычные машины — гонщики встанут за ними
            float spacing = Settings.carSpacing;
            float s = QueuePath.Length;
            for (int i = 0; i < Settings.raceQueueCars; i++, s -= spacing)
                SpawnNpc("Queue").Setup(NpcRole.Queue, QueuePath, s, true);
            foreach (var pump in Pumps)
                SpawnNpc("Pump").PlaceAtPump(pump, Random.Range(0.1f, 0.9f));
            // Улица перекрыта под гонку: обычного потока по городу нет

            // Стартовая решётка: по две машины в ряд, ряды через 8 м за стартовой линией
            var names = new List<string>(RacerNames);
            for (int i = 0; i < GridSize; i++)
            {
                var lane = i % 2 == 0 ? RaceLeft : RaceRight;
                float gs = RaceLayout.StartLineS - 5f - (i / 2) * 8f - (i % 2) * 2f;
                if (i == PlayerGridSlot)
                {
                    Player.PlaceOnPath(lane, gs);
                    continue;
                }
                var visual = RacerVisual != null ? RacerVisual(i) : CarFactory.Build($"Racer {i}", Color.red, CarModel.Rio, false);
                visual.name = $"Racer {i + 1}";
                var npc = Wrap(visual, ServiceKind.None);
                int k = Random.Range(0, names.Count);
                // Чем ближе к поулу, тем быстрее соперник
                float skill = Mathf.Lerp(1f, 0.88f, i / (float)(GridSize - 1)) * Random.Range(0.97f, 1.02f);
                npc.SetupRacer(lane, gs, ProfileFor(lane), names[k], skill);
                names.RemoveAt(k);
                Racers.Add(npc);
            }

            // Немного обычных машин на трассе — ползут по правой полосе
            for (float ts = 170f + Random.Range(0f, 40f); ts < RaceTraffic.Length - 650f; ts += Random.Range(90f, 150f))
            {
                var car = SpawnNpc("Car");
                car.Setup(NpcRole.Through, RaceTraffic, ts, false);
                car.MaxSpeedCap = Random.Range(8f, 12f);
            }
        }

        public SpeedProfile ProfileFor(LanePath lane) => lane == RaceLeft ? profileLeft : lane == RaceRight ? profileRight : null;

        public LanePath OtherRaceLane(LanePath lane) => lane == RaceLeft ? RaceRight : lane == RaceRight ? RaceLeft : null;

        public void StartRace() => RaceStarted = true;

        /// <summary>Соперник доехал по трассе до главной дороги: из правой полосы — в хвост очереди, из левой — лезет «вторым рядом».</summary>
        public void OnRacerReachedRoad(NpcCar racer)
        {
            if (racer.Path == RaceRight) racer.JoinQueueFromRace(QueuePath);
            else racer.CutFromRace(MiddlePath);
        }

        /// <summary>Ряд очереди рядом с машиной пуст (хвост очереди впереди) — можно просто встать в хвост.</summary>
        public bool QueueLaneFreeBeside(NpcCar npc, bool pushy = false)
        {
            float qs = QueuePath.Project(npc.Position, out float lat);
            if (Mathf.Abs(lat) > (pushy ? 7.5f : 5f) || (!pushy && qs > QueueRoadEndS - 6f)) return false;
            if (!LaneClearNear(QueuePath, qs, pushy ? 7f : 9f, npc)) return false;
            if (pushy) return true; // долго ждёт у въезда — влезает в любую дырку рядом
            // Хвост очереди должен быть впереди: сразу позади нас в очереди никого (иначе это уже «влезть»)
            foreach (var e in queue)
                if (e.s < qs && e.s > qs - 40f) return false;
            return true;
        }

        public void OnRacerFinished(NpcCar racer)
        {
            FinishOrder.Add(racer.RacerName);
            RacerFinished?.Invoke(racer);
        }

        public void Despawn(NpcCar npc)
        {
            if (!despawnList.Contains(npc)) despawnList.Add(npc);
        }

        // ---------- Кадр ----------

        void Update()
        {
            if (Time.deltaTime <= 0f) return;

            foreach (var npc in despawnList)
            {
                if (npc == null) continue;
                if (npc.Pump != null) npc.Pump.Release(npc);
                Npcs.Remove(npc);
                Destroy(npc.gameObject);
            }
            despawnList.Clear();

            RebuildLanes();
            RebuildQueue();
            if (Gm == null || Gm.State == GameState.Finished) return;

            UpdateStation();
            UpdateSpawns(Time.deltaTime);

            if (pendingBrawler != null)
            {
                pendingBrawlerTimer -= Time.deltaTime;
                if (pendingBrawlerTimer <= 0f)
                {
                    Brawler.Spawn(pendingBrawler, this, BrawlReason.CutIn);
                    pendingBrawler = null;
                }
            }
            Pedestrians.RemoveAll(p => p == null);
            if (Brawler.Active != null && !Pedestrians.Contains(Brawler.Active.transform)) Pedestrians.Add(Brawler.Active.transform);
        }

        void RebuildLanes()
        {
            foreach (var list in lanes.Values) list.Clear();
            foreach (var npc in Npcs)
                if (npc.Role != NpcRole.Fueling && lanes.TryGetValue(npc.Path, out var list))
                    list.Add(new PathEntry { v = npc, s = npc.S, offset = npc.Offset });

            // Машина игрока считается стоящей на маршруте, если она вдоль него и не дальше полутора метров вбок
            var pos = Player.Position;
            foreach (var path in allPaths)
            {
                float s = path.Project(pos, out float lat);
                if (Mathf.Abs(lat) > 1.6f || s <= 0.01f || s >= path.Length - 0.01f) continue;
                if (Vector3.Dot(Player.Forward, path.TangentAt(s)) < 0.5f) continue;
                lanes[path].Add(new PathEntry { v = Player, s = s, offset = lat });
            }
        }

        void RebuildQueue()
        {
            queue.Clear();
            foreach (var npc in Npcs)
                if (npc.Role == NpcRole.Queue && npc.Path == QueuePath && Mathf.Abs(npc.Offset) < 1.2f)
                    queue.Add(new QueueEntry { v = npc, s = npc.S });

            float ps = QueuePath.Project(Player.Position, out float lat);
            bool aligned = Vector3.Dot(Player.Forward, QueuePath.TangentAt(ps)) > 0.6f;
            bool served = Gm != null && (Gm.PlayerFueled || Gm.State == GameState.DrivingAway);
            PlayerInQueue = Mathf.Abs(lat) < 1.7f && aligned && ps > 1f && !served;
            PlayerQueueS = ps;
            if (PlayerInQueue) queue.Add(new QueueEntry { v = Player, s = ps });
            queue.Sort((a, b) => b.s.CompareTo(a.s));

            PlayerQueueIndex = -1;
            for (int i = 0; i < queue.Count; i++)
                if (queue[i].v == Player) PlayerQueueIndex = i;

            PlayerIsHead = PlayerQueueIndex == 0 && ps > QueuePath.Length - 25f;

            float gapAhead = 0f;
            if (PlayerQueueIndex > 0)
            {
                var leader = queue[PlayerQueueIndex - 1];
                gapAhead = (leader.s - leader.v.Length / 2f) - (ps + Player.Length / 2f);
            }
            else if (PlayerQueueIndex == 0) gapAhead = PlayerPump != null ? 99f : QueuePath.Length - ps;
            PlayerShouldMoveUp = PlayerInQueue && Mathf.Abs(Player.Speed) < 0.3f && gapAhead > 4.5f && !(Barrier.IsDown && PlayerIsHead);

            // Запоминаем, кто стоял впереди, когда игрок выехал из очереди
            if (!PlayerInQueue && wasInQueue)
            {
                aheadWhenLeft.Clear();
                foreach (var e in queue)
                    if (e.s > ps) aheadWhenLeft.Add(e.v);
            }
            // Игрок вернулся в очередь впереди тех, кто раньше стоял перед ним, — влез без очереди
            if (PlayerInQueue && !wasInQueue && PlayerQueueIndex >= 0 && PlayerQueueIndex + 1 < queue.Count && Gm != null)
            {
                var behind = queue[PlayerQueueIndex + 1];
                if (behind.v is NpcCar npc && aheadWhenLeft.Contains(npc) && npc != courtesyGiver)
                {
                    npc.Honk();
                    npc.Say(AngryAtCutter[Random.Range(0, AngryAtCutter.Length)]);
                    Gm.OnPlayerSqueezedIn();
                    // Обиженный выходит разбираться — но не сразу, сначала побибикает
                    if (Random.value < 0.65f) pendingBrawler = npc;
                    pendingBrawlerTimer = 2.5f;
                }
            }
            wasInQueue = PlayerInQueue;
        }

        public bool IsDirectlyBehindPlayer(NpcCar npc) =>
            PlayerQueueIndex >= 0 && PlayerQueueIndex + 1 < queue.Count && queue[PlayerQueueIndex + 1].v == npc;

        // ---------- Заправка: кого пускать к колонкам ----------

        void UpdateStation()
        {
            if (Barrier.IsDown) return;

            if (queue.Count > 0 && queue[0].v is NpcCar head && head.S >= QueuePath.Length - 1.5f && head.Speed < 0.2f)
            {
                var pump = FreePump();
                if (pump != null) head.GoToPump(pump);
            }

            if (PlayerIsHead && Gm.State == GameState.Queueing)
            {
                // Шлагбаум опускаем, только когда под ним никто не проезжает
                // В гонке шлагбаум не опускаем: и так весело
                if (!Gm.FuelRanOut && !RaceMode)
                {
                    if (BarrierClear()) Gm.TriggerOutOfFuel();
                }
                else if (PlayerPump == null)
                {
                    var pump = FreePump();
                    if (pump != null)
                    {
                        pump.reservedForPlayer = true;
                        PlayerPump = pump;
                        Gm.OnPlayerGranted(pump);
                    }
                }
            }
            else if (PlayerBribed && PlayerPump == null && Gm.State == GameState.Queueing && !Gm.PlayerFueled)
            {
                var pump = FreePump();
                if (pump != null)
                {
                    pump.reservedForPlayer = true;
                    PlayerPump = pump;
                    Gm.OnPlayerGranted(pump);
                    // Очередь видит, кого пустили без очереди
                    if (queue.Count > 0 && queue[0].v is NpcCar first)
                    {
                        first.Honk();
                        first.Say("Э! А он куда?! Мы тут с утра стоим!");
                    }
                }
            }
        }

        bool BarrierClear()
        {
            var box = Barrier.Box;
            box.half += new Vector2(0.5f, 2.5f);
            foreach (var npc in Npcs)
                if (Obb.Overlap(box, npc.Box)) return false;
            return true;
        }

        Pump FreePump()
        {
            Pump best = null;
            int seen = 0;
            foreach (var p in Pumps)
            {
                if (p.Occupant != null || p.reservedForPlayer) continue;
                if (Vector3.Distance(Player.Position, p.spot) < 3.5f) continue;
                // Выбираем случайную свободную, чтобы машины не липли к одной колонке
                if (Random.Range(0, ++seen) == 0) best = p;
            }
            return best;
        }

        /// <summary>У какой колонки стоит машина игрока (или null).</summary>
        public Pump PumpAtPlayerCar()
        {
            foreach (var p in Pumps)
                if (p.Occupant == null && Vector3.Distance(Player.Position, p.spot) < 2.4f) return p;
            return null;
        }

        public void ReleasePlayerPump()
        {
            if (PlayerPump != null) PlayerPump.reservedForPlayer = false;
            PlayerPump = null;
        }

        // ---------- Дистанция до препятствия впереди ----------

        /// <summary>
        /// Сколько метров машина может проехать вперёд, сохраняя дистанцию desiredGap.
        /// Учитывает машину впереди на своём маршруте, всё, что попадает в «датчик» перед капотом, и пешехода.
        /// </summary>
        public float FreeDistance(NpcCar me, float desiredGap, out Vehicle blocker)
        {
            float free = float.MaxValue;
            blocker = null;

            if (lanes.TryGetValue(me.Path, out var list))
            {
                float bestS = float.MaxValue;
                foreach (var e in list)
                {
                    if (e.v == me || e.s <= me.S || e.s >= bestS) continue;
                    // Наглец, который перестраивается из нашего ряда (или передумал и возвращается),
                    // держит своё место — иначе задний заедет туда, и вернуться будет некуда
                    bool holdsSlot = e.v is NpcCar c && c.Role == NpcRole.Cutter;
                    if (Mathf.Abs(e.offset - me.Offset) > 2.4f && !holdsSlot) continue;
                    bestS = e.s;
                    float f = (e.s - e.v.Length / 2f) - (me.S + me.Length / 2f) - desiredGap;
                    if (f < free)
                    {
                        free = f;
                        blocker = e.v;
                    }
                }
            }

            // Кто уже наполовину перестроился в наш ряд — для нас он уже впереди
            foreach (var other in Npcs)
            {
                if (other == me || other.TargetPath != me.Path || Mathf.Abs(other.Offset) < 1f) continue;
                var d = other.Position - me.Position;
                if (d.x * d.x + d.z * d.z > 400f) continue;
                float os = me.Path.Project(other.Position, out _);
                if (os <= me.S) continue;
                float f = (os - other.Length / 2f) - (me.S + me.Length / 2f) - desiredGap;
                if (f < free)
                {
                    free = f;
                    blocker = other;
                }
            }

            var box = me.Box;
            float corridor = me.Width / 2f + 0.25f;
            var myPos = me.Position;
            for (int i = -1; i < Npcs.Count; i++)
            {
                Vehicle v = i < 0 ? (Vehicle)Player : Npcs[i];
                if (v == me || v == null) continue;
                var d = v.Position - myPos;
                if (d.x * d.x + d.z * d.z > 40f * 40f) continue;
                // Взаимная блокировка (обе машины видят друг друга) — проезжает та, что «старше»
                if (v is NpcCar other && other.BlockedBy == me && me.Id > other.Id) continue;
                if (Obb.AheadDistance(box, v.Box, corridor, 28f, out float dist))
                {
                    float f = dist - desiredGap;
                    if (f < free)
                    {
                        free = f;
                        blocker = v;
                    }
                }
            }

            if (Walker != null && Walker.Active && Obb.AheadDistance(box, Walker.Box, corridor + 0.3f, 20f, out float wd))
                free = Mathf.Min(free, wd - 1.5f);
            foreach (var p in Pedestrians)
            {
                if (p == null || !p.gameObject.activeInHierarchy) continue;
                if (Obb.AheadDistance(box, new Obb(p.position, Vector3.forward, 0.7f, 0.7f), corridor + 0.3f, 20f, out float pd))
                    free = Mathf.Min(free, pd - 1.5f);
            }

            return free;
        }

        /// <summary>
        /// Не залезет ли машина NPC в игрока или пешехода. Если машины уже чуть-чуть касаются,
        /// разрешаем движение, которое их не сближает (иначе обе застрянут навсегда).
        /// </summary>
        public bool WouldHitPlayer(Obb box, Obb current)
        {
            var playerBox = Player.Box;
            playerBox.half += new Vector2(0.05f, 0.1f);
            if (Obb.Overlap(box, playerBox, out var mtvNew))
            {
                if (!Obb.Overlap(current, playerBox, out var mtvOld)) return true;
                if (mtvNew.sqrMagnitude > mtvOld.sqrMagnitude + 0.0001f) return true;
            }
            if (Walker != null && Walker.Active && box.PushCircle(Walker.Position2, 0.45f, out _)) return true;
            foreach (var p in Pedestrians)
                if (p != null && p.gameObject.activeInHierarchy && box.PushCircle(new Vector2(p.position.x, p.position.z), 0.45f, out _)) return true;
            return false;
        }

        /// <summary>Нет ли рядом с точкой ни одной машины (по реальным координатам, а не по параметру маршрута).</summary>
        public bool AreaClear(Vector3 point, float radius, NpcCar except = null)
        {
            foreach (var npc in Npcs)
            {
                if (npc == except) continue;
                var d = npc.Position - point;
                if (d.x * d.x + d.z * d.z < radius * radius) return false;
            }
            var pd = Player.Position - point;
            return pd.x * pd.x + pd.z * pd.z >= radius * radius;
        }

        public bool LaneClearNear(LanePath path, float s, float radius, NpcCar except = null)
        {
            if (!lanes.TryGetValue(path, out var list)) return true;
            foreach (var e in list)
                if (e.v != except && Mathf.Abs(e.s - s) < radius && Mathf.Abs(e.offset) < 2f) return false;
            // Машины, которые как раз перестраиваются в этот ряд (уже сместились больше чем на метр)
            var p = path.PointAt(s);
            foreach (var npc in Npcs)
                if (npc != except && npc.TargetPath == path && Mathf.Abs(npc.Offset) > 1f &&
                    Vector3.Distance(npc.Position, p) < radius) return false;
            return true;
        }

        // ---------- Вклинивания ----------

        /// <summary>Ищет в очереди «дырку», куда можно влезть, рядом с машиной cutter.</summary>
        public bool FindQueueGap(NpcCar cutter, float gapNeeded, float minCenterS, out Vehicle follower, out float gapCenterS)
        {
            follower = null;
            gapCenterS = 0f;
            float best = float.MaxValue;
            for (int i = 1; i < queue.Count; i++)
            {
                var leader = queue[i - 1];
                var f = queue[i];
                if (f.s > QueueRoadEndS - 6f) continue; // только на прямом участке дороги
                if (IsTargeted(f.v, cutter)) continue;
                float start = f.s + f.v.Length / 2f;
                float end = leader.s - leader.v.Length / 2f;
                if (end - start < gapNeeded) continue;
                float center = (start + end) / 2f;
                if (center < cutter.S - 2f || center > cutter.S + 40f || center < minCenterS) continue;
                float dist = Mathf.Abs(center - cutter.S);
                if (dist < best)
                {
                    best = dist;
                    follower = f.v;
                    gapCenterS = center;
                }
            }
            return follower != null;
        }

        /// <summary>Дырка перед follower ещё открыта? gapCenterS — где её середина сейчас.</summary>
        public bool GapStillOpen(NpcCar cutter, Vehicle follower, float minGap, out float gapCenterS)
        {
            gapCenterS = cutter.S;
            for (int i = 1; i < queue.Count; i++)
            {
                if (queue[i].v != follower) continue;
                var leader = queue[i - 1];
                float start = queue[i].s + follower.Length / 2f;
                float end = leader.s - leader.v.Length / 2f;
                gapCenterS = (start + end) / 2f;
                return end - start >= minGap;
            }
            return false;
        }

        bool IsTargeted(Vehicle follower, NpcCar except)
        {
            foreach (var npc in Npcs)
                if (npc != except && npc.Role == NpcRole.Cutter && npc.CutFollower == follower) return true;
            return false;
        }

        /// <summary>Вежливый NPC пропускает того, кто уже моргает поворотником перед ним.</summary>
        public bool MustYieldToCutter(NpcCar me)
        {
            if (me.Role != NpcRole.Queue) return false;
            foreach (var npc in Npcs)
                if (npc.Role == NpcRole.Cutter && npc.CutFollower == me && npc.CutPolite &&
                    (npc.Cut == NpcCar.CutState.Signaling || npc.Cut == NpcCar.CutState.Merging))
                    return true;
            return false;
        }

        /// <summary>Кто-то невежливо целится влезть прямо перед этой машиной.</summary>
        public bool IsCutterTarget(NpcCar me)
        {
            if (me.Role != NpcRole.Queue) return false;
            foreach (var npc in Npcs)
                if (npc.Role == NpcRole.Cutter && npc.CutFollower == me && !npc.CutPolite && npc.Cut != NpcCar.CutState.Looking)
                    return true;
            return false;
        }

        float lastPoliteTime = -999f;

        /// <summary>
        /// Пропустит ли сосед наглеца. В жизни такое редко: примерно каждый седьмой,
        /// и не чаще раза в минуту на всю очередь.
        /// </summary>
        public bool RollPoliteness()
        {
            if (Time.time - lastPoliteTime < 60f / Mathf.Max(1f, Settings.fastTestMode ? Settings.testSpeedup : 1f)) return false;
            if (Random.value > 0.15f) return false;
            lastPoliteTime = Time.time;
            return true;
        }

        /// <summary>Не залезет ли машина NPC в другую машину NPC (касание, которое не усиливается, разрешаем).</summary>
        public bool WouldHitNpc(NpcCar me, Obb box, Obb current)
        {
            var c = box.center;
            foreach (var other in Npcs)
            {
                if (other == me) continue;
                var d = other.Box.center - c;
                if (d.sqrMagnitude > 64f) continue;
                var ob = other.Box;
                if (!Obb.Overlap(box, ob, out var mtvNew)) continue;
                if (!Obb.Overlap(current, ob, out var mtvOld)) return true;
                if (mtvNew.sqrMagnitude > mtvOld.sqrMagnitude + 0.0001f) return true;
            }
            return false;
        }

        public void OnCutterJoined(NpcCar cutter, Vehicle follower)
        {
            if (follower == null) return;
            if (follower.IsPlayer) Gm.OnPlayerCutIn();
            else if (follower is NpcCar npc && Random.value < 0.6f)
            {
                npc.Honk();
                npc.Say(AngryAtCutter[Random.Range(0, AngryAtCutter.Length)]);
            }
        }

        public void OnSlowDriver(NpcCar npc)
        {
            if (Gm != null && PlayerQueueIndex >= 0 && PlayerQueueIndex <= 8 && npc.Pump != null)
                Gm.ShowMessage($"Водитель у колонки №{npc.Pump.Number} что-то тупит...");
        }

        // ---------- Реакции ----------

        public void OnPlayerHonk()
        {
            // Отвечает тот, кто прямо перед игроком (в очереди или на дороге)
            NpcCar target = null;
            if (PlayerQueueIndex > 0) target = queue[PlayerQueueIndex - 1].v as NpcCar;
            if (target == null)
            {
                float best = 25f;
                var box = Player.Box;
                foreach (var npc in Npcs)
                    if (Obb.AheadDistance(box, npc.Box, 1.6f, 25f, out float d) && d < best)
                    {
                        best = d;
                        target = npc;
                    }
            }
            if (target != null)
            {
                var lines = target.Service == ServiceKind.Ambulance ? AmbulanceHonk
                    : target.Service == ServiceKind.Police ? PoliceHonk : AnswersToHonk;
                target.React(lines[Random.Range(0, lines.Length)]);
            }
        }

        static readonly string[] RoofLines =
        {
            "Э! Ты чё на крышу залез?!", "Слезь с машины, больной!", "Это тебе не батут!",
            "Ты мне крышу помнёшь!", "Мужик, ты в порядке вообще?", "Я сейчас выйду!",
            "Совсем от очереди крыша поехала...", "Снимаю на видео, будешь звездой!",
        };

        readonly Dictionary<NpcCar, int> roofJumps = new Dictionary<NpcCar, int>();

        /// <summary>Игрок спрыгнул на крышу машины NPC. С третьего раза водитель может выйти разбираться.</summary>
        public void OnPlayerJumpedOnCar(NpcCar car)
        {
            roofJumps.TryGetValue(car, out int n);
            roofJumps[car] = ++n;
            car.Hold(2.5f);
            car.Say(RoofLines[Random.Range(0, RoofLines.Length)]);
            if (Random.value < 0.6f) car.Honk();
            if (car.damage != null) car.damage.Wear(0.8f, Vector3.zero);
            Gm.OnJumpedOnCar();
            if (n >= 3 && Random.value < 0.5f) Brawler.Spawn(car, this, BrawlReason.Roof);
        }

        // ---------- Новые машины ----------

        void UpdateSpawns(float dt)
        {
            int total = Npcs.Count;
            if (RaceMode)
            {
                UpdateRaceSpawns(dt, total);
                return;
            }

            // Хвост очереди растёт
            tailTimer += dt;
            if (tailTimer > Settings.ServiceTime * 0.7f)
            {
                tailTimer = 0f;
                int behind = PlayerQueueIndex >= 0 ? queue.Count - 1 - PlayerQueueIndex : queue.Count;
                if (behind < Settings.maxCarsBehind && queue.Count > 0 && total < 110)
                {
                    float spawnS = queue[queue.Count - 1].s - Settings.carSpacing * 4f;
                    if (spawnS > 5f && LaneClearNear(QueuePath, spawnS, 8f))
                    {
                        // Каждая седьмая-восьмая — на газу (чаще такси): встаёт в хвост, чтобы доехать до въезда
                        bool gas = Random.value < 0.14f;
                        var npc = gas ? SpawnGasCar() : SpawnNpc("Queue");
                        npc.Setup(NpcRole.Queue, QueuePath, spawnS, true);
                        npc.StartRolling(5f);
                    }
                }
            }

            // Поток по дороге: левая полоса нашего направления и встречка
            for (int i = 0; i < throughTimers.Count; i++)
            {
                throughTimers[i] -= dt;
                if (throughTimers[i] > 0f) continue;
                throughTimers[i] = Random.Range(Settings.trafficIntervalMin, Settings.trafficIntervalMax);
                var lane = i == 0 ? leftPath : oncoming[i - 1];
                if (total < 110 && LaneClearNear(lane, 0f, 25f))
                    SpawnNpc("Car").Setup(NpcRole.Through, lane, 0f, false);
            }

            // Кто-то пытается влезть из соседнего ряда
            cutterTimer += dt;
            if (cutterTimer > Settings.CutterInterval)
            {
                cutterTimer = 0f;
                int cutters = 0;
                foreach (var n in Npcs) if (n.Role == NpcRole.Cutter) cutters++;
                // Большинство едет к съезду и встаёт вторым рядом, остальные лезут где-то рядом с игроком
                bool toEntrance = Random.value < 0.7f;
                float s = toEntrance ? SecondRowWaitS - Random.Range(90f, 220f)
                    : PlayerInQueue ? PlayerQueueS - Random.Range(50f, 90f) : Random.Range(100f, 400f);
                if (cutters < 4 && s > 0f && LaneClearNear(MiddlePath, s, 15f))
                    SpawnNpc("Cutter").SetupCutter(MiddlePath, s, toEntrance);
            }

            UpdateVendors(dt);
            UpdateRumor(dt);
            UpdatePlayerSignal(dt);
            UpdateVip(dt);
            UpdateServices(dt);
            UpdateGasBanter(dt);

            // Кто-то в очереди не выдерживает
            giveUpTimer += dt;
            if (giveUpTimer > Settings.GiveUpCheckInterval)
            {
                giveUpTimer = 0f;
                if (Random.value < Settings.giveUpChance) TryGiveUp();
            }
        }

        /// <summary>В гонке улица перекрыта: ни потока, ни депутата. Только продавцы и разговоры. Хвост очереди — сами гонщики.</summary>
        void UpdateRaceSpawns(float dt, int total)
        {
            UpdateVendors(dt);
            UpdatePlayerSignal(dt);
            UpdateServices(dt);
        }

        // ---------- Игрок моргает правым поворотником во втором ряду ----------

        float signalTimer;
        bool courtesyRolled;
        NpcCar courtesyGiver;

        void UpdatePlayerSignal(float dt)
        {
            float ps = QueuePath.Project(Player.Position, out float lat);
            bool signaling = Player.visual.blinker == 1 && !PlayerInQueue && !Gm.OnFoot &&
                             lat < -1.7f && lat > -5.5f && ps < QueueRoadEndS && Mathf.Abs(Player.Speed) < 3f;
            if (!signaling)
            {
                signalTimer = 0f;
                courtesyRolled = false;
                return;
            }
            signalTimer += dt;
            if (signalTimer < 1.5f || courtesyRolled) return;
            courtesyRolled = true;

            // Тот, кто стоит в очереди рядом с нами (чуть сзади), решает — пустить или нет
            NpcCar candidate = null;
            float best = float.MinValue;
            foreach (var e in queue)
                if (e.v is NpcCar npc && e.s > ps - 8f && e.s < ps + 1f && e.s > best)
                {
                    best = e.s;
                    candidate = npc;
                }
            if (candidate == null) return;
            if (Random.value < 0.3f)
            {
                courtesyGiver = candidate;
                candidate.Hold(7f);
                candidate.Say("Давай, проезжай");
                Gm.ShowMessage("Вас пропускают! Вот что значит поворотник.");
            }
            else candidate.Say(Random.value < 0.5f ? "Ага, щас!" : "Стой как все!");
        }

        // ---------- Депутат с мигалкой ----------

        LanePath vipPath;
        public LanePath VipOutPath { get; private set; }

        public void OnVipFueling()
        {
            Gm.ShowMessage(Gm.State == GameState.OutOfFuel
                ? "У всех «бензина нет», а у служебной колонки «для своих» — есть. Депутат заправляется."
                : "Депутат заправляется у служебной колонки «для своих». Без очереди, разумеется.", 7f);
        }
        float vipTimer = 200f;

        void UpdateVip(float dt)
        {
            vipTimer -= dt * (Settings.fastTestMode ? Settings.testSpeedup : 1f);
            if (vipTimer > 0f) return;
            vipTimer = Random.Range(650f, 850f);
            foreach (var n in Npcs) if (n.IsVip && n.Role != NpcRole.Exiting) return; // один депутат за раз
            float s = Random.Range(60f, 140f);
            if (!AreaClear(vipPath.PointAt(s), 14f)) return;
            var visual = CarFactory.Build($"VIP {++carCounter}", new Color(0.04f, 0.04f, 0.05f), CarModel.Maybach, false);
            visual.transform.SetParent(transform, false);
            var npc = visual.gameObject.AddComponent<NpcCar>();
            npc.visual = visual;
            npc.traffic = this;
            npc.damage = visual.gameObject.AddComponent<CarDamage>();
            npc.damage.Init(visual, WorldRoot);
            Npcs.Add(npc);
            npc.SetupVip(vipPath);
            Gm.OnVipArrived();
        }

        // ---------- Продавцы вдоль очереди ----------

        void UpdateVendors(float dt)
        {
            Vendors.RemoveAll(v => v == null);
            // Продавцы ходят вдоль очереди и подходят к игроку, даже если он выехал из неё во второй ряд
            bool onRoad = Player.Position.x < CityLayout.LotMinX && Player.Position.z < CityLayout.LotMinZ - 30f;
            if (!onRoad || Gm == null || Gm.OnFoot) return;
            canisterTimer -= dt;
            pieTimer -= dt;
            if (canisterTimer <= 0f)
            {
                canisterTimer = Random.Range(45f, 80f);
                TrySpawnVendor(VendorKind.Canister);
            }
            if (pieTimer <= 0f)
            {
                pieTimer = Random.Range(60f, 100f);
                TrySpawnVendor(VendorKind.Pies);
            }
        }

        void TrySpawnVendor(VendorKind kind)
        {
            foreach (var v in Vendors) if (v.Kind == kind) return;
            float dir = Random.value < 0.5f ? 1f : -1f;
            float z = Player.Position.z - dir * Random.Range(35f, 55f);
            if (z > CityLayout.LotMinZ - 20f) z = Player.Position.z - 45f;
            Vendors.Add(Vendor.Spawn(kind, this, z, dir));
        }

        /// <summary>Продавец у любого окна машины игрока.</summary>
        public Vendor VendorAtCar(float radius)
        {
            var box = Player.Box;
            foreach (var v in Vendors)
                if (v != null && box.PushCircle(new Vector2(v.transform.position.x, v.transform.position.z), radius, out _)) return v;
            return null;
        }

        public Vendor VendorNear(Vector3 point, float radius)
        {
            foreach (var v in Vendors)
                if (v != null && Vector3.Distance(v.transform.position, point) < radius) return v;
            return null;
        }

        // ---------- Слух про соседнюю заправку ----------

        void UpdateRumor(float dt)
        {
            if (!rumorDone && PlayerInQueue && PlayerQueueIndex >= 6 && PlayerQueueIndex <= 16 && Gm.State == GameState.Queueing)
            {
                rumorTimer += dt;
                if (rumorTimer > Settings.ServiceTime * 2.5f) StartRumor();
            }

            for (int i = rumorQuitters.Count - 1; i >= 0; i--)
            {
                var (car, at) = rumorQuitters[i];
                if (Time.time < at) continue;
                rumorQuitters.RemoveAt(i);
                if (car != null && car.Role == NpcRole.Queue) car.GiveUp("На соседней есть бензин!");
            }

            if (rumorReturnTimer < 0f) return;
            rumorReturnTimer -= dt;
            if (rumorReturnTimer > 0f || rumorReturnCount <= 0) return;
            // Вернулись ни с чем — встают в конец очереди
            rumorSpawnTimer -= dt;
            if (rumorSpawnTimer > 0f || queue.Count == 0) return;
            float spawnS = queue[queue.Count - 1].s - Settings.carSpacing * 4f;
            if (spawnS <= 5f || !LaneClearNear(QueuePath, spawnS, 8f)) return;
            var npc = SpawnNpc("Queue");
            npc.Setup(NpcRole.Queue, QueuePath, spawnS, true);
            npc.StartRolling(6f);
            rumorSpawnTimer = 1.5f;
            if (--rumorReturnCount == 0)
                Gm.ShowMessage("Те, кто уехал на «соседнюю», вернулись — там тоже пусто. Теперь они в конце очереди.", 8f);
        }

        void StartRumor()
        {
            rumorDone = true;
            var candidates = new List<NpcCar>();
            foreach (var e in queue)
                if (e.v is NpcCar npc && npc.S < QueueRoadEndS - 15f) candidates.Add(npc);
            int count = Mathf.Min(8, Mathf.RoundToInt(candidates.Count * 0.4f));
            if (count == 0) return;
            Gm.ShowMessage("По очереди пошёл слух: «На соседней заправке есть бензин!» Половина очереди срывается с места...", 9f);
            float t = Time.time;
            for (int i = 0; i < count; i++)
            {
                int k = Random.Range(0, candidates.Count);
                rumorQuitters.Add((candidates[k], t + i * Random.Range(0.8f, 2f)));
                candidates.RemoveAt(k);
            }
            rumorReturnCount = count;
            rumorReturnTimer = Settings.ServiceTime * 5f;
        }

        void TryGiveUp()
        {
            var candidates = new List<NpcCar>();
            int p = PlayerQueueIndex;
            for (int i = 1; i < queue.Count; i++)
            {
                if (p >= 0 && Mathf.Abs(i - p) > 10) continue;
                if (queue[i].v is NpcCar npc && npc.Speed < 0.1f && npc.S < QueueRoadEndS - 10f)
                    candidates.Add(npc);
            }
            if (candidates.Count == 0) return;
            var quitter = candidates[Random.Range(0, candidates.Count)];
            bool ahead = p >= 0 && quitter.S > PlayerQueueS;
            quitter.GiveUp(GiveUpPhrases[Random.Range(0, GiveUpPhrases.Length)]);
            Gm.OnSomeoneGaveUp(ahead);
        }
    }

    static class EnumerableExt
    {
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> a, IEnumerable<T> b)
        {
            foreach (var x in a) yield return x;
            foreach (var x in b) yield return x;
        }
    }
}
