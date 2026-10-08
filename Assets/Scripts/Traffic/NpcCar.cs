using UnityEngine;

namespace GasQueue
{
    public enum NpcRole
    {
        Queue,     // стоит в очереди
        ToPump,    // заезжает к колонке
        Fueling,   // заправляется
        Exiting,   // уезжает с заправки
        Through,   // просто едет мимо по дороге
        Cutter,    // пытается вклиниться в очередь из соседнего ряда
        GivingUp,  // не выдержал, перестраивается и уезжает
        Vip,       // депутат с мигалкой: едет к заправке мимо очереди
        ToGas,     // на газу: свернул из очереди к газовой колонке
        GasFueling,// заправляется газом
        Racing,    // гонщик на трассе (режим «Самая быстрая гонка»)
        Rushing,   // гонщик едет к колонкам без очереди и ждёт рядом с головой очереди
    }

    /// <summary>
    /// Машина NPC. Едет по маршруту (LanePath), держит дистанцию до машины впереди,
    /// умеет перестраиваться (смещение вбок от маршрута), заправляться, вклиниваться и сдаваться.
    /// </summary>
    public class NpcCar : Vehicle
    {
        const float Accel = 2.6f;
        const float Decel = 5f;
        const float LaneChangeRate = 1.3f; // м/с вбок
        const float ArriveTolerance = 0.25f; // «доехал до места», если осталось меньше

        static readonly string[] HonkAtPlayer =
        {
            "Проезжай давай!", "Уснул, что ли?!", "Э! Двигайся!", "Ну поехали уже!", "Алё, очередь!",
        };
        static readonly string[] BlockedRoad =
        {
            "Ты чего встал посреди дороги?!", "Освободи полосу!", "Права купил?!",
        };
        static readonly string[] CursesAfterCrash =
        {
            "Ты офигел?! Заплатишь за всё!", "Глаза разуй!", "Страховку доставай!", "Ну всё, приехали...",
            "Ты куда смотришь?!", "Это новая машина была!",
        };
        static readonly string[] CutInThanks = { "Спасибо, брат!", "Хе-хе, успел", "Мне только спросить!", "Я тут стоял!" };
        static readonly string[] CutInFail = { "Ну и стой!", "Жлоб!", "Да пропусти ты!", "Пф-ф..." };
        static readonly string[] IntolerantShouts =
        {
            "Иди нахер!", "Куда лезешь?!", "Ага, щас!", "Хрен тебе, а не место!", "В конец очереди, умник!",
            "Я тут с утра стою!", "Даже не думай!", "Самый умный, да?!",
        };

        /// <summary>
        /// «Нетерпила»: увидел, что перед ним кто-то лезет, — сразу поджимается к машине впереди почти вплотную
        /// и орёт в окно. Примерно половина очереди такие.
        /// </summary>
        public bool Intolerant { get; private set; }
        bool pressing;
        float shoutCooldown;

        public override bool IsPlayer => false;

        // ---------- Гонка ----------

        /// <summary>Соперник в гонке: после трассы стоит в очереди как все, потом едет к финишу.</summary>
        public bool IsRacer { get; private set; }
        public string RacerName { get; private set; }
        public bool RaceFinished { get; private set; }
        /// <summary>Ограничение скорости для обычной машины на трассе (0 — нет).</summary>
        public float MaxSpeedCap;
        float skill = 1f;
        SpeedProfile profile;
        float raceStuck;
        float accel = Accel, decel = Decel, laneRate = LaneChangeRate;

        public NpcRole Role { get; private set; }
        public LanePath Path { get; private set; }
        public float S { get; private set; }
        public float Offset { get; private set; }
        public Pump Pump { get; private set; }

        /// <summary>В какой ряд машина сейчас перестраивается (или null).</summary>
        public LanePath TargetPath => pathAfterLaneChange;

        /// <summary>Остановиться в конце маршрута (голова очереди, место у колонки).</summary>
        public bool StopAtEnd { get; private set; }

        // Движение
        bool moving;
        float reactTimer;
        float reactionDelay;
        float targetOffset;
        LanePath pathAfterLaneChange;
        float holdTimer;      // водитель вышел ругаться — машина стоит
        float blockedTimer;   // как долго нас держит игрок
        float impatience;

        // Заправка
        float serviceTimer;
        PumpCustomer customer;

        /// <summary>Сколько налито: 0…1. Покупатель ждёт у колонки, пока не станет 1.</summary>
        public float FuelProgress => serviceDuration > 0f ? serviceTimer / serviceDuration : 1f;
        float serviceDuration;

        // Вклинивание
        public enum CutState { Looking, Aligning, Signaling, Merging, Waiting }
        public CutState Cut { get; private set; }
        public Vehicle CutFollower { get; private set; } // перед кем пытаемся влезть
        public bool CutPolite { get; private set; }       // пропустит ли нас NPC-сосед
        float cutTimer;
        float cutCooldown;
        float cutTargetS;

        // Сдаться
        float giveUpTimer;

        AudioSource horn;

