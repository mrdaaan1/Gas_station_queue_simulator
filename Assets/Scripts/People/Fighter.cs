using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Здоровье и драка для человечка (и игрока пешком, и водителя NPC).
    /// Получил достаточно — падает, полежал — встаёт и хромает. На лице появляются синяки.
    /// </summary>
    public class Fighter : MonoBehaviour
    {
        public const float MaxHealth = 100f;
        const float PunchReach = 1.35f;

        public HumanRig Rig { get; private set; }
        public float Health { get; private set; } = MaxHealth;
        public bool Down { get; private set; }
        public bool Limping => !Down && Health < 45f;
        public string DisplayName = "Водитель";

        /// <summary>Когда последний раз получал или наносил удар (для «драка идёт»).</summary>
        public float LastCombatTime { get; private set; } = -100f;

        float downTimer;
        float cooldown;
        AudioSource audioSource;

        public static Fighter AddTo(HumanRig rig, string name)
        {
            var f = rig.gameObject.AddComponent<Fighter>();
            f.Rig = rig;
            f.DisplayName = name;
            f.audioSource = SoundFactory.Source3D(rig.gameObject, 0.9f, 40f);
            return f;
        }

        public bool InCombat => Time.time - LastCombatTime < 4f;
        public bool CanPunch => !Down && cooldown <= 0f && !Rig.IsFallen;

        /// <summary>Удар по тому, кто перед нами. Возвращает true, если попали.</summary>
        public bool Punch(Fighter target, float minDamage, float maxDamage, float cooldownSeconds)
        {
            if (!CanPunch) return false;
            cooldown = cooldownSeconds;
            LastCombatTime = Time.time;
            if (Random.value < 0.25f) Rig.Kick(); else Rig.Punch();
            if (target == null || target.Down) return false;

            var to = target.transform.position - transform.position;
            to.y = 0f;
            bool inReach = to.magnitude < PunchReach && Vector3.Angle(transform.forward, to) < 70f;
            if (!inReach) return false;
            target.TakeHit(Random.Range(minDamage, maxDamage));
            return true;
        }

        public void TakeHit(float damage)
        {
            if (Down) return;
            LastCombatTime = Time.time;
            float before = Health;
            Health = Mathf.Max(0f, Health - damage);
            Rig.Flinch();
            audioSource.pitch = Random.Range(0.85f, 1.15f);
            audioSource.PlayOneShot(SoundFactory.Punch);

            // Синяки по мере того, как здоровье падает
            foreach (float mark in new[] { 80f, 60f, 40f, 25f, 10f })
                if (before > mark && Health <= mark) Rig.AddBruise();

            if (Health <= 0f)
            {
                Down = true;
                downTimer = 6f;
                Rig.SetFallen(true);
                SpeechBubble.Show(transform, "Ой-ой-ой...", 0.6f);
            }
        }

        public void Heal(float amount) => Health = Mathf.Min(MaxHealth, Health + amount);

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            cooldown -= dt;
            Rig.Limping = Limping;
            Rig.Guard = !Down && InCombat;

            if (Down)
            {
                downTimer -= dt;
                if (downTimer <= 0f)
                {
                    Down = false;
                    Health = 30f; // встал, но еле живой
                    Rig.SetFallen(false);
                }
                return;
            }
            // Потихоньку отходит, если не дерётся
            if (!InCombat) Heal(dt * 0.8f);
        }
    }
}
