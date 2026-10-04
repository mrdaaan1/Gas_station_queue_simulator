using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Очередь машин. cars[0] стоит у колонки, дальше — по порядку назад.
    /// Передняя машина заправляется и уезжает, остальные подтягиваются.
    /// </summary>
    public class QueueManager : MonoBehaviour
    {
        public readonly List<QueueCar> cars = new List<QueueCar>();
        public GameSettings Settings { get; private set; }
        public PlayerCar Player { get; private set; }

        static readonly string[] AnswersToHonk =
        {
            "Да куда я поеду?!", "Сам бибикай!", "Очередь для всех одна!", "Не нервничай, брат",
            "Ещё раз бибикнешь — выйду!", "Я тоже тороплюсь!", "Тише, тише...", "И что?",
        };

        static readonly string[] GiveUpPhrases =
        {
            "Да ну вас всех!", "Поеду на другую...", "Пешком дойду!", "Всё, я пас", "Завтра приеду",
        };

        Barrier barrier;
        float barrierZ;
        int carCounter;

        bool serviceStarted;
        float serviceTimer;
        float serviceDuration;
        float giveUpTimer;
        float tailTimer;

        public int PlayerIndex => Player == null ? -1 : cars.IndexOf(Player);

        public void Init(GameSettings settings, Barrier barrier, float barrierZ, PlayerCar player)
        {
            Settings = settings;
            this.barrier = barrier;
            this.barrierZ = barrierZ;
            Player = player;

            float z = WorldBuilder.PumpZ;
            for (int i = 0; i < settings.carsAhead; i++, z -= settings.carSpacing)
                cars.Add(SpawnNpc(z));

            player.queue = this;
            player.transform.position = new Vector3(WorldBuilder.QueueLaneX, 0f, z);
            cars.Add(player);
            z -= settings.carSpacing;

            for (int i = 0; i < settings.carsBehind; i++, z -= settings.carSpacing)
                cars.Add(SpawnNpc(z));

            // Первая машина уже наполовину заправилась
            serviceStarted = true;
            serviceDuration = settings.ServiceTime;
            serviceTimer = settings.ServiceTime * 0.5f;
        }

        NpcCar SpawnNpc(float z)
        {
            var visual = CarFactory.Build($"Car {++carCounter}", CarFactory.Paints[Random.Range(0, CarFactory.Paints.Length)],
                CarFactory.RandomShape(), false);
            visual.transform.SetParent(transform, false);
            visual.transform.position = new Vector3(WorldBuilder.QueueLaneX, 0f, z);
            var npc = visual.gameObject.AddComponent<NpcCar>();
            npc.visual = visual;
            npc.queue = this;
            return npc;
        }

        public QueueCar Ahead(QueueCar car)
        {
            int i = cars.IndexOf(car);
            return i > 0 ? cars[i - 1] : null;
        }

        /// <summary>Куда машина хочет подъехать: вплотную (с нормальным зазором) за передней.</summary>
        public float DesiredZ(QueueCar car)
        {
            int i = cars.IndexOf(car);
            float desired = i <= 0 ? WorldBuilder.PumpZ : cars[i - 1].Z - Settings.carSpacing;
            return Mathf.Min(desired, HardLimitZ(car));
        }

        /// <summary>Дальше этой точки машина физически проехать не может (машина впереди, шлагбаум, колонка).</summary>
        public float HardLimitZ(QueueCar car)
        {
            int i = cars.IndexOf(car);
            float limit = float.MaxValue;
            if (i > 0) limit = cars[i - 1].RearZ - 0.6f - car.Length / 2f;
            else if (i == 0) limit = WorldBuilder.PumpZ + (car.IsPlayer ? 1.5f : 0f);

            bool behindBarrier = car.Z + car.Length / 2f < barrierZ + 0.5f;
            if (barrier.IsDown && behindBarrier)
                limit = Mathf.Min(limit, barrierZ - 0.6f - car.Length / 2f);
            return limit;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var gm = GameManager.Instance;
            if (gm.State == GameState.Finished) return;

            UpdateService(dt, gm);
            UpdateGiveUps(dt, gm);
            UpdateTail(dt);
        }

        void UpdateService(float dt, GameManager gm)
        {
            if (cars.Count == 0 || !(cars[0] is NpcCar front) || !front.IsAtPump) return;

            if (!serviceStarted)
            {
                serviceStarted = true;
                serviceTimer = 0f;
                serviceDuration = Settings.ServiceTime * Random.Range(0.75f, 1.25f);
                int p = PlayerIndex;
                if (Random.value < Settings.slowDriverChance)
                {
                    serviceDuration *= 2.2f;
                    if (p >= 0 && p <= 6) gm.ShowMessage("Водитель у колонки что-то тупит...");
                }
            }

            serviceTimer += dt;
            if (serviceTimer < serviceDuration) return;

            front.Leave();
            cars.RemoveAt(0);
            serviceStarted = false;

            // Игрок стал первым — и именно сейчас бензин заканчивается
            if (cars.Count > 0 && cars[0] == Player && !gm.FuelRanOut)
                gm.TriggerOutOfFuel();
        }

        void UpdateGiveUps(float dt, GameManager gm)
        {
            giveUpTimer += dt;
            if (giveUpTimer < Settings.GiveUpCheckInterval) return;
            giveUpTimer = 0f;

            int p = PlayerIndex;
            if (p < 0 || Random.value > Settings.giveUpChance) return;

            // Сдаются те, кого игрок видит: в пределах десятка машин спереди и сзади
            var candidates = new List<NpcCar>();
            for (int i = Mathf.Max(1, p - 10); i < Mathf.Min(cars.Count, p + 9); i++)
                if (i != p && cars[i] is NpcCar npc && npc.IsStill)
                    candidates.Add(npc);
            if (candidates.Count == 0) return;

            var quitter = candidates[Random.Range(0, candidates.Count)];
            bool ahead = cars.IndexOf(quitter) < p;
            cars.Remove(quitter);
            quitter.GiveUp(GiveUpPhrases[Random.Range(0, GiveUpPhrases.Length)]);
            gm.OnSomeoneGaveUp(ahead);
        }

        /// <summary>Хвост очереди растёт: подъезжают новые машины.</summary>
        void UpdateTail(float dt)
        {
            tailTimer += dt;
            if (tailTimer < Settings.ServiceTime * 0.7f) return;
            tailTimer = 0f;

            int p = PlayerIndex;
            int behind = p < 0 ? cars.Count : cars.Count - 1 - p;
            if (behind >= Settings.maxCarsBehind || cars.Count == 0) return;
            var last = cars[cars.Count - 1];
            cars.Add(SpawnNpc(last.Z - Settings.carSpacing * 5f));
        }

        public void OnPlayerHonk()
        {
            int p = PlayerIndex;
            if (p <= 0) return;
            if (cars[p - 1] is NpcCar first && first.IsStill)
                first.React(AnswersToHonk[Random.Range(0, AnswersToHonk.Length)]);
            if (p > 1 && Random.value < 0.35f && cars[p - 2] is NpcCar second && second.IsStill)
                second.React(AnswersToHonk[Random.Range(0, AnswersToHonk.Length)]);
        }

        /// <summary>Игрок заправился и уезжает — больше не в очереди.</summary>
        public void RemovePlayer() => cars.Remove(Player);
    }
}
