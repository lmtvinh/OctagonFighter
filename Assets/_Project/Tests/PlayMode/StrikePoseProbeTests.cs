using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace OctagonFighter.Tests.PlayMode
{
    /// <summary>
    /// Records where the striking bones actually travel, in both root space and hips space, so a
    /// strike that misses can be attributed to the clip orientation, the root twist, or the reach
    /// envelope instead of being guessed at.
    /// </summary>
    public sealed class StrikePoseProbeTests : CombatSceneFixture
    {
        [UnityTest]
        public IEnumerator RecordsJabBoneTrajectory()
        {
            PlaceFighters(1.05f);
            yield return Settle(0.8f);

            Transform hips = AttackerAnimator.GetBoneTransform(HumanBodyBones.Hips);
            Transform chest = AttackerAnimator.GetBoneTransform(HumanBodyBones.Chest);
            Transform leftHand = AttackerAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = AttackerAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform leftShoulder = AttackerAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Assert.IsNotNull(hips, "The rig must expose hips.");
            Assert.IsNotNull(leftHand, "The rig must expose the left hand.");

            var csv = new StringBuilder();
            csv.AppendLine(
                "elapsed,state,rootYaw,hipsYaw,chestYaw,hipsLocalLX,hipsLocalLY,hipsLocalLZ," +
                "rootLocalLX,rootLocalLY,rootLocalLZ,hipsLocalRX,hipsLocalRY,hipsLocalRZ," +
                "shoulderX,shoulderZ,handX,handZ,handToDefender,animNorm");

            float start = Time.time;
            BeginScenario("pose_probe");
            yield return Press(Key.J);

            float end = Time.time + 1.0f;
            while (Time.time < end)
            {
                yield return null;
                Vector3 hipsLocal = hips.InverseTransformPoint(leftHand.position);
                Vector3 rootLocal = Attacker.transform.InverseTransformPoint(leftHand.position);
                Vector3 hipsLocalRight = hips.InverseTransformPoint(rightHand.position);
                Vector3 shoulder = leftShoulder.position;
                Vector3 hand = leftHand.position;
                AnimatorStateInfo info = AttackerAnimator.GetCurrentAnimatorStateInfo(0);
                csv.Append((Time.time - start).ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(Attacker.Snapshot.State).Append(',')
                    .Append(YawOf(Attacker.transform).ToString("0.0", CultureInfo.InvariantCulture)).Append(',')
                    .Append(YawOf(hips).ToString("0.0", CultureInfo.InvariantCulture)).Append(',')
                    .Append(YawOf(chest).ToString("0.0", CultureInfo.InvariantCulture)).Append(',')
                    .Append(hipsLocal.x.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(hipsLocal.y.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(hipsLocal.z.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(rootLocal.x.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(rootLocal.y.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(rootLocal.z.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(hipsLocalRight.x.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(hipsLocalRight.y.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(hipsLocalRight.z.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(shoulder.x.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(shoulder.z.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(hand.x.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(hand.z.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(Vector2.Distance(
                        new Vector2(hand.x, hand.z),
                        new Vector2(Defender.transform.position.x, Defender.transform.position.z))
                        .ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(info.normalizedTime.ToString("0.000", CultureInfo.InvariantCulture))
                    .AppendLine();
            }

            EndScenario();
            string path = Path.Combine(ShotDirectory, "pose_probe.csv");
            File.WriteAllText(path, csv.ToString());
            Debug.Log($"[P41] pose probe -> {path} ({csv.Length} chars)");

            yield return WaitForIdle();
        }
    }
}
