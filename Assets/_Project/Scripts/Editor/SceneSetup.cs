#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using HanziZombieDefense.Core;
using HanziZombieDefense.Difficulty;
using HanziZombieDefense.Hanzi.Data;
using HanziZombieDefense.Hanzi.Input;
using HanziZombieDefense.Player;
using HanziZombieDefense.Scoring;
using HanziZombieDefense.Spawning;
using HanziZombieDefense.UI.HUD;
using HanziZombieDefense.UI.Menus;
using HanziZombieDefense.UI.Writing;
using HanziZombieDefense.VFX;
using HanziZombieDefense.Zombies;

namespace HanziZombieDefense.Editor
{
    public static class SceneSetup
    {
        private const string MenuRoot          = "Tools/Hanzi Zombie Defense/Setup Scenes/";
        private const string ScenesFolder      = "Assets/_Project/Scenes";
        private const string PrefabsRoot       = "Assets/_Project/Prefabs";
        private const string ZombiePrefabsDir  = PrefabsRoot + "/Zombies";
        private const string VfxPrefabsDir     = PrefabsRoot + "/VFX";

        private const string BootScenePath     = ScenesFolder + "/Boot.unity";
        private const string MainMenuScenePath = ScenesFolder + "/MainMenu.unity";
        private const string GameplayScenePath = ScenesFolder + "/Gameplay.unity";

        private const string ZombiePrefabPath    = ZombiePrefabsDir + "/Zombie.prefab";
        private const string ExplosionPrefabPath = VfxPrefabsDir + "/Explosion.prefab";

        [MenuItem(MenuRoot + "Build Boot Scene", priority = 100)]
        public static void BuildBootScene()
        {
            EnsureDirectory(ScenesFolder);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootGo = new GameObject("BootLoader");
            bootGo.AddComponent<BootLoader>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, BootScenePath);
            Debug.Log($"[SceneSetup] Boot scene saved to {BootScenePath}");
        }

        [MenuItem(MenuRoot + "Build MainMenu Scene", priority = 101)]
        public static void BuildMainMenuScene()
        {
            EnsureDirectory(ScenesFolder);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 1f, -10f);
                cam.transform.rotation = Quaternion.identity;
                cam.backgroundColor = new Color(0.07f, 0.08f, 0.10f);
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

