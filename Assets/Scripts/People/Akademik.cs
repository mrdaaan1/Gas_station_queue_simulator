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
        Transform bin, body;
        readonly Transform[] wheels = new Transform[4];
        AudioSource engine;
        bool flying;
        float dir = 1f;
        float timer = 45f;
        float y, vy;
        float wheelAngle, wobble;
        bool announced, saidHi;

        static readonly Color Green = Shapes.Hex("#2d5a2a");

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
            // Гладкая модель, как у спорткаров (World/Models/AkademikModel): бак, рама, колёса, шлем
            var map = ModelSpawner.Spawn(AkademikModel.Get().root, bin, key => CarMaterials.Get(key, Green));
            body = map["Bin"];
            wheels[0] = map["WheelFL"]; wheels[1] = map["WheelFR"]; wheels[2] = map["WheelRL"]; wheels[3] = map["WheelRR"];

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
            wheelAngle = Mathf.Repeat(wheelAngle + Speed / AkademikModel.WheelR * Mathf.Rad2Deg * dt, 360f);
            foreach (var w in wheels) if (w != null) w.localRotation = Quaternion.Euler(0f, 0f, 90f) * Quaternion.Euler(0f, -wheelAngle, 0f);

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
