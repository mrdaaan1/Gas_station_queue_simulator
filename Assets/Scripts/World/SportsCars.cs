using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>Машины, которые может выбрать игрок.</summary>
    public enum PlayerCarKind { Vaz2107, Supra, Gelik, Skyline, Rx7, S2000, X5, A7, Mater }

    /// <summary>
    /// Спорткары из гладких сеток (<see cref="SupraModel"/> и следующие): собирает объекты Unity
    /// и заполняет <see cref="CarVisual"/> — колёса, руль, стрелки, зеркала, фары, детали для поломок.
    /// </summary>
    public static class SportsCars
    {
        public static string Title(PlayerCarKind kind) =>
            kind == PlayerCarKind.Supra ? "Тоёта Супра (1997)" : kind == PlayerCarKind.Gelik ? "Гелик (2025)" :
            kind == PlayerCarKind.Skyline ? "Скайлайн GT-R (1999)" : kind == PlayerCarKind.Rx7 ? "Мазда RX-7 (1993)" : kind == PlayerCarKind.S2000 ? "Хонда S2000 (2001)" : kind == PlayerCarKind.X5 ? "БМВ Х5 М — тачка Давидыча" : kind == PlayerCarKind.A7 ? "Ауди A7 Sportback (2019)" : kind == PlayerCarKind.Mater ? "Мэтр — эвакуатор из «Тачек»" : "ВАЗ-2107";

        /// <summary>Розовая, как у Суки.</summary>
        public static readonly Color S2000Pink = Shapes.Hex("#f24fa0");

        /// <summary>Красная, как у Доминика в первом «Форсаже».</summary>
        public static readonly Color Rx7Red = Shapes.Hex("#d4141c");

        /// <summary>Серебристый, как у Брайана во «Двойном форсаже».</summary>
        public static readonly Color SkylineSilver = Shapes.Hex("#c3c7cc");

        /// <summary>Синий матовый «Гелик» как на фото.</summary>
        public static readonly Color GelikBlue = Shapes.Hex("#2b44b0");

        /// <summary>Золотистый — для подкраски деталей X5 (сам кузов — камуфляжная текстура).</summary>
        public static readonly Color X5Gold = Shapes.Hex("#d8a53f");

        /// <summary>Серебристый металлик «Флорет», как на фото A7.</summary>
        public static readonly Color A7Silver = Shapes.Hex("#b8bcc1");

        /// <summary>Цвет по умолчанию — как у красной «Супры» с фото.</summary>
        public static readonly Color SupraRed = Shapes.Hex("#b80f18");

        public static CarVisual BuildSupra(string name, Color paint)
        {
            var root = new GameObject(name).transform;
            var visual = root.gameObject.AddComponent<CarVisual>();
            var map = ModelSpawner.Spawn(SupraModel.Get().root, root, key => CarMaterials.Get(key, paint));

            visual.body = map["Body"];
            visual.length = 4.52f;
            visual.width = 1.81f;
            visual.height = 1.275f;
            visual.hoodTop = 0.82f;
            visual.rightHandDrive = true;
            visual.maxSpeed = 50f;        // 180 км/ч — японский ограничитель
            visual.accel = 7.5f;
            visual.speedoMaxKmh = 180f;
            visual.wheelRadius = SupraModel.WheelR;
            visual.sporty = true;

            foreach (var tag in new[] { "FL", "FR", "RL", "RR" })
                visual.wheels.Add(map["Wheel" + tag]);
            visual.frontSteer.Add(map["SteerFL"]);
            visual.frontSteer.Add(map["SteerFR"]);

            visual.steeringWheel = map["SteeringWheel"];
            visual.steeringTilt = SupraModel.SteeringTilt;
            visual.speedNeedle = map["SpeedNeedle"];
            visual.fuelNeedle = map["FuelNeedle"];
            visual.tachNeedle = map["TachNeedle"];
            visual.fuelLamp = map["FuelLamp"].GetComponent<Renderer>();
            visual.engineLamp = map["EngineLamp"].GetComponent<Renderer>();
            visual.radioDisplay = Fonts.WorldText(map["RadioScreen"], new Vector3(0f, 0f, -0.002f), "", Shapes.Hex("#7dffa8"), 0.0034f);

            visual.mirrorLeft = map["MirrorGlassL"];
            visual.mirrorRight = map["MirrorGlassR"];
            visual.mirrorRear = map["RearMirrorGlass"];

            visual.driverHead = map["DriverHead"];
            visual.driverTorso = map["DriverTorso"];
            AddArms(visual, SupraModel.DriverEyes, 0.98f, -0.92f, 0.172f);
            visual.driverEyes = Shapes.Group("DriverEyes", root, SupraModel.DriverEyes);
            visual.driverDoorLocal = new Vector3(1.45f, 0f, -0.75f);
            visual.fuelCapLocal = new Vector3(0.93f, 0.83f, -1.12f);

            visual.headlights.Add(map["HeadlightL"]);
            visual.headlights.Add(map["HeadlightR"]);
            visual.taillights.Add(map["TaillightL"]);
            visual.taillights.Add(map["TaillightR"]);
            visual.bumperFront = map["BumperF"];
            visual.bumperRear = map["BumperR"];
            visual.frontPanel = map["Hood"];
            visual.rearPanel = map["Trunk"];
            visual.doors.Add(map["DoorL"]);
            visual.doors.Add(map["DoorR"]);
            visual.roof = map["Roof"];
            visual.leftBlinkers.Add(map["BlinkFL"].GetComponent<Renderer>());
            visual.leftBlinkers.Add(map["BlinkRL"].GetComponent<Renderer>());
            visual.rightBlinkers.Add(map["BlinkFR"].GetComponent<Renderer>());
            visual.rightBlinkers.Add(map["BlinkRR"].GetComponent<Renderer>());
            visual.blinkOffMat = CarMaterials.Get("amber", paint);

            PlateText(map["PlateFront"], "Е 555 КХ");
            PlateText(map["PlateRear"], "Е 555 КХ");
            return visual;
        }

        /// <summary>«Гелик» 2025: руль слева, цифровые приборы, матовая краска, высокий кузов.</summary>
        public static CarVisual BuildGelik(string name, Color paint)
        {
            var root = new GameObject(name).transform;
            var visual = root.gameObject.AddComponent<CarVisual>();
            var map = ModelSpawner.Spawn(GelikModel.Get().root, root, key => CarMaterials.Get(key, paint, 0.5f));

            visual.body = map["Body"];
            visual.length = 4.85f;
            visual.width = 1.98f;
            visual.height = 1.96f;
            visual.hoodTop = 1.2f;
            visual.rightHandDrive = false;
            visual.maxSpeed = 61f;        // 220 км/ч
            visual.accel = 6f;
            visual.speedoMaxKmh = 260f;
            visual.wheelRadius = GelikModel.WheelR;
            visual.sporty = true;
            visual.rearMirrorNear = 3.15f; // за запаской

            foreach (var tag in new[] { "FL", "FR", "RL", "RR" })
                visual.wheels.Add(map["Wheel" + tag]);
            visual.frontSteer.Add(map["SteerFL"]);
            visual.frontSteer.Add(map["SteerFR"]);

            visual.steeringWheel = map["SteeringWheel"];
            visual.steeringTilt = GelikModel.SteeringTilt;
            var white = Color.white;
            visual.digitalSpeed = Fonts.WorldText(map["ClusterScreen"], new Vector3(-0.13f, 0f, -0.003f), "", white, 0.0034f);
            visual.digitalFuel = Fonts.WorldText(map["ClusterScreen"], new Vector3(0.13f, 0f, -0.003f), "", white, 0.0026f);
            visual.radioDisplay = Fonts.WorldText(map["RadioScreen"], new Vector3(0f, 0f, -0.003f), "", Shapes.Hex("#9fd0ff"), 0.0034f);

            visual.mirrorLeft = map["MirrorGlassL"];
            visual.mirrorRight = map["MirrorGlassR"];
            visual.mirrorRear = map["RearMirrorGlass"];

            visual.driverHead = map["DriverHead"];
            visual.driverTorso = map["DriverTorso"];
            AddArms(visual, GelikModel.DriverEyes, 1.42f, -0.58f, 0.182f);
            visual.driverEyes = Shapes.Group("DriverEyes", root, GelikModel.DriverEyes);
            visual.driverDoorLocal = new Vector3(-1.5f, 0f, -0.4f);
            visual.fuelCapLocal = new Vector3(0.95f, 1.12f, -1.75f);

            visual.headlights.Add(map["HeadlightL"]);
            visual.headlights.Add(map["HeadlightR"]);
            visual.taillights.Add(map["TaillightL"]);
            visual.taillights.Add(map["TaillightR"]);
            visual.bumperFront = map["BumperF"];
            visual.bumperRear = map["BumperR"];
            visual.frontPanel = map["Hood"];
            visual.rearPanel = map["Tailgate"];
            foreach (var d in new[] { "DoorFL", "DoorFR", "DoorRL", "DoorRR" }) visual.doors.Add(map[d]);
            visual.roof = map["Roof"];
            visual.leftBlinkers.Add(map["BlinkFL"].GetComponent<Renderer>());
            visual.leftBlinkers.Add(map["BlinkRL"].GetComponent<Renderer>());
            visual.rightBlinkers.Add(map["BlinkFR"].GetComponent<Renderer>());
            visual.rightBlinkers.Add(map["BlinkRR"].GetComponent<Renderer>());
            visual.blinkOffMat = CarMaterials.Get("amber", paint);

            PlateText(map["PlateFront"], "М 777 ММ");
            PlateText(map["PlateRear"], "М 777 ММ");
            return visual;
        }

        /// <summary>
        /// BMW X5 M (E70) в золотом хромированном камуфляже («тачка Давидыча»): руль слева, цифровой щиток,
        /// наклейки-питбули на капоте и дверях. Кузов — материал «paint», подменённый на камуфляжную текстуру.
        /// </summary>
        public static CarVisual BuildX5(string name, Color paint)
        {
            var root = new GameObject(name).transform;
            var visual = root.gameObject.AddComponent<CarVisual>();
            var map = ModelSpawner.Spawn(X5Source().root, root, key => CarMaterials.Get(key == "paint" ? "gold_camo" : key == "glass" ? "glass_tint" : key, paint));

            visual.body = map["Body"];
            visual.length = 4.88f;
            visual.width = 1.96f;
            visual.height = 1.76f;
            visual.hoodTop = 1.12f;
            visual.rightHandDrive = false;
            visual.maxSpeed = 66f;        // ~240 км/ч — 555 л.с.
            visual.accel = 7.0f;
            visual.speedoMaxKmh = 280f;
            visual.wheelRadius = X5Model.WheelR;
            visual.sporty = true;

            foreach (var tag in new[] { "FL", "FR", "RL", "RR" })
                visual.wheels.Add(map["Wheel" + tag]);
            visual.frontSteer.Add(map["SteerFL"]);
            visual.frontSteer.Add(map["SteerFR"]);

            visual.steeringWheel = map["SteeringWheel"];
            visual.steeringTilt = X5Model.SteeringTilt;
            var white = Color.white;
            visual.digitalSpeed = Fonts.WorldText(map["ClusterScreen"], new Vector3(-0.11f, 0f, -0.003f), "", white, 0.0032f);
            visual.digitalFuel = Fonts.WorldText(map["ClusterScreen"], new Vector3(0.11f, 0f, -0.003f), "", white, 0.0025f);
            visual.radioDisplay = Fonts.WorldText(map["RadioScreen"], new Vector3(0f, 0f, -0.003f), "", Shapes.Hex("#9fd0ff"), 0.0032f);

            visual.mirrorLeft = map["MirrorGlassL"];
            visual.mirrorRight = map["MirrorGlassR"];
            visual.mirrorRear = map["RearMirrorGlass"];

            visual.driverHead = map["DriverHead"];
            visual.driverTorso = map["DriverTorso"];
            AddArms(visual, X5Model.DriverEyes, 1.27f, -0.40f, 0.172f);
            visual.driverEyes = Shapes.Group("DriverEyes", root, X5Model.DriverEyes);
            visual.driverDoorLocal = new Vector3(-1.5f, 0f, 0.2f);
            visual.fuelCapLocal = new Vector3(0.98f, 0.98f, -1.70f);

            visual.headlights.Add(map["HeadlightL"]);
            visual.headlights.Add(map["HeadlightR"]);
            visual.taillights.Add(map["TaillightL"]);
            visual.taillights.Add(map["TaillightR"]);
            visual.bumperFront = map["BumperF"];
            visual.bumperRear = map["BumperR"];
            visual.frontPanel = map["Hood"];
            visual.rearPanel = map["Tailgate"];
            foreach (var d in new[] { "DoorFL", "DoorFR", "DoorRL", "DoorRR" }) visual.doors.Add(map[d]);
            visual.roof = map["Roof"];
            visual.leftBlinkers.Add(map["BlinkFL"].GetComponent<Renderer>());
            visual.leftBlinkers.Add(map["BlinkRL"].GetComponent<Renderer>());
            visual.rightBlinkers.Add(map["BlinkFR"].GetComponent<Renderer>());
            visual.rightBlinkers.Add(map["BlinkRR"].GetComponent<Renderer>());
            visual.blinkOffMat = CarMaterials.Get("amber", paint);

            PlateText(map["PlateFront"], "Д 777 ДД");
            PlateText(map["PlateRear"], "Д 777 ДД");
            root.gameObject.AddComponent<ChromeProbe>(); // хром отражает улицу, а не только небо
            return visual;
        }

        static readonly Dictionary<string, Model> modelFiles = new Dictionary<string, Model>();

        /// <summary>
        /// Модель, доведённая в Blender (Assets/Resources/Models/&lt;name&gt;.bytes, готовит Tools/Blender/car_build.py) —
        /// с толщиной панелей, скруглёнными кромками, детальными колёсами и салоном; если файла нет — построенная кодом.
        /// </summary>
        static Model FileOr(string name, System.Func<Model> fromCode)
        {
            if (!modelFiles.TryGetValue(name, out var model))
            {
                var file = Resources.Load<TextAsset>("Models/" + name);
                if (file != null)
                {
                    try { model = ModelFile.Read(file.bytes); }
                    catch (System.Exception e) { Debug.LogWarning(name + ".bytes не прочитан, беру модель из кода: " + e.Message); }
                }
                modelFiles[name] = model;
            }
            return model ?? fromCode();
        }

        static Model X5Source() => FileOr("X5", X5Model.Get);

        /// <summary>
        /// Мэтр — ржавый эвакуатор из «Тачек» (ВНИМАНИЕ: персонаж Disney/Pixar — перед выпуском в Steam убрать или заменить
        /// своим). Модель целиком из Blender (Tools/Blender/mater_build.py → Models/Mater.bytes): глаза на лобовом стекле,
        /// зубы и бампер-улыбка, одна фара, кран с крюком, сдвоенные задние колёса. Если файла нет — обычная «семёрка».
        /// </summary>
        public static CarVisual BuildMater(string name)
        {
            var model = FileOr("Mater", () => null);
            if (model == null) return CarFactory.Build(name, Shapes.Hex("#8a5a3a"), CarModel.Vaz2107, true);
            var root = new GameObject(name).transform;
            var visual = root.gameObject.AddComponent<CarVisual>();
            var map = ModelSpawner.Spawn(model.root, root, key => CarMaterials.Get(key == "glass" ? "glass_tint" : key, Color.white));

            visual.body = map["Body"];
            visual.length = 4.8f;
            visual.width = 2.12f;
            visual.height = 2.45f;
            visual.hoodTop = 1.40f;
            visual.rightHandDrive = false;
            visual.maxSpeed = 34f;        // ~120 км/ч — старый эвакуатор, зато задом умеет
            visual.accel = 4.2f;
            visual.speedoMaxKmh = 120f;
            visual.wheelRadius = 0.42f;
            visual.sporty = false;

            foreach (var tag in new[] { "FL", "FR", "RL", "RR" })
                visual.wheels.Add(map["Wheel" + tag]);
            visual.frontSteer.Add(map["SteerFL"]);
            visual.frontSteer.Add(map["SteerFR"]);

            visual.steeringWheel = map["SteeringWheel"];
            visual.steeringTilt = -45f;
            visual.speedNeedle = map["SpeedNeedle"];
            visual.fuelNeedle = map["FuelNeedle"];
            visual.fuelLamp = map["FuelLamp"].GetComponent<Renderer>();
            visual.engineLamp = map["EngineLamp"].GetComponent<Renderer>();
            visual.radioDisplay = Fonts.WorldText(map["RadioScreen"], new Vector3(0f, 0f, -0.003f), "", Shapes.Hex("#ffb84a"), 0.0026f);

            visual.mirrorLeft = map["MirrorGlassL"];
            visual.mirrorRight = map["MirrorGlassR"];
            visual.mirrorRear = map["RearMirrorGlass"];

            var eyes = new Vector3(-0.40f, 1.62f, -0.15f);
            visual.driverHead = map["DriverHead"];
            visual.driverTorso = map["DriverTorso"];
            AddArms(visual, eyes, 1.48f, -0.32f, 0.205f);
            visual.driverEyes = Shapes.Group("DriverEyes", root, eyes);
            visual.driverDoorLocal = new Vector3(-1.5f, 0f, 0.0f);
            visual.fuelCapLocal = new Vector3(0.98f, 1.0f, -0.62f);

            visual.headlights.Add(map["HeadlightL"]);
            visual.headlights.Add(map["HeadlightR"]);
            visual.taillights.Add(map["TaillightL"]);
            visual.taillights.Add(map["TaillightR"]);
            visual.bumperFront = map["BumperF"];
            visual.bumperRear = map["BumperR"];
            visual.frontPanel = map["Hood"];
            visual.rearPanel = map["Tailgate"];
            visual.doors.Add(map["DoorFL"]);
            visual.doors.Add(map["DoorFR"]);
            visual.roof = map["Roof"];
            visual.leftBlinkers.Add(map["BlinkFL"].GetComponent<Renderer>());
            visual.leftBlinkers.Add(map["BlinkRL"].GetComponent<Renderer>());
            visual.rightBlinkers.Add(map["BlinkFR"].GetComponent<Renderer>());
            visual.rightBlinkers.Add(map["BlinkRR"].GetComponent<Renderer>());
            visual.blinkOffMat = CarMaterials.Get("amber", Color.white);

            // Надписи на дверях и номер
            var cream = Shapes.Hex("#f3ead2");
            foreach (var t in new[] { map["DoorTextL"], map["DoorTextR"] })
            {
                Fonts.WorldText(t, new Vector3(0f, 0.09f, -0.004f), "Tow Mater", cream, 0.0105f);
                Fonts.WorldText(t, new Vector3(0f, -0.02f, -0.004f), "TOWING & SALVAGE", cream, 0.0042f);
                Fonts.WorldText(t, new Vector3(0f, -0.09f, -0.004f), "Radiator Springs", cream, 0.0055f);
            }
            Fonts.WorldText(map["PlateRear"], new Vector3(0f, 0f, -0.0075f), "A113", Shapes.Hex("#1a1a1a"), 0.0105f);
            return visual;
        }

        /// <summary>
        /// Audi A7 Sportback (C8): серебристый фастбэк, руль слева, бежевый салон, цифровой щиток и экраны MMI.
        /// </summary>
        public static CarVisual BuildA7(string name, Color paint)
        {
            var root = new GameObject(name).transform;
            var visual = root.gameObject.AddComponent<CarVisual>();
            var map = ModelSpawner.Spawn(FileOr("A7", A7Model.Get).root, root, key => CarMaterials.Get(key == "glass" ? "glass_tint" : key, paint, 0.88f));

            visual.body = map["Body"];
            visual.length = 4.97f;
            visual.width = 1.91f;
            visual.height = 1.42f;
            visual.hoodTop = 0.98f;
            visual.rightHandDrive = false;
            visual.maxSpeed = 69f;        // 250 км/ч — электронный ограничитель
            visual.accel = 7.2f;
            visual.speedoMaxKmh = 300f;
            visual.wheelRadius = A7Model.WheelR;
            visual.sporty = true;

            foreach (var tag in new[] { "FL", "FR", "RL", "RR" })
                visual.wheels.Add(map["Wheel" + tag]);
            visual.frontSteer.Add(map["SteerFL"]);
            visual.frontSteer.Add(map["SteerFR"]);

            visual.steeringWheel = map["SteeringWheel"];
            visual.steeringTilt = A7Model.SteeringTilt;
            var white = Color.white;
            visual.digitalSpeed = Fonts.WorldText(map["ClusterScreen"], new Vector3(-0.10f, 0f, -0.003f), "", white, 0.0030f);
            visual.digitalFuel = Fonts.WorldText(map["ClusterScreen"], new Vector3(0.10f, 0f, -0.003f), "", white, 0.0024f);
            visual.radioDisplay = Fonts.WorldText(map["RadioScreen"], new Vector3(0f, 0f, -0.003f), "", Shapes.Hex("#e8eef5"), 0.0030f);

            visual.mirrorLeft = map["MirrorGlassL"];
            visual.mirrorRight = map["MirrorGlassR"];
            visual.mirrorRear = map["RearMirrorGlass"];

            visual.driverHead = map["DriverHead"];
            visual.driverTorso = map["DriverTorso"];
            AddArms(visual, A7Model.DriverEyes, 1.00f, -0.50f, 0.176f);
            visual.driverEyes = Shapes.Group("DriverEyes", root, A7Model.DriverEyes);
            visual.driverDoorLocal = new Vector3(-1.45f, 0f, 0.1f);
            visual.fuelCapLocal = new Vector3(0.97f, 0.90f, -1.66f);

            visual.headlights.Add(map["HeadlightL"]);
            visual.headlights.Add(map["HeadlightR"]);
            visual.taillights.Add(map["TaillightL"]);
            visual.taillights.Add(map["TaillightR"]);
            visual.bumperFront = map["BumperF"];
            visual.bumperRear = map["BumperR"];
            visual.frontPanel = map["Hood"];
            visual.rearPanel = map["Tailgate"];
            foreach (var d in new[] { "DoorFL", "DoorFR", "DoorRL", "DoorRR" }) visual.doors.Add(map[d]);
            visual.roof = map["Roof"];
            visual.leftBlinkers.Add(map["BlinkFL"].GetComponent<Renderer>());
            visual.leftBlinkers.Add(map["BlinkRL"].GetComponent<Renderer>());
            visual.rightBlinkers.Add(map["BlinkFR"].GetComponent<Renderer>());
            visual.rightBlinkers.Add(map["BlinkRR"].GetComponent<Renderer>());
            visual.blinkOffMat = CarMaterials.Get("amber", paint);

            PlateText(map["PlateFront"], "А 007 АА");
            PlateText(map["PlateRear"], "А 007 АА");
            root.gameObject.AddComponent<ChromeProbe>(); // серебристый металлик отражает улицу
            return visual;
        }

        /// <summary>«Скайлайн» R34 из «Двойного форсажа»: правый руль, стрелочные приборы, раскраска полосами.</summary>
        public static CarVisual BuildSkyline(string name, Color paint)
        {
            var root = new GameObject(name).transform;
            var visual = root.gameObject.AddComponent<CarVisual>();
            var map = ModelSpawner.Spawn(SkylineModel.Get().root, root, key => CarMaterials.Get(key, paint, 0.85f));

            visual.body = map["Body"];
            visual.length = 4.6f;
            visual.width = 1.785f;
            visual.height = 1.36f;
            visual.hoodTop = 0.8f;
            visual.rightHandDrive = true;
            visual.maxSpeed = 50f;        // 180 км/ч — японский ограничитель
            visual.accel = 8.5f;          // полный привод, стартует резче всех
            visual.speedoMaxKmh = 180f;
            visual.wheelRadius = SkylineModel.WheelR;
            visual.sporty = true;

            foreach (var tag in new[] { "FL", "FR", "RL", "RR" })
                visual.wheels.Add(map["Wheel" + tag]);
            visual.frontSteer.Add(map["SteerFL"]);
            visual.frontSteer.Add(map["SteerFR"]);

            visual.steeringWheel = map["SteeringWheel"];
            visual.steeringTilt = SkylineModel.SteeringTilt;
            visual.speedNeedle = map["SpeedNeedle"];
            visual.fuelNeedle = map["FuelNeedle"];
            visual.tachNeedle = map["TachNeedle"];
            visual.fuelLamp = map["FuelLamp"].GetComponent<Renderer>();
            visual.engineLamp = map["EngineLamp"].GetComponent<Renderer>();
            visual.radioDisplay = Fonts.WorldText(map["RadioScreen"], new Vector3(0f, 0f, -0.002f), "", Shapes.Hex("#7dffa8"), 0.0034f);

            visual.mirrorLeft = map["MirrorGlassL"];
            visual.mirrorRight = map["MirrorGlassR"];
            visual.mirrorRear = map["RearMirrorGlass"];

            visual.driverHead = map["DriverHead"];
            visual.driverTorso = map["DriverTorso"];
            AddArms(visual, SkylineModel.DriverEyes, 1.03f, -0.86f, 0.168f);
            visual.driverEyes = Shapes.Group("DriverEyes", root, SkylineModel.DriverEyes);
            visual.driverDoorLocal = new Vector3(1.45f, 0f, -0.7f);
            visual.fuelCapLocal = new Vector3(0.92f, 0.84f, -1.62f);

            visual.headlights.Add(map["HeadlightL"]);
            visual.headlights.Add(map["HeadlightR"]);
            visual.taillights.Add(map["TaillightL"]);
            visual.taillights.Add(map["TaillightR"]);
            visual.bumperFront = map["BumperF"];
            visual.bumperRear = map["BumperR"];
            visual.frontPanel = map["Hood"];
            visual.rearPanel = map["Trunk"];
            visual.doors.Add(map["DoorL"]);
            visual.doors.Add(map["DoorR"]);
            visual.roof = map["Roof"];
            visual.leftBlinkers.Add(map["BlinkFL"].GetComponent<Renderer>());
            visual.leftBlinkers.Add(map["BlinkRL"].GetComponent<Renderer>());
            visual.rightBlinkers.Add(map["BlinkFR"].GetComponent<Renderer>());
            visual.rightBlinkers.Add(map["BlinkRR"].GetComponent<Renderer>());
            visual.blinkOffMat = CarMaterials.Get("amber", paint);

            PlateText(map["PlateFront"], "Р 340 ТР");
            PlateText(map["PlateRear"], "Р 340 ТР");
            return visual;
        }

        /// <summary>«Мазда RX-7» Доминика из «Форсажа»: руль слева, стрелочные приборы, «когти» на кузове.</summary>
        public static CarVisual BuildRx7(string name, Color paint)
        {
            var root = new GameObject(name).transform;
            var visual = root.gameObject.AddComponent<CarVisual>();
            var map = ModelSpawner.Spawn(Rx7Model.Get().root, root, key => CarMaterials.Get(key, paint, 0.85f));

            visual.body = map["Body"];
            visual.length = 4.3f;
            visual.width = 1.83f;
            visual.height = 1.23f;
            visual.hoodTop = 0.76f;
            visual.rightHandDrive = false;
            visual.maxSpeed = 58f;        // ~210 км/ч, роторный мотор крутится до отсечки
            visual.accel = 7.8f;
            visual.speedoMaxKmh = 260f;
            visual.wheelRadius = Rx7Model.WheelR;
            visual.sporty = true;

            foreach (var tag in new[] { "FL", "FR", "RL", "RR" })
                visual.wheels.Add(map["Wheel" + tag]);
            visual.frontSteer.Add(map["SteerFL"]);
            visual.frontSteer.Add(map["SteerFR"]);

            visual.steeringWheel = map["SteeringWheel"];
            visual.steeringTilt = Rx7Model.SteeringTilt;
            visual.speedNeedle = map["SpeedNeedle"];
            visual.fuelNeedle = map["FuelNeedle"];
            visual.tachNeedle = map["TachNeedle"];
            visual.fuelLamp = map["FuelLamp"].GetComponent<Renderer>();
            visual.engineLamp = map["EngineLamp"].GetComponent<Renderer>();
            visual.radioDisplay = Fonts.WorldText(map["RadioScreen"], new Vector3(0f, 0f, -0.002f), "", Shapes.Hex("#7dffa8"), 0.0034f);

            visual.mirrorLeft = map["MirrorGlassL"];
            visual.mirrorRight = map["MirrorGlassR"];
            visual.mirrorRear = map["RearMirrorGlass"];

            visual.driverHead = map["DriverHead"];
            visual.driverTorso = map["DriverTorso"];
            AddArms(visual, Rx7Model.DriverEyes, 0.93f, -0.86f, 0.172f);
            visual.driverEyes = Shapes.Group("DriverEyes", root, Rx7Model.DriverEyes);
            visual.driverDoorLocal = new Vector3(-1.45f, 0f, -0.7f);
            visual.fuelCapLocal = new Vector3(0.92f, 0.8f, -1.55f);

            visual.headlights.Add(map["HeadlightL"]);
            visual.headlights.Add(map["HeadlightR"]);
            visual.taillights.Add(map["TaillightL"]);
            visual.taillights.Add(map["TaillightR"]);
            visual.bumperFront = map["BumperF"];
            visual.bumperRear = map["BumperR"];
            visual.frontPanel = map["Hood"];
            visual.rearPanel = map["Trunk"];
            visual.doors.Add(map["DoorL"]);
            visual.doors.Add(map["DoorR"]);
            visual.roof = map["Roof"];
            visual.leftBlinkers.Add(map["BlinkFL"].GetComponent<Renderer>());
            visual.leftBlinkers.Add(map["BlinkRL"].GetComponent<Renderer>());
            visual.rightBlinkers.Add(map["BlinkFR"].GetComponent<Renderer>());
            visual.rightBlinkers.Add(map["BlinkRR"].GetComponent<Renderer>());
            visual.blinkOffMat = CarMaterials.Get("amber", paint);

            PlateText(map["PlateFront"], "В 777 ОР");
            PlateText(map["PlateRear"], "В 777 ОР");
            return visual;
        }

        /// <summary>«Хонда S2000» Суки: родстер без крыши, руль слева, цифровой щиток.</summary>
        public static CarVisual BuildS2000(string name, Color paint)
        {
            var root = new GameObject(name).transform;
            var visual = root.gameObject.AddComponent<CarVisual>();
            var map = ModelSpawner.Spawn(S2000Model.Get().root, root, key => CarMaterials.Get(key, paint, 0.85f));

            visual.body = map["Body"];
            visual.length = 4.14f;
            visual.width = 1.75f;
            visual.height = 1.13f;
            visual.hoodTop = 0.78f;
            visual.rightHandDrive = false;
            visual.maxSpeed = 56f;        // ~200 км/ч, мотор крутится до 9000
            visual.accel = 7.2f;
            visual.speedoMaxKmh = 240f;
            visual.wheelRadius = S2000Model.WheelR;
            visual.sporty = true;

            foreach (var tag in new[] { "FL", "FR", "RL", "RR" })
                visual.wheels.Add(map["Wheel" + tag]);
            visual.frontSteer.Add(map["SteerFL"]);
            visual.frontSteer.Add(map["SteerFR"]);

            visual.steeringWheel = map["SteeringWheel"];
            visual.steeringTilt = S2000Model.SteeringTilt;
            var white = Color.white;
            visual.digitalSpeed = Fonts.WorldText(map["ClusterScreen"], new Vector3(0f, -0.02f, -0.004f), "", Shapes.Hex("#ffd27a"), 0.0062f);
            Fonts.WorldText(map["ClusterScreen"], new Vector3(0f, -0.052f, -0.004f), "км/ч", Shapes.Hex("#c08a3a"), 0.0022f);
            Fonts.WorldText(map["ClusterScreen"], new Vector3(0.155f, 0.05f, -0.004f), "F", Shapes.Hex("#c08a3a"), 0.0022f);
            Fonts.WorldText(map["ClusterScreen"], new Vector3(0.155f, -0.062f, -0.004f), "E", Shapes.Hex("#c08a3a"), 0.0022f);
            visual.tachLeds = new Renderer[30];
            for (int k = 0; k < 30; k++) visual.tachLeds[k] = map["Led" + k].GetComponent<Renderer>();
            visual.fuelLeds = new Renderer[8];
            for (int k = 0; k < 8; k++) visual.fuelLeds[k] = map["FuelLed" + k].GetComponent<Renderer>();
            visual.ledOff = CarMaterials.Get("led_off", paint);
            visual.ledOn = CarMaterials.Get("led_on", paint);
            visual.ledRed = CarMaterials.Get("led_red", paint);
            visual.radioDisplay = Fonts.WorldText(map["RadioScreen"], new Vector3(0f, 0f, -0.003f), "", Shapes.Hex("#9fd0ff"), 0.0034f);

            visual.mirrorLeft = map["MirrorGlassL"];
            visual.mirrorRight = map["MirrorGlassR"];
            visual.mirrorRear = map["RearMirrorGlass"];

            visual.driverHead = map["DriverHead"];
            visual.driverTorso = map["DriverTorso"];
            AddArms(visual, S2000Model.DriverEyes, 0.91f, -0.66f, 0.162f);
            visual.driverEyes = Shapes.Group("DriverEyes", root, S2000Model.DriverEyes);
            visual.driverDoorLocal = new Vector3(-1.4f, 0f, -0.45f);
            visual.fuelCapLocal = new Vector3(0.9f, 0.78f, -1.48f);

            visual.headlights.Add(map["HeadlightL"]);
            visual.headlights.Add(map["HeadlightR"]);
            visual.taillights.Add(map["TaillightL"]);
            visual.taillights.Add(map["TaillightR"]);
            visual.bumperFront = map["BumperF"];
            visual.bumperRear = map["BumperR"];
            visual.frontPanel = map["Hood"];
            visual.rearPanel = map["Trunk"];
            visual.doors.Add(map["DoorL"]);
            visual.doors.Add(map["DoorR"]);
            visual.roof = map["Roof"];
            visual.leftBlinkers.Add(map["BlinkFL"].GetComponent<Renderer>());
            visual.leftBlinkers.Add(map["BlinkRL"].GetComponent<Renderer>());
            visual.rightBlinkers.Add(map["BlinkFR"].GetComponent<Renderer>());
            visual.rightBlinkers.Add(map["BlinkRR"].GetComponent<Renderer>());
            visual.blinkOffMat = CarMaterials.Get("amber", paint);

            PlateText(map["PlateFront"], "С 200 ОО");
            PlateText(map["PlateRear"], "С 200 ОО");
            return visual;
        }

        static void PlateText(Transform plate, string number)
        {
            var black = Shapes.Hex("#111111");
            Fonts.WorldText(plate, new Vector3(-0.035f, 0f, -0.0075f), number, black, 0.0098f);
            Shapes.Box(plate, new Vector3(0.175f, 0f, -0.0065f), new Vector3(0.002f, 0.09f, 0.002f), black, name: "RegionLine");
            Fonts.WorldText(plate, new Vector3(0.215f, 0.012f, -0.0075f), "77", black, 0.0062f);
            Fonts.WorldText(plate, new Vector3(0.215f, -0.03f, -0.0075f), "RUS", black, 0.0026f);
        }

        /// <summary>Руки водителя: кисти держат руль (крутятся с ним), предплечья тянутся от плеч.</summary>
        static void AddArms(CarVisual visual, Vector3 eyes, float shoulderY, float shoulderZ, float grip)
        {
            var jacket = Shapes.Hex("#2a2c33");
            var skin = Shapes.Hex("#e2b08a");
            var x = eyes.x;
            visual.shoulderL = new Vector3(x - 0.19f, shoulderY, shoulderZ);
            visual.shoulderR = new Vector3(x + 0.19f, shoulderY, shoulderZ);
            foreach (float side in new[] { -1f, 1f })
            {
                var hand = Shapes.Group(side < 0 ? "HandL" : "HandR", visual.steeringWheel, new Vector3(grip * side, 0.012f, 0.03f));
                Shapes.Make(PrimitiveType.Sphere, hand, Vector3.zero, new Vector3(0.075f, 0.065f, 0.095f), skin, name: "Fist");
                var arm = Shapes.Box(visual.body, Vector3.zero, new Vector3(0.095f, 0.095f, 1f), jacket, name: side < 0 ? "ArmL" : "ArmR").transform;
                if (side < 0) { visual.leftHand = hand; visual.leftArm = arm; }
                else { visual.rightHand = hand; visual.rightArm = arm; }
            }
        }
    }
}
