using System.Collections.Generic;
using UnityEngine;

namespace GasQueue
{
    /// <summary>
    /// Шампур шашлыка в правой руке: пять кусков мяса с луком. Человечек время от времени подносит его ко рту
    /// и откусывает — кусок пропадает. Съел всё — шампур выбрасывается. Работает и у игрока, и у водителей NPC.
    /// </summary>
    public class ShashlikSkewer : MonoBehaviour
    {
        const int Pieces = 5;

        public bool Finished => left <= 0 && biteAnim <= 0f;
        /// <summary>Пауза между укусами, с.</summary>
        public float BiteInterval = 2.4f;

        HumanRig rig;
        Transform root;
        readonly List<Transform> chunks = new List<Transform>();
        int left = Pieces;
        float biteTimer = 1.2f, biteAnim;
        bool bitten;

        static readonly Color Meat = Shapes.Hex("#7a3b1e");
        static readonly Color MeatDark = Shapes.Hex("#5a2a14");
        static readonly Color Onion = Shapes.Hex("#efe6f2");
        static readonly Color Steel = Shapes.Hex("#b8bcc2");

        public void Init(HumanRig humanRig)
        {
            rig = humanRig;
            rig.HoldingSkewer = true;
            // Шампур — поперёк кисти (вдоль локальной X руки), кончиком к середине тела: ко рту подносится «как кукуруза»
            root = Shapes.Group("Shashlik", rig.armR, new Vector3(0f, -0.62f, 0.06f));
            Shapes.Box(root, new Vector3(-0.27f, 0f, 0f), new Vector3(0.62f, 0.012f, 0.012f), Steel, name: "Skewer");
            Shapes.Box(root, new Vector3(0.05f, 0f, 0f), new Vector3(0.05f, 0.03f, 0.03f), Steel, name: "Ring");
            for (int i = 0; i < Pieces; i++)
            {
                float x = -0.1f - i * 0.1f;
                var chunk = Shapes.Group("Piece", root, new Vector3(x, 0f, 0f));
                Shapes.Box(chunk, Vector3.zero, new Vector3(0.07f, 0.075f, 0.07f), i % 2 == 0 ? Meat : MeatDark, new Vector3(0f, 0f, 20f * i));
                Shapes.Box(chunk, new Vector3(0.045f, 0f, 0f), new Vector3(0.012f, 0.06f, 0.06f), Onion, name: "Onion");
                chunks.Add(chunk);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || root == null) return;
            if (biteAnim > 0f)
            {
                biteAnim = Mathf.Max(0f, biteAnim - dt / 0.9f);
                // Поднёс ко рту — откусил с дальнего конца, опустил
                if (!bitten && biteAnim < 0.5f && left > 0)
                {
                    bitten = true;
                    left--;
                    var c = chunks[left];
                    if (c != null) c.gameObject.SetActive(false);
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
            rig.HoldingSkewer = true;
            rig.Bite = Mathf.Sin(biteAnim * Mathf.PI);
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
                rig.Bite = 0f;
            }
            if (root != null) Destroy(root.gameObject);
        }
    }
}
