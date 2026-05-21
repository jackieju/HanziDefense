#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HanziZombieDefense.Editor
{
    public static class GltfAnimationExtractor
    {
        [MenuItem("Tools/Hanzi Zombie Defense/Extract GLB Animations")]
        public static void ExtractAnimations()
        {
            string glbPath = "Assets/_Project/Art/Models/Animated-Zombie_by_get3dmodels.glb";
            var importer = AssetImporter.GetAtPath(glbPath);
            
            if (importer == null)
            {
                glbPath = "Assets/_Project/Art/Models/Animated-Zombie_by_get3dmodels 1.glb";
                importer = AssetImporter.GetAtPath(glbPath);
            }

            if (importer == null)
            {
                Debug.LogError("[GltfAnimationExtractor] Could not find GLB file.");
                return;
            }

            var allAssets = AssetDatabase.LoadAllAssetsAtPath(glbPath);
            string outputDir = "Assets/_Project/Art/Animations";
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            int clipCount = 0;
            foreach (var asset in allAssets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
                {
                    var newClip = Object.Instantiate(clip);
                    string safeName = clip.name.Replace("|", "_").Replace("/", "_").Replace("\\", "_").Replace(":", "_");
                    newClip.name = safeName;
                    string path = $"{outputDir}/{safeName}.anim";
                    AssetDatabase.CreateAsset(newClip, path);
                    clipCount++;
                    Debug.Log($"[GltfAnimationExtractor] Extracted: {clip.name} -> {path}");
                }
            }

            if (clipCount == 0)
            {
                Debug.LogWarning("[GltfAnimationExtractor] No AnimationClips found in the GLB. Listing all sub-assets:");
                foreach (var asset in allAssets)
                {
                    Debug.Log($"  - {asset.GetType().Name}: {asset.name}");
                }
            }
            else
            {
                Debug.Log($"[GltfAnimationExtractor] Done. Extracted {clipCount} clips to {outputDir}/");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Hanzi Zombie Defense/Create Zombie Animator Controller")]
        public static void CreateAnimatorController()
        {
            string animDir = "Assets/_Project/Art/Animations";
            string controllerPath = "Assets/_Project/Art/Animations/ZombieAnimator.controller";

            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Spawn", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;

            AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Zombie_ZombieIdle.anim");
            AnimationClip walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Zombie_ZombieWalk.anim");
            AnimationClip attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Zombie_ZombieBite.anim");
            AnimationClip runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Zombie_ZombieRun.anim");
            AnimationClip crawlClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Zombie_ZombieCrawl.anim");

            if (idleClip == null) idleClip = walkClip;

            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;

            var walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walkClip != null ? walkClip : runClip;

            var attackState = rootStateMachine.AddState("Attack");
            attackState.motion = attackClip;

            var dieState = rootStateMachine.AddState("Die");
            dieState.motion = crawlClip;

            rootStateMachine.defaultState = idleState;

            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsWalking");
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.1f;

            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsWalking");
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.1f;

            var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
            anyToAttack.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Attack");
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.1f;

            var attackToIdle = attackState.AddTransition(idleState);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 0.9f;
            attackToIdle.duration = 0.1f;

            var anyToDie = rootStateMachine.AddAnyStateTransition(dieState);
            anyToDie.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Die");
            anyToDie.hasExitTime = false;
            anyToDie.duration = 0.1f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GltfAnimationExtractor] Created Animator Controller at {controllerPath}");
        }
    }
}
#endif
