using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Звуки генерируются кодом, чтобы прототип работал без единого аудиофайла.
    /// Потом заменим на нормальные записи.
    /// </summary>
    public static class SoundFactory
    {
        const int Rate = 44100;

        static AudioClip horn, engine, noise;

        /// <summary>Клаксон: два чуть расстроенных тона с «квадратным» оттенком.</summary>
        public static AudioClip Horn
        {
            get
            {
                if (horn != null) return horn;
                int n = (int)(Rate * 0.55f);
                var data = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / Rate;
                    float env = Mathf.Clamp01(t / 0.02f) * Mathf.Clamp01((0.55f - t) / 0.06f);
                    float s = Soft(Mathf.Sin(2 * Mathf.PI * 415f * t)) + Soft(Mathf.Sin(2 * Mathf.PI * 523f * t));
                    data[i] = s * 0.35f * env;
                }
                horn = Create("Horn", data);
                return horn;
            }
        }

        /// <summary>Гул мотора на холостых. Ровно 1 секунда — склеивается в цикл без щелчков.</summary>
        public static AudioClip Engine
        {
            get
            {
                if (engine != null) return engine;
                var data = new float[Rate];
                var rnd = new System.Random(7);
                float lp = 0f;
                for (int i = 0; i < Rate; i++)
                {
                    float t = (float)i / Rate;
                    lp = Mathf.Lerp(lp, (float)rnd.NextDouble() * 2f - 1f, 0.05f);
                    float s = Mathf.Sin(2 * Mathf.PI * 38f * t) * 0.5f
                              + Mathf.Sin(2 * Mathf.PI * 76f * t) * 0.3f
                              + Mathf.Sin(2 * Mathf.PI * 114f * t) * 0.1f
                              + lp * 0.4f;
                    data[i] = s * 0.5f;
                }
                engine = Create("Engine", data);
                return engine;
            }
        }

        static AudioClip starter;

        /// <summary>Стартер: «вжик-вжик-вжик», чтобы было слышно, что машина заводится.</summary>
        public static AudioClip Starter
        {
            get
            {
                if (starter != null) return starter;
                int n = (int)(Rate * 1.1f);
                var data = new float[n];
                var rnd = new System.Random(11);
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / Rate;
                    float pulse = 0.55f + 0.45f * Mathf.Sin(2 * Mathf.PI * 9f * t);
                    float whine = Mathf.Sin(2 * Mathf.PI * (180f + 40f * Mathf.Sin(2 * Mathf.PI * 9f * t)) * t);
                    float noiseS = (float)rnd.NextDouble() * 2f - 1f;
                    float env = Mathf.Clamp01(t / 0.05f) * Mathf.Clamp01((1.1f - t) / 0.1f);
                    data[i] = (whine * 0.25f + noiseS * 0.25f) * pulse * env;
                }
                starter = Create("Starter", data);
                return starter;
            }
        }

        /// <summary>Радиопомехи при переключении станций.</summary>
        public static AudioClip Noise
        {
            get
            {
                if (noise != null) return noise;
                int n = (int)(Rate * 0.4f);
                var data = new float[n];
                var rnd = new System.Random(3);
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / n;
                    data[i] = ((float)rnd.NextDouble() * 2f - 1f) * 0.25f * (1f - t);
                }
                noise = Create("Noise", data);
                return noise;
            }
        }

        /// <summary>
        /// Простая зацикленная мелодия для радиостанции.
        /// notes — номера полутонов от ля (440 Гц), -100 = пауза.
        /// </summary>
        public static AudioClip Melody(string name, int[] notes, float noteLength, float waveSoftness, int bassOffset = -24)
        {
            int perNote = (int)(Rate * noteLength);
            var data = new float[perNote * notes.Length];
            for (int k = 0; k < notes.Length; k++)
            {
                if (notes[k] <= -100) continue;
                float f = 440f * Mathf.Pow(2f, notes[k] / 12f);
                float fb = 440f * Mathf.Pow(2f, (notes[k - k % 4] + bassOffset) / 12f);
                for (int i = 0; i < perNote; i++)
                {
                    float t = (float)i / Rate;
                    float env = Mathf.Exp(-t * 3f) * Mathf.Clamp01(t / 0.01f);
                    float lead = Mathf.Sin(2 * Mathf.PI * f * t);
                    lead = Mathf.Lerp(Mathf.Sign(lead) * 0.6f, lead, waveSoftness);
                    float bass = Mathf.Sin(2 * Mathf.PI * fb * t) * 0.5f;
                    data[k * perNote + i] = (lead * env + bass * 0.6f) * 0.3f;
                }
            }
            return Create(name, data);
        }

        static float Soft(float x) => Mathf.Clamp(x * 2.2f, -1f, 1f);

        static AudioClip Create(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Источник звука в 3D, привязанный к объекту.</summary>
        public static AudioSource Source3D(GameObject owner, float volume = 1f, float maxDistance = 120f)
        {
            var src = owner.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 3f;
            src.maxDistance = maxDistance;
            src.volume = volume;
            return src;
        }
    }
}