        protected override void Awake()
        {
            Intolerant = Random.value < 0.5f;
            base.Awake();
            reactionDelay = Random.Range(0.4f, 1.4f);
            horn = SoundFactory.Source3D(gameObject, 0.8f);
            horn.clip = SoundFactory.Horn;
            horn.pitch = Random.Range(0.8f, 1.15f);
        }

        /// <summary>Гонщик на стартовой решётке.</summary>
        public void SetupRacer(LanePath lane, float s, SpeedProfile speedProfile, string name, float racerSkill)
        {
            IsRacer = true;
            RacerName = name;
            skill = racerSkill;
            profile = speedProfile;
            accel = 12.5f * racerSkill;
            decel = 13f;
            laneRate = 3.6f;
            // В уличной гонке трасса кончается парковкой за финишем — там и встаём
            Setup(NpcRole.Racing, lane, s, traffic.StreetRace);
            moving = false;
            Speed = 0f;
            reactionDelay = Random.Range(0.02f, 0.2f); // реакция на зелёный — все срываются почти разом
        }

        /// <summary>Трасса кончилась на правой полосе — в хвост очереди.</summary>
        bool fromTrack;

        public void JoinQueueFromRace(LanePath queuePath)
        {
            fromTrack = true;
            Role = NpcRole.Queue;
            Path = queuePath;
            S = queuePath.Project(transform.position, out _);
            Offset = targetOffset = 0f;
            pathAfterLaneChange = null;
            visual.blinker = 0;
            StopAtEnd = true;
            decel = 8f;
        }

        static readonly string[] RushLines = { "Я только спросить!", "Мне быстро, у меня гонка!", "Пропустите, я гонщик!", "Я тут занимал!" };

        /// <summary>Трасса кончилась на левой полосе — мимо очереди прямо к колонкам.</summary>
        public void RushToPumps(LanePath rush)
        {
            Role = NpcRole.Rushing;
            Path = rush;
            S = rush.Project(transform.position, out _);
            Offset = targetOffset = 0f;
            pathAfterLaneChange = null;
            visual.blinker = 0;
            StopAtEnd = true;
            decel = 8f;
            laneRate = LaneChangeRate;
        }

        /// <summary>Перейти на другой маршрут без рывка: стоим там же, плавно смещаемся к его оси.</summary>
        void SlideOnto(LanePath path)
        {
            Path = path;
            S = path.Project(transform.position, out float lateral);
            Offset = lateral;
            targetOffset = 0f;
            pathAfterLaneChange = null;
        }

        /// <summary>Доехал до места у головы очереди — кричит, что ему только спросить.</summary>
        public void ShoutRush()
        {
            Say(RushLines[Random.Range(0, RushLines.Length)]);
            Honk();
        }

        /// <summary>Колонка освободилась — заезжает к ней своим путём (не от головы очереди).</summary>
        public void GoToPump(Pump pump, LanePath via)
        {
            GoToPump(pump);
            Path = via;
            S = 0f;
        }

        /// <summary>Трасса кончилась на левой полосе — едет к въезду и лезет «вторым рядом».</summary>
        public void CutFromRace(LanePath middle)
        {
            Role = NpcRole.Cutter;
            Path = middle;
            S = middle.Project(transform.position, out _);
            Offset = targetOffset = 0f;
            pathAfterLaneChange = null;
            visual.blinker = 0;
            StopAtEnd = false;
            Cut = CutState.Looking;
            AimsAtEntrance = true;
            cutTimer = 0f;
            decel = 8f;
            laneRate = 3f; // гонщик перестраивается резко
        }

        public void Setup(NpcRole role, LanePath path, float s, bool stopAtEnd)
        {
            Role = role;
            Path = path;
            S = s;
            Offset = targetOffset = 0f;
            StopAtEnd = stopAtEnd;
            moving = role == NpcRole.Through || role == NpcRole.Exiting || role == NpcRole.Cutter;
            if (moving) Speed = path.speedLimit * 0.8f;
            Place(path.PointAt(s), path.TangentAt(s));
        }

        float SpeedLimit()
        {
            float limit;
            switch (Role)
            {
                case NpcRole.Cutter: limit = Cut == CutState.Looking ? 6f : 4f; break;
                case NpcRole.GivingUp: limit = Offset > -3f ? 3f : 10f; break;
                case NpcRole.Vip: limit = S > Path.Length - 45f ? 5f : Path.speedLimit; break; // по территории — потише
                case NpcRole.Racing:
                    limit = profile != null ? profile.At(S) * skill * CatchUp() : Path.speedLimit;
                    if (RaceFinished) limit = Mathf.Min(limit, 9f); // финишировал — катится к парковке
                    break;
                case NpcRole.Rushing: limit = S < Path.Length - 40f ? Path.speedLimit : 8f; break;
                default: limit = Path.speedLimit; break;
            }
            if (IsRacer)
            {
                // Гонщик и по городу спешит: до хвоста очереди, до въезда «вторым рядом», от выезда до финиша
                if (Role == NpcRole.Queue && S < traffic.QueueRoadEndS - 25f) limit = 32f;
                else if (Role == NpcRole.Cutter && Cut == CutState.Looking && S < traffic.SecondRowWaitS - 40f) limit = 30f;
                else if (Role == NpcRole.Exiting && transform.position.x < 11f) limit = 44f * skill;
            }
            // Приехал с трассы в хвост очереди — подъезжает к хвосту, а не ползёт 500 м со скоростью очереди
            if (fromTrack && !IsRacer && Role == NpcRole.Queue && S < traffic.QueueRoadEndS - 25f) limit = 14f;
            if (MaxSpeedCap > 0f) limit = Mathf.Min(limit, MaxSpeedCap);
            return limit;
        }

