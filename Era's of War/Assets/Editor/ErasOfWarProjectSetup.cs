using System;
using System.IO;
using System.Linq;
using ErasOfWar.Unity.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ErasOfWar.Unity.EditorTools
{
    /// <summary>
    /// One-time project setup for the 2D base screen. Safe to run again.
    ///  1. Creates Assets/Settings/Renderer2D.asset (URP 2D Renderer) and makes it the only renderer
    ///     on every URP pipeline asset (Mobile and PC quality levels).
    ///  2. Creates Assets/Scenes/Base.unity: orthographic camera, Global Light 2D, BaseSceneController.
    ///  3. Puts Base first in Build Settings, sets portrait orientation and 2D editor mode.
    /// Menu: Eras of War > Set Up 2D Base Scene. Batch: -executeMethod ErasOfWar.Unity.EditorTools.ErasOfWarProjectSetup.RunBatch
    /// </summary>
    public static class ErasOfWarProjectSetup
    {
        private const string RendererPath = "Assets/Settings/Renderer2D.asset";
        private const string ScenePath = "Assets/Scenes/Base.unity";
        private const string UrpPackagePath = "Packages/com.unity.render-pipelines.universal";

        [MenuItem("Eras of War/Set Up 2D Base Scene")]
        public static void RunFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Run();
            EditorSceneManager.OpenScene(ScenePath);
        }

        public static void RunBatch()
        {
            try
            {
                Run();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[Eras of War Setup] FAILED: " + e);
                EditorApplication.Exit(1);
            }
        }

        public static void Run()
        {
            var renderer = CreateOrLoad2DRenderer();
            var assets = AssignRendererToPipelineAssets(renderer);
            CreateBaseScene();
            ConfigureProject();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Eras of War Setup] Done. 2D Renderer assigned to {assets} URP asset(s); scene {ScenePath}.");
        }

        // ------------------------------------------------------------------ renderer

        private static Renderer2DData CreateOrLoad2DRenderer()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
            if (existing != null) return existing;

            // Same steps as the editor's Create > Rendering > URP 2D Renderer menu.
            var data = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(data, RendererPath);
            ResourceReloader.ReloadAllNullIn(data, UrpPackagePath);

            // Post-process data is internal on Renderer2DData; copy it from the existing 3D renderer.
            var so = new SerializedObject(data);
            var pp = so.FindProperty("m_PostProcessData");
            if (pp != null && pp.objectReferenceValue == null)
            {
                var source = AssetDatabase.FindAssets("t:UniversalRendererData")
                    .Select(g => AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(r => r != null)
                    .Select(r => new SerializedObject(r).FindProperty("postProcessData")?.objectReferenceValue)
                    .FirstOrDefault(o => o != null);
                if (source != null)
                {
                    pp.objectReferenceValue = source;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("[Eras of War Setup] Created " + RendererPath);
            return data;
        }

        private static int AssignRendererToPipelineAssets(Renderer2DData renderer)
        {
            var count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/", StringComparison.Ordinal)) continue;
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (asset == null) continue;

                var so = new SerializedObject(asset);
                var list = so.FindProperty("m_RendererDataList");
                var index = so.FindProperty("m_DefaultRendererIndex");
                if (list == null || index == null)
                    throw new InvalidOperationException($"{path}: renderer list not found (URP version changed?)");

                list.arraySize = 1;
                list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                index.intValue = 0;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                Debug.Log($"[Eras of War Setup] {path}: renderer = 2D Renderer");
                count++;
            }
            if (count == 0) throw new InvalidOperationException("No URP pipeline assets found under Assets/");
            return count;
        }

        // ------------------------------------------------------------------ scene

        private static void CreateBaseScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 14f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = PlaceholderArt.Background;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            camGo.transform.position = new Vector3(10f, 10f, -10f);
            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.AddComponent<BaseCameraRig>();

            // 2D lit sprites need a light; one global light at full strength = sprites show their true colors.
            var lightGo = new GameObject("Global Light 2D");
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = Color.white;

            new GameObject("Base").AddComponent<BaseSceneController>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not save " + ScenePath);
            Debug.Log("[Eras of War Setup] Saved " + ScenePath);
        }

        // ------------------------------------------------------------------ project settings

        private static void ConfigureProject()
        {
            // Base scene first in the build (keeps any others after it).
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            foreach (var s in scenes.Skip(1)) s.enabled = false;
            EditorBuildSettings.scenes = scenes.ToArray();

            // Portrait phone game
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.productName = "Eras of War";

            // New textures import as sprites, Scene view opens in 2D
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        }
    }
}
