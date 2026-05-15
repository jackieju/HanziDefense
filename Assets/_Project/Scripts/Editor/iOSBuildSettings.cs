#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace HanziZombieDefense.Editor
{
    public static class iOSBuildSettings
    {
        private const string BundleId = "com.hanzizombiedefense.game";

        [MenuItem("Tools/Hanzi Zombie Defense/Apply iOS Settings")]
        public static void ApplyIOSSettings()
        {
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.useOnDemandResources = false;
            PlayerSettings.accelerometerFrequency = 0;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.statusBarHidden = true;

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, BundleId);

            PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.iOS, ApiCompatibilityLevel.NET_Standard_2_0);

#if UNITY_2021_2_OR_NEWER
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.iOS, ApiCompatibilityLevel.NET_Standard);
#endif

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[iOSBuildSettings] iOS settings applied. Bundle: " + BundleId);
        }
    }
}
#endif
