using UnityEngine;

namespace GasQueue
{
    public enum VendorKind { Canister, Pies }

    /// <summary>
    /// Ходит между рядами вдоль очереди и торгует: мужик с канистрами (бензин втридорога)
    /// или тётя с пирожками и чаем (восстанавливают силы после драки).
    /// Останавливается у водительского окна игрока, предлагает товар и идёт дальше.
    /// </summary>
    public class Vendor : MonoBehaviour
    {
        const float LaneX = 7.25f; // между очередью и соседним рядом

        static readonly string[] CanisterLines =
        {
            "Бензинчик! 95-й, свежий, как с завода!", "Канистра — и вы уже не в очереди!", "Дешевле только даром! Ну почти.",
            "Без очереди, без кассы, без чека!",
        };
        static readonly string[] PieLines =
        {
            "Пирожки! С капустой, с картошкой!", "Чай горячий, пирожки домашние!", "Подкрепитесь, вам ещё стоять и стоять!",
        };

        public VendorKind Kind { get; private set; }
        public bool Offering { get; private set; }

        HumanRig rig;
        TrafficManager traffic;
        float direction; // +1 — идёт вперёд по очереди, −1 — назад
        float offerTimer;
        float lineTimer;
        bool visitedPlayer;
        float life;

        public int Price
        {
            get
            {
                var gm = GameManager.Instance;
                return Kind == VendorKind.Canister ? Mathf.RoundToInt(gm.PriceBoard.price95 * 3f * 10f / 10f) * 10 : 150;
            }
        }

        public string Offer => Kind == VendorKind.Canister
            ? $"E — купить канистру 10 л за {Price} руб. (втрое дороже, чем на заправке)"
            : $"E — купить пирожок и чай за {Price} руб. (восстанавливает силы)";

        public static Vendor Spawn(VendorKind kind, TrafficManager traffic, float startZ, float direction)
        {
            var look = HumanRig.RandomLook();
            if (kind == VendorKind.Pies)
            {
                look.shirt = Shapes.Hex("#b5485d");
                look.hair = Shapes.Hex("#9a9a9a");
            }
            var rig = HumanRig.Build(kind == VendorKind.Canister ? "Canister seller" : "Pie seller", traffic.WorldRoot, look);
            rig.transform.position = new Vector3(LaneX, 0f, startZ);
            rig.transform.rotation = Quaternion.LookRotation(Vector3.forward * direction);

            // Товар в руке
            if (kind == VendorKind.Canister)
            {
                var can = Shapes.Group("Canister", rig.armR, new Vector3(0f, -0.62f, 0f));
                Shapes.Box(can, new Vector3(0, -0.2f, 0), new Vector3(0.14f, 0.4f, 0.32f), Shapes.Hex("#b3241b"));
                Shapes.Box(can, new Vector3(0, 0.02f, 0), new Vector3(0.05f, 0.05f, 0.18f), Shapes.Hex("#1b1b1b"), name: "Handle");
            }
            else
            {
                var basket = Shapes.Group("Basket", rig.armL, new Vector3(0f, -0.6f, 0.05f));
                Shapes.Box(basket, new Vector3(0, -0.1f, 0.08f), new Vector3(0.35f, 0.18f, 0.25f), Shapes.Hex("#a67c45"));
                Shapes.Make(PrimitiveType.Cylinder, rig.armR, new Vector3(0f, -0.7f, 0f), new Vector3(0.1f, 0.14f, 0.1f), Shapes.Hex("#3d6b3a"), name: "Thermos");
            }

            var v = rig.gameObject.AddComponent<Vendor>();
            v.Kind = kind;
            v.rig = rig;
            v.traffic = traffic;
            v.direction = direction;
            traffic.Pedestrians.Add(rig.transform);
            return v;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            var gm = GameManager.Instance;
            var player = traffic.Player;
            lineTimer -= dt;
            life += dt;
            if (life > 70f) visitedPlayer = true; // так и не дождался — идёт дальше

            // Остановились у окна игрока и предлагаем товар
            var window = player.DriverDoor;
            bool playerHere = Mathf.Abs(Mathf.Abs(player.Speed)) < 0.3f && !gm.OnFoot && Vector3.Distance(transform.position, window) < 2.2f;
            if (!visitedPlayer && playerHere)
            {
                Offering = true;
                visitedPlayer = true;
                offerTimer = 14f;
            }
            if (Offering)
            {
                offerTimer -= dt;
                var to = window - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), dt * 5f);
                if (lineTimer <= 0f)
                {
                    lineTimer = 4.5f;
                    var lines = Kind == VendorKind.Canister ? CanisterLines : PieLines;
                    SpeechBubble.Show(transform, lines[Random.Range(0, lines.Length)], 1.5f);
                }
                rig.Animate(0f, dt);
                if (offerTimer <= 0f || Mathf.Abs(player.Speed) > 1f) Offering = false;
                return;
            }

            // Идём вдоль очереди к машине игрока, потом дальше
            float targetZ = visitedPlayer ? transform.position.z + direction * 10f : window.z;
            float dz = targetZ - transform.position.z;
            float step = Mathf.Sign(dz) * Mathf.Min(Mathf.Abs(dz), 1.1f * dt);
            if (!visitedPlayer && Mathf.Abs(dz) < 0.3f) step = 0f; // ждём, пока игрок остановится
            var pos = transform.position;
            pos.x = Mathf.MoveTowards(pos.x, LaneX, dt);
            pos.z += step;
            transform.position = pos;
            if (Mathf.Abs(step) > 0.0001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.forward * Mathf.Sign(step)), dt * 6f);
            rig.Animate(Mathf.Abs(step) / dt, dt);

            // Ушёл далеко — исчезает
            if (Mathf.Abs(transform.position.z - player.Position.z) > 140f || transform.position.z > CityLayout.LotMinZ - 6f)
            {
                traffic.Pedestrians.Remove(transform);
                Destroy(gameObject);
            }
        }

        /// <summary>Игрок купил товар.</summary>
        public void Sold()
        {
            Offering = false;
            SpeechBubble.Show(transform, Kind == VendorKind.Canister ? "Приятно иметь дело!" : "Кушайте на здоровье!", 1.5f);
        }
    }
}
