using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using OctagonFighter.Domain.Combat;
using OctagonFighter.Domain.StateMachine;
using OctagonFighter.Infrastructure.Combat;
using OctagonFighter.Presentation.Fighter;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OctagonFighter.Tests.PlayMode
{
    /// <summary>
    /// Loads the real combat scene, drives the player through the real input source, and records
    /// per-frame state plus rendered frames so strike behaviour can be inspected visually.
    /// </summary>
    /// <remarks>
    /// The rig repositions the two shipped fighters at runtime instead of editing the scene, so the
    /// committed scene stays untouched. Frames are rendered into a RenderTexture because
    /// <see cref="ScreenCapture"/> does not write files in batch mode.
    /// </remarks>
    /// <remarks>
    /// Timing is driven by a fixed capture clock (<see cref="Time.captureDeltaTime"/>), so each
    /// rendered frame advances the simulation by exactly one <see cref="TickSeconds"/> step no
    /// matter how long the frame took in wall-clock time. Without this, a PNG readback/encode stall
    /// (0.2-0.3 s in batch mode) would make <see cref="Time.time"/> jump, skipping contact windows
    /// and shifting damage timestamps. PNG files are encoded on the frame but written to disk on
    /// background tasks after the measurement, so file I/O never runs on the simulation path.
    /// </remarks>
    public abstract class CombatSceneFixture
    {
        protected const float TickSeconds = 1f / 60f;

        protected FighterController Attacker { get; private set; }
        protected FighterController Defender { get; private set; }
        protected FighterStrikeRig Rig { get; private set; }
        protected Animator AttackerAnimator { get; private set; }
        protected Animator DefenderAnimator { get; private set; }
        protected string ShotDirectory { get; private set; }
        protected Keyboard ValidationKeyboard => _keyboard;

        private RenderTexture _texture;
        private Texture2D _image;
        private GameObject _cameraObject;
        private Camera _sceneCamera;
        private Camera _validationCamera;
        private Keyboard _keyboard;
        private InputSettings.BackgroundBehavior _previousBackgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode _previousEditorInputBehavior;
        private float _previousFixedDeltaTime;
        private float _previousCaptureDeltaTime;
        private readonly StringBuilder _csv = new StringBuilder();
        private readonly List<(float Delay, int Amount)> _damageEvents = new List<(float, int)>();
        private readonly List<Task> _pendingShotWrites = new List<Task>();
        private int _shotIndex;
        private float _nextShotTime;
        private string _scenario;
        private float _scenarioStart = -1f;
        private float _health;
        private FighterStateId _previousState;

        /// <summary>
        /// Gets the simulation seconds elapsed since the current scenario started. With the fixed
        /// capture clock active this advances by exactly one tick per rendered frame, so it is not
        /// affected by wall-clock stalls (PNG readback, GC, disk).
        /// </summary>
        protected float Elapsed => _scenarioStart < 0f ? 0f : Time.time - _scenarioStart;

        /// <summary>Gets the health drops observed in the current scenario, with their delay from the last attack start.</summary>
        protected IReadOnlyList<(float Delay, int Amount)> DamageEvents => _damageEvents;

        /// <summary>Gets the state at which the current strike state was entered.</summary>
        protected float AttackStartedElapsed { get; private set; } = -1f;

        protected float TimeSinceAttackStart => AttackStartedElapsed < 0f
            ? -1f
            : Elapsed - AttackStartedElapsed;

        /// <summary>Gets the smallest fist-to-capsule gap observed in the current scenario.</summary>
        protected float MinimumFistGap { get; private set; } = float.NaN;

        /// <summary>Gets the scenario time at which <see cref="MinimumFistGap"/> was observed.</summary>
        protected float MinimumFistGapTime { get; private set; } = -1f;

        [UnitySetUp]
        public IEnumerator LoadCombatScene()
        {
            _previousFixedDeltaTime = Time.fixedDeltaTime;
            _previousCaptureDeltaTime = Time.captureDeltaTime;
            Time.fixedDeltaTime = TickSeconds;
            Time.captureDeltaTime = TickSeconds;
            Time.timeScale = 1f;

            // A batch-mode player is never focused, so the input system disables its devices and
            // drops every queued key event. Ignoring focus keeps the virtual keyboard alive.
            _previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

            SceneManager.LoadScene("CombatTest", LoadSceneMode.Single);
            yield return null;
            yield return null;
            yield return null;

            FighterController[] fighters =
                UnityEngine.Object.FindObjectsByType<FighterController>(FindObjectsInactive.Exclude);
            Assert.AreEqual(2, fighters.Length, "CombatTest must contain exactly two fighters.");
            foreach (FighterController fighter in fighters)
            {
                if (fighter.gameObject.name.Contains("Player"))
                {
                    Attacker = fighter;
                }
                else if (fighter.gameObject.name.Contains("Opponent"))
                {
                    Defender = fighter;
                }
            }

            Assert.IsNotNull(Attacker, "Fighter_Player was not found.");
            Assert.IsNotNull(Defender, "Fighter_Opponent was not found.");
            Assert.IsTrue(Attacker.IsInitialized, "The player fighter did not initialize.");
            Assert.IsTrue(Defender.IsInitialized, "The opponent fighter did not initialize.");

            Rig = Attacker.GetComponent<FighterStrikeRig>();
            Assert.IsNotNull(Rig, "FighterStrikeRig must be attached by FighterController.");
            AttackerAnimator = Attacker.GetComponentInChildren<Animator>();
            DefenderAnimator = Defender.GetComponentInChildren<Animator>();
            Assert.IsNotNull(AttackerAnimator, "The attacker must have an animator.");
            Assert.IsTrue(AttackerAnimator.isHuman, "The attacker rig must be a humanoid.");

            ShotDirectory = Path.Combine(Path.GetTempPath(), "p41_shots");
            Directory.CreateDirectory(ShotDirectory);

            _sceneCamera = Camera.main;
            if (_sceneCamera != null)
            {
                _sceneCamera.enabled = false;
            }

            _cameraObject = new GameObject("ValidationCamera");
            Camera camera = _cameraObject.AddComponent<Camera>();
            _validationCamera = camera;
            camera.transform.position = new Vector3(0f, 1.45f, -2.9f);
            camera.transform.LookAt(new Vector3(0f, 1.05f, 0f));
            camera.fieldOfView = 42f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.13f, 0.15f, 0.19f);

            _texture = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
            _texture.Create();
            camera.targetTexture = _texture;
            _image = new Texture2D(_texture.width, _texture.height, TextureFormat.RGBA32, false);

            _keyboard = InputSystem.GetDevice<Keyboard>();
            if (_keyboard == null)
            {
                _keyboard = InputSystem.AddDevice<Keyboard>("ValidationKeyboard");
            }

            InputSystem.EnableDevice(_keyboard);
            yield return null;
            Debug.Log(
                $"[P41] input keyboard={_keyboard.name} enabled={_keyboard.enabled} " +
                $"keys={_keyboard.allKeys.Count} background={InputSystem.settings.backgroundBehavior} " +
                $"editorInput={InputSystem.settings.editorInputBehaviorInPlayMode}");

            _health = Defender.Snapshot.CurrentHealth;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (_validationCamera != null)
            {
                _validationCamera.targetTexture = null;
            }

            if (_texture != null)
            {
                _texture.Release();
            }

            if (_image != null)
            {
                UnityEngine.Object.DestroyImmediate(_image);
            }

            if (_texture != null)
            {
                UnityEngine.Object.DestroyImmediate(_texture);
            }

            if (_cameraObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_cameraObject);
            }

            if (_sceneCamera != null)
            {
                _sceneCamera.enabled = true;
            }

            FlushShotWrites();
            Time.captureDeltaTime = _previousCaptureDeltaTime;
            Time.fixedDeltaTime = _previousFixedDeltaTime;
            InputSystem.settings.backgroundBehavior = _previousBackgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = _previousEditorInputBehavior;
            yield return null;
        }

        /// <summary>Places the fighters a fixed distance apart, facing each other by default.</summary>
        protected void PlaceFighters(float distance, float attackerYawOffsetDegrees = 0f)
        {
            Attacker.transform.position = new Vector3(-distance * 0.5f, 0f, 0f);
            Defender.transform.position = new Vector3(distance * 0.5f, 0f, 0f);
            Attacker.transform.rotation = Quaternion.Euler(0f, attackerYawOffsetDegrees, 0f) *
                Quaternion.LookRotation(Vector3.right, Vector3.up);
            Defender.transform.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
            Physics.SyncTransforms();
        }

        /// <summary>Waits until both fighters stand still in idle, facing each other.</summary>
        protected IEnumerator Settle(float seconds = 0.6f)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
            {
                yield return null;
            }

            Assert.AreEqual(
                FighterStateId.Idle,
                Attacker.Snapshot.State,
                "The player must be idle before the scenario starts.");
        }

        protected void BeginScenario(string name)
        {
            _scenario = name;
            _shotIndex = 0;
            _scenarioStart = Time.time;
            _csv.Clear();
            _csv.AppendLine(
                "elapsed,state,health,yaw,posX,posZ,leftX,leftY,leftZ,clearance," +
                "rightX,rightY,rightZ,animStateHash,animNormalized,defenderX,defenderZ");
            _damageEvents.Clear();
            _health = Defender.Snapshot.CurrentHealth;
            AttackStartedElapsed = -1f;
            MinimumFistGap = float.NaN;
            MinimumFistGapTime = -1f;
            _previousState = Attacker.Snapshot.State;
        }

        protected void EndScenario()
        {
            if (string.IsNullOrEmpty(_scenario))
            {
                return;
            }

            string path = Path.Combine(ShotDirectory, _scenario + ".csv");
            File.WriteAllText(path, _csv.ToString());

            // Measurement is over, so it is safe to block here until every frame captured during
            // the scenario has reached disk. Assertions run after EndScenario, which means the
            // diagnostic images for a failing scenario are guaranteed to exist.
            FlushShotWrites();

            Debug.Log($"[P41] scenario '{_scenario}' log -> {path} ({_csv.Length} chars)");
            _scenarioStart = -1f;
        }

        /// <summary>Presses a key through the virtual device and waits for it to be processed.</summary>
        protected IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
        }

        protected IEnumerator Hold(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            yield return null;
        }

        protected IEnumerator Release(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
        }

        /// <summary>
        /// Samples state every frame for the given wall-clock duration. When
        /// <paramref name="captureWindow"/> is set, rendered frames are written at the requested
        /// interval inside that window, measured from the scenario start.
        /// </summary>
        protected IEnumerator Sample(
            float seconds,
            Vector2 captureWindow = default,
            float captureInterval = 0.05f,
            Action<float> perFrame = null)
        {
            bool capture = captureWindow.y > captureWindow.x;
            _nextShotTime = captureWindow.x;
            float end = Elapsed + seconds;

            while (Elapsed < end)
            {
                yield return null;
                float delta = Time.deltaTime;
                perFrame?.Invoke(delta);
                Record();
                if (capture && Elapsed <= captureWindow.y && Elapsed >= _nextShotTime)
                {
                    _nextShotTime += captureInterval;
                    Capture();
                }
            }
        }

        /// <summary>Samples until the player leaves the given state or the timeout elapses.</summary>
        protected IEnumerator SampleUntilStateChanges(
            FighterStateId state,
            float timeoutSeconds,
            Vector2 captureWindow = default,
            float captureInterval = 0.05f)
        {
            bool capture = captureWindow.y > captureWindow.x;
            _nextShotTime = captureWindow.x;
            float deadline = Elapsed + timeoutSeconds;

            while (Elapsed < deadline && Attacker.Snapshot.State == state)
            {
                yield return null;
                Record();
                if (capture && Elapsed <= captureWindow.y && Elapsed >= _nextShotTime)
                {
                    _nextShotTime += captureInterval;
                    Capture();
                }
            }
        }

        protected IEnumerator WaitForState(FighterStateId state, float timeoutSeconds)
        {
            float deadline = Time.time + timeoutSeconds;
            while (Time.time < deadline && Attacker.Snapshot.State != state)
            {
                yield return null;
            }

            Assert.AreEqual(state, Attacker.Snapshot.State, $"Timed out waiting for {state}.");
        }

        /// <summary>Waits for the strike state to finish and the fighter to be idle again.</summary>
        protected IEnumerator WaitForIdle(float timeoutSeconds = 1.5f)
        {
            float deadline = Time.time + timeoutSeconds;
            while (Time.time < deadline && Attacker.Snapshot.State != FighterStateId.Idle)
            {
                yield return null;
            }

            Assert.AreEqual(
                FighterStateId.Idle,
                Attacker.Snapshot.State,
                "The fighter must return to idle after the strike.");
        }

        protected float AttackerYaw
        {
            get
            {
                Vector3 forward = Attacker.transform.forward;
                return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            }
        }

        protected static float YawOf(Transform target)
        {
            Vector3 forward = target.forward;
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        private void Record()
        {
            Rig.TryGetHand(StrikeHand.Left, out Transform left);
            Rig.TryGetHand(StrikeHand.Right, out Transform right);
            AnimatorStateInfo info = AttackerAnimator.GetCurrentAnimatorStateInfo(0);

            FighterStateId state = Attacker.Snapshot.State;
            bool isAttack = IsAttackState(state);
            bool wasAttack = IsAttackState(_previousState);
            if (isAttack && !wasAttack)
            {
                AttackStartedElapsed = Elapsed;
            }

            _previousState = state;

            float health = Defender.Snapshot.CurrentHealth;
            if (health < _health - 0.001f)
            {
                _damageEvents.Add((
                    AttackStartedElapsed < 0f ? -1f : Elapsed - AttackStartedElapsed,
                    Mathf.RoundToInt(_health - health)));
            }

            _health = health;

            float clearance = HandClearance();
            if (float.IsFinite(clearance) && (float.IsNaN(MinimumFistGap) || clearance < MinimumFistGap))
            {
                MinimumFistGap = clearance;
                MinimumFistGapTime = Elapsed;
            }

            _csv.Append(Elapsed.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                .Append(state).Append(',')
                .Append(health.ToString("0.#", CultureInfo.InvariantCulture)).Append(',')
                .Append(AttackerYaw.ToString("0.0", CultureInfo.InvariantCulture)).Append(',')
                .Append(Attacker.transform.position.x.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                .Append(Attacker.transform.position.z.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                .Append(Format(left, 'x')).Append(',')
                .Append(Format(left, 'y')).Append(',')
                .Append(Format(left, 'z')).Append(',')
                .Append(clearance.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                .Append(Format(right, 'x')).Append(',')
                .Append(Format(right, 'y')).Append(',')
                .Append(Format(right, 'z')).Append(',')
                .Append(info.fullPathHash).Append(',')
                .Append(info.normalizedTime.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                .Append(Defender.transform.position.x.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                .Append(Defender.transform.position.z.ToString("0.000", CultureInfo.InvariantCulture))
                .AppendLine();
        }

        private static string Format(Transform hand, char axis)
        {
            if (hand == null)
            {
                return "nan";
            }

            float value = axis == 'x' ? hand.position.x : axis == 'y' ? hand.position.y : hand.position.z;
            return value.ToString("0.000", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the smallest planar gap between either fist and the defender's collision capsule.
        /// A negative value means a fist is inside the defender's capsule.
        /// </summary>
        protected float HandClearance()
        {
            if (Defender == null)
            {
                return float.NaN;
            }

            Vector3 defender = Defender.transform.position;
            float best = float.MaxValue;
            if (Rig.TryGetHand(StrikeHand.Left, out Transform left))
            {
                best = Mathf.Min(best, PlanarDistance(left.position, defender));
            }

            if (Rig.TryGetHand(StrikeHand.Right, out Transform right))
            {
                best = Mathf.Min(best, PlanarDistance(right.position, defender));
            }

            return best == float.MaxValue ? float.NaN : best - 0.3f;
        }

        private void Capture()
        {
            // Readback and encoding must happen on the rendered frame, but they are pure image
            // work: no file system calls run on the simulation path. The encoded PNG bytes are
            // owned by the queued task, so the buffer stays valid until the write completes.
            RenderTexture.active = _texture;
            _image.ReadPixels(new Rect(0, 0, _texture.width, _texture.height), 0, 0);
            _image.Apply();
            RenderTexture.active = null;

            string name = $"{_scenario}_{_shotIndex:000}_{Elapsed:0.00}s.png";
            string path = Path.Combine(ShotDirectory, name);
            byte[] png = _image.EncodeToPNG();
            _shotIndex++;
            _pendingShotWrites.Add(Task.Run(() => File.WriteAllBytes(path, png)));
        }

        /// <summary>
        /// Waits for every queued PNG write to finish. Called after measurement (scenario end) and
        /// during teardown, never inside the sampling loop.
        /// </summary>
        private void FlushShotWrites()
        {
            if (_pendingShotWrites.Count == 0)
            {
                return;
            }

            Task[] pending = _pendingShotWrites.ToArray();
            _pendingShotWrites.Clear();
            Task.WaitAll(pending);
        }

        private static bool IsAttackState(FighterStateId state)
        {
            return state == FighterStateId.LightPunch || state == FighterStateId.HeavyPunch;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt((dx * dx) + (dz * dz));
        }
    }
}
