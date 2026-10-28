using System;
using System.IO;
using System.Linq;
using OctagonFighter.Data.Fighters;
using OctagonFighter.Infrastructure.Combat;
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
    /// Builds the fighter definition, prefab, locomotion animator, and demo scene from imported Mixamo assets.
    /// </summary>
    public static class FighterAssetBuilder
    {
        private const string AnimationsFolder = "Assets/_Project/Art/Animations";
        private const string BoxerModelPath = "Assets/_Project/Art/Characters/boxer.fbx";
        private const string FighterDefinitionPath = "Assets/_Project/ScriptableObjects/Fighters/FD_Boxer.asset";
        private const string AnimatorControllerPath = "Assets/_Project/Art/Animations/AC_Fighter_Base.controller";
        private const string FighterPrefabPath = "Assets/_Project/Prefabs/Fighters/PF_Fighter_Boxer.prefab";
        private const string DemoScenePath = "Assets/_Project/Scenes/CombatTest.unity";
        private const string LocomotionStateName = "Locomotion";
        private const string BlendTreeName = "LocomotionBlendTree";
        private const string SpeedParameterName = "Speed";
        private const string MoveXParameterName = "MoveX";
        private const string MoveYParameterName = "MoveY";
        private const string JabStateName = "Jab";
        private const string CrossStateName = "Cross";
        private const string LightPunchToken = "Lead Jab";
        private const string HeavyPunchToken = "Cross";
        private const string IdleAnimationPath =
            "Assets/_Project/Art/Animations/FightingIdle_mixamo.fbx";
        private const string WalkAnimationPath =
            "Assets/_Project/Art/Animations/Standard Walk.fbx";
        private const string StrafeAnimationPath =
            "Assets/_Project/Art/Animations/Strafe.fbx";
        private const float ForwardWalkPlayback = 1.1f;
        private const float BackwardWalkPlayback = -0.88f;
        private const float StrafePlayback = 3.1f;

        /// <summary>
        /// Generates the fighter assets and opens the demo scene.
        /// </summary>
        [MenuItem("OctagonFighter/Assets/Rebuild Fighter Assets")]
        public static void BuildAll()
        {
            EnsureFolder("Assets/_Project/ScriptableObjects/Fighters");
            EnsureFolder("Assets/_Project/Prefabs/Fighters");

            string idlePath = ResolveAnimationPath(IdleAnimationPath, "idle");
            string walkPath = ResolveAnimationPath(WalkAnimationPath, "walk");
            string strafePath = ResolveAnimationPath(StrafeAnimationPath, "strafe");

            ConfigureHumanoidImport(BoxerModelPath);
            ConfigureHumanoidImport(idlePath);
            ConfigureHumanoidImport(walkPath);
            ConfigureHumanoidImport(strafePath);

            FighterDefinition definition = CreateOrUpdateFighterDefinition();
            AnimatorController animatorController = CreateOrUpdateLocomotionController(
                idlePath,
                walkPath,
                strafePath);
            EnsureAttackAnimatorStates();
            GameObject fighterPrefab = CreateOrUpdateFighterPrefab(definition, animatorController);
            CreateOrUpdateDemoScene(fighterPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateLocomotionAssets();
            Debug.Log("Fighter and locomotion assets built successfully.");
        }

        /// <summary>
        /// Verifies that the generated prefab, animator controller, and demo scene are complete.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when a required asset or setting is missing.</exception>
        [MenuItem("OctagonFighter/Assets/Validate Locomotion Assets")]
        public static void ValidateLocomotionAssets()
        {
            var errors = new System.Text.StringBuilder();

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (controller == null)
            {
                errors.Append("Animator controller is missing. ");
            }
            else
            {
                ValidateAnimatorController(controller, errors);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FighterPrefabPath);
            if (prefab == null)
            {
                errors.Append("Fighter prefab is missing. ");
            }
            else
            {
                ValidateFighterPrefab(prefab, controller, errors);
            }

            var definition = AssetDatabase.LoadAssetAtPath<FighterDefinition>(FighterDefinitionPath);
            if (definition == null)
            {
                errors.Append("Fighter definition is missing. ");
            }
            else
            {
                try
                {
                    definition.CreateRuntimeData();
                }
                catch (Exception exception)
                {
                    errors.Append($"Fighter definition is invalid: {exception.Message} ");
                }
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoScenePath) == null)
            {
                errors.Append("Demo scene is missing. ");
            }

            string error = errors.ToString().Trim();
            if (error.Length > 0)
            {
                throw new InvalidOperationException($"Locomotion validation failed: {error}");
            }

            Debug.Log("Locomotion validation passed.");
        }

        /// <summary>
        /// Adds jab and cross states to the existing animator controller without rebuilding it.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the controller or a punch clip is missing.</exception>
        [MenuItem("OctagonFighter/Assets/Ensure Attack Animator States")]
        public static void EnsureAttackAnimatorStates()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException(
                    $"Animator controller is missing at '{AnimatorControllerPath}'. " +
                    "Run 'Rebuild Fighter Assets' first.");
            }

            string jabPath = RequireAnimationPath(LightPunchToken);
            string crossPath = RequireAnimationPath(HeavyPunchToken);
            ConfigureHumanoidImport(jabPath);
            ConfigureHumanoidImport(crossPath);

            AnimationClip jabClip = LoadAnimationClip(jabPath, "jab");
            AnimationClip crossClip = LoadAnimationClip(crossPath, "cross");
            EnsureNonLooping(jabClip, jabPath);
            EnsureNonLooping(crossClip, crossPath);

            EnsureParameter(controller, JabStateName, AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, CrossStateName, AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState locomotionState = stateMachine.states
                .Select(childState => childState.state)
                .FirstOrDefault(state => state.name == LocomotionStateName);

            if (locomotionState == null)
            {
                locomotionState = stateMachine.defaultState;
            }

            if (locomotionState == null)
            {
                throw new InvalidOperationException(
                    "Animator controller has no Locomotion or default state to return to.");
            }

            AnimatorState jabState = EnsureAttackState(stateMachine, JabStateName, jabClip);
            AnimatorState crossState = EnsureAttackState(stateMachine, CrossStateName, crossClip);

            EnsureAttackTransition(locomotionState, jabState, JabStateName);
            EnsureAttackTransition(locomotionState, crossState, CrossStateName);

            // Attack exit belongs to FighterView so the strike state and the animator finish on the
            // same tick. Remove any exit-time return transition left by older builds.
            RemoveSelfTimedReturn(jabState, locomotionState);
            RemoveSelfTimedReturn(crossState, locomotionState);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log($"Attack animator states ensured on '{AnimatorControllerPath}'.");
        }

        private static AnimatorState EnsureAttackState(
            AnimatorStateMachine stateMachine,
            string stateName,
            Motion motion)
        {
            AnimatorState state = stateMachine.states
                .Select(childState => childState.state)
                .FirstOrDefault(candidate => candidate.name == stateName);

            if (state == null)
            {
                state = stateMachine.AddState(stateName);
            }

            state.motion = motion;
            state.speed = 1f;
            return state;
        }

        private static void EnsureAttackTransition(AnimatorState from, AnimatorState to, string trigger)
        {
            if (from.transitions.Any(transition => transition.destinationState == to))
            {
                return;
            }

            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.05f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void RemoveSelfTimedReturn(AnimatorState from, AnimatorState to)
        {
            foreach (AnimatorStateTransition transition in from.transitions.ToArray())
            {
                if (transition.destinationState == to && transition.hasExitTime)
                {
                    from.RemoveTransition(transition);
                }
            }
        }

        private static void EnsureNonLooping(AnimationClip clip, string path)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime)
            {
                return;
            }

            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            Debug.Log($"Disabled looping for '{clip.name}' from '{path}'.");
        }

        private static void ValidateAnimatorController(AnimatorController controller, System.Text.StringBuilder errors)
        {
            ValidateFloatParameter(controller, SpeedParameterName, errors);
            ValidateFloatParameter(controller, MoveXParameterName, errors);
            ValidateFloatParameter(controller, MoveYParameterName, errors);

            if (controller.layers.Length == 0)
            {
                errors.Append("Animator controller has no layers. ");
                return;
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            ValidateAttackAnimatorState(controller, stateMachine, JabStateName, errors);
            ValidateAttackAnimatorState(controller, stateMachine, CrossStateName, errors);

            AnimatorState locomotionState = stateMachine.states
                .Select(childState => childState.state)
                .FirstOrDefault(state => state.name == LocomotionStateName);

            if (locomotionState == null)
            {
                errors.Append($"Animator state '{LocomotionStateName}' is missing. ");
                return;
            }

            if (!(locomotionState.motion is BlendTree blendTree))
            {
                errors.Append("Locomotion state does not use a blend tree. ");
                return;
            }

            if (blendTree.blendType != BlendTreeType.FreeformDirectional2D ||
                blendTree.blendParameter != MoveXParameterName ||
                blendTree.blendParameterY != MoveYParameterName)
            {
                errors.Append(
                    "Locomotion blend tree must be 2D directional and driven by MoveX/MoveY. ");
            }

            ChildMotion[] children = blendTree.children;
            if (children.Length < 5 || children.Any(child => child.motion == null))
            {
                errors.Append(
                    "Locomotion blend tree must contain idle, forward, backward, left, and right motions. ");
            }
            else
            {
                bool hasIdle = children.Any(child => child.position.sqrMagnitude < 0.01f);
                bool hasForward = children.Any(child =>
                    child.position.y >= 0.5f && Mathf.Abs(child.position.x) < 0.5f);
                bool hasBackward = children.Any(child =>
                    child.position.y <= -0.5f && Mathf.Abs(child.position.x) < 0.5f);
                bool hasLeft = children.Any(child =>
                    child.position.x <= -0.5f && Mathf.Abs(child.position.y) < 0.5f);
                bool hasRight = children.Any(child =>
                    child.position.x >= 0.5f && Mathf.Abs(child.position.y) < 0.5f);

                if (!hasIdle || !hasForward || !hasBackward || !hasLeft || !hasRight)
                {
                    errors.Append(
                        "Locomotion blend tree is missing idle or a cardinal movement direction. ");
                }
            }

            if (stateMachine.defaultState != locomotionState)
            {
                errors.Append("Locomotion state must be the default animator state. ");
            }
        }

        private static void ValidateFloatParameter(
            AnimatorController controller,
            string parameterName,
            System.Text.StringBuilder errors)
        {
            bool exists = controller.parameters.Any(parameter =>
                parameter.name == parameterName &&
                parameter.type == AnimatorControllerParameterType.Float);

            if (!exists)
            {
                errors.Append($"Animator parameter '{parameterName}' is missing or not a float. ");
            }
        }

        private static void ValidateAttackAnimatorState(
            AnimatorController controller,
            AnimatorStateMachine stateMachine,
            string stateName,
            System.Text.StringBuilder errors)
        {
            bool hasTrigger = controller.parameters.Any(parameter =>
                parameter.name == stateName &&
                parameter.type == AnimatorControllerParameterType.Trigger);

            if (!hasTrigger)
            {
                errors.Append($"Animator trigger '{stateName}' is missing. ");
            }

            AnimatorState state = stateMachine.states
                .Select(childState => childState.state)
                .FirstOrDefault(candidate => candidate.name == stateName);

            if (state == null)
            {
                errors.Append($"Animator state '{stateName}' is missing. ");
            }
            else if (state.motion == null)
            {
                errors.Append($"Animator state '{stateName}' has no motion. ");
            }
            else if (state.transitions.Any(transition =>
                         transition.hasExitTime &&
                         transition.destinationState != null &&
                         transition.destinationState.name == LocomotionStateName))
            {
                errors.Append(
                    $"Animator state '{stateName}' has an exit-time return to locomotion. " +
                    "FighterView must be the only authority that ends a strike. ");
            }
        }

        private static void ValidateFighterPrefab(
            GameObject prefab,
            AnimatorController controller,
            System.Text.StringBuilder errors)
        {
            if (!prefab.TryGetComponent(out CharacterController _))
            {
                errors.Append("Fighter prefab has no CharacterController. ");
            }

            if (!prefab.TryGetComponent(out FighterMotor _))
            {
                errors.Append("Fighter prefab has no FighterMotor. ");
            }

            if (!prefab.TryGetComponent(out FighterView _))
            {
                errors.Append("Fighter prefab has no FighterView. ");
            }

            if (!prefab.TryGetComponent(out FighterController _))
            {
                errors.Append("Fighter prefab has no FighterController. ");
            }

            Animator animator = prefab.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                errors.Append("Fighter prefab has no Animator. ");
            }
            else if (controller != null && animator.runtimeAnimatorController != controller)
            {
                errors.Append("Fighter prefab Animator does not use the generated controller. ");
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static string ResolveAnimationPath(string preferredPath, string token)
        {
            return File.Exists(preferredPath) ? preferredPath : RequireAnimationPath(token);
        }

        private static string RequireAnimationPath(string token)
        {
            string path = AssetDatabase.FindAssets("t:Model", new[] { AnimationsFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(candidate =>
                    candidate.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) &&
                    Path.GetFileNameWithoutExtension(candidate)
                        .IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(candidate => candidate.Length)
                .FirstOrDefault();

            if (path == null)
            {
                throw new InvalidOperationException(
                    $"No Mixamo animation containing '{token}' was found in '{AnimationsFolder}'. " +
                    $"Add a {token} FBX before rebuilding fighter assets.");
            }

            return path;
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

            // Every model must build an avatar from its own skeleton. Copying an avatar from an
            // unrelated rig (for example a mixamorig7 animation onto the mixamorig5 boxer) leaves
            // the model without a valid avatar, which silently removes its Animator and leaves the
            // character in its bind pose (T-pose).
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

            SerializedProperty movementProperty = serializedDefinition.FindProperty("_movement");
            movementProperty.FindPropertyRelative("_walkSpeedMultiplier").floatValue = 0.55f;
            movementProperty.FindPropertyRelative("_rotationDegreesPerSecond").floatValue = 720f;

            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static AnimatorController CreateOrUpdateLocomotionController(
            string idlePath,
            string walkPath,
            string strafePath)
        {
            AssetDatabase.DeleteAsset(AnimatorControllerPath);

            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            EnsureParameter(controller, SpeedParameterName, AnimatorControllerParameterType.Float);
            EnsureParameter(controller, MoveXParameterName, AnimatorControllerParameterType.Float);
            EnsureParameter(controller, MoveYParameterName, AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsBlocking", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsDefeated", AnimatorControllerParameterType.Bool);

            AnimationClip idleClip = LoadAnimationClip(idlePath, "idle");
            AnimationClip walkClip = LoadAnimationClip(walkPath, "walk");
            AnimationClip strafeClip = LoadAnimationClip(strafePath, "strafe");

            EnsureLooping(idleClip, idlePath);
            EnsureLooping(walkClip, walkPath);
            EnsureLooping(strafeClip, strafePath);

            BlendTree blendTree = new BlendTree
            {
                name = BlendTreeName,
                blendType = BlendTreeType.FreeformDirectional2D,
                blendParameter = MoveXParameterName,
                blendParameterY = MoveYParameterName,
                useAutomaticThresholds = false,
                minThreshold = -1f,
                maxThreshold = 1f
            };

            AssetDatabase.AddObjectToAsset(blendTree, controller);

            blendTree.children = new[]
            {
                new ChildMotion
                {
                    motion = idleClip,
                    position = Vector2.zero,
                    timeScale = 1f,
                    directBlendParameter = "Blend"
                },
                new ChildMotion
                {
                    motion = walkClip,
                    position = Vector2.up,
                    timeScale = ForwardWalkPlayback,
                    directBlendParameter = "Blend"
                },
                new ChildMotion
                {
                    motion = walkClip,
                    position = Vector2.down,
                    timeScale = BackwardWalkPlayback,
                    directBlendParameter = "Blend"
                },
                new ChildMotion
                {
                    motion = strafeClip,
                    position = Vector2.right,
                    timeScale = StrafePlayback,
                    directBlendParameter = "Blend"
                },
                new ChildMotion
                {
                    motion = strafeClip,
                    position = Vector2.left,
                    timeScale = StrafePlayback,
                    mirror = true,
                    directBlendParameter = "Blend"
                }
            };

            AnimatorState locomotionState = stateMachine.AddState(LocomotionStateName);
            locomotionState.motion = blendTree;
            locomotionState.speed = 1f;
            stateMachine.defaultState = locomotionState;

            EditorUtility.SetDirty(blendTree);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
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

        private static AnimationClip LoadAnimationClip(string path, string token)
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__"))
                .ToArray();

            if (clips.Length == 0)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                clips = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    .Where(clip => !clip.name.StartsWith("__preview__"))
                    .ToArray();
            }

            if (clips.Length == 0)
            {
                throw new InvalidOperationException($"No animation clip found in '{path}'.");
            }

            AnimationClip clip = clips
                .OrderByDescending(c => c.length)
                .First();

            Debug.Log($"Using animation clip '{clip.name}' from '{path}'.");
            return clip;
        }

        private static void EnsureLooping(AnimationClip clip, string path)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (settings.loopTime)
            {
                return;
            }

            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            Debug.Log($"Enabled looping for '{clip.name}' from '{path}'.");
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

            root.AddComponent<HealthComponent>();
            root.AddComponent<StaminaComponent>();
            var identity = root.AddComponent<FighterIdentity>();
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
            controllerSerialized.FindProperty("_identity").objectReferenceValue = identity;
            controllerSerialized.FindProperty("_motor").objectReferenceValue = motor;
            controllerSerialized.FindProperty("_view").objectReferenceValue = view;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            var viewSerialized = new SerializedObject(view);
            viewSerialized.FindProperty("_animator").objectReferenceValue = animator;
            viewSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, FighterPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
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

            if (fighterTwo.TryGetComponent(out PlayerIntentSource opponentInput))
            {
                opponentInput.enabled = false;
            }

            var passiveIntent = fighterTwo.AddComponent<PassiveIntentSource>();
            var opponentController = fighterTwo.GetComponent<FighterController>();
            var opponentSerialized = new SerializedObject(opponentController);
            opponentSerialized.FindProperty("_intentSourceBehaviour").objectReferenceValue = passiveIntent;
            opponentSerialized.ApplyModifiedPropertiesWithoutUndo();

            var sessionSerialized = new SerializedObject(session);
            sessionSerialized.FindProperty("_tickDriver").objectReferenceValue = tickDriver;

            SerializedProperty fightersProperty = sessionSerialized.FindProperty("_fighters");
            fightersProperty.arraySize = 2;
            fightersProperty.GetArrayElementAtIndex(0).objectReferenceValue =
                fighterOne.GetComponent<FighterController>();
            fightersProperty.GetArrayElementAtIndex(1).objectReferenceValue = opponentController;
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
