using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Тупое радио в машине. R — следующая станция.
    /// Свои треки (mp3/ogg/wav) кладутся в Assets/Resources/RaceMusic — появляется станция «Гонка FM»:
    /// треки по кругу в случайном порядке. В гонке она включается сама на зелёный.
    /// Озвученные новости и реклама (Tools/make_radio_news.sh, голоса macOS) лежат в Assets/Resources/RadioNews —
    /// появляется станция «Очередь FM»: тихая музыка, между ней дикторы читают выпуски, заставка каждые несколько выпусков.
    /// </summary>
    public class Radio : MonoBehaviour
    {
        class Station
        {
            public string name;
            public string shortName;
            public string[] lines;
            public AudioClip music;
            public AudioClip[] playlist; // своя музыка: треки по очереди
            public int track;
            public AudioClip[] voices;  // «Очередь FM»: озвученные выпуски
            public AudioClip[] jingles; // и заставки станции
        }

        const string MusicFolder = "RaceMusic";
        const string NewsFolder = "RadioNews";

        /// <summary>Субтитры к озвученным выпускам: имя файла без номера → текст (тот же, что в make_radio_news.sh).</summary>
        static readonly System.Collections.Generic.Dictionary<string, string> NewsText = new System.Collections.Generic.Dictionary<string, string>
        {
            { "news_stable", "«Новости. Ситуация с топливом в регионе стабильная. Дефицита бензина нет. Есть повышенный спрос, который будет удовлетворён в порядке живой очереди.»" },
            { "news_price", "«Экономика. Цена на девяносто второй выросла на сорок копеек. Эксперты подчёркивают: это не рост цен, а плановая корректировка вверх.»" },
            { "news_queue", "«Дорожная обстановка. На заправках города наблюдается небольшое скопление автомобилей. Средняя длина небольшого скопления — четыре километра.»" },
            { "news_deliver", "«Срочно. Бензовоз выехал. Куда — не уточняется. Когда приедет — тоже. Оставайтесь на нашей волне и в своей очереди.»" },
            { "news_minister", "«Профильное министерство призвало граждан не создавать ажиотаж и заправляться только по необходимости. Например, когда кончился бензин.»" },
            { "news_record", "«Хорошие новости. Очередь на заправке ЛУКАВОЙЛ установила новый рекорд города. Поздравляем всех участников.»" },
            { "news_tip", "«Совет водителям. Чтобы сэкономить топливо, глушите двигатель в очереди. А чтобы сэкономить нервы — не смотрите на табло с ценами.»" },
            { "news_weather", "«Погода. Ночью ожидается похолодание. Водителям в очереди рекомендуем взять плед, термос и запасное терпение.»" },
            { "news_neighbor", "«По непроверенным данным, на соседней заправке бензин есть. По проверенным — там тоже очередь.»" },
            { "news_survey", "«Опрос показал: девяносто процентов водителей довольны ситуацией с топливом. Остальные десять процентов стояли в очереди и опрос не прошли.»" },
            { "ad_lukavoil", "«ЛУКАВОЙЛ. Бензин есть всегда. Кроме сегодня. И вчера. Звёздочка — условия акции уточняйте у заправщика.»" },
            { "ad_vip", "«Надоели очереди? Талон без очереди — всего за пять тысяч рублей! Звоните прямо сейчас. Номер не скажем, вы его и так знаете.»" },
            { "ad_pies", "«Пирожки от тёти Вали. С картошкой, с капустой, с ожиданием. Ищите тётю Валю вдоль очереди.»" },
            { "ad_canister", "«Канистра по блату. Двадцать литров по цене шестидесяти. Без чека, без вопросов, без гарантий.»" },
            { "jingle_1", "«Вы слушаете радио «Очередь эф эм». Мы стоим вместе с вами.»" },
            { "jingle_2", "«Радио «Очередь эф эм». Музыка, пока вы ждёте. А ждать вы будете долго.»" },
        };

        /// <summary>Гонка: на радио только свои треки, R — следующий трек (задаётся до создания радио).</summary>
        public static bool RaceOnly;
        bool tracksOnly;

        Station[] stations;
        int current; // 0 — выключено
        int lineIndex;
        float lineTimer;
        AudioSource music;
        AudioSource fx;
        AudioSource voice;
        float voiceGap;      // пауза до следующего выпуска
        int voiceIndex, sinceJingle;

        /// <summary>Экран магнитолы на торпеде машины игрока.</summary>
        public TextMesh display;

        public string StationName => current == 0 ? null : stations[current - 1].name;
        public string CurrentLine { get; private set; }

        void Awake()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.volume = 0.12f;
            music.spatialBlend = 0f;
            fx = gameObject.AddComponent<AudioSource>();
            fx.volume = 0.5f;
            fx.spatialBlend = 0f;
            voice = gameObject.AddComponent<AudioSource>();
            voice.volume = 0.9f;
            voice.spatialBlend = 0f;

            var list = new System.Collections.Generic.List<Station>();
            var tracks = Resources.LoadAll<AudioClip>(MusicFolder);
            if (tracks.Length > 0)
            {
                // Перемешиваем, чтобы каждый заезд начинался с другого трека
                for (int i = tracks.Length - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (tracks[i], tracks[j]) = (tracks[j], tracks[i]);
                }
                list.Add(new Station
                {
                    name = "Гонка FM",
                    shortName = "104.5 ГОНКА",
                    playlist = tracks,
                    lines = new[] { "" },
                });
            }
            raceStation = list.Count > 0 ? 1 : 0;
            tracksOnly = RaceOnly && raceStation == 1;
            if (tracksOnly)
            {
                stations = list.ToArray();
                return;
            }
            var news = LoadNews(out var jingles);
            if (news.Length + jingles.Length > 0)
            {
                list.Add(new Station
                {
                    name = "Радио «Очередь FM»",
                    shortName = "88.8 ОЧЕРЕДЬ",
                    music = SoundFactory.Melody("QueueFM", new[] { 0, 4, 7, 12, 7, 4, 2, 5, 9, 5, 2, -1, 0, -100, 0, -100 }, 0.5f, 0.5f),
                    voices = news.Length > 0 ? news : jingles,
                    jingles = jingles,
                    lines = new[] { "«Очередь FM» — мы стоим вместе с вами" },
                });
            }
            list.AddRange(new[]
            {
                new Station
                {
                    name = "Радио «Ожидание FM»",
                    shortName = "101.7 ОЖИДАНИЕ",
                    music = SoundFactory.Melody("Lullaby", new[] { 0, 4, 7, 4, 2, 5, 9, 5, -3, 0, 4, 0, -5, -1, 2, -1 }, 0.42f, 1f),
                    lines = new[]
                    {
                        "«...а у нас в студии снова никто не торопится...»",
                        "«Звонок в эфир: я уже третий час стою, передайте привет маме!»",
                        "«Следующая песня — для тех, кто в пробке. То есть для всех.»",
                    },
                },
                new Station
                {
                    name = "Топливо-Инфо",
                    shortName = "95.2 ТОПЛИВО",
                    lines = new[]
                    {
                        "«Ситуация с топливом стабильная. Очередей нет.»",
                        "«Эксперты: стоять в очереди полезно для нервной системы.»",
                        "«Бензин есть. Просто не везде и не всем.»",
                        "«Напоминаем: паниковать не нужно. Нужно ждать.»",
                        "«Цены на топливо остаются неизменно растущими.»",
                    },
                },
                new Station
                {
                    name = "Шансон у колонки",
                    shortName = "92.0 ШАНСОН",
                    music = SoundFactory.Melody("Chanson", new[] { -3, 0, 4, 0, -3, 0, 5, 4, -5, -1, 2, -1, -3, -100, -3, -100 }, 0.3f, 0.3f),
                    lines = new[]
                    {
                        "«~ Ой, колонка ты моя, сорок литров... ~»",
                        "«~ Не спеши, водитель, бензин уже в пути... ~»",
                    },
                },
            });
            stations = list.ToArray();
        }

        int raceStation; // номер станции с треками (0 — треков нет)

        /// <summary>Выпуски из Resources/RadioNews (перемешаны) и отдельно заставки (jingle_*).</summary>
        static AudioClip[] LoadNews(out AudioClip[] jingles)
        {
            var all = Resources.LoadAll<AudioClip>(NewsFolder);
            var news = new System.Collections.Generic.List<AudioClip>();
            var jl = new System.Collections.Generic.List<AudioClip>();
            foreach (var c in all) (Key(c).StartsWith("jingle") ? jl : news).Add(c);
            for (int i = news.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (news[i], news[j]) = (news[j], news[i]);
            }
            jingles = jl.ToArray();
            return news.ToArray();
        }

        /// <summary>«03_news_queue» → «news_queue».</summary>
        static string Key(AudioClip c)
        {
            var n = c.name;
            int u = n.IndexOf('_');
            return u >= 0 && u < 4 && char.IsDigit(n[0]) ? n.Substring(u + 1) : n;
        }

        /// <summary>«Очередь FM»: следующий выпуск (каждый четвёртый — заставка станции), музыка на это время тише.</summary>
        void PlayVoice(Station st)
        {
            AudioClip clip;
            if (st.jingles.Length > 0 && (sinceJingle >= 3 || voiceIndex == 0))
            {
                clip = st.jingles[Random.Range(0, st.jingles.Length)];
                sinceJingle = 0;
            }
            else
            {
                clip = st.voices[voiceIndex % st.voices.Length];
                sinceJingle++;
            }
            voiceIndex++;
            voice.clip = clip;
            voice.Play();
            CurrentLine = NewsText.TryGetValue(Key(clip), out var text) ? text : st.lines[0];
        }

        /// <summary>Включить «Гонка FM» (если треки положены в папку). Вызывается на старте гонки.</summary>
        public void TuneRace()
        {
            if (raceStation == 0 || current == raceStation) return;
            current = raceStation - 1;
            Next(quiet: true);
        }

        void PlayTrack(Station st)
        {
            var clip = st.playlist[st.track % st.playlist.Length];
            music.clip = clip;
            music.loop = false;
            music.volume = 0.4f;
            music.Play();
            CurrentLine = "Сейчас играет: " + clip.name;
        }

        void Update()
        {
            if (GameInput.RadioPressed) Next();
            if (display != null) display.text = current == 0 ? "--:--" : stations[current - 1].shortName;

            if (current == 0) return;
            var station = stations[current - 1];
            if (station.playlist != null)
            {
                // Трек кончился — следующий (во время паузы не переключаем)
                if (!music.isPlaying && Time.timeScale > 0f && !AudioListener.pause)
                {
                    station.track++;
                    PlayTrack(station);
                }
                return;
            }
            if (station.voices != null)
            {
                // Дикторы: пока говорят — музыка тише; замолчали — пауза 4–8 с и следующий выпуск
                bool talking = voice.isPlaying;
                music.volume = Mathf.MoveTowards(music.volume, talking ? 0.035f : 0.12f, Time.deltaTime * 0.3f);
                if (!talking && Time.timeScale > 0f && !AudioListener.pause)
                {
                    voiceGap -= Time.deltaTime;
                    if (voiceGap <= 0f)
                    {
                        voiceGap = Random.Range(4f, 8f);
                        PlayVoice(station);
                    }
                }
                return;
            }
            lineTimer += Time.deltaTime;
            if (lineTimer > 10f)
            {
                lineTimer = 0f;
                var lines = stations[current - 1].lines;
                lineIndex = (lineIndex + 1) % lines.Length;
                CurrentLine = lines[lineIndex];
            }
        }

        void Next() => Next(false);

        void Next(bool quiet)
        {
            // В гонке R листает свои треки, а не станции
            if (tracksOnly && current == raceStation && !quiet)
            {
                var playing = stations[current - 1];
                playing.track++;
                fx.PlayOneShot(SoundFactory.Tick);
                music.Stop();
                PlayTrack(playing);
                return;
            }
            current = (current + 1) % (stations.Length + 1);
            if (!quiet)
            {
                fx.PlayOneShot(SoundFactory.Noise);
                GameManager.Instance.OnRadioSwitched();
            }
            music.Stop();
            voice.Stop();
            CurrentLine = null;
            if (current == 0) return;

            var st = stations[current - 1];
            if (st.playlist != null)
            {
                PlayTrack(st);
                return;
            }
            music.loop = true;
            music.volume = 0.12f;
            lineIndex = Random.Range(0, st.lines.Length);
            CurrentLine = st.lines[lineIndex];
            lineTimer = 0f;
            voiceGap = 1.2f; // включили «Очередь FM» — диктор заговорит почти сразу
            if (st.music != null)
            {
                music.clip = st.music;
                music.Play();
            }
        }
    }
}
