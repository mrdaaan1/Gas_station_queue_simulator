using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Водитель и пассажир болтают, пока стоят в очереди. Строго по очереди: один сказал, пауза, второй ответил.
    /// Во всём мире одновременно идёт только один разговор — и только рядом с игроком, чтобы его можно было прочитать.
    /// </summary>
    public class CarChatter : MonoBehaviour
    {
        // В — водитель, П — пассажир. Политика, выборы, «всё плохо» — но с подколами и без злобы.
        static readonly string[][] Dialogs =
        {
            new[] { "В:В сентябре выборы в Думу. Пойдёшь?", "П:Пойду. Там буфет хороший.", "В:На бутербродах демократия и держится." },
            new[] { "П:Бензин опять подорожал.", "В:Зато очередь бесплатная!", "П:И с видом на заправку. Красота." },
            new[] { "В:По радио сказали: ситуация\nс топливом стабильная.", "П:Стабильно нет бензина, да.", "В:Главное — стабильность." },
            new[] { "В:Кандидат обещал дороги без ям.", "П:А заправки без очередей?", "В:Это во втором сроке.", "П:Ну, есть к чему стремиться." },
            new[] { "П:Мы тут уже три часа стоим.", "В:Зато впервые за год поговорили.", "П:...Ну, тоже плюс." },
            new[] { "В:Знаешь, что у нас растёт\nбыстрее цен?", "П:Что?", "В:Очередь за бензином.", "П:Оптимист." },
            new[] { "П:Дед рассказывал, при Союзе\nтоже так стояли.", "В:Вот! А говорят, традиций нет.", "П:Скрепа, получается." },
            new[] { "В:Агитатор звонил, звал на выборы.", "П:А бензин обещал?", "В:Талон на сентябрь.", "П:Ого, это уже программа!" },
            new[] { "П:Курс опять скакнул.", "В:Не смотри на курс, смотри на небо.", "П:Небо хотя бы бесплатное.", "В:Пока что." },
            new[] { "П:Включи радио.", "В:Там опять про стабильность.", "П:Тогда выключи. Я и так спокоен." },
            new[] { "В:Жена спрашивает, когда я буду дома.", "П:Скажи: после завоза.", "В:Она спросит: какого года?" },
            new[] { "П:Вот изберём нового депутата...", "В:...и он заправится без очереди.", "П:Ну хоть кому-то хорошо." },
            new[] { "В:Зато у нас самая длинная\nочередь в городе!", "П:Хоть в чём-то первые." },
            new[] { "П:Как думаешь, бензин сегодня будет?", "В:Надежда умирает последней.", "П:А мы — в очереди." },
            new[] { "В:На участке в сентябре\nбудут пирожки давать.", "П:С капустой?", "В:С обещаниями.", "П:Сытно." },
            new[] { "П:Может, на дачу не поедем?", "В:Три часа стоим — поедем из принципа!" },
            new[] { "В:Сосед на газ перешёл.", "П:И как?", "В:Ездит мимо и машет.", "П:Предатель. Но умный." },
            new[] { "П:Говорят, зарплаты растут.", "В:Растут. Медленнее, чем эта очередь,\nно растут!", "П:Вот видишь, всё хорошо." },
            new[] { "В:Если бензина не будет,\nпойду в депутаты.", "П:Зачем?", "В:У них своя колонка." },
            new[] { "П:Это всё временно.", "В:Как и моё терпение.", "П:Держись. Пирожок хочешь?" },
        };

        static readonly Color DriverColor = Shapes.Hex("#ffe14d");
        static readonly Color PassengerColor = Shapes.Hex("#9fe6ff");

        const float LineSeconds = 3.2f;   // сколько висит реплика
        const float GapSeconds = 1.0f;    // пауза перед ответом — реплики не накладываются
        const float HearRadius = 24f;

        static CarChatter speaking;       // кто сейчас говорит (один на весь мир)
        static float nextAllowed;
        static readonly List<int> unused = new List<int>();

        CarVisual visual;
        Vehicle vehicle;
        string[] dialog;
        int line;
        float timer;
        float myCooldown;
        Transform talkingHead;
        Vector3 headBase;

        public void Init(CarVisual v) => visual = v;

        void Start()
        {
            vehicle = GetComponent<Vehicle>();
            myCooldown = Random.Range(5f, 30f);
        }

        void OnDestroy()
        {
            if (speaking == this) speaking = null;
            ResetHead();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || visual == null) return;
            myCooldown -= dt;

            if (speaking == this)
            {
                Talk(dt);
                return;
            }
            if (speaking != null || Time.time < nextAllowed || myCooldown > 0f) return;
            if (!CanTalk() || !NearListener()) return;
            if (Random.value > dt * 0.25f) return; // не все сразу: кто-нибудь заговорит в ближайшие секунды

            if (unused.Count == 0)
                for (int i = 0; i < Dialogs.Length; i++) unused.Add(i);
            int pick = unused[Random.Range(0, unused.Count)];
            unused.Remove(pick);
            dialog = Dialogs[pick];
            line = -1;
            timer = 0f;
            speaking = this;
        }

        /// <summary>Болтают, только когда стоят (в очереди или у колонки) и водитель в машине.</summary>
        bool CanTalk()
        {
            if (vehicle != null && Mathf.Abs(vehicle.Speed) > 1.5f) return false;
            return visual.driverHead != null && visual.driverHead.gameObject.activeSelf &&
                   visual.passengerHead != null && visual.passengerHead.gameObject.activeSelf;
        }

        bool NearListener()
        {
            var cam = Camera.main;
            if (cam == null) return false;
            return (cam.transform.position - transform.position).sqrMagnitude < HearRadius * HearRadius;
        }

        void Talk(float dt)
        {
            timer -= dt;
            // Говорящий слегка кивает
            if (talkingHead != null)
                talkingHead.localPosition = headBase + Vector3.up * (Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.02f * Mathf.Clamp01(timer));
            if (timer > 0f) return;

            ResetHead();
            // Уехали, водитель вышел или игрок ушёл далеко — разговор обрывается
            if (!CanTalk() || !NearListener() || ++line >= dialog.Length)
            {
                speaking = null;
                nextAllowed = Time.time + Random.Range(6f, 14f);
                myCooldown = Random.Range(60f, 120f);
                return;
            }

            string raw = dialog[line];
            bool driver = raw.StartsWith("В:");
            string text = raw.Substring(2);
            talkingHead = driver ? visual.driverHead : visual.passengerHead;
            headBase = talkingHead.localPosition;
            var offset = new Vector3(driver ? -0.55f : 0.55f, 0f, 0f);
            SpeechBubble.Show(transform, text, visual.height, offset, driver ? DriverColor : PassengerColor, LineSeconds);
            timer = LineSeconds + GapSeconds;
        }

        void ResetHead()
        {
            if (talkingHead != null) talkingHead.localPosition = headBase;
            talkingHead = null;
        }
    }
}
