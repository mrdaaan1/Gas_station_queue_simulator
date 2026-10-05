using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    public enum EngineState { Off, Starting, Running }

    /// <summary>
    /// Машина игрока со свободной ездой: газ/тормоз (W/S), задний ход (S, когда стоим), руль (A/D).
    /// Мотор глушится и заводится (I) и на холостых жжёт бензин. Врезается во всё твёрдое и мнётся.
    /// </summary>
    public class PlayerCar : Vehicle
    {
        const float ReverseAccel = 2.5f;
        const float BrakeDecel = 9f;
        const float CoastDecel = 1.2f;
        const float MaxReverse = 4f;
        const float Wheelbase = 2.6f;
        const float MaxSteer = 34f;
        const float SteerSpeed = 110f;
        const float StartDuration = 1.1f;

        static readonly Color LampOff = Shapes.Hex("#3a2a10");
        static readonly Color LampFuel = Shapes.Hex("#ffb000");
        static readonly Color LampEngine = Shapes.Hex("#ff3020");

        public override bool IsPlayer => true;

        /// <summary>Уровень топлива от 0 до 1.</summary>
        [Range(0f, 1f)] public float fuel = 0.12f;

        /// <summary>Сидит ли игрок за рулём и можно ли управлять.</summary>
        public bool controlsEnabled = true;

        public EngineState Engine { get; private set; } = EngineState.Running;
        public float FuelLiters => fuel * settings.tankLiters;
        public float SpeedKmh => Mathf.Abs(Speed) * 3.6f;
        public float SteerAngle { get; private set; }

        GameSettings settings;
        AudioSource horn, engine, starter, crash;
        float startTimer;
        float lampBlink;
        bool warnedEngineOff;
        readonly Dictionary<object, float> lastHit = new Dictionary<object, float>();

        public void Init(CarVisual v, GameSettings s, Transform worldRoot)
        {
            visual = v;
            settings = s;
            fuel = Mathf.Clamp01(s.startFuelLiters / s.tankLiters);
            damage = gameObject.AddComponent<CarDamage>();
            damage.Init(v, worldRoot);

            horn = SoundFactory.Source3D(gameObject, 1f);
            horn.spatialBlend = 0.3f;
            horn.clip = SoundFactory.Horn;

            starter = SoundFactory.Source3D(gameObject, 0.6f);
            starter.spatialBlend = 0f;
            starter.clip = SoundFactory.Starter;

            crash = SoundFactory.Source3D(gameObject, 1f);
            crash.spatialBlend = 0.2f;

            engine = SoundFactory.Source3D(gameObject, 0.18f);
            engine.spatialBlend = 0.4f;
            engine.clip = SoundFactory.Engine;
            engine.loop = true;
            engine.Play();
        }

        public void PlaceOnPath(LanePath path, float s) => Place(path.PointAt(s), path.TangentAt(s));

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // пауза

            if (controlsEnabled && GameInput.IgnitionPressed) ToggleEngine();
            UpdateEngine(dt);

            bool running = Engine == EngineState.Running;
            bool gas = controlsEnabled && GameInput.Gas;
            bool back = controlsEnabled && GameInput.Brake;
            float steerInput = controlsEnabled ? GameInput.Steer : 0f;

            if ((gas || back) && !running && !warnedEngineOff && Mathf.Abs(Speed) < 0.5f)
            {
                warnedEngineOff = true;
                GameManager.Instance.ShowMessage("Двигатель заглушен. Нажмите I, чтобы завести.");
            }
            if (!gas && !back) warnedEngineOff = false;

            // Газ, тормоз, задний ход
            // Побитая машина тянет хуже
            float power = Mathf.Lerp(0.4f, 1f, Mathf.Clamp01(damage.Health / 60f));
            float maxSpeed = visual.maxSpeed * power;
            if (gas)
            {
                if (Speed < -0.1f) Speed = Mathf.Min(0f, Speed + BrakeDecel * dt);
                else if (running) Speed = Mathf.Min(maxSpeed, Speed + visual.accel * power * (1f - Speed / (maxSpeed * 1.2f)) * dt);
            }
            else if (back)
            {
                if (Speed > 0.1f) Speed = Mathf.Max(0f, Speed - BrakeDecel * dt);
                else if (running) Speed = Mathf.Max(-MaxReverse, Speed - ReverseAccel * dt);
            }
            else Speed = Mathf.MoveTowards(Speed, 0f, CoastDecel * dt);

            // Руль: быстрее возвращается в ноль, на скорости поворачивается меньше
            // Спорткар на большой скорости рулит ещё аккуратнее
            float maxSteer = Mathf.Lerp(MaxSteer, 14f, Mathf.Clamp01(Mathf.Abs(Speed) / 16f));
            if (Mathf.Abs(Speed) > 16f) maxSteer = Mathf.Lerp(14f, 5f, Mathf.Clamp01((Mathf.Abs(Speed) - 16f) / 30f));
            float targetSteer = steerInput * maxSteer;
            float rate = Mathf.Abs(targetSteer) < Mathf.Abs(SteerAngle) ? SteerSpeed * 1.6f : SteerSpeed;
            SteerAngle = Mathf.MoveTowards(SteerAngle, targetSteer, rate * dt);
            visual.Steer(SteerAngle);

            // Кинематика «велосипеда»: поворот зависит от скорости и угла колёс
            float yawRate = Speed / Wheelbase * Mathf.Tan(SteerAngle * Mathf.Deg2Rad) * Mathf.Rad2Deg;
            var fwd = Quaternion.Euler(0f, yawRate * dt, 0f) * transform.forward;
            var pos = transform.position + fwd * Speed * dt;

            ResolveCollisions(ref pos, fwd);
            BurnFuel(dt, (pos - transform.position).magnitude);
            Place(pos, fwd);

            if (controlsEnabled && GameInput.HornPressed)
            {
                horn.Play();
                GameManager.Instance.OnPlayerHonk();
            }

            UpdateBlinkers(dt);
            UpdateDashboard(dt);
            engine.pitch = visual.sporty
                ? 0.7f + Rpm / 7000f * 1.3f
                : 0.8f + Mathf.Abs(Speed) / visual.maxSpeed * 1.1f + (gas && running ? 0.15f : 0f);
            throttle = gas && running;
        }

        // ---------- Поворотники ----------

        float blinkerTimer;
        bool blinkWasOn;
        AudioSource tick;

        void UpdateBlinkers(float dt)
        {
            if (controlsEnabled)
            {
                if (GameInput.BlinkLeftPressed) SetBlinker(visual.blinker == -1 ? 0 : -1);
                // E в машине — правый поворотник, если у окна нет продавца (тогда E — «купить»)
                if (GameInput.BlinkRightPressed && !GameManager.Instance.VendorAtWindow) SetBlinker(visual.blinker == 1 ? 0 : 1);
            }
            if (visual.blinker != 0)
            {
                blinkerTimer += dt;
                if (blinkerTimer > 20f) SetBlinker(0); // сам выключается через 20 секунд
            }
            // Тиканье реле поворотника
            bool on = visual.blinker != 0 && Mathf.Repeat(Time.time, 0.8f) < 0.45f;
            if (on != blinkWasOn && visual.blinker != 0)
            {
                if (tick == null)
                {
                    tick = SoundFactory.Source3D(gameObject, 0.35f);
                    tick.spatialBlend = 0f;
                }
                tick.PlayOneShot(SoundFactory.Tick, on ? 1f : 0.7f);
            }
            blinkWasOn = on;
        }

        public void SetBlinker(int side)
        {
            visual.blinker = side;
            blinkerTimer = 0f;
        }

        // ---------- Столкновения ----------

        void ResolveCollisions(ref Vector3 pos, Vector3 fwd)
        {
            for (int iter = 0; iter < 3; iter++)
            {
                bool any = false;
                var box = new Obb(pos, fwd, Width, Length);

                foreach (var o in Obstacles.All)
                {
                    var d = new Vector2(o.box.center.x - pos.x, o.box.center.y - pos.z);
                    float reach = o.box.half.magnitude + 3f;
                    if (d.sqrMagnitude > reach * reach) continue;
                    if (Obb.Overlap(box, o.box, out var mtv))
                    {
                        Push(ref pos, ref box, fwd, mtv, o.name, null);
                        any = true;
                    }
                }

                if (traffic.Barrier.IsDown && Obb.Overlap(box, traffic.Barrier.Box, out var bm))
                {
                    Push(ref pos, ref box, fwd, bm, "шлагбаум", null);
                    any = true;
                }

                foreach (var npc in traffic.Npcs)
                {
                    var d = npc.Position - pos;
                    if (d.x * d.x + d.z * d.z > 64f) continue;
                    if (Obb.Overlap(box, npc.Box, out var mtv))
                    {
                        Push(ref pos, ref box, fwd, mtv, npc, npc);
                        any = true;
                    }
                }
                if (!any) break;
            }
        }

        void Push(ref Vector3 pos, ref Obb box, Vector3 fwd, Vector2 mtv, object what, NpcCar npc)
        {
            pos += new Vector3(mtv.x, 0f, mtv.y);
            box = new Obb(pos, fwd, Width, Length);

            var n = mtv.normalized;
            var fwd2 = new Vector2(fwd.x, fwd.z);
            float impact = -Vector2.Dot(fwd2 * Speed, n); // скорость сближения с препятствием
            if (impact <= 0.05f) return;

            // Удар носом или задом
            bool ourFront = Vector2.Dot(fwd2, n) < 0f;
            // Гасим скорость и слегка отскакиваем
            Speed = -Speed * 0.12f;

            if (impact < 0.9f) return;
            float now = Time.time;
            if (lastHit.TryGetValue(what, out float t) && now - t < 0.8f) return;
            lastHit[what] = now;

            float severity = impact / 3f;
            var velocity = fwd * (ourFront ? impact : -impact);
            string report = damage.Hit(ourFront, severity, velocity);
            if (damage.Wrecked) ForceEngineOff();

            crash.pitch = Random.Range(0.85f, 1.1f);
            crash.PlayOneShot(SoundFactory.Crash, Mathf.Clamp01(0.3f + impact / 6f));

            if (npc != null)
            {
                bool theirFront = Vector3.Dot(transform.position - npc.Position, npc.Forward) > 0f;
                npc.damage.Hit(theirFront, severity, -velocity);
                npc.OnHitByPlayer(impact);
            }
            GameManager.Instance.OnPlayerCrash(impact, npc != null, what as string, report);
        }

        // ---------- Двигатель и топливо ----------

        void ToggleEngine()
        {
            if (Engine == EngineState.Off)
            {
                if (GameManager.Instance.NozzleIn)
                {
                    GameManager.Instance.ShowMessage("Сначала закончите заправку — пистолет ещё в баке!");
                    return;
                }
                if (damage.Wrecked)
                {
                    GameManager.Instance.ShowMessage("Стартер крутит, а толку ноль. Машина разбита.");
                    starter.Play();
                    return;
                }
                Engine = EngineState.Starting;
                startTimer = 0f;
                starter.Play();
            }
            else if (Engine == EngineState.Running)
            {
                Engine = EngineState.Off;
                starter.Stop();
                GameManager.Instance.OnEngineToggled(false);
            }
        }

        public void ForceEngineOff()
        {
            if (Engine == EngineState.Off) return;
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

            float targetVolume = Engine == EngineState.Running ? 0.18f : 0f;
            engine.volume = Mathf.MoveTowards(engine.volume, targetVolume, dt * 0.4f);
        }

        void BurnFuel(float dt, float distance)
        {
            if (Engine != EngineState.Running) return;
            float gameHours = dt * GameManager.Instance.ClockRate / 3600f;
            float liters = settings.idleLitersPerHour * gameHours + distance / 1000f * settings.drivingLitersPer100Km / 100f;
            fuel = Mathf.Max(0f, fuel - liters / settings.tankLiters);
            if (fuel <= 0f)
            {
                ForceEngineOff();
                GameManager.Instance.OnPlayerRanDry();
            }
        }

        /// <summary>Возмущённый водитель пинает или колотит нашу машину.</summary>
        public void OnKickedByNpc(Vector3 from)
        {
            bool front = Vector3.Dot(from - transform.position, transform.forward) > 0f;
            crash.pitch = Random.Range(0.9f, 1.2f);
            crash.PlayOneShot(SoundFactory.Thud, 0.8f);
            visual.Bounce();
            var report = damage.Hit(front, 0.08f, Vector3.zero); // пинок ≈ −1% прочности
            if (damage.Wrecked) ForceEngineOff();
            GameManager.Instance.OnCarKicked(report);
        }

        bool throttle;
        float shownRpm;

        /// <summary>Обороты мотора: шесть передач, на каждой стрелка тахометра бежит от ~3000 до отсечки.</summary>
        public float Rpm
        {
            get
            {
                if (Engine != EngineState.Running) return 0f;
                float v = Mathf.Abs(Speed);
                const int gears = 6;
                float top = visual.maxSpeed;
                float idle = throttle ? 1900f : 950f;
                if (v < 0.3f) return idle;
                // Передача i ведёт от top·(i−1)/6 до top·i/6
                float g = Mathf.Clamp(v / top * gears, 0f, gears - 0.001f);
                int gear = (int)g;
                float frac = g - gear;
                float low = gear == 0 ? 1200f : 3200f;
                return Mathf.Max(idle, Mathf.Lerp(low, 7600f, frac) + (throttle ? 300f : 0f));
            }
        }

        public void AddFuelLiters(float liters) => fuel = Mathf.Clamp01(fuel + liters / settings.tankLiters);

        void UpdateDashboard(float dt)
        {
            bool ignition = Engine != EngineState.Off;
            if (visual.fuelNeedle != null)
            {
                float target = ignition ? Mathf.Lerp(60f, -60f, fuel) : 70f;
                float current = visual.fuelNeedle.localEulerAngles.z;
                if (current > 180f) current -= 360f;
                visual.fuelNeedle.localRotation = Quaternion.Euler(0, 0, Mathf.MoveTowards(current, target, dt * 60f));
            }
            if (visual.speedNeedle != null)
                visual.speedNeedle.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(120f, -120f, SpeedKmh / visual.speedoMaxKmh));
            if (visual.tachNeedle != null)
            {
                float rpm = ignition && Engine == EngineState.Running ? Rpm : 0f;
                shownRpm = Mathf.MoveTowards(shownRpm, rpm, dt * 9000f);
                visual.tachNeedle.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(120f, -120f, shownRpm / 9000f));
            }

            lampBlink += dt;
            bool reserve = ignition && fuel < 0.1f && Mathf.Repeat(lampBlink, 1f) < 0.6f;
            if (visual.fuelLamp != null) visual.fuelLamp.sharedMaterial = Shapes.Mat(reserve ? LampFuel : LampOff);
            if (visual.engineLamp != null) visual.engineLamp.sharedMaterial = Shapes.Mat(Engine == EngineState.Starting ? LampEngine : LampOff);

            // Руль в салоне крутится вместе с колёсами (в 8 раз сильнее, как у настоящей машины)
            if (visual.steeringWheel != null)
                visual.steeringWheel.localRotation = Quaternion.Euler(visual.steeringTilt, 0, 0) * Quaternion.Euler(0, SteerAngle * 8f, 0);
            visual.UpdateArms();
        }
    }
}
