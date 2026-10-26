#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowerOfChaos.Game.Editor
{
    /// <summary>Tower > Build Windows Preview: a Windows x64 player of the Phase-0 slice, to look at the game on a PC (output in git-ignored Builds/).</summary>
    public static class PreviewBuild
    {
        private const string ScenePath = "Assets/_Project/Scenes/Slice.unity";
        private const string OutputPath = "Builds/Windows/TowerOfChaos.exe";

        [MenuItem("Tower/Build Windows Preview")]
        public static void BuildWindows()
        {
            // TowerBootstrap creates the camera, light, hero and HUD at runtime. The scene only holds a disabled cube:
            // the runtime primitives use the default material, whose shader the build strips unless something references it.
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var keep = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keep.name = "DefaultMaterialReference";
            keep.SetActive(false);
            EditorSceneManager.SaveScene(scene, ScenePath);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
                throw new System.Exception("Windows preview build failed: " + summary.totalErrors + " errors");
            Debug.Log("Windows preview build OK: " + OutputPath + " (" + (summary.totalSize / (1024 * 1024)) + " MB)");
        }
    }
}
#endif
