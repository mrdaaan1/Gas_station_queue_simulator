using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Правила режима «Самая быстрая гонка»: светофор на старте, лампочка бензина на последнем повороте,
    /// места по ходу гонки (с учётом очереди на заправку), финиш и сход, если бензин кончился до финиша.
    /// Сама очередь, касса и заправка — те же, что в обычной игре (<see cref="GameManager"/>).
    /// </summary>
    public class RaceManager : MonoBehaviour
    {
        public static RaceManager Instance { get; private set; }

        const float CountdownTime = 4f;

        /// <summary>Секунд до зелёного (≤ 0 — гонка идёт).</summary>
        public float Countdown { get; private set; } = CountdownTime;
        public bool Started { get; private set; }
        public float RaceTime { get; private set; }
        /// <summary>Сколько игрок простоял в очереди и у колонки (реальные секунды).</summary>
        public float QueueTime { get; private set; }
        public int Place { get; private set; } = TrafficManager.PlayerGridSlot + 1;
        public int Total => TrafficManager.GridSize;
        public bool PlayerFinished { get; private set; }
        /// <summary>Бензин кончился после заправки, так и не доехав, — сход.</summary>
        public bool RanDry { get; private set; }
        public float DryMetersToFinish { get; private set; }
        public bool FuelSignal { get; private set; }

        /// <summary>На сколько метров хватит бензина в гонке.</summary>
        public float RangeMeters => player.FuelLiters / Mathf.Max(0.01f, gm.Settings.raceLitersPer100Km / 100f) * 1000f;

        /// <summary>Сколько ехать до въезда на заправку (по трассе и по главной дороге).</summary>
        public float StationDistance
        {
            get
            {
                var p = player.Position;
                float road = CityLayout.EntranceMinZ - RaceLayout.JoinZ;
                if (p.z < RaceLayout.JoinZ - 1f) return center.Length - center.Project(p, out _) + road;
                return Mathf.Max(0f, CityLayout.EntranceMinZ - p.z);
            }
        }

        /// <summary>Нужно ли сейчас кричать «заправься»: лампочка горит, бак не залит, в очереди и у колонки не стоим.</summary>
        public bool NeedsFuel => FuelSignal && !gm.PlayerFueled && !traffic.PlayerInQueue && !traffic.PlayerIsHead && !traffic.PlayerWaitingWithoutQueue && traffic.PlayerPump == null && !gm.Paid;

        bool warnedLow, warnedCritical;
        TrafficManager traffic;
        PlayerCar player;
        GameManager gm;
        Renderer[] lights;
        AudioSource beeper;
        LanePath center;
        float signalS;
        int litLights = -1;
        float placeTimer;
        bool arrivedAnnounced, firstFinishAnnounced;

        public void Init(TrafficManager traffic, PlayerCar player, GameManager gm, RaceTrackBuilder.Result track)
        {
            Instance = this;
            this.traffic = traffic;
            this.player = player;
            this.gm = gm;
            lights = track != null ? track.lights : null;
            center = new LanePath("RaceCenter", 1f, RaceLayout.Center(), false);
            signalS = center.Project(RaceLayout.Corners[RaceLayout.FuelSignalCorner], out _) - 25f;
            player.controlsEnabled = false;
            player.raceFuel = true;
            beeper = SoundFactory.Source3D(gameObject, 0.7f);
            beeper.spatialBlend = 0f;
            traffic.RacerFinished += OnRacerFinished;
            SetLights(0, false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (traffic != null) traffic.RacerFinished -= OnRacerFinished;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || gm == null || gm.State == GameState.Finished) return;

            if (!Started)
            {
                UpdateCountdown(dt);
                return;
            }

            RaceTime += dt;
            bool queueing = !gm.PlayerFueled && (traffic.PlayerInQueue || traffic.PlayerPump != null || gm.OnFoot);
            if (queueing && player.Position.z > RaceLayout.JoinZ) QueueTime += dt;

            UpdateFuelSignal();
            UpdateFuelWarnings();
            placeTimer -= dt;
            if (placeTimer <= 0f)
            {
                placeTimer = 0.25f;
                Place = ComputePlace();
            }

            if (!arrivedAnnounced && traffic.PlayerInQueue && player.Position.z > RaceLayout.JoinZ)
            {
                arrivedAnnounced = true;
                gm.ShowMessage("Вы в очереди на заправку. Гонка гонкой, а без бензина до финиша не доехать.", 8f);
            }

            if (!PlayerFinished && !gm.OnFoot && player.Position.z > RaceLayout.FinishZ && player.Position.x < CityLayout.LotMinX)
            {
                PlayerFinished = true;
                traffic.FinishOrder.Add("Вы");
                Place = traffic.FinishOrder.Count;
                gm.FinishRace();
            }
        }

        void UpdateCountdown(float dt)
        {
            Countdown -= dt;
            // Три красных по очереди, потом все зелёные
            int lit = Countdown > 3f ? 0 : Countdown > 2f ? 1 : Countdown > 1f ? 2 : Countdown > 0f ? 3 : 4;
            if (lit != litLights)
            {
                litLights = lit;
                if (lit >= 1 && lit <= 3) beeper.PlayOneShot(SoundFactory.Beep(false));
                if (lit == 4) beeper.PlayOneShot(SoundFactory.Beep(true));
                SetLights(Mathf.Min(lit, 3), lit == 4);
            }
            if (Countdown > 0f) return;

            Started = true;
            traffic.StartRace();
            if (gm.Radio != null) gm.Radio.TuneRace();
            if (!gm.OnFoot) player.controlsEnabled = true;
            gm.ShowMessage("ПОЕХАЛИ! Пять поворотов — и финиш. Наверное.", 5f);
        }

        void SetLights(int red, bool green)
        {
            if (lights == null) return;
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null) continue;
                var c = green ? RaceTrackBuilder.LightGreen : i < red ? RaceTrackBuilder.LightRed : RaceTrackBuilder.LightOff;
                lights[i].sharedMaterial = Shapes.Mat(c);
            }
        }

        /// <summary>Последний поворот: у всех загорается лампочка бензина. Доехать хватит только до заправки.</summary>
        void UpdateFuelSignal()
        {
            if (FuelSignal || player.Position.z > RaceLayout.JoinZ) return;
            if (center.Project(player.Position, out float lat) < signalS || Mathf.Abs(lat) > 12f) return;
            FuelSignal = true;
            // Хватит до въезда на заправку и ещё метров на двести — но не до финиша
            float toEntrance = Vector3.Distance(player.Position, new Vector3(7f, 0f, RaceLayout.JoinZ)) + (CityLayout.EntranceMinZ - RaceLayout.JoinZ);
            float liters = (toEntrance + 220f) / 1000f * gm.Settings.raceLitersPer100Km / 100f;
            if (player.FuelLiters > liters) player.SetFuelLiters(liters);
            gm.ShowMessage("Загорелась лампочка бензина! У соперников тоже. Все — на заправку «" + CityBuilder.Brand + "», она прямо по курсу.", 10f);
            gm.ShowMessage("Без заправки до финиша не доехать: бензин кончится раньше.", 10f);
        }

        /// <summary>Едет мимо или тянет — напоминаем всё настойчивее.</summary>
        void UpdateFuelWarnings()
        {
            if (!NeedsFuel) return;
            float range = RangeMeters;
            if (!warnedLow && range < StationDistance + 80f)
            {
                warnedLow = true;
                gm.ShowMessage("Бензин на исходе! Сворачивайте на заправку и вставайте в очередь — дальше улица перекрыта.", 9f);
            }
            if (!warnedCritical && range < 120f)
            {
                warnedCritical = true;
                gm.ShowMessage($"Бензина метров на {Mathf.RoundToInt(range)}! Ещё чуть-чуть — и мотор заглохнет.", 8f);
            }
        }

        void OnRacerFinished(NpcCar racer)
        {
            if (PlayerFinished || gm == null) return;
            int n = traffic.FinishOrder.Count;
            if (!firstFinishAnnounced)
            {
                firstFinishAnnounced = true;
                gm.ShowMessage(gm.PlayerFueled
                    ? $"{racer.RacerName} уже на финише! Догоняйте!"
                    : $"{racer.RacerName} уже финишировал(а). А вы всё ещё на заправке.", 8f);
            }
            else gm.ShowMessage($"{racer.RacerName} финишировал(а) {n}-м.", 5f);
        }

        /// <summary>Бензин кончился на пути к финишу без заправки — сход.</summary>
        public bool OnPlayerRanDry()
        {
            if (gm.PlayerFueled || player.Position.z < CityLayout.EntranceMinZ) return false;
            RanDry = true;
            DryMetersToFinish = Mathf.Max(0f, RaceLayout.FinishZ - player.Position.z);
            gm.ShowMessage($"Мотор чихнул и заглох. До финиша {Mathf.RoundToInt(DryMetersToFinish)} м...", 8f);
            gm.FinishRace();
            return true;
        }

        // ---------- Места ----------

        int ComputePlace()
        {
            float mine = Progress(player.Position, gm.PlayerFueled || gm.State == GameState.DrivingAway, traffic.PlayerPump != null);
            int place = 1 + traffic.FinishOrder.Count;
            foreach (var r in traffic.Racers)
            {
                if (r == null || r.RaceFinished) continue;
                bool fueled = r.Role == NpcRole.Exiting;
                bool atPump = r.Role == NpcRole.ToPump || r.Role == NpcRole.Fueling;
                if (Progress(r.Position, fueled, atPump) > mine) place++;
            }
            return Mathf.Min(place, Total);
        }

        /// <summary>Сколько пройдено: по оси трассы, потом по главной дороге; заправленные — впереди стоящих в очереди.</summary>
        float Progress(Vector3 p, bool fueled, bool atPump)
        {
            if (fueled) return 20000f + p.z;
            if (atPump) return 10000f + p.z;
            if (p.z < RaceLayout.JoinZ - 1f) return center.Project(p, out _);
            return center.Length + (p.z - RaceLayout.JoinZ) + (p.x > CityLayout.LotMinX ? 30f : 0f);
        }

        public static string FormatTime(float seconds)
        {
            int m = (int)(seconds / 60f);
            return $"{m}:{seconds - m * 60f:00.0}";
        }
    }
}
