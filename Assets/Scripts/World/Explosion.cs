using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Взрыв колонки: вспышка, огненный шар из нескольких сфер, разлёт обломков, огонь и столб чёрного дыма.
    /// Всё из примитивов, как и остальной мир.
    /// </summary>
    public class Explosion : MonoBehaviour
    {
        class Ball
        {
            public Transform t;
            public Vector3 velocity;
            public float size, delay;
        }

        readonly List<Ball> balls = new List<Ball>();
        Light flash;
        Smoke smoke;
        float age;
        float scale;

        static readonly Color[] FireColors =
        {
            Shapes.Hex("#fff3b0"), Shapes.Hex("#ffd23f"), Shapes.Hex("#ff9a1f"), Shapes.Hex("#ff5a1f"), Shapes.Hex("#d9301a"),
        };

        public static Explosion Spawn(Vector3 at, Transform worldRoot, float scale)
        {
            var go = new GameObject("Explosion");
            go.transform.SetParent(worldRoot, false);
            go.transform.position = at;
            var e = go.AddComponent<Explosion>();
            e.scale = scale;
            e.Build(worldRoot);
            return e;
        }

        void Build(Transform worldRoot)
        {
            // Огненный шар: сферы разлетаются вверх и в стороны, растут и тают
            for (int i = 0; i < 26; i++)
            {
                var c = FireColors[Random.Range(0, FireColors.Length)];
                var b = Shapes.Make(PrimitiveType.Sphere, transform, Vector3.up * 0.8f, Vector3.one * 0.3f, c, name: "Fire").transform;
                var dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 1.6f + 0.2f;
                balls.Add(new Ball
                {
                    t = b,
                    velocity = dir.normalized * Random.Range(3f, 9f) * scale,
                    size = Random.Range(1.8f, 3.6f) * scale,
                    delay = Random.Range(0f, 0.25f),
                });
            }

            // Вспышка
            var lightGo = new GameObject("Flash");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = Vector3.up * 2f;
            flash = lightGo.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = Shapes.Hex("#ffb347");
            flash.range = 45f * scale;
            flash.intensity = 8f;

            // Обломки колонки разлетаются по площадке
            for (int i = 0; i < 14; i++)
            {
                var col = i % 3 == 0 ? Shapes.Hex("#c8312b") : i % 3 == 1 ? Shapes.Hex("#e8e8e4") : Shapes.Hex("#2a2a2e");
                var piece = Shapes.Box(worldRoot, transform.position + Vector3.up * 1.2f + Random.insideUnitSphere * 0.5f,
                    new Vector3(Random.Range(0.15f, 0.6f), Random.Range(0.05f, 0.3f), Random.Range(0.15f, 0.6f)), col,
                    Random.insideUnitSphere * 180f, "Wreckage").transform;
                var v = Random.onUnitSphere;
                v.y = Mathf.Abs(v.y) + 0.6f;
                Debris.Launch(piece, v.normalized * Random.Range(6f, 14f) * scale);
            }

            // Столб дыма надолго
            smoke = gameObject.AddComponent<Smoke>();
            smoke.Init(transform, Vector3.up * 1.5f, worldRoot);
            smoke.sizeScale = 2.2f * scale;
            smoke.intensity = 1f;

            // Грохот на всю округу
            var src = SoundFactory.Source3D(gameObject, 1f, 400f);
            src.minDistance = 25f;
            src.spatialBlend = 0.6f;
            src.clip = SoundFactory.Explosion;
            src.Play();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            age += dt;

            foreach (var b in balls)
            {
                if (b.t == null) continue;
                float k = Mathf.Clamp01((age - b.delay) / 1.4f);
                if (k <= 0f) { b.t.localScale = Vector3.zero; continue; }
                b.velocity *= Mathf.Exp(-dt * 2.5f);
                b.velocity += Vector3.up * dt * 2f; // горячее поднимается
                b.t.position += b.velocity * dt;
                float s = b.size * Mathf.Sin(Mathf.Min(1f, k * 1.3f) * Mathf.PI * 0.5f) * (1f - Mathf.Clamp01((k - 0.7f) / 0.3f));
                b.t.localScale = Vector3.one * Mathf.Max(0.001f, s);
                if (k >= 1f) { Destroy(b.t.gameObject); b.t = null; }
            }

            // Вспышка гаснет, остаётся мерцающий огонь
            if (flash != null)
                flash.intensity = age < 0.25f ? 8f : Mathf.Lerp(flash.intensity, 1.6f + Mathf.PerlinNoise(age * 6f, 0f) * 1.2f, dt * 3f);
            if (smoke != null) smoke.intensity = age < 20f ? 1f : Mathf.Max(0.3f, 1f - (age - 20f) / 40f);
        }
    }
}
