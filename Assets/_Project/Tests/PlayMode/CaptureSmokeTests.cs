using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OctagonFighter.Tests.PlayMode
{
    /// <summary>
    /// Verifies that Play Mode can render the combat scene into a texture in this environment
    /// before the scripted strike scenarios depend on it.
    /// </summary>
    public sealed class CaptureSmokeTests
    {
        [UnityTest]
        public IEnumerator RendersTheCombatSceneToATexture()
        {
            string directory = Path.Combine(Path.GetTempPath(), "p41_shots");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "smoke.png");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            SceneManager.LoadScene("CombatTest", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var fighters = Object.FindObjectsByType<OctagonFighter.Presentation.Fighter.FighterController>(
                FindObjectsInactive.Exclude);
            Debug.Log($"[P41] smoke fighters found={fighters.Length}");

            Camera sceneCamera = Camera.main;
            if (sceneCamera != null)
            {
                sceneCamera.enabled = false;
            }

            var cameraObject = new GameObject("ValidationCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 1.5f, -3.4f);
            camera.transform.LookAt(new Vector3(0f, 1.05f, 0f));
            camera.fieldOfView = 42f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.13f, 0.15f, 0.19f);

            var texture = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
            texture.Create();
            camera.targetTexture = texture;

            yield return new WaitForSeconds(0.3f);
            yield return null;
            yield return null;

            RenderTexture.active = texture;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            image.Apply();
            RenderTexture.active = null;

            byte[] png = image.EncodeToPNG();
            File.WriteAllBytes(path, png);

            Color32[] pixels = image.GetPixels32();
            int lit = 0;
            for (int index = 0; index < pixels.Length; index += 37)
            {
                Color32 pixel = pixels[index];
                if (pixel.r > 12 || pixel.g > 12 || pixel.b > 12)
                {
                    lit++;
                }
            }

            int sampled = Mathf.CeilToInt(pixels.Length / 37f);
            Debug.Log($"[P41] smoke render '{path}' bytes={png.Length} lit={lit}/{sampled} " +
                $"camera={camera.name} sceneCamera={(sceneCamera != null ? sceneCamera.name : "none")}");

            camera.targetTexture = null;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(cameraObject);

            Assert.IsTrue(png.Length > 1024, "The rendered frame must contain an image.");
        }
    }
}
