using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>Дым из-под капота: серые клубы растут, поднимаются и тают. Чем сильнее повреждения, тем гуще и чернее.</summary>
    public class Smoke : MonoBehaviour
    {
        public float intensity;
        Transform emitter;
        Transform world;
        float timer;

        class Puff
        {
            public Transform t;
            public float age, life, size;
            public Vector3 drift;
        }

        readonly List<Puff> puffs = new List<Puff>();

        public void Init(Transform parent, Vector3 localPos, Transform worldRoot)
        {
            emitter = Shapes.Group("SmokeEmitter", parent, localPos);
            world = worldRoot;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || emitter == null) return;
            timer -= dt;
            if (intensity > 0.01f && timer <= 0f && puffs.Count < 40)
            {
                timer = Mathf.Lerp(0.5f, 0.08f, intensity);
                var shade = Mathf.Round(Mathf.Lerp(0.62f, 0.12f, intensity) * 10f) / 10f; // немного оттенков — немного материалов
                var go = Shapes.Make(PrimitiveType.Sphere, world, emitter.position + Random.insideUnitSphere * 0.15f, Vector3.one * 0.2f,
                    new Color(shade, shade, shade), name: "Puff");
                puffs.Add(new Puff
                {
                    t = go.transform,
                    life = Random.Range(1.8f, 3f),
                    size = Mathf.Lerp(0.6f, 1.6f, intensity),
                    drift = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.8f, 1.4f), Random.Range(-0.3f, 0.3f)),
                });
            }
            for (int i = puffs.Count - 1; i >= 0; i--)
            {
                var p = puffs[i];
                p.age += dt;
                float k = p.age / p.life;
                if (k >= 1f || p.t == null)
                {
                    if (p.t != null) Destroy(p.t.gameObject);
                    puffs.RemoveAt(i);
                    continue;
                }
                p.t.position += p.drift * dt;
                // Растёт, потом тает
                float s = p.size * Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) + 0.05f;
                p.t.localScale = Vector3.one * s;
            }
        }

        void OnDestroy()
        {
            foreach (var p in puffs) if (p.t != null) Destroy(p.t.gameObject);
        }
    }
}
