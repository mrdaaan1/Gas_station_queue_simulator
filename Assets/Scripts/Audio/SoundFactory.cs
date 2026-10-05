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

        static AudioClip crash;

        /// <summary>Удар: глухой «бум» + скрежет металла + звон стекла.</summary>
        public static AudioClip Crash
        {
            get
            {
                if (crash != null) return crash;
                int n = (int)(Rate * 0.9f);
                var data = new float[n];
                var rnd = new System.Random(21);
                float lp = 0f;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / Rate;
                    float noiseS = (float)rnd.NextDouble() * 2f - 1f;
                    lp = Mathf.Lerp(lp, noiseS, 0.15f);
                    float thump = Mathf.Sin(2 * Mathf.PI * (70f - 30f * t) * t) * Mathf.Exp(-t * 9f);
                    float metal = (Mathf.Sin(2 * Mathf.PI * 820f * t) * 0.4f + Mathf.Sin(2 * Mathf.PI * 1310f * t) * 0.3f + lp) * Mathf.Exp(-t * 6f);
                    float glass = t > 0.05f ? Mathf.Sin(2 * Mathf.PI * 3900f * t) * Mathf.Exp(-(t - 0.05f) * 14f) * (rnd.NextDouble() < 0.3 ? 1f : 0.2f) : 0f;
                    data[i] = Mathf.Clamp(thump * 0.9f + metal * 0.35f + glass * 0.15f, -1f, 1f) * Mathf.Clamp01(t / 0.004f);
                }
                crash = Create("Crash", data);
                return crash;
            }
        }

        static AudioClip tick;

        /// <summary>Щелчок реле поворотника.</summary>
        public static AudioClip Tick
        {
            get
            {
                if (tick != null) return tick;
                int n = (int)(Rate * 0.03f);
                var data = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / Rate;
                    data[i] = Mathf.Sin(2 * Mathf.PI * 2200f * t) * Mathf.Exp(-t * 220f) * 0.6f;
                }
                tick = Create("Tick", data);
                return tick;
            }
        }

        static AudioClip beepLow, beepHigh;

        /// <summary>Писк стартового светофора: низкий — «приготовиться», высокий — «старт».</summary>
        public static AudioClip Beep(bool high)
        {
            if (high ? beepHigh != null : beepLow != null) return high ? beepHigh : beepLow;
            float len = high ? 0.7f : 0.25f, freq = high ? 1320f : 880f;
            int n = (int)(Rate * len);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Min(1f, t * 200f) * Mathf.Min(1f, (len - t) * 30f);
                data[i] = (Mathf.Sin(2 * Mathf.PI * freq * t) > 0f ? 1f : -1f) * 0.25f * env;
            }
            var clip = Create(high ? "BeepHigh" : "BeepLow", data);
            if (high) beepHigh = clip; else beepLow = clip;
            return clip;
        }

        static AudioClip siren;

        /// <summary>«Кряк» спецсигнала: тон, который быстро взлетает и падает.</summary>
        public static AudioClip Siren
        {
            get
            {
                if (siren != null) return siren;
                int n = (int)(Rate * 1.2f);
                var data = new float[n];
                float phase = 0f;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / Rate;
                    float f = 650f + 550f * Mathf.Abs(Mathf.Sin(2 * Mathf.PI * 1.6f * t));
                    phase += 2 * Mathf.PI * f / Rate;
                    float s = Mathf.Clamp(Mathf.Sin(phase) * 1.8f, -1f, 1f);
                    float env = Mathf.Clamp01(t / 0.02f) * Mathf.Clamp01((1.2f - t) / 0.1f);
                    data[i] = s * 0.28f * env;
                }
                siren = Create("Siren", data);
                return siren;
            }
        }

        static AudioClip explosion;

        /// <summary>Взрыв: резкий хлопок, потом долгий низкий грохот с потрескиванием.</summary>
        public static AudioClip Explosion
        {
            get
            {
                if (explosion != null) return explosion;
                int n = (int)(Rate * 3.2f);
                var data = new float[n];
                var rnd = new System.Random(51);
                float lp = 0f, lp2 = 0f;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / Rate;
                    float noise = (float)rnd.NextDouble() * 2f - 1f;
                    lp = Mathf.Lerp(lp, noise, 0.04f);   // низкий гул
                    lp2 = Mathf.Lerp(lp2, noise, 0.35f); // треск
                    float crack = noise * Mathf.Exp(-t * 30f);
                    float boom = Mathf.Sin(2 * Mathf.PI * (48f - 20f * Mathf.Min(t, 1f)) * t) * Mathf.Exp(-t * 2.2f);
                    float rumble = lp * 3.2f * Mathf.Exp(-t * 1.1f);
                    float crackle = lp2 * (rnd.NextDouble() < 0.02 ? 1f : 0.15f) * Mathf.Exp(-t * 1.5f);
                    data[i] = Mathf.Clamp(crack * 0.9f + boom * 0.9f + rumble + crackle * 0.4f, -1f, 1f);
                }
                explosion = Create("Explosion", data);
                return explosion;
            }
        }

        static AudioClip punch, thud;

        /// <summary>Удар кулаком: короткий глухой шлепок.</summary>
        public static AudioClip Punch
        {
            get
            {
                if (punch != null) return punch;
                int n = (int)(Rate * 0.18f);
                var data = new float[n];
                var rnd = new System.Random(31);
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / Rate;
                    float slap = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 60f);
                    float body = Mathf.Sin(2 * Mathf.PI * (120f - 200f * t) * t) * Mathf.Exp(-t * 25f);
                    data[i] = Mathf.Clamp(slap * 0.6f + body * 0.8f, -1f, 1f);
                }
                punch = Create("Punch", data);
                return punch;
            }
        }

        /// <summary>Пинок по машине: жестяной «бдыщ».</summary>
        public static AudioClip Thud
        {
            get
            {
                if (thud != null) return thud;
                int n = (int)(Rate * 0.35f);
                var data = new float[n];
                var rnd = new System.Random(41);
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / Rate;
                    float tin = (Mathf.Sin(2 * Mathf.PI * 430f * t) + Mathf.Sin(2 * Mathf.PI * 690f * t) * 0.6f) * Mathf.Exp(-t * 12f);
                    float knock = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 40f);
                    data[i] = Mathf.Clamp(tin * 0.35f + knock * 0.5f, -1f, 1f);
                }
                thud = Create("Thud", data);
                return thud;
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
