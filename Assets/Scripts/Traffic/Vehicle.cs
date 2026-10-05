using UnityEngine;

namespace GasQueue
{
    /// <summary>Спецмашины, которые стоят в очереди со всеми: колонка «для своих» — только для депутатов.</summary>
    public enum ServiceKind { None, Ambulance, Police }

    /// <summary>Любая машина на дороге: и NPC, и машина игрока.</summary>
    public abstract class Vehicle : MonoBehaviour
    {
        static int nextId;

        public int Id { get; private set; }
        public CarVisual visual;
        public CarDamage damage;
        public TrafficManager traffic;

        /// <summary>Скорость вдоль направления машины, м/с (минус — задний ход).</summary>
        public float Speed { get; protected set; }

        public float Length => visual.length;
        public float Width => visual.width;
        public Vector3 Position => transform.position;
        public Vector3 Forward => transform.forward;
        public Obb Box => new Obb(transform.position, transform.forward, Width, Length);

        /// <summary>Кто перед нами мешает ехать (для разруливания взаимных блокировок).</summary>
        public Vehicle BlockedBy { get; protected set; }

        public abstract bool IsPlayer { get; }

        protected virtual void Awake() => Id = ++nextId;

        /// <summary>Ставит машину в точку и крутит колёса на пройденное расстояние.</summary>
        protected void Place(Vector3 pos, Vector3 fwd)
        {
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = transform.forward;
            visual.Roll(Vector3.Dot(pos - transform.position, transform.forward));
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd.normalized, Vector3.up));
        }

        public void Say(string phrase)
        {
            SpeechBubble.Show(transform, phrase, visual.height);
            visual.Bounce();
        }

        /// <summary>Точка у водительской двери в мире.</summary>
        public Vector3 DriverDoor => transform.TransformPoint(visual.driverDoorLocal);
    }
}
