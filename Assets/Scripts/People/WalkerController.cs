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
        public Fighter Fighter { get; private set; }
        public bool Active => gameObject.activeSelf;
        public Vector2 Position2 => new Vector2(transform.position.x, transform.position.z);
        public Obb Box => new Obb(transform.position, transform.forward, 0.6f, 0.6f);
        public float Speed { get; private set; }

        /// <summary>Высота ступней над асфальтом (на крыше машины — высота крыши).</summary>
        public float Height => height;
        public bool Airborne => !grounded;

        const float Gravity = 20f;
        const float JumpSpeed = 4.2f;   // обычный прыжок ~0.45 м
        const float ClimbMax = 1.3f;    // выше этого не залезть (с асфальта — на капот, с капота — на крышу)
        const float ClimbTime = 0.55f;

        float climbTimer;
        Vector3 climbFrom, climbTo;
        Vehicle climbCar;

        float height, vy;
        bool grounded = true;
        Vehicle standingOn;
        Vector3 standingOnLast;

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
            w.Fighter = Fighter.AddTo(human, "Вы");
            w.traffic = traffic;
            w.cameraRig = rig;
            human.gameObject.SetActive(false);
            return w;
        }

        public void Appear(Vector3 at, Vector3 facing)
        {
            at.y = 0f;
            height = 0f;
            vy = 0f;
            grounded = true;
            standingOn = null;
            transform.position = at;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(facing.normalized);
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        GiantCigarette cigarette;
        public bool Smoking => cigarette != null;

        public void GiveCigarette()
        {
            if (cigarette != null) return;
            cigarette = gameObject.AddComponent<GiantCigarette>();
            cigarette.Init(Rig, traffic.WorldRoot);
        }

        ShashlikSkewer shashlik;
        public bool EatingShashlik => shashlik != null;

        public void GiveShashlik()
        {
            if (shashlik != null) return;
            DropCigarette(); // одна рука — сигарета-бревно, другая — шампур? Нет уж, выбирай
            shashlik = gameObject.AddComponent<ShashlikSkewer>();
            shashlik.Init(Rig);
        }

        /// <summary>Убрать шампур (доел или сел в машину). Возвращает true, если он был.</summary>
        public bool DropShashlik()
        {
            if (shashlik == null) return false;
            shashlik.Throw();
            shashlik = null;
            return true;
        }

        /// <summary>Выбросить сигарету. Возвращает true, если она была.</summary>
        public bool DropCigarette()
        {
            if (cigarette == null) return false;
            cigarette.Throw();
            cigarette = null;
            return true;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            var input = GameInput.Move;
            var gm = GameManager.Instance;
            if (gm != null && gm.DialogOpen) input = Vector2.zero;
            if (cigarette != null && cigarette.Finished)
            {
                DropCigarette();
                gm?.OnCigaretteFinished();
            }
            if (shashlik != null && shashlik.Finished)
            {
                DropShashlik();
                gm?.OnShashlikFinished(false);
            }
            if (Fighter.Down)
            {
                // Лежим, пока не очухаемся (если сбили на крыше — падаем на асфальт)
                Speed = 0f;
                if (height > 0f) Fall(transform.position, dt);
                Rig.Airborne = false;
                Rig.Animate(0f, dt);
                return;
            }
            if (climbTimer > 0f)
            {
                UpdateClimb(dt);
                return;
            }
            if (GameInput.AttackPressed) Attack();
            if (GameInput.JumpPressed && grounded && (gm == null || !gm.DialogOpen) && !TryClimb())
            {
                vy = JumpSpeed;
                grounded = false;
                standingOn = null;
            }
            if (input.sqrMagnitude > 1f) input.Normalize();

            float yaw = cameraRig.FootYaw;
            var forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            var right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            var move = forward * input.y + right * input.x;
            float targetSpeed = move.sqrMagnitude > 0.01f ? (GameInput.Run ? 4.5f : 1.9f) : 0f;
            if (Fighter.Limping) targetSpeed *= 0.55f;
            Speed = Mathf.MoveTowards(Speed, targetSpeed, dt * 12f);

            var pos = transform.position;
            // Стоим на крыше машины, которая поехала, — нас везёт вместе с ней
            if (standingOn != null && grounded)
            {
                var delta = standingOn.Position - standingOnLast;
                delta.y = 0f;
                pos += delta;
            }
            var before = pos;
            if (move.sqrMagnitude > 0.01f)
            {
                pos += move.normalized * Speed * dt;
                // От третьего лица разворачиваемся по ходу движения, от первого — тело смотрит туда же, куда камера
                // (S — просто шаг назад, а не разворот к камере лицом)
                if (!cameraRig.FootFirstPerson)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move.normalized), dt * 12f);
            }
            if (cameraRig.FootFirstPerson) transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            pos = Collide(pos);
            // Стоим на капоте, а впереди крыша выше нас — это стенка, туда только прыжком
            if (HitsHigherLevel(new Vector2(pos.x, pos.z))) { pos.x = before.x; pos.z = before.z; }
            pos = Fall(pos, dt);
            transform.position = pos;

            Rig.Airborne = !grounded;
            Rig.Animate(grounded ? Speed : 0f, dt);
        }

        /// <summary>Гравитация: падаем, пока не встанем на асфальт или на крышу машины.</summary>
        Vector3 Fall(Vector3 pos, float dt)
        {
            float ground = GroundAt(new Vector2(pos.x, pos.z), out var car);
            if (grounded && height > ground + 0.05f) grounded = false; // сошли с края крыши
            if (!grounded)
            {
                vy -= Gravity * dt;
                height += vy * dt;
                if (height <= ground && vy <= 0f)
                {
                    float fallSpeed = -vy;
                    height = ground;
                    vy = 0f;
                    grounded = true;
                    if (car != null && fallSpeed > 1.2f) Landed(car);
                }
            }
            else height = ground;

            standingOn = grounded ? car : null;
            if (standingOn != null) standingOnLast = standingOn.Position;
            pos.y = height;
            return pos;
        }

        /// <summary>На какой высоте опора под точкой: асфальт или крыша машины.</summary>
        float GroundAt(Vector2 p, out Vehicle car)
        {
            Vehicle found = null;
            float best = 0f;
            Check(traffic.Player);
            foreach (var npc in traffic.Npcs) Check(npc);
            car = found;
            return best;

            void Check(Vehicle v)
            {
                if (v == null) return;
                float top = SurfaceAt(v, p);
                // Встать можно только сверху: если ноги ниже, это не опора, а стенка
                if (top <= best || top > height + 0.25f) return;
                best = top;
                found = v;
            }
        }

        /// <summary>
        /// Высота машины в точке: над салоном — крыша, спереди и сзади — капот и багажник.
        /// −1, если точка не над машиной.
        /// </summary>
        static float SurfaceAt(Vehicle v, Vector2 p)
        {
            var box = v.Box;
            var rel = p - box.center;
            if (rel.sqrMagnitude > 25f) return -1f;
            float lx = Vector2.Dot(rel, box.Right), lz = Vector2.Dot(rel, box.forward);
            if (Mathf.Abs(lx) > box.half.x - 0.1f || Mathf.Abs(lz) > box.half.y - 0.2f) return -1f;
            float len = box.half.y * 2f;
            bool overCabin = lz > -0.32f * len && lz < 0.12f * len;
            return overCabin ? v.visual.height : HoodHeight(v);
        }

        static float HoodHeight(Vehicle v) => v.visual.hoodTop > 0f ? v.visual.hoodTop : Mathf.Min(1.0f, v.visual.height - 0.3f);

        bool HitsHigherLevel(Vector2 p)
        {
            if (height < 0.3f) return false; // на асфальте машины выталкивают как обычно
            if (Higher(traffic.Player)) return true;
            foreach (var npc in traffic.Npcs) if (Higher(npc)) return true;
            return false;

            bool Higher(Vehicle v) => v != null && Above(v) && SurfaceAt(v, p) > height + 0.25f;
        }

        /// <summary>
        /// Пробел вплотную к машине: не прыжок, а залезть — с асфальта на капот или багажник,
        /// с капота на крышу. Человек не олимпиец, так что только по ступенькам.
        /// </summary>
        bool TryClimb()
        {
            var me = transform.position;
            var fwd = transform.forward;
            fwd.y = 0f;
            fwd.Normalize();
            foreach (float reach in new[] { 0.5f, 0.8f })
            {
                var p = new Vector2(me.x + fwd.x * reach, me.z + fwd.z * reach);
                Vehicle best = null;
                float top = -1f;
                Check(traffic.Player);
                foreach (var npc in traffic.Npcs) Check(npc);
                if (best == null) continue;

                climbCar = best;
                climbFrom = me;
                climbTo = new Vector3(p.x, top, p.y);
                climbTimer = ClimbTime;
                grounded = false;
                standingOn = null;
                return true;

                void Check(Vehicle v)
                {
                    if (v == null) return;
                    float h = SurfaceAt(v, p);
                    if (h > height + 0.3f && h <= height + ClimbMax && h > top)
                    {
                        top = h;
                        best = v;
                    }
                }
            }
            return false;
        }

        void UpdateClimb(float dt)
        {
            climbTimer -= dt;
            float t = 1f - Mathf.Clamp01(climbTimer / ClimbTime);
            // Сначала подтягиваемся вверх, потом перекидываем ноги вперёд
            float up = Mathf.Clamp01(t / 0.6f), along = Mathf.Clamp01((t - 0.3f) / 0.7f);
            var pos = Vector3.Lerp(climbFrom, climbTo, along);
            height = Mathf.Lerp(climbFrom.y, climbTo.y, up);
            pos.y = height;
            transform.position = pos;
            Rig.Airborne = true;
            Rig.Animate(0f, dt);
            if (climbTimer > 0f) return;

            height = climbTo.y;
            vy = 0f;
            grounded = true;
            if (climbCar != null)
            {
                standingOn = climbCar;
                standingOnLast = climbCar.Position;
                Landed(climbCar);
            }
        }

        void Landed(Vehicle car)
        {
            var gm = GameManager.Instance;
            if (car is NpcCar npc) traffic.OnPlayerJumpedOnCar(npc);
            else if (gm != null) gm.OnJumpedOnOwnCar();
            car.visual.Bounce();
            AudioSource.PlayClipAtPoint(SoundFactory.Thud, transform.position, 0.8f);
        }

        /// <summary>Удар кулаком: если рядом возмущённый водитель — разворачиваемся к нему и бьём.</summary>
        void Attack()
        {
            Fighter target = null;
            var brawler = Brawler.Active;
            if (brawler != null && !brawler.Fighter.Down)
            {
                var to = brawler.transform.position - transform.position;
                bool sameLevel = Mathf.Abs(to.y) < 0.6f;
                to.y = 0f;
                if (sameLevel && to.magnitude < 2.2f)
                {
                    target = brawler.Fighter;
                    if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to.normalized);
                }
            }
            if (target == null)
            {
                // Любой водитель на заправке или в очереди в кассу
                float best = 2.2f;
                foreach (var c in PumpCustomer.All)
                {
                    if (c == null || c.Fighter.Down) continue;
                    var to = c.transform.position - transform.position;
                    if (Mathf.Abs(to.y) > 0.6f) continue;
                    to.y = 0f;
                    if (to.magnitude < best)
                    {
                        best = to.magnitude;
                        target = c.Fighter;
                    }
                }
                if (target != null)
                {
                    var to = target.transform.position - transform.position;
                    to.y = 0f;
                    if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to.normalized);
                }
            }
            if (Fighter.Punch(target, 9f, 16f, 0.45f) && target != null && !(Brawler.Active != null && target == Brawler.Active.Fighter))
                GameManager.Instance?.OnPunchedBystander();
        }

        /// <summary>Ноги выше капота — по этой машине можно ходить, она не выталкивает.</summary>
        bool Above(Vehicle v) => height >= HoodHeight(v) - 0.2f;

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
                if (!Above(traffic.Player) && traffic.Player.Box.PushCircle(p2, Radius, out var pp)) p2 += pp;
                foreach (var npc in traffic.Npcs)
                {
                    var d = npc.Position - pos;
                    if (d.x * d.x + d.z * d.z > 36f) continue;
                    if (Above(npc)) continue;
                    if (npc.Box.PushCircle(p2, Radius, out var np)) p2 += np;
                }
                pos = new Vector3(p2.x, pos.y, p2.y);
            }
            return pos;
        }
    }
}
