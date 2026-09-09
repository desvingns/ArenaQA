using System.Collections.Generic;
using System.IO;
using Arena.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arena.EditorTools
{
    [InitializeOnLoad]
    public static class ArenaSceneCreator
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string ScenePath = ScenesFolder + "/Arena.unity";
        private const string SessionKey = "Arena.SceneBootstrapped";

        static ArenaSceneCreator()
        {
            EditorApplication.delayCall += TryBootstrap;
        }

        private static void TryBootstrap()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += TryBootstrap;
                return;
            }

            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (File.Exists(ScenePath))
            {
                if (SceneManager.GetActiveScene().path != ScenePath)
                {
                    EditorSceneManager.OpenScene(ScenePath);
                }

                return;
            }

            CreateDemoScene();
        }

        [MenuItem("Arena/Create Demo Scene")]
        public static void CreateDemoScene()
        {
            if (!Directory.Exists(ScenesFolder))
            {
                Directory.CreateDirectory(ScenesFolder);
                AssetDatabase.Refresh();
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject root = new GameObject("Arena");
            root.AddComponent<ArenaGame>();

            GameObject light = GameObject.Find("Directional Light");
            if (light != null)
            {
                light.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();

            Debug.Log($"[Arena] Сцена готова: {ScenePath}. Нажми Play.");
        }

        private static void RegisterInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == ScenePath)
                {
                    return;
                }
            }

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
