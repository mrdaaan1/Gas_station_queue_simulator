using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Снимок «с телефона»: временная камера у вытянутой руки игрока смотрит на два лица,
    /// вспышка подсвечивает их настоящим светом, картинка читается в текстуру.
    /// </summary>
    public static class SelfieCamera
    {
        const int W = 480, H = 600;

        public static Texture2D Take(WalkerController me, DavidychRig dav)
        {
            var a = me.Rig.head.position;
            var b = dav.Head.position;
            var mid = (a + b) / 2f;
            var facing = dav.transform.forward;
            // Телефон на вытянутой руке: перед лицами на уровне глаз того, кто ниже (иначе видно только макушку),
            // в кадре — оба лица целиком
            var pos = mid + facing * 1.05f;
            pos.y = Mathf.Min(a.y, b.y) + 0.1f;

            var go = new GameObject("SelfieCamera");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(mid - Vector3.up * 0.04f - pos);
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = 46f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400f;
            var main = Camera.main;
            if (main != null)
            {
                cam.clearFlags = main.clearFlags;
                cam.backgroundColor = main.backgroundColor;
            }
            // Вспышка
            var flash = go.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.range = 5f;
            flash.intensity = 2.2f;
            flash.color = new Color(1f, 0.97f, 0.92f);

            // Реплики над головами в кадр не берём
            var bubbles = new System.Collections.Generic.List<GameObject>();
            foreach (var sb in Object.FindObjectsByType<SpeechBubble>(FindObjectsSortMode.None))
                if (sb.gameObject.activeSelf) { sb.gameObject.SetActive(false); bubbles.Add(sb.gameObject); }

            // Руку с телефоном в кадр не берём: снимает она сама
            var hidden = new System.Collections.Generic.List<Renderer>();
            foreach (var r in me.Rig.armR.GetComponentsInChildren<Renderer>())
                if (r.enabled) { r.enabled = false; hidden.Add(r); }

            var rt = RenderTexture.GetTemporary(W, H, 24);
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            Warm(tex);
            tex.Apply();
            RenderTexture.active = prev;
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            Object.Destroy(go);
            foreach (var g in bubbles) if (g != null) g.SetActive(true);
            foreach (var r in hidden) if (r != null) r.enabled = true;
            return tex;
        }

        /// <summary>Тёплый «фильтр» и виньетка — как у фото с телефона на закате.</summary>
        static void Warm(Texture2D tex)
        {
            var px = tex.GetPixels();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = (x - W / 2f) / (W / 2f), dy = (y - H / 2f) / (H / 2f);
                    float vig = 1f - 0.35f * Mathf.Clamp01((dx * dx + dy * dy) - 0.25f);
                    var c = px[y * W + x];
                    c = new Color(c.r * 1.06f + 0.02f, c.g * 1.0f + 0.01f, c.b * 0.9f, 1f) * vig;
                    px[y * W + x] = c;
                }
            tex.SetPixels(px);
        }
    }

    /// <summary>
    /// Вспышка и карточка с фото в духе меню Mafia II: тёмная панель с рваными краями,
    /// заголовок на тёмной «мазне», фото в рамке, под ним дата и подпись. Висит несколько секунд,
    /// любая клавиша — закрыть.
    /// </summary>
    public class SelfieCard : MonoBehaviour
    {
        Texture2D photo;
        string caption;
        int number;
        float t;
        float tilt;
        static Texture2D brush, panel, white;
        static int shown;

        public static void Show(Texture2D photo, string caption)
        {
            var go = new GameObject("SelfieCard");
            var card = go.AddComponent<SelfieCard>();
            card.photo = photo;
            card.caption = caption;
            card.number = ++shown;
            card.tilt = Random.Range(-3f, 3f);
            AudioSource.PlayClipAtPoint(SoundFactory.Tick, Camera.main != null ? Camera.main.transform.position : Vector3.zero, 1f);
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (t > 1.2f && t < 6.5f && GameInput.AnyPressed) t = 6.5f;
            if (t > 7f)
            {
                Destroy(photo);
                Destroy(gameObject);
            }
        }

        static Texture2D Ragged(int w, int h, Color c, float edge, int seed)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var rnd = new System.Random(seed);
            var px = new Color32[w * h];
            // Рваный край: по каждой стороне — своя «зубчатая» граница
            var left = new float[h]; var right = new float[h]; var top = new float[w]; var bottom = new float[w];
            float v = 0f;
            for (int y = 0; y < h; y++) { v = Mathf.Clamp(v + (float)(rnd.NextDouble() - 0.5) * 0.4f, 0f, 1f); left[y] = edge * (0.3f + v); }
            for (int y = 0; y < h; y++) { v = Mathf.Clamp(v + (float)(rnd.NextDouble() - 0.5) * 0.4f, 0f, 1f); right[y] = edge * (0.3f + v); }
            for (int x = 0; x < w; x++) { v = Mathf.Clamp(v + (float)(rnd.NextDouble() - 0.5) * 0.4f, 0f, 1f); top[x] = edge * (0.3f + v); }
            for (int x = 0; x < w; x++) { v = Mathf.Clamp(v + (float)(rnd.NextDouble() - 0.5) * 0.4f, 0f, 1f); bottom[x] = edge * (0.3f + v); }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool inside = x > left[y] && x < w - right[y] && y > bottom[x] && y < h - top[x];
                    float grain = 0.9f + 0.1f * (float)rnd.NextDouble();
                    px[y * w + x] = inside ? (Color32)new Color(c.r * grain, c.g * grain, c.b * grain, c.a) : new Color32(0, 0, 0, 0);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        void OnGUI()
        {
            if (brush == null) brush = Ragged(512, 72, new Color(0.08f, 0.08f, 0.08f, 0.95f), 10f, 3);
            if (panel == null) panel = Ragged(640, 480, new Color(0.05f, 0.05f, 0.06f, 0.82f), 14f, 7);
            if (white == null) { white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply(); }
            GUI.depth = -100;

            float sw = Screen.width, sh = Screen.height;
            // Вспышка
            float flash = 1f - Mathf.Clamp01(t / 0.45f);
            if (flash > 0f)
            {
                GUI.color = new Color(1f, 1f, 1f, flash);
                GUI.DrawTexture(new Rect(0, 0, sw, sh), white);
            }
            float a = Mathf.Clamp01((t - 0.15f) / 0.3f) * (1f - Mathf.Clamp01((t - 6.5f) / 0.5f));
            if (a <= 0f) { GUI.color = Color.white; return; }

            float k = sh / 720f;
            float grow = Mathf.Lerp(1.12f, 1f, Mathf.Clamp01((t - 0.15f) / 0.3f));
            float pw = 560f * k * grow, ph = 520f * k * grow;
            var pr = new Rect((sw - pw) / 2f, (sh - ph) / 2f, pw, ph);
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTexture(pr, panel);

            // Заголовок на «мазне», как «СНОВА В ДЕЛЕ»
            var hr = new Rect(pr.x - 10f * k, pr.y - 18f * k, 300f * k, 46f * k);
            GUI.DrawTexture(hr, brush);
            var head = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(26 * k), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            head.normal.textColor = new Color(0.93f, 0.92f, 0.88f, a);
            GUI.Label(new Rect(hr.x + 16f * k, hr.y, hr.width, hr.height), "ФОТО НА ПАМЯТЬ", head);

            // Карточка: чёрная рамка, тонкая светлая кайма, фото, дата и подпись
            float cw = 300f * k * grow, chh = 420f * k * grow;
            var cr = new Rect(pr.center.x - cw / 2f, pr.y + 50f * k, cw, chh);
            var pivot = cr.center;
            GUIUtility.RotateAroundPivot(tilt, pivot);
            GUI.color = new Color(0.02f, 0.02f, 0.02f, a);
            GUI.DrawTexture(cr, white);
            GUI.color = new Color(0.85f, 0.84f, 0.8f, a);
            GUI.DrawTexture(new Rect(cr.x + 2, cr.y + 2, cr.width - 4, cr.height - 4), white);
            GUI.color = new Color(0.06f, 0.06f, 0.06f, a);
            GUI.DrawTexture(new Rect(cr.x + 4, cr.y + 4, cr.width - 8, cr.height - 8), white);
            float m = 12f * k;
            float photoH = (cw - 2 * m) * 600f / 480f;
            GUI.color = new Color(1f, 1f, 1f, a);
            if (photo != null) GUI.DrawTexture(new Rect(cr.x + m, cr.y + m, cw - 2 * m, photoH), photo, ScaleMode.ScaleAndCrop);

            var txt = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(15 * k), fontStyle = FontStyle.Italic, alignment = TextAnchor.UpperLeft };
            txt.normal.textColor = new Color(0.9f, 0.89f, 0.85f, a);
            float ty = cr.y + m + photoH + 4f * k;
            string clock = DayNight.Instance != null ? "  " + DayNight.Instance.ClockText : "";
            GUI.Label(new Rect(cr.x + m, ty, cw, 22f * k), "/ " + System.DateTime.Now.ToString("dd/MM/yyyy") + clock, txt);
            GUI.Label(new Rect(cr.x + m, ty + 20f * k, cw, 22f * k), "*" + caption, txt);
            var num = new GUIStyle(txt) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal, fontSize = Mathf.RoundToInt(13 * k) };
            num.normal.textColor = new Color(0.7f, 0.7f, 0.68f, a);
            GUI.Label(new Rect(cr.x, cr.yMax - 22f * k, cw, 18f * k), number.ToString(), num);
            GUI.matrix = Matrix4x4.identity;

            var hint = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(13 * k), alignment = TextAnchor.LowerRight };
            hint.normal.textColor = new Color(0.8f, 0.8f, 0.78f, a * 0.8f);
            GUI.Label(new Rect(pr.x, pr.yMax - 30f * k, pr.width - 18f * k, 22f * k), "любая клавиша — дальше", hint);
            GUI.color = Color.white;
        }
    }
}
