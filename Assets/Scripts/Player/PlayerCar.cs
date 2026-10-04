using UnityEngine;

namespace GasQueue
{
    public enum EngineState { Off, Starting, Running }

    /// <summary>
    /// Машина игрока. Игрок сам жмёт газ (W) и тормоз (S), чтобы подъехать в очереди,
    /// глушит и заводит мотор (I). Мотор на холостых жжёт бензин, которого и так почти нет.
    /// Пока без руля: машина едет только вперёд по своей полосе (руль — следующий этап).
    /// </summary>
    public class PlayerCar : QueueCar
    {
        const float Accel = 4f;
        const float BrakeDecel = 10f;
        const float CoastDecel = 1.5f;
        const float MaxSpeed = 9f;
        const float StartDuration = 1.1f;

        static readonly Color LampOff = Shapes.Hex("#3a2a10");
        static readonly Color LampFuel = Shapes.Hex("#ffb000");
        static readonly Color LampEngine = Shapes.Hex("#ff3020");

        /// <summary>Уровень топлива от 0 до 1.</summary>
        [Range(0f, 1f)] public float fuel = 0.12f;

        /// <summary>Можно ли сейчас управлять (во время заправки и на финальном экране — нельзя).</summary>
        public bool controlsEnabled = true;

        public override bool IsPlayer => true;

        public EngineState Engine { get; private set; } = EngineState.Running;
        public float FuelLiters => fuel * settings.tankLiters;
        public float SpeedKmh => Speed * 3.6f;

        GameSettings settings;
        AudioSource horn;
        AudioSource engine;
        AudioSource starter;
        float startTimer;
        float steerWobble;
        float lampBlink;
        bool warnedEngineOff;

        public void Init(CarVisual v, GameSettings s)
        {
            visual = v;
            settings = s;
            fuel = Mathf.Clamp01(s.startFuelLiters / s.tankLiters);

            horn = SoundFactory.Source3D(gameObject, 1f);
            horn.spatialBlend = 0.3f;
            horn.clip = SoundFactory.Horn;

            starter = SoundFactory.Source3D(gameObject, 0.6f);
            starter.spatialBlend = 0f;
            starter.clip = SoundFactory.Starter;

            engine = SoundFactory.Source3D(gameObject, 0.18f);
            engine.spatialBlend = 0f;
            engine.clip = SoundFactory.Engine;
            engine.loop = true;
            engine.Play();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // пауза

            if (controlsEnabled && GameInput.IgnitionPressed) ToggleEngine();
            UpdateEngine(dt);

            bool running = Engine == EngineState.Running;
            bool gas = controlsEnabled && GameInput.Gas;
            bool brake = controlsEnabled && GameInput.Brake;

            if (gas && !running && !warnedEngineOff)
            {
                warnedEngineOff = true;
                GameManager.Instance.ShowMessage("Двигатель заглушен. Нажмите I, чтобы завести.");
            }
            if (!gas) warnedEngineOff = false;

            if (gas && running) Speed = Mathf.Min(MaxSpeed, Speed + Accel * dt);
            else if (brake) Speed = Mathf.Max(0f, Speed - BrakeDecel * dt);
            else Speed = Mathf.Max(0f, Speed - CoastDecel * dt);

            float z = Z + Speed * dt;
            float limit = queue.HardLimitZ(this);
            if (z > limit)
            {
                z = Mathf.Max(Z, limit);
                if (Speed > 2.5f) GameManager.Instance.OnPlayerBump();
                Speed = 0f;
            }
            BurnFuel(dt, z - Z);
            MoveTo(z);

            if (controlsEnabled && GameInput.HornPressed)
            {
                horn.Play();
                GameManager.Instance.OnPlayerHonk();
            }

            UpdateDashboard(dt);
            engine.pitch = 0.8f + Speed / MaxSpeed * 0.9f + (gas && running ? 0.15f : 0f);
        }

        void ToggleEngine()
        {
            if (Engine == EngineState.Off)
            {
                Engine = EngineState.Starting;
                startTimer = 0f;
                starter.Play();
            }
            else if (Engine == EngineState.Running)
            {
                StopEngine();
                GameManager.Instance.OnEngineToggled(false);
            }
        }

        void StopEngine()
        {
            Engine = EngineState.Off;
            starter.Stop();
        }

        void UpdateEngine(float dt)
        {
            if (Engine == EngineState.Starting)
            {
                startTimer += dt;
                if (startTimer >= StartDuration)
                {
                    if (fuel > 0f)
                    {
                        Engine = EngineState.Running;
                        GameManager.Instance.OnEngineToggled(true);
                    }
                    else
                    {
                        Engine = EngineState.Off;
                        GameManager.Instance.ShowMessage("Чих-пых... Не заводится: бак пустой.");
                    }
                }
            }

            // Мотор работает — гул слышен, заглушили — тишина
            float targetVolume = Engine == EngineState.Running ? 0.18f : 0f;
            engine.volume = Mathf.MoveTowards(engine.volume, targetVolume, dt * 0.4f);
        }

        void BurnFuel(float dt, float distance)
        {
            if (Engine != EngineState.Running) return;
            float gameHours = dt * GameManager.Instance.ClockRate / 3600f;
            float liters = settings.idleLitersPerHour * gameHours
                           + Mathf.Abs(distance) / 1000f * settings.drivingLitersPer100Km / 100f;
            fuel = Mathf.Max(0f, fuel - liters / settings.tankLiters);
            if (fuel <= 0f)
            {
                StopEngine();
                GameManager.Instance.OnPlayerRanDry();
            }
        }

        public void AddFuelLiters(float liters) => fuel = Mathf.Clamp01(fuel + liters / settings.tankLiters);

        void UpdateDashboard(float dt)
        {
            // Пустой бак — стрелка слева (+60°), полный — справа (-60°). Без зажигания стрелки падают.
            bool ignition = Engine != EngineState.Off;
            if (visual.fuelNeedle != null)
            {
                float target = ignition ? Mathf.Lerp(60f, -60f, fuel) : 70f;
                float current = visual.fuelNeedle.localEulerAngles.z;
                if (current > 180f) current -= 360f;
                visual.fuelNeedle.localRotation = Quaternion.Euler(0, 0, Mathf.MoveTowards(current, target, dt * 60f));
            }
            if (visual.speedNeedle != null)
                visual.speedNeedle.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(120f, -120f, SpeedKmh / 120f));

            // Лампа резерва мигает, когда бензина меньше 10%; лампа двигателя горит, пока он не работает
            lampBlink += dt;
            bool reserve = ignition && fuel < 0.1f && Mathf.Repeat(lampBlink, 1f) < 0.6f;
            if (visual.fuelLamp != null)
                visual.fuelLamp.sharedMaterial = Shapes.Mat(reserve ? LampFuel : LampOff);
            if (visual.engineLamp != null)
                visual.engineLamp.sharedMaterial = Shapes.Mat(Engine == EngineState.Starting ? LampEngine : LampOff);

            // Руль слегка подрагивает в руках, когда едем
            steerWobble += dt * (0.5f + Speed);
            if (visual.steeringWheel != null)
                visual.steeringWheel.localRotation = Quaternion.Euler(-65f, 0, 0) *
                                                     Quaternion.Euler(0, Mathf.Sin(steerWobble) * Speed * 1.5f, 0);
        }
    }
}
