using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Стартовое меню поверх города: название, пара слов об игре, «Начать», «Как играть», «Выйти».
    /// Пока меню открыто, игра стоит на паузе.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        /// <summary>Открытое сейчас меню. Привязано к экземпляру: старое меню, которое уничтожается
        /// при перестройке мира, не должно закрывать уже открытое новое.</summary>
        static MainMenu open;
        public static bool IsOpen => open != null;

        CameraRig rig;
        bool showHelp;
        GUIStyle title, text, button;
        float builtForHeight;

        public void Open(CameraRig cameraRig)
        {
            rig = cameraRig;
            open = this;
            GameInput.Paused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            rig.SetCursorLocked(false);
        }

        void Close()
        {
            if (open == this) open = null;
            PauseMenu.ResetGlobalState();
            rig.SetCursorLocked(true);
        }

        void OnDestroy()
        {
            if (open == this)
            {
                open = null;
                PauseMenu.ResetGlobalState();
            }
        }

        void OnGUI()
        {
            if (open != this) return;
            float w = Screen.width, h = Screen.height, k = h / 1080f;
            if (title == null || !Mathf.Approximately(builtForHeight, h))
            {
                builtForHeight = h;
                title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(64 * k), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false };
                title.normal.textColor = Color.white;
                text = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(22 * k), alignment = TextAnchor.UpperLeft, wordWrap = true };
                text.normal.textColor = Color.white;
                button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(30 * k) };
            }

            var old = GUI.color;
            GUI.color = new Color(0.04f, 0.04f, 0.06f, 0.78f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = new Color(0.78f, 0.19f, 0.17f, 1f);
            GUI.DrawTexture(new Rect(w / 2 - 420 * k, h * 0.2f, 840 * k, 6 * k), Texture2D.whiteTexture);
            GUI.color = old;

            GUI.Label(new Rect(0, h * 0.08f, w, 100 * k), "СИМУЛЯТОР ОЧЕРЕДИ НА ЗАПРАВКУ", title);

            float bw = 420 * k, bh = 64 * k, x = w / 2 - bw / 2, y = h * 0.3f;
            if (GUI.Button(new Rect(x, y, bw, bh), "Начать", button)) Close();
            y += bh + 16 * k;
            if (GUI.Button(new Rect(x, y, bw, bh), showHelp ? "Скрыть управление" : "Как играть", button)) showHelp = !showHelp;
            y += bh + 16 * k;
            if (GUI.Button(new Rect(x, y, bw, bh), "Выйти из игры", button)) Quit();
            y += bh + 24 * k;

            if (showHelp)
            {
                var r = new Rect(w / 2 - 470 * k, y, 940 * k, h - y - 30 * k);
                GUI.color = new Color(0, 0, 0, 0.5f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = old;
                GUI.Label(new Rect(r.x + 24 * k, r.y + 16 * k, r.width / 2 - 30 * k, r.height),
                    "<b>В машине</b>\n" +
                    "W / S — газ, тормоз, задний ход\n" +
                    "A / D — руль\n" +
                    "Q / E — поворотники\n" +
                    "I — заглушить / завести мотор\n" +
                    "H или Пробел — бибикнуть\n" +
                    "F — выйти из машины\n" +
                    "C — камера, R — радио", Rich(text));
                GUI.Label(new Rect(r.x + r.width / 2 + 10 * k, r.y + 16 * k, r.width / 2 - 30 * k, r.height),
                    "<b>Пешком</b>\n" +
                    "WASD — идти, Shift — бежать\n" +
                    "Пробел — прыжок, у машины — залезть\n" +
                    "ЛКМ — ударить\n" +
                    "E — касса, пистолет, поговорить\n" +
                    "F — сесть в машину\n" +
                    "Esc — пауза, Tab — подсказки", Rich(text));
            }
        }

        static GUIStyle Rich(GUIStyle s)
        {
            s.richText = true;
            return s;
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
