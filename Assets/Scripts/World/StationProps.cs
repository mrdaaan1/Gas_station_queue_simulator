using UnityEngine;

namespace GasQueue
{
    /// <summary>Шлагбаум перед колонками. Опускается ровно перед игроком.</summary>
    public class Barrier : MonoBehaviour
    {
        public Transform arm;
        public float armLength = 6.8f;
        public bool IsDown { get; private set; }

        const float UpAngle = -85f;
        float angle = UpAngle;

        public void SetDown(bool down) => IsDown = down;

        /// <summary>Габарит опущенной стрелы (стрела идёт от стойки в сторону −X).</summary>
        public Obb Box => Obb.Axis(transform.position + Vector3.left * (armLength / 2f), armLength, 0.3f);

        void Update()
        {
            float target = IsDown ? 0f : UpAngle;
            angle = Mathf.MoveTowards(angle, target, 70f * Time.deltaTime);
            arm.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    /// <summary>Цены на стеле у дороги (как на настоящих заправках: 95 / 92 / ДТ / газ).</summary>
    public class PriceBoard : MonoBehaviour
    {
        public TextMesh[] rows;
        public float price92 = 61.7f;
        public float price95 = 69.7f;
        public float priceDiesel = 69.7f;
        public bool soldOut;

        public float CurrentPrice => price95;

        void Update()
        {
            if (rows == null || rows.Length < 4) return;
            rows[0].text = soldOut ? "95  --.--" : $"95  {price95:00.00}";
            rows[1].text = soldOut ? "92  --.--" : $"92  {price92:00.00}";
            rows[2].text = soldOut ? "ДТ  --.--" : $"ДТ  {priceDiesel:00.00}";
            rows[3].text = "ГАЗ 00.00";
        }

        public void RaisePrices(float amount)
        {
            price92 += amount;
            price95 += amount;
            priceDiesel += amount;
        }
    }

    /// <summary>Всегда поворачивает объект лицом к камере (для надписей над машинами).</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }

    /// <summary>Реплика над машиной, исчезает через пару секунд.</summary>
    public class SpeechBubble : MonoBehaviour
    {
        float life = 3f;
        TextMesh text;
        TextMesh shadow;

        public static void Show(Transform car, string phrase, float height)
        {
            var old = car.GetComponentInChildren<SpeechBubble>();
            if (old != null) Destroy(old.gameObject);

            var go = new GameObject("Speech");
            go.transform.SetParent(car, false);
            go.transform.localPosition = new Vector3(0, height + 0.9f, 0);
            go.AddComponent<Billboard>();
            var bubble = go.AddComponent<SpeechBubble>();
            bubble.shadow = Fonts.WorldText(go.transform, new Vector3(0.02f, -0.02f, 0.01f), phrase, Color.black, 0.035f);
            bubble.text = Fonts.WorldText(go.transform, Vector3.zero, phrase, Shapes.Hex("#ffe14d"), 0.035f);
        }

        void Update()
        {
            life -= Time.deltaTime;
            float a = Mathf.Clamp01(life / 0.5f);
            text.color = new Color(text.color.r, text.color.g, text.color.b, a);
            shadow.color = new Color(0, 0, 0, a);
            transform.localPosition += Vector3.up * Time.deltaTime * 0.15f;
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
