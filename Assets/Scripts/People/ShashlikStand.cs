using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Шашлычная у дороги вдоль очереди: мангал с дымом под тентом, два шашлычника (один машет картонкой,
    /// второй крутит шампуры), щит с ценой, пластиковый столик. Водители выходят из машин, заказывают,
    /// едят у столика и возвращаются — их машина всё это время стоит, очередь уходит вперёд.
    /// Игрок тоже может выйти и заказать (только наличными), а потом есть на ходу.
    /// </summary>
    public class ShashlikStand : MonoBehaviour
    {
        public static ShashlikStand Instance { get; private set; }
        public const int Price = 350;

        static readonly string[] CookLines =
        {
            "Барев, ахпер! Шашлык — огонь, клянусь мамой!", "Ара, подходи! Очередь стоит, а шашлык не ждёт!",
            "Джан, такой свинины в Ереване не найдёшь!", "Вай, какой дымок! Чувствуешь, брат-джан?",
            "Цавд танем, покушай — тебе ещё стоять и стоять!", "Бензина нет — шашлык есть! Шат лав, слушай!",
            "Ахпер, люля-кебаб, лаваш, лучок — всё как дома!", "Ара, Гарик, переворачивай, сгорит же, вай!",
            "Пять минут, джан, — и ты самый счастливый человек на этой очереди!", "Слушай, зачем тебе бензин? Покушай сначала!",
        };
        static readonly string[] ServeLines =
        {
            "Держи, ахпер! Шат лав будет, клянусь!", "Кушай на здоровье, джан!", "Горячий, осторожно, цавд танем!",
            "Вай, какой шашлык! Сам бы ел, но тебе отдаю!", "Приятного, брат-джан! Приходи ещё!", "Апрес! Лучший шашлык на всей очереди!",
        };
        /// <summary>Пока жарится заказ игрока.</summary>
        static readonly string[] CookingLines =
        {
            "Сейчас, джан! Самый сочный тебе положу.", "Ара, Гарик, давай самый лучший шампур ахперу!", "Минутку, брат-джан, мясо любит терпение. Как очередь.",
        };

        TrafficManager traffic;
        HumanRig fanner, turner;
        float lineTimer = 3f, spawnTimer = 12f;
        bool announced;
        readonly List<ShashlikCustomer> customers = new List<ShashlikCustomer>();

        // Заказ игрока: 0 — нет, 1 — жарится, 2 — готов
        public int PlayerOrder { get; private set; }
        float playerReadyAt;
        public float PlayerWaitLeft => Mathf.Max(0f, playerReadyAt - Time.time);
        public float CookTime => 10f / Mathf.Max(1f, traffic.Settings.Speedup);

        public static void Build(Transform root, TrafficManager traffic)
        {
            var g = Shapes.Group("Shashlik", root, CityLayout.MangalSpot);
            var stand = g.gameObject.AddComponent<ShashlikStand>();
            stand.traffic = traffic;
            Instance = stand;
            stand.BuildProps(g, root);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void BuildProps(Transform g, Transform root)
        {
            var steel = Shapes.Hex("#2a2a2e");
            // Мангал на ножках вдоль дороги, угли светятся, шампуры поперёк
            Shapes.Box(g, new Vector3(0f, 0.85f, 0f), new Vector3(0.45f, 0.28f, 1.7f), steel, name: "Mangal");
            Shapes.Box(g, new Vector3(0f, 1.0f, 0f), new Vector3(0.36f, 0.02f, 1.6f), Shapes.Hex("#ff6a1c"), name: "Coals");
            foreach (var p in new[] { new Vector3(-0.18f, 0f, -0.75f), new Vector3(0.18f, 0f, -0.75f), new Vector3(-0.18f, 0f, 0.75f), new Vector3(0.18f, 0f, 0.75f) })
                Shapes.Box(g, p + new Vector3(0f, 0.36f, 0f), new Vector3(0.04f, 0.72f, 0.04f), steel, name: "Leg");
            for (int i = 0; i < 7; i++)
            {
                float z = -0.66f + i * 0.22f;
                Shapes.Box(g, new Vector3(0f, 1.04f, z), new Vector3(0.62f, 0.012f, 0.012f), Shapes.Hex("#b8bcc2"), name: "Skewer");
                for (int k = 0; k < 4; k++)
                    Shapes.Box(g, new Vector3(-0.15f + k * 0.1f, 1.05f, z), new Vector3(0.07f, 0.065f, 0.065f), Shapes.Hex(k % 2 == 0 ? "#7a3b1e" : "#5a2a14"), name: "Meat");
            }
            Obstacles.AddBox(CityLayout.MangalSpot, 0.55f, 1.8f, "мангал");
            var smoke = g.gameObject.AddComponent<Smoke>();
            smoke.Init(g, new Vector3(0f, 1.15f, 0f), root);
            smoke.intensity = 0.4f;
            smoke.sizeScale = 0.8f;

            // Тент в красно-белую полоску
            foreach (var p in new[] { new Vector3(-0.8f, 0f, -2.2f), new Vector3(-0.8f, 0f, 2.2f), new Vector3(3.2f, 0f, -2.2f), new Vector3(3.2f, 0f, 2.2f) })
            {
                Shapes.Box(g, p + new Vector3(0f, 1.3f, 0f), new Vector3(0.06f, 2.6f, 0.06f), Shapes.Hex("#d8d8d8"), name: "TentPole");
                Obstacles.AddBox(CityLayout.MangalSpot + p, 0.15f, 0.15f, "стойка тента");
            }
            for (int i = 0; i < 5; i++)
                Shapes.Box(g, new Vector3(1.2f, 2.65f, -2.2f + i * 0.88f + 0.44f), new Vector3(4.4f, 0.08f, 0.88f), i % 2 == 0 ? Shapes.Hex("#c8312b") : Color.white, name: "Tent");

            // Щит с ценой — к дороге
            var sign = Shapes.Group("ShashlikSign", g, new Vector3(-1.4f, 0f, 3.1f), new Vector3(0f, 90f, 0f));
            Shapes.Box(sign, new Vector3(0f, 0.8f, 0f), new Vector3(0.08f, 1.6f, 0.08f), steel);
            Shapes.Box(sign, new Vector3(0f, 1.75f, 0f), new Vector3(1.6f, 1.1f, 0.06f), Shapes.Hex("#f2c81a"));
            var text = Fonts.WorldText(sign, new Vector3(0f, 1.75f, -0.05f), "ШАШЛЫК\nлюля · лаваш\n" + Price + " руб. — наличными", Shapes.Hex("#7a1e12"), 0.022f);
            text.transform.localRotation = Quaternion.identity;
            Obstacles.AddBox(CityLayout.MangalSpot + new Vector3(-1.4f, 0f, 3.1f), 0.2f, 0.2f, "щит шашлычной");

            // Высокая вывеска на столбе — видно издалека из очереди
            var tall = Shapes.Group("ShashlikTallSign", g, new Vector3(-0.9f, 0f, -3.4f), new Vector3(0f, 90f, 0f));
            Shapes.Make(PrimitiveType.Cylinder, tall, new Vector3(0f, 2.6f, 0f), new Vector3(0.14f, 2.6f, 0.14f), steel);
            Shapes.Box(tall, new Vector3(0f, 5.6f, 0f), new Vector3(3.6f, 1.3f, 0.12f), Shapes.Hex("#c8312b"));
            foreach (float face in new[] { -1f, 1f })
            {
                var big = Fonts.WorldText(tall, new Vector3(0f, 5.6f, 0.08f * face), "ШАШЛЫК", Color.white, 0.09f);
                big.transform.localRotation = Quaternion.Euler(0f, face > 0f ? 180f : 0f, 0f);
            }
            Obstacles.AddBox(CityLayout.MangalSpot + new Vector3(-0.9f, 0f, -3.4f), 0.3f, 0.3f, "столб вывески");

            // Пластиковый столик и стулья
            var table = CityLayout.ShashlikTable - CityLayout.MangalSpot;
            Shapes.Box(g, table + new Vector3(0f, 0.72f, 0f), new Vector3(1.0f, 0.04f, 1.0f), Color.white, name: "Table");
            Shapes.Make(PrimitiveType.Cylinder, g, table + new Vector3(0f, 0.36f, 0f), new Vector3(0.08f, 0.36f, 0.08f), Color.white, name: "TableLeg");
            foreach (var c in new[] { new Vector3(-0.9f, 0f, 0f), new Vector3(0.9f, 0f, 0f) })
            {
                Shapes.Box(g, table + c + new Vector3(0f, 0.42f, 0f), new Vector3(0.45f, 0.04f, 0.45f), Color.white, name: "Chair");
                Shapes.Box(g, table + c + new Vector3(Mathf.Sign(c.x) * 0.22f, 0.7f, 0f), new Vector3(0.04f, 0.55f, 0.45f), Color.white, name: "ChairBack");
            }
            Obstacles.AddBox(CityLayout.ShashlikTable, 1.0f, 1.0f, "столик");

            // «Жигули» шашлычников за тентом
            var car = CarFactory.Build("Shashlik car", Shapes.Hex("#e3dccb"), CarModel.Vaz2107, false);
            car.transform.SetParent(g, false);
            car.transform.position = CityLayout.MangalSpot + new Vector3(6.5f, 0f, -5.5f);
            car.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            Obstacles.Add(new Obb(car.transform.position, car.transform.forward, car.width, car.length), "машина шашлычников");

            // Шашлычники: Ашот машет картонкой, Гарик крутит шампуры
            fanner = Cook(g, "Ашот", new Vector3(1.05f, 0f, -0.45f), Shapes.Hex("#f2f2ee"));
            turner = Cook(g, "Гарик", new Vector3(1.05f, 0f, 0.55f), Shapes.Hex("#2b2f3a"));
            var cardboard = Shapes.Group("Cardboard", fanner.armR, new Vector3(0f, -0.65f, 0.08f));
            Shapes.Box(cardboard, Vector3.zero, new Vector3(0.02f, 0.3f, 0.35f), Shapes.Hex("#b08a5a"));
        }

        static HumanRig Cook(Transform g, string name, Vector3 local, Color shirt)
        {
            var look = HumanRig.RandomLook();
            look.shirt = shirt;
            look.hair = Shapes.Hex("#1b1b1b");
            var rig = HumanRig.Build(name, g, look);
            rig.transform.localPosition = local;
            rig.transform.rotation = Quaternion.Euler(0f, -90f, 0f); // лицом к мангалу и дороге
            Shapes.Box(rig.body, new Vector3(0f, 1.05f, 0.14f), new Vector3(0.4f, 0.55f, 0.02f), Color.white, name: "Apron");
            return rig;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            float t = Time.time;
            fanner.Animate(0f, dt);
            turner.Animate(0f, dt);
            // Машет картонкой над углями
            fanner.armR.localRotation = Quaternion.Euler(-75f + Mathf.Sin(t * 9f) * 22f, 0f, 12f);
            // Крутит шампуры
            turner.armL.localRotation = Quaternion.Euler(-55f, 0f, -10f);
            turner.armR.localRotation = Quaternion.Euler(-55f + Mathf.Sin(t * 2.3f) * 8f, 0f, 10f);

            var gm = GameManager.Instance;
            if (gm == null) return;
            var playerPos = gm.OnFoot && gm.Walker != null ? gm.Walker.transform.position : traffic.Player.Position;

            // Один раз подсказываем, что это и как купить
            if (!announced && Vector3.Distance(playerPos, CityLayout.MangalSpot) < 60f)
            {
                announced = true;
                gm.ShowMessage("Справа у дороги — шашлычная! Выйдите из машины (F), подойдите к мангалу и нажмите E. Только наличными.", 9f);
            }

            // Зазывают, когда игрок рядом
            lineTimer -= dt;
            if (lineTimer <= 0f && Vector3.Distance(playerPos, CityLayout.MangalSpot) < 16f)
            {
                lineTimer = Random.Range(7f, 12f);
                Speak(Random.value < 0.5f ? fanner : turner, CookLines[Random.Range(0, CookLines.Length)], false);
            }

            // Заказ игрока готов
            if (PlayerOrder == 1 && Time.time >= playerReadyAt)
            {
                PlayerOrder = 2;
                if (gm.OnFoot && Vector3.Distance(playerPos, CityLayout.ShashlikOrder) < 4f) HandToPlayer(gm);
                else gm.ShowMessage("Ашот: «Ахпер-джан, твой шашлык готов! Забирай, пока горячий, вай!»", 6f);
            }

            UpdateCustomers(dt);
        }

        // ---------- Игрок ----------

        /// <summary>Игрок заказал люля-кебаб, а не шашлык.</summary>
        public bool PlayerLula { get; private set; }

        public void OrderForPlayer(bool lula)
        {
            PlayerOrder = 1;
            PlayerLula = lula;
            playerReadyAt = Time.time + CookTime;
            Speak(turner, CookingLines[Random.Range(0, CookingLines.Length)], true);
        }

        /// <summary>Игрок у мангала, а заказ готов — отдаём шампур.</summary>
        public bool HandToPlayer(GameManager gm)
        {
            if (PlayerOrder != 2) return false;
            PlayerOrder = 0;
            Speak(fanner, ServeLines[Random.Range(0, ServeLines.Length)], true);
            gm.OnShashlikServed();
            return true;
        }

        // ---------- Водители из очереди ----------

        void UpdateCustomers(float dt)
        {
            customers.RemoveAll(c => c == null);
            spawnTimer -= dt;
            // Не больше двух сразу (водители, бросившие машину, — редкость: их объезжают)
            if (spawnTimer > 0f || customers.Count >= 2) return;
            spawnTimer = Random.Range(35f, 60f) / Mathf.Max(1f, traffic.Settings.Speedup * 0.6f);
            NpcCar best = null;
            float bestD = float.MaxValue;
            foreach (var npc in traffic.Npcs)
            {
                if (npc.Role != NpcRole.Queue || npc.Path != traffic.QueuePath || npc.Speed > 0.1f || npc.IsVip || npc.IsGas) continue;
                if (npc.Service != ServiceKind.None || npc.visual.driverHead == null || !npc.visual.driverHead.gameObject.activeSelf) continue;
                float d = Mathf.Abs(npc.Position.z - CityLayout.ShashlikZ);
                if (d > 70f || d < 6f) continue;
                bool taken = false;
                foreach (var c in customers) if (c.Car == npc) taken = true;
                if (taken) continue;
                d += Random.Range(0f, 25f);
                if (d < bestD) { bestD = d; best = npc; }
            }
            if (best == null) return;
            // Обычно сходить за шашлыком отправляют пассажира — машина остаётся в очереди и едет дальше
            bool passenger = best.visual.passengerHead != null && best.visual.passengerHead.gameObject.activeSelf && Random.value < 0.75f;
            customers.Add(ShashlikCustomer.Spawn(best, traffic, this, customers.Count, passenger));
        }

        /// <summary>Шашлык для водителя готов через CookTime после заказа.</summary>
        public void Serve(HumanRig to) => Speak(fanner, ServeLines[Random.Range(0, ServeLines.Length)], false);

        // ---------- Реплики: говорит один за раз ----------

        /// <summary>До какого момента ещё висит реплика одного из поваров.</summary>
        float speechUntil;
        HumanRig lastSpeaker;

        /// <summary>
        /// Повара стоят рядом — две реплики сразу перекрывают друг друга. Поэтому говорит один:
        /// пока висит прошлая реплика, новая пропускается, а важная (заказ игрока, «держи шашлык»)
        /// снимает прошлую и говорит сама.
        /// </summary>
        void Speak(HumanRig who, string text, bool important)
        {
            if (who == null) return;
            if (Time.time < speechUntil)
            {
                if (!important) return;
                if (lastSpeaker != null && lastSpeaker != who)
                    foreach (var b in lastSpeaker.GetComponentsInChildren<SpeechBubble>()) Destroy(b.gameObject);
            }
            SpeechBubble.Show(who.transform, text, 1.5f);
            lastSpeaker = who;
            speechUntil = Time.time + 3.5f;
            // Зазывания — не сразу после важной реплики
            lineTimer = Mathf.Max(lineTimer, 4f);
        }
    }

    /// <summary>
    /// Водитель из очереди сходил за шашлыком: вышел, обошёл машину сзади, заказал у мангала, поел у столика,
    /// вернулся. Пока его нет, машина стоит (а очередь впереди уезжает — сзади бибикают).
    /// </summary>
    public class ShashlikCustomer : MonoBehaviour
    {
        enum Step { ToStand, Waiting, ToTable, Eating, ToCar }

        static readonly string[] GoLines = { "Пойду шашлычка возьму, всё равно стоим.", "Да ну эту очередь, есть хочу!", "Я быстро! Место моё!" };
        static readonly string[] OrderLines = { "Две палки свиной!", "Мне один, с лучком.", "Люля есть? Давай люля.", "Ахпер, мне как себе!" };
        static readonly string[] EatLines = { "М-м-м, вот это шашлык!", "Лучше, чем бензин!", "Ради этого стоило стоять." };
        static readonly string[] HurryLines = { "Ой, очередь пошла! Давай на вынос!", "Моя машина! Бегу-бегу!", "Не занимайте, я тут стоял!" };
        bool hurrying;

        public NpcCar Car { get; private set; }
        /// <summary>Сходил пассажир: машина едет дальше в очереди, он догоняет. Иначе — водитель, машина стоит.</summary>
        public bool Passenger { get; private set; }
        float heldTime, bypassTimer;
        TrafficManager traffic;
        ShashlikStand stand;
        HumanRig rig;
        Step step;
        float timer, readyAt;
        readonly List<Vector3> route = new List<Vector3>();
        Vector3 slot, door, rearLeft, rearRight;
        ShashlikSkewer skewer;

        public static ShashlikCustomer Spawn(NpcCar car, TrafficManager traffic, ShashlikStand stand, int index, bool passenger)
        {
            var rig = HumanRig.Build("Shashlik customer", traffic.WorldRoot, HumanRig.RandomLook());
            var c = rig.gameObject.AddComponent<ShashlikCustomer>();
            c.Car = car;
            c.traffic = traffic;
            c.stand = stand;
            c.rig = rig;
            c.door = car.DriverDoor;
            c.door.y = 0f;
            var tr = car.transform;
            c.rearLeft = tr.TransformPoint(new Vector3(-car.Width / 2f - 0.5f, 0f, -car.Length / 2f - 0.7f));
            c.rearRight = new Vector3(12.6f, 0f, c.rearLeft.z);
            c.rearLeft.y = 0f;
            c.slot = CityLayout.ShashlikOrder + new Vector3(0f, 0f, -1.2f - index * 1.1f);
            c.Passenger = passenger;
            if (passenger)
            {
                // Пассажир сидит справа — выходит прямо к тротуару
                c.door = c.PassengerDoor();
                rig.transform.position = c.door;
                SetPassengerVisible(car, false);
                c.Go(Step.ToStand, new Vector3(12.6f, 0f, c.door.z), c.slot);
            }
            else
            {
                rig.transform.position = c.door;
                SetDriverVisible(car, false);
                c.Go(Step.ToStand, c.rearLeft, c.rearRight, c.slot);
            }
            traffic.Pedestrians.Add(rig.transform);
            SpeechBubble.Show(rig.transform, GoLines[Random.Range(0, GoLines.Length)], 1.5f);
            return c;
        }

        Vector3 PassengerDoor()
        {
            var p = Car.transform.TransformPoint(new Vector3(Car.Width / 2f + 0.45f, 0f, 0.1f));
            p.y = 0f;
            return p;
        }

        static void SetPassengerVisible(NpcCar car, bool visible)
        {
            if (car == null) return;
            if (car.visual.passengerHead != null) car.visual.passengerHead.gameObject.SetActive(visible);
            if (car.visual.passengerTorso != null) car.visual.passengerTorso.gameObject.SetActive(visible);
        }

        static void SetDriverVisible(NpcCar car, bool visible)
        {
            if (car == null) return;
            if (car.visual.driverHead != null) car.visual.driverHead.gameObject.SetActive(visible);
            if (car.visual.driverTorso != null) car.visual.driverTorso.gameObject.SetActive(visible);
        }

        void Go(Step next, params Vector3[] points)
        {
            step = next;
            route.Clear();
            route.AddRange(points);
            timer = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            timer += dt;
            if (Car == null) { Destroy(gameObject); return; }
            if (rig.Knocked) { rig.Animate(0f, dt); if (!Passenger) Car.Hold(0.5f); return; } // сбила машина — лежит
            if (Passenger)
            {
                // Машина едет дальше в очереди. Уехала далеко от мангала — пассажир спешит её догнать
                if (!hurrying && step != Step.ToStand && (Car.Role != NpcRole.Queue || Mathf.Abs(Car.Position.z - CityLayout.ShashlikZ) > 28f))
                {
                    hurrying = true;
                    if (step != Step.ToCar) GoToCar();
                    SpeechBubble.Show(transform, HurryLines[Random.Range(0, HurryLines.Length)], 1.5f);
                }
            }
            else
            {
                // Машина без водителя никуда не едет — а соседи сзади объезжают её по левой полосе
                Car.Hold(0.5f);
                heldTime += dt;
                bypassTimer -= dt;
                float gap = GapAhead();
                if (heldTime > 5f && bypassTimer <= 0f && gap > 9f) TryBypass();
                // Совсем огромная дыра — всё-таки бегом обратно с шашлыком «на вынос»
                if (!hurrying && gap > 25f)
                {
                    if (step == Step.ToCar) hurrying = true;
                    else Hurry();
                }
            }
            float speed = 0f;
            switch (step)
            {
                case Step.ToStand:
                    speed = Walk(dt);
                    if (speed == 0f)
                    {
                        step = Step.Waiting;
                        readyAt = Time.time + stand.CookTime * Random.Range(0.9f, 1.4f);
                        SpeechBubble.Show(transform, OrderLines[Random.Range(0, OrderLines.Length)], 1.5f);
                    }
                    break;
                case Step.Waiting:
                    Face(CityLayout.MangalSpot, dt);
                    if (Time.time >= readyAt)
                    {
                        stand.Serve(rig);
                        skewer = gameObject.AddComponent<ShashlikSkewer>();
                        skewer.Init(rig, lula: Random.value < 0.3f);
                        skewer.BiteInterval = 1.6f;
                        Go(Step.ToTable, CityLayout.ShashlikTable + new Vector3(-0.9f, 0f, Random.Range(-0.6f, 0.6f)));
                    }
                    break;
                case Step.ToTable:
                    speed = Walk(dt);
                    if (speed == 0f) { step = Step.Eating; timer = 0f; }
                    break;
                case Step.Eating:
                    Face(CityLayout.ShashlikTable, dt);
                    if (timer > 3f && timer - dt <= 3f && Random.value < 0.6f) SpeechBubble.Show(transform, EatLines[Random.Range(0, EatLines.Length)], 1.5f);
                    if (skewer == null || skewer.Finished)
                    {
                        if (skewer != null) skewer.Throw();
                        GoToCar();
                    }
                    break;
                case Step.ToCar:
                    // Пассажир идёт к правой двери машины, которая могла проехать вперёд
                    if (Passenger && route.Count > 0 && (route[route.Count - 1] - PassengerDoor()).sqrMagnitude > 1f) GoToCar();
                    speed = Walk(dt);
                    if (speed == 0f)
                    {
                        if (Passenger) SetPassengerVisible(Car, true);
                        else SetDriverVisible(Car, true);
                        Destroy(gameObject);
                        return;
                    }
                    break;
            }
            rig.Animate(speed, dt);
        }

        void OnDestroy()
        {
            if (traffic != null) traffic.Pedestrians.Remove(transform);
            if (skewer != null) skewer.Throw();
        }

        /// <summary>Маршрут к своей машине: пассажир — по тротуару к правой двери, водитель — вокруг машины к левой.</summary>
        void GoToCar()
        {
            if (Passenger)
            {
                var d = PassengerDoor();
                var here = transform.position;
                if (here.x > 11.6f) Go(Step.ToCar, new Vector3(12.6f, 0f, d.z), d);
                else Go(Step.ToCar, d);
                return;
            }
            Go(Step.ToCar, rearRight, rearLeft, door);
        }

        /// <summary>Сосед сзади объезжает брошенную машину и встаёт в очередь перед ней.</summary>
        void TryBypass()
        {
            bypassTimer = 4f;
            NpcCar behind = null;
            float bestS = float.MinValue;
            foreach (var n in traffic.Npcs)
                if (n != Car && n.Path == traffic.QueuePath && n.Role == NpcRole.Queue && n.S < Car.S && n.S > Car.S - 14f && n.S > bestS)
                {
                    bestS = n.S;
                    behind = n;
                }
            if (behind == null) return;
            // Во втором ряду рядом никого, иначе подождём
            var mid = traffic.MiddlePath;
            float ms = mid.Project(behind.Position, out _);
            if (!traffic.LaneClearNear(mid, ms, 14f, behind)) return;
            behind.BypassStalled(Car.S + Car.Length / 2f + 2f);
        }

        /// <summary>Свободное место перед его машиной в очереди.</summary>
        float GapAhead()
        {
            var q = traffic.QueuePath;
            if (Car.Path != q) return 0f;
            float front = Car.S + Car.Length / 2f, best = float.MaxValue;
            foreach (var n in traffic.Npcs)
                if (n != Car && n.Path == q && n.Role == NpcRole.Queue && n.S > Car.S)
                    best = Mathf.Min(best, n.S - n.Length / 2f - front);
            if (traffic.PlayerInQueue && traffic.PlayerQueueS > Car.S)
                best = Mathf.Min(best, traffic.PlayerQueueS - traffic.Player.Length / 2f - front);
            return best == float.MaxValue ? 0f : best;
        }

        void Hurry()
        {
            hurrying = true;
            SpeechBubble.Show(transform, HurryLines[Random.Range(0, HurryLines.Length)], 1.5f);
            if (skewer == null && step != Step.ToStand)
            {
                // Не дождался у мангала — забирает «на вынос» недожаренный
                stand.Serve(rig);
                skewer = gameObject.AddComponent<ShashlikSkewer>();
                skewer.Init(rig, lula: Random.value < 0.3f);
                skewer.BiteInterval = 1.6f;
            }
            var here = transform.position;
            var tr = Car.transform;
            var rearL = tr.TransformPoint(new Vector3(-Car.Width / 2f - 0.5f, 0f, -Car.Length / 2f - 0.7f));
            rearL.y = 0f;
            var side = new Vector3(12.6f, 0f, rearL.z);
            var d = Car.DriverDoor;
            d.y = 0f;
            Go(Step.ToCar, here.x > 12.8f ? new[] { side, rearL, d } : new[] { rearL, d });
        }

        float Walk(float dt)
        {
            while (route.Count > 0)
            {
                var to = route[0] - transform.position;
                to.y = 0f;
                if (to.magnitude > 0.1f)
                {
                    float sp = hurrying ? 3.6f : 1.6f;
                    transform.position += to.normalized * Mathf.Min(sp * dt, to.magnitude);
                    Face(route[0], dt);
                    return sp;
                }
                route.RemoveAt(0);
            }
            return 0f;
        }

        void Face(Vector3 point, float dt)
        {
            var to = point - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), dt * 8f);
        }
    }
}
