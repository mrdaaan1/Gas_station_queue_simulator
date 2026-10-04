using UnityEngine;

namespace GasQueue
{
    /// <summary>Синяя мигалка и сирена депутатского автомобиля.</summary>
    public class VipLights : MonoBehaviour
    {
        public Renderer beacon;
        AudioSource siren;
        float sirenTimer;

        static readonly Color Bright = new Color(0.25f, 0.5f, 1f);
        static readonly Color Dim = new Color(0.08f, 0.16f, 0.4f);

        void Start()
        {
            siren = SoundFactory.Source3D(gameObject, 0.7f, 160f);
            siren.clip = SoundFactory.Siren;
        }

        /// <summary>Коротко «крякнуть» сиреной — расступитесь!</summary>
        public void Whoop()
        {
            if (siren != null && sirenTimer <= 0f)
            {
                siren.Play();
                sirenTimer = 3f;
            }
        }

        void Update()
        {
            sirenTimer -= Time.deltaTime;
            if (beacon == null) return;
            bool on = Mathf.Repeat(Time.time * 3f, 1f) < 0.5f;
            beacon.sharedMaterial = Shapes.Mat(on ? Bright : Dim);
        }
    }
}
