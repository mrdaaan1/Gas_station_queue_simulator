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
        [Tooltip("Ускоряет все ожидания, чтобы пройти игру за 3–4 минуты, а не за 20.")]
        public bool fastTestMode = true;
        [Tooltip("Во сколько раз быстрее идёт всё в тестовом режиме.")]
        public float testSpeedup = 6f;

        [Header("Очередь")]
        public int carsAhead = 22;
        public int carsBehind = 14;
        public int maxCarsBehind = 30;
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

        [Header("Часы в очереди")]
        [Tooltip("Таймер стартует не с нуля: игрок уже давно стоит.")]
        public int startMinutes = 80;
        [Tooltip("Сколько игровых секунд проходит за одну реальную.")]
        public float clockScale = 6f;

        float Speed => fastTestMode ? Mathf.Max(1f, testSpeedup) : 1f;

        public float ServiceTime => serviceTime / Speed;
        public float GiveUpCheckInterval => giveUpCheckInterval / Speed;
        public float DeliveryDuration => deliveryDuration / Speed;
        public float LitersPerSecond => litersPerSecond * Speed;
        public float ClockScale => clockScale * Speed;
    }
}
