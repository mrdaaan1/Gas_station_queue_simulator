using UnityEngine;

namespace GasQueue
{
    public enum CameraMode { Cabin, Chase, Top, Rear }

    /// <summary>
    /// Камера. В машине: из салона (основной), сзади, сверху (виден масштаб очереди), назад (виден хвост).
    /// Пешком: из-за плеча или от первого лица. C — переключить вид.
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
        public bool FootFirstPerson { get; private set; }
        public bool OnFoot => onFoot;
        public bool CursorLocked => Cursor.lockState == CursorLockMode.Locked;

        /// <summary>Куда смотрит камера по горизонтали, когда игрок пешком (для ходьбы относительно камеры).</summary>
        public float FootYaw => footYaw;

        PlayerCar player;
        WalkerController walker;
        Camera cam;
        float yaw, pitch;
        float footYaw, footPitch = 10f;
        float sensitivity;
        Vector3 chasePos;
        bool onFoot;

        public void Init(PlayerCar player)
        {
            this.player = player;
            cam = gameObject.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 900f;
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

        public void SetWalker(WalkerController w) => walker = w;
        public void ReloadSensitivity() => sensitivity = Sensitivity;

        /// <summary>Переключение между «в машине» и «пешком».</summary>
        public void SetOnFoot(bool value)
        {
            onFoot = value;
            if (value)
            {
                footYaw = player.transform.eulerAngles.y - 90f; // выходим из левой двери — смотрим от машины
                footPitch = 10f;
            }
            yaw = pitch = 0f;
            chasePos = transform.position;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (GameInput.SwitchCameraPressed)
            {
                if (onFoot) FootFirstPerson = !FootFirstPerson;
                else
                {
                    Mode = (CameraMode)(((int)Mode + 1) % 4);
                    yaw = pitch = 0f;
                    chasePos = transform.position;
                }
            }

            // Курсор захватывается обратно кликом (например, после того как его отпустил редактор Unity)
            if (GameInput.ClickPressed && gm != null && gm.State != GameState.Finished) SetCursorLocked(true);
            if (!CursorLocked || GameInput.Paused) return;

            var d = GameInput.MouseDelta * sensitivity;
            if (onFoot)
            {
                footYaw = Mathf.Repeat(footYaw + d.x + 180f, 360f) - 180f;
                footPitch = Mathf.Clamp(footPitch - d.y, -60f, 70f);
            }
            else
            {
                if (Mode == CameraMode.Chase || Mode == CameraMode.Top) yaw = Mathf.Repeat(yaw + d.x + 180f, 360f) - 180f;
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
            // Водитель за рулём: в салоне видны только руки (голова — это мы), снаружи — весь; вышел — пусто
            bool inside = !onFoot && Mode == CameraMode.Cabin;
            player.visual.SetDriverVisible(!onFoot && !inside, !onFoot);
            if (onFoot && walker != null)
            {
                FollowWalker();
                return;
            }

            var car = player.transform;
            float dt = Time.unscaledDeltaTime;
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
                case CameraMode.Chase:
                case CameraMode.Top:
                {
                    bool top = Mode == CameraMode.Top;
                    var offset = top ? new Vector3(0f, 16f, -22f) : new Vector3(0f, 3.4f, -8.5f);
                    float carYaw = car.eulerAngles.y;
                    var want = car.position + Quaternion.Euler(0f, carYaw + yaw, 0f) * offset;
                    chasePos = Vector3.Lerp(chasePos == Vector3.zero ? want : chasePos, want, 1f - Mathf.Exp(-dt * 6f));
                    transform.position = chasePos;
                    var lookAt = car.position + car.forward * (top ? 14f : 4f) + Vector3.up * (1.2f - pitch * 0.05f);
                    transform.rotation = Quaternion.LookRotation(lookAt - transform.position);
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

        void FollowWalker()
        {
            var w = walker.transform;
            var rot = Quaternion.Euler(footPitch, footYaw, 0f);
            if (FootFirstPerson)
            {
                transform.position = w.position + Vector3.up * 1.68f + w.forward * 0.12f;
                transform.rotation = rot;
                return;
            }
            var pivot = w.position + Vector3.up * 1.6f;
            var back = rot * new Vector3(0.45f, 0.1f, -3.2f);
            transform.position = pivot + back;
            transform.rotation = rot;
        }

        public string ModeName
        {
            get
            {
                if (onFoot) return FootFirstPerson ? "От первого лица" : "Из-за плеча";
                switch (Mode)
                {
                    case CameraMode.Cabin: return "Из салона";
                    case CameraMode.Chase: return "Сзади";
                    case CameraMode.Top: return "Сверху";
                    default: return "Назад";
                }
            }
        }
    }
}
