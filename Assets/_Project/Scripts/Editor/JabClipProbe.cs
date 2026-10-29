using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace OctagonFighter.EditorTools
{
    /// <summary>
    /// Temporary diagnostic: plays the imported "Jab" sub-clip on the boxer rig and logs the hips
    /// height plus both fists, to find out why the runtime pose sinks into a crouch.
    /// </summary>
    public static class JabClipProbe
    {
        private const string BoxerPath = "Assets/_Project/Art/Characters/boxer.fbx";
        private const string ClipPath = "Assets/_Project/Art/Animations/Boxing_mixamo.fbx";
        private const string TempControllerPath = "Assets/__JabClipProbe.controller";

        public static void Run()
        {
            Debug.Log($"[JABP] stateHashJab={Animator.StringToHash("Jab")} locomotion={Animator.StringToHash("Locomotion")}");

            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(ClipPath).OfType<AnimationClip>().ToArray();
            foreach (AnimationClip clip in clips)
            {
                Debug.Log($"[JABP] asset clip={clip.name} len={clip.length:0.000} fps={clip.frameRate}");
            }

            AnimationClip full = clips.FirstOrDefault(c => c.name == "clip");
            AnimationClip jab = clips.FirstOrDefault(c => c.name == "Jab");
            Profile("full", full);
            Profile("jab", jab);
            AssetDatabase.DeleteAsset(TempControllerPath);
            Debug.Log("[JABP] done");
        }

        private static void Profile(string label, AnimationClip clip)
        {
            if (clip == null)
            {
                Debug.Log($"[JABP] {label} missing");
                return;
            }

            GameObject rigAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BoxerPath);
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(BoxerPath).OfType<Avatar>().FirstOrDefault();
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
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

            var csv = new StringBuilder("t,hipY,lhX,lhY,lhZ,rhY\n");
            int steps = Mathf.Max(1, Mathf.RoundToInt(clip.length * 60f));
            for (int step = 0; step <= steps; step++)
            {
                float normalized = step / (float)steps;
                animator.Play(stateHash, 0, normalized);
                animator.Update(0.0001f);
                Vector3 hip = instance.transform.InverseTransformPoint(hips.position);
                Vector3 lh = instance.transform.InverseTransformPoint(leftHand.position);
                Vector3 rh = instance.transform.InverseTransformPoint(rightHand.position);
                csv.Append((normalized * clip.length).ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(hip.y.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(lh.x.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(lh.y.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(lh.z.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(rh.y.ToString("0.000", CultureInfo.InvariantCulture))
                    .Append('\n');

                if (step % 6 == 0)
                {
                    Debug.Log(
                        $"[JABP] {label} t={normalized * clip.length:0.000} hipY={hip.y:0.000} " +
                        $"lh=({lh.x:0.000},{lh.y:0.000},{lh.z:0.000}) rhY={rh.y:0.000}");
                }
            }

            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "p41_shots", $"jabprobe_{label}.csv"),
                csv.ToString());
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
