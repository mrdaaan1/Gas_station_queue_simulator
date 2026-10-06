using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Правила гонки: светофор на старте, места по ходу гонки, финиш, время.
    /// «Самая быстрая гонка»: лампочка бензина на последнем повороте, очередь на заправку (та же, что в обычной игре,
    /// см. <see cref="GameManager"/>), сход, если бензин кончился до финиша.
    /// Уличная гонка по Тольятти: без заправки, финиш — линия в кармане у «Мадагаскара», подсказки по маршруту.
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
        public bool NeedsFuel => track.FuelStop && FuelSignal && !gm.PlayerFueled && !traffic.PlayerInQueue && !traffic.PlayerIsHead && !traffic.PlayerWaitingWithoutQueue && traffic.PlayerPump == null && !gm.Paid;

        bool warnedLow, warnedCritical;
        RaceTrack track;
        /// <summary>Уличная гонка (Тольятти): сколько игрок проехал по оси трассы.</summary>
        float playerS;
        Vector3 lastPlayerPos;
        public RaceTrack Track => track;

        /// <summary>Подсказка по маршруту («через 300 м: налево на 70 лет Октября») или null.</summary>
        public string Hint
        {
            get
            {
                if (track.FuelStop) return null;
                string text = track.HintAt(playerS, out float m);
                if (text == null) return null;
                return m < 25f ? text : $"Через {Mathf.RoundToInt(m / 10f) * 10} м: {text}";
            }
        }
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

        public void Init(TrafficManager traffic, PlayerCar player, GameManager gm, RaceTrackBuilder.Result built, RaceTrack raceTrack)
        {
            Instance = this;
            this.traffic = traffic;
            this.player = player;
            this.gm = gm;
            track = raceTrack;
            lights = built != null ? built.lights : null;
            center = track.CenterPath;
            if (track.FuelStop) signalS = center.Project(RaceLayout.Corners[RaceLayout.FuelSignalCorner], out _) - 25f;
            playerS = center.Project(player.Position, out _);
            lastPlayerPos = player.Position;
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
            if (!track.FuelStop)
            {
                UpdateStreetRace();
                return;
            }
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
            if (track.FuelStop) gm.ShowMessage("ПОЕХАЛИ! Пять поворотов — и финиш. Наверное.", 5f);
            else
            {
                gm.ShowMessage("ПОЕХАЛИ! Обводное → Офицерская → налево на 70 лет Октября.", 6f);
                gm.ShowMessage("Два кольца, на втором — налево на Льва Яшина. Финиш — в кармане у ТЦ «Мадагаскар».", 8f);
            }
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
                gm.ShowMessage(gm.PlayerFueled || !track.FuelStop
                    ? $"{racer.RacerName} уже на финише! Догоняйте!"
                    : $"{racer.RacerName} уже финишировал(а). А вы всё ещё на заправке.", 8f);
            }
            else gm.ShowMessage($"{racer.RacerName} финишировал(а) {n}-м.", 5f);
        }

        // ---------- Уличная гонка ----------

        void UpdateStreetRace()
        {
            var pos = player.Position;
            // Ищем место на оси рядом с прошлым: трасса местами проходит близко к себе
            playerS = center.ProjectNear(pos, playerS - 60f, playerS + 120f, out float lat);
            if (Mathf.Abs(lat) > 40f) playerS = center.Project(pos, out _); // срезал через дворы — ищем заново
            placeTimer -= Time.deltaTime;
            if (placeTimer <= 0f)
            {
                placeTimer = 0.25f;
                Place = ComputePlace();
            }
            if (!PlayerFinished && !gm.OnFoot && track.CrossedFinish(lastPlayerPos, pos))
            {
                PlayerFinished = true;
                traffic.FinishOrder.Add("Вы");
                Place = traffic.FinishOrder.Count;
                gm.ShowMessage(Place == 1 ? "ФИНИШ! Первым у «Мадагаскара»!" : $"ФИНИШ! {Place}-е место.", 8f);
                gm.FinishRace();
            }
            lastPlayerPos = pos;
        }

        /// <summary>Бензин кончился на пути к финишу без заправки — сход.</summary>
        public bool OnPlayerRanDry()
        {
            if (!track.FuelStop)
            {
                RanDry = true;
                DryMetersToFinish = Mathf.Max(0f, track.FinishS - playerS);
                gm.ShowMessage("Бензин кончился посреди Тольятти. Сход.", 8f);
                gm.FinishRace();
                return true;
            }
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
            if (!track.FuelStop)
            {
                // Уличная гонка: кто дальше по оси трассы (полосы чуть разной длины — пересчитываем)
                int ahead = 1 + traffic.FinishOrder.Count;
                if (PlayerFinished) return traffic.FinishOrder.IndexOf("Вы") + 1;
                foreach (var r in traffic.Racers)
                {
                    if (r == null || r.RaceFinished) continue;
                    float rs = r.S * center.Length / Mathf.Max(1f, r.Path.Length);
                    if (rs > playerS) ahead++;
                }
                return Mathf.Min(ahead, Total);
            }
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
