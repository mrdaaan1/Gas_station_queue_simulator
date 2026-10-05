using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>Машины, которые может выбрать игрок.</summary>
    public enum PlayerCarKind { Vaz2107, Supra }

    /// <summary>
    /// Спорткары из гладких сеток (<see cref="SupraModel"/> и следующие): собирает объекты Unity
    /// и заполняет <see cref="CarVisual"/> — колёса, руль, стрелки, зеркала, фары, детали для поломок.
    /// </summary>
    public static class SportsCars
    {
        public static string Title(PlayerCarKind kind) => kind == PlayerCarKind.Supra ? "Тоёта Супра (1997)" : "ВАЗ-2107";

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
            AddArms(visual);
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

        static void PlateText(Transform plate, string number)
        {
            var black = Shapes.Hex("#111111");
            Fonts.WorldText(plate, new Vector3(-0.035f, 0f, -0.0075f), number, black, 0.0098f);
            Shapes.Box(plate, new Vector3(0.175f, 0f, -0.0065f), new Vector3(0.002f, 0.09f, 0.002f), black, name: "RegionLine");
            Fonts.WorldText(plate, new Vector3(0.215f, 0.012f, -0.0075f), "77", black, 0.0062f);
            Fonts.WorldText(plate, new Vector3(0.215f, -0.03f, -0.0075f), "RUS", black, 0.0026f);
        }

        /// <summary>Руки водителя: кисти держат руль (крутятся с ним), предплечья тянутся от плеч.</summary>
        static void AddArms(CarVisual visual)
        {
            var jacket = Shapes.Hex("#2a2c33");
            var skin = Shapes.Hex("#e2b08a");
            var x = SupraModel.DriverEyes.x;
            visual.shoulderL = new Vector3(x - 0.19f, 0.98f, -0.92f);
            visual.shoulderR = new Vector3(x + 0.19f, 0.98f, -0.92f);
            foreach (float side in new[] { -1f, 1f })
            {
                var hand = Shapes.Group(side < 0 ? "HandL" : "HandR", visual.steeringWheel, new Vector3(0.172f * side, 0.012f, 0.03f));
                Shapes.Make(PrimitiveType.Sphere, hand, Vector3.zero, new Vector3(0.075f, 0.065f, 0.095f), skin, name: "Fist");
                var arm = Shapes.Box(visual.body, Vector3.zero, new Vector3(0.095f, 0.095f, 1f), jacket, name: side < 0 ? "ArmL" : "ArmR").transform;
                if (side < 0) { visual.leftHand = hand; visual.leftArm = arm; }
                else { visual.rightHand = hand; visual.rightArm = arm; }
            }
        }
    }
}