        /// <summary>
        /// «Резинка», как в аркадных гонках: игрок уехал вперёд — отстающие едут резвее (до +20%),
        /// так соперники не теряются из виду. Игрок позади — без поблажек.
        /// </summary>
        float CatchUp()
        {
            float lead = traffic.PlayerRaceS - S;
            return 1f + Mathf.Clamp(lead / 200f, 0f, 0.2f);
        }

        float DesiredGap => Role == NpcRole.Racing ? 1.5f + Speed * 0.3f : Role == NpcRole.Through ? 5f : pressing ? 0.55f : 2f;

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (cutCooldown > 0f) cutCooldown -= dt;
            if (shoutCooldown > 0f) shoutCooldown -= dt;

            if (holdTimer > 0f)
            {
                holdTimer -= dt;
                Speed = 0f;
                moving = false;
                return;
            }

            // На старте ждём зелёного
            if (Role == NpcRole.Racing && !traffic.RaceStarted) return;

            if (Role == NpcRole.GasFueling)
            {
                serviceTimer += dt;
                if (serviceTimer >= serviceDuration) StartGasExit();
                return;
            }

            if (Role == NpcRole.Fueling)
            {
                if (customer != null)
                {
                    // Водитель ходит платить: бензин льётся только после оплаты, уезжаем, когда он сел обратно
                    if (customer.Paid) serviceTimer = Mathf.Min(serviceDuration, serviceTimer + dt);
                    if (customer.Done)
                    {
                        customer = null;
                        StartExit();
                    }
                    return;
                }
                serviceTimer += dt;
                if (serviceTimer >= serviceDuration) StartExit();
                return;
            }

            if (Role == NpcRole.Cutter) UpdateCutter(dt);
            if (Role == NpcRole.GivingUp) UpdateGivingUp(dt);

            Drive(dt);

            if (Role == NpcRole.Exiting && Pump != null && S > 8f)
            {
                Pump.Release(this);
                Pump = null;
            }

            // Остановились в паре сантиметров от конца маршрута — считаем, что доехали
            // (иначе машина вечно стоит у колонки, не начиная заправку)
            bool atEnd = S >= Path.Length - (StopAtEnd ? ArriveTolerance : 0.05f);
            if (atEnd && !moving && StopAtEnd && S < Path.Length)
            {
                S = Path.Length;
                Place(Path.PointAt(S), Path.TangentAt(S));
            }
            // Газовая машина доехала до въезда — сворачивает из очереди к пропану
            if (IsGas && Role == NpcRole.Queue && Path == traffic.QueuePath && S >= traffic.GasBranchS) BranchToGas();

            if (atEnd && !moving)
            {
                if (Role == NpcRole.ToGas) StartGasFueling();
                if (Role == NpcRole.ToPump) StartFueling();
                else if (Role == NpcRole.Vip)
                {
                    StartFueling();
                    traffic.OnVipFueling();
                }
            }
            // Гонщик пересёк финиш
            if (IsRacer && !RaceFinished && !traffic.StreetRace && Role == NpcRole.Exiting && transform.position.z > RaceLayout.FinishZ && transform.position.x < 14f)
            {
                RaceFinished = true;
                traffic.OnRacerFinished(this);
            }
            // Уличная гонка: пересёк финишную линию в кармане
            if (IsRacer && !RaceFinished && traffic.StreetRace && Role == NpcRole.Racing && S >= traffic.FinishSOn(Path))
            {
                RaceFinished = true;
                traffic.OnRacerFinished(this);
            }
            // Трасса кончилась — дальше очередь на заправку
            if (Role == NpcRole.Racing && atEnd)
            {
                traffic.OnRacerReachedRoad(this);
                return;
            }
            // Обычная машина с трассы тоже едет заправляться
            if (atEnd && Role == NpcRole.Through && Path == traffic.RaceTraffic && !traffic.StreetRace)
            {
                JoinQueueFromRace(traffic.QueuePath);
                MaxSpeedCap = 0f;
                return;
            }
            if (atEnd && !StopAtEnd) traffic.Despawn(this);
        }

        // ---------- Езда ----------

