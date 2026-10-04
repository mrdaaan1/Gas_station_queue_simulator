using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Интерфейс прототипа на встроенном IMGUI: не требует ни TextMeshPro, ни Canvas.
    /// Таймер очереди, сообщения, подсказки, баннер «Бензин закончился» и финальный экран.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        bool showHelp = true;
        GUIStyle timerStyle, smallStyle, messageStyle, bannerStyle, bigStyle, boxStyle;
        float builtForHeight;
        Texture2D pixel;

        void Update()
        {
            if (GameInput.HelpPressed) showHelp = !showHelp;
        }

        void BuildStyles()
        {
            float k = Screen.height / 1080f;
            builtForHeight = Screen.height;
            pixel = Texture2D.whiteTexture;

            timerStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(40 * k), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            timerStyle.normal.textColor = Color.white;
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(22 * k), alignment = TextAnchor.UpperLeft, wordWrap = true };
            smallStyle.normal.textColor = new Color(1f, 1f, 1f, 0.92f);
            messageStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(30 * k), alignment = TextAnchor.MiddleCenter, wordWrap = true, fontStyle = FontStyle.Bold };
            bannerStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(60 * k), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            bannerStyle.normal.textColor = Color.white;
            bigStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(30 * k), alignment = TextAnchor.UpperCenter, wordWrap = true };
            bigStyle.normal.textColor = Color.white;
            boxStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(26 * k), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            boxStyle.normal.textColor = Shapes.Hex("#ffe14d");
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

            // Таймер очереди
            var timerRect = new Rect(w / 2 - 330 * k, 18 * k, 660 * k, 70 * k);
            Panel(timerRect, new Color(0, 0, 0, 0.55f));
            GUI.Label(timerRect, "Вы в очереди: " + GameManager.FormatQueueTime(gm.QueueSeconds), timerStyle);

            int p = gm.Queue.PlayerIndex;
            string place = gm.State == GameState.DrivingAway ? "Свобода!"
                : p == 0 ? "Вы первый у колонки!"
                : $"Машин впереди: {p}";
            var placeRect = new Rect(w / 2 - 200 * k, 92 * k, 400 * k, 40 * k);
            Panel(placeRect, new Color(0, 0, 0, 0.4f));
            GUI.Label(placeRect, place, boxStyle);

            // Бензин закончился
            if (gm.State == GameState.OutOfFuel)
            {
                var banner = new Rect(0, h * 0.2f, w, 170 * k);
                Panel(banner, new Color(0.65f, 0.08f, 0.08f, 0.85f));
                GUI.Label(new Rect(0, h * 0.2f + 10 * k, w, 80 * k), "БЕНЗИН ЗАКОНЧИЛСЯ", bannerStyle);
                GUI.Label(new Rect(0, h * 0.2f + 95 * k, w, 60 * k),
                    $"Следующий завоз через {Mathf.CeilToInt(gm.DeliveryMinutesLeft)} мин", timerStyle);
            }

            // Заправка
            if (gm.State == GameState.Fueling)
            {
                var r = new Rect(w / 2 - 360 * k, h * 0.22f, 720 * k, 110 * k);
                Panel(r, new Color(0, 0, 0, 0.6f));
                GUI.Label(new Rect(r.x, r.y + 8 * k, r.width, 50 * k), $"Залито: {gm.LitersFilled:0.0} л", timerStyle);
                GUI.Label(new Rect(r.x, r.y + 60 * k, r.width, 40 * k),
                    $"АИ-95: {gm.PriceBoard.CurrentPrice:0.00} руб/л     Сумма: {gm.MoneySpent:0} руб.", boxStyle);
            }

            if (gm.PlayerAtPump)
                Prompt(w, h, k, "Нажмите E, чтобы заправиться");

            // Сообщения
            if (gm.MessageAlpha > 0f && !string.IsNullOrEmpty(gm.Message))
            {
                var r = new Rect(w / 2 - 520 * k, h * 0.62f, 1040 * k, 90 * k);
                Panel(r, new Color(0, 0, 0, 0.5f * gm.MessageAlpha));
                messageStyle.normal.textColor = new Color(1f, 1f, 1f, gm.MessageAlpha);
                GUI.Label(r, gm.Message, messageStyle);
            }

            // Радио
            var radio = gm.Radio;
            if (radio != null && radio.StationName != null)
            {
                var r = new Rect(w - 520 * k, h - 130 * k, 500 * k, 110 * k);
                Panel(r, new Color(0, 0, 0, 0.45f));
                GUI.Label(new Rect(r.x + 14 * k, r.y + 8 * k, r.width - 28 * k, r.height),
                    $"<b>{radio.StationName}</b>\n{radio.CurrentLine}", RichSmall());
            }

            // Подсказки по управлению
            if (showHelp)
            {
                var r = new Rect(20 * k, h - 260 * k, 430 * k, 240 * k);
                Panel(r, new Color(0, 0, 0, 0.45f));
                GUI.Label(new Rect(r.x + 14 * k, r.y + 10 * k, r.width - 28 * k, r.height),
                    "W / ↑ — газ     S / ↓ — тормоз\n" +
                    "H / Пробел — бибикнуть\n" +
                    $"C — камера ({gm.CameraRig.ModeName})\n" +
                    "R — переключить радио\n" +
                    "E — заправиться (у колонки)\n" +
                    "Клик — осмотреться мышью, Esc — курсор\n" +
                    "F1 — скрыть подсказки", smallStyle);
            }
            else
            {
                GUI.Label(new Rect(20 * k, h - 50 * k, 400 * k, 40 * k), "F1 — управление", smallStyle);
            }

            if (!gm.CameraRig.CursorLocked && gm.State != GameState.Fueling)
                GUI.Label(new Rect(w / 2 - 250 * k, h - 50 * k, 500 * k, 40 * k), "Кликните, чтобы осмотреться мышью",
                    new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleCenter });
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
                $"Переключали радио: {gm.RadioSwitches} раз\n" +
                $"Залили: {gm.LitersFilled:0.0} л на {gm.MoneySpent:0} руб.";
            GUI.Label(new Rect(w / 2 - 450 * k, h * 0.22f, 900 * k, 340 * k), stats, bigStyle);

            string ach = "Достижения:\n" + string.Join("\n", gm.Achievements().ConvertAll(a => "• " + a));
            GUI.Label(new Rect(w / 2 - 450 * k, h * 0.56f, 900 * k, 300 * k), ach, bigStyle);

            GUI.Label(new Rect(0, h - 90 * k, w, 60 * k),
                "А через километр — пустая заправка без очереди...     Enter — сыграть ещё раз", boxStyle);
        }

        void Prompt(float w, float h, float k, string text)
        {
            var r = new Rect(w / 2 - 300 * k, h * 0.5f, 600 * k, 60 * k);
            Panel(r, new Color(0.1f, 0.4f, 0.1f, 0.8f));
            GUI.Label(r, text, timerStyle);
        }

        GUIStyle richSmall;
        GUIStyle RichSmall()
        {
            if (richSmall == null || richSmall.fontSize != smallStyle.fontSize)
                richSmall = new GUIStyle(smallStyle) { richText = true };
            return richSmall;
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
