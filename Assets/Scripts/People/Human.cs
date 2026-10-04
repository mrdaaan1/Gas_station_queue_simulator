using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Временный человечек из примитивов: ноги, туловище, руки, голова.
    /// Когда придёт модель разработчика (с анимациями Mixamo), заменим только этот класс.
    /// Точка опоры — между ступнями, вперёд — ось +Z.
    /// </summary>
    public class HumanRig : MonoBehaviour
    {
        public Transform legL, legR, armL, armR, head, body;
        float phase;
        float angry;

        public struct Look
        {
            public Color shirt, pants, skin, hair, shoes;
        }

        static readonly Color[] Shirts =
        {
            Shapes.Hex("#3b5998"), Shapes.Hex("#555555"), Shapes.Hex("#8b2e2e"), Shapes.Hex("#2e6b4f"),
            Shapes.Hex("#d2b48c"), Shapes.Hex("#6a4c93"), Shapes.Hex("#1f1f1f"),
        };
        static readonly Color[] Pants = { Shapes.Hex("#2b2f3a"), Shapes.Hex("#3d3d3d"), Shapes.Hex("#4a5a78"), Shapes.Hex("#5b4a3a") };
        static readonly Color[] Hair = { Shapes.Hex("#2a1d14"), Shapes.Hex("#5a3d22"), Shapes.Hex("#b49360"), Shapes.Hex("#8c8c8c"), Shapes.Hex("#1a1a1a") };
        static readonly Color[] Skins = { Shapes.Hex("#e0ac85"), Shapes.Hex("#c9926b"), Shapes.Hex("#f1c7a3") };

        public static Look RandomLook() => new Look
        {
            shirt = Shirts[Random.Range(0, Shirts.Length)],
            pants = Pants[Random.Range(0, Pants.Length)],
            skin = Skins[Random.Range(0, Skins.Length)],
            hair = Hair[Random.Range(0, Hair.Length)],
            shoes = Shapes.Hex("#1b1b1b"),
        };

        public static HumanRig Build(string name, Transform parent, Look look)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var rig = root.gameObject.AddComponent<HumanRig>();
            rig.body = Shapes.Group("Body", root);

            rig.legL = Limb(rig.body, "LegL", new Vector3(-0.11f, 0.92f, 0f), new Vector3(0.17f, 0.88f, 0.2f), look.pants, look.shoes);
            rig.legR = Limb(rig.body, "LegR", new Vector3(0.11f, 0.92f, 0f), new Vector3(0.17f, 0.88f, 0.2f), look.pants, look.shoes);
            Shapes.Box(rig.body, new Vector3(0f, 1.22f, 0f), new Vector3(0.44f, 0.62f, 0.25f), look.shirt, name: "Torso");
            rig.armL = Limb(rig.body, "ArmL", new Vector3(-0.29f, 1.48f, 0f), new Vector3(0.12f, 0.62f, 0.14f), look.shirt, look.skin);
            rig.armR = Limb(rig.body, "ArmR", new Vector3(0.29f, 1.48f, 0f), new Vector3(0.12f, 0.62f, 0.14f), look.shirt, look.skin);
            Shapes.Make(PrimitiveType.Cylinder, rig.body, new Vector3(0f, 1.58f, 0f), new Vector3(0.11f, 0.05f, 0.11f), look.skin, name: "Neck");
            rig.head = Shapes.Group("Head", rig.body, new Vector3(0f, 1.73f, 0f));
            Shapes.Make(PrimitiveType.Sphere, rig.head, Vector3.zero, new Vector3(0.24f, 0.27f, 0.25f), look.skin);
            Shapes.Make(PrimitiveType.Sphere, rig.head, new Vector3(0f, 0.05f, -0.02f), new Vector3(0.255f, 0.2f, 0.26f), look.hair, name: "Hair");
            Shapes.Box(rig.head, new Vector3(0.05f, 0.02f, 0.12f), new Vector3(0.03f, 0.03f, 0.01f), Shapes.Hex("#1a1a1a"), name: "Eye");
            Shapes.Box(rig.head, new Vector3(-0.05f, 0.02f, 0.12f), new Vector3(0.03f, 0.03f, 0.01f), Shapes.Hex("#1a1a1a"), name: "Eye");
            Shapes.Box(rig.head, new Vector3(0f, -0.02f, 0.13f), new Vector3(0.035f, 0.05f, 0.03f), look.skin, name: "Nose");
            return rig;
        }

        /// <summary>Конечность: шарнир сверху, сама она свисает вниз, на конце — ступня или кисть.</summary>
        static Transform Limb(Transform parent, string name, Vector3 joint, Vector3 size, Color color, Color end)
        {
            var pivot = Shapes.Group(name, parent, joint);
            Shapes.Box(pivot, new Vector3(0f, -size.y / 2f, 0f), size, color);
            bool leg = name.StartsWith("Leg");
            Shapes.Box(pivot, new Vector3(0f, -size.y + (leg ? 0.04f : -0.04f), leg ? 0.05f : 0f),
                leg ? new Vector3(size.x + 0.02f, 0.08f, size.z + 0.1f) : new Vector3(size.x, 0.1f, size.z), end);
            return pivot;
        }

        /// <summary>Шагаем: ноги и руки качаются в такт скорости.</summary>
        public void Animate(float speed, float dt)
        {
            float swing;
            if (speed > 0.05f)
            {
                phase += dt * (3.5f + speed * 2.2f);
                swing = Mathf.Sin(phase) * Mathf.Clamp(speed * 18f, 0f, 38f);
            }
            else
            {
                phase = 0f;
                swing = 0f;
            }
            legL.localRotation = Quaternion.Euler(swing, 0, 0);
            legR.localRotation = Quaternion.Euler(-swing, 0, 0);

            if (angry > 0f)
            {
                // Машет руками и возмущается
                angry -= dt;
                float wave = Mathf.Sin(Time.time * 9f) * 25f;
                armL.localRotation = Quaternion.Euler(-120f + wave, 0, -15f);
                armR.localRotation = Quaternion.Euler(-100f - wave, 0, 15f);
                head.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 5f) * 12f, 0);
            }
            else
            {
                armL.localRotation = Quaternion.Euler(-swing * 0.8f, 0, -4f);
                armR.localRotation = Quaternion.Euler(swing * 0.8f, 0, 4f);
                head.localRotation = Quaternion.identity;
            }
        }

        public void Rage(float seconds) => angry = seconds;
    }

    /// <summary>Водитель, которого игрок стукнул: выходит, идёт к обидчику, машет руками и ругается, возвращается.</summary>
    public class AngryDriver : MonoBehaviour
    {
        static readonly string[] Lines =
        {
            "Ты офигел?! Заплатишь за всё!", "Ты видел, что ты сделал?!", "Я сейчас ГАИ вызову!",
            "Кто тебе права продал?!", "Бампер новый, между прочим!", "Ну всё, стой теперь, оформляем!",
        };

        NpcCar car;
        PlayerCar player;
        HumanRig rig;
        float timer, duration;
        int phase; // 0 — выходит и идёт, 1 — ругается, 2 — возвращается
        float lineTimer;

        public static void Spawn(NpcCar car, PlayerCar player, Transform parent, float seconds)
        {
            var rig = HumanRig.Build("AngryDriver", parent, HumanRig.RandomLook());
            rig.transform.position = car.DriverDoor;
            var d = rig.gameObject.AddComponent<AngryDriver>();
            d.car = car;
            d.player = player;
            d.rig = rig;
            d.duration = seconds;
            SetDriverVisible(car, false);
        }

        static void SetDriverVisible(NpcCar car, bool visible)
        {
            if (car == null) return;
            if (car.visual.driverHead != null) car.visual.driverHead.gameObject.SetActive(visible);
            if (car.visual.driverTorso != null) car.visual.driverTorso.gameObject.SetActive(visible);
        }

        Vector3 Target()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.OnFoot && gm.Walker != null) return gm.Walker.transform.position;
            return player.DriverDoor;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            timer += dt;
            if (car == null) { Destroy(gameObject); return; }
            car.Hold(0.5f); // без водителя машина никуда не едет

            Vector3 goal = phase == 2 ? car.DriverDoor : Target();
            var to = goal - transform.position;
            to.y = 0f;
            float stopAt = phase == 2 ? 0.2f : 1.3f;
            float speed = 0f;
            if (to.magnitude > stopAt)
            {
                speed = phase == 2 ? 1.6f : 2.2f;
                transform.position += to.normalized * speed * dt;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), dt * 8f);
            }
            else if (phase == 0)
            {
                phase = 1;
                lineTimer = 0f;
            }
            else if (phase == 2)
            {
                SetDriverVisible(car, true);
                Destroy(gameObject);
                return;
            }

            if (phase == 1)
            {
                if (to.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), dt * 8f);
                lineTimer -= dt;
                if (lineTimer <= 0f)
                {
                    lineTimer = 3f;
                    SpeechBubble.Show(transform, Lines[Random.Range(0, Lines.Length)], 1.5f);
                    rig.Rage(2.5f);
                }
                if (timer > duration) phase = 2;
            }
            rig.Animate(speed, dt);
        }
    }
}
