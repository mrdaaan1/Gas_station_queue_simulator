using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Стрельнул сигаретку у водителя — а она размером с бейсбольную биту. Персонаж держит её двумя руками
    /// у рта, кончик тлеет и дымит, затяжки раздувают огонёк. Догорает примерно за полторы минуты.
    /// </summary>
    public class GiantCigarette : MonoBehaviour
    {
        const float FilterLength = 0.28f;
        const float PaperLength = 0.95f;
        const float Radius = 0.075f;
        const float BurnSeconds = 90f;

        public bool Finished => paperLeft <= 0.001f;

        HumanRig rig;
        Transform root, paper, ember, ash;
        Smoke smoke;
        float paperLeft = PaperLength;
        float puffTimer = 2f, puffGlow;

        static readonly Color Paper = Shapes.Hex("#f4f1ea");
        static readonly Color Filter = Shapes.Hex("#d9822b");
        static readonly Color Ember = Shapes.Hex("#ff4a1c");
        static readonly Color Ash = Shapes.Hex("#8c8a86");

        public void Init(HumanRig humanRig, Transform worldRoot)
        {
            rig = humanRig;
            rig.HoldingBig = true;
            // От губ вперёд и чуть вверх. Ось сигареты — локальная Y группы.
            root = Shapes.Group("GiantCigarette", rig.body, new Vector3(0f, 1.66f, 0.13f), new Vector3(72f, 0f, 0f));
            Shapes.Make(PrimitiveType.Cylinder, root, new Vector3(0f, FilterLength / 2f, 0f),
                new Vector3(Radius * 2f, FilterLength / 2f, Radius * 2f), Filter, name: "Filter");
            // Золотое колечко между фильтром и бумагой — для солидности
            Shapes.Make(PrimitiveType.Cylinder, root, new Vector3(0f, FilterLength, 0f),
                new Vector3(Radius * 2.04f, 0.012f, Radius * 2.04f), Shapes.Hex("#d4b24a"), name: "Ring");
            paper = Shapes.Make(PrimitiveType.Cylinder, root, Vector3.zero, Vector3.one, Paper, name: "Paper").transform;
            ash = Shapes.Make(PrimitiveType.Cylinder, root, Vector3.zero, new Vector3(Radius * 1.9f, 0.02f, Radius * 1.9f), Ash, name: "Ash").transform;
            ember = Shapes.Make(PrimitiveType.Sphere, root, Vector3.zero, Vector3.one * Radius * 1.7f, Ember, name: "Ember").transform;
            smoke = gameObject.AddComponent<Smoke>();
            smoke.Init(ember, Vector3.zero, worldRoot);
            smoke.sizeScale = 0.35f;
            smoke.intensity = 0.15f;
            Layout();
        }

        void Layout()
        {
            float y0 = FilterLength + 0.012f;
            paper.localPosition = new Vector3(0f, y0 + paperLeft / 2f, 0f);
            paper.localScale = new Vector3(Radius * 2f, Mathf.Max(0.001f, paperLeft / 2f), Radius * 2f);
            paper.gameObject.SetActive(paperLeft > 0.005f);
            float tip = y0 + paperLeft;
            ash.localPosition = new Vector3(0f, tip + 0.015f, 0f);
            ember.localPosition = new Vector3(0f, tip + 0.03f, 0f);
            float glow = 1f + puffGlow * 0.6f;
            ember.localScale = Vector3.one * Radius * 1.7f * glow;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || root == null) return;

            // Затяжка: огонёк раздувается, дым гуще, тлеет быстрее
            puffTimer -= dt;
            if (puffTimer <= 0f)
            {
                puffTimer = Random.Range(4f, 7f);
                puffGlow = 1f;
            }
            puffGlow = Mathf.MoveTowards(puffGlow, 0f, dt * 0.8f);
            smoke.intensity = 0.15f + puffGlow * 0.35f;

            paperLeft = Mathf.Max(0f, paperLeft - dt * PaperLength / BurnSeconds * (1f + puffGlow * 2f));
            Layout();
            rig.HoldingBig = true;
        }

        /// <summary>Выбросить (докурил или сел в машину).</summary>
        public void Throw()
        {
            if (root != null) Destroy(root.gameObject);
            Destroy(this);
            Destroy(smoke);
        }

        void OnDestroy()
        {
            if (rig != null) rig.HoldingBig = false;
            if (root != null) Destroy(root.gameObject);
        }
    }
}
