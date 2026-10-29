using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace OctagonFighter.EditorTools
{
    /// <summary>
    /// Temporary diagnostic. Re-imports the unused boxing clips as Humanoid (they were Generic, so
    /// they could not drive the Humanoid boxer rig at all) and profiles the strike hands so a real
    /// forward punch can be told apart from a lateral arm sweep.
    /// </summary>
    public static class StrikeCandidateProbe
    {
        private const string BoxerPath = "Assets/_Project/Art/Characters/boxer.fbx";
        private const string TempControllerPath = "Assets/__StrikeCandidateProbe.controller";

        private static readonly string[] Candidates =
        {
            "Assets/_Project/Art/Animations/ShadowBoxing_mixamo.fbx",
            "Assets/_Project/Art/Animations/Boxing_mixamo.fbx",
            "Assets/_Project/Art/Animations/Boxing.fbx",
            "Assets/_Project/Art/Animations/Hook.fbx",
            "Assets/_Project/Art/Animations/Center Block.fbx",
        };

        public static void Run()
        {
            foreach (string path in Candidates)
            {
                ConvertToHumanoid(path);
            }

            foreach (string path in Candidates)
            {
                Profile(path);
            }

            AssetDatabase.DeleteAsset(TempControllerPath);
            Debug.Log("[CAND] done");
        }

        public static void Revert()
        {
            foreach (string path in Candidates)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
                importer.SaveAndReimport();
                Debug.Log($"[CAND] reverted {Path.GetFileName(path)}");
            }

            Debug.Log("[CAND] revert done");
        }

        private static void ConvertToHumanoid(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.Log($"[CAND] no importer for {path}");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            Debug.Log(
                $"[CAND] imported {Path.GetFileName(path)} type={importer.animationType} " +
                $"avatar={(avatar != null ? avatar.name : "none")} human={(avatar != null && avatar.isHuman)} " +
                $"valid={(avatar != null && avatar.isValid)}");
        }

        private static void Profile(string clipPath)
        {
            GameObject rigAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BoxerPath);
            AnimationClip clip = AssetDatabase
                .LoadAllAssetsAtPath(clipPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__", StringComparison.Ordinal));
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(BoxerPath).OfType<Avatar>().FirstOrDefault();

            if (rigAsset == null || clip == null || avatar == null)
            {
                Debug.Log($"[CAND] skip {clipPath} clip={clip != null}");
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
            Transform leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);

            var csv = new StringBuilder("t,lhX,lhY,lhZ,rhX,rhY,rhZ,lArm,rArm\n");
            float maxLeftZ = float.MinValue, maxRightZ = float.MinValue;
            float leftAt = 0f, rightAt = 0f;
            float maxLeftArm = 0f, maxRightArm = 0f;
            var leftForward = new List<float>();
            var rightForward = new List<float>();

            const int Steps = 240;
            for (int step = 0; step <= Steps; step++)
            {
                float normalized = step / (float)Steps;
                animator.Play(stateHash, 0, normalized);
                animator.Update(0.0001f);

                Vector3 lh = instance.transform.InverseTransformPoint(leftHand.position);
                Vector3 rh = instance.transform.InverseTransformPoint(rightHand.position);
                float leftArm = Vector3.Distance(
                    lh,
                    instance.transform.InverseTransformPoint(leftUpperArm.position));
                float rightArm = Vector3.Distance(
                    rh,
                    instance.transform.InverseTransformPoint(rightUpperArm.position));

                float t = normalized * clip.length;
                csv.Append(t.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(lh.x.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(lh.y.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(lh.z.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(rh.x.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(rh.y.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(rh.z.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(leftArm.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(rightArm.ToString("0.000", CultureInfo.InvariantCulture))
                    .Append('\n');

                if (lh.z > maxLeftZ)
                {
                    maxLeftZ = lh.z;
                    leftAt = t;
                }

                if (rh.z > maxRightZ)
                {
                    maxRightZ = rh.z;
                    rightAt = t;
                }

                maxLeftArm = Mathf.Max(maxLeftArm, leftArm);
                maxRightArm = Mathf.Max(maxRightArm, rightArm);
                if (lh.z > 0.30f)
                {
                    leftForward.Add(t);
                }

                if (rh.z > 0.30f)
                {
                    rightForward.Add(t);
                }
            }

            string outPath = Path.Combine(
                Path.GetTempPath(),
                "p41_shots",
                "cand_" + Path.GetFileNameWithoutExtension(clipPath) + ".csv");
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllText(outPath, csv.ToString());

            Debug.Log(
                $"[CAND] {Path.GetFileName(clipPath)} len={clip.length:0.000} " +
                $"LmaxZ={maxLeftZ:0.000}@{leftAt:0.000}s armL={maxLeftArm:0.000} " +
                $"RmaxZ={maxRightZ:0.000}@{rightAt:0.000}s armR={maxRightArm:0.000} " +
                $"L>0.30={Span(leftForward)} R>0.30={Span(rightForward)} -> {outPath}");

            UnityEngine.Object.DestroyImmediate(instance);
        }

        private static string Span(List<float> times)
        {
            if (times.Count == 0)
            {
                return "none";
            }

            return $"{times.Min():0.000}-{times.Max():0.000}s({times.Count})";
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
