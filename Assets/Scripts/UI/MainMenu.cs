using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Стартовое меню: слева название и кнопки («Начать», выбор машины, «Как играть», «Выйти»),
    /// справа — витрина: камера медленно облетает выбранную машину. Пока меню открыто, игра стоит на паузе.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        /// <summary>Открытое сейчас меню. Привязано к экземпляру: старое меню, которое уничтожается
        /// при перестройке мира, не должно закрывать уже открытое новое.</summary>
        static MainMenu open;
        public static bool IsOpen => open != null;

        CameraRig rig;
        System.Action<PlayerCarKind> changeCar;
        bool showHelp;
        GUIStyle title, text, button, carStyle;
        float builtForHeight;

        public void Open(CameraRig cameraRig, System.Action<PlayerCarKind> onCarChange = null)
        {
            rig = cameraRig;
            changeCar = onCarChange;
            rig.Showcase = true;
            open = this;
            GameInput.Paused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            rig.SetCursorLocked(false);
        }

        void Close()
        {
            if (open == this) open = null;
            rig.Showcase = false;
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
                title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(54 * k), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false };
                title.normal.textColor = Color.white;
                text = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(22 * k), alignment = TextAnchor.UpperLeft, wordWrap = true };
                text.normal.textColor = Color.white;
                button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(30 * k) };
                carStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(28 * k), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                carStyle.normal.textColor = new Color(1f, 0.85f, 0.4f);
            }

            var old = GUI.color;
            // Слева тёмная панель с кнопками, справа видна машина
            float panel = Mathf.Max(560f * k, w * 0.38f);
            GUI.color = new Color(0.04f, 0.04f, 0.06f, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, panel, h), Texture2D.whiteTexture);
            GUI.color = new Color(0.04f, 0.04f, 0.06f, 0.18f);
            GUI.DrawTexture(new Rect(panel, 0, w - panel, h), Texture2D.whiteTexture);
            GUI.color = new Color(0.78f, 0.19f, 0.17f, 1f);
            GUI.DrawTexture(new Rect(40 * k, h * 0.26f, panel - 80 * k, 6 * k), Texture2D.whiteTexture);
            GUI.color = old;

            title.wordWrap = true;
            title.alignment = TextAnchor.LowerLeft;
            GUI.Label(new Rect(40 * k, h * 0.04f, panel - 80 * k, h * 0.21f), "СИМУЛЯТОР ОЧЕРЕДИ НА ЗАПРАВКУ", title);

            float bw = panel - 80 * k, bh = 64 * k, x = 40 * k, y = h * 0.3f;
            if (GUI.Button(new Rect(x, y, bw, bh), "Начать", button)) Close();
            y += bh + 16 * k;

            // Выбор машины: ◀ название ▶
            var kind = GameBootstrap.CarChoice;
            float arrow = bh;
            if (GUI.Button(new Rect(x, y, arrow, bh), "◀", button)) Switch(kind, -1);
            GUI.Label(new Rect(x + arrow, y, bw - arrow * 2, bh), SportsCars.Title(kind), carStyle);
            if (GUI.Button(new Rect(x + bw - arrow, y, arrow, bh), "▶", button)) Switch(kind, 1);
            y += bh + 16 * k;

            if (GUI.Button(new Rect(x, y, bw, bh), showHelp ? "Скрыть управление" : "Как играть", button)) showHelp = !showHelp;
            y += bh + 16 * k;
            if (GUI.Button(new Rect(x, y, bw, bh), "Выйти из игры", button)) Quit();
            y += bh + 24 * k;

            if (showHelp)
            {
                var r = new Rect(40 * k, y, Mathf.Max(panel - 80 * k, 940 * k), h - y - 30 * k);
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

        void Switch(PlayerCarKind current, int dir)
        {
            int count = System.Enum.GetValues(typeof(PlayerCarKind)).Length;
            var next = (PlayerCarKind)(((int)current + dir + count) % count);
            if (changeCar != null) changeCar(next);
            else GameBootstrap.CarChoice = next;
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