        void Drive(float dt)
        {
            float free = traffic.FreeDistance(this, DesiredGap, out var blocker);
            BlockedBy = blocker;
            // Если впереди не машина, а просто конец маршрута (место у колонки, голова очереди) —
            // доезжаем до него, даже если осталось совсем чуть-чуть
            bool limitedByStop = false;
            if (StopAtEnd && Path.Length - S < free)
            {
                free = Path.Length - S;
                limitedByStop = true;
            }
            if (Role == NpcRole.Cutter && Cut != CutState.Looking)
                free = Mathf.Min(free, cutTargetS - S + (Cut == CutState.Merging ? 0.5f : 0f));
            if (traffic.MustYieldToCutter(this)) free = Mathf.Min(free, 0f);

            // Перестроение: пока машина смещается вбок, она должна хоть немного ехать
            bool changingLane = Mathf.Abs(targetOffset - Offset) > 0.01f;
            float limit = SpeedLimit();

            // Невежливый сосед, перед которым кто-то моргает поворотником, спешит поджать дырку.
            // Нетерпила ещё и прижимается к машине впереди почти вплотную и ругается.
            bool targeted = traffic.IsCutterTarget(this);
            pressing = targeted && Intolerant;
            if (!moving && targeted) reactionDelay = Mathf.Min(reactionDelay, Intolerant ? 0.05f : 0.15f);
            // Орём только когда игрок за рулём: пешком он никуда не лезет, а крики вокруг принимал на свой счёт
            var gmNow = GameManager.Instance;
            bool playerOnFoot = gmNow != null && gmNow.OnFoot;
            if (pressing && shoutCooldown <= 0f && !playerOnFoot)
            {
                shoutCooldown = 8f;
                Say(IntolerantShouts[Random.Range(0, IntolerantShouts.Length)]);
                if (traffic.PlayerCutTarget == this) traffic.OnIntolerantBlocksPlayer();
                Honk();
            }

            if (!moving)
            {
                float startThreshold = limitedByStop ? ArriveTolerance : Role == NpcRole.Through || Role == NpcRole.Racing ? 0.3f : 1.2f;
                if (free > startThreshold || (changingLane && free > 0.6f))
                {
                    reactTimer += dt;
                    if (reactTimer >= reactionDelay)
                    {
                        moving = true;
                        reactTimer = 0f;
                        reactionDelay = Role == NpcRole.Racing ? Random.Range(0.05f, 0.2f) : Random.Range(0.4f, 1.4f);
                    }
                }
                else reactTimer = 0f;
            }

            float step = 0f;
            if (moving)
            {
                float stopDistance = Speed * Speed / (2f * decel);
                // Разгон слабеет к максимальной скорости (у гонщиков заметно)
                float push = IsRacer ? accel * Mathf.Clamp(1.15f - Speed / (traffic.Track.TopSpeed * 1.1f), 0.25f, 1f) : accel;
                Speed = free <= stopDistance + 0.05f
                    ? Mathf.Max(0f, Speed - decel * dt)
                    : Mathf.Min(limit, Speed + push * dt);
                if (Speed > limit) Speed = Mathf.Max(limit, Speed - decel * dt);

                step = Mathf.Min(Speed * dt, Mathf.Max(0f, free));
                if (free < 0.08f && Speed < 0.4f)
                {
                    Speed = 0f;
                    moving = false;
                }
            }

            float newS = S + step;
            if (StopAtEnd) newS = Mathf.Min(newS, Path.Length);
            float newOffset = Offset;
            if (changingLane)
                newOffset = Mathf.MoveTowards(Offset, targetOffset, laneRate * dt);

            var pos = Path.PointAt(newS) + Path.RightAt(newS) * newOffset;
            var fwd = Path.TangentAt(newS);
            if (changingLane)
            {
                // Нос машины поворачивает в сторону перестроения
                float yaw = Mathf.Atan2(newOffset - Offset, Mathf.Max(step, laneRate * dt * 2f)) * Mathf.Rad2Deg;
                fwd = Quaternion.Euler(0f, Mathf.Clamp(yaw, -14f, 14f), 0f) * fwd;
            }

            // Страховка: никогда не въезжаем в игрока, пешехода или другую машину
            var current = Box;
            var newBox = new Obb(pos, fwd, Width, Length);
            if (Blocked(newBox, current))
            {
                // Может, мешает только поворот носа — пробуем сдвинуться, не поворачиваясь
                fwd = Path.TangentAt(newS);
                newBox = new Obb(pos, fwd, Width, Length);
                if (Blocked(newBox, current))
                {
                    // И без бокового сдвига — только вперёд
                    pos = Path.PointAt(newS) + Path.RightAt(newS) * Offset;
                    newOffset = Offset;
                    newBox = new Obb(pos, fwd, Width, Length);
                    if (Blocked(newBox, current))
                    {
                        Speed = 0f;
                        moving = false;
                        if (changingLane) laneBlockedTimer += dt;
                        return;
                    }
                    if (changingLane) laneBlockedTimer += dt;
                }
            }

            S = newS;
            Offset = newOffset;
            Place(pos, fwd);

            if (changingLane && Mathf.Abs(targetOffset - Offset) < 0.01f)
            {
                laneBlockedTimer = 0f;
                FinishLaneChange();
            }

            UpdateHonking(dt, blocker);
            UpdateOvertake(dt, blocker);
            UpdateRaceOvertake(dt, blocker);
            // Депутату мешают — «крякает» сиреной
            if (IsVip && vipLights != null && blocker != null && Speed < 1f) vipLights.Whoop();
        }

