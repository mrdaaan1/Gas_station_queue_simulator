using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Интерфейс на встроенном IMGUI (без TextMeshPro и Canvas).
    /// Раскладка — так, чтобы центр экрана оставался свободным для игры:
    ///   сверху по центру — таймер очереди;
    ///   слева сверху — лента уведомлений;
    ///   справа снизу — приборы (скорость, топливо, двигатель, радио);
    ///   слева снизу — подсказки по клавишам (F1).
    /// В центре только главное событие: «Бензин закончился».
    /// </summary>
    public class Hud : MonoBehaviour
    {
        bool showHelp = true;
        float helpAutoHide = 25f;
        GUIStyle timerStyle, smallStyle, feedStyle, bannerStyle, bigStyle, accentStyle, speedStyle, unitStyle, promptStyle;
        float builtForHeight;
        Texture2D pixel;
        PauseMenu pause;

        public void Init(PauseMenu pauseMenu) => pause = pauseMenu;

        void Update()
        {
            if (GameInput.HelpPressed) showHelp = !showHelp;
            // Подсказки сами прячутся через полминуты, чтобы не мешать
            if (helpAutoHide > 0f)
            {
                helpAutoHide -= Time.deltaTime;
                if (helpAutoHide <= 0f) showHelp = false;
            }
        }

        void BuildStyles()
        {
            float k = Screen.height / 1080f;
            builtForHeight = Screen.height;
            pixel = Texture2D.whiteTexture;

            timerStyle = Style(34, TextAnchor.MiddleCenter, Color.white, true);
            smallStyle = Style(19, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.9f));
            smallStyle.wordWrap = true;
            feedStyle = Style(20, TextAnchor.MiddleLeft, Color.white);
            feedStyle.wordWrap = true;
            bannerStyle = Style(56, TextAnchor.MiddleCenter, Color.white, true);
            bigStyle = Style(30, TextAnchor.UpperCenter, Color.white);
            bigStyle.wordWrap = true;
            accentStyle = Style(22, TextAnchor.MiddleCenter, Shapes.Hex("#ffe14d"), true);
            speedStyle = Style(54, TextAnchor.LowerRight, Color.white, true);
            unitStyle = Style(18, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.7f));
            promptStyle = Style(24, TextAnchor.MiddleCenter, Color.white, true);

            GUIStyle Style(int size, TextAnchor anchor, Color color, bool bold = false)
            {
                var st = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(size * k),
                    alignment = anchor,
                    fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                    richText = true,
                };
                st.normal.textColor = color;
                return st;
            }
        }

        void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (timerStyle == null || !Mathf.Approximately(builtForHeight, Screen.height)) BuildStyles();

            float w = Screen.width, h = Screen.height, k = h / 1080f;

            if (gm.State == GameState.Finished)
            {
                DrawFinal(gm, w, h, k);
                return;
            }
            if (pause != null && pause.IsOpen) return;

            DrawTimer(gm, w, k);
            DrawFeed(gm, k);
            DrawGauges(gm, w, h, k);
            DrawHelp(gm, h, k);

            // Бензин закончился — единственное, что занимает центр экрана
            if (gm.State == GameState.OutOfFuel)
            {
                var banner = new Rect(0, h * 0.17f, w, 150 * k);
                Panel(banner, new Color(0.65f, 0.08f, 0.08f, 0.8f));
                GUI.Label(new Rect(0, banner.y + 8 * k, w, 76 * k), "БЕНЗИН ЗАКОНЧИЛСЯ", bannerStyle);
                GUI.Label(new Rect(0, banner.y + 86 * k, w, 54 * k),
                    $"Следующий завоз через {Mathf.CeilToInt(gm.DeliveryMinutesLeft)} мин", timerStyle);
            }

            // Заправка — компактная плашка под таймером
            if (gm.State == GameState.Fueling)
            {
                var r = new Rect(w / 2 - 300 * k, 150 * k, 600 * k, 80 * k);
                Panel(r, new Color(0, 0, 0, 0.55f));
                GUI.Label(new Rect(r.x, r.y + 4 * k, r.width, 40 * k), $"Залито: {gm.LitersFilled:0.0} л", timerStyle);
                GUI.Label(new Rect(r.x, r.y + 44 * k, r.width, 30 * k),
                    $"АИ-95: {gm.PriceBoard.CurrentPrice:0.00} руб/л     Сумма: {gm.MoneySpent:0} руб.", accentStyle);
            }

            // Подсказка действия — снизу по центру, над краем экрана
            string prompt = null;
            if (gm.PlayerAtPump)
                prompt = gm.Player.Engine == EngineState.Off ? "E — заправиться" : "Заглушите мотор (I), затем E — заправиться";
            else if (!gm.CameraRig.CursorLocked)
                prompt = "Кликните, чтобы осмотреться мышью";
            if (prompt != null)
            {
                var r = new Rect(w / 2 - 320 * k, h - 120 * k, 640 * k, 46 * k);
                Panel(r, new Color(0.08f, 0.3f, 0.12f, 0.75f));
                GUI.Label(r, prompt, promptStyle);
            }
        }

        void DrawTimer(GameManager gm, float w, float k)
        {
            var timerRect = new Rect(w / 2 - 270 * k, 14 * k, 540 * k, 58 * k);
            Panel(timerRect, new Color(0, 0, 0, 0.5f));
            GUI.Label(timerRect, "Вы в очереди: " + GameManager.FormatQueueTime(gm.QueueSeconds), timerStyle);

            int p = gm.Queue.PlayerIndex;
            string place = gm.State == GameState.DrivingAway ? "Свобода!"
                : p == 0 ? "Вы первый у колонки!"
                : $"Машин впереди: {p}";
            var placeRect = new Rect(w / 2 - 160 * k, 76 * k, 320 * k, 36 * k);
            Panel(placeRect, new Color(0, 0, 0, 0.4f));
            GUI.Label(placeRect, place, accentStyle);
        }

        /// <summary>Лента уведомлений слева сверху: новые снизу, старые плавно гаснут.</summary>
        void DrawFeed(GameManager gm, float k)
        {
            float x = 20 * k, y = 20 * k, width = 430 * k;
            foreach (var item in gm.Feed)
            {
                float age = Time.time - item.shownAt;
                float alpha = Mathf.Clamp01((item.duration - age) / 1f) * Mathf.Clamp01(age / 0.25f);
                if (alpha <= 0f) continue;
                float height = feedStyle.CalcHeight(new GUIContent(item.text), width - 24 * k) + 14 * k;
                var r = new Rect(x, y, width, height);
                Panel(r, new Color(0, 0, 0, 0.45f * alpha));
                Panel(new Rect(x, y, 4 * k, height), new Color(1f, 0.85f, 0.3f, alpha));
                feedStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);
                GUI.Label(new Rect(x + 14 * k, y + 7 * k, width - 24 * k, height - 14 * k), item.text, feedStyle);
                y += height + 6 * k;
            }
        }

        /// <summary>Приборы справа снизу, как в гонках: скорость, топливо, двигатель, радио.</summary>
        void DrawGauges(GameManager gm, float w, float h, float k)
        {
            var player = gm.Player;
            float pw = 330 * k, ph = 175 * k;
            var r = new Rect(w - pw - 20 * k, h - ph - 20 * k, pw, ph);
            Panel(r, new Color(0, 0, 0, 0.5f));

            float x = r.x + 18 * k, y = r.y + 10 * k, inner = pw - 36 * k;

            // Скорость
            GUI.Label(new Rect(x, y, 150 * k, 62 * k), Mathf.RoundToInt(player.SpeedKmh).ToString(), speedStyle);
            GUI.Label(new Rect(x + 156 * k, y + 8 * k, 80 * k, 50 * k), "км/ч", unitStyle);

            // Двигатель
            string engineText; Color engineColor;
            switch (player.Engine)
            {
                case EngineState.Running: engineText = "МОТОР ВКЛ"; engineColor = Shapes.Hex("#5be37d"); break;
                case EngineState.Starting: engineText = "ЗАВОДИМ..."; engineColor = Shapes.Hex("#ffd34d"); break;
                default: engineText = "МОТОР ВЫКЛ"; engineColor = Shapes.Hex("#9a9a9a"); break;
            }
            var engineRect = new Rect(r.xMax - 132 * k, y + 16 * k, 114 * k, 32 * k);
            Panel(engineRect, new Color(engineColor.r, engineColor.g, engineColor.b, 0.25f));
            accentStyle.normal.textColor = engineColor;
            GUI.Label(engineRect, engineText, Shrink(accentStyle, 0.75f));
            accentStyle.normal.textColor = Shapes.Hex("#ffe14d");
            y += 72 * k;

            // Топливо
            float fuel = player.fuel;
            bool low = fuel < 0.1f;
            GUI.Label(new Rect(x, y, inner, 26 * k), $"Топливо: {player.FuelLiters:0.0} л из {gm.Settings.tankLiters:0}", smallStyle);
            y += 28 * k;
            var bar = new Rect(x, y, inner, 16 * k);
            Panel(bar, new Color(1f, 1f, 1f, 0.15f));
            var fill = low ? (Mathf.Repeat(Time.time, 1f) < 0.6f ? Shapes.Hex("#ff5a3c") : Shapes.Hex("#a83a28")) : Shapes.Hex("#f0b429");
            Panel(new Rect(bar.x, bar.y, bar.width * fuel, bar.height), fill);
            y += 24 * k;

            // Радио
            var radio = gm.Radio;
            string radioText = radio != null && radio.StationName != null ? radio.StationName : "Радио выкл (R)";
            GUI.Label(new Rect(x, y, inner, 26 * k), radioText, smallStyle);
            if (radio != null && radio.CurrentLine != null)
            {
                var lineRect = new Rect(r.x, r.y - 70 * k, pw, 62 * k);
                Panel(lineRect, new Color(0, 0, 0, 0.35f));
                GUI.Label(new Rect(lineRect.x + 12 * k, lineRect.y + 6 * k, pw - 24 * k, 56 * k), radio.CurrentLine, smallStyle);
            }
        }

        void DrawHelp(GameManager gm, float h, float k)
        {
            if (!showHelp)
            {
                GUI.Label(new Rect(20 * k, h - 40 * k, 300 * k, 30 * k), "F1 — управление", smallStyle);
                return;
            }
            var r = new Rect(20 * k, h - 250 * k, 360 * k, 230 * k);
            Panel(r, new Color(0, 0, 0, 0.45f));
            GUI.Label(new Rect(r.x + 14 * k, r.y + 10 * k, r.width - 28 * k, r.height),
                "W / S — газ / тормоз\n" +
                "I — заглушить / завести мотор\n" +
                "H / Пробел — бибикнуть\n" +
                $"C — камера ({gm.CameraRig.ModeName})\n" +
                "R — радио\n" +
                "E — заправиться (у колонки)\n" +
                "Мышь — оглядеться, Esc — пауза\n" +
                "F1 — скрыть подсказки", smallStyle);
        }

        void DrawFinal(GameManager gm, float w, float h, float k)
        {
            Panel(new Rect(0, 0, w, h), new Color(0.05f, 0.06f, 0.08f, 0.88f));
            GUI.Label(new Rect(0, h * 0.1f, w, 90 * k), "ВЫ ЗАПРАВИЛИСЬ!", bannerStyle);

            string stats =
                $"Простояли в очереди: {GameManager.FormatQueueTime(gm.QueueSeconds)}\n" +
                $"Бибикнули: {gm.Honks} раз\n" +
                $"Бибикали на вас: {gm.HonkedAt} раз\n" +
                $"Видели, как сдались и уехали: {gm.GiveUpsSeen}\n" +
                $"Поцеловали бампер: {gm.Bumps} раз\n" +
                $"Глушили мотор: {gm.EngineStops} раз\n" +
                $"Переключали радио: {gm.RadioSwitches} раз\n" +
                $"Залили: {gm.LitersFilled:0.0} л на {gm.MoneySpent:0} руб.";
            GUI.Label(new Rect(w / 2 - 450 * k, h * 0.22f, 900 * k, 360 * k), stats, bigStyle);

            string ach = "Достижения:\n" + string.Join("\n", gm.Achievements().ConvertAll(a => "• " + a));
            GUI.Label(new Rect(w / 2 - 450 * k, h * 0.58f, 900 * k, 300 * k), ach, bigStyle);

            GUI.Label(new Rect(0, h - 90 * k, w, 60 * k),
                "А через километр — пустая заправка без очереди...     Enter — сыграть ещё раз", accentStyle);
        }

        GUIStyle shrunk;
        GUIStyle Shrink(GUIStyle source, float factor)
        {
            if (shrunk == null) shrunk = new GUIStyle(source);
            shrunk.fontSize = Mathf.RoundToInt(source.fontSize * factor);
            shrunk.normal.textColor = source.normal.textColor;
            return shrunk;
        }

        void Panel(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, pixel);
            GUI.color = old;
        }
    }
}
