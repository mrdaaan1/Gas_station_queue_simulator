using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Планировка города — все координаты в одном месте.
    ///
    /// Дорога идёт вдоль оси Z, движение правостороннее. Наше направление — к +Z.
    /// Полосы нашего направления: X = 1.75 (левая), 5.25 (средняя), 8.75 (правая — в ней стоит очередь).
    /// Встречка: X = −1.75, −5.25, −8.75 (едут к −Z).
    /// Справа от дороги тротуар (X 10.5…14), за ним территория заправки (X 14…43, Z −36…46).
    /// Въезд на заправку Z −34…−22, выезд Z 30…42. За заправкой служебный проезд для бензовоза (X ≈ 46.5).
    /// </summary>
    public static class CityLayout
    {
        public const float LaneWidth = 3.5f;
        public const float RoadHalfWidth = 10.5f;
        public const float SidewalkOuter = 14f;

        public const float LaneLeft = 1.75f;
        public const float LaneMiddle = 5.25f;
        public const float LaneQueue = 8.75f;

        public const float RoadStartZ = -560f;
        public const float RoadEndZ = 480f;

        // Территория заправки
        public const float LotMinX = 14f, LotMaxX = 43f, LotMinZ = -36f, LotMaxZ = 46f;
        public const float EntranceMinZ = -34f, EntranceMaxZ = -22f;
        public const float ExitMinZ = 30f, ExitMaxZ = 42f;

        /// <summary>Где очередь заканчивается: первая машина ждёт здесь, перед шлагбаумом.</summary>
        public static readonly Vector3 QueueHead = new Vector3(20.5f, 0f, -16f);

        // Шлагбаум перед колонками
        public const float BarrierZ = -12f;
        public const float BarrierPostX = 24.2f;
        public const float BarrierArmLength = 6.8f;

        // Колонки: два островка, у каждого две стороны
        public static readonly float[] IslandX = { 22f, 30f };
        public const float IslandZ = 2f;
        public static readonly Vector3[] PumpSpots =
        {
            new Vector3(19.6f, 0f, IslandZ), new Vector3(24.4f, 0f, IslandZ),
            new Vector3(27.6f, 0f, IslandZ), new Vector3(32.4f, 0f, IslandZ),
        };

        // Магазин с кассой
        public const float ShopMinX = 36.5f, ShopMaxX = 42.5f, ShopMinZ = -6f, ShopMaxZ = 12f;
        public const float ShopDoorZ = 2f;
        public static readonly Vector3 CashierSpot = new Vector3(41.2f, 0f, 4.5f);
        public static readonly Vector3 CounterFront = new Vector3(38.8f, 0f, 4.5f);
        /// <summary>
        /// Шашлычная у дороги справа, за тротуаром — впереди по ходу очереди: игрок появляется на Z ≈ −142
        /// и видит её с самого начала, а машины перед ним ходят туда есть. Мангал под тентом,
        /// место для заказа на краю тротуара, пластиковый столик. Дома в этом месте не ставим.
        /// </summary>
        public const float ShashlikZ = -100f;
        public static readonly Vector3 MangalSpot = new Vector3(15.9f, 0f, ShashlikZ);
        public static readonly Vector3 ShashlikOrder = new Vector3(14.75f, 0f, ShashlikZ);
        public static readonly Vector3 ShashlikTable = new Vector3(21.5f, 0f, ShashlikZ + 5.5f);

        /// <summary>Зелёный банкомат «СБЕРКАССА» у фасада внутри магазина (слева от входа, подальше от очереди в кассу) и место перед ним.</summary>
        public static readonly Vector3 AtmSpot = new Vector3(36.95f, 0f, -1.9f);
        public static readonly Vector3 AtmFront = new Vector3(37.75f, 0f, -1.9f);
        /// <summary>Заправщик стоит у торца первого островка, лицом к шлагбауму.</summary>
        public static readonly Vector3 AttendantSpot = new Vector3(22f, 0f, -1.7f);

        // Газовая колонка (пропан-бутан) — справа от въезда, в юго-восточном углу участка.
        // Газовые машины стоят в общей очереди только до въезда, потом сворачивают сюда.
        public static readonly Vector3 GasSpot = new Vector3(30.5f, 0f, -28f);       // машина у колонки (носом к +X)
        public static readonly Vector3 GasDispenser = new Vector3(30.5f, 0f, -30.4f); // колонка справа от машины
        public static readonly Vector3 GasBranch = new Vector3(10.2f, 0f, -34f);      // где сворачивают из очереди
        /// <summary>Проезд к выезду вдоль западной стены магазина (в газоне у шлагбаума для него проём).</summary>
        public const float GasExitX = 35.25f;

        public const float TankerLaneX = 46.5f;
        public static readonly Vector3 TankerStop = new Vector3(46.5f, 0f, 2f);

        /// <summary>Машина игрока, проехавшая сюда после заправки, — финал.</summary>
        public const float FinishZ = 90f;

        public static Vector3 P(float x, float z) => new Vector3(x, 0f, z);

        // ---------- Маршруты ----------

        public static LanePath QueuePath() => new LanePath("Queue", 7f, new[]
        {
            P(LaneQueue, RoadStartZ), P(LaneQueue, -42f), P(9.6f, -35f), P(12.5f, -30.5f),
            P(16.5f, -26f), P(19.6f, -21.5f), P(20.5f, -18f), QueueHead,
        });

        public static LanePath MiddleLanePath() =>
            new LanePath("Middle", 11f, new[] { P(LaneMiddle, RoadStartZ), P(LaneMiddle, RoadEndZ) });

        public static LanePath LeftLanePath() =>
            new LanePath("Left", 13f, new[] { P(LaneLeft, RoadStartZ), P(LaneLeft, RoadEndZ) });

        public static LanePath OncomingPath(float x) =>
            new LanePath("Oncoming", 13f, new[] { P(x, RoadEndZ), P(x, RoadStartZ) });

        public static LanePath PumpEnterPath(int pump)
        {
            var spot = PumpSpots[pump];
            return new LanePath($"ToPump{pump + 1}", 4f, new[]
            {
                QueueHead, P(QueueHead.x, -10f), P(spot.x, -4f), P(spot.x, -1f), spot,
            });
        }

        public static LanePath PumpExitPath(int pump)
        {
            var spot = PumpSpots[pump];
            return new LanePath($"FromPump{pump + 1}", 6f, new[]
            {
                spot, P(spot.x, 9f), P(spot.x, 13f), P(20f, 22f), P(16f, 30f), P(12.5f, 35.5f),
                P(9.6f, 40f), P(LaneQueue, 46f), P(LaneQueue, RoadEndZ),
            });
        }

        /// <summary>Служебная колонка «для своих» в дальнем углу заправки — бензин в ней есть всегда.</summary>
        public static readonly Vector3 VipSpot = new Vector3(31f, 0f, 41f);

        /// <summary>
        /// Депутат с мигалкой: по левому ряду мимо всей очереди, через ВЫЕЗД против движения —
        /// прямиком к служебной колонке. Очередь и обычные колонки не пересекает.
        /// </summary>
        public static LanePath VipInPath() => new LanePath("VipIn", 14f, new[]
        {
            P(LaneLeft, RoadStartZ), P(LaneLeft, 5f), P(LaneMiddle, 20f), P(8.2f, 30f), P(11.5f, 37.5f),
            P(15f, 40.5f), P(24f, 41f), VipSpot,
        });

        /// <summary>От служебной колонки — кругом по территории к обычному выезду.</summary>
        public static LanePath VipOutPath() => new LanePath("VipOut", 6f, new[]
        {
            VipSpot, P(37f, 41f), P(40f, 37.5f), P(39f, 31f), P(33f, 28.5f), P(24f, 28f), P(17.5f, 31f),
            P(12.5f, 35.8f), P(9.6f, 40f), P(LaneQueue, 46f), P(LaneQueue, RoadEndZ),
        });

        public static LanePath GasInPath() => new LanePath("ToGas", 4.5f, new[]
        {
            GasBranch, P(13.6f, -32.6f), P(18.5f, -31.2f), P(24f, -29f), GasSpot,
        });

        public static LanePath GasOutPath() => new LanePath("FromGas", 6f, new[]
        {
            GasSpot, P(33.6f, -27.6f), P(GasExitX, -23f), P(GasExitX, -12f), P(GasExitX, 10f), P(31.5f, 16.5f),
            P(24f, 21.5f), P(19.5f, 24.5f), P(16f, 30f), P(12.5f, 35.5f), P(9.6f, 40f), P(LaneQueue, 46f), P(LaneQueue, RoadEndZ),
        });

        public static LanePath TankerArrivePath() =>
            new LanePath("TankerIn", 9f, new[] { P(TankerLaneX, 320f), TankerStop }, false);

        public static LanePath TankerLeavePath() =>
            new LanePath("TankerOut", 12f, new[] { TankerStop, P(TankerLaneX, RoadStartZ) }, false);
    }

    /// <summary>Неподвижные препятствия: столбы, островки, стены, заборы, деревья, дома.</summary>
    public static class Obstacles
    {
        public struct Entry
        {
            public Obb box;
            public string name;
        }

        public static readonly List<Entry> All = new List<Entry>();

        public static void Clear() => All.Clear();

        public static void Add(Obb box, string name) => All.Add(new Entry { box = box, name = name });

        public static void AddBox(Vector3 center, float sizeX, float sizeZ, string name) =>
            Add(Obb.Axis(center, sizeX, sizeZ), name);
    }
}
