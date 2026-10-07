using UnityEngine;

namespace GasQueue
{
    /// <summary>Режим игры из главного меню.</summary>
    public enum GameMode
    {
        Queue, // основная игра: стоять в очереди на заправку
        Race,  // «Самая быстрая гонка»: трасса, а потом та же очередь
        Tolyatti, // уличная гонка по Тольятти: Офицерская — 70 лет Октября — Льва Яшина
    }

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
        const string ModeKey = "GasQueue.Mode";

        /// <summary>Режим, выбранный в меню (запоминается между запусками).</summary>
        public static GameMode Mode
        {
            get => (GameMode)Mathf.Clamp(PlayerPrefs.GetInt(ModeKey, 0), 0, System.Enum.GetValues(typeof(GameMode)).Length - 1);
            set => PlayerPrefs.SetInt(ModeKey, (int)value);
        }

        /// <summary>Машина игрока, выбранная в меню (запоминается между запусками).</summary>
        public static PlayerCarKind CarChoice
        {
            get => (PlayerCarKind)Mathf.Clamp(PlayerPrefs.GetInt(CarKey, 0), 0, System.Enum.GetValues(typeof(PlayerCarKind)).Length - 1);
            set => PlayerPrefs.SetInt(CarKey, (int)value);
        }

        void Start()
        {
            showMenu = true;
            SafeBuild();
            IntroSplash.PlayOnce(gameObject); // заставка «92» поверх меню, один раз за запуск
        }

        /// <summary>
        /// Построить мир; если гоночная трасса упала с ошибкой — не оставлять чёрный экран,
        /// а показать ошибку в Console и открыть меню в обычном режиме.
        /// </summary>
        void SafeBuild()
        {
            try
            {
                Build();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                if (Mode == GameMode.Queue) throw;
                Debug.LogError("Трасса режима «" + Mode + "» не построилась — открываю обычный режим. Пришлите ошибку выше.");
                if (world != null) Destroy(world);
                Mode = GameMode.Queue;
                showMenu = true;
                Build();
            }
        }

        void Build()
        {
            var settings = GetComponent<GameSettings>();
            PauseMenu.ResetGlobalState();
            CashierLine.Reset();
            DisableForeignCameras();

            world = new GameObject("World (создаётся при запуске)");
            var root = world.transform;

            bool race = Mode != GameMode.Queue;
            // Город с заправкой строится всегда (на нём держатся правила игры); Тольятти — далеко в стороне
            var city = CityBuilder.Build(root);
            var raceTrack = Mode == GameMode.Race ? RaceTrack.Classic() : Mode == GameMode.Tolyatti ? TolyattiLayout.Build() : null;
            RaceTrack.Current = raceTrack;
            var track = Mode == GameMode.Race ? RaceTrackBuilder.Build(root) : Mode == GameMode.Tolyatti ? TolyattiBuilder.Build(root, raceTrack) : null;
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
            if (race) player.SetFuelLiters(settings.raceStartFuelLiters);

            var traffic = new GameObject("Traffic").AddComponent<TrafficManager>();
            traffic.transform.SetParent(root, false);
            player.traffic = traffic;
            TrafficManager.RacerVisual = race ? RacerVisual : null;
            traffic.Init(settings, player, city.barrier, debris, raceTrack);
            // Шашлык у дороги вдоль очереди (в Тольятти очереди нет)
            if (Mode != GameMode.Tolyatti) ShashlikStand.Build(root, traffic);

            var rig = new GameObject("Camera").AddComponent<CameraRig>();
            rig.transform.SetParent(root, false);
            rig.Init(player);
            if (race) rig.ShowcaseSide = 1f;
            playerVisual.gameObject.AddComponent<CarMirrors>().Init(playerVisual, rig);

            var walker = WalkerController.Create(root, traffic, rig);
            rig.SetWalker(walker);

            var systems = new GameObject("Systems");
            systems.transform.SetParent(root, false);
            Radio.RaceOnly = race;
            var radio = systems.AddComponent<Radio>();
            radio.display = playerVisual.radioDisplay;
            var pause = systems.AddComponent<PauseMenu>();
            pause.Init(settings, rig, Restart, ToMenu);
            systems.AddComponent<Hud>().Init(pause, Restart, ToMenu);
            var gm = systems.AddComponent<GameManager>();
            gm.Init(settings, traffic, player, walker, rig, radio, city.barrier, city.priceBoard, city.cashier, Restart, race);
            // Вечер, ночь и погода — только в основной игре (в гонках всегда ясный день)
            if (Mode == GameMode.Queue) systems.AddComponent<DayNight>().Init(player, root);
            if (race) systems.AddComponent<RaceManager>().Init(traffic, player, gm, track, raceTrack);

            var menu = systems.AddComponent<MainMenu>();
            if (showMenu) menu.Open(rig, ChangeCar, ChangeMode);
            showMenu = false;
        }

        static readonly Color[] RacerPaints =
        {
            Shapes.Hex("#f2c81a"), Shapes.Hex("#1f6fd6"), Shapes.Hex("#e8e8e8"), Shapes.Hex("#2e9e4f"),
            Shapes.Hex("#ff6a13"), Shapes.Hex("#7b2fbf"), Shapes.Hex("#d31f2a"), Shapes.Hex("#202226"),
        };

        /// <summary>Соперники — те же спорткары разных цветов, по кругу.</summary>
        static CarVisual RacerVisual(int index)
        {
            var paint = RacerPaints[index % RacerPaints.Length];
            CarVisual v;
            switch (index % 5)
            {
                case 0: v = SportsCars.BuildSupra("Racer", paint); break;
                case 1: v = SportsCars.BuildSkyline("Racer", SportsCars.SkylineSilver); break;
                case 2: v = SportsCars.BuildRx7("Racer", paint); break;
                case 3: v = SportsCars.BuildS2000("Racer", SportsCars.S2000Pink); break;
                default: v = SportsCars.BuildGelik("Racer", paint); break;
            }
            v.UpdateArms(); // руки водителя на руле (у машины игрока это делается каждый кадр)
            return v;
        }

        /// <summary>Переключили режим в меню — перестраиваем мир (трасса есть только в гонке).</summary>
        void ChangeMode(GameMode mode)
        {
            Mode = mode;
            showMenu = true;
            Restart();
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
            SafeBuild();
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
