using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Академик на мусорном контейнере: зелёный бак на картинговых колёсах, из бака торчит голова в шлеме.
    /// Пролетает по разделительной (между нашим левым рядом и встречкой) на 150 км/ч, на заправку не заезжает —
    /// у него же просто мусорный контейнер. Доехал до края — через минуту летит обратно. И так по кругу.
    /// Если на разделительной кто-то стоит (машина игрока, пешеход, перестраивающийся) — перепрыгивает.
    /// Только в основной игре.
    /// </summary>
    public class Akademik : MonoBehaviour
    {
        const float Speed = 150f / 3.6f;   // 41,7 м/с
        const float LaneX = 0f;            // разделительная
        const float Gravity = 22f;

        TrafficManager traffic;
        Transform bin, body, head;
        readonly Transform[] wheels = new Transform[4];
        AudioSource engine;
        bool flying;
        float dir = 1f;
        float timer = 45f;
        float y, vy;
        float wheelAngle, wobble;
        bool announced, saidHi;

        static readonly Color Green = Shapes.Hex("#2d5a2a");
        static readonly Color GreenDark = Shapes.Hex("#234821");
        static readonly Color Black = Shapes.Hex("#151515");
        static readonly Color Hub = Shapes.Hex("#c9ccd0");

        static readonly string[] PassLines =
        {
            "Бензин? Не, не слышал!", "Йу-ху-у-у!", "Сто пятьдесят, детка!", "Очередь — для слабаков!",
            "Мусорка не заправляется!", "Посторони-и-ись!",
        };
        static readonly string[] JumpLines = { "Опа!", "Трамплин!", "Йии-ха!" };

        public void Init(TrafficManager t)
        {
            traffic = t;
            Build();
            bin.gameObject.SetActive(false);
        }

        void Build()
        {
            bin = Shapes.Group("Akademik", traffic.WorldRoot);
            body = Shapes.Group("Body", bin, new Vector3(0f, 0.2f, 0f));
            // Бак: снизу уже, кверху шире; толстый верхний обод, рёбра и ручки
            Shapes.Box(body, new Vector3(0f, 0.55f, 0f), new Vector3(1.12f, 0.75f, 1.36f), Green, name: "Lower");
            Shapes.Box(body, new Vector3(0f, 0.98f, 0f), new Vector3(1.2f, 0.18f, 1.44f), Green, name: "Upper");
            Shapes.Box(body, new Vector3(0f, 1.1f, 0f), new Vector3(1.28f, 0.08f, 1.52f), GreenDark, name: "Rim");
            Shapes.Box(body, new Vector3(0f, 1.09f, 0f), new Vector3(1.1f, 0.06f, 1.34f), Shapes.Hex("#0f1a0e"), name: "Inside");
            foreach (float side in new[] { -1f, 1f })
            {
                Shapes.Box(body, new Vector3(0.57f * side, 0.6f, 0f), new Vector3(0.04f, 0.6f, 0.08f), GreenDark, name: "Rib");
                Shapes.Box(body, new Vector3(0f, 0.6f, 0.69f * side), new Vector3(0.08f, 0.6f, 0.04f), GreenDark, name: "Rib");
                Shapes.Box(body, new Vector3(0.35f * side, 0.85f, 0.73f), new Vector3(0.22f, 0.12f, 0.04f), Shapes.Hex("#1b3519"), name: "Handle");
            }
            // Петли крышки сзади (крышки нет — улетела давно)
            foreach (float x in new[] { -0.4f, 0.4f })
                Shapes.Box(body, new Vector3(x, 1.12f, -0.78f), new Vector3(0.24f, 0.08f, 0.12f), Black, name: "Hinge");
            // Голова в шлеме торчит над краем
            head = Shapes.Group("Rider", body, new Vector3(0f, 1.3f, 0.12f));
            Shapes.Make(PrimitiveType.Sphere, head, Vector3.zero, new Vector3(0.3f, 0.3f, 0.32f), Shapes.Hex("#9fd14a"), name: "Helmet");
            Shapes.Make(PrimitiveType.Sphere, head, new Vector3(0f, 0.04f, 0f), new Vector3(0.24f, 0.2f, 0.33f), Shapes.Hex("#f2f2f0"), name: "Stripe");
            Shapes.Box(head, new Vector3(0f, -0.02f, 0.13f), new Vector3(0.22f, 0.1f, 0.06f), Shapes.Hex("#1a1f2a"), name: "Visor");
            Shapes.Box(head, new Vector3(0f, -0.1f, 0.14f), new Vector3(0.12f, 0.04f, 0.03f), Shapes.Hex("#e2b08a"), name: "Chin");
            // Картинговые колёса по углам
            int i = 0;
            foreach (float z in new[] { 0.62f, -0.62f })
                foreach (float x in new[] { -0.66f, 0.66f })
                {
                    var w = Shapes.Group("Wheel", bin, new Vector3(x, 0.19f, z));
                    Shapes.Make(PrimitiveType.Cylinder, w, Vector3.zero, new Vector3(0.38f, 0.11f, 0.38f), Black, new Vector3(0f, 0f, 90f), "Tyre");
                    Shapes.Make(PrimitiveType.Cylinder, w, new Vector3(0.115f * Mathf.Sign(x), 0f, 0f), new Vector3(0.2f, 0.01f, 0.2f), Hub, new Vector3(0f, 0f, 90f), "Hub");
                    wheels[i++] = w;
                }
            Shapes.Box(bin, new Vector3(0f, 0.2f, 0f), new Vector3(1.2f, 0.06f, 1.2f), Black, name: "Frame");

            engine = bin.gameObject.AddComponent<AudioSource>();
            engine.clip = SoundFactory.Engine;
            engine.loop = true;
            engine.pitch = 2.6f;
            engine.volume = 0.9f;
            engine.spatialBlend = 1f;
            engine.minDistance = 6f;
            engine.maxDistance = 140f;
            engine.rolloffMode = AudioRolloffMode.Linear;
            engine.dopplerLevel = 1.2f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || traffic == null || bin == null) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.State == GameState.Finished) return;

            if (!flying)
            {
                timer -= dt;
                if (timer > 0f) return;
                flying = true;
                saidHi = false;
                y = 0f; vy = 0f;
                bin.position = new Vector3(LaneX, 0f, dir > 0f ? CityLayout.RoadStartZ : CityLayout.RoadEndZ);
                bin.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, dir));
                bin.gameObject.SetActive(true);
                engine.Play();
                return;
            }

            var pos = bin.position;
            pos.z += dir * Speed * dt;
            // Препятствие на разделительной — прыжок
            if (y <= 0f && SomethingAhead(pos))
            {
                vy = 9f;
                SpeechBubble.Show(bin, JumpLines[Random.Range(0, JumpLines.Length)], 1.6f);
            }
            vy -= Gravity * dt;
            y = Mathf.Max(0f, y + vy * dt);
            if (y <= 0f) vy = 0f;
            wobble += dt * 13f;
            pos.x = LaneX + Mathf.Sin(wobble * 0.37f) * 0.05f;
            pos.y = y;
            bin.position = pos;
            // Бак потряхивает на неровностях, в прыжке задирает нос
            body.localRotation = Quaternion.Euler(-Mathf.Clamp(vy * 2.5f, -20f, 20f) + Mathf.Sin(wobble) * 1.5f, 0f, Mathf.Sin(wobble * 0.8f) * 2.5f);
            wheelAngle += Speed / 0.19f * Mathf.Rad2Deg * dt;
            foreach (var w in wheels) w.localRotation = Quaternion.Euler(wheelAngle, 0f, 0f);

            // Поравнялся с игроком — крик и одно сообщение в ленту за всю игру
            var me = gm.OnFoot && gm.Walker != null ? gm.Walker.transform.position : traffic.Player.Position;
            if (!saidHi && Mathf.Abs(me.z - pos.z) < 35f && (me.z - pos.z) * dir > 0f)
            {
                saidHi = true;
                SpeechBubble.Show(bin, PassLines[Random.Range(0, PassLines.Length)], 1.6f);
                if (!announced)
                {
                    announced = true;
                    gm.ShowMessage("По разделительной на мусорном контейнере пролетел Академик. 150 км/ч. Ему заправка не нужна.", 8f);
                }
            }

            if (pos.z > CityLayout.RoadEndZ + 5f || pos.z < CityLayout.RoadStartZ - 5f)
            {
                flying = false;
                bin.gameObject.SetActive(false);
                engine.Stop();
                dir = -dir;
                timer = Random.Range(45f, 75f);
            }
        }

        /// <summary>Что-то стоит на разделительной в 8–30 м впереди (машина игрока, NPC, пешеход).</summary>
        bool SomethingAhead(Vector3 pos)
        {
            bool Blocks(Vector3 p, float halfWidth)
            {
                float ahead = (p.z - pos.z) * dir;
                return ahead > 6f && ahead < 30f && Mathf.Abs(p.x - LaneX) < halfWidth + 0.6f;
            }
            var player = traffic.Player;
            if (player != null && Blocks(player.Position, Half(player))) return true;
            foreach (var npc in traffic.Npcs)
                if (Mathf.Abs(npc.Position.x) < 4f && Blocks(npc.Position, Half(npc)))
                    return true;
            var gm = GameManager.Instance;
            if (gm != null && gm.OnFoot && gm.Walker != null && Blocks(gm.Walker.transform.position, 0.3f)) return true;
            foreach (var p in traffic.Pedestrians)
                if (p != null && Blocks(p.position, 0.3f)) return true;
            return false;
        }

        /// <summary>Половина ширины машины поперёк дороги (с учётом поворота).</summary>
        static float Half(Vehicle v) => Mathf.Abs(v.Forward.x) * v.Length / 2f + Mathf.Abs(v.Forward.z) * v.Width / 2f;

        void OnDestroy()
        {
            if (bin != null) Destroy(bin.gameObject);
        }
    }
}
