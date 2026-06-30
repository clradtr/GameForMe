#if UNITY_EDITOR
using System;
using System.IO;
using ArenaPrototype;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArenaPrototypeEditor
{
    public static class ArenaPrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/ArenaPrototype.unity";
        private const string WindowsBuildDirectory = "Builds/ArenaPrototype";
        private const string WindowsBuildPath = WindowsBuildDirectory + "/ArenaPrototype.exe";
        private static double smokeStartedAt;
        private static bool smokeWaveStarted;
        private static bool previousEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions previousEnterPlayModeOptions;

        [MenuItem("Arena Prototype/Rebuild Arena Scene")]
        public static void BuildArenaScene()
        {
            EnsureFolder("Assets", "Scenes");
            GeneratedArtSetup.BuildGeneratedArtAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Arena Prototype Root");
            root.AddComponent<ArenaGame>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 12.5f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 18f, -14f);
            cameraObject.transform.rotation = Quaternion.Euler(58f, 0f, 0f);

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Built arena prototype scene at " + ScenePath);
        }

        [MenuItem("Arena Prototype/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            BuildArenaScene();
            Directory.CreateDirectory(WindowsBuildDirectory);

            PlayerSettings.companyName = "Personal Prototype";
            PlayerSettings.productName = "Arena Preference Prototype";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.runInBackground = true;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = WindowsBuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Windows player build failed: " + summary.result);
            }

            Debug.Log(string.Format("Built Windows player at {0} ({1:0.0} MB)", WindowsBuildPath, summary.totalSize / (1024f * 1024f)));
        }

        public static void SmokeTestArenaScene()
        {
            BuildArenaScene();
            EditorSceneManager.OpenScene(ScenePath);
            smokeStartedAt = 0d;
            smokeWaveStarted = false;
            previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged += OnSmokePlayModeStateChanged;
            EditorApplication.EnterPlaymode();
        }

        private static void OnSmokePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            smokeStartedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update += SmokeUpdate;
        }

        private static void SmokeUpdate()
        {
            try
            {
                double elapsed = EditorApplication.timeSinceStartup - smokeStartedAt;
                ArenaGame game = UnityEngine.Object.FindFirstObjectByType<ArenaGame>();
                if (game == null)
                {
                    if (elapsed < 1.5d)
                    {
                        return;
                    }

                    throw new InvalidOperationException("ArenaGame was not created in play mode.");
                }

                if (!smokeWaveStarted && elapsed > 1.0d)
                {
                    game.StartNextWave();
                    smokeWaveStarted = true;
                }

                if (elapsed < 3.2d)
                {
                    return;
                }

                PlayerController player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                ArenaHUD hud = UnityEngine.Object.FindFirstObjectByType<ArenaHUD>();
                WaveSpawner waveSpawner = UnityEngine.Object.FindFirstObjectByType<WaveSpawner>();

                if (player == null || player.Health == null || !player.Health.IsAlive)
                {
                    throw new InvalidOperationException("Player was not initialized.");
                }

                if (player.Skills == null || player.Skills.Skills.Count != 4)
                {
                    throw new InvalidOperationException("Expected exactly four player skills.");
                }

                if (hud == null)
                {
                    throw new InvalidOperationException("HUD was not initialized.");
                }

                if (waveSpawner == null || !waveSpawner.WaveActive || waveSpawner.AliveCount <= 0)
                {
                    throw new InvalidOperationException("Wave did not spawn enemies.");
                }

                if (GameObject.Find("Arena Floor") == null)
                {
                    throw new InvalidOperationException("Runtime arena floor was not created.");
                }

                Debug.Log("Arena prototype smoke test passed.");
                CleanupSmokeTest();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                CleanupSmokeTest();
                EditorApplication.Exit(1);
            }
        }

        private static void CleanupSmokeTest()
        {
            EditorApplication.update -= SmokeUpdate;
            EditorApplication.playModeStateChanged -= OnSmokePlayModeStateChanged;
            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
#endif
