#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace TowerOfChaos.Game.Editor
{
    /// <summary>Tower > Apply Project Settings: Android build settings required by SPEC 25.3 (ARM64 only, IL2CPP, high stripping).</summary>
    public static class ProjectSetup
    {
        [MenuItem("Tower/Apply Project Settings (Android)")]
        public static void Apply()
        {
            var android = NamedBuildTarget.Android;
            PlayerSettings.companyName = "TowerOfChaos";
            PlayerSettings.productName = "Tower of Chaos";
            PlayerSettings.SetApplicationIdentifier(android, "com.towerofchaos.game");
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetManagedStrippingLevel(android, ManagedStrippingLevel.High);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            Debug.Log("Tower of Chaos: Android settings applied (IL2CPP, ARM64, Managed Stripping High, landscape).");
        }
    }
}
#endif
