using System.Linq;
using OctagonFighter.Data.Fighters;
using OctagonFighter.Infrastructure.Health;
using OctagonFighter.Infrastructure.Input;
using OctagonFighter.Infrastructure.Movement;
using OctagonFighter.Infrastructure.Stamina;
using OctagonFighter.Infrastructure.Time;
using OctagonFighter.Presentation.Fighter;
using OctagonFighter.Presentation.Scenes;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OctagonFighter.Editor
{
    /// <summary>
    /// Builds the Phase 1 fighter definition, prefab, and demo scene from the imported Mixamo assets.
    /// </summary>
    public static class Phase1AssetBuilder
    {
        private const string BoxerModelPath = "Assets/_Project/Art/Characters/boxer.fbx";
        private const string IdleAnimationPath = "Assets/_Project/Art/Animations/FightingIdle_mixamo.fbx";
        private const string FighterDefinitionPath = "Assets/_Project/ScriptableObjects/Fighters/FD_Boxer.asset";
        private const string AnimatorControllerPath = "Assets/_Project/Art/Animations/AC_Fighter_Base.controller";
        private const string FighterPrefabPath = "Assets/_Project/Prefabs/Fighters/PF_Fighter_Boxer.prefab";
        private const string DemoScenePath = "Assets/_Project/Scenes/CombatTest.unity";

        /// <summary>
        /// Generates every Phase 1 asset and opens the demo scene.
        /// </summary>
        [MenuItem("OctagonFighter/Phase 1/Rebuild Fighter Assets")]
        public static void BuildAll()
        {
            EnsureFolder("Assets/_Project/ScriptableObjects/Fighters");
            EnsureFolder("Assets/_Project/Prefabs/Fighters");

            ConfigureHumanoidImport(BoxerModelPath);
            ConfigureHumanoidImport(IdleAnimationPath);

            FighterDefinition definition = CreateOrUpdateFighterDefinition();
            AnimatorController animatorController = CreateOrUpdateAnimatorController();
            GameObject fighterPrefab = CreateOrUpdateFighterPrefab(definition, animatorController);
            CreateOrUpdateDemoScene(fighterPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 1 fighter assets built successfully.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void ConfigureHumanoidImport(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer))
            {
                Debug.LogWarning($"Could not find model importer for '{path}'.");
                return;
            }

            bool changed = false;

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static FighterDefinition CreateOrUpdateFighterDefinition()
        {
            var definition = AssetDatabase.LoadAssetAtPath<FighterDefinition>(FighterDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<FighterDefinition>();
                AssetDatabase.CreateAsset(definition, FighterDefinitionPath);
            }

            var serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("_id").stringValue = "boxer_01";
            serializedDefinition.FindProperty("_displayName").stringValue = "The Boxer";
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static AnimatorController CreateOrUpdateAnimatorController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
            }

            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            if (stateMachine.states.Length == 0)
            {
                AnimatorState idleState = stateMachine.AddState("Idle");
                AnimationClip idleClip = LoadFirstAnimationClip(IdleAnimationPath);
                if (idleClip != null)
                {
                    idleState.motion = idleClip;
                }
                else
                {
                    Debug.LogWarning($"No animation clip found in '{IdleAnimationPath}'.");
                }

                stateMachine.defaultState = idleState;
            }

            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsBlocking", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsDefeated", AnimatorControllerParameterType.Bool);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void EnsureParameter(
            AnimatorController controller,
            string parameterName,
            AnimatorControllerParameterType parameterType)
        {
            if (controller.parameters.Any(parameter => parameter.name == parameterName))
            {
                return;
            }

            controller.AddParameter(parameterName, parameterType);
        }

        private static AnimationClip LoadFirstAnimationClip(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__"))
                .OrderByDescending(clip => clip.name.Contains("FightingIdle"))
                .FirstOrDefault();
        }

        private static GameObject CreateOrUpdateFighterPrefab(
            FighterDefinition definition,
            AnimatorController animatorController)
        {
            AssetDatabase.DeleteAsset(FighterPrefabPath);

            var root = new GameObject("PF_Fighter_Boxer");

            var characterController = root.AddComponent<CharacterController>();
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.height = 1.8f;
            characterController.radius = 0.3f;
            characterController.stepOffset = 0.3f;
            characterController.slopeLimit = 45f;

            var health = root.AddComponent<HealthComponent>();
            var stamina = root.AddComponent<StaminaComponent>();
            var motor = root.AddComponent<FighterMotor>();
            var intentSource = root.AddComponent<PlayerIntentSource>();
            var view = root.AddComponent<FighterView>();
            var fighterController = root.AddComponent<FighterController>();

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BoxerModelPath);
            if (modelAsset == null)
            {
                throw new MissingReferenceException($"Could not load model at '{BoxerModelPath}'.");
            }

            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            modelInstance.name = "Model";
            modelInstance.transform.SetParent(root.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            Animator animator = modelInstance.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                animator = modelInstance.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;

            var controllerSerialized = new SerializedObject(fighterController);
            controllerSerialized.FindProperty("_definition").objectReferenceValue = definition;
            controllerSerialized.FindProperty("_intentSourceBehaviour").objectReferenceValue = intentSource;
            controllerSerialized.FindProperty("_motor").objectReferenceValue = motor;
            controllerSerialized.FindProperty("_view").objectReferenceValue = view;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            var viewSerialized = new SerializedObject(view);
            viewSerialized.FindProperty("_animator").objectReferenceValue = animator;
            viewSerialized.ApplyModifiedPropertiesWithoutUndo();

            _ = health;
            _ = stamina;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, FighterPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateOrUpdateDemoScene(GameObject fighterPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sessionRoot = new GameObject("CombatTest");
            var tickDriver = sessionRoot.AddComponent<CombatTickDriver>();
            var session = sessionRoot.AddComponent<CombatSession>();

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(2f, 1f, 2f);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(0f, 2.6f, -5.5f);
            cameraObject.transform.LookAt(new Vector3(0f, 1.1f, 0f));

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var fighterOne = (GameObject)PrefabUtility.InstantiatePrefab(fighterPrefab);
            fighterOne.name = "Fighter_Player";
            fighterOne.transform.position = new Vector3(-1.5f, 0f, 0f);

            var fighterTwo = (GameObject)PrefabUtility.InstantiatePrefab(fighterPrefab);
            fighterTwo.name = "Fighter_Opponent";
            fighterTwo.transform.position = new Vector3(1.5f, 0f, 0f);
            fighterTwo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var sessionSerialized = new SerializedObject(session);
            sessionSerialized.FindProperty("_tickDriver").objectReferenceValue = tickDriver;

            SerializedProperty fightersProperty = sessionSerialized.FindProperty("_fighters");
            fightersProperty.arraySize = 2;
            fightersProperty.GetArrayElementAtIndex(0).objectReferenceValue =
                fighterOne.GetComponent<FighterController>();
            fightersProperty.GetArrayElementAtIndex(1).objectReferenceValue =
                fighterTwo.GetComponent<FighterController>();
            sessionSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, DemoScenePath);
            AddSceneToBuildSettings(DemoScenePath);
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
            if (existingScenes.Any(scene => scene.path == scenePath))
            {
                return;
            }

            var scenes = new EditorBuildSettingsScene[existingScenes.Length + 1];
            for (int index = 0; index < existingScenes.Length; index++)
            {
                scenes[index] = existingScenes[index];
            }

            scenes[existingScenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = scenes;
        }
    }
}