        float stuckBehindTimer;
        float laneBlockedTimer; // как долго перестроение упирается в соседей

        // Если перестроение 3 с упирается в соседей-NPC, машина «протискивается» (чуть касаясь их),
        // иначе застрявшая наполовину в очереди машина перекрывает её навсегда. В игрока не въезжаем никогда.
        bool Blocked(Obb newBox, Obb current) =>
            traffic.WouldHitPlayer(newBox, current) ||
            ((laneBlockedTimer < 3f || Role == NpcRole.Through) && traffic.WouldHitNpc(this, newBox, current));

        /// <summary>Едет мимо по среднему ряду, а там встал «второй ряд» — уходит в левый ряд и объезжает.</summary>
        void UpdateOvertake(float dt, Vehicle blocker)
        {
            // Объезд не удался (в левом ряду кто-то подъехал) — возвращаемся в свой ряд, а не протискиваемся
            if (Role == NpcRole.Through && Path == traffic.MiddlePath && pathAfterLaneChange == traffic.LeftPath && laneBlockedTimer > 2f)
            {
                targetOffset = 0f;
                pathAfterLaneChange = null;
                laneBlockedTimer = 0f;
                stuckBehindTimer = -3f; // и не сразу пробуем снова
                return;
            }
            if (Role != NpcRole.Through || Path != traffic.MiddlePath || pathAfterLaneChange != null) { stuckBehindTimer = 0f; return; }
            bool stuck = blocker != null && blocker.Speed < 0.3f && Speed < 0.3f && !blocker.IsPlayer;
            stuckBehindTimer = stuck ? stuckBehindTimer + dt : 0f;
            var left = traffic.LeftPath;
            if (stuckBehindTimer > 2.5f && traffic.LaneClearNear(left, S, 12f, this) &&
                traffic.AreaClear(left.PointAt(left.Project(Position, out _)), 9f, this))
            {
                StartLaneChange(CityLayout.LaneLeft - CityLayout.LaneMiddle, traffic.LeftPath);
                stuckBehindTimer = 0f;
            }
        }

        /// <summary>Гонщик упёрся в медленную машину — на прямой уходит в соседнюю полосу трассы.</summary>
        void UpdateRaceOvertake(float dt, Vehicle blocker)
        {
            if (Role != NpcRole.Racing || pathAfterLaneChange != null || profile == null) { raceStuck = 0f; return; }
            bool slowAhead = blocker != null && (blocker.Position - Position).sqrMagnitude < 35f * 35f &&
                             blocker.Speed < SpeedLimit() * 0.92f;
            raceStuck = slowAhead ? raceStuck + dt : 0f;
            if (raceStuck < 0.4f) return;
            var other = traffic.OtherRaceLane(Path);
            if (other == null || !profile.StraightAhead(S, 45f, traffic.Track.TopSpeed)) return;
            float os = other.Project(Position, out _);
            if (!traffic.LaneClearNear(other, os, 14f, this)) return;
            float side = other == traffic.RaceRight ? 1f : -1f;
            StartLaneChange(side * traffic.Track.LaneOffset * 2f, other);
            raceStuck = 0f;
        }

        void FinishLaneChange()
        {
            Offset = targetOffset;
            if (pathAfterLaneChange == null) return;
            Path = pathAfterLaneChange;
            pathAfterLaneChange = null;
            S = Path.Project(transform.position, out _);
            Offset = targetOffset = 0f;
            visual.blinker = 0;

            if (Role == NpcRole.Cutter)
            {
                bypassMinS = -1f;
                // Влез!
                Role = NpcRole.Queue;
                StopAtEnd = true;
                Say(CutInThanks[Random.Range(0, CutInThanks.Length)]);
                traffic.OnCutterJoined(this, CutFollower);
                CutFollower = null;
            }
            else if (Role == NpcRole.GivingUp)
            {
                Role = NpcRole.Through;
                StopAtEnd = false;
            }
        }

        void StartLaneChange(float offset, LanePath target)
        {
            targetOffset = offset;
            pathAfterLaneChange = target;
            laneBlockedTimer = 0f;
            visual.blinker = offset > 0 ? 1 : -1;
        }

        /// <summary>Сигналим игроку, если он мешает: стоит, хотя мог бы подъехать, или перегородил дорогу.</summary>
        void UpdateHonking(float dt, Vehicle blocker)
        {
            var player = traffic.Player;
            bool playerBlocks = false;

            if (Role == NpcRole.Queue && traffic.IsDirectlyBehindPlayer(this))
                playerBlocks = traffic.PlayerShouldMoveUp;
            else if (blocker != null && blocker.IsPlayer && Speed < 0.3f && Role != NpcRole.Queue)
            {
                blockedTimer += dt;
                playerBlocks = blockedTimer > 2f;
            }
            else blockedTimer = 0f;

            if (!playerBlocks || player == null)
            {
                impatience = Mathf.Min(impatience + dt, 0f);
                return;
            }

            impatience += dt;
            if (impatience > 3.5f)
            {
                Honk();
                var lines = Role == NpcRole.Queue ? HonkAtPlayer : BlockedRoad;
                Say(lines[Random.Range(0, lines.Length)]);
                GameManager.Instance.OnPlayerHonkedAt();
                impatience = -Random.Range(3f, 6f);
            }
        }

