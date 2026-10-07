using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Время суток и погода в основной игре. Игрок встаёт в очередь ближе к вечеру (16:30), час игрового времени
    /// идёт ~2,5 реальной минуты: за обычную партию солнце садится, зажигаются фонари и фары, становится темно.
    /// Погода меняется сама раз в несколько минут: ясно → облачно → дождь (капли вокруг камеры, шум, туман гуще).
    /// Освещение берёт солнце и небо, которые создал <see cref="CityBuilder"/>, и подкручивает их.
    /// </summary>
    public class DayNight : MonoBehaviour
    {
        public static DayNight Instance { get; private set; }

        const float StartHour = 16.5f;
        const float SecondsPerHour = 150f;

        public enum Weather { Clear, Cloudy, Rain }

        /// <summary>Часы, 0…24.</summary>
        public float Hour { get; private set; } = StartHour;
        public Weather Current { get; private set; } = Weather.Clear;
        public bool IsDark => darkness > 0.5f;
        public string ClockText => $"{(int)Hour:00}:{(int)((Hour - (int)Hour) * 60f):00}";

        PlayerCar player;
        Light sun;
        Material sky;
        float baseExposure = 1.3f;
        float darkness;      // 0 — день, 1 — ночь
        float cloud;         // 0…1, плавно к цели
        float rain;          // 0…1
        float weatherTimer = Random.Range(150f, 240f);
        bool announcedDusk, announcedNight;

        // Ночные огни
        Light[] lampLights;
        Light headLight;
        Light[] canopyLights;
        Material headMat, tailMat, lampMat;

        // Дождь
        ParticleSystem rainPs;
        AudioSource rainSound;

        static readonly Color DayAmbSky = Shapes.Hex("#b4c8de"), DayAmbEq = Shapes.Hex("#9a9a94"), DayAmbGround = Shapes.Hex("#55574f");
        static readonly Color NightAmbSky = Shapes.Hex("#26304a"), NightAmbEq = Shapes.Hex("#1a1e28"), NightAmbGround = Shapes.Hex("#0e0f13");
        static readonly Color DayFog = Shapes.Hex("#c9d3dc"), DuskFog = Shapes.Hex("#d6a888"), NightFog = Shapes.Hex("#0e131d"), RainFog = Shapes.Hex("#8d969e");
        static readonly Color SunDay = Shapes.Hex("#fff1dc"), SunLow = Shapes.Hex("#ffa860"), Moon = Shapes.Hex("#8aa2d8");

        public void Init(PlayerCar playerCar, Transform root)
        {
            Instance = this;
            player = playerCar;
            sun = RenderSettings.sun;
            if (RenderSettings.skybox != null)
            {
                // Копия неба: крутим яркость, не трогая общий материал
                sky = new Material(RenderSettings.skybox);
                if (sky.HasProperty("_Exposure")) baseExposure = sky.GetFloat("_Exposure");
                RenderSettings.skybox = sky;
            }

            // Фонари: несколько настоящих источников света переезжают к ближайшим к игроку столбам
            lampLights = new Light[6];
            for (int i = 0; i < lampLights.Length; i++)
                lampLights[i] = MakeLight(root, "StreetLight", LightType.Point, Shapes.Hex("#ffd9a0"), 18f, 0f);
            // Навес над колонками
            canopyLights = new Light[2];
            for (int i = 0; i < canopyLights.Length; i++)
            {
                canopyLights[i] = MakeLight(root, "CanopyLight", LightType.Point, Shapes.Hex("#f4f6ff"), 16f, 0f);
                canopyLights[i].transform.position = new Vector3(CityLayout.IslandX[i], 5.2f, CityLayout.IslandZ);
            }
            // Фары игрока
            headLight = MakeLight(player.transform, "Headlights", LightType.Spot, Shapes.Hex("#fff3d6"), 45f, 0f);
            headLight.spotAngle = 70f;
            headLight.transform.localPosition = new Vector3(0f, 0.8f, 2.2f);
            headLight.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);

            // Фары и фонари у всех машин — общие материалы, ночью им включаем свечение
            headMat = Shapes.Mat(Shapes.Hex("#fff6d5"));
            tailMat = Shapes.Mat(Shapes.Hex("#b5160f"));
            lampMat = Shapes.Mat(CityBuilder.LampGlass);

            BuildRain();
            Apply(0f);
        }

        static Light MakeLight(Transform parent, string name, LightType type, Color color, float range, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<Light>();
            l.type = type;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            l.enabled = false;
            return l;
        }

        void BuildRain()
        {
            var go = new GameObject("Rain");
            go.transform.SetParent(transform, false);
            rainPs = go.AddComponent<ParticleSystem>();
            rainPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = rainPs.main;
            main.loop = true;
            main.startLifetime = 1.1f;
            main.startSpeed = 0f;
            main.startSize3D = true;
            main.startSizeX = 0.025f;
            main.startSizeY = 0.5f;
            main.startSizeZ = 0.025f;
            main.maxParticles = 2500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.75f, 0.8f, 0.88f, 1f);
            var vel = rainPs.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.6f);
            vel.y = new ParticleSystem.MinMaxCurve(-16f);
            vel.z = new ParticleSystem.MinMaxCurve(0f);
            var shape = rainPs.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(36f, 0.5f, 36f);
            var emission = rainPs.emission;
            emission.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.035f;
            r.lengthScale = 1f;
            r.sharedMaterial = Shapes.Mat(new Color(0.72f, 0.78f, 0.86f));
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            // Сквозь крышу машины игрока не капает: капли внутри неё гаснут
            var shield = new GameObject("RainShield").AddComponent<BoxCollider>();
            shield.transform.SetParent(player.transform, false);
            shield.isTrigger = true;
            shield.center = new Vector3(0f, 0.9f, 0f);
            shield.size = new Vector3(2.3f, 1.9f, 4.8f);
            var trigger = rainPs.trigger;
            trigger.enabled = true;
            trigger.AddCollider(shield);
            trigger.inside = ParticleSystemOverlapAction.Kill;
            trigger.enter = ParticleSystemOverlapAction.Kill;
            trigger.outside = ParticleSystemOverlapAction.Ignore;
            rainPs.Play();

            rainSound = gameObject.AddComponent<AudioSource>();
            rainSound.clip = RainClip();
            rainSound.loop = true;
            rainSound.volume = 0f;
            rainSound.spatialBlend = 0f;
            rainSound.Play();
        }

        /// <summary>Шум дождя: сглаженный белый шум с редкими «каплями».</summary>
        static AudioClip RainClip()
        {
            const int rate = 22050;
            int n = rate * 3;
            var data = new float[n];
            var rnd = new System.Random(5);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float white = (float)rnd.NextDouble() * 2f - 1f;
                lp += (white - lp) * 0.35f;
                float drop = rnd.NextDouble() < 0.0015 ? (float)rnd.NextDouble() * 0.6f : 0f;
                data[i] = lp * 0.5f + drop;
            }
            // Сшиваем концы, чтобы петля не щёлкала
            for (int i = 0; i < 2000; i++)
            {
                float k = i / 2000f;
                data[n - 2000 + i] = Mathf.Lerp(data[n - 2000 + i], data[i], k);
            }
            var clip = AudioClip.Create("Rain", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Hour = (Hour + dt / SecondsPerHour) % 24f;
            UpdateWeather(dt);
            Apply(dt);
        }

        void UpdateWeather(float dt)
        {
            weatherTimer -= dt;
            if (weatherTimer <= 0f)
            {
                weatherTimer = Random.Range(150f, 260f);
                var next = Current == Weather.Clear ? Weather.Cloudy
                    : Current == Weather.Rain ? (Random.value < 0.6f ? Weather.Cloudy : Weather.Clear)
                    : (Random.value < 0.65f ? Weather.Rain : Weather.Clear);
                SetWeather(next);
            }
            float cloudTarget = Current == Weather.Clear ? 0f : 1f;
            float rainTarget = Current == Weather.Rain ? 1f : 0f;
            cloud = Mathf.MoveTowards(cloud, cloudTarget, dt / 25f);
            rain = Mathf.MoveTowards(rain, rainTarget, dt / (rainTarget > rain ? 12f : 8f));
        }

        void SetWeather(Weather w)
        {
            if (w == Current) return;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                if (w == Weather.Rain)
                    gm.ShowMessage(Random.value < 0.5f ? "Пошёл дождь. Очередь стоит, как стояла." : "Дождь. Те, кто ушёл за шашлыком, бегут к машинам.", 6f);
                else if (Current == Weather.Rain)
                    gm.ShowMessage("Дождь кончился. Очередь — нет.", 5f);
                else if (w == Weather.Cloudy)
                    gm.ShowMessage("Затянуло тучами. Кажется, будет дождь.", 5f);
            }
            Current = w;
        }

        void Apply(float dt)
        {
            // Высота солнца: восход ~5:30, закат ~20:30 (лето), в полдень ~55°
            float day = (Hour - 5.5f) / 15f;
            float elev = day > 0f && day < 1f ? Mathf.Sin(day * Mathf.PI) * 55f : -12f;
            if (day >= 1f && Hour - 20.5f < 1f) elev = -12f * Mathf.Clamp01((Hour - 20.5f) / 1f); // сумерки после заката
            // Темнота: от сумерек (солнце у горизонта) до ночи
            darkness = Mathf.Clamp01(Mathf.InverseLerp(6f, -8f, elev));
            float low = Mathf.Clamp01(Mathf.InverseLerp(18f, 2f, elev)) * (1f - darkness); // закатный свет

            if (sun != null)
            {
                float az = Mathf.Lerp(-110f, 110f, Mathf.Clamp01(day));
                // Ночью свет — «луна»: высоко, холодный, слабый
                var dir = darkness < 0.98f ? Quaternion.Euler(Mathf.Max(elev, 4f), az, 0f) : Quaternion.Euler(50f, 30f, 0f);
                sun.transform.rotation = dir;
                var c = Color.Lerp(Color.Lerp(SunDay, SunLow, low), Moon, darkness);
                sun.color = c;
                float cloudDim = Mathf.Lerp(1f, 0.45f, cloud);
                sun.intensity = Mathf.Lerp(1.1f * cloudDim, 0.12f, darkness);
                sun.shadowStrength = Mathf.Lerp(1f, 0.3f, Mathf.Max(cloud * 0.8f, darkness));
            }

            var grey = new Color(0.62f, 0.64f, 0.66f);
            RenderSettings.ambientSkyColor = Color.Lerp(Color.Lerp(Color.Lerp(DayAmbSky, Shapes.Hex("#d9b9a8"), low), grey, cloud * 0.6f), NightAmbSky, darkness);
            RenderSettings.ambientEquatorColor = Color.Lerp(Color.Lerp(DayAmbEq, Shapes.Hex("#a08878"), low), NightAmbEq, darkness);
            RenderSettings.ambientGroundColor = Color.Lerp(DayAmbGround, NightAmbGround, darkness);

            var fog = Color.Lerp(Color.Lerp(DayFog, DuskFog, low), RainFog, Mathf.Max(cloud * 0.5f, rain * 0.9f));
            RenderSettings.fogColor = Color.Lerp(fog, NightFog, darkness);
            RenderSettings.fogStartDistance = Mathf.Lerp(120f, 30f, rain);
            RenderSettings.fogEndDistance = Mathf.Lerp(520f, 230f, Mathf.Max(rain, darkness * 0.5f));

            if (sky != null)
            {
                if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", baseExposure * Mathf.Lerp(Mathf.Lerp(1f, 0.55f, cloud), 0.06f, darkness));
                if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(1f, 2.2f, Mathf.Max(cloud * 0.7f, low)));
            }

            // Огни: включаются в сумерках
            float lights = Mathf.Clamp01(Mathf.InverseLerp(0.25f, 0.7f, darkness) + rain * 0.3f * Mathf.Clamp01(darkness * 3f));
            Glow(headMat, new Color(1f, 0.93f, 0.75f) * 1.6f * lights);
            Glow(tailMat, new Color(0.9f, 0.05f, 0.03f) * 1.2f * lights);
            Glow(lampMat, new Color(1f, 0.82f, 0.55f) * 2f * lights);
            foreach (var cl in canopyLights) SetLight(cl, 2.2f * lights);
            SetLight(headLight, 3f * lights);
            PlaceLampLights(lights);

            // Дождь — вокруг камеры
            var cam = Camera.main;
            if (rainPs != null)
            {
                if (cam != null) rainPs.transform.position = cam.transform.position + Vector3.up * 14f + cam.transform.forward * 6f;
                var em = rainPs.emission;
                em.rateOverTime = 2000f * rain;
                rainSound.volume = 0.35f * rain;
            }

            // Сообщения о вечере
            var gm = GameManager.Instance;
            if (gm != null && dt > 0f)
            {
                if (!announcedDusk && low > 0.6f)
                {
                    announcedDusk = true;
                    gm.ShowMessage($"Вечереет ({ClockText}). Солнце садится, а очередь — нет.", 6f);
                }
                if (!announcedNight && darkness > 0.8f)
                {
                    announcedNight = true;
                    gm.ShowMessage($"Стемнело ({ClockText}). Встали в очередь засветло. Фонари горят через один.", 7f);
                }
            }
        }

        static void SetLight(Light l, float intensity)
        {
            if (l == null) return;
            l.intensity = intensity;
            l.enabled = intensity > 0.02f;
        }

        static void Glow(Material m, Color emission)
        {
            if (m == null || !m.HasProperty("_EmissionColor")) return;
            if (emission.maxColorComponent > 0.01f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
        }

        /// <summary>Свет у ближайших к игроку фонарей (каждый второй столб «не горит» — как в жизни).</summary>
        void PlaceLampLights(float lights)
        {
            var heads = CityBuilder.LampHeads;
            var gm = GameManager.Instance;
            Vector3 me = gm != null && gm.OnFoot && gm.Walker != null ? gm.Walker.transform.position : player.transform.position;
            int used = 0;
            if (lights > 0.02f && heads.Count > 0)
            {
                // Простой выбор ближайших: фонарей немного (~60), раз в кадр это дёшево
                var taken = new bool[heads.Count];
                for (; used < lampLights.Length; used++)
                {
                    int best = -1;
                    float bestD = 70f * 70f;
                    for (int i = 0; i < heads.Count; i++)
                    {
                        if (taken[i] || i % 3 == 2) continue; // каждый третий не горит
                        float d = (heads[i] - me).sqrMagnitude;
                        if (d < bestD) { bestD = d; best = i; }
                    }
                    if (best < 0) break;
                    taken[best] = true;
                    lampLights[used].transform.position = heads[best];
                    SetLight(lampLights[used], 2.4f * lights);
                }
            }
            for (int i = used; i < lampLights.Length; i++) SetLight(lampLights[i], 0f);
        }

        void OnDestroy()
        {
            // Общие материалы переживают перестройку мира — гасим свечение
            Glow(headMat, Color.black);
            Glow(tailMat, Color.black);
            Glow(lampMat, Color.black);
            if (Instance == this) Instance = null;
        }
    }
}