            BuildMainMenuUI();
            EnsureEventSystem();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            Debug.Log($"[SceneSetup] MainMenu scene saved to {MainMenuScenePath}");
        }

        [MenuItem(MenuRoot + "Build Gameplay Scene", priority = 102)]
        public static void BuildGameplayScene()
        {
            EnsureDirectory(ScenesFolder);
            EnsureDirectory(ZombiePrefabsDir);
            EnsureDirectory(VfxPrefabsDir);

            GameObject zombiePrefab = BuildZombiePrefab();
            GameObject explosionPrefab = BuildExplosionPrefab();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildEnvironment();
            var playerGo = BuildPlayerRig();
            BuildSpawningSystem(zombiePrefab);
            var comboTracker = BuildSystems(explosionPrefab);
            BuildDrawingCanvas();
            BuildHudCanvas(playerGo, comboTracker);
            BuildPauseCanvas();
            BuildGameOverCanvas();
            EnsureEventSystem();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameplayScenePath);
            Debug.Log($"[SceneSetup] Gameplay scene saved to {GameplayScenePath}");
        }

        [MenuItem(MenuRoot + "Build All Scenes", priority = 200)]
        public static void BuildAllScenes()
        {
            BuildBootScene();
            BuildMainMenuScene();
            BuildGameplayScene();
            ConfigureBuildSettings();
            Debug.Log("[SceneSetup] All scenes built and Build Settings configured.");
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true)
            };
        }

        private static void BuildMainMenuUI()
        {
            var canvasGo = CreateCanvas("Canvas_MainMenu", 0);

            var bg = CreateUiImage(canvasGo.transform, "Background");
            bg.color = new Color(0f, 0f, 0f, 0.55f);
            StretchFull(bg.rectTransform);

            var title = CreateTmpText(canvasGo.transform, "Title", "HANZI ZOMBIE DEFENSE", 72);
            title.alignment = TextAlignmentOptions.Center;
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -120f);
            titleRt.sizeDelta = new Vector2(1200f, 120f);

            var startBtn   = CreateTmpButton(canvasGo.transform, "StartButton",   "START GAME", new Vector2(0f,   30f));
            var optionsBtn = CreateTmpButton(canvasGo.transform, "OptionsButton", "OPTIONS",    new Vector2(0f,  -50f));
            var quitBtn    = CreateTmpButton(canvasGo.transform, "QuitButton",    "QUIT",       new Vector2(0f, -130f));

            var optionsPanelGo = new GameObject("OptionsPanel", typeof(RectTransform), typeof(Image));
            optionsPanelGo.transform.SetParent(canvasGo.transform, false);
            StretchFull(optionsPanelGo.GetComponent<RectTransform>());
            optionsPanelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            var optionsTitle = CreateTmpText(optionsPanelGo.transform, "Title", "OPTIONS", 56);
            optionsTitle.alignment = TextAlignmentOptions.Center;
            var optionsTitleRt = optionsTitle.rectTransform;
            optionsTitleRt.anchorMin = new Vector2(0.5f, 1f);
            optionsTitleRt.anchorMax = new Vector2(0.5f, 1f);
            optionsTitleRt.pivot     = new Vector2(0.5f, 1f);
            optionsTitleRt.anchoredPosition = new Vector2(0f, -100f);
            optionsTitleRt.sizeDelta = new Vector2(800f, 80f);

            optionsPanelGo.SetActive(false);

            var controllerGo = new GameObject("MainMenuController");
            var controller = controllerGo.AddComponent<MainMenu>();
            var so = new SerializedObject(controller);
            so.FindProperty("startButton").objectReferenceValue   = startBtn;
            so.FindProperty("optionsButton").objectReferenceValue = optionsBtn;
            so.FindProperty("quitButton").objectReferenceValue    = quitBtn;
            so.FindProperty("optionsPanel").objectReferenceValue  = optionsPanelGo;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildEnvironment()
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.color = Color.white;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            ground.transform.position = Vector3.zero;

            var renderer = ground.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.30f, 0.45f, 0.25f);
                renderer.sharedMaterial = mat;
            }

            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.NavigationStatic);
        }

        private static GameObject BuildPlayerRig()
        {
            var playerGo = new GameObject("Player");
            playerGo.transform.position = new Vector3(0f, 1.6f, 0f);

            try { playerGo.tag = "Player"; }
            catch { Debug.LogWarning("[SceneSetup] 'Player' tag missing; please add it in Tags & Layers."); }

            playerGo.AddComponent<PlayerHealth>();
            playerGo.AddComponent<PlayerTargeting>();
            playerGo.AddComponent<MobileSetup>();

            var controller = playerGo.AddComponent<PlayerController>();

            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(playerGo.transform, false);
            camGo.transform.localPosition = Vector3.zero;
            camGo.transform.localRotation = Quaternion.identity;
            try { camGo.tag = "MainCamera"; } catch { }

            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.5f, 0.65f, 0.85f);
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;

            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<PlayerCamera>();

            var soController = new SerializedObject(controller);
            soController.FindProperty("playerCamera").objectReferenceValue = cam;
            soController.ApplyModifiedPropertiesWithoutUndo();

            return playerGo;
        }

        private static List<SpawnPoint> BuildSpawningSystem(GameObject zombiePrefab)
        {
            var root = new GameObject("--- Spawning ---");

            var spawnPoints = new List<SpawnPoint>
            {
                CreateSpawnPoint(root.transform, "SpawnPoint_Left2",  new Vector3(-10f, 0f, 25f), SpawnLane.Left),
                CreateSpawnPoint(root.transform, "SpawnPoint_Left1",  new Vector3( -5f, 0f, 25f), SpawnLane.Left),
                CreateSpawnPoint(root.transform, "SpawnPoint_Center", new Vector3(  0f, 0f, 28f), SpawnLane.Center),
                CreateSpawnPoint(root.transform, "SpawnPoint_Right1", new Vector3(  5f, 0f, 25f), SpawnLane.Right),
                CreateSpawnPoint(root.transform, "SpawnPoint_Right2", new Vector3( 10f, 0f, 25f), SpawnLane.Right),
            };

            var spawnerGo = new GameObject("ZombieSpawner");
            spawnerGo.transform.SetParent(root.transform, false);
            var spawner = spawnerGo.AddComponent<ZombieSpawner>();

            var spawnerSo = new SerializedObject(spawner);
            var prefabComponent = zombiePrefab != null ? zombiePrefab.GetComponent<Zombie>() : null;
            spawnerSo.FindProperty("zombiePrefab").objectReferenceValue = prefabComponent;
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();

            var directorGo = new GameObject("WaveDirector");
            directorGo.transform.SetParent(root.transform, false);
            var director = directorGo.AddComponent<WaveDirector>();

            var directorSo = new SerializedObject(director);
            directorSo.FindProperty("spawner").objectReferenceValue = spawner;

            var spawnPointsProp = directorSo.FindProperty("spawnPoints");
            spawnPointsProp.arraySize = spawnPoints.Count;
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                spawnPointsProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];
            }

            var wavesProp = directorSo.FindProperty("waves");
            wavesProp.arraySize = 3;
            ConfigureWave(wavesProp.GetArrayElementAtIndex(0), zombieCount: 8,  spawnInterval: 2.0f, restAfter: 4f);
            ConfigureWave(wavesProp.GetArrayElementAtIndex(1), zombieCount: 12, spawnInterval: 1.5f, restAfter: 4f);
            ConfigureWave(wavesProp.GetArrayElementAtIndex(2), zombieCount: 16, spawnInterval: 1.2f, restAfter: 5f);

            directorSo.ApplyModifiedPropertiesWithoutUndo();

            return spawnPoints;
        }

        private static void ConfigureWave(SerializedProperty waveProp, int zombieCount, float spawnInterval, float restAfter)
        {
            var countProp    = waveProp.FindPropertyRelative("zombieCount");
            var intervalProp = waveProp.FindPropertyRelative("spawnInterval");
            var restProp     = waveProp.FindPropertyRelative("restTimeAfterWave");

            if (countProp != null) countProp.intValue = zombieCount;
            if (intervalProp != null) intervalProp.floatValue = spawnInterval;
            if (restProp != null) restProp.floatValue = restAfter;
        }

        private static SpawnPoint CreateSpawnPoint(Transform parent, string name, Vector3 position, SpawnLane lane)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var sp = go.AddComponent<SpawnPoint>();
            var so = new SerializedObject(sp);
            so.FindProperty("lane").enumValueIndex = (int)lane;
            so.ApplyModifiedPropertiesWithoutUndo();
            return sp;
        }

        private static ComboTracker BuildSystems(GameObject explosionPrefab)
        {
            var root = new GameObject("--- Systems ---");

            var difficultyGo = new GameObject("DifficultyManager");
            difficultyGo.transform.SetParent(root.transform, false);
            difficultyGo.AddComponent<DifficultyManager>();

            var comboGo = new GameObject("ComboTracker");
            comboGo.transform.SetParent(root.transform, false);
            var combo = comboGo.AddComponent<ComboTracker>();

            var scoreGo = new GameObject("ScoreManager");
            scoreGo.transform.SetParent(root.transform, false);
            var score = scoreGo.AddComponent<ScoreManager>();
            var scoreSo = new SerializedObject(score);
            scoreSo.FindProperty("comboTracker").objectReferenceValue = combo;
            scoreSo.ApplyModifiedPropertiesWithoutUndo();

            var dbGo = new GameObject("HanziDatabase");
            dbGo.transform.SetParent(root.transform, false);
            dbGo.AddComponent<HanziDatabase>();

            var explosionGo = new GameObject("ExplosionPool");
            explosionGo.transform.SetParent(root.transform, false);
            var pool = explosionGo.AddComponent<ExplosionPool>();
            if (explosionPrefab != null)
            {
                var ps = explosionPrefab.GetComponent<ParticleSystem>();
                var poolSo = new SerializedObject(pool);
                poolSo.FindProperty("explosionPrefab").objectReferenceValue = ps;
                poolSo.ApplyModifiedPropertiesWithoutUndo();
            }

            return combo;
        }

        private static void BuildDrawingCanvas()
        {
            var canvasGo = CreateCanvas("Canvas_Drawing", 0);

            var surfaceGo = new GameObject("DrawingSurface", typeof(RectTransform), typeof(Image));
            surfaceGo.transform.SetParent(canvasGo.transform, false);
            StretchFull(surfaceGo.GetComponent<RectTransform>());

            var img = surfaceGo.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 5f / 255f);
            img.raycastTarget = true;

            var recorder = surfaceGo.AddComponent<StrokeRecorder>();
            var renderer = surfaceGo.AddComponent<StrokeRenderer>();
            var writingCanvas = surfaceGo.AddComponent<WritingCanvas>();
            var session = surfaceGo.AddComponent<WritingSession>();

            var canvasSo = new SerializedObject(writingCanvas);
            canvasSo.FindProperty("recorder").objectReferenceValue = recorder;
            canvasSo.ApplyModifiedPropertiesWithoutUndo();

            var sessionSo = new SerializedObject(session);
            sessionSo.FindProperty("writingCanvas").objectReferenceValue = writingCanvas;
            sessionSo.FindProperty("strokeRecorder").objectReferenceValue = recorder;
            sessionSo.FindProperty("strokeRenderer").objectReferenceValue = renderer;
            sessionSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildHudCanvas(GameObject playerGo, ComboTracker comboTracker)
        {
            var canvasGo = CreateCanvas("Canvas_HUD", 10);
            var hudController = canvasGo.AddComponent<HudController>();

            var healthBarUi = BuildHealthBar(canvasGo.transform, playerGo);
            var scoreUi     = BuildScoreReadout(canvasGo.transform);
            var comboUi     = BuildComboReadout(canvasGo.transform, comboTracker);
            BuildWritingPanel(canvasGo.transform);

            var hudSo = new SerializedObject(hudController);
            hudSo.FindProperty("hudRoot").objectReferenceValue = canvasGo;
            hudSo.FindProperty("healthBar").objectReferenceValue = healthBarUi;
            hudSo.FindProperty("scoreUI").objectReferenceValue = scoreUi;
            hudSo.FindProperty("comboUI").objectReferenceValue = comboUi;
            hudSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static HealthBarUI BuildHealthBar(Transform parent, GameObject playerGo)
        {
            var healthRoot = new GameObject("HealthBar", typeof(RectTransform), typeof(Image));
            healthRoot.transform.SetParent(parent, false);
            var healthRt = healthRoot.GetComponent<RectTransform>();
            healthRt.anchorMin = new Vector2(0f, 1f);
            healthRt.anchorMax = new Vector2(0f, 1f);
            healthRt.pivot     = new Vector2(0f, 1f);
            healthRt.anchoredPosition = new Vector2(20f, -20f);
            healthRt.sizeDelta = new Vector2(300f, 30f);

            var healthImg = healthRoot.GetComponent<Image>();
            healthImg.color = new Color(0.2f, 0.85f, 0.3f);
            healthImg.type = Image.Type.Filled;
            healthImg.fillMethod = Image.FillMethod.Horizontal;
            healthImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthImg.fillAmount = 1f;
            healthImg.raycastTarget = false;

            var healthBarUi = healthRoot.AddComponent<HealthBarUI>();
            var healthSo = new SerializedObject(healthBarUi);
            healthSo.FindProperty("healthBar").objectReferenceValue = healthImg;
            if (playerGo != null)
            {
                healthSo.FindProperty("playerHealth").objectReferenceValue = playerGo.GetComponent<PlayerHealth>();
            }
            healthSo.ApplyModifiedPropertiesWithoutUndo();
            return healthBarUi;
        }

        private static ScoreUI BuildScoreReadout(Transform parent)
        {
            var scoreText = CreateTmpText(parent, "ScoreText", "Score: 0", 36);
            var scoreRt = scoreText.rectTransform;
            scoreRt.anchorMin = new Vector2(1f, 1f);
            scoreRt.anchorMax = new Vector2(1f, 1f);
            scoreRt.pivot     = new Vector2(1f, 1f);
            scoreRt.anchoredPosition = new Vector2(-20f, -20f);
            scoreRt.sizeDelta = new Vector2(400f, 50f);
            scoreText.alignment = TextAlignmentOptions.Right;

            var scoreUiGo = new GameObject("ScoreUI");
            scoreUiGo.transform.SetParent(parent, false);
            var scoreUi = scoreUiGo.AddComponent<ScoreUI>();
            var scoreUiSo = new SerializedObject(scoreUi);
            scoreUiSo.FindProperty("scoreText").objectReferenceValue = scoreText;
            scoreUiSo.FindProperty("punchTarget").objectReferenceValue = scoreText.rectTransform;
            scoreUiSo.ApplyModifiedPropertiesWithoutUndo();
            return scoreUi;
        }

        private static ComboUI BuildComboReadout(Transform parent, ComboTracker comboTracker)
        {
            var comboGo = new GameObject("ComboUI", typeof(RectTransform), typeof(CanvasGroup));
            comboGo.transform.SetParent(parent, false);
            var comboRt = comboGo.GetComponent<RectTransform>();
            comboRt.anchorMin = new Vector2(1f, 1f);
            comboRt.anchorMax = new Vector2(1f, 1f);
            comboRt.pivot     = new Vector2(1f, 1f);
            comboRt.anchoredPosition = new Vector2(-20f, -80f);
            comboRt.sizeDelta = new Vector2(400f, 80f);

            var comboText = CreateTmpText(comboGo.transform, "ComboText", "x1", 28);
            var comboTextRt = comboText.rectTransform;
            comboTextRt.anchorMin = new Vector2(1f, 1f);
            comboTextRt.anchorMax = new Vector2(1f, 1f);
            comboTextRt.pivot     = new Vector2(1f, 1f);
            comboTextRt.anchoredPosition = Vector2.zero;
            comboTextRt.sizeDelta = new Vector2(400f, 40f);
            comboText.alignment = TextAlignmentOptions.Right;

            var multiplierText = CreateTmpText(comboGo.transform, "MultiplierText", "1.0x", 22);
            var multRt = multiplierText.rectTransform;
            multRt.anchorMin = new Vector2(1f, 1f);
            multRt.anchorMax = new Vector2(1f, 1f);
            multRt.pivot     = new Vector2(1f, 1f);
            multRt.anchoredPosition = new Vector2(0f, -40f);
            multRt.sizeDelta = new Vector2(400f, 30f);
            multiplierText.alignment = TextAlignmentOptions.Right;
            multiplierText.color = new Color(1f, 0.85f, 0.3f);

            var comboUi = comboGo.AddComponent<ComboUI>();
            var comboUiSo = new SerializedObject(comboUi);
            comboUiSo.FindProperty("comboTracker").objectReferenceValue = comboTracker;
            comboUiSo.FindProperty("comboText").objectReferenceValue = comboText;
            comboUiSo.FindProperty("multiplierText").objectReferenceValue = multiplierText;
            comboUiSo.ApplyModifiedPropertiesWithoutUndo();
            return comboUi;
        }

        private static WritingPanel BuildWritingPanel(Transform parent)
        {
            var writingPanelGo = new GameObject("WritingPanel", typeof(RectTransform));
            writingPanelGo.transform.SetParent(parent, false);
            var wpRt = writingPanelGo.GetComponent<RectTransform>();
            wpRt.anchorMin = new Vector2(0.5f, 0f);
            wpRt.anchorMax = new Vector2(0.5f, 0f);
            wpRt.pivot     = new Vector2(0.5f, 0f);
            wpRt.anchoredPosition = new Vector2(0f, 40f);
            wpRt.sizeDelta = new Vector2(600f, 240f);

            var targetCharText = CreateTmpText(writingPanelGo.transform, "TargetCharacterText", "字", 200);
            targetCharText.alignment = TextAlignmentOptions.Center;
            var tctRt = targetCharText.rectTransform;
            tctRt.anchorMin = new Vector2(0.5f, 0f);
            tctRt.anchorMax = new Vector2(0.5f, 0f);
            tctRt.pivot     = new Vector2(0.5f, 0f);
            tctRt.anchoredPosition = new Vector2(0f, 60f);
            tctRt.sizeDelta = new Vector2(280f, 240f);
            var faded = targetCharText.color;
            faded.a = 0.3f;
            targetCharText.color = faded;

            var progressText = CreateTmpText(writingPanelGo.transform, "ProgressText", "0/0", 32);
            progressText.alignment = TextAlignmentOptions.Center;
            var ptRt = progressText.rectTransform;
            ptRt.anchorMin = new Vector2(0.5f, 0f);
            ptRt.anchorMax = new Vector2(0.5f, 0f);
            ptRt.pivot     = new Vector2(0.5f, 0f);
            ptRt.anchoredPosition = Vector2.zero;
            ptRt.sizeDelta = new Vector2(200f, 50f);

            var writingPanel = writingPanelGo.AddComponent<WritingPanel>();
            var wpSo = new SerializedObject(writingPanel);
            wpSo.FindProperty("targetCharacterText").objectReferenceValue = targetCharText;
            wpSo.FindProperty("strokeProgressText").objectReferenceValue = progressText;
            wpSo.ApplyModifiedPropertiesWithoutUndo();
            return writingPanel;
        }

        private static void BuildPauseCanvas()
        {
            var canvasGo = CreateCanvas("Canvas_Pause", 20);

            var panelGo = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            StretchFull(panelGo.GetComponent<RectTransform>());
            panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            var title = CreateTmpText(panelGo.transform, "Title", "PAUSED", 80);
            title.alignment = TextAlignmentOptions.Center;
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -160f);
            titleRt.sizeDelta = new Vector2(600f, 100f);

            var resumeBtn = CreateTmpButton(panelGo.transform, "ResumeButton",   "RESUME",    new Vector2(0f,   30f));
            var menuBtn   = CreateTmpButton(panelGo.transform, "MainMenuButton", "MAIN MENU", new Vector2(0f,  -50f));
            var quitBtn   = CreateTmpButton(panelGo.transform, "QuitButton",     "QUIT",      new Vector2(0f, -130f));

            panelGo.SetActive(false);

            var pauseMenu = canvasGo.AddComponent<PauseMenu>();
            var so = new SerializedObject(pauseMenu);
            so.FindProperty("pausePanel").objectReferenceValue     = panelGo;
            so.FindProperty("resumeButton").objectReferenceValue   = resumeBtn;
            so.FindProperty("mainMenuButton").objectReferenceValue = menuBtn;
            so.FindProperty("quitButton").objectReferenceValue     = quitBtn;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildGameOverCanvas()
        {
            var canvasGo = CreateCanvas("Canvas_GameOver", 30);

            var panelGo = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            StretchFull(panelGo.GetComponent<RectTransform>());
            panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            var title = CreateTmpText(panelGo.transform, "GameOverTitle", "GAME OVER", 96);
            title.color = new Color(1f, 0.25f, 0.25f);
            title.alignment = TextAlignmentOptions.Center;
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -150f);
            titleRt.sizeDelta = new Vector2(900f, 130f);

            var finalScore = CreateTmpText(panelGo.transform, "FinalScoreText", "Score: 0", 48);
            finalScore.alignment = TextAlignmentOptions.Center;
            var fsRt = finalScore.rectTransform;
            fsRt.anchorMin = new Vector2(0.5f, 0.5f);
            fsRt.anchorMax = new Vector2(0.5f, 0.5f);
            fsRt.pivot = new Vector2(0.5f, 0.5f);
            fsRt.anchoredPosition = new Vector2(0f, 80f);
            fsRt.sizeDelta = new Vector2(700f, 70f);

            var highScore = CreateTmpText(panelGo.transform, "HighScoreText", "High Score: 0", 36);
            highScore.alignment = TextAlignmentOptions.Center;
            var hsRt = highScore.rectTransform;
            hsRt.anchorMin = new Vector2(0.5f, 0.5f);
            hsRt.anchorMax = new Vector2(0.5f, 0.5f);
            hsRt.pivot = new Vector2(0.5f, 0.5f);
            hsRt.anchoredPosition = new Vector2(0f, 20f);
            hsRt.sizeDelta = new Vector2(700f, 50f);

            var killsText = CreateTmpText(panelGo.transform, "KillsText", "Kills: 0", 28);
            killsText.alignment = TextAlignmentOptions.Center;
            var ktRt = killsText.rectTransform;
            ktRt.anchorMin = new Vector2(0.5f, 0.5f);
            ktRt.anchorMax = new Vector2(0.5f, 0.5f);
            ktRt.pivot = new Vector2(0.5f, 0.5f);
            ktRt.anchoredPosition = new Vector2(0f, -30f);
            ktRt.sizeDelta = new Vector2(700f, 40f);

            var retryBtn = CreateTmpButton(panelGo.transform, "RetryButton",    "RETRY",     new Vector2(-160f, -150f));
            var menuBtn  = CreateTmpButton(panelGo.transform, "MainMenuButton", "MAIN MENU", new Vector2( 160f, -150f));

            panelGo.SetActive(false);

            var screen = canvasGo.AddComponent<GameOverScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("panel").objectReferenceValue          = panelGo;
            so.FindProperty("finalScoreText").objectReferenceValue = finalScore;
            so.FindProperty("highScoreText").objectReferenceValue  = highScore;
            so.FindProperty("killsText").objectReferenceValue      = killsText;
            so.FindProperty("retryButton").objectReferenceValue    = retryBtn;
            so.FindProperty("mainMenuButton").objectReferenceValue = menuBtn;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuildZombiePrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Zombie_Prefab";

            Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());

            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 1f, 0f);
            box.size = new Vector3(1.2f, 2.0f, 1.2f);

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.speed = 2.5f;
            agent.angularSpeed = 360f;
            agent.acceleration = 16f;
            agent.stoppingDistance = 1.4f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

            go.AddComponent<Animator>();

            var zombie    = go.AddComponent<Zombie>();
            var ai        = go.AddComponent<ZombieAI>();
            var health    = go.AddComponent<ZombieHealth>();
            var zAnimator = go.AddComponent<ZombieAnimator>();
            var deathFx   = go.AddComponent<ZombieDeathFX>();

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            labelGo.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            var canvas = labelGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            labelGo.AddComponent<CanvasScaler>();
            labelGo.AddComponent<GraphicRaycaster>();
            var canvasRt = (RectTransform)labelGo.transform;
            canvasRt.sizeDelta = new Vector2(200f, 100f);

            var labelTextGo = new GameObject("Text", typeof(RectTransform));
            labelTextGo.transform.SetParent(labelGo.transform, false);
            var labelTmp = labelTextGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "字";
            labelTmp.fontSize = 72;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = Color.white;
            var labelTextRt = labelTmp.rectTransform;
            labelTextRt.anchorMin = Vector2.zero;
            labelTextRt.anchorMax = Vector2.one;
            labelTextRt.offsetMin = Vector2.zero;
            labelTextRt.offsetMax = Vector2.zero;

            var label = labelGo.AddComponent<ZombieCharacterLabel>();

            var labelSo = new SerializedObject(label);
            labelSo.FindProperty("canvas").objectReferenceValue = canvas;
            labelSo.FindProperty("text").objectReferenceValue = labelTmp;
            labelSo.ApplyModifiedPropertiesWithoutUndo();

            var zSo = new SerializedObject(zombie);
            zSo.FindProperty("health").objectReferenceValue         = health;
            zSo.FindProperty("ai").objectReferenceValue             = ai;
            zSo.FindProperty("zombieAnimator").objectReferenceValue = zAnimator;
            zSo.FindProperty("label").objectReferenceValue          = label;
            zSo.FindProperty("deathFx").objectReferenceValue        = deathFx;
            zSo.ApplyModifiedPropertiesWithoutUndo();

            WireZombieBackref(ai,        "zombie", zombie);
            WireZombieBackref(health,    "zombie", zombie);
            WireZombieBackref(zAnimator, "zombie", zombie);
            WireZombieBackref(deathFx,   "zombie", zombie);

            EnsureDirectory(ZombiePrefabsDir);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, ZombiePrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void WireZombieBackref(Component comp, string fieldName, Zombie zombie)
        {
            if (comp == null) return;
            var so = new SerializedObject(comp);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = zombie;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject BuildExplosionPrefab()
        {
            var go = new GameObject("Explosion_VFX");

            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 1.0f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.8f;
            main.startSpeed = 5f;
            main.startSize = 0.3f;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.55f, 0.1f),
                new Color(1f, 0.15f, 0.05f));

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 1f, 0f, 0f),
                new Keyframe(1f, 0f, -2f, 0f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.55f, 0.1f), 0f),
                    new GradientColorKey(new Color(1f, 0.15f, 0.05f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                Material defaultParticleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
                if (defaultParticleMat == null)
                {
                    var shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
                    if (shader != null) defaultParticleMat = new Material(shader);
                }
                if (defaultParticleMat != null) renderer.sharedMaterial = defaultParticleMat;
            }

            EnsureDirectory(VfxPrefabsDir);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, ExplosionPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return go;
        }

        private static Image CreateUiImage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            return go.GetComponent<Image>();
        }

        private static TextMeshProUGUI CreateTmpText(Transform parent, string name, string text, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button CreateTmpButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(360f, 70f);
            rt.anchoredPosition = anchoredPosition;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.18f, 0.20f, 0.25f, 0.95f);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor      = new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = new Color(0.85f, 0.85f, 1f, 1f);
            colors.pressedColor     = new Color(0.6f, 0.6f, 0.7f, 1f);
            btn.colors = colors;

            var labelTmp = CreateTmpText(go.transform, "Label", label, 32);
            labelTmp.alignment = TextAlignmentOptions.Center;
            var labelRt = labelTmp.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            labelTmp.color = Color.white;

            return btn;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot     = new Vector2(0.5f, 0.5f);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static void EnsureDirectory(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return;
            string absolute = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, assetPath);
            if (!Directory.Exists(absolute)) Directory.CreateDirectory(absolute);
            AssetDatabase.Refresh();
        }
    }
}
#endif
