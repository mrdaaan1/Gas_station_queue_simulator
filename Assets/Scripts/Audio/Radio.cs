using UnityEngine;

namespace GasQueue
{
    /// <summary>Тупое радио в машине. R — следующая станция.</summary>
    public class Radio : MonoBehaviour
    {
        class Station
        {
            public string name;
            public string[] lines;
            public AudioClip music;
        }

        Station[] stations;
        int current; // 0 — выключено
        int lineIndex;
        float lineTimer;
        AudioSource music;
        AudioSource fx;

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

            stations = new[]
            {
                new Station
                {
                    name = "Радио «Ожидание FM»",
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
                    music = SoundFactory.Melody("Chanson", new[] { -3, 0, 4, 0, -3, 0, 5, 4, -5, -1, 2, -1, -3, -100, -3, -100 }, 0.3f, 0.3f),
                    lines = new[]
                    {
                        "«~ Ой, колонка ты моя, сорок литров... ~»",
                        "«~ Не спеши, водитель, бензин уже в пути... ~»",
                    },
                },
            };
        }

        void Update()
        {
            if (GameInput.RadioPressed) Next();

            if (current == 0) return;
            lineTimer += Time.deltaTime;
            if (lineTimer > 10f)
            {
                lineTimer = 0f;
                var lines = stations[current - 1].lines;
                lineIndex = (lineIndex + 1) % lines.Length;
                CurrentLine = lines[lineIndex];
            }
        }

        void Next()
        {
            current = (current + 1) % (stations.Length + 1);
            fx.PlayOneShot(SoundFactory.Noise);
            GameManager.Instance.OnRadioSwitched();
            music.Stop();
            CurrentLine = null;
            if (current == 0) return;

            var st = stations[current - 1];
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
