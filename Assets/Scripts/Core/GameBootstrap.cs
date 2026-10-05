using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Точка входа. Висит на объекте "Game" в сцене Prototype и при запуске строит весь мир кодом:
    /// город, заправку, очередь и поток машин, машину игрока, человечка, камеру и интерфейс.
    /// Поэтому в сцене почти ничего нет — всё создаётся при нажатии Play.
    /// </summary>
    [RequireComponent(typeof(GameSettings))]
    public class GameBootstrap : MonoBehaviour
    {
        GameObject world;

        /// <summary>Показать стартовое меню после постройки мира (при запуске и «В главное меню»).</summary>
        static bool showMenu = true;

        const string CarKey = "GasQueue.PlayerCar";

        /// <summary>Машина игрока, выбранная в меню (запоминается между запусками).</summary>
        public static PlayerCarKind CarChoice
        {
            get => (PlayerCarKind)Mathf.Clamp(PlayerPrefs.GetInt(CarKey, 0), 0, System.Enum.GetValues(typeof(PlayerCarKind)).Length - 1);
            set => PlayerPrefs.SetInt(CarKey, (int)value);
        }

        void Start()
        {
            showMenu = true;
            Build();
        }

        void Build()
        {
            var settings = GetComponent<GameSettings>();
            PauseMenu.ResetGlobalState();
            CashierLine.Reset();
            DisableForeignCameras();

            world = new GameObject("World (создаётся при запуске)");
            var root = world.transform;

            var city = CityBuilder.Build(root);
            var debris = Shapes.Group("Debris", root);

            var playerVisual = CarChoice == PlayerCarKind.Supra ? SportsCars.BuildSupra("PlayerCar", SportsCars.SupraRed)
                : CarChoice == PlayerCarKind.Gelik ? SportsCars.BuildGelik("PlayerCar", SportsCars.GelikBlue)
                : CarChoice == PlayerCarKind.Skyline ? SportsCars.BuildSkyline("PlayerCar", SportsCars.SkylineSilver)
                : CarChoice == PlayerCarKind.Rx7 ? SportsCars.BuildRx7("PlayerCar", SportsCars.Rx7Red)
                : CarChoice == PlayerCarKind.S2000 ? SportsCars.BuildS2000("PlayerCar", SportsCars.S2000Pink)
                : CarFactory.Build("PlayerCar", Shapes.Hex("#e3dccb"), CarModel.Vaz2107, true);
            playerVisual.transform.SetParent(root, false);
            var player = playerVisual.gameObject.AddComponent<PlayerCar>();
            player.Init(playerVisual, settings, debris);

            var traffic = new GameObject("Traffic").AddComponent<TrafficManager>();
            traffic.transform.SetParent(root, false);
            player.traffic = traffic;
            traffic.Init(settings, player, city.barrier, debris);

            var rig = new GameObject("Camera").AddComponent<CameraRig>();
            rig.transform.SetParent(root, false);
            rig.Init(player);
            playerVisual.gameObject.AddComponent<CarMirrors>().Init(playerVisual, rig);
            if (playerVisual.sporty) playerVisual.gameObject.AddComponent<CabinDebug>();

            var walker = WalkerController.Create(root, traffic, rig);
            rig.SetWalker(walker);

            var systems = new GameObject("Systems");
            systems.transform.SetParent(root, false);
            var radio = systems.AddComponent<Radio>();
            radio.display = playerVisual.radioDisplay;
            var pause = systems.AddComponent<PauseMenu>();
            pause.Init(settings, rig, Restart, ToMenu);
            systems.AddComponent<Hud>().Init(pause, Restart, ToMenu);
            var gm = systems.AddComponent<GameManager>();
            gm.Init(settings, traffic, player, walker, rig, radio, city.barrier, city.priceBoard, city.cashier, Restart);

            var menu = systems.AddComponent<MainMenu>();
            if (showMenu) menu.Open(rig, ChangeCar);
            showMenu = false;
        }

        /// <summary>Выбрали другую машину в меню — перестраиваем мир с ней, меню остаётся открытым.</summary>
        void ChangeCar(PlayerCarKind kind)
        {
            CarChoice = kind;
            showMenu = true;
            Restart();
        }

        void ToMenu()
        {
            showMenu = true;
            Restart();
        }

        void Restart()
        {
            PauseMenu.ResetGlobalState();
            Destroy(world);
            Build();
        }

        /// <summary>Если сцену запустили не нашу (например, SampleScene), выключаем её камеру, чтобы не мешала.</summary>
        static void DisableForeignCameras()
        {
            foreach (var cam in Camera.allCameras)
            {
                if (cam.GetComponent<CameraRig>() != null) continue;
                cam.gameObject.SetActive(false);
            }
        }
    }
}
