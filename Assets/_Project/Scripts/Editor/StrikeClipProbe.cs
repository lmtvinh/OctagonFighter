using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace OctagonFighter.EditorTools
{
    /// <summary>
    /// Editor-only diagnostic that replays a strike clip on its own source rig and on the boxer rig,
    /// logging the striking hand's displacement in each rig's local space. It answers whether a
    /// missing strike comes from the clip itself or from humanoid retargeting onto the boxer avatar.
    /// </summary>
    public static class StrikeClipProbe
    {
        private const string BoxerPath = "Assets/_Project/Art/Characters/boxer.fbx";
        private const string JabPath = "Assets/_Project/Art/Animations/Lead Jab.fbx";
        private const string CrossPath = "Assets/_Project/Art/Animations/Jab Cross.fbx";
        private const string TempControllerPath = "Assets/__StrikeClipProbe.controller";

        public static void Run()
        {
            Sweep(BoxerPath, "Assets/_Project/Art/Animations/Lead Jab.fbx");
            Sweep(BoxerPath, "Assets/_Project/Art/Animations/Jab Cross.fbx");
            Sweep(BoxerPath, "Assets/_Project/Art/Animations/Boxing.fbx");
            Sweep(BoxerPath, "Assets/_Project/Art/Animations/Boxing_mixamo.fbx");
            Sweep(BoxerPath, "Assets/_Project/Art/Animations/ShadowBoxing_mixamo.fbx");
            Sweep(BoxerPath, "Assets/_Project/Art/Animations/Hook.fbx");
            Sweep(BoxerPath, "Assets/_Project/Art/Animations/Center Block.fbx");
            Sweep(BoxerPath, "Assets/_Project/Art/Animations/Boxing Idle.fbx");
            AssetDatabase.DeleteAsset(TempControllerPath);
            Debug.Log("[PROBE] done");
        }

        /// <summary>
        /// Summarizes how far each fist actually travels forward from the rig root, which is the
        /// number that decides whether a strike can reach a target at all.
        /// </summary>
        private static void Sweep(string rigPath, string clipPath)
        {
            GameObject rigAsset = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
            AnimationClip clip = AssetDatabase
                .LoadAllAssetsAtPath(clipPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__", StringComparison.Ordinal));
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(rigPath).OfType<Avatar>().FirstOrDefault();
            if (rigAsset == null || clip == null || avatar == null)
            {
                Debug.Log($"[PROBE] sweep skip {clipPath}");
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(rigAsset);
            Animator animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            AnimatorController controller = BuildController(clip);
            animator.runtimeAnimatorController = controller;
            int stateHash = Animator.StringToHash(controller.layers[0].stateMachine.states[0].state.name);

            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);

            float maxLeftZ = float.MinValue, maxRightZ = float.MinValue;
            float maxLeftArm = 0f, maxRightArm = 0f;
            float leftAt = 0f, rightAt = 0f;

            for (int step = 0; step <= 120; step++)
            {
                float normalized = step / 120f;
                animator.Play(stateHash, 0, normalized);
                animator.Update(0.0001f);
                Vector3 lh = instance.transform.InverseTransformPoint(leftHand.position);
                Vector3 rh = instance.transform.InverseTransformPoint(rightHand.position);
                float leftArm = Vector3.Distance(lh, instance.transform.InverseTransformPoint(leftShoulder.position));
                float rightArm = Vector3.Distance(rh, instance.transform.InverseTransformPoint(rightShoulder.position));
                if (lh.z > maxLeftZ)
                {
                    maxLeftZ = lh.z;
                    leftAt = normalized * clip.length;
                }

                if (rh.z > maxRightZ)
                {
                    maxRightZ = rh.z;
                    rightAt = normalized * clip.length;
                }

                maxLeftArm = Mathf.Max(maxLeftArm, leftArm);
                maxRightArm = Mathf.Max(maxRightArm, rightArm);
            }

            Debug.Log(
                $"[PROBE] sweep {System.IO.Path.GetFileName(clipPath)} len={clip.length:0.000} " +
                $"leftMaxZ={maxLeftZ:0.000}@{leftAt:0.000}s armL={maxLeftArm:0.000} " +
                $"rightMaxZ={maxRightZ:0.000}@{rightAt:0.000}s armR={maxRightArm:0.000}");

            UnityEngine.Object.DestroyImmediate(instance);
        }

        private static void Probe(string rigPath, string clipPath, string label)
        {
            GameObject rigAsset = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
            AnimationClip clip = AssetDatabase
                .LoadAllAssetsAtPath(clipPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__", StringComparison.Ordinal));
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(rigPath).OfType<Avatar>().FirstOrDefault();

            if (rigAsset == null || clip == null || avatar == null)
            {
                Debug.Log($"[PROBE] {label} missing rig={rigAsset != null} clip={clip != null} avatar={avatar != null}");
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(rigAsset);
            instance.name = "Probe_" + label;
            instance.SetActive(true);
            Animator animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            AnimatorController controller = BuildController(clip);
            animator.runtimeAnimatorController = controller;
            int stateHash = Animator.StringToHash(controller.layers[0].stateMachine.states[0].state.name);

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

            var csv = new StringBuilder();
            csv.AppendLine("t,hipZ,lhX,lhY,lhZ,rhX,rhY,rhZ,lShoulderX,lShoulderZ,armLength");
            float duration = clip.length;
            for (int step = 0; step <= 60; step++)
            {
                float normalized = step / 60f;
                animator.Play(stateHash, 0, normalized);
                animator.Update(0.0001f);
                if (step == 15)
                {
                    AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                    Debug.Log(
                        $"[PROBE] {label} state={info.fullPathHash} normalized={info.normalizedTime:0.000} " +
                        $"speed={animator.speed} enabled={animator.enabled}");
                }
                Vector3 lh = instance.transform.InverseTransformPoint(leftHand.position);
                Vector3 rh = instance.transform.InverseTransformPoint(rightHand.position);
                Vector3 ls = instance.transform.InverseTransformPoint(leftShoulder.position);
                Vector3 hip = instance.transform.InverseTransformPoint(hips.position);
                csv.Append((normalized * duration).ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(hip.z.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(lh.x.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(lh.y.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(lh.z.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(rh.x.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(rh.y.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(rh.z.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(ls.x.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(ls.z.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(Vector3.Distance(lh, ls).ToString("0.000", CultureInfo.InvariantCulture))
                    .AppendLine();
            }

            string outPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "p41_shots",
                label + ".csv");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outPath));
            System.IO.File.WriteAllText(outPath, csv.ToString());

            Debug.Log(
                $"[PROBE] {label} rig={rigAsset.name} clip={clip.name} len={duration:0.000} " +
                $"avatar={avatar.name} human={avatar.isHuman} -> {outPath}");
            Debug.Log(
                $"[PROBE] {label} bones LUpperArm={avatar.humanDescription.human.FirstOrDefault(h => h.humanName == "LeftUpperArm").boneName} " +
                $"LLowerArm={avatar.humanDescription.human.FirstOrDefault(h => h.humanName == "LeftLowerArm").boneName} " +
                $"LHand={avatar.humanDescription.human.FirstOrDefault(h => h.humanName == "LeftHand").boneName} " +
                $"RUpperArm={avatar.humanDescription.human.FirstOrDefault(h => h.humanName == "RightUpperArm").boneName}");

            UnityEngine.Object.DestroyImmediate(instance);
        }

        private static AnimatorController BuildController(AnimationClip clip)
        {
            AssetDatabase.DeleteAsset(TempControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(TempControllerPath);
            controller.AddMotion(clip);
            return controller;
        }
    }
}