        public void Honk()
        {
            if (horn != null) horn.Play();
        }

        /// <summary>Реакция на бибиканье игрока.</summary>
        public void React(string phrase)
        {
            Say(phrase);
            if (Random.value < 0.3f) Invoke(nameof(Honk), 0.6f);
        }

        // ---------- Заправка ----------

        /// <summary>Депутат с мигалкой: по левому ряду и через выезд — к месту ожидания у магазина.</summary>
        /// <summary>Это депутатская машина (остаётся таковой и у колонки, и на выезде).</summary>
        public bool IsVip { get; private set; }

        /// <summary>Скорая или ДПС — стоят в общей очереди, как все.</summary>
        public ServiceKind Service;

        /// <summary>Ездит на газу: стоит в общей очереди только до въезда, потом сворачивает к пропану.</summary>
        public bool IsGas;

        static readonly string[] GasBranchLines =
        {
            "Пока, лохи! У меня газ!", "Удачи с бензином!", "Бензина нет? А газ есть!", "Газ — сила, бензин — могила!",
            "Всем привет, я на пропан!", "Учитесь, пока я жив!",
        };
        VipLights vipLights;

        public void SetupVip(LanePath path, float s)
        {
            IsVip = true;
            vipLights = GetComponent<VipLights>();
            Setup(NpcRole.Vip, path, s, true);
            moving = true;
            Speed = path.speedLimit * 0.8f;
        }


        public void GoToPump(Pump pump)
        {
            Pump = pump;
            pump.Occupy(this);
            Role = NpcRole.ToPump;
            Path = pump.enterPath;
            S = 0f;
            StopAtEnd = true;
            moving = false;
            reactTimer = 0f;
        }

        /// <summary>Сразу поставить машину на колонку (в начале игры там уже кто-то заправляется).</summary>
        public void PlaceAtPump(Pump pump, float progress)
        {
            Setup(NpcRole.ToPump, pump.enterPath, pump.enterPath.Length, true);
            Pump = pump;
            pump.Occupy(this);
            StartFueling(progress);
        }

        /// <summary>Доехала до конца маршрута и стоит (для машин со StopAtEnd).</summary>
        public bool Parked => StopAtEnd && !moving && S >= Path.Length - 0.3f;

        /// <summary>Стояла в конце маршрута — трогается по новому (в конце которого исчезнет, если не stopAtEnd).</summary>
        public void Continue(LanePath path, bool stopAtEnd)
        {
            traffic.AddPath(path);
            Path = path;
            S = 0f;
            Offset = targetOffset = 0f;
            StopAtEnd = stopAtEnd;
            moving = false;
            Speed = 0f;
        }

        /// <summary>Новая машина подъезжает к хвосту очереди уже на ходу.</summary>
        public void StartRolling(float speed)
        {
            moving = true;
            Speed = speed;
        }

        public void StartFueling(float progress = 0f)
        {
            Role = NpcRole.Fueling;
            serviceDuration = traffic.Settings.PumpServiceTime * Random.Range(0.75f, 1.3f);
            if (!IsVip && Random.value < traffic.Settings.slowDriverChance)
            {
                serviceDuration *= 1.6f;
                traffic.OnSlowDriver(this);
            }
            serviceTimer = serviceDuration * progress;
            Speed = 0f;
            moving = false;

            if (!IsVip)
            {
                // Сам идёт на кассу: заправка после оплаты короче, чтобы темп очереди остался прежним
                bool alreadyPaid = progress > 0.35f;
                customer = PumpCustomer.Spawn(this, traffic, alreadyPaid);
                if (customer != null && !alreadyPaid)
                {
                    serviceDuration *= 0.45f;
                    serviceTimer = 0f;
                }
            }
        }

        // ---------- Газ ----------

        void BranchToGas()
        {
            Role = NpcRole.ToGas;
            Path = traffic.GasInPath;
            S = Path.Project(transform.position, out _);
            Offset = targetOffset = 0f;
            StopAtEnd = true;
            Say(GasBranchLines[Random.Range(0, GasBranchLines.Length)]);
            if (Random.value < 0.5f) Honk();
            traffic.OnGasCarBranched(this);
        }

        void StartGasFueling()
        {
            Role = NpcRole.GasFueling;
            serviceDuration = traffic.Settings.PumpServiceTime * Random.Range(0.25f, 0.4f); // газ льют быстро, без кассы
            serviceTimer = 0f;
            Speed = 0f;
            moving = false;
        }

        void StartGasExit()
        {
            Role = NpcRole.Exiting;
            Path = traffic.GasOutPath;
            S = 0f;
            StopAtEnd = false;
            moving = false;
            reactTimer = 0f;
            reactionDelay = Random.Range(0.3f, 1f);
        }

        void StartExit()
        {
            Role = NpcRole.Exiting;
            Path = IsVip ? traffic.VipOutPath : Pump.exitPath;
            S = 0f;
            StopAtEnd = false;
            moving = false;
            reactTimer = 0f;
            reactionDelay = Random.Range(0.3f, 1f);
        }

