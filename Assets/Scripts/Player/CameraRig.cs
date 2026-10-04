using UnityEngine;

namespace GasQueue
{
    public enum CameraMode { Cabin, ThirdPerson, Rear }

    /// <summary>
    /// Камера: вид из салона (основной), сверху-сзади (виден масштаб очереди) и назад (виден хвост).
    /// C — переключить вид. Клик — осмотреться мышью, Esc — вернуть курсор.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public CameraMode Mode { get; private set; } = CameraMode.Cabin;
        public bool CursorLocked => Cursor.lockState == CursorLockMode.Locked;

        PlayerCar player;
        Camera cam;
        float yaw, pitch;

        public void Init(PlayerCar player)
        {
            this.player = player;
            cam = gameObject.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 700f;
            cam.fieldOfView = 70f;
            if (RenderSettings.skybox != null) cam.clearFlags = CameraClearFlags.Skybox;
            else
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = RenderSettings.fogColor;
            }
            gameObject.AddComponent<AudioListener>();
            gameObject.tag = "MainCamera";
        }

        void Update()
        {
            if (GameInput.SwitchCameraPressed)
            {
                Mode = (CameraMode)(((int)Mode + 1) % 3);
                yaw = pitch = 0f;
            }

            if (GameInput.EscapePressed) SetCursorLocked(false);
            else if (GameInput.ClickPressed && GameManager.Instance.State != GameState.Finished) SetCursorLocked(true);

            if (CursorLocked)
            {
                var d = GameInput.MouseDelta * 2f;
                yaw = Mathf.Clamp(yaw + d.x, -120f, 120f);
                pitch = Mathf.Clamp(pitch - d.y, -50f, 60f);
            }
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void LateUpdate()
        {
            if (player == null) return;
            var car = player.transform;
            switch (Mode)
            {
                case CameraMode.Cabin:
                {
                    var eyes = player.visual.driverEyes;
                    transform.position = eyes.position;
                    transform.rotation = car.rotation * Quaternion.Euler(pitch + 6f, yaw, 0f);
                    break;
                }
                case CameraMode.ThirdPerson:
                {
                    var offset = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 9f, -15f);
                    transform.position = car.position + car.rotation * offset;
                    transform.LookAt(car.position + car.forward * 12f + Vector3.up * (1f - pitch * 0.1f));
                    break;
                }
                case CameraMode.Rear:
                {
                    transform.position = car.TransformPoint(new Vector3(0f, 1.9f, -1.6f));
                    transform.rotation = car.rotation * Quaternion.Euler(8f + pitch * 0.5f, 180f + yaw * 0.5f, 0f);
                    break;
                }
            }
        }

        public string ModeName => Mode == CameraMode.Cabin ? "Из салона" : Mode == CameraMode.ThirdPerson ? "Сверху" : "Назад";
    }
}
