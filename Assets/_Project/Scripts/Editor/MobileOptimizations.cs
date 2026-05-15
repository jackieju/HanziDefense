#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace HanziZombieDefense.Editor
{
    public static class MobileOptimizations
    {
        [MenuItem("Tools/Hanzi Zombie Defense/Apply Mobile Optimizations")]
        public static void ApplyMobileOptimizations()
        {
            int qualityCount = QualitySettings.names.Length;
            for (int i = 0; i < qualityCount; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.shadowDistance = 20f;
                QualitySettings.shadowResolution = ShadowResolution.Low;
                QualitySettings.shadowCascades = 1;
                QualitySettings.pixelLightCount = 1;
                QualitySettings.particleRaycastBudget = 64;
                QualitySettings.softParticles = false;
                QualitySettings.realtimeReflectionProbes = false;
                QualitySettings.billboardsFaceCameraPosition = false;
                QualitySettings.vSyncCount = 0;
                QualitySettings.antiAliasing = 2;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            }

            QualitySettings.SetQualityLevel(2, true);

            PlayerSettings.MTRendering = true;
            PlayerSettings.graphicsJobs = false;

            AssetDatabase.SaveAssets();
            Debug.Log("[MobileOptimizations] Mobile quality settings applied across " + qualityCount + " levels.");
        }
    }
}
#endif
