using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Повреждения машины спереди и сзади. Чем сильнее и чаще удары, тем хуже:
    /// царапины и вмятина → разбитые фары → бампер висит → бампер и фары отваливаются → кузов мнётся.
    /// </summary>
    public class CarDamage : MonoBehaviour
    {
        public float Front { get; private set; }
        public float Rear { get; private set; }

        /// <summary>Запас прочности, 0…100. На нуле машина больше не едет.</summary>
        public float Health { get; private set; } = 100f;
        public bool Wrecked => Health <= 0f;

        Smoke smoke;

        CarVisual v;
        Transform debrisRoot;
        readonly HashSet<string> done = new HashSet<string>();

        static readonly Color Scratch = Shapes.Hex("#3b3b3b");
        static readonly Color BrokenGlass = Shapes.Hex("#4a4a4a");

        public void Init(CarVisual visual, Transform debrisParent)
        {
            v = visual;
            debrisRoot = debrisParent;
        }

        /// <summary>
        /// Удар. severity ≈ скорость удара / 3 м/с. velocity — скорость машины, чтобы обломки летели по инерции.
        /// Возвращает описание того, что отвалилось (или null).
        /// </summary>
        /// <param name="wearScale">Доля прочности за удар (по умолчанию severity × 14 %).</param>
        public string Hit(bool front, float severity, Vector3 velocity, float wearScale = 1f)
        {
            float value = (front ? Front : Rear) + severity;
            if (front) Front = value; else Rear = value;

            string end = front ? "F" : "R";
            var bumper = front ? v.bumperFront : v.bumperRear;
            var lights = front ? v.headlights : v.taillights;
            var panel = front ? v.frontPanel : v.rearPanel;
            float sign = front ? 1f : -1f;
            string report = Wear(severity * 14f * wearScale, velocity);

            if (value >= 0.35f && Once("dent" + end))
            {
                // Вмятина: бампер чуть перекосило и вдавило, на кузове царапины
                if (bumper != null)
                {
                    bumper.localRotation = Quaternion.Euler(0f, Random.Range(-6f, 6f), Random.Range(-4f, 4f));
                    bumper.localPosition -= new Vector3(0f, 0f, 0.04f * sign);
                }
                for (int i = 0; i < 3; i++)
                {
                    var z = (v.length / 2f + 0.005f) * sign;
                    Shapes.Box(v.body, new Vector3(Random.Range(-0.7f, 0.7f), Random.Range(0.5f, 0.85f), z),
                        new Vector3(Random.Range(0.15f, 0.4f), 0.015f, 0.01f), Scratch, new Vector3(0, 0, Random.Range(-25f, 25f)), "Scratch");
                }
            }

            if (value >= 1.2f && Once("lights" + end))
            {
                foreach (var l in lights)
                {
                    if (l == null) continue;
                    l.GetComponent<Renderer>().sharedMaterial = Shapes.Mat(BrokenGlass);
                    for (int i = 0; i < 3; i++) SpawnShard(l.position, velocity);
                }
                report = front ? "Фары разбиты!" : "Задние фонари разбиты!";
            }

            if (value >= 2f && Once("hang" + end) && bumper != null && bumper.parent == v.body)
            {
                bumper.localRotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(-8f, 8f), Random.value < 0.5f ? 14f : -14f);
                bumper.localPosition -= new Vector3(0f, 0.08f, 0f);
                report = "Бампер висит на соплях!";
            }

            if (value >= 2.8f && Once("fall" + end))
            {
                if (bumper != null && bumper.parent == v.body) Debris.Detach(bumper, velocity, debrisRoot);
                if (lights.Count > 0 && lights[0] != null) Debris.Detach(lights[0], velocity, debrisRoot);
                report = front ? "Передний бампер отвалился!" : "Задний бампер отвалился!";
            }

            if (value >= 3.6f && Once("crumple" + end))
            {
                if (lights.Count > 1 && lights[1] != null) Debris.Detach(lights[1], velocity, debrisRoot);
                if (panel != null)
                {
                    panel.localScale = new Vector3(panel.localScale.x, panel.localScale.y * 0.85f, panel.localScale.z * 0.8f);
                    panel.localRotation = Quaternion.Euler(Random.Range(4f, 9f) * sign, Random.Range(-4f, 4f), 0f);
                }
                report = front ? "Капот всмятку!" : "Багажник всмятку!";
            }

            return report;
        }

        bool Once(string key) => done.Add(key);

        /// <summary>
        /// Износ кузова: дым → капот открылся → отлетела дверь → помята крыша → отвалился капот →
        /// вторая дверь → чёрный дым → машина умерла. Возвращает описание (или null).
        /// </summary>
        public string Wear(float amount, Vector3 velocity)
        {
            if (Wrecked) return null;
            Health = Mathf.Max(0f, Health - amount);
            string report = null;

            if (Health < 40f && Once("smoke"))
            {
                smoke = gameObject.AddComponent<Smoke>();
                smoke.Init(v.body, new Vector3(0f, 1.0f, v.length / 2f - 0.6f), debrisRoot);
                report = "Из-под капота пошёл дым!";
            }
            if (smoke != null) smoke.intensity = Mathf.Clamp01((45f - Health) / 45f);

            if (Health < 30f && v.frontPanel != null && Once("hoodOpen"))
            {
                // Капот приподнялся и перекосился
                v.frontPanel.localRotation = Quaternion.Euler(-24f, Random.Range(-5f, 5f), Random.Range(-4f, 4f));
                v.frontPanel.localPosition += new Vector3(0f, 0.18f, 0f);
                report = "Капот задрался!";
            }
            if (Health < 22f && v.doors.Count > 0 && Once("door1"))
            {
                Debris.Detach(v.doors[Random.Range(0, v.doors.Count)], velocity + Vector3.up, debrisRoot);
                report = "Отвалилась дверь!";
            }
            if (Health < 15f && v.roof != null && Once("roof"))
            {
                v.roof.localPosition -= new Vector3(0f, 0.09f, 0f);
                v.roof.localRotation = Quaternion.Euler(Random.Range(-4f, 4f), 0f, Random.Range(-5f, 5f));
                report = "Крыша помята!";
            }
            if (Health < 9f && v.frontPanel != null && v.frontPanel.parent == v.body && Once("hoodOff"))
            {
                Debris.Detach(v.frontPanel, velocity + Vector3.up * 2f, debrisRoot);
                report = "Капот отлетел!";
            }
            if (Health < 4f && Once("door2"))
            {
                foreach (var d in v.doors)
                    if (d != null && d.parent == v.body) { Debris.Detach(d, velocity, debrisRoot); break; }
                report = "Машина разваливается на ходу! Ещё пара ударов — и всё.";
            }
            if (Wrecked) report = "Машина разбита. Приехали.";
            return report;
        }

        void SpawnShard(Vector3 at, Vector3 velocity)
        {
            var shard = Shapes.Box(debrisRoot, at, new Vector3(0.05f, 0.01f, 0.04f), Shapes.Hex("#d8d8d0"),
                new Vector3(Random.Range(0, 360f), Random.Range(0, 360f), 0), "Shard").transform;
            Debris.Launch(shard, velocity * 0.5f + Random.insideUnitSphere * 1.5f + Vector3.up * 1.5f);
        }

        /// <summary>Примерная стоимость ремонта — для финального экрана.</summary>
        public int RepairCost => Mathf.RoundToInt((Front + Rear) * 7300f / 100f) * 100;
    }

    /// <summary>Отвалившаяся деталь: падает, подпрыгивает и остаётся лежать на асфальте.</summary>
    public class Debris : MonoBehaviour
    {
        Vector3 velocity;
        Vector3 spin;
        float halfHeight = 0.05f;
        bool settled;

        public static void Detach(Transform part, Vector3 carVelocity, Transform parent)
        {
            part.SetParent(parent, true);
            Launch(part, carVelocity * 0.6f + new Vector3(Random.Range(-1f, 1f), Random.Range(1f, 2.5f), Random.Range(-1f, 1f)));
        }

        public static void Launch(Transform part, Vector3 velocity)
        {
            var d = part.gameObject.AddComponent<Debris>();
            d.velocity = velocity;
            d.spin = Random.insideUnitSphere * 360f;
            d.halfHeight = Mathf.Max(0.02f, Mathf.Min(part.lossyScale.y, part.lossyScale.x) / 2f);
        }

        void Update()
        {
            if (settled) return;
            float dt = Time.deltaTime;
            velocity.y -= 9.81f * dt;
            transform.position += velocity * dt;
            transform.Rotate(spin * dt, Space.World);
            var p = transform.position;
            if (p.y < halfHeight)
            {
                transform.position = new Vector3(p.x, halfHeight, p.z);
                velocity = new Vector3(velocity.x * 0.5f, -velocity.y * 0.3f, velocity.z * 0.5f);
                spin *= 0.5f;
                if (velocity.magnitude < 0.4f)
                {
                    settled = true;
                    // Ложится плашмя
                    var e = transform.eulerAngles;
                    transform.rotation = Quaternion.Euler(0f, e.y, 0f);
                }
            }
        }
    }
}
