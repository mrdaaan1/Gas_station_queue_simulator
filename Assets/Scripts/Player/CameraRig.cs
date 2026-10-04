using UnityEngine;

namespace GasQueue
{
    public enum CameraMode { Cabin, ThirdPerson, Rear }

    /// <summary>
    /// Камера: вид из салона (основной), сверху-сзади (виден масштаб очереди) и назад (виден хвост).
    /// C — переключить вид. Мышью можно обернуться почти на 180°, чтобы посмотреть в заднее стекло.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        const string SensitivityKey = "GasQueue.MouseSensitivity";

        /// <summary>Чувствительность мыши, меняется в меню паузы и запоминается.</summary>
        public static float Sensitivity
        {
            get => PlayerPrefs.GetFloat(SensitivityKey, 2f);
            set => PlayerPrefs.SetFloat(SensitivityKey, value);
        }

        public CameraMode Mode { get; private set; } = CameraMode.Cabin;
        public bool CursorLocked => Cursor.lockState == CursorLockMode.Locked;

        PlayerCar player;
        Camera cam;
        float yaw, pitch;
        float sensitivity;

        public void Init(PlayerCar player)
        {
            this.player = player;
            cam = gameObject.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 700f;
            cam.fieldOfView = 72f;
            if (RenderSettings.skybox != null) cam.clearFlags = CameraClearFlags.Skybox;
            else
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = RenderSettings.fogColor;
            }
            gameObject.AddComponent<AudioListener>();
            gameObject.tag = "MainCamera";
            sensitivity = Sensitivity;
            SetCursorLocked(true);
        }

        public void ReloadSensitivity() => sensitivity = Sensitivity;

        void Update()
        {
            if (GameInput.SwitchCameraPressed)
            {
                Mode = (CameraMode)(((int)Mode + 1) % 3);
                yaw = pitch = 0f;
            }

            // Курсор захватывается обратно кликом (например, после того как его отпустил редактор Unity)
            if (GameInput.ClickPressed && GameManager.Instance.State != GameState.Finished) SetCursorLocked(true);

            if (CursorLocked && !GameInput.Paused)
            {
                var d = GameInput.MouseDelta * sensitivity;
                if (Mode == CameraMode.ThirdPerson) yaw = Mathf.Repeat(yaw + d.x + 180f, 360f) - 180f;
                else yaw = Mathf.Clamp(yaw + d.x, -175f, 175f);
                pitch = Mathf.Clamp(pitch - d.y, -55f, 65f);
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
                    // Оборачиваясь назад, водитель немного наклоняется к центру салона
                    float turn = Mathf.Clamp01((Mathf.Abs(yaw) - 60f) / 100f);
                    var lean = new Vector3(turn * 0.2f, 0f, -turn * 0.05f);
                    transform.position = player.visual.driverEyes.TransformPoint(lean);
                    transform.rotation = car.rotation * Quaternion.Euler(pitch + 4f, yaw, 0f);
                    break;
                }
                case CameraMode.ThirdPerson:
                {
                    var offset = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 9f, -15f);
                    transform.position = car.position + car.rotation * offset;
                    transform.LookAt(car.position + Vector3.up * (1f - pitch * 0.1f));
                    transform.rotation *= Quaternion.Euler(-12f, 0f, 0f);
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
