using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Все числа баланса в одном месте. Их можно крутить в Inspector прямо во время Play
    /// (объект "Game" в иерархии сцены).
    /// </summary>
    public class GameSettings : MonoBehaviour
    {
        [Header("Режим для тестов")]
        [Tooltip("Ускоряет все ожидания, чтобы пройти игру быстрее, чем за 20 минут.")]
        public bool fastTestMode = true;
        [Tooltip("Во сколько раз быстрее идёт всё в тестовом режиме. 2 — около 10 минут на всю игру.")]
        public float testSpeedup = 2f;

        [Header("Топливо игрока")]
        [Tooltip("Сколько литров в баке в начале. Бак почти пустой — так задумано.")]
        public float startFuelLiters = 4.8f;
        [Tooltip("Расход на холостых, литров в игровой час. Повод заглушить мотор (I).")]
        public float idleLitersPerHour = 0.9f;
        [Tooltip("Расход при движении, литров на 100 км.")]
        public float drivingLitersPer100Km = 9f;

        [Header("Очередь")]
        public int carsAhead = 20;
        public int carsBehind = 14;
        public int maxCarsBehind = 26;
        [Tooltip("Расстояние между машинами в очереди, метры.")]
        public float carSpacing = 6.5f;

        [Header("Время (в реальных секундах, без ускорения)")]
        [Tooltip("Сколько одна машина стоит у колонки. Определяет темп очереди.")]
        public float serviceTime = 32f;
        [Tooltip("Шанс, что водитель у колонки «тупит» и стоит дольше.")]
        [Range(0f, 1f)] public float slowDriverChance = 0.2f;
        [Tooltip("Как часто кто-то впереди может сдаться и уехать.")]
        public float giveUpCheckInterval = 45f;
        [Range(0f, 1f)] public float giveUpChance = 0.5f;
        [Tooltip("Сколько реально длится «час» до завоза бензина.")]
        public float deliveryDuration = 300f;
        [Tooltip("Литров в секунду при заправке. Мучительно медленно — так задумано.")]
        public float litersPerSecond = 0.5f;
        public float tankLiters = 40f;
        [Tooltip("Как часто кто-то пытается влезть в очередь из соседнего ряда.")]
        public float cutterInterval = 22f;

        [Header("Гонка «Самая быстрая гонка»")]
        [Tooltip("Сколько обычных машин уже стоит в очереди, когда приезжают гонщики.")]
        public int raceQueueCars = 14;
        [Tooltip("Бензин на старте гонки, литров.")]
        public float raceStartFuelLiters = 6f;
        [Tooltip("Расход спорткара в гонке, литров на 100 км (на холостых — ноль).")]
        public float raceLitersPer100Km = 45f;

        [Header("Деньги")]
        public float startMoney = 3000f;

        [Header("Поток машин по дороге (секунды между машинами, без ускорения)")]
        public float trafficIntervalMin = 3f;
        public float trafficIntervalMax = 9f;

        [Header("Часы в очереди")]
        [Tooltip("Таймер стартует не с нуля: игрок уже давно стоит.")]
        public int startMinutes = 80;
        [Tooltip("Сколько игровых секунд проходит за одну реальную.")]
        public float clockScale = 6f;

        // Номер версии настроек: когда меняем значения по умолчанию, старая сцена обновляется сама
        [SerializeField, HideInInspector] int settingsVersion;
        const int CurrentVersion = 5;

        void Awake() => Migrate();
        void OnValidate() => Migrate();

        void Migrate()
        {
            if (settingsVersion >= CurrentVersion) return;
            if (settingsVersion < 2) testSpeedup = 2f; // было 6 — игроку показалось слишком быстро
            if (settingsVersion < 3)
            {
                // Город и 4 колонки: очередь чуть короче, хвост не бесконечный
                carsAhead = 20;
                maxCarsBehind = 26;
            }
            if (settingsVersion < 4) cutterInterval = 22f; // наглецов вторым рядом стало больше
            if (settingsVersion < 5) raceQueueCars = 14; // очередь в гонке должна быть видна с дороги
            settingsVersion = CurrentVersion;
        }

        float Speed => fastTestMode ? Mathf.Max(1f, testSpeedup) : 1f;
        public float Speedup => Speed;

        public float ServiceTime => serviceTime / Speed;
        /// <summary>Сколько одна машина стоит у колонки (≈ 8 игровых минут при обычной скорости часов).</summary>
        public float PumpServiceTime => ServiceTime * 2.5f;
        public float CutterInterval => cutterInterval / Speed;
        public float GiveUpCheckInterval => giveUpCheckInterval / Speed;
        public float DeliveryDuration => deliveryDuration / Speed;
        public float LitersPerSecond => litersPerSecond * Speed;
        public float ClockScale => clockScale * Speed;
    }
}
