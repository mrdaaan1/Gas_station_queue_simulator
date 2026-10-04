using UnityEngine;

namespace GasQueue
{
    public enum BrawlReason { Crash, CutIn }

    /// <summary>
    /// Возмущённый водитель NPC. Выходит из машины, идёт к игроку:
    /// если игрок сидит в машине — пинает и колотит её, если вышел — лезет драться.
    /// Чем бы ни кончилось, возвращается в свою машину: ему ведь тоже надо заправиться.
    /// </summary>
    public class Brawler : MonoBehaviour
    {
        enum Phase { Approach, RantAtCar, Fight, Return }

        static readonly string[] CrashLines =
        {
            "Ты офигел?! Заплатишь за всё!", "Ты видел, что ты сделал?!", "Я сейчас ГАИ вызову!",
            "Кто тебе права продал?!", "Бампер новый, между прочим!",
        };
        static readonly string[] CutInLines =
        {
            "Ты куда влез?!", "Я тут два часа стою!", "Самый умный, да?!", "Выходи, поговорим!", "Совести нет!",
        };
        static readonly string[] FightLines = { "Ну давай, давай!", "Получай!", "Я тебе покажу очередь!", "На!" };
        static readonly string[] WinLines = { "Будешь знать!", "Вот так-то.", "Ещё раз влезешь — получишь!" };
        static readonly string[] LoseLines = { "Ладно-ладно, всё...", "Я тебя запомнил!", "Ну ты псих..." };

        NpcCar car;
        TrafficManager traffic;
        HumanRig rig;
        Fighter fighter;
        BrawlReason reason;
        Phase phase;
        float timer;
        float lineTimer;
        float kickTimer;
        bool lost;
        bool announcedFight;

        public Fighter Fighter => fighter;
        public bool Fighting => phase == Phase.Fight;
        public static Brawler Active { get; private set; }

        public static bool CanSpawnFrom(NpcCar car) =>
            car != null && car.visual.driverHead != null && car.visual.driverHead.gameObject.activeSelf && car.Speed < 0.5f;

        public static Brawler Spawn(NpcCar car, TrafficManager traffic, BrawlReason reason)
        {
            if (!CanSpawnFrom(car) || Active != null) return null;
            var rig = HumanRig.Build("Angry driver", traffic.WorldRoot, HumanRig.RandomLook());
            rig.transform.position = car.DriverDoor;
            var b = rig.gameObject.AddComponent<Brawler>();
            b.car = car;
            b.traffic = traffic;
            b.rig = rig;
            b.reason = reason;
            b.fighter = Fighter.AddTo(rig, "Водитель");
            SetDriverVisible(car, false);
            car.Hold(1f);
            Active = b;
            GameManager.Instance.OnBrawlerOut(reason);
            return b;
        }

        static void SetDriverVisible(NpcCar car, bool visible)
        {
            if (car == null) return;
            if (car.visual.driverHead != null) car.visual.driverHead.gameObject.SetActive(visible);
            if (car.visual.driverTorso != null) car.visual.driverTorso.gameObject.SetActive(visible);
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;
        }

        void Say(string[] lines, float cooldown)
        {
            lineTimer = cooldown;
            SpeechBubble.Show(transform, lines[Random.Range(0, lines.Length)], 1.5f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (car == null) { Destroy(gameObject); return; }
            car.Hold(0.5f); // без водителя машина никуда не едет

            var gm = GameManager.Instance;
            timer += dt;
            lineTimer -= dt;
            float speed = 0f;

            if (fighter.Down)
            {
                lost = true;
                rig.Animate(0f, dt);
                return;
            }

            bool playerOnFoot = gm.OnFoot && gm.Walker != null && gm.Walker.Active;
            var playerFighter = playerOnFoot ? gm.Walker.Fighter : null;

            switch (phase)
            {
                case Phase.Approach:
                case Phase.RantAtCar:
                {
                    if (playerOnFoot && Vector3.Distance(transform.position, gm.Walker.transform.position) < 9f)
                    {
                        phase = Phase.Fight;
                        timer = 0f;
                        break;
                    }
                    var goal = gm.Player.DriverDoor;
                    speed = MoveTo(goal, phase == Phase.Approach ? 2.4f : 0f, 1.0f, dt);
                    if (phase == Phase.Approach && speed == 0f) { phase = Phase.RantAtCar; timer = 0f; }
                    if (lineTimer <= 0f) Say(reason == BrawlReason.CutIn ? CutInLines : CrashLines, 3f);

                    if (phase == Phase.RantAtCar)
                    {
                        Face(gm.Player.Position, dt);
                        kickTimer -= dt;
                        if (kickTimer <= 0f)
                        {
                            kickTimer = Random.Range(1.0f, 1.6f);
                            if (Random.value < 0.5f) rig.Kick(); else rig.Punch();
                            gm.Player.OnKickedByNpc(transform.position);
                        }
                        if (timer > 13f) BeginReturn(false);
                    }
                    else if (timer > 25f) BeginReturn(false);
                    break;
                }
                case Phase.Fight:
                {
                    if (!announcedFight)
                    {
                        announcedFight = true;
                        gm.ShowMessage("Драка! Бейте — левая кнопка мыши. Проигравший ложится.", 6f);
                    }
                    if (playerFighter == null || !playerOnFoot)
                    {
                        // Игрок сбежал в машину — возвращаемся пинать её
                        phase = Phase.RantAtCar;
                        timer = 0f;
                        break;
                    }
                    if (playerFighter.Down)
                    {
                        Say(WinLines, 99f);
                        gm.OnPlayerLostFight();
                        BeginReturn(false);
                        break;
                    }
                    var target = gm.Walker.transform.position;
                    float dist = Vector3.Distance(transform.position, target);
                    if (dist > 16f || timer > 40f) { BeginReturn(false); break; }
                    speed = MoveTo(target, 2.8f, 1.0f, dt);
                    Face(target, dt);
                    if (dist < 1.3f && fighter.CanPunch)
                    {
                        fighter.Punch(playerFighter, 6f, 12f, Random.Range(0.9f, 1.5f));
                        if (Random.value < 0.3f && lineTimer <= 0f) Say(FightLines, 2.5f);
                    }
                    break;
                }
                case Phase.Return:
                {
                    speed = MoveTo(car.DriverDoor, lost ? 1.0f : 1.6f, 0.25f, dt);
                    if (speed == 0f)
                    {
                        SetDriverVisible(car, true);
                        Destroy(gameObject);
                        return;
                    }
                    break;
                }
            }
            rig.Animate(speed, dt);
        }

        void BeginReturn(bool beaten)
        {
            phase = Phase.Return;
            if (beaten) Say(LoseLines, 99f);
        }

        /// <summary>Вызывается, когда водитель встал после нокаута.</summary>
        void LateUpdate()
        {
            if (lost && !fighter.Down && phase != Phase.Return)
            {
                Say(LoseLines, 99f);
                GameManager.Instance.OnPlayerWonFight();
                BeginReturn(true);
            }
        }

        float MoveTo(Vector3 goal, float speed, float stopDistance, float dt)
        {
            var to = goal - transform.position;
            to.y = 0f;
            if (to.magnitude <= stopDistance || speed <= 0f) return 0f;
            if (fighter.Limping) speed *= 0.5f;
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
