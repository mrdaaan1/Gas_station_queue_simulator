using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Депутат прилетает на вертолёте: чёрный «Майбах» с мигалкой висит на тросе под вертолётом.
    /// Вертолёт заходит со стороны, зависает над служебной колонкой «для своих», опускает «Майбах»,
    /// ждёт, пока его заправят, поднимает обратно и улетает. Раз в 6–8 минут, только в основной игре
    /// и только если служебная колонка свободна (дорожного депутата там нет).
    /// </summary>
    public class Helicopter : MonoBehaviour
    {
        const float Cable = 7f;          // от брюха вертолёта до «Майбаха» (до крыши)
        const float CarRoof = 1.5f;
        const float Cruise = 26f;
        const float FuelTime = 22f;

        enum Step { Waiting, Approach, Lower, Fueling, Lift, Leave }

        TrafficManager traffic;
        Step step = Step.Waiting;
        float timer = 170f;
        Transform heli, rotor, tailRotor, cable, hook;
        readonly Transform[] slings = new Transform[4];
        CarVisual car;
        AudioSource sound;
        Vector3 from, hover, away;
        float rotorAngle, tailAngle;
        Vector3 velocity;
        bool obstacleAdded, announced;

        static readonly Color Body = Shapes.Hex("#15181f");
        static readonly Color Stripe = Shapes.Hex("#b8862f");
        static readonly Color Glass = Shapes.Hex("#24323f");
        static readonly Color Metal = Shapes.Hex("#2a2c30");
        static readonly Color Steel = Shapes.Hex("#8f949a");

        /// <summary>Майбах стоит носом к +X, как у дорожного депутата.</summary>
        static Vector3 CarSpot => CityLayout.VipSpot;
        static readonly Quaternion CarRot = Quaternion.LookRotation(Vector3.right);

        public void Init(TrafficManager t)
        {
            traffic = t;
            Build();
            heli.gameObject.SetActive(false);
        }

        void Build()
        {
            heli = Shapes.Group("Helicopter", traffic.WorldRoot);
            var b = Shapes.Group("Body", heli);
            // Кабина-капля, хвостовая балка, киль, стабилизатор
            Shapes.Make(PrimitiveType.Sphere, b, new Vector3(0f, 0f, 0.3f), new Vector3(2.3f, 2.1f, 4.4f), Body, name: "Cabin");
            Shapes.Make(PrimitiveType.Sphere, b, new Vector3(0f, 0.15f, 1.45f), new Vector3(1.9f, 1.4f, 2.2f), Glass, name: "Windshield");
            Shapes.Make(PrimitiveType.Sphere, b, new Vector3(0f, 0.95f, -0.2f), new Vector3(1.2f, 0.7f, 2.2f), Body, name: "Engine");
            Shapes.Make(PrimitiveType.Cylinder, b, new Vector3(0f, 0.35f, -3.6f), new Vector3(0.45f, 2.6f, 0.45f), Body, new Vector3(90f, 0f, 0f), "TailBoom");
            Shapes.Box(b, new Vector3(0f, 1.05f, -6.1f), new Vector3(0.12f, 1.5f, 0.9f), Body, new Vector3(-15f, 0f, 0f), "Fin");
            Shapes.Box(b, new Vector3(0f, 0.35f, -5.4f), new Vector3(2f, 0.08f, 0.5f), Body, name: "Stab");
            // Золотая полоса по борту — «служебный»
            Shapes.Box(b, new Vector3(0f, -0.15f, 0.2f), new Vector3(2.32f, 0.16f, 3.2f), Stripe, name: "Stripe");
            foreach (float side in new[] { -1f, 1f })
            {
                // Полозья на стойках
                Shapes.Make(PrimitiveType.Cylinder, b, new Vector3(0.95f * side, -1.35f, 0.2f), new Vector3(0.1f, 1.9f, 0.1f), Metal, new Vector3(90f, 0f, 0f), "Skid");
                foreach (float z in new[] { -0.6f, 1.0f })
                    Shapes.Box(b, new Vector3(0.78f * side, -1.05f, z), new Vector3(0.07f, 0.6f, 0.07f), Metal, new Vector3(0f, 0f, 25f * side), "Strut");
                // Боковые окна
                Shapes.Make(PrimitiveType.Sphere, b, new Vector3(0.98f * side, 0.2f, 0.5f), new Vector3(0.35f, 0.9f, 1.6f), Glass, name: "Window");
            }
            // Несущий винт: мачта, втулка, две лопасти
            Shapes.Make(PrimitiveType.Cylinder, b, new Vector3(0f, 1.4f, 0f), new Vector3(0.18f, 0.25f, 0.18f), Metal, name: "Mast");
            rotor = Shapes.Group("Rotor", b, new Vector3(0f, 1.68f, 0f));
            Shapes.Make(PrimitiveType.Cylinder, rotor, Vector3.zero, new Vector3(0.4f, 0.06f, 0.4f), Metal, name: "Hub");
            Shapes.Box(rotor, Vector3.zero, new Vector3(10.5f, 0.04f, 0.32f), Metal, name: "Blade1");
            Shapes.Box(rotor, Vector3.zero, new Vector3(0.32f, 0.04f, 10.5f), Metal, name: "Blade2");
            // Рулевой винт на хвосте
            tailRotor = Shapes.Group("TailRotor", b, new Vector3(0.18f, 1.2f, -6.3f));
            Shapes.Box(tailRotor, Vector3.zero, new Vector3(0.04f, 1.5f, 0.14f), Metal, name: "TBlade1");
            Shapes.Box(tailRotor, Vector3.zero, new Vector3(0.04f, 0.14f, 1.5f), Metal, name: "TBlade2");
            // Огни
            Shapes.Make(PrimitiveType.Sphere, b, new Vector3(0f, -1.0f, 0.4f), Vector3.one * 0.2f, Shapes.Hex("#ff3020"), name: "Beacon");

            // Трос с крюком и четыре стропы к «Майбаху»
            cable = Shapes.Make(PrimitiveType.Cylinder, heli, Vector3.zero, Vector3.one, Steel, name: "Cable").transform;
            hook = Shapes.Make(PrimitiveType.Sphere, heli, Vector3.zero, Vector3.one * 0.3f, Shapes.Hex("#e0b020"), name: "Hook").transform;
            for (int i = 0; i < 4; i++)
                slings[i] = Shapes.Make(PrimitiveType.Cylinder, heli, Vector3.zero, Vector3.one, Steel, name: "Sling").transform;

            sound = SoundFactory.Source3D(heli.gameObject, 1f, 320f);
            sound.clip = SoundFactory.Rotor;
            sound.loop = true;
            sound.dopplerLevel = 0.6f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || traffic == null) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.State == GameState.Finished) return;

            if (step == Step.Waiting)
            {
                timer -= dt;
                if (timer > 0f) return;
                // Служебная колонка занята дорожным депутатом — попробуем чуть позже
                if (traffic.VipOnSite || !traffic.AreaClear(CarSpot, 4f)) { timer = 10f; return; }
                Begin();
                return;
            }

            rotorAngle += dt * 1500f;
            tailAngle += dt * 2400f;
            rotor.localRotation = Quaternion.Euler(0f, rotorAngle, 0f);
            tailRotor.localRotation = Quaternion.Euler(tailAngle, 0f, 0f);

            switch (step)
            {
                case Step.Approach:
                    if (Fly(hover, dt))
                    {
                        step = Step.Lower;
                        if (!announced)
                        {
                            announced = true;
                            gm.ShowMessage("Над заправкой вертолёт... На тросе висит «Майбах» с мигалкой. Депутату некогда стоять даже в пробке.", 9f);
                        }
                    }
                    break;
                case Step.Lower:
                    // Опускаемся, пока «Майбах» не встанет колёсами на асфальт
                    if (Fly(CarSpot + Vector3.up * (Cable + CarRoof + 0.02f), dt, 3f))
                    {
                        step = Step.Fueling;
                        timer = FuelTime;
                        if (!obstacleAdded)
                        {
                            obstacleAdded = true;
                            Obstacles.Add(new Obb(CarSpot, CarRot * Vector3.forward, car.width, car.length), MaybachObstacle);
                        }
                        gm.ShowMessage("«Майбах» поставили прямо к служебной колонке. Бензин для своих есть всегда.", 7f);
                    }
                    break;
                case Step.Fueling:
                    timer -= dt;
                    Fly(CarSpot + Vector3.up * (Cable + CarRoof + 0.02f), dt, 1f);
                    if (timer <= 0f)
                    {
                        step = Step.Lift;
                        RemoveObstacle();
                    }
                    break;
                case Step.Lift:
                    if (Fly(hover, dt, 4f)) step = Step.Leave;
                    break;
                case Step.Leave:
                    if (Fly(away, dt)) Finish();
                    break;
            }
            HangCar(dt);
        }

        const string MaybachObstacle = "майбах депутата";

        void Begin()
        {
            traffic.VipSpotReserved = true;
            // Заход сзади-справа (со стороны поля за заправкой), уход — дальше по ходу дороги
            from = CarSpot + new Vector3(260f, 55f, -180f);
            hover = CarSpot + Vector3.up * 28f;
            away = CarSpot + new Vector3(-220f, 70f, 320f);
            heli.position = from;
            heli.rotation = Quaternion.LookRotation(Flat(hover - from));
            velocity = Vector3.zero;
            car = CarFactory.Build("Heli Maybach", new Color(0.04f, 0.04f, 0.05f), CarModel.Maybach, false);
            car.transform.SetParent(traffic.WorldRoot, false);
            heli.gameObject.SetActive(true);
            sound.Play();
            step = Step.Approach;
            HangCar(1f);
        }

        void Finish()
        {
            RemoveObstacle();
            if (car != null) Destroy(car.gameObject);
            car = null;
            heli.gameObject.SetActive(false);
            sound.Stop();
            traffic.VipSpotReserved = false;
            step = Step.Waiting;
            timer = Random.Range(360f, 480f);
        }

        void RemoveObstacle()
        {
            if (!obstacleAdded) return;
            obstacleAdded = false;
            Obstacles.All.RemoveAll(e => e.name == MaybachObstacle);
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward;
        }

        /// <summary>Летим к точке: разгон до крейсерской, торможение у цели, наклон носом по ходу. true — на месте.</summary>
        bool Fly(Vector3 target, float dt, float maxSpeed = Cruise)
        {
            var to = target - heli.position;
            float dist = to.magnitude;
            float want = Mathf.Min(maxSpeed, dist * 0.5f + 0.4f);
            var desired = dist > 0.01f ? to / dist * want : Vector3.zero;
            velocity = Vector3.MoveTowards(velocity, desired, dt * 6f);
            heli.position += velocity * dt;
            var flat = new Vector3(velocity.x, 0f, velocity.z);
            // Над колонкой разворачиваемся вдоль «Майбаха»; в полёте — носом по ходу и с наклоном
            var yawDir = step == Step.Lower || step == Step.Fueling || flat.magnitude < 2f ? CarRot * Vector3.forward : flat.normalized;
            var yaw = Quaternion.LookRotation(yawDir);
            float pitch = Mathf.Clamp(flat.magnitude * 0.6f, 0f, 14f);
            heli.rotation = Quaternion.Slerp(heli.rotation, yaw * Quaternion.Euler(pitch, 0f, 0f), dt * 1.5f);
            return dist < 0.3f;
        }

        /// <summary>«Майбах» висит под брюхом: трос вниз, крюк, четыре стропы к углам крыши; чуть раскачивается.</summary>
        void HangCar(float dt)
        {
            if (car == null) return;
            var belly = heli.position + Vector3.down * 1.2f;
            bool landed = step == Step.Fueling || (step == Step.Lower && heli.position.y < CarSpot.y + Cable + CarRoof + 0.3f);
            float sway = landed ? 0f : Mathf.Sin(Time.time * 1.3f) * 0.25f;
            var carPos = heli.position + Vector3.down * (Cable + CarRoof) + heli.right * sway - Flat(velocity) * Mathf.Min(1.5f, velocity.magnitude * 0.05f);
            if (carPos.y < 0f) carPos.y = 0f;
            var carRot = landed ? CarRot : Quaternion.LookRotation(Flat(heli.forward)) * Quaternion.Euler(0f, 0f, sway * 6f);
            car.transform.SetPositionAndRotation(carPos, carRot);

            var hookPos = carPos + Vector3.up * (CarRoof + 1.6f);
            Stretch(cable, belly, hookPos, 0.05f);
            hook.position = hookPos;
            int i = 0;
            foreach (float sx in new[] { -0.7f, 0.7f })
                foreach (float sz in new[] { -1.6f, 1.6f })
                    Stretch(slings[i++], hookPos, car.transform.TransformPoint(new Vector3(sx, CarRoof - 0.05f, sz)), 0.025f);
        }

        static void Stretch(Transform t, Vector3 a, Vector3 b, float thickness)
        {
            t.position = (a + b) / 2f;
            var d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            t.up = d.normalized;
            t.localScale = new Vector3(thickness, d.magnitude / 2f, thickness);
        }

        void OnDestroy()
        {
            RemoveObstacle();
            if (heli != null) Destroy(heli.gameObject);
            if (car != null) Destroy(car.gameObject);
        }
    }
}