        // ---------- Удар от игрока ----------

        public void OnHitByPlayer(float impactSpeed)
        {
            Honk();
            Say(CursesAfterCrash[Random.Range(0, CursesAfterCrash.Length)]);
            bool parked = Role == NpcRole.Queue || Role == NpcRole.Fueling || Role == NpcRole.ToPump || Role == NpcRole.GasFueling;
            if (impactSpeed > 1.8f && parked)
            {
                // Водитель выходит разбираться
                Brawler.Spawn(this, traffic, BrawlReason.Crash);
            }
            else if (Role == NpcRole.Through)
            {
                holdTimer = 3f;
            }
        }

        // ---------- Вклинивание из соседнего ряда ----------

        /// <summary>Едет к съезду на заправку и встаёт «вторым рядом» (иначе — ищет дырку прямо в середине очереди).</summary>
        public bool AimsAtEntrance { get; private set; }

        static readonly string[] BypassLines = { "Ну и стой тут со своим шашлыком!", "Водитель ушёл есть, а мы стоим?! Объезжаю.", "Кто за рулём?! Никого? Ну и ладно." };

        /// <summary>
        /// Машина впереди стоит без водителя (ушёл за шашлыком), а очередь перед ней уехала:
        /// уходим влево во второй ряд и встаём обратно в очередь перед ней (обычная логика наглеца).
        /// </summary>
        /// <summary>Объезжающий встаёт в очередь только впереди этой точки (перед брошенной машиной).</summary>
        float bypassMinS = -1f;

        public void BypassStalled(float aheadOfS)
        {
            bypassMinS = aheadOfS;
            Role = NpcRole.Cutter;
            StopAtEnd = false;
            Cut = CutState.Looking;
            AimsAtEntrance = false;
            cutTimer = 0f;
            cutCooldown = 0f;
            SlideOnto(traffic.MiddlePath);
            visual.blinker = -1;
            Say(BypassLines[Random.Range(0, BypassLines.Length)]);
            Honk();
        }

        public void SetupCutter(LanePath lane, float s, bool aimEntrance)
        {
            Setup(NpcRole.Cutter, lane, s, false);
            Cut = CutState.Looking;
            AimsAtEntrance = aimEntrance;
            cutTimer = 0f;
        }

        // Наглецы у съезда лезут в дырку чуть длиннее своей машины, остальные — в дырку побольше
        float GapNeeded => Length + (Cut == CutState.Waiting || AimsAtEntrance ? 1.8f : 2.4f);

        void UpdateCutter(float dt)
        {
            cutTimer += dt;
            switch (Cut)
            {
                case CutState.Looking:
                {
                    float minCenter = AimsAtEntrance ? traffic.SecondRowWaitS - 30f : 0f;
                    if (bypassMinS > 0f) minCenter = Mathf.Max(minCenter, bypassMinS);
                    if (cutCooldown <= 0f && traffic.FindQueueGap(this, GapNeeded, minCenter, out var follower, out float gapCenterS))
                    {
                        CutFollower = follower;
                        cutTargetS = gapCenterS;
                        CutPolite = !follower.IsPlayer && traffic.RollPoliteness();
                        Cut = CutState.Aligning;
                        cutTimer = 0f;
                    }
                    else if (S > traffic.SecondRowWaitS)
                    {
                        // Доехал до въезда — встаём «вторым рядом» и ждём, когда пустят
                        Cut = CutState.Waiting;
                        cutTargetS = traffic.SecondRowWaitS;
                        cutTimer = 0f;
                    }
                    break;
                }
                case CutState.Waiting:
                {
                    if (cutTimer > 2f && traffic.FindQueueGap(this, GapNeeded, 0f, out var follower, out float gapCenterS) && gapCenterS < S + 12f)
                    {
                        CutFollower = follower;
                        cutTargetS = gapCenterS;
                        CutPolite = !follower.IsPlayer && traffic.RollPoliteness();
                        Cut = CutState.Aligning;
                        cutTimer = 0f;
                    }
                    else if (IsRacer && cutTimer > 1.5f && traffic.QueueLaneFreeBeside(this, cutTimer > 10f)) StartTailMerge();
                    // Гонщик застрял наполовину в ряду очереди и не может вернуться — дожимает и встаёт в очередь
                    else if (IsRacer && Mathf.Abs(Offset) > 1f && laneBlockedTimer > 3f)
                    {
                        float keep = laneBlockedTimer; // продолжаем «протискиваться» (касаясь соседей)
                        JoinQueueFromRace(traffic.QueuePath);
                        SlideOnto(traffic.QueuePath);
                        laneBlockedTimer = keep;
                    }
                    // Долго не пускают у въезда — плюёт на очередь и едет прямо к колонкам
                    else if (IsRacer && cutTimer > 25f)
                    {
                        RushToPumps(traffic.RushPath);
                        SlideOnto(traffic.RushPath);
                    }
                    else if (cutTimer > 45f) GiveUpCutting();
                    break;
                }
                case CutState.Aligning:
                {
                    if (!traffic.GapStillOpen(this, CutFollower, GapNeeded - 1.4f, out cutTargetS)) { AbortCut(); break; }
                    if (Mathf.Abs(S - cutTargetS) < 1.2f)
                    {
                        Cut = CutState.Signaling;
                        visual.blinker = 1;
                        cutTimer = 0f;
                    }
                    else if (cutTimer > 15f || cutTargetS < S - 3f) AbortCut();
                    break;
                }
                case CutState.Signaling:
                {
                    if (!traffic.GapStillOpen(this, CutFollower, GapNeeded - 1.4f, out cutTargetS)) { AbortCut(); break; }
                    if (cutTimer > 1.0f)
                    {
                        Cut = CutState.Merging;
                        StartLaneChange(CityLayout.LaneQueue - CityLayout.LaneMiddle, traffic.QueuePath);
                    }
                    break;
                }
                case CutState.Merging:
                {
                    // Если сосед успел поджать, а мы ещё не влезли наполовину — отказываемся.
                    // Если боком упёрлись в соседей — тоже отказываемся и возвращаемся в свой ряд.
                    if (tailMerge)
                    {
                        if (laneBlockedTimer > 2.5f) AbortCut();
                    }
                    else if ((Offset < 2f && !traffic.GapStillOpen(this, CutFollower, Length + 0.3f, out cutTargetS)) || laneBlockedTimer > 2.5f)
                        AbortCut();
                    break;
                }
            }
        }

