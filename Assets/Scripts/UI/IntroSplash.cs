using System.Threading.Tasks;
using GasQueue.Intro;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Заставка при запуске игры (после логотипа Unity, ~5 с): коллаж из девяти кадров в стиле обложки GTA VI
    /// выпрыгивает по одному, потом бьёт градиентное «92» и влетает «симулятор очереди на заправку».
    /// Картинки рисуются кодом в отдельном потоке (<see cref="IntroArt"/>), анимация — <see cref="IntroLayout.Frame"/>.
    /// Любая клавиша или клик — пропустить. Показывается один раз за запуск, не при выходе в меню.
    /// </summary>
    public class IntroSplash : MonoBehaviour
    {
        static bool shown;

        /// <summary>Заставка закрывает экран — меню пока не рисуем.</summary>
        public static bool Playing { get; private set; }

        IntroArt art;
        Task[] jobs;
        Texture2D[] textures;
        IntroLayout layout;
        float t, stuck;
        float lastSoundT = -1f;
        AudioSource audioSource;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            shown = false;
            Playing = false;
        }

        public static void PlayOnce(GameObject host)
        {
            if (shown) return;
            shown = true;
            host.AddComponent<IntroSplash>();
        }

        void Awake()
        {
            Playing = true;
            int w = Screen.width, h = Screen.height;
            // На больших экранах рисуем чуть меньше и растягиваем — так быстрее
            float quality = Mathf.Min(1f, 1200f / Mathf.Max(1, h));
            art = IntroArt.Start(w, h, quality, out jobs);
            layout = art.Layout;
            textures = new Texture2D[IntroLayout.TexCount];
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true; // меню ставит звук на паузу
        }

        void Update()
        {
            // Готовые картинки — в текстуры; дальше той картинки, что ещё рисуется, время не идёт
            float limit = float.MaxValue;
            for (int i = 0; i < jobs.Length; i++)
            {
                if (textures[i] != null) continue;
                var job = jobs[i];
                if (job.IsFaulted || job.IsCanceled)
                {
                    Debug.LogWarning("Заставка не нарисовалась: " + job.Exception);
                    Finish();
                    return;
                }
                if (job.IsCompleted && art.Images[i] != null) textures[i] = ToTexture(art.Images[i], i);
                else limit = Mathf.Min(limit, IntroLayout.NeedTime(i) - 0.01f);
            }

            // Меню ставит игру на паузу (timeScale = 0) — время заставки идёт по реальным часам.
            // Первые кадры после загрузки бывают длинными — не перепрыгиваем через анимацию.
            float prev = t;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 20f);
            if (t > 0.3f && t < IntroLayout.FadeStart && GameInput.AnyPressed) t = IntroLayout.FadeStart;
            if (t < IntroLayout.FadeStart) t = Mathf.Min(t + dt, Mathf.Max(t, limit));
            else t += dt;
            // Что-то рисуется подозрительно долго — не держим игрока
            stuck = t == prev ? stuck + Time.unscaledDeltaTime : 0f;
            if (stuck > 5f) t = IntroLayout.FadeStart;
            PlaySounds(prev, t);
            if (t >= IntroLayout.MenuTime) Playing = false;
            if (t >= IntroLayout.EndTime) Finish();
        }

        static Texture2D ToTexture(Raster img, int index)
        {
            var tex = new Texture2D(img.Width, img.Height, TextureFormat.RGBA32, false)
            {
                name = "Intro" + index,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            tex.LoadRawTextureData(img.ToRgba32(true));
            tex.Apply(false, true);
            return tex;
        }

        void PlaySounds(float from, float to)
        {
            if (to <= lastSoundT || t >= IntroLayout.FadeStart) return;
            for (int i = 0; i < IntroLayout.PanelCount; i++)
            {
                float pt = IntroLayout.PanelTime(i);
                if (pt > from && pt <= to) audioSource.PlayOneShot(SoundFactory.Whoosh, 0.35f);
            }
            if (IntroLayout.LogoTime > from && IntroLayout.LogoTime <= to) audioSource.PlayOneShot(SoundFactory.IntroHit, 0.9f);
            for (int i = 0; i < 3; i++)
            {
                float lt = IntroLayout.LineTime(i);
                if (lt > from && lt <= to) audioSource.PlayOneShot(SoundFactory.Punch, 0.7f);
            }
            lastSoundT = to;
        }

        void Finish()
        {
            Playing = false;
            Destroy(this);
            Destroy(audioSource, 2f);
        }

        void OnDestroy()
        {
            if (textures == null) return;
            foreach (var tex in textures)
                if (tex != null) Destroy(tex);
            art = null;
        }

        void OnGUI()
        {
            GUI.depth = -1000; // поверх меню и интерфейса
            float w = Screen.width, h = Screen.height;
            if (Event.current.type != EventType.Repaint)
            {
                // Пока заставка закрывает экран, клики не доходят до меню
                if (Playing && (Event.current.isMouse || Event.current.isKey)) Event.current.Use();
                return;
            }

            var old = GUI.color;
            foreach (var s in IntroLayout.Frame(layout, w, h, t))
            {
                var tex = s.Tex >= 0 ? textures[s.Tex] : Texture2D.whiteTexture;
                if (tex == null) continue;
                GUI.color = new Color(s.R, s.G, s.B, s.Alpha);
                var uv = new Rect(s.U, 1f - s.V - s.VH, s.UW, s.VH);
                if (s.Clip)
                {
                    GUI.BeginGroup(new Rect(s.ClipX, s.ClipY, s.ClipW, s.ClipH));
                    GUI.DrawTextureWithTexCoords(new Rect(s.X - s.ClipX, s.Y - s.ClipY, s.W, s.H), tex, uv);
                    GUI.EndGroup();
                }
                else GUI.DrawTextureWithTexCoords(new Rect(s.X, s.Y, s.W, s.H), tex, uv);
            }
            GUI.color = old;
        }
    }
}
