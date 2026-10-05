using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GasQueue
{
    /// <summary>
    /// Временная отладка белых пятен в салоне спорткара (клавиша K): по очереди отключает
    /// разные вещи, чтобы по одному запуску понять, что именно даёт белый цвет.
    /// </summary>
    public class CabinDebug : MonoBehaviour
    {
        static readonly string[] Names =
        {
            "0 — как обычно",
            "1 — салон не принимает тени",
            "2 — у солнца выключены тени",
            "3 — материалы салона как у рук водителя",
            "4 — без глянца и отражений неба",
            "5 — зеркала выключены",
        };

        int mode;
        readonly Dictionary<Renderer, Material[]> original = new Dictionary<Renderer, Material[]>();
        readonly Dictionary<Renderer, bool> receive = new Dictionary<Renderer, bool>();
        readonly Dictionary<Material, Material> matte = new Dictionary<Material, Material>();

        void Start()
        {
            foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
            {
                original[r] = r.sharedMaterials;
                receive[r] = r.receiveShadows;
            }
        }

        void Update()
        {
            if (!GameInput.DebugCabinPressed) return;
            mode = (mode + 1) % Names.Length;
            Apply();
            if (GameManager.Instance != null) GameManager.Instance.ShowMessage("Отладка салона (K): " + Names[mode], 5f);
        }

        void Apply()
        {
            var sun = RenderSettings.sun;
            if (sun != null) sun.shadows = mode == 2 ? LightShadows.None : LightShadows.Soft;
            var mirrors = GetComponent<CarMirrors>();
            if (mirrors != null) mirrors.enabled = mode != 5;
            foreach (var cam in GetComponentsInChildren<Camera>(true))
                if (cam.targetTexture != null && mode == 5) cam.enabled = false;

            foreach (var kv in original)
            {
                var r = kv.Key;
                if (r == null) continue;
                r.receiveShadows = mode == 1 ? false : receive[r];
                var mats = (Material[])kv.Value.Clone();
                if (mode == 3 || mode == 4)
                    for (int i = 0; i < mats.Length; i++)
                        if (mats[i] != null && mats[i].name.StartsWith("Car_") && mats[i].renderQueue < 2500 && !mats[i].IsKeywordEnabled("_EMISSION"))
                            mats[i] = mode == 3 ? Shapes.Mat(mats[i].color) : Matte(mats[i]);
                r.sharedMaterials = mats;
            }
        }

        Material Matte(Material m)
        {
            if (matte.TryGetValue(m, out var copy)) return copy;
            copy = new Material(m) { name = m.name + "_Matte" };
            if (copy.HasProperty("_Glossiness")) copy.SetFloat("_Glossiness", 0f);
            if (copy.HasProperty("_Metallic")) copy.SetFloat("_Metallic", 0f);
            if (copy.HasProperty("_SpecularHighlights")) copy.SetFloat("_SpecularHighlights", 0f);
            if (copy.HasProperty("_GlossyReflections")) copy.SetFloat("_GlossyReflections", 0f);
            copy.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            copy.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            matte[m] = copy;
            return copy;
        }
    }
}