        /// <summary>Гонщик во втором ряду: рядом в очереди пусто (хвост впереди) — просто перестраивается в хвост.</summary>
        void StartTailMerge()
        {
            tailMerge = true;
            Cut = CutState.Merging;
            CutFollower = null;
            cutTargetS = S + 6f;
            // Насколько сдвинуться вбок, чтобы оказаться на маршруте очереди (у въезда он уходит вправо)
            var queue = traffic.QueuePath;
            var target = queue.PointAt(queue.Project(Position, out _));
            float offset = Mathf.Clamp(Vector3.Dot(target - Path.PointAt(S), Path.RightAt(S)), 2.5f, 7f);
            StartLaneChange(offset, queue);
        }

        bool tailMerge;

        void AbortCut()
        {
            tailMerge = false;
            bool byPlayer = CutFollower != null && CutFollower.IsPlayer;
            if (Cut == CutState.Merging || Cut == CutState.Signaling)
            {
                Say(CutInFail[Random.Range(0, CutInFail.Length)]);
                if (byPlayer) GameManager.Instance.OnCutInBlocked();
            }
            targetOffset = 0f;
            pathAfterLaneChange = null;
            laneBlockedTimer = 0f;
            visual.blinker = 0;
            CutFollower = null;
            // Наглец у съезда не сдаётся — снова встаёт вторым рядом и ждёт
            Cut = AimsAtEntrance && S > traffic.SecondRowWaitS - 30f ? CutState.Waiting : CutState.Looking;
            if (Cut == CutState.Waiting) cutTargetS = Mathf.Max(S, traffic.SecondRowWaitS - 15f);
            cutCooldown = Random.Range(3f, 7f);
        }

        void GiveUpCutting()
        {
            if (IsRacer)
            {
                // Гонщик не сдаётся: без бензина до финиша всё равно не доехать
                Say("Пустите, у меня гонка!");
                Honk();
                cutTimer = 0f;
                return;
            }
            Say("Да ну вас!");
            Role = NpcRole.Through;
            visual.blinker = 0;
            CutFollower = null;
        }

        // ---------- Сдаться и уехать ----------

        public void GiveUp(string phrase)
        {
            Role = NpcRole.GivingUp;
            StopAtEnd = false;
            giveUpTimer = 0f;
            visual.blinker = -1;
            Say(phrase);
        }

        void UpdateGivingUp(float dt)
        {
            giveUpTimer += dt;
            // Отказываемся раньше, чем включится «протиснуться» (3 с), пока ещё больше чем наполовину в своём ряду
            if (pathAfterLaneChange != null && laneBlockedTimer > (Mathf.Abs(Offset) < 2f ? 2.5f : 4f))
            {
                // Боком не протиснуться — передумал, возвращается на место в очереди
                targetOffset = 0f;
                pathAfterLaneChange = null;
                laneBlockedTimer = 0f;
                Role = NpcRole.Queue;
                StopAtEnd = true;
                visual.blinker = 0;
                return;
            }
            if (pathAfterLaneChange != null || giveUpTimer < 2f) return;
            // Ждём, пока в соседнем ряду будет свободно
            if (traffic.LaneClearNear(traffic.MiddlePath, S, 10f, this))
            {
                StartLaneChange(CityLayout.LaneMiddle - CityLayout.LaneQueue, traffic.MiddlePath);
                return;
            }
            // Так и не смог перестроиться — передумал и остался в очереди
            if (giveUpTimer > 15f)
            {
                Role = NpcRole.Queue;
                StopAtEnd = true;
                visual.blinker = 0;
            }
        }

        public void Hold(float seconds) => holdTimer = Mathf.Max(holdTimer, seconds);
    }
}
