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

        // Драка
        public bool Guard;          // стойка: кулаки у лица
        public bool Limping;        // после драки хромает
        public bool Airborne;       // в прыжке: ноги поджаты, руки вверх
        float punchTimer, kickTimer, flinchTimer;
        bool punchRight;
        float fall;                 // 0 — стоит, 1 — лежит
        bool fallen;
        int bruises;

        public bool IsFallen => fall > 0.5f;

        // Стойка: кулаки перед лицом (руки вперёд и чуть внутрь)
        static Quaternion GuardL => Quaternion.Euler(-75f, 0, 25f);
        static Quaternion GuardR => Quaternion.Euler(-75f, 0, -25f);

        public void Punch()
        {
            punchTimer = 0.28f;
            punchRight = !punchRight;
        }

        public void Kick() => kickTimer = 0.4f;
        public void Flinch() => flinchTimer = 0.22f;
        public void SetFallen(bool value) => fallen = value;

        /// <summary>Синяки на лице: синяк → фингал → разбитый нос → ещё синяки.</summary>
        public void AddBruise()
        {
            var purple = Shapes.Hex("#5b2a5e");
            var red = Shapes.Hex("#a3322e");
            switch (bruises++)
            {
                case 0:
                    Shapes.Make(PrimitiveType.Sphere, head, new Vector3(0.07f, -0.02f, 0.095f), new Vector3(0.07f, 0.06f, 0.03f), purple, name: "Bruise");
                    break;
                case 1: // фингал под глазом
                    Shapes.Make(PrimitiveType.Sphere, head, new Vector3(-0.05f, 0.015f, 0.112f), new Vector3(0.065f, 0.055f, 0.02f), Shapes.Hex("#3b1d40"), name: "BlackEye");
                    break;
                case 2: // разбитый нос
                    Shapes.Box(head, new Vector3(0f, -0.06f, 0.125f), new Vector3(0.03f, 0.04f, 0.01f), red, name: "Blood");
                    Shapes.Make(PrimitiveType.Sphere, head, new Vector3(0f, -0.02f, 0.14f), new Vector3(0.045f, 0.055f, 0.04f), red, name: "Nose");
                    break;
                default:
                    if (bruises > 7) return;
                    Shapes.Make(PrimitiveType.Sphere, head,
                        new Vector3(Random.Range(-0.09f, 0.09f), Random.Range(-0.08f, 0.06f), 0.1f),
                        new Vector3(0.05f, 0.04f, 0.02f), Random.value < 0.5f ? purple : red, name: "Bruise");
                    break;
            }
        }

        /// <summary>Шагаем: ноги и руки качаются в такт скорости. Плюс удары, стойка, падение и хромота.</summary>
        public void Animate(float speed, float dt)
        {
            punchTimer = Mathf.Max(0f, punchTimer - dt);
            kickTimer = Mathf.Max(0f, kickTimer - dt);
            flinchTimer = Mathf.Max(0f, flinchTimer - dt);

            // Падение назад и подъём
            fall = Mathf.MoveTowards(fall, fallen ? 1f : 0f, dt * (fallen ? 3f : 1.2f));
            if (fall > 0.001f)
            {
                body.localRotation = Quaternion.Euler(-88f * fall, 0f, 0f);
                body.localPosition = new Vector3(0f, 0.15f * fall, 0f);
                legL.localRotation = Quaternion.Euler(-10f * fall, 0, -8f * fall);
                legR.localRotation = Quaternion.Euler(-25f * fall, 0, 8f * fall);
                armL.localRotation = Quaternion.Euler(-160f * fall, 0, -30f * fall);
                armR.localRotation = Quaternion.Euler(-150f * fall, 0, 35f * fall);
                if (fall > 0.99f) return;
            }

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

            // Хромает: заваливается на одну ногу, вторая почти не сгибается
            float lean = Limping ? Mathf.Sin(phase) * 7f + 4f : 0f;
            float flinch = flinchTimer > 0f ? -12f * (flinchTimer / 0.22f) : 0f;
            if (fall <= 0.001f)
            {
                body.localRotation = Quaternion.Euler(flinch, 0f, lean);
                body.localPosition = Vector3.zero;
            }

            float kick = kickTimer > 0f ? Mathf.Sin(kickTimer / 0.4f * Mathf.PI) * -85f : 0f;
            legL.localRotation = Quaternion.Euler(swing, 0, 0);
            legR.localRotation = Quaternion.Euler(kickTimer > 0f ? kick : (Limping ? -swing * 0.3f : -swing), 0, 0);

            if (punchTimer > 0f)
            {
                // Резкий выпад рукой вперёд, вторая — у лица
                float t = Mathf.Sin(punchTimer / 0.28f * Mathf.PI);
                var hit = Quaternion.Euler(-70f - 25f * t, 0, 0);
                armR.localRotation = punchRight ? hit : GuardR;
                armL.localRotation = punchRight ? GuardL : hit;
                head.localRotation = Quaternion.identity;
            }
            else if (Guard)
            {
                armL.localRotation = GuardL;
                armR.localRotation = GuardR;
                head.localRotation = Quaternion.Euler(8f, 0, 0);
            }
            else if (angry > 0f)
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
                head.localRotation = Quaternion.Euler(flinch * 1.5f, 0, 0);
            }

            // Прыжок: одна нога вперёд, другая назад, руки вверх и в стороны
            air = Mathf.MoveTowards(air, Airborne ? 1f : 0f, dt * 8f);
            if (air > 0.001f && fall <= 0.001f)
            {
                legL.localRotation = Quaternion.Slerp(legL.localRotation, Quaternion.Euler(-45f, 0, 0), air);
                legR.localRotation = Quaternion.Slerp(legR.localRotation, Quaternion.Euler(25f, 0, 0), air);
                if (punchTimer <= 0f)
                {
                    armL.localRotation = Quaternion.Slerp(armL.localRotation, Quaternion.Euler(-150f, 0, -35f), air);
                    armR.localRotation = Quaternion.Slerp(armR.localRotation, Quaternion.Euler(-150f, 0, 35f), air);
                }
            }
        }

        float air;

        public void Rage(float seconds) => angry = seconds;
    }
}
