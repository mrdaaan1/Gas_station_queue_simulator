using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Весь мир создаёт материалы кодом, а Unity при сборке выкидывает шейдеры, на которые не ссылается
/// ни один файл — и в собранной игре всё розовое. Поэтому перед каждой сборкой (и при открытии проекта)
/// сохраняем в Resources по материалу на каждый нужный шейдер: обычный, стекло и небо.
/// Игра берёт их оттуда (Shapes.Mat, MeshFactory.Glass, CityBuilder). Плюс оставляем в сборке линейный туман.
/// </summary>
[InitializeOnLoad]
public class BuildMaterials : IPreprocessBuildWithReport
{
    const string Dir = "Assets/Resources/GasQueueGenerated";

    static BuildMaterials()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) Generate();
        };
    }

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        Generate();
        GameBuilder.ApplyIcon();
        KeepLinearFog();
        var rp = GraphicsSettings.currentRenderPipeline;
        Debug.Log("[Gas Queue] Сборка: рендер-пайплайн " + (rp != null ? rp.name : "Built-in") + ", материалы в " + Dir);
    }

    [MenuItem("Gas Queue/Обновить материалы для сборки")]
    public static void Generate()
    {
        var rp = GraphicsSettings.currentRenderPipeline;
        var def = rp != null ? rp.defaultMaterial : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
        if (def == null)
        {
            Debug.LogWarning("[Gas Queue] Не нашёл материал по умолчанию — сборка может быть розовой.");
            return;
        }
        Directory.CreateDirectory(Dir);

        Save("Base", new Material(def));

        var glass = new Material(def) { color = new Color(0.65f, 0.8f, 0.9f, 0.16f) };
        GasQueue.MeshFactory.MakeTransparent(glass);
        Save("Glass", glass);

        // Светящиеся фонари и приборы спорткаров: без этого Unity вырежет вариант шейдера с подсветкой
        var glow = new Material(def);
        glow.EnableKeyword("_EMISSION");
        if (glow.HasProperty("_EmissionColor")) glow.SetColor("_EmissionColor", new Color(0.5f, 0.1f, 0.05f));
        glow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        Save("Glow", glow);

        // Зеркала: картинка с камеры без освещения
        var unlit = Shader.Find(rp != null ? "Universal Render Pipeline/Unlit" : "Unlit/Texture");
        if (unlit != null) Save("Mirror", new Material(unlit));

        var skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader != null)
        {
            var sky = new Material(skyShader);
            if (sky.HasProperty("_SunSize")) sky.SetFloat("_SunSize", 0.04f);
            if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 1f);
            if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1.3f);
            Save("Sky", sky);
        }
        AssetDatabase.SaveAssets();
    }

    static void Save(string name, Material m)
    {
        m.name = name;
        string path = $"{Dir}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(m, path);
            return;
        }
        if (existing.shader == m.shader && existing.renderQueue == m.renderQueue && existing.IsKeywordEnabled("_EMISSION") == m.IsKeywordEnabled("_EMISSION")) return; // уже актуален
        EditorUtility.CopySerialized(m, existing);
        EditorUtility.SetDirty(existing);
    }

    /// <summary>Туман включается кодом, а не в сцене — без этого Unity вырежет его из шейдеров сборки.</summary>
    static void KeepLinearFog()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (assets == null || assets.Length == 0) return;
        var so = new SerializedObject(assets[0]);
        var strip = so.FindProperty("m_FogStripping");
        var linear = so.FindProperty("m_FogKeepLinear");
        if (strip == null || linear == null) return;
        strip.intValue = 1; // Custom
        linear.boolValue = true;
        so.ApplyModifiedProperties();
    }
}
