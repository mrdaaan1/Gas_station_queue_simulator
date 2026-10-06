using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Уличная гонка по Тольятти (Автозаводский район) — третий режим. Маршрут по карте:
    /// старт на Обводном шоссе → по диагонали вниз по Офицерской (через Южное шоссе) →
    /// налево на ул. 70 лет Октября → кольцо (прямо) → перекрёсток с Тополиной → кольцо (налево) →
    /// ул. Льва Яшина → налево в карман (дублёр) → финиш у ТЦ «Мадагаскар». Около 3,4 км.
    ///
    /// Координаты взяты с карты (пиксели скриншота 2000×942, 1 пиксель ≈ 1,15 м) и перенесены
    /// далеко от города с заправкой (X ≈ 2000…4500), чтобы миры не пересекались.
    /// В мире: X — восток, Z — север (на карте Y вниз — это юг).
    /// </summary>
    public static class TolyattiLayout
    {
        public const float Scale = 1.15f;
        const float OriginX = 3200f;

        /// <summary>Точка карты (пиксели) → мир.</summary>
        public static Vector3 M(float px, float py) => new Vector3((px - 1000f) * Scale + OriginX, 0f, (500f - py) * Scale);

        // ---------- Улицы ----------

        /// <summary>Обводное шоссе, переходящее в Офицерскую: одна прямая с северо-востока на юго-запад.</summary>
        public static readonly Vector3 OfitA = M(905f, -75f), OfitB = M(45f, 585f);
        public static Vector3 OfitDir => (OfitB - OfitA).normalized;
        /// <summary>Офицерская: две проезжие части по 3 полосы (10,5 м) и разделительная полоса 4 м с фонарями.</summary>
        public const float OfitLanes = 10.5f, OfitMedian = 4f;
        /// <summary>Гонка идёт по «нашей» проезжей части (справа по ходу — к северо-западу).</summary>
        public static float OfitRaceOffset => OfitMedian / 2f + OfitLanes / 2f;
        /// <summary>Дублёр Офицерской — параллельно, северо-западнее.</summary>
        public const float DublyorOffset = 30f;

        /// <summary>Южное шоссе — поперёк Офицерской.</summary>
        public static readonly Vector3 YuzhA = M(-60f, 22f), YuzhB = M(2060f, 345f);
        public const float YuzhWidth = 30f;

        /// <summary>Ул. 70 лет Октября: прямая с запада на восток (чуть к югу), 4 полосы, двойная сплошная.</summary>
        public static readonly Vector3 Okt70A = M(40f, 582f), Okt70B = M(1860f, 852f);
        public static Vector3 Okt70Dir => (Okt70B - Okt70A).normalized;
        public const float StreetWidth = 14f;

        /// <summary>Кольца на 70 лет Октября: первое — проезжаем прямо, второе — налево на Льва Яшина.</summary>
        public static readonly Vector3 Ring1 = M(567f, 660f), Ring2 = M(1860f, 852f);
        public const float RingIsland = 20f, RingRace = 25.5f, RingOuter = 31f;

        /// <summary>Перекрёсток с Тополиной (светофоры, «зебра»).</summary>
        public static Vector3 Topolinaya => OnOkt70(1205f);

        /// <summary>Улица через первое кольцо (север–юг).</summary>
        public static readonly Vector3[] Ring1North = { M(632f, 150f), M(606f, 420f) };
        public static readonly Vector3[] Ring1South = { M(545f, 800f), M(528f, 980f) };
        public static readonly Vector3[] TopolinayaNorth = { M(1285f, 240f), M(1238f, 500f) };
        public static readonly Vector3[] TopolinayaSouth = { M(1192f, 860f), M(1180f, 980f) };
        public static readonly Vector3 Ring2SouthWest = M(1745f, 975f), Ring2NorthEast = M(2070f, 728f), Ring2South = M(1845f, 1000f);

        /// <summary>Ул. Льва Яшина: от второго кольца на север.</summary>
        public static Vector3 YashinaDir => (M(1918f, 340f) - M(1858f, 800f)).normalized;
        public static Vector3 YashinaLeft => new Vector3(-YashinaDir.z, 0f, YashinaDir.x);
        public const float YashinaWidth = 15f;
        /// <summary>
        /// Карман — дублёр Льва Яшина слева, за газоном с парковкой. Въезд вскоре после кольца,
        /// по левую руку магазины («Домовой», «Сбербанк»), в конце — ТЦ «Мадагаскар».
        /// </summary>
        public const float PocketOffset = 19f, PocketWidth = 8f, ParkingOffset = 12.5f;
        public const float PocketEnterFrom = 100f, PocketEnterTo = 150f, PocketEnd = 410f, FinishDist = 330f;
        /// <summary>Точка на Льва Яшина: dist — от центра второго кольца, side — влево (+) от оси.</summary>
        public static Vector3 OnYashina(float dist, float side = 0f) => Ring2 + YashinaDir * dist + YashinaLeft * side;

        /// <summary>Точка на оси 70 лет Октября под пикселем карты px.</summary>
        public static Vector3 OnOkt70(float px)
        {
            float t = (px - 40f) / (1860f - 40f);
            return Vector3.Lerp(Okt70A, Okt70B, t);
        }

        /// <summary>Перекрёсток Офицерской (её ось) и 70 лет Октября.</summary>
        public static Vector3 OfitOkt70 => RaceTrack.Intersect(OfitA, OfitDir, Okt70A, Okt70Dir);

        static Vector3 RightOf(Vector3 d) => new Vector3(d.z, 0f, -d.x);

        // ---------- Трасса ----------

        public const float TopSpeed = 54f, CornerGrip = 24f, Braking = 13f;
        public const float StartLineS = 40f;

        public static RaceTrack Build()
        {
            var center = Center(out var hints);
            var track = new RaceTrack
            {
                Name = "Тольятти: Офицерская — 70 лет Октября — Льва Яшина",
                FuelStop = false,
                Center = center,
                CenterPath = new LanePath("TltCenter", 1f, center, false),
                Left = new LanePath("TltLeft", TopSpeed, RaceLayout.Offset(center, -RaceLayout.LaneOffset), false),
                Right = new LanePath("TltRight", TopSpeed, RaceLayout.Offset(center, RaceLayout.LaneOffset), false),
                Traffic = new LanePath("TltTraffic", 12f, RaceLayout.Offset(center, RaceLayout.LaneOffset), false),
                StartLineS = StartLineS,
                TopSpeed = TopSpeed,
                CornerGrip = CornerGrip,
                Braking = Braking,
            };
            var finish = OnYashina(FinishDist, PocketOffset);
            track.FinishA = finish - YashinaLeft * (PocketWidth / 2f + 0.5f);
            track.FinishB = finish + YashinaLeft * (PocketWidth / 2f + 0.5f);
            track.FinishS = track.CenterPath.Project(finish, out _);
            foreach (var (p, text) in hints)
                track.Hints.Add((track.CenterPath.Project(p, out _), text));
            track.Hints.Add((track.FinishS, "ФИНИШ у ТЦ «Мадагаскар»"));
            track.Hints.Sort((a, b) => a.s.CompareTo(b.s));
            return track;
        }

        /// <summary>Ось гонки: по нашей проезжей части Офицерской, через перекрёсток, через кольца по кругу (против часовой).</summary>
        public static List<Vector3> Center(out List<(Vector3, string)> hints)
        {
            hints = new List<(Vector3, string)>();
            var pts = new List<Vector3>();
            var dOf = OfitDir;
            var d70 = Okt70Dir;
            var dYa = YashinaDir;

            // Наша проезжая часть Офицерской — правее оси улицы; поворот — где она пересекает ось 70 лет Октября
            var raceLineOrigin = OfitA + RightOf(dOf) * OfitRaceOffset;
            var corner = RaceTrack.Intersect(raceLineOrigin, dOf, Okt70A, d70);
            var start = corner - dOf * 1140f;
            pts.Add(start);
            hints.Add((corner, "Налево на ул. 70 лет Октября"));

            // Первое кольцо — прямо (против часовой, по южной стороне острова)
            var a1 = Ring1 - d70 * 58f;
            RaceTrack.AddCorner(pts, start, corner, a1, 24f);
            Roundabout(pts, Ring1, d70, d70);
            hints.Add((Ring1, "Кольцо — прямо по 70 лет Октября"));
            hints.Add((Topolinaya, "Перекрёсток с Тополиной — прямо"));

            // Второе кольцо — налево на Льва Яшина (три четверти круга)
            Roundabout(pts, Ring2, d70, dYa);
            hints.Add((Ring2, "Кольцо — налево на ул. Льва Яшина"));

            // По Льва Яшина и налево в карман (дублёр) к «Мадагаскару»
            var enter0 = OnYashina(PocketEnterFrom);
            var enter1 = OnYashina(PocketEnterTo, PocketOffset);
            pts.Add(enter0);
            RaceTrack.AddHermite(pts, enter0, dYa, enter1, dYa);
            pts.Add(OnYashina(PocketEnd, PocketOffset));
            hints.Add((enter0, "Налево в карман — к ТЦ «Мадагаскар»"));
            return RaceTrack.Clean(pts);
        }

        /// <summary>
        /// Проезд кольца: подъезд по оси дороги, плавный въезд на круг, по кругу против часовой стрелки
        /// (правостороннее движение), плавный съезд в направлении outDir.
        /// </summary>
        static void Roundabout(List<Vector3> pts, Vector3 c, Vector3 inDir, Vector3 outDir)
        {
            const float approach = 58f, entryShift = 34f;
            var a = c - inDir * approach;
            var b = c + outDir * approach;
            float inAngle = Angle(-inDir) + entryShift;
            float outAngle = Angle(outDir) - entryShift;
            while (outAngle <= inAngle + 10f) outAngle += 360f;
            var e = c + Dir(inAngle) * RingRace;
            var x = c + Dir(outAngle) * RingRace;
            pts.Add(a);
            RaceTrack.AddHermite(pts, a, inDir, e, Tangent(inAngle));
            RaceTrack.AddArc(pts, c, RingRace, inAngle, outAngle);
            RaceTrack.AddHermite(pts, x, Tangent(outAngle), b, outDir);
        }

        static float Angle(Vector3 d) => Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg;
        static Vector3 Dir(float deg) => new Vector3(Mathf.Cos(deg * Mathf.Deg2Rad), 0f, Mathf.Sin(deg * Mathf.Deg2Rad));
        /// <summary>Направление движения по кругу против часовой.</summary>
        static Vector3 Tangent(float deg) => new Vector3(-Mathf.Sin(deg * Mathf.Deg2Rad), 0f, Mathf.Cos(deg * Mathf.Deg2Rad));

        // ---------- Перекрытия (бетонные блоки поперёк боковых улиц) ----------

        /// <summary>Линии перекрытий: середина и направление поперёк, ширина.</summary>
        public static List<(Vector3 mid, Vector3 across, float width)> Closures()
        {
            var list = new List<(Vector3, Vector3, float)>();
            var dOf = OfitDir;
            var dY = (YuzhB - YuzhA).normalized;
            var jy = RaceTrack.Intersect(OfitA, dOf, YuzhA, dY);
            // Южное шоссе по обе стороны от Офицерской
            foreach (float side in new[] { -1f, 1f })
                list.Add((jy + dY * side * 30f, RightOf(dY), YuzhWidth + 2f));
            // Позади стартовой решётки — поперёк нашей проезжей части
            // (её ставит строитель по оси трассы, здесь — только боковые улицы)
            // Офицерская дальше поворота и 70 лет Октября западнее перекрёстка
            var j = OfitOkt70;
            list.Add((j + dOf * 34f, RightOf(dOf), OfitLanes * 2f + OfitMedian + 2f));
            list.Add((j - Okt70Dir * 34f, RightOf(Okt70Dir), StreetWidth + 2f));
            // Боковые улицы у колец и Тополиной
            list.Add(Arm(Ring1, Ring1North[1], RingOuter + 6f));
            list.Add(Arm(Ring1, Ring1South[0], RingOuter + 6f));
            list.Add(Arm(Topolinaya, TopolinayaNorth[1], StreetWidth / 2f + 6f));
            list.Add(Arm(Topolinaya, TopolinayaSouth[0], StreetWidth / 2f + 6f));
            list.Add(Arm(Ring2, Ring2SouthWest, RingOuter + 6f));
            list.Add(Arm(Ring2, Ring2NorthEast, RingOuter + 6f));
            list.Add(Arm(Ring2, Ring2South, RingOuter + 6f));
            // Льва Яшина дальше кармана: к финишу только через карман
            list.Add((OnYashina(PocketEnterTo + 8f), RightOf(YashinaDir), YashinaWidth + 1f));
            return list;
        }

        static (Vector3, Vector3, float) Arm(Vector3 c, Vector3 toward, float dist)
        {
            var d = (toward - c).normalized;
            return (c + d * dist, RightOf(d), StreetWidth + 2f);
        }
    }
}
