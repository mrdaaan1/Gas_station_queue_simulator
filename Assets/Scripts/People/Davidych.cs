using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Давидыч — коренастый, с короткой тёмной стрижкой и бородой, в коричневой кофте на молнии
    /// с лоскутной вставкой на груди, белой футболке и серо-синих джинсах. В отличие от обычных человечков
    /// (<see cref="HumanRig"/>) у него есть колени: он умеет приседать по-настоящему.
    /// Точка опоры — между ступнями, вперёд — ось +Z.
    /// </summary>
    public class DavidychRig : MonoBehaviour
    {
        const float Thigh = 0.46f, Shin = 0.44f, FootH = 0.06f;
        const float HipH = Thigh + Shin + FootH; // 0,96 — высота таза стоя

        Transform pelvis, torso, head, thighL, thighR, shinL, shinR, footL, footR, armL, armR, foreL, foreR;

        static readonly Color Skin = Shapes.Hex("#e6b08c");
        static readonly Color HairC = Shapes.Hex("#2a1c13");
        static readonly Color Beard = Shapes.Hex("#6a4530");
        static readonly Color Jacket = Shapes.Hex("#5b3934");
        static readonly Color JacketDark = Shapes.Hex("#4a2e2a");
        static readonly Color Tee = Shapes.Hex("#efefe8");
        static readonly Color Jeans = Shapes.Hex("#4f5d6e");
        static readonly Color Shoes = Shapes.Hex("#2b211c");
        static readonly Color[] Patch = { Shapes.Hex("#8c8f91"), Shapes.Hex("#dcd9d2"), Shapes.Hex("#3a3a3a"), Shapes.Hex("#a9aaa6"), Shapes.Hex("#6c6f71") };

        public static DavidychRig Build(Transform parent)
        {
            var root = new GameObject("Davidych").transform;
            root.SetParent(parent, false);
            var rig = root.gameObject.AddComponent<DavidychRig>();
            rig.Create();
            return rig;
        }

        void Create()
        {
            pelvis = Shapes.Group("Pelvis", transform, new Vector3(0f, HipH, 0f));
            Shapes.Box(pelvis, new Vector3(0f, 0.02f, 0f), new Vector3(0.46f, 0.24f, 0.31f), Jeans, name: "Hips");
            Shapes.Box(pelvis, new Vector3(0f, 0.12f, 0f), new Vector3(0.47f, 0.05f, 0.32f), Shapes.Hex("#2a2420"), name: "Belt");

            foreach (float side in new[] { -1f, 1f })
            {
                var thigh = Shapes.Group(side < 0 ? "ThighL" : "ThighR", pelvis, new Vector3(0.12f * side, 0f, 0f));
                Shapes.Box(thigh, new Vector3(0f, -Thigh / 2f, 0f), new Vector3(0.2f, Thigh + 0.04f, 0.23f), Jeans);
                var shin = Shapes.Group("Shin", thigh, new Vector3(0f, -Thigh, 0f));
                Shapes.Make(PrimitiveType.Sphere, shin, Vector3.zero, new Vector3(0.19f, 0.17f, 0.21f), Jeans, name: "Knee");
                Shapes.Box(shin, new Vector3(0f, -Shin / 2f, 0f), new Vector3(0.17f, Shin, 0.19f), Jeans);
                var foot = Shapes.Group("Foot", shin, new Vector3(0f, -Shin, 0f));
                Shapes.Box(foot, new Vector3(0f, -FootH / 2f, 0.06f), new Vector3(0.15f, FootH + 0.02f, 0.31f), Shoes);
                if (side < 0) { thighL = thigh; shinL = shin; footL = foot; }
                else { thighR = thigh; shinR = shin; footR = foot; }
            }

            torso = Shapes.Group("Torso", pelvis, new Vector3(0f, 0.1f, 0f));
            // Коренастый: широкая грудь и живот
            Shapes.Box(torso, new Vector3(0f, 0.33f, 0f), new Vector3(0.56f, 0.62f, 0.34f), Jacket, name: "Chest");
            Shapes.Make(PrimitiveType.Sphere, torso, new Vector3(0f, 0.2f, 0.06f), new Vector3(0.54f, 0.46f, 0.38f), Jacket, name: "Belly");
            Shapes.Box(torso, new Vector3(0f, 0.02f, 0f), new Vector3(0.52f, 0.07f, 0.36f), JacketDark, name: "Hem");
            foreach (float side in new[] { -1f, 1f })
                Shapes.Make(PrimitiveType.Sphere, torso, new Vector3(0.27f * side, 0.6f, 0f), new Vector3(0.2f, 0.18f, 0.34f), Jacket, name: "Shoulder");
            // Лоскутная вставка на левой стороне груди: серые, белые и чёрные квадраты
            int k = 0;
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 2; col++)
                    Shapes.Box(torso, new Vector3(-0.08f - col * 0.1f, 0.58f - row * 0.11f, 0.175f),
                        new Vector3(0.1f, 0.11f, 0.012f), Patch[(k++ * 3 + row) % Patch.Length], name: "Patch");
            Shapes.Box(torso, new Vector3(-0.235f, 0.47f, 0.17f), new Vector3(0.03f, 0.34f, 0.012f), Patch[2], name: "PatchStripe");
            // Молния, ворот-стойка и белая футболка в вырезе
            Shapes.Box(torso, new Vector3(0.0f, 0.33f, 0.178f), new Vector3(0.016f, 0.6f, 0.01f), Shapes.Hex("#c9b89a"), name: "Zip");
            Shapes.Box(torso, new Vector3(0f, 0.645f, 0.165f), new Vector3(0.13f, 0.07f, 0.02f), Tee, name: "Tee");
            Shapes.Box(torso, new Vector3(0f, 0.69f, 0f), new Vector3(0.36f, 0.07f, 0.33f), Jacket, name: "Collar");
            // Карманы-кенгуру на животе
            Shapes.Box(torso, new Vector3(0f, 0.13f, 0.235f), new Vector3(0.36f, 0.015f, 0.02f), JacketDark, name: "Pocket");

            Shapes.Make(PrimitiveType.Cylinder, torso, new Vector3(0f, 0.74f, 0.01f), new Vector3(0.15f, 0.05f, 0.15f), Skin, name: "Neck");
            head = Shapes.Group("Head", torso, new Vector3(0f, 0.88f, 0.01f));
            Shapes.Make(PrimitiveType.Sphere, head, Vector3.zero, new Vector3(0.26f, 0.29f, 0.27f), Skin, name: "Face");
            // Короткая тёмная стрижка
            Shapes.Make(PrimitiveType.Sphere, head, new Vector3(0f, 0.085f, -0.03f), new Vector3(0.27f, 0.17f, 0.25f), HairC, name: "Hair");
            // Борода по скулам и подбородку, усы
            Shapes.Make(PrimitiveType.Sphere, head, new Vector3(0f, -0.088f, 0.012f), new Vector3(0.258f, 0.15f, 0.25f), Beard, name: "Beard");
            Shapes.Make(PrimitiveType.Sphere, head, new Vector3(0f, -0.13f, 0.065f), new Vector3(0.12f, 0.08f, 0.1f), Beard, name: "Chin");
            Shapes.Box(head, new Vector3(0f, -0.04f, 0.148f), new Vector3(0.1f, 0.022f, 0.02f), Shapes.Hex("#4a2e1c"), name: "Moustache");
            Shapes.Box(head, new Vector3(0f, -0.068f, 0.156f), new Vector3(0.05f, 0.012f, 0.01f), Shapes.Hex("#8a4a3e"), name: "Lips");
            // Глаза смотрят чуть вверх, брови приподняты — фирменный взгляд
            foreach (float side in new[] { -1f, 1f })
            {
                Shapes.Box(head, new Vector3(0.052f * side, 0.025f, 0.123f), new Vector3(0.04f, 0.024f, 0.01f), Shapes.Hex("#f4f1ea"), name: "EyeWhite");
                Shapes.Box(head, new Vector3(0.052f * side, 0.031f, 0.129f), new Vector3(0.02f, 0.018f, 0.008f), Shapes.Hex("#2b1d14"), name: "Pupil");
                Shapes.Box(head, new Vector3(0.055f * side, 0.064f, 0.124f), new Vector3(0.065f, 0.017f, 0.014f), HairC, new Vector3(0f, 0f, -12f * side), "Brow");
                Shapes.Make(PrimitiveType.Sphere, head, new Vector3(0.132f * side, 0.0f, 0f), new Vector3(0.04f, 0.07f, 0.05f), Skin, name: "Ear");
            }
            Shapes.Box(head, new Vector3(0f, -0.005f, 0.135f), new Vector3(0.045f, 0.06f, 0.045f), Skin, name: "Nose");

            foreach (float side in new[] { -1f, 1f })
            {
                var arm = Shapes.Group(side < 0 ? "ArmL" : "ArmR", torso, new Vector3(0.34f * side, 0.58f, 0f));
                Shapes.Box(arm, new Vector3(0f, -0.16f, 0f), new Vector3(0.16f, 0.34f, 0.17f), Jacket);
                var fore = Shapes.Group("Fore", arm, new Vector3(0f, -0.32f, 0f));
                Shapes.Box(fore, new Vector3(0f, -0.13f, 0f), new Vector3(0.14f, 0.27f, 0.15f), Jacket);
                Shapes.Box(fore, new Vector3(0f, -0.27f, 0f), new Vector3(0.145f, 0.04f, 0.155f), Patch[3], name: "Cuff");
                Shapes.Make(PrimitiveType.Sphere, fore, new Vector3(0f, -0.33f, 0.01f), new Vector3(0.1f, 0.12f, 0.1f), Skin, name: "Hand");
                if (side < 0) { armL = arm; foreL = fore; } else { armR = arm; foreR = fore; }
            }
            Stand();
        }

        static Quaternion X(float deg) => Quaternion.Euler(deg, 0f, 0f);

        void Legs(float thighDeg, float kneeDeg, float footDeg, float thighDegR, float kneeDegR, float footDegR)
        {
            thighL.localRotation = X(thighDeg); shinL.localRotation = X(kneeDeg); footL.localRotation = X(footDeg);
            thighR.localRotation = X(thighDegR); shinR.localRotation = X(kneeDegR); footR.localRotation = X(footDegR);
        }

        public void Stand()
        {
            pelvis.localPosition = new Vector3(0f, HipH, 0f);
            Legs(0f, 0f, 0f, 0f, 0f, 0f);
            torso.localRotation = Quaternion.identity;
            head.localRotation = Quaternion.identity;
            armL.localRotation = Quaternion.Euler(0f, 0f, -6f);
            armR.localRotation = Quaternion.Euler(0f, 0f, 6f);
            foreL.localRotation = X(-10f);
            foreR.localRotation = X(-10f);
        }

        /// <summary>Присед: t = 0 — стоит, 1 — внизу. Ступни на месте, колени вперёд, таз опускается, руки вперёд.</summary>
        public void Squat(float t)
        {
            float a = 72f * t;
            float rad = a * Mathf.Deg2Rad;
            pelvis.localPosition = new Vector3(0f, FootH + (Thigh + Shin) * Mathf.Cos(rad), -0.05f * t);
            Legs(-a, 2f * a, -a, -a, 2f * a, -a);
            float lean = a * 0.45f;
            torso.localRotation = X(lean);
            head.localRotation = X(-lean * 0.8f); // голову держит прямо, смотрит вперёд
            armL.localRotation = Quaternion.Euler(-90f * t - lean * t, 0f, -6f + 4f * t);
            armR.localRotation = Quaternion.Euler(-90f * t - lean * t, 0f, 6f - 4f * t);
            foreL.localRotation = X(-10f * (1f - t));
            foreR.localRotation = X(-10f * (1f - t));
        }

        /// <summary>Шаг: phase — фаза походки (радианы).</summary>
        public void Walk(float phase)
        {
            float sw = Mathf.Sin(phase) * 24f;
            pelvis.localPosition = new Vector3(0f, HipH - 0.02f + Mathf.Abs(Mathf.Cos(phase)) * 0.02f, 0f);
            float kneeL = Mathf.Max(0f, -Mathf.Sin(phase)) * 30f, kneeR = Mathf.Max(0f, Mathf.Sin(phase)) * 30f;
            Legs(-sw, kneeL, 0f, sw, kneeR, 0f);
            torso.localRotation = X(3f);
            head.localRotation = Quaternion.identity;
            armL.localRotation = Quaternion.Euler(sw * 0.8f, 0f, -8f);
            armR.localRotation = Quaternion.Euler(-sw * 0.8f, 0f, 8f);
            foreL.localRotation = X(-15f);
            foreR.localRotation = X(-15f);
        }

        /// <summary>Сидит за рулём: бёдра вперёд, голени вниз, руки на руле.</summary>
        public void Seated()
        {
            pelvis.localPosition = new Vector3(0f, HipH, 0f);
            Legs(-88f, 80f, 8f, -85f, 75f, 10f);
            torso.localRotation = X(-12f);
            head.localRotation = X(10f);
            armL.localRotation = Quaternion.Euler(-60f, 0f, 10f);
            armR.localRotation = Quaternion.Euler(-60f, 0f, -10f);
            foreL.localRotation = X(-25f);
            foreR.localRotation = X(-25f);
        }
    }

    /// <summary>
    /// BMW X5 Давидыча: когда игрок подъезжает к заправке, мимо очереди пролетает золотой X5 и встаёт на тротуар
    /// напротив колонок. Давидыч выходит, 30 раз приседает у машины (считая вслух), садится и уезжает.
    /// Через минуту возвращается с другой стороны — встаёт на противоположном тротуаре примерно там же,
    /// снова приседает и уезжает в другую сторону. И так по кругу. Только в основной игре.
    /// </summary>
    public class Davidych : MonoBehaviour
    {
        const int Reps = 30;
        const float RepTime = 1.25f;
        const float ParkZ = 10f;

        enum Step { Waiting, Driving, Parked, ToSpot, Squatting, ToCar, Leaving }

        TrafficManager traffic;
        Step step = Step.Waiting;
        float timer = 30f, firstTimer = 140f;
        bool sideA = true; // A — едет к +Z и встаёт справа (у заправки), B — едет к −Z и встаёт слева
        bool announced;

        NpcCar car;
        DavidychRig seated, walker;
        Vector3 spot, corner;
        Vector3 spotFacing;
        bool viaCorner; // обходит машину через угол у капота, а не сквозь крыло
        int reps;
        float repTimer, walkPhase;

        static readonly string[] Arrive =
        {
            "Здарова, очередь! Пока стоите — смотрите, как надо!", "Бензин подождёт, ноги — нет!", "Так, тридцать раз — и дальше по делам.",
            "Чего стоим? Приседаем!",
        };
        static readonly string[] Tens =
        {
            "Десять! Это даже не разминка.", "Двадцать! Я в своё время по три тысячи приседал!", "Ноги — это база, запомните!",
        };
        static readonly string[] Done =
        {
            "Тридцать! Всё, поехал, некогда мне тут.", "Тридцать! Учитесь, пока я жив.", "Тридцать! Завтра — сто.",
        };

        public void Init(TrafficManager t)
        {
            traffic = t;
        }

        // Маршруты: A — по левому ряду мимо очереди, через пустую правую полосу (между въездом и выездом) на тротуар;
        // B — со встречки на левый тротуар напротив. Встаёт там, где нет столбов и деревьев (Z ≈ 10).
        static LanePath InA() => new LanePath("DavidychInA", 17f, new[]
        {
            CityLayout.P(CityLayout.LaneLeft, CityLayout.RoadStartZ), CityLayout.P(CityLayout.LaneLeft, -30f), CityLayout.P(CityLayout.LaneMiddle, -16f),
            CityLayout.P(CityLayout.LaneQueue, -4f), CityLayout.P(11.4f, 4.5f), CityLayout.P(12.2f, ParkZ),
        });
        static LanePath OutA() => new LanePath("DavidychOutA", 15f, new[]
        {
            CityLayout.P(12.2f, ParkZ), CityLayout.P(11.2f, 17f), CityLayout.P(8f, 24f), CityLayout.P(CityLayout.LaneMiddle, 33f),
            CityLayout.P(CityLayout.LaneLeft, 46f), CityLayout.P(CityLayout.LaneLeft, CityLayout.RoadEndZ),
        });
        static LanePath InB() => new LanePath("DavidychInB", 17f, new[]
        {
            CityLayout.P(-1.75f, CityLayout.RoadEndZ), CityLayout.P(-1.75f, 40f), CityLayout.P(-5.25f, 30f), CityLayout.P(-8.75f, 21f),
            CityLayout.P(-11.4f, 15.5f), CityLayout.P(-12.2f, ParkZ),
        });
        static LanePath OutB() => new LanePath("DavidychOutB", 15f, new[]
        {
            CityLayout.P(-12.2f, ParkZ), CityLayout.P(-11.2f, 3f), CityLayout.P(-8.75f, -5f), CityLayout.P(-8.75f, CityLayout.RoadStartZ),
        });

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || traffic == null) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.State == GameState.Finished) return;

            switch (step)
            {
                case Step.Waiting:
                    timer -= dt;
                    firstTimer -= dt;
                    // Первый раз — когда игрок подъезжает к заправке (или через пару минут в любом случае)
                    bool ready = announced ? timer <= 0f : (timer <= 0f && (traffic.Player.Position.z > -110f || firstTimer <= 0f));
                    if (ready && !TrySpawn()) timer = 2f;
                    break;
                case Step.Driving:
                    if (car == null) { Reset(); break; }
                    if (car.Parked)
                    {
                        step = Step.Parked;
                        timer = 0.8f;
                    }
                    break;
                case Step.Parked:
                    if (car == null) { Reset(); break; }
                    timer -= dt;
                    if (timer <= 0f) GetOut();
                    break;
                case Step.ToSpot:
                    if (viaCorner) { if (WalkTo(corner, dt)) viaCorner = false; break; }
                    if (WalkTo(spot, dt))
                    {
                        step = Step.Squatting;
                        reps = 0;
                        repTimer = 0f;
                        Face(spotFacing);
                        SpeechBubble.Show(walker.transform, Arrive[Random.Range(0, Arrive.Length)], 1.95f);
                    }
                    break;
                case Step.Squatting:
                    UpdateSquats(dt);
                    break;
                case Step.ToCar:
                    if (car == null) { Reset(); break; }
                    if (viaCorner) { if (WalkTo(corner, dt)) viaCorner = false; break; }
                    if (WalkTo(DoorPoint(), dt)) GetIn();
                    break;
                case Step.Leaving:
                    timer -= dt;
                    // Уехал за край дороги — машина исчезла; ждём и возвращаемся с другой стороны
                    if (car == null) { step = Step.Waiting; timer = Random.Range(35f, 60f); }
                    break;
            }
        }

        bool TrySpawn()
        {
            var path = sideA ? InA() : InB();
            float s;
            if (sideA)
            {
                // Появляется сзади игрока и обгоняет очередь по левому ряду
                float z = Mathf.Clamp(traffic.Player.Position.z - 70f, CityLayout.RoadStartZ + 20f, -80f);
                s = path.Project(CityLayout.P(CityLayout.LaneLeft, z), out _);
            }
            else s = path.Project(CityLayout.P(-1.75f, 300f), out _);
            // Рядом очередь (соседняя полоса) — это не помеха; нужен свободный кусок своей полосы
            var at = path.PointAt(s);
            if (!traffic.AreaClear(at, sideA ? 3.2f : 10f)) return false;
            if (sideA && !traffic.LaneClearNear(traffic.LeftPath, traffic.LeftPath.Project(at, out _), 25f)) return false;

            var visual = SportsCars.BuildX5("Davidych X5", SportsCars.X5Gold);
            visual.SetDriverVisible(false, false);
            car = traffic.SpawnScripted(visual, path, s, true);
            seated = DavidychRig.Build(visual.transform);
            seated.transform.localPosition = new Vector3(X5Model.DriverEyes.x, -0.25f, X5Model.DriverEyes.z + 0.1f);
            seated.transform.localScale = Vector3.one * 0.85f;
            seated.Seated();
            step = Step.Driving;

            var gm = GameManager.Instance;
            if (!announced)
            {
                announced = true;
                gm?.ShowMessage("Мимо очереди пролетает золотой X5 с питбулями... Это же Давидыч! Сейчас что-то будет.", 8f);
            }
            return true;
        }

        Vector3 DoorPoint() => car.transform.TransformPoint(car.visual.driverDoorLocal);

        void GetOut()
        {
            if (seated != null) seated.gameObject.SetActive(false);
            walker = DavidychRig.Build(traffic.WorldRoot);
            walker.transform.position = DoorPoint();
            // Встаёт перед капотом, лицом к дороге — чтобы очередь видела
            var fwd = car.transform.forward;
            spot = car.transform.position + fwd * (car.Length / 2f + 1.3f);
            corner = DoorPoint() + fwd * (car.Length / 2f + 0.6f - Vector3.Dot(DoorPoint() - car.transform.position, fwd));
            viaCorner = true;
            spotFacing = new Vector3(-Mathf.Sign(spot.x), 0f, 0f);
            traffic.Pedestrians.Add(walker.transform);
            step = Step.ToSpot;
        }

        void GetIn()
        {
            traffic.Pedestrians.Remove(walker.transform);
            Destroy(walker.gameObject);
            walker = null;
            if (seated != null) seated.gameObject.SetActive(true);
            car.Continue(sideA ? OutA() : OutB(), false);
            sideA = !sideA;
            step = Step.Leaving;
        }

        void UpdateSquats(float dt)
        {
            if (walker == null) { Reset(); return; }
            repTimer += dt;
            float phase = repTimer / RepTime;
            float t = (1f - Mathf.Cos(phase * Mathf.PI * 2f)) / 2f;
            walker.Squat(t);
            if (repTimer >= RepTime)
            {
                repTimer -= RepTime;
                reps++;
                if (reps >= Reps)
                {
                    walker.Stand();
                    SpeechBubble.Show(walker.transform, Done[Random.Range(0, Done.Length)], 1.95f);
                    viaCorner = true;
                    step = Step.ToCar;
                    return;
                }
                string say = reps % 10 == 0 ? $"{reps}! " + Tens[Random.Range(0, Tens.Length)] : reps.ToString();
                SpeechBubble.Show(walker.transform, say, 1.95f, Vector3.zero, Shapes.Hex("#ffe14d"), reps % 10 == 0 ? 3f : 1.1f);
            }
        }

        /// <summary>Идёт к точке; true — дошёл.</summary>
        bool WalkTo(Vector3 target, float dt)
        {
            if (walker == null) return false;
            var p = walker.transform.position;
            var d = target - p;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist < 0.08f)
            {
                walker.Stand();
                return true;
            }
            var dir = d / dist;
            walker.transform.position = p + dir * Mathf.Min(dist, 1.5f * dt);
            Face(dir);
            walkPhase += dt * 7f;
            walker.Walk(walkPhase);
            return false;
        }

        void Face(Vector3 dir)
        {
            if (walker == null || dir.sqrMagnitude < 1e-4f) return;
            walker.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
        }

        void Reset()
        {
            if (walker != null)
            {
                traffic.Pedestrians.Remove(walker.transform);
                Destroy(walker.gameObject);
            }
            walker = null;
            if (car != null) car.Continue(sideA ? OutA() : OutB(), false);
            car = null;
            step = Step.Waiting;
            timer = 30f;
        }
    }
}
