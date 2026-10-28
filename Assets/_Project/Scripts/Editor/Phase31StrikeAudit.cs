using System.Collections.Generic;
using System.Text;
using OctagonFighter.Presentation.Fighter;
using UnityEditor;
using UnityEngine;

namespace OctagonFighter.Editor
{
    /// <summary>
    /// Temporary audit of punch hand extension timing inside the authored strike clips.
    /// </summary>
    public static class Phase31StrikeAudit
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Fighters/PF_Fighter_Boxer.prefab";
        private const float SampleRate = 120f;

        public static void Run()
        {
            var report = new StringBuilder();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[V31S] Fighter prefab not found.");
                return;
            }

            GameObject instance = Object.Instantiate(prefab);
            instance.name = "StrikeAuditFighter";
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;

            FighterController controller = instance.GetComponent<FighterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            Animator animator = instance.GetComponentInChildren<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Update(0f);

            Transform leftHand = Find(instance.transform, "LeftHand");
            Transform rightHand = Find(instance.transform, "RightHand");
            Transform head = Find(instance.transform, "Head");
            report.AppendLine("[V31S] bones " +
                $"leftHand={leftHand != null} rightHand={rightHand != null} head={head != null}");

            if (leftHand == null || rightHand == null || head == null)
            {
                Debug.LogError("[V31S] Hand or head bones not found.");
                Object.DestroyImmediate(instance);
                return;
            }

            Audit(report, animator, instance.transform, leftHand, rightHand, head, "Jab", 1.500f, 0.120f, 0.080f, 0.200f);
            Audit(report, animator, instance.transform, leftHand, rightHand, head, "Cross", 2.233f, 0.220f, 0.100f, 0.330f);

            Debug.Log(report.ToString());
            Object.DestroyImmediate(instance);
        }

        private static void Audit(
            StringBuilder report,
            Animator animator,
            Transform root,
            Transform leftHand,
            Transform rightHand,
            Transform head,
            string state,
            float clipLength,
            float startup,
            float active,
            float recovery)
        {
            animator.speed = 1f;
            animator.Play(state, 0, 0f);
            animator.Update(0f);

            int sampleCount = Mathf.CeilToInt(clipLength * SampleRate);
            var times = new List<float>(sampleCount);
            var left = new List<Vector3>(sampleCount);
            var right = new List<Vector3>(sampleCount);
            var headPositions = new List<Vector3>(sampleCount);

            for (int index = 0; index < sampleCount; index++)
            {
                animator.Update(1f / SampleRate);
                float time = (index + 1) / SampleRate;
                times.Add(time);
                left.Add(root.InverseTransformPoint(leftHand.position));
                right.Add(root.InverseTransformPoint(rightHand.position));
                headPositions.Add(root.InverseTransformPoint(head.position));
            }

            if (times.Count == 0)
            {
                report.AppendLine($"[V31S] {state}: no samples.");
                return;
            }

            DescribeHand(report, state, "left", times, left, headPositions, clipLength, startup, active, recovery);
            DescribeHand(report, state, "right", times, right, headPositions, clipLength, startup, active, recovery);
            report.AppendLine(
                $"[V31S] {state} stateTimeline startup={startup:0.000} " +
                $"activeEnd={startup + active:0.000} stateEnd={startup + active + recovery:0.000} " +
                $"clipLength={clipLength:0.000} cut={clipLength - (startup + active + recovery):0.000}s");

            report.AppendLine(DescribeSamples(state, "left", times, left, headPositions, 1.0f));
            report.AppendLine(DescribeSamples(state, "right", times, right, headPositions, 1.0f));
        }

        private static void DescribeHand(
            StringBuilder report,
            string state,
            string hand,
            List<float> times,
            List<Vector3> positions,
            List<Vector3> headPositions,
            float clipLength,
            float startup,
            float active,
            float recovery)
        {
            float guard = (positions[0] - headPositions[0]).magnitude;
            float peak = guard;
            int peakIndex = 0;
            for (int index = 1; index < positions.Count; index++)
            {
                float distance = (positions[index] - headPositions[index]).magnitude;
                if (distance > peak)
                {
                    peak = distance;
                    peakIndex = index;
                }
            }

            float threshold = guard + ((peak - guard) * 0.9f);
            float reachStart = -1f;
            float reachEnd = -1f;
            for (int index = 0; index < positions.Count; index++)
            {
                float distance = (positions[index] - headPositions[index]).magnitude;
                bool extended = distance >= threshold;
                if (extended && reachStart < 0f)
                {
                    reachStart = times[index];
                }

                if (extended)
                {
                    reachEnd = times[index];
                }
            }

            float timeAtPeak = times[peakIndex];
            float duration = startup + active + recovery;
            string verdict;
            if (timeAtPeak < startup)
            {
                verdict = "extension before startup";
            }
            else if (timeAtPeak > startup + active)
            {
                verdict = "extension after active window";
            }
            else
            {
                verdict = "extension inside active window";
            }

            report.AppendLine(
                $"[V31S] {state} {hand} guardDist={guard:0.000} peakDist={peak:0.000} " +
                $"extend={peak - guard:0.000} peakAt={timeAtPeak:0.000}s " +
                $"reach90={reachStart:0.000}-{reachEnd:0.000}s threshold={threshold:0.000} " +
                $"stateEnd={duration:0.000}s clip={clipLength:0.000}s -> {verdict}");
        }

        private static string DescribeSamples(
            string state,
            string hand,
            List<float> times,
            List<Vector3> positions,
            List<Vector3> headPositions,
            float duration)
        {
            var samples = new StringBuilder();
            for (float time = 0f; time <= duration + 0.001f; time += 0.05f)
            {
                int index = Mathf.Clamp(
                    Mathf.RoundToInt((time * SampleRate) - 1f),
                    0,
                    positions.Count - 1);
                float distance = (positions[index] - headPositions[index]).magnitude;
                samples.Append($" {time:0.00}:d={distance:0.000}" +
                               $",p=({positions[index].x:0.000},{positions[index].y:0.000},{positions[index].z:0.000})");
            }

            return $"[V31S] {state} {hand} samples{samples}";
        }

        private static Transform Find(Transform root, string suffix)
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name.EndsWith(suffix, System.StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
