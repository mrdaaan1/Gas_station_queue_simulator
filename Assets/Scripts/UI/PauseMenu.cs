using UnityEngine;

namespace GasQueue
{
    /// <summary>Пауза на Esc: продолжить, настройки, начать заново, выйти.</summary>
    public class PauseMenu : MonoBehaviour
    {
        const string VolumeKey = "GasQueue.Volume";

        public bool IsOpen { get; private set; }

        GameSettings settings;
        CameraRig rig;
        System.Action restart, toMenu;
        GUIStyle title, label, button;
        float builtForHeight;

        public void Init(GameSettings settings, CameraRig rig, System.Action restart, System.Action toMenu)
        {
            this.toMenu = toMenu;
            this.settings = settings;
            this.rig = rig;
            this.restart = restart;
            AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            SetOpen(false);
        }

        void Update()
        {
            if (MainMenu.IsOpen) return;
            if (GameManager.Instance == null || GameManager.Instance.State == GameState.Finished) return;
            if (GameInput.PausePressed) SetOpen(!IsOpen);
        }

        void SetOpen(bool open)
        {
            IsOpen = open;
            GameInput.Paused = open;
            Time.timeScale = open ? 0f : 1f;
            AudioListener.pause = open;
            rig.SetCursorLocked(!open);
            if (!open) PlayerPrefs.Save();
        }

        /// <summary>Сбросить паузу (при перезапуске и выходе из Play).</summary>
        public static void ResetGlobalState()
        {
            GameInput.Paused = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        void OnDestroy()
        {
            if (IsOpen) ResetGlobalState();
        }

        void OnGUI()
        {
            if (!IsOpen) return;
            float h = Screen.height, w = Screen.width, k = h / 1080f;
            if (title == null || !Mathf.Approximately(builtForHeight, h))
            {
                builtForHeight = h;
                title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(56 * k), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                title.normal.textColor = Color.white;
                label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(24 * k) };
                label.normal.textColor = Color.white;
                button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(28 * k) };
            }

            var old = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = old;

            float pw = 560 * k, x = w / 2 - pw / 2, y = h * 0.14f;
            GUI.Label(new Rect(0, y, w, 80 * k), "ПАУЗА", title);
            y += 110 * k;

            float bh = 64 * k, gap = 14 * k;
            if (GUI.Button(new Rect(x, y, pw, bh), "Продолжить", button)) SetOpen(false);
            y += bh + gap;
            if (GUI.Button(new Rect(x, y, pw, bh), "Начать заново", button))
            {
                SetOpen(false);
                restart();
                return;
            }
            y += bh + gap;
            if (GUI.Button(new Rect(x, y, pw, bh), "В главное меню", button))
            {
                SetOpen(false);
                toMenu();
                return;
            }
            y += bh + gap * 3;

            // Настройки
            GUI.Label(new Rect(x, y, pw, 34 * k), $"Чувствительность мыши: {CameraRig.Sensitivity:0.0}", label);
            y += 36 * k;
            float sens = GUI.HorizontalSlider(new Rect(x, y, pw, 24 * k), CameraRig.Sensitivity, 0.3f, 6f);
            if (!Mathf.Approximately(sens, CameraRig.Sensitivity))
            {
                CameraRig.Sensitivity = sens;
                rig.ReloadSensitivity();
            }
            y += 40 * k;

            GUI.Label(new Rect(x, y, pw, 34 * k), $"Громкость: {Mathf.RoundToInt(AudioListener.volume * 100)}%", label);
            y += 36 * k;
            float vol = GUI.HorizontalSlider(new Rect(x, y, pw, 24 * k), AudioListener.volume, 0f, 1f);
            if (!Mathf.Approximately(vol, AudioListener.volume))
            {
                AudioListener.volume = vol;
                PlayerPrefs.SetFloat(VolumeKey, vol);
            }
            y += 40 * k;

            string speedText = settings.fastTestMode ? $"ускорено в {settings.testSpeedup:0.0} раза" : "реальное (~20 минут)";
            GUI.Label(new Rect(x, y, pw, 34 * k), $"Скорость игры: {speedText}", label);
            y += 36 * k;
            settings.fastTestMode = GUI.Toggle(new Rect(x, y, pw, 30 * k), settings.fastTestMode, " Ускорение для тестов", label);
            y += 36 * k;
            if (settings.fastTestMode)
            {
                settings.testSpeedup = GUI.HorizontalSlider(new Rect(x, y, pw, 24 * k), settings.testSpeedup, 1f, 6f);
                y += 40 * k;
            }

            y += gap * 2;
            if (GUI.Button(new Rect(x, y, pw, bh), "Выйти из игры", button))
                MainMenu.Quit();
        }
    }
}
