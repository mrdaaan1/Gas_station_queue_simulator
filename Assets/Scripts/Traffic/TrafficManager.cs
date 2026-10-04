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

        readonly List<LanePath> roadPaths = new List<LanePath>();
        readonly List<LanePath> allPaths = new List<LanePath>();
        readonly Dictionary<LanePath, List<PathEntry>> lanes = new Dictionary<LanePath, List<PathEntry>>();
        readonly List<QueueEntry> queue = new List<QueueEntry>();
        readonly List<NpcCar> despawnList = new List<NpcCar>();

        LanePath leftPath;
        readonly List<LanePath> oncoming = new List<LanePath>();
        readonly List<float> throughTimers = new List<float>();

        int carCounter;
        float tailTimer, giveUpTimer, cutterTimer;
        bool wasInQueue = true;
        readonly HashSet<Vehicle> aheadWhenLeft = new HashSet<Vehicle>();

        GameManager Gm => GameManager.Instance;

        public void Init(GameSettings settings, PlayerCar player, Barrier barrier, Transform worldRoot)
        {
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
            foreach (var p in allPaths) lanes[p] = new List<PathEntry>();

            QueueRoadEndS = QueuePath.Project(CityLayout.P(CityLayout.LaneQueue, -44f), out _);
            SecondRowWaitS = MiddlePath.Project(CityLayout.P(CityLayout.LaneMiddle, -58f), out _);

            SpawnInitial();
            for (int i = 0; i < 1 + oncoming.Count; i++) throughTimers.Add(Random.Range(0f, 3f));
        }

        // ---------- Создание машин ----------

        NpcCar SpawnNpc(string tag)
        {
            var visual = CarFactory.Build($"{tag} {++carCounter}", CarFactory.Paints[Random.Range(0, CarFactory.Paints.Length)],
                CarFactory.RandomShape(), false);
            visual.transform.SetParent(transform, false);
            var npc = visual.gameObject.AddComponent<NpcCar>();
            npc.visual = visual;
            npc.traffic = this;
            npc.damage = visual.gameObject.AddComponent<CarDamage>();
            npc.damage.Init(visual, WorldRoot);
            Npcs.Add(npc);
            return npc;
        }

        void SpawnInitial()
        {
            float spacing = Settings.carSpacing;
            float s = QueuePath.Length;
            for (int i = 0; i < Settings.carsAhead; i++, s -= spacing)
                SpawnNpc("Queue").Setup(NpcRole.Queue, QueuePath, s, true);

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
                if (behind.v is NpcCar npc && aheadWhenLeft.Contains(npc))
                {
                    npc.Honk();
                    npc.Say(AngryAtCutter[Random.Range(0, AngryAtCutter.Length)]);
                    Gm.OnPlayerSqueezedIn();
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

            if (queue.Count > 0 && queue[0].v is NpcCar head && head.S >= QueuePath.Length - 0.6f && head.Speed < 0.2f)
            {
                var pump = FreePump();
                if (pump != null) head.GoToPump(pump);
            }

            if (PlayerIsHead && Gm.State == GameState.Queueing)
            {
                // Шлагбаум опускаем, только когда под ним никто не проезжает
                if (!Gm.FuelRanOut)
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
                    if (Mathf.Abs(e.offset - me.Offset) > 2.4f) continue;
                    bestS = e.s;
                    float f = (e.s - e.v.Length / 2f) - (me.S + me.Length / 2f) - desiredGap;
                    if (f < free)
                    {
                        free = f;
                        blocker = e.v;
                    }
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

            return free;
        }

        /// <summary>Не залезет ли машина NPC в игрока или пешехода.</summary>
        public bool WouldHitPlayer(Obb box)
        {
            var playerBox = Player.Box;
            playerBox.half += new Vector2(0.05f, 0.1f);
            if (Obb.Overlap(box, playerBox)) return true;
            if (Walker != null && Walker.Active && box.PushCircle(Walker.Position2, 0.45f, out _)) return true;
            return false;
        }

        public bool LaneClearNear(LanePath path, float s, float radius)
        {
            if (!lanes.TryGetValue(path, out var list)) return true;
            foreach (var e in list)
                if (Mathf.Abs(e.s - s) < radius && Mathf.Abs(e.offset) < 2f) return false;
            // Машины, которые как раз перестраиваются в этот ряд
            var p = path.PointAt(s);
            foreach (var npc in Npcs)
                if (npc.Role == NpcRole.Cutter || npc.Role == NpcRole.GivingUp)
                    if (Vector3.Distance(npc.Position, p) < radius && npc.Path != path) return false;
            return true;
        }

        // ---------- Вклинивания ----------

        /// <summary>Ищет в очереди «дырку», куда можно влезть, рядом с машиной cutter.</summary>
        public bool FindQueueGap(NpcCar cutter, out Vehicle follower, out float gapCenterS)
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
                if (end - start < cutter.Length + 2.2f) continue;
                float center = (start + end) / 2f;
                if (center < cutter.S - 2f || center > cutter.S + 40f) continue;
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
        public bool GapStillOpen(NpcCar cutter, Vehicle follower, out float gapCenterS)
        {
            gapCenterS = cutter.S;
            for (int i = 1; i < queue.Count; i++)
            {
                if (queue[i].v != follower) continue;
                var leader = queue[i - 1];
                float start = queue[i].s + follower.Length / 2f;
                float end = leader.s - leader.v.Length / 2f;
                gapCenterS = (start + end) / 2f;
                return end - start >= cutter.Length + 0.8f;
            }
            return false;
        }

        bool IsTargeted(Vehicle follower, NpcCar except)
        {
            foreach (var npc in Npcs)
                if (npc != except && npc.Role == NpcRole.Cutter && npc.CutFollower == follower) return true;
            return false;
        }

        /// <summary>Вежливый NPC пропускает того, кто моргает поворотником перед ним.</summary>
        public bool MustYieldToCutter(NpcCar me)
        {
            if (me.Role != NpcRole.Queue) return false;
            foreach (var npc in Npcs)
                if (npc.Role == NpcRole.Cutter && npc.CutFollower == me && npc.CutPolite &&
                    (npc.Cut == NpcCar.CutState.Signaling || npc.Cut == NpcCar.CutState.Merging || npc.Cut == NpcCar.CutState.Aligning))
                    return true;
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
            if (target != null) target.React(AnswersToHonk[Random.Range(0, AnswersToHonk.Length)]);
        }

        // ---------- Новые машины ----------

        void UpdateSpawns(float dt)
        {
            int total = Npcs.Count;

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
                        var npc = SpawnNpc("Queue");
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
                float s = PlayerInQueue ? PlayerQueueS - Random.Range(50f, 90f) : Random.Range(100f, 400f);
                if (cutters < 3 && s > 0f && LaneClearNear(MiddlePath, s, 15f))
                    SpawnNpc("Cutter").SetupCutter(MiddlePath, s);
            }

            // Кто-то в очереди не выдерживает
            giveUpTimer += dt;
            if (giveUpTimer > Settings.GiveUpCheckInterval)
            {
                giveUpTimer = 0f;
                if (Random.value < Settings.giveUpChance) TryGiveUp();
            }
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
