using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Игрок пешком: WASD относительно камеры, Shift — бегом. Не проходит сквозь машины, стены и столбы.
    /// </summary>
    public class WalkerController : MonoBehaviour
    {
        const float Radius = 0.3f;

        public HumanRig Rig { get; private set; }
        public bool Active => gameObject.activeSelf;
        public Vector2 Position2 => new Vector2(transform.position.x, transform.position.z);
        public Obb Box => new Obb(transform.position, transform.forward, 0.6f, 0.6f);
        public float Speed { get; private set; }

        TrafficManager traffic;
        CameraRig cameraRig;

        public static WalkerController Create(Transform parent, TrafficManager traffic, CameraRig rig)
        {
            var look = new HumanRig.Look
            {
                shirt = Shapes.Hex("#e0752d"), // оранжевая куртка — чтобы своего было видно издалека
                pants = Shapes.Hex("#2b3a55"),
                skin = Shapes.Hex("#e8b48f"),
                hair = Shapes.Hex("#3a2717"),
                shoes = Shapes.Hex("#f2f2f2"),
            };
            var human = HumanRig.Build("Player (пешком)", parent, look);
            var w = human.gameObject.AddComponent<WalkerController>();
            w.Rig = human;
            w.traffic = traffic;
            w.cameraRig = rig;
            human.gameObject.SetActive(false);
            return w;
        }

        public void Appear(Vector3 at, Vector3 facing)
        {
            at.y = 0f;
            transform.position = at;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(facing.normalized);
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            var input = GameInput.Move;
            var gm = GameManager.Instance;
            if (gm != null && gm.DialogOpen) input = Vector2.zero;
            if (input.sqrMagnitude > 1f) input.Normalize();

            float yaw = cameraRig.FootYaw;
            var forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            var right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            var move = forward * input.y + right * input.x;
            float targetSpeed = move.sqrMagnitude > 0.01f ? (GameInput.Run ? 4.5f : 1.9f) : 0f;
            Speed = Mathf.MoveTowards(Speed, targetSpeed, dt * 12f);

            var pos = transform.position;
            if (move.sqrMagnitude > 0.01f)
            {
                pos += move.normalized * Speed * dt;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move.normalized), dt * 12f);
            }
            pos = Collide(pos);
            pos.y = 0f;
            transform.position = pos;

            Rig.Animate(Speed, dt);
        }

        Vector3 Collide(Vector3 pos)
        {
            for (int iter = 0; iter < 2; iter++)
            {
                var p2 = new Vector2(pos.x, pos.z);
                foreach (var o in Obstacles.All)
                {
                    var d = o.box.center - p2;
                    float reach = o.box.half.magnitude + 1f;
                    if (d.sqrMagnitude > reach * reach) continue;
                    if (o.box.PushCircle(p2, Radius, out var push)) p2 += push;
                }
                if (traffic.Barrier.IsDown && traffic.Barrier.Box.PushCircle(p2, Radius, out var bp)) p2 += bp;
                if (traffic.Player.Box.PushCircle(p2, Radius, out var pp)) p2 += pp;
                foreach (var npc in traffic.Npcs)
                {
                    var d = npc.Position - pos;
                    if (d.x * d.x + d.z * d.z > 36f) continue;
                    if (npc.Box.PushCircle(p2, Radius, out var np)) p2 += np;
                }
                pos = new Vector3(p2.x, 0f, p2.y);
            }
            return pos;
        }
    }
}
