using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Сборка игры одной кнопкой: меню Gas Queue → «Собрать для Mac» / «Собрать для Windows».
/// Готовая игра появляется в папке Builds рядом с Assets (в Git она не попадает).
/// </summary>
public static class GameBuilder
{
    const string ScenePath = "Assets/Scenes/Prototype.unity";
    const string ProductName = "Симулятор очереди на заправку";

    [MenuItem("Gas Queue/Собрать для Mac (.app)")]
    static void BuildMac() => Build(BuildTarget.StandaloneOSX, "Builds/Mac/GasQueue.app", "Mac");

    [MenuItem("Gas Queue/Собрать для Windows (.exe)")]
    static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/GasQueue.exe", "Windows");

    /// <summary>Сборка из Терминала (Tools/build_mac.sh): Unity без окна собирает игру и закрывается.</summary>
    public static void BuildMacBatch()
    {
        bool ok = Build(BuildTarget.StandaloneOSX, "Builds/Mac/GasQueue.app", "Mac");
        EditorApplication.Exit(ok ? 0 : 1);
    }

    const string IconPath = "Assets/Icons/AppIcon.png";

    /// <summary>Иконка приложения (Tools/Icon/make_icon.py) — для Mac и Windows.</summary>
    public static void ApplyIcon()
    {
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon != null) PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
    }

    /// <summary>
    /// Одна сборка и для Mac на Apple Silicon (M1–M4), и для старых Intel.
    /// Через рефлексию: модуль сборки для Mac есть не у всех версий Unity в одном и том же виде.
    /// </summary>
    static void MakeUniversalMac()
    {
        try
        {
            var settings = FindType("UnityEditor.OSXStandalone.UserBuildSettings");
            var arch = FindType("UnityEditor.Build.OSArchitecture");
            var prop = settings?.GetProperty("architecture", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (prop == null || arch == null) return;
            prop.SetValue(null, System.Enum.Parse(arch, "x64ARM64"));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Gas Queue] Не удалось включить универсальную сборку (Intel + Apple Silicon): " + e.Message);
        }
    }

    static System.Type FindType(string fullName)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(fullName, false);
            if (t != null) return t;
        }
        return null;
    }

    static bool Build(BuildTarget target, string output, string platform)
    {
        if (!File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Сборка", "Нет сцены " + ScenePath + ". Сначала: Gas Queue → Create Prototype Scene.", "OK");
            return false;
        }
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
        {
            EditorUtility.DisplayDialog("Сборка",
                "В Unity не установлен модуль сборки для " + platform + ".\n\n" +
                "Unity Hub → Installs → у вашей версии ⚙ → Add modules → «" + platform + " Build Support (Mono)». " +
                "После установки перезапустите Unity.", "OK");
            return false;
        }

        BuildMaterials.Generate(); // материалы для шейдеров — иначе в сборке всё розовое
        ApplyIcon();
        PlayerSettings.productName = ProductName;
        if (string.IsNullOrEmpty(PlayerSettings.companyName) || PlayerSettings.companyName == "DefaultCompany")
            PlayerSettings.companyName = "GasQueue";

        if (target == BuildTarget.StandaloneOSX) MakeUniversalMac();
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = output,
            target = target,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Gas Queue] Сборка для {platform} готова: {output} ({report.summary.totalSize / (1024 * 1024)} МБ)");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(output);
            return true;
        }
        else
        {
            EditorUtility.DisplayDialog("Сборка",
                $"Сборка для {platform} не удалась ({report.summary.result}). Красные ошибки — в окне Console, пришлите их Claude.", "OK");
            return false;
        }
    }
}
