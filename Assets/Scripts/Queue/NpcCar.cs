using UnityEngine;

namespace GasQueue
{
    /// <summary>Машина NPC: стоит в очереди, подтягивается за передней, заправляется, уезжает или сдаётся.</summary>
    public class NpcCar : QueueCar
    {
        enum State { InQueue, Leaving, GivingUp }

        const float Accel = 3f;
        const float Decel = 5f;
        const float MaxSpeed = 5.5f;

        static readonly string[] HonkAtPlayer =
        {
            "Проезжай давай!", "Уснул, что ли?!", "Э! Двигайся!", "Ну поехали уже!", "Алё, очередь!",
        };

        State state = State.InQueue;
        bool moving;
        float reactTimer;
        float reactionDelay;
        float impatience;
        float giveUpTime;
        AudioSource horn;

        public override bool IsPlayer => false;

        /// <summary>Стоит на месте в очереди (не едет и не уезжает).</summary>
        public bool IsStill => state == State.InQueue && !moving;

        public bool IsAtPump => IsStill && Mathf.Abs(Z - WorldBuilder.PumpZ) < 0.2f;

        void Awake()
        {
            reactionDelay = Random.Range(0.4f, 1.4f);
            horn = SoundFactory.Source3D(gameObject, 0.8f);
            horn.clip = SoundFactory.Horn;
            horn.pitch = Random.Range(0.8f, 1.15f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            switch (state)
            {
                case State.InQueue: UpdateInQueue(dt); break;
                case State.Leaving: UpdateLeaving(dt); break;
                case State.GivingUp: UpdateGivingUp(dt); break;
            }
        }

        void UpdateInQueue(float dt)
        {
            float desired = queue.DesiredZ(this);
            float remaining = desired - Z;

            if (!moving)
            {
                // Водитель замечает, что впереди освободилось место, не сразу
                if (remaining > 1.5f)
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

            if (moving)
            {
                float stopDistance = Speed * Speed / (2f * Decel);
                Speed = remaining <= stopDistance + 0.05f
                    ? Mathf.Max(0f, Speed - Decel * dt)
                    : Mathf.Min(MaxSpeed, Speed + Accel * dt);

                float z = Z + Speed * dt;
                if (z >= desired || (remaining < 0.08f && Speed < 0.4f))
                {
                    z = Mathf.Min(z, desired);
                    Speed = 0f;
                    moving = false;
                }
                MoveTo(Mathf.Min(z, queue.HardLimitZ(this)));
            }

            UpdateImpatience(dt);
        }

        /// <summary>Если игрок зевает и не подъезжает, машина сзади начинает бибикать.</summary>
        void UpdateImpatience(float dt)
        {
            var ahead = queue.Ahead(this);
            // Игрок стоит, хотя перед ним уже освободилось место
            bool blockedByPlayer = ahead != null && ahead.IsPlayer && ahead.Speed < 0.3f
                                   && queue.DesiredZ(ahead) - ahead.Z > queue.Settings.carSpacing * 0.8f;
            if (!blockedByPlayer)
            {
                impatience = Mathf.Min(impatience + dt, 0f); // остываем после прошлого гудка
                return;
            }

            impatience += dt;
            if (impatience > 3.5f)
            {
                Honk();
                Say(HonkAtPlayer[Random.Range(0, HonkAtPlayer.Length)]);
                GameManager.Instance.OnPlayerHonkedAt();
                impatience = -Random.Range(3f, 6f);
            }
        }

        public void Honk() => horn.Play();

        /// <summary>Реакция на бибиканье игрока.</summary>
        public void React(string phrase)
        {
            Say(phrase);
            if (Random.value < 0.3f) Invoke(nameof(Honk), 0.6f);
        }

        /// <summary>Заправился — уезжает вперёд.</summary>
        public void Leave()
        {
            state = State.Leaving;
            moving = false;
        }

        void UpdateLeaving(float dt)
        {
            Speed = Mathf.Min(14f, Speed + Accel * dt);
            MoveTo(Z + Speed * dt);
            if (Z > 260f) Destroy(gameObject);
        }

        /// <summary>Не выдержал — разворачивается и уезжает обратно по встречке.</summary>
        public void GiveUp(string phrase)
        {
            state = State.GivingUp;
            moving = false;
            Speed = 0f;
            giveUpTime = 0f;
            Say(phrase);
        }

        void UpdateGivingUp(float dt)
        {
            const float turnTime = 2.4f;
            giveUpTime += dt;
            if (giveUpTime < turnTime)
            {
                // Нелепый разворот на месте — для прототипа сойдёт
                float t = Mathf.SmoothStep(0f, 1f, giveUpTime / turnTime);
                var p = transform.position;
                float x = Mathf.Lerp(WorldBuilder.QueueLaneX, WorldBuilder.OppositeLaneX, t);
                transform.position = new Vector3(x, p.y, p.z);
                transform.rotation = Quaternion.Euler(0, -180f * t, 0);
                visual.Roll(dt * 1.5f);
                return;
            }

            Speed = Mathf.Min(12f, Speed + Accel * dt);
            transform.position += Vector3.back * Speed * dt;
            visual.Roll(Speed * dt);
            if (Z < -520f) Destroy(gameObject);
        }
    }
}
