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

        public override bool IsPlayer => false;

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
            base.Awake();
            reactionDelay = Random.Range(0.4f, 1.4f);
            horn = SoundFactory.Source3D(gameObject, 0.8f);
            horn.clip = SoundFactory.Horn;
            horn.pitch = Random.Range(0.8f, 1.15f);
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
            switch (Role)
            {
                case NpcRole.Cutter: return Cut == CutState.Looking ? 6f : 4f;
                case NpcRole.GivingUp: return Offset > -3f ? 3f : 10f;
                case NpcRole.Vip: return S > Path.Length - 45f ? 5f : Path.speedLimit; // по территории — потише
                default: return Path.speedLimit;
            }
        }

        float DesiredGap => Role == NpcRole.Through ? 5f : 2f;

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (cutCooldown > 0f) cutCooldown -= dt;

            if (holdTimer > 0f)
            {
                holdTimer -= dt;
                Speed = 0f;
                moving = false;
                return;
            }

            if (Role == NpcRole.Fueling)
            {
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
            if (atEnd && !moving)
            {
                if (Role == NpcRole.ToPump) StartFueling();
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

            // Невежливый сосед, перед которым кто-то моргает поворотником, спешит поджать дырку
            if (!moving && traffic.IsCutterTarget(this)) reactionDelay = Mathf.Min(reactionDelay, 0.15f);

            if (!moving)
            {
                float startThreshold = limitedByStop ? ArriveTolerance : Role == NpcRole.Through ? 0.5f : 1.2f;
                if (free > startThreshold || (changingLane && free > 0.6f))
                {
                    reactTimer += dt;
                    if (reactTimer >= reactionDelay)
                    {
                        moving = true;
                        reactTimer = 0f;
                        reactionDelay = Random.Range(0.4f, 1.4f);
                    }
                }
                else reactTimer = 0f;
            }

            float step = 0f;
            if (moving)
            {
                float stopDistance = Speed * Speed / (2f * Decel);
                Speed = free <= stopDistance + 0.05f
                    ? Mathf.Max(0f, Speed - Decel * dt)
                    : Mathf.Min(limit, Speed + Accel * dt);
                if (Speed > limit) Speed = Mathf.Max(limit, Speed - Decel * dt);

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
                newOffset = Mathf.MoveTowards(Offset, targetOffset, LaneChangeRate * dt);

            var pos = Path.PointAt(newS) + Path.RightAt(newS) * newOffset;
            var fwd = Path.TangentAt(newS);
            if (changingLane)
            {
                // Нос машины поворачивает в сторону перестроения
                float yaw = Mathf.Atan2(newOffset - Offset, Mathf.Max(step, LaneChangeRate * dt * 2f)) * Mathf.Rad2Deg;
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
            // Депутату мешают — «крякает» сиреной
            if (IsVip && vipLights != null && blocker != null && Speed < 1f) vipLights.Whoop();
        }

        float stuckBehindTimer;
        float laneBlockedTimer; // как долго перестроение упирается в соседей

        // Если перестроение 3 с упирается в соседей-NPC, машина «протискивается» (чуть касаясь их),
        // иначе застрявшая наполовину в очереди машина перекрывает её навсегда. В игрока не въезжаем никогда.
        bool Blocked(Obb newBox, Obb current) =>
            traffic.WouldHitPlayer(newBox, current) ||
            (laneBlockedTimer < 3f && traffic.WouldHitNpc(this, newBox, current));

        /// <summary>Едет мимо по среднему ряду, а там встал «второй ряд» — уходит в левый ряд и объезжает.</summary>
        void UpdateOvertake(float dt, Vehicle blocker)
        {
            if (Role != NpcRole.Through || Path != traffic.MiddlePath || pathAfterLaneChange != null) { stuckBehindTimer = 0f; return; }
            bool stuck = blocker != null && blocker.Speed < 0.3f && Speed < 0.3f && !blocker.IsPlayer;
            stuckBehindTimer = stuck ? stuckBehindTimer + dt : 0f;
            if (stuckBehindTimer > 2.5f && traffic.LaneClearNear(traffic.LeftPath, S, 12f, this))
            {
                StartLaneChange(CityLayout.LaneLeft - CityLayout.LaneMiddle, traffic.LeftPath);
                stuckBehindTimer = 0f;
            }
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
        VipLights vipLights;

        public void SetupVip(LanePath path)
        {
            IsVip = true;
            vipLights = GetComponent<VipLights>();
            Setup(NpcRole.Vip, path, 0f, true);
            moving = true;
            Speed = path.speedLimit * 0.8f;
        }

        /// <summary>Депутату дали колонку — он заезжает с другой стороны, не дожидаясь очереди.</summary>
        public void GoToPumpAsVip(Pump pump)
        {
            Pump = pump;
            pump.Occupy(this);
            Role = NpcRole.ToPump;
            Path = pump.vipPath;
            S = 0f;
            StopAtEnd = true;
            moving = false;
            reactTimer = 0f;
        }

        public bool IsVipWaiting => Role == NpcRole.Vip && S >= Path.Length - ArriveTolerance && !moving;

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
            if (Random.value < traffic.Settings.slowDriverChance)
            {
                serviceDuration *= 1.6f;
                traffic.OnSlowDriver(this);
            }
            serviceTimer = serviceDuration * progress;
            Speed = 0f;
            moving = false;
        }

        void StartExit()
        {
            Role = NpcRole.Exiting;
            Path = Pump.exitPath;
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
            bool parked = Role == NpcRole.Queue || Role == NpcRole.Fueling || Role == NpcRole.ToPump;
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
                    if ((Offset < 2f && !traffic.GapStillOpen(this, CutFollower, Length + 0.3f, out cutTargetS)) || laneBlockedTimer > 2.5f)
                        AbortCut();
                    break;
                }
            }
        }

        void AbortCut()
        {
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
            if (pathAfterLaneChange != null && laneBlockedTimer > 4f)
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
