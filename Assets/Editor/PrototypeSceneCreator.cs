using System.IO;
using System.Linq;
using GasQueue;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// При первом открытии проекта сам создаёт сцену Assets/Scenes/Prototype.unity,
/// кладёт в неё объект "Game" и открывает её. Остаётся только нажать Play.
/// Пересоздать вручную: меню Gas Queue → Create Prototype Scene.
/// </summary>
[InitializeOnLoad]
public static class PrototypeSceneCreator
{
    const string ScenePath = "Assets/Scenes/Prototype.unity";
    const string AutoCreatedKey = "GasQueue.PrototypeSceneAutoCreated";

    static PrototypeSceneCreator()
    {
        EditorApplication.delayCall += () =>
        {
            UsePrototypeForPlay();
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(ScenePath) || SessionState.GetBool(AutoCreatedKey, false)) return;
            SessionState.SetBool(AutoCreatedKey, true);
            CreateScene();
        };
    }

    /// <summary>
    /// Play всегда запускает игру со сцены Prototype — даже если в редакторе случайно открыта
    /// пустая сцена «Untitled» (иначе видно только небо и землю, а мира нет).
    /// </summary>
    static void UsePrototypeForPlay()
    {
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        if (scene != null) EditorSceneManager.playModeStartScene = scene;
    }

    [MenuItem("Gas Queue/Create Prototype Scene")]
    public static void CreateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var game = new GameObject("Game");
        game.AddComponent<GameSettings>();
        game.AddComponent<GameBootstrap>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        UsePrototypeForPlay();
        AddToBuildSettings();
        Debug.Log("[Gas Queue] Сцена создана: " + ScenePath + ". Нажмите Play ▶");
    }

    [MenuItem("Gas Queue/Open Prototype Scene")]
    public static void OpenScene()
    {
        if (!File.Exists(ScenePath))
        {
            CreateScene();
            return;
        }
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(ScenePath);
    }

    static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == ScenePath)) return;
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
