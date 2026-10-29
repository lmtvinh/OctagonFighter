using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OctagonFighter.EditorTools
{
    /// <summary>
    /// One-off importer configuration that carves the lead jab out of the Boxing_mixamo routine
    /// and imports it as a Humanoid clip. Logs the resulting clip identity so the animator
    /// controller can reference it.
    /// </summary>
    public static class BoxingJabClipSetup
    {
        private const string ClipPath = "Assets/_Project/Art/Animations/Boxing_mixamo.fbx";

        public static void Run()
        {
            var importer = AssetImporter.GetAtPath(ClipPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[JABSETUP] no importer for {ClipPath}");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.clipAnimations = new[]
            {
                new ModelImporterClipAnimation
                {
                    name = "Jab",
                    firstFrame = 50,
                    lastFrame = 64,
                    loopTime = false,

                    // Without these the importer treats the authored body height as root motion
                    // and bakes it out of the pose, which leaves the fighter kneeling.
                    keepOriginalOrientation = false,
                    keepOriginalPositionY = true,
                    keepOriginalPositionXZ = false,
                },
            };
            importer.SaveAndReimport();

            foreach (AnimationClip clip in AssetDatabase
                .LoadAllAssetsAtPath(ClipPath)
                .OfType<AnimationClip>())
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out string guid, out long localId);
                Debug.Log(
                    $"[JABSETUP] clip={clip.name} len={clip.length:0.000} fps={clip.frameRate} " +
                    $"loop={clip.isLooping} guid={guid} localId={localId}");
            }

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ClipPath).OfType<Avatar>().FirstOrDefault();
            Debug.Log($"[JABSETUP] avatar={avatar?.name} human={avatar != null && avatar.isHuman} valid={avatar != null && avatar.isValid}");
            Debug.Log($"[JABSETUP] meta={File.Exists(ClipPath + ".meta")}");
        }
    }
}
