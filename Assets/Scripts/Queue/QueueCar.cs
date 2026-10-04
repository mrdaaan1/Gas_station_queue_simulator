using UnityEngine;

namespace GasQueue
{
    /// <summary>Любая машина в очереди: и NPC, и машина игрока.</summary>
    public abstract class QueueCar : MonoBehaviour
    {
        public CarVisual visual;
        public QueueManager queue;

        public float Speed { get; protected set; }
        public float Z => transform.position.z;
        public float Length => visual.length;
        public float RearZ => Z - Length / 2f;

        public abstract bool IsPlayer { get; }

        /// <summary>Двигает машину вдоль полосы и крутит колёса.</summary>
        protected void MoveTo(float z)
        {
            var p = transform.position;
            visual.Roll(z - p.z);
            transform.position = new Vector3(p.x, p.y, z);
        }

        public void Say(string phrase)
        {
            SpeechBubble.Show(transform, phrase, visual.height);
            visual.Bounce();
        }
    }
}
