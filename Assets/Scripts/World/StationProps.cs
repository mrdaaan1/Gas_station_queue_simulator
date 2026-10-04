using UnityEngine;

namespace GasQueue
{
    /// <summary>Шлагбаум перед колонкой. Опускается ровно перед игроком.</summary>
    public class Barrier : MonoBehaviour
    {
        public Transform arm;
        public bool IsDown { get; private set; }

        const float UpAngle = -85f;
        float angle = UpAngle;

        public void SetDown(bool down) => IsDown = down;

        void Update()
        {
            float target = IsDown ? 0f : UpAngle;
            angle = Mathf.MoveTowards(angle, target, 70f * Time.deltaTime);
            arm.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    /// <summary>Табло с ценами на топливо.</summary>
    public class PriceBoard : MonoBehaviour
    {
        public TextMesh text;
        public float price92 = 57.4f;
        public float price95 = 62.9f;
        public bool soldOut;

        public float CurrentPrice => price95;

        void Update()
        {
            text.text = soldOut
                ? "АИ-92   НЕТ\nАИ-95   НЕТ\nДТ      НЕТ"
                : $"АИ-92  {price92:0.0}\nАИ-95  {price95:0.0}\nДТ      НЕТ";
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
