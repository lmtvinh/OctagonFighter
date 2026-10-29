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
    /// Temporary diagnostic: dumps the strike hands of Boxing_mixamo at 60 Hz so a jab and a cross
    /// segment can be cut from the 17.9 s routine using measured frames instead of guesses.
    /// </summary>
    public static class BoxingProfileProbe
    {
        private const string BoxerPath = "Assets/_Project/Art/Characters/boxer.fbx";
        private const string ClipPath = "Assets/_Project/Art/Animations/Boxing_mixamo.fbx";
        private const string TempControllerPath = "Assets/__BoxingProfileProbe.controller";

        public static void Run()
        {
            GameObject rigAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BoxerPath);
            AnimationClip clip = AssetDatabase
                .LoadAllAssetsAtPath(ClipPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__", System.StringComparison.Ordinal));
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(BoxerPath).OfType<Avatar>().FirstOrDefault();

            Debug.Log($"[BP] clip={clip.name} len={clip.length:0.000} fps={clip.frameRate} avatar={avatar.name}");

            GameObject instance = UnityEngine.Object.Instantiate(rigAsset);
            Animator animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            AssetDatabase.DeleteAsset(TempControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(TempControllerPath);
            controller.AddMotion(clip);
            animator.runtimeAnimatorController = controller;
            int stateHash = Animator.StringToHash(controller.layers[0].stateMachine.states[0].state.name);

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);

            var csv = new StringBuilder("t,lhX,lhY,lhZ,rhX,rhY,rhZ,lArm,rArm,chestYaw\n");
            int steps = Mathf.RoundToInt(clip.length * 60f);
            for (int step = 0; step <= steps; step++)
            {
                float normalized = step / (float)steps;
                animator.Play(stateHash, 0, normalized);
                animator.Update(0.0001f);

                Vector3 lh = instance.transform.InverseTransformPoint(leftHand.position);
                Vector3 rh = instance.transform.InverseTransformPoint(rightHand.position);
                float lArm = Vector3.Distance(lh, instance.transform.InverseTransformPoint(leftUpperArm.position));
                float rArm = Vector3.Distance(rh, instance.transform.InverseTransformPoint(rightUpperArm.position));
                Vector3 chestForward = instance.transform.InverseTransformDirection(chest.forward);
                float chestYaw = Mathf.Atan2(chestForward.x, chestForward.z) * Mathf.Rad2Deg;

                csv.Append((normalized * clip.length).ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(lh.x.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(lh.y.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(lh.z.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(rh.x.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(rh.y.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(rh.z.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(lArm.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(rArm.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(chestYaw.ToString("0.0", CultureInfo.InvariantCulture))
                    .Append('\n');
            }

            string outPath = Path.Combine(Path.GetTempPath(), "p41_shots", "boxing60.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllText(outPath, csv.ToString());
            Debug.Log($"[BP] wrote {outPath} rows={steps + 1}");

            UnityEngine.Object.DestroyImmediate(instance);
            AssetDatabase.DeleteAsset(TempControllerPath);
            Debug.Log("[BP] done");
        }
    }
}
