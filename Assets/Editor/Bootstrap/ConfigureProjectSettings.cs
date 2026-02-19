using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoboterLego.Editor.Bootstrap
{
    public static class ConfigureProjectSettings
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string AndroidAppId = "com.roboterlego.toddlerbot";

        [MenuItem("RoboterLego/Bootstrap/Configure Project Settings")]
        public static void ApplyForAndroidMenu()
        {
            ApplyForAndroid();
            Debug.Log("Project settings configured for Android.");
        }

        public static void ApplyForAndroid()
        {
            if (!File.Exists(MainScenePath))
            {
                EnsureScenesFolder();
            }

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AndroidAppId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            bool needsSceneReset = EditorBuildSettings.scenes.Length != 1
                || EditorBuildSettings.scenes[0].path != MainScenePath
                || !EditorBuildSettings.scenes[0].enabled;
            if (needsSceneReset)
            {
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(MainScenePath, true)
                };
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureScenesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
        }
    }
}
