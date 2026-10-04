using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Машина игрока. Игрок сам жмёт газ (W) и тормоз (S), чтобы подъехать в очереди.
    /// Пока без руля: машина едет только вперёд по своей полосе.
    /// </summary>
    public class PlayerCar : QueueCar
    {
        const float Accel = 4f;
        const float BrakeDecel = 10f;
        const float CoastDecel = 1.5f;
        const float MaxSpeed = 9f;

        [Range(0f, 1f)] public float fuel = 0.08f;

        AudioSource horn;
        AudioSource engine;
        float steerWobble;

        public override bool IsPlayer => true;

        /// <summary>Можно ли сейчас рулить (на финальном экране — нельзя).</summary>
        public bool controlsEnabled = true;

        public void Init(CarVisual v)
        {
            visual = v;
            horn = SoundFactory.Source3D(gameObject, 1f);
            horn.spatialBlend = 0.3f;
            horn.clip = SoundFactory.Horn;

            engine = SoundFactory.Source3D(gameObject, 0.18f);
            engine.spatialBlend = 0f;
            engine.clip = SoundFactory.Engine;
            engine.loop = true;
            engine.Play();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool gas = controlsEnabled && GameInput.Gas;
            bool brake = controlsEnabled && GameInput.Brake;

            if (gas) Speed = Mathf.Min(MaxSpeed, Speed + Accel * dt);
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
            MoveTo(z);

            if (controlsEnabled && GameInput.HornPressed)
            {
                horn.Play();
                GameManager.Instance.OnPlayerHonk();
            }

            UpdateDashboard(dt);
            engine.pitch = 0.8f + Speed / MaxSpeed * 0.9f + (gas ? 0.15f : 0f);
        }

        void UpdateDashboard(float dt)
        {
            // Пустой бак — стрелка слева (+60°), полный — справа (-60°)
            if (visual.fuelNeedle != null)
                visual.fuelNeedle.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(60f, -60f, fuel));
            if (visual.speedNeedle != null)
                visual.speedNeedle.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(110f, -110f, Speed / 25f));

            // Руль слегка подрагивает в руках, когда едем
            steerWobble += dt * (0.5f + Speed);
            if (visual.steeringWheel != null)
                visual.steeringWheel.localRotation = Quaternion.Euler(60f, 0, 0) *
                                                     Quaternion.Euler(0, Mathf.Sin(steerWobble) * Speed * 1.5f, 0);
        }
    }
}
