using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>Машины, которые может выбрать игрок.</summary>
    public enum PlayerCarKind { Vaz2107, Supra, Gelik }

    /// <summary>
    /// Спорткары из гладких сеток (<see cref="SupraModel"/> и следующие): собирает объекты Unity
    /// и заполняет <see cref="CarVisual"/> — колёса, руль, стрелки, зеркала, фары, детали для поломок.
    /// </summary>
    public static class SportsCars
    {
        public static string Title(PlayerCarKind kind) =>
            kind == PlayerCarKind.Supra ? "Тоёта Супра (1997)" : kind == PlayerCarKind.Gelik ? "Гелик (2025)" : "ВАЗ-2107";

        /// <summary>Синий матовый «Гелик» как на фото.</summary>
        public static readonly Color GelikBlue = Shapes.Hex("#2b44b0");

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
