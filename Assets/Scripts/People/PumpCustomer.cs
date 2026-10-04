using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Водитель NPC на заправке: выходит, вставляет пистолет, идёт в магазин, стоит в очереди в кассу,
    /// платит, возвращается, ждёт, пока нальётся, вешает пистолет и уезжает.
    /// Его можно ударить: дерётся в ответ, а если упадёт в очереди — игрок занимает его место.
    /// </summary>
    public class PumpCustomer : MonoBehaviour
    {
        enum Step { ToNozzle, Nozzle, ToShop, InLine, Paying, ToPump, WaitFuel, HangUp, ToCar }

        static readonly string[] PayLines =
        {
            "Колонка такая-то, до полного.", "На тысячу, девяносто пятый.", "Карту не берёт? А наличкой?",
            "Ещё сигареты и кофе.", "Чек не надо.", "Можно побыстрее? Меня там бибикают.",
        };
        static readonly string[] LineLines =
        {
            "Почему одна касса?!", "Девушка, побыстрее можно?", "Я за вами занимал!", "Опять терминал виснет...",
            "Тут вообще очередь движется?", "Как в девяностые, ей-богу.",
        };
        static readonly string[] BreakLines = { "Какой ещё перерыв?!", "Пятнадцать минут — это сколько по-вашему?", "Безобразие!" };
        static readonly string[] HitLines = { "Ты чё творишь?!", "Охрана!", "Мужик, ты больной?!", "Полиция!", "Ну всё, держись!" };
        static readonly string[] UpLines = { "Псих... Ладно, в конец так в конец.", "Я на тебя жалобу напишу!", "Ну и очередь у вас тут..." };

        public static readonly List<PumpCustomer> All = new List<PumpCustomer>();

        public bool Paid { get; private set; }
        public bool Done { get; private set; }
        public Fighter Fighter { get; private set; }

        NpcCar car;
        TrafficManager traffic;
        HumanRig rig;
        Step step;
        float timer;
        float walkSpeed;
        readonly List<Vector3> route = new List<Vector3>();
        Vector3 stand, rearRight, rearLeft;
        Transform hose;
        float lastHealth;
        float angryTimer;
        float lineTalkTimer;
        bool wasDown;

        const float CorridorZ = -3.3f;
        static readonly Vector3 DoorOutside = new Vector3(CityLayout.ShopMinX - 1.3f, 0f, CityLayout.ShopDoorZ);
        static readonly Vector3 DoorInside = new Vector3(CityLayout.ShopMinX + 0.9f, 0f, CityLayout.ShopDoorZ);

        public static PumpCustomer Spawn(NpcCar car, TrafficManager traffic, bool alreadyPaid)
        {
            if (car.Pump == null || car.visual.driverHead == null || !car.visual.driverHead.gameObject.activeSelf) return null;
            var rig = HumanRig.Build("Customer", traffic.WorldRoot, HumanRig.RandomLook());
            var c = rig.gameObject.AddComponent<PumpCustomer>();
            c.car = car;
            c.traffic = traffic;
            c.rig = rig;
            c.Fighter = Fighter.AddTo(rig, "Водитель");
            c.lastHealth = c.Fighter.Health;
            c.walkSpeed = Mathf.Min(2.6f, 1.5f * Mathf.Sqrt(traffic.Settings.Speedup));

            var t = car.transform;
            float hw = car.Width / 2f, hl = car.Length / 2f;
            c.stand = t.TransformPoint(new Vector3(hw + 0.45f, 0f, -hl + 1.0f));
            c.rearRight = t.TransformPoint(new Vector3(hw + 0.6f, 0f, -hl - 0.8f));
            c.rearLeft = t.TransformPoint(new Vector3(-hw - 0.6f, 0f, -hl - 0.8f));
            c.stand.y = c.rearRight.y = c.rearLeft.y = 0f;

            c.hose = Shapes.Make(PrimitiveType.Cylinder, rig.transform.parent, Vector3.zero, Vector3.one, Shapes.Hex("#1b1b1b"), name: "Hose").transform;
            c.hose.gameObject.SetActive(false);

            SetDriverVisible(car, false);
            if (alreadyPaid)
            {
                c.Paid = true;
                rig.transform.position = c.stand;
                c.step = Step.WaitFuel;
                c.hose.gameObject.SetActive(true);
            }
            else
            {
                var door = car.DriverDoor;
                door.y = 0f;
                rig.transform.position = door;
                c.Go(Step.ToNozzle, c.rearLeft, c.rearRight, c.stand);
            }
            traffic.Pedestrians.Add(rig.transform);
            All.Add(c);
            return c;
        }

        static void SetDriverVisible(NpcCar car, bool visible)
        {
            if (car == null) return;
            if (car.visual.driverHead != null) car.visual.driverHead.gameObject.SetActive(visible);
            if (car.visual.driverTorso != null) car.visual.driverTorso.gameObject.SetActive(visible);
        }

        void OnDestroy()
        {
            All.Remove(this);
            CashierLine.Leave(this);
            if (hose != null) Destroy(hose.gameObject);
        }

        void Go(Step next, params Vector3[] points)
        {
            step = next;
            route.Clear();
            route.AddRange(points);
            timer = 0f;
        }

        void Say(string[] lines) => SpeechBubble.Show(transform, lines[Random.Range(0, lines.Length)], 1.5f);

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (car == null) { Destroy(gameObject); return; }
            timer += dt;
            float speed = 0f;

            if (Fighter.Down)
            {
                if (!wasDown) KnockedDown();
                wasDown = true;
                rig.Animate(0f, dt);
                return;
            }
            if (wasDown)
            {
                // Встал, отряхнулся — в конец очереди (или дальше по своим делам)
                wasDown = false;
                Say(UpLines);
                if (step == Step.InLine || step == Step.Paying) Go(Step.InLine);
            }

            if (Fighter.Health < lastHealth - 0.01f)
            {
                angryTimer = 6f;
                Say(HitLines);
            }
            lastHealth = Fighter.Health;
            if (angryTimer > 0f && FightBack(dt)) return;

            switch (step)
            {
                case Step.ToNozzle:
                    speed = Walk(dt);
                    if (speed == 0f) { step = Step.Nozzle; timer = 0f; }
                    break;
                case Step.Nozzle:
                    Face(car.Pump != null ? car.Pump.dispenser : stand, dt);
                    if (timer > 1.2f)
                    {
                        hose.gameObject.SetActive(true);
                        Go(Step.ToShop, rearRight, new Vector3(rearRight.x, 0f, CorridorZ),
                            new Vector3(DoorOutside.x, 0f, CorridorZ), DoorOutside, DoorInside);
                    }
                    break;
                case Step.ToShop:
                    speed = Walk(dt);
                    if (speed == 0f)
                    {
                        CashierLine.Join(this);
                        step = Step.InLine;
                        lineTalkTimer = Random.Range(6f, 14f);
                    }
                    break;
                case Step.InLine:
                {
                    int i = CashierLine.Join(this);
                    var slot = CashierLine.Slot(i);
                    speed = MoveTo(slot, walkSpeed * 0.7f, 0.12f, dt);
                    if (speed == 0f) Face(CashierLine.FacingPoint(i), dt);
                    lineTalkTimer -= dt;
                    if (lineTalkTimer <= 0f)
                    {
                        lineTalkTimer = Random.Range(10f, 22f);
                        if (Random.value < 0.5f) Say(CashierLine.CashierAway ? BreakLines : LineLines);
                    }
                    if (i == 0 && speed == 0f && !CashierLine.CashierAway)
                    {
                        step = Step.Paying;
                        timer = 0f;
                        Say(PayLines);
                    }
                    break;
                }
                case Step.Paying:
                    Face(CityLayout.CashierSpot, dt);
                    if (CashierLine.CashierAway) { step = Step.InLine; break; }
                    if (timer > Mathf.Max(1.5f, 5f / traffic.Settings.Speedup))
                    {
                        Paid = true;
                        CashierLine.Leave(this);
                        if (CashierLine.Cashier != null && Random.value < 0.5f)
                            SpeechBubble.Show(CashierLine.Cashier, "Следующий!", 1.5f);
                        Go(Step.ToPump, DoorInside, DoorOutside, new Vector3(DoorOutside.x, 0f, CorridorZ),
                            new Vector3(rearRight.x, 0f, CorridorZ), rearRight, stand);
                    }
                    break;
                case Step.ToPump:
                    speed = Walk(dt);
                    if (speed == 0f) { step = Step.WaitFuel; timer = 0f; }
                    break;
                case Step.WaitFuel:
                    Face(car.Position, dt);
                    if (car.FuelProgress >= 1f) { step = Step.HangUp; timer = 0f; }
                    break;
                case Step.HangUp:
                    if (timer > 1f)
                    {
                        hose.gameObject.SetActive(false);
                        var door = car.DriverDoor;
                        door.y = 0f;
                        Go(Step.ToCar, rearRight, rearLeft, door);
                    }
                    break;
                case Step.ToCar:
                    speed = Walk(dt);
                    if (speed == 0f)
                    {
                        SetDriverVisible(car, true);
                        Done = true;
                        Destroy(gameObject);
                        return;
                    }
                    break;
            }

            UpdateHose();
            rig.Animate(speed, dt);
        }

        /// <summary>Ударили — отвечает, пока игрок рядом. Возвращает true, если сейчас дерётся.</summary>
        bool FightBack(float dt)
        {
            angryTimer -= dt;
            var gm = GameManager.Instance;
            if (gm == null || !gm.OnFoot || gm.Walker == null || !gm.Walker.Active) return false;
            var me = gm.Walker;
            var to = me.transform.position - transform.position;
            if (Mathf.Abs(to.y) > 0.6f) return false;
            to.y = 0f;
            if (to.magnitude > 2.5f) return false;
            Face(me.transform.position, dt);
            if (to.magnitude < 1.3f && Fighter.CanPunch && Random.value < 0.6f)
                Fighter.Punch(me.Fighter, 5f, 10f, Random.Range(1.0f, 1.6f));
            UpdateHose();
            rig.Animate(0f, dt);
            return true;
        }

        void KnockedDown()
        {
            int index = CashierLine.IndexOf(this);
            if (index < 0) return;
            CashierLine.Leave(this);
            GameManager.Instance?.OnLineVictimDown(index, transform.position);
        }

        void UpdateHose()
        {
            if (!hose.gameObject.activeSelf || car.Pump == null) return;
            var a = car.Pump.dispenser + Vector3.up * 1.0f;
            var b = car.transform.TransformPoint(car.visual.fuelCapLocal);
            if (Vector3.Dot(a - car.Position, car.transform.right) < 0f) b += Vector3.up * 0.9f; // бак не с той стороны — через крышу
            hose.position = (a + b) / 2f;
            hose.up = (b - a).normalized;
            hose.localScale = new Vector3(0.05f, Vector3.Distance(a, b) / 2f, 0.05f);
        }

        /// <summary>Идём по точкам маршрута. 0 — дошли до последней.</summary>
        float Walk(float dt)
        {
            while (route.Count > 0)
            {
                float s = MoveTo(route[0], walkSpeed, 0.1f, dt);
                if (s > 0f) return s;
                route.RemoveAt(0);
            }
            return 0f;
        }

        float MoveTo(Vector3 goal, float speed, float stopDistance, float dt)
        {
            var to = goal - transform.position;
            to.y = 0f;
            if (to.magnitude <= stopDistance) return 0f;
            if (Fighter.Limping) speed *= 0.5f;
            transform.position += to.normalized * Mathf.Min(speed * dt, to.magnitude);
            Face(goal, dt);
            return speed;
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
