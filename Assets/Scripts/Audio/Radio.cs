using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Тупое радио в машине. R — следующая станция.
    /// Свои треки (mp3/ogg/wav) кладутся в Assets/Resources/RaceMusic — появляется станция «Гонка FM»:
    /// треки по кругу в случайном порядке. В гонке она включается сама на зелёный.
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
        }

        const string MusicFolder = "RaceMusic";

        Station[] stations;
        int current; // 0 — выключено
        int lineIndex;
        float lineTimer;
        AudioSource music;
        AudioSource fx;

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
            current = (current + 1) % (stations.Length + 1);
            if (!quiet)
            {
                fx.PlayOneShot(SoundFactory.Noise);
                GameManager.Instance.OnRadioSwitched();
            }
            music.Stop();
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
            if (st.music != null)
            {
                music.clip = st.music;
                music.Play();
            }
        }
    }
}
