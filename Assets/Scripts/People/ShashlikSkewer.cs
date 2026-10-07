using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Шампур шашлыка (или люля-кебаба) в руках. Человечек время от времени подносит его ко рту
    /// и откусывает — кусок пропадает. Съел всё — шампур выбрасывается. Работает и у игрока, и у водителей NPC.
    /// У водителей — обычный шампур в правой руке. Игроку Ашот кладёт «самый лучший» — в 4 раза больше:
    /// двухметровое бревно мяса, которое держат двумя руками поперёк груди, а кусать приходится как кукурузу —
    /// шампур ездит вбок, чтобы очередной кусок оказался у рта. Каждый гигантский кусок — на два укуса.
    /// </summary>
    public class ShashlikSkewer : MonoBehaviour
    {
        public const float GiantScale = 4f;

        public bool Finished => left <= 0 && biteAnim <= 0f;
        public bool Giant { get; private set; }
        public bool Lula { get; private set; }
        /// <summary>Пауза между укусами, с.</summary>
        public float BiteInterval = 2.4f;

        HumanRig rig;
        Transform root;
        readonly List<Transform> chunks = new List<Transform>();
        readonly List<float> chunkX = new List<float>();
        int left;
        int bitesOnChunk; // гигантский кусок съедается в два укуса
        float biteTimer = 1.2f, biteAnim;
        bool bitten;
        float centerX;

        static readonly Color Meat = Shapes.Hex("#7a3b1e");
        static readonly Color MeatDark = Shapes.Hex("#5a2a14");
        static readonly Color LulaMeat = Shapes.Hex("#8a4a26");
        static readonly Color Grill = Shapes.Hex("#3a1a0c");
        static readonly Color Onion = Shapes.Hex("#efe6f2");
        static readonly Color Herbs = Shapes.Hex("#3f7a2a");
        static readonly Color Steel = Shapes.Hex("#b8bcc2");

        // Гигантский шампур: у груди и у рта (в системе тела)
        static readonly Vector3 GiantHold = new Vector3(0f, 1.18f, 0.5f);
        static readonly Vector3 GiantMouth = new Vector3(0f, 1.62f, 0.46f);

        public void Init(HumanRig humanRig, bool giant = false, bool lula = false)
        {
            rig = humanRig;
            Giant = giant;
            Lula = lula;
            float k = giant ? GiantScale : 1f;
            if (giant)
            {
                rig.HoldingGiant = true;
                root = Shapes.Group("GiantShashlik", rig.body, GiantHold);
                BiteInterval = 2.6f;
            }
            else
            {
                rig.HoldingSkewer = true;
                // Шампур — поперёк кисти (вдоль локальной X руки), кончиком к середине тела: ко рту подносится «как кукуруза»
                root = Shapes.Group("Shashlik", rig.armR, new Vector3(0f, -0.62f, 0.06f));
            }

            // Шампур с кольцом-ручкой на +X, мясо — к −X
            float steel = 0.012f * (giant ? 3f : 1f);
            Shapes.Box(root, new Vector3(-0.27f * k, 0f, 0f), new Vector3(0.62f * k, steel, steel * (lula ? 2.5f : 1f)), Steel, name: "Skewer");
            Shapes.Box(root, new Vector3(0.05f * k, 0f, 0f), new Vector3(0.05f * k, 0.03f * k, 0.03f * k), Steel, name: "Ring");

            if (lula)
            {
                // Люля: три длинные колбаски из фарша с полосками от решётки и зеленью
                for (int i = 0; i < 3; i++)
                {
                    float x = -0.13f * k - i * 0.165f * k;
                    var chunk = Shapes.Group("Lula", root, new Vector3(x, 0f, 0f));
                    Shapes.Make(PrimitiveType.Capsule, chunk, Vector3.zero, new Vector3(0.06f * k, 0.075f * k, 0.06f * k), LulaMeat, new Vector3(0f, 0f, 90f));
                    for (int g = -1; g <= 1; g++)
                        Shapes.Box(chunk, new Vector3(g * 0.04f * k, 0.028f * k, 0f), new Vector3(0.008f * k, 0.008f * k, 0.05f * k), Grill, new Vector3(0f, 25f, 0f), name: "Grill");
                    Shapes.Box(chunk, new Vector3(0.02f * k, 0.03f * k, 0.012f * k), new Vector3(0.02f * k, 0.006f * k, 0.012f * k), Herbs, name: "Herb");
                    chunks.Add(chunk);
                    chunkX.Add(x);
                }
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    float x = -0.1f * k - i * 0.1f * k;
                    var chunk = Shapes.Group("Piece", root, new Vector3(x, 0f, 0f));
                    Shapes.Box(chunk, Vector3.zero, new Vector3(0.07f * k, 0.075f * k, 0.07f * k), i % 2 == 0 ? Meat : MeatDark, new Vector3(0f, 0f, 20f * i));
                    Shapes.Box(chunk, new Vector3(0.045f * k, 0f, 0f), new Vector3(0.012f * k, 0.06f * k, 0.06f * k), Onion, name: "Onion");
                    chunks.Add(chunk);
                    chunkX.Add(x);
                }
            }
            left = chunks.Count;
            // Гигантский держим за середину мяса
            centerX = giant ? -(chunkX[0] + chunkX[chunkX.Count - 1]) / 2f : 0f;
            if (giant) root.localPosition = GiantHold + new Vector3(centerX, 0f, 0f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || root == null) return;
            float biteTime = Giant ? 1.3f : 0.9f;
            if (biteAnim > 0f)
            {
                biteAnim = Mathf.Max(0f, biteAnim - dt / biteTime);
                // Поднёс ко рту — откусил с дальнего конца, опустил
                if (!bitten && biteAnim < 0.5f && left > 0)
                {
                    bitten = true;
                    var c = chunks[left - 1];
                    if (Giant && bitesOnChunk == 0)
                    {
                        // Первый укус гигантского куска — откусил половину
                        bitesOnChunk = 1;
                        if (c != null) c.localScale = new Vector3(0.55f, 0.85f, 0.85f);
                    }
                    else
                    {
                        bitesOnChunk = 0;
                        left--;
                        if (c != null) c.gameObject.SetActive(false);
                    }
                }
            }
            else if (left > 0)
            {
                biteTimer -= dt;
                if (biteTimer <= 0f)
                {
                    biteTimer = BiteInterval * Random.Range(0.8f, 1.2f);
                    biteAnim = 1f;
                    bitten = false;
                }
            }

            float lift = Mathf.Sin(biteAnim * Mathf.PI);
            if (Giant)
            {
                // Подносим ко рту и сдвигаем вбок, чтобы крайний кусок оказался перед лицом
                int target = Mathf.Clamp(left - 1, 0, chunkX.Count - 1);
                float x = Mathf.Lerp(centerX, -chunkX[target], lift);
                root.localPosition = Vector3.Lerp(GiantHold, GiantMouth, lift) + new Vector3(x, 0f, 0f);
                rig.HoldingGiant = true;
            }
            else rig.HoldingSkewer = true;
            rig.Bite = lift;
        }

        /// <summary>Доел (или пришлось выбросить).</summary>
        public void Throw()
        {
            if (root != null) Destroy(root.gameObject);
            Destroy(this);
        }

        void OnDestroy()
        {
            if (rig != null)
            {
                rig.HoldingSkewer = false;
                rig.HoldingGiant = false;
                rig.Bite = 0f;
            }
            if (root != null) Destroy(root.gameObject);
        }
    }
}
