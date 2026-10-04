using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Точка входа. Висит на объекте "Game" в сцене Prototype и при запуске строит весь мир кодом:
    /// дорогу, заправку, очередь, машину игрока, камеру и интерфейс.
    /// Поэтому в сцене почти ничего нет — всё создаётся при нажатии Play.
    /// </summary>
    [RequireComponent(typeof(GameSettings))]
    public class GameBootstrap : MonoBehaviour
    {
        GameObject world;

        void Start() => Build();

        void Build()
        {
            var settings = GetComponent<GameSettings>();
            PauseMenu.ResetGlobalState();
            DisableForeignCameras();

            world = new GameObject("World (создаётся при запуске)");
            var root = world.transform;

            var station = WorldBuilder.Build(root, settings.carSpacing);

            var playerVisual = CarFactory.Build("PlayerCar", Shapes.Hex("#d8c25a"), CarShape.Sedan, true);
            playerVisual.transform.SetParent(root, false);
            var player = playerVisual.gameObject.AddComponent<PlayerCar>();
            player.Init(playerVisual, settings);

            var queue = new GameObject("Queue").AddComponent<QueueManager>();
            queue.transform.SetParent(root, false);
            queue.Init(settings, station.barrier, station.barrierZ, player);

            var rig = new GameObject("Camera").AddComponent<CameraRig>();
            rig.transform.SetParent(root, false);
            rig.Init(player);

            var systems = new GameObject("Systems");
            systems.transform.SetParent(root, false);
            var radio = systems.AddComponent<Radio>();
            radio.display = playerVisual.radioDisplay;
            var pause = systems.AddComponent<PauseMenu>();
            pause.Init(settings, rig, Restart);
            systems.AddComponent<Hud>().Init(pause);
            var gm = systems.AddComponent<GameManager>();
            gm.Init(settings, queue, player, rig, radio, station.barrier, station.priceBoard, Restart);
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
