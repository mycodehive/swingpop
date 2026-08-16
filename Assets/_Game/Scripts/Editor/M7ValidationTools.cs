using System.Collections;
using System.Collections.Generic;
using SwingPop.CameraSystem;
using SwingPop.CharacterSystem;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Club;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using UnityEditor;
using UnityEngine;

namespace SwingPop.Editor
{
    public static class M7ValidationTools
    {
        [MenuItem("SwingPop/M7/Run Character Flow Validation")]
        public static void RunCharacterFlowValidation()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before running the M7 character validation.");
                return;
            }

            if (Object.FindAnyObjectByType<M7PlayModeValidationDriver>() != null)
            {
                Debug.LogWarning("M7 character validation is already running.");
                return;
            }

            CharacterGolfController character = Object.FindAnyObjectByType<CharacterGolfController>();
            CharacterAnimationController animationController = Object.FindAnyObjectByType<CharacterAnimationController>();
            GolfBallController ball = Object.FindAnyObjectByType<GolfBallController>();
            ShotFlowController shotFlow = Object.FindAnyObjectByType<ShotFlowController>();
            HoleFlowController holeFlow = Object.FindAnyObjectByType<HoleFlowController>();
            CameraDirector cameraDirector = Object.FindAnyObjectByType<CameraDirector>();
            if (character == null || animationController == null || ball == null
                || shotFlow == null || holeFlow == null || cameraDirector == null)
            {
                Debug.LogError("SWINGPOP_M7_PLAYMODE_VALIDATION_FAIL: M7 scene dependencies were not found.");
                return;
            }

            GameObject driverObject = new("M7 PlayMode Validation Driver")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            driverObject.AddComponent<M7PlayModeValidationDriver>().Begin(
                character,
                animationController,
                ball,
                shotFlow,
                holeFlow,
                cameraDirector);
        }
    }

    internal sealed class M7PlayModeValidationDriver : MonoBehaviour
    {
        private const float TimeoutSeconds = 45f;

        private readonly List<CharacterState> observedCharacterStates = new();
        private readonly List<CameraMode> observedCameraModes = new();
        private CharacterGolfController character;
        private CharacterAnimationController animationController;
        private GolfBallController ball;
        private ShotFlowController shotFlow;
        private HoleFlowController holeFlow;
        private CameraDirector cameraDirector;
        private ShotInputController inputController;
        private ClubData putter;
        private int launchCount;
        private bool finished;

        public void Begin(
            CharacterGolfController targetCharacter,
            CharacterAnimationController targetAnimation,
            GolfBallController targetBall,
            ShotFlowController targetShotFlow,
            HoleFlowController targetHoleFlow,
            CameraDirector targetCamera)
        {
            character = targetCharacter;
            animationController = targetAnimation;
            ball = targetBall;
            shotFlow = targetShotFlow;
            holeFlow = targetHoleFlow;
            cameraDirector = targetCamera;
            inputController = Object.FindAnyObjectByType<ShotInputController>();
            if (inputController != null)
            {
                inputController.enabled = false;
            }

            SerializedObject serializedHole = new(holeFlow);
            putter = serializedHole.FindProperty("putter").objectReferenceValue as ClubData;
            if (putter == null)
            {
                Fail("Putter data was not assigned.");
                return;
            }

            animationController.StateChanged += OnCharacterStateChanged;
            ball.Launched += OnBallLaunched;
            cameraDirector.ModeChanged += OnCameraModeChanged;
            StartCoroutine(RunValidation());
        }

        private void OnDestroy()
        {
            if (animationController != null)
            {
                animationController.StateChanged -= OnCharacterStateChanged;
            }
            if (ball != null)
            {
                ball.Launched -= OnBallLaunched;
            }
            if (cameraDirector != null)
            {
                cameraDirector.ModeChanged -= OnCameraModeChanged;
            }
            if (inputController != null)
            {
                inputController.enabled = true;
            }
        }

        private IEnumerator RunValidation()
        {
            holeFlow.SetAutomaticFlowSuspended(false);
            holeFlow.DebugResetHole();
            yield return null;
            cameraDirector.SkipIntro();
            yield return WaitFor(
                () => character.CurrentState == CharacterState.Address
                      && shotFlow.State == ShotFlowState.Aiming,
                "initial Character Address");
            if (finished) yield break;

            Vector3 firstAddressPosition = character.transform.position;
            float initialYaw = character.CharacterAimAngle;
            yield return ApplyAimInput(1f, 0.25f);
            if (Mathf.Abs(Mathf.DeltaAngle(initialYaw, character.CharacterAimAngle)) < 1f)
            {
                Fail("Character orientation did not follow Aim input.");
                yield break;
            }

            shotFlow.ConfirmCurrentStep();
            yield return WaitFor(
                () => shotFlow.State == ShotFlowState.PowerSelecting
                      && character.CurrentState == CharacterState.Address,
                "PowerSelecting Address hold");
            if (finished) yield break;
            yield return new WaitForSeconds(0.22f);
            shotFlow.ConfirmCurrentStep();
            yield return WaitFor(
                () => shotFlow.State == ShotFlowState.ImpactSelecting
                      && character.CurrentState == CharacterState.BackSwing,
                "ImpactSelecting BackSwing");
            if (finished) yield break;

            yield return new WaitForSeconds(0.1f);
            float committedAt = Time.time;
            shotFlow.ForcePerfectImpactAndCommit();
            yield return WaitFor(
                () => character.CurrentState == CharacterState.Swing && ball.State == BallState.Ready,
                "Swing before Ball launch");
            if (finished) yield break;
            yield return WaitFor(
                () => launchCount == 1 && character.ImpactEventFired && ball.State != BallState.Ready,
                "single Impact event Ball launch");
            if (finished) yield break;
            if (Time.time - committedAt < 0.08f || shotFlow.LastBallLaunchUsedFallback)
            {
                Fail("Ball did not wait for the primary animation Impact event.");
                yield break;
            }

            yield return WaitFor(
                () => observedCharacterStates.Contains(CharacterState.FollowThrough),
                "FollowThrough");
            if (finished) yield break;
            yield return WaitFor(
                () => observedCharacterStates.Contains(CharacterState.WatchBall),
                "WatchBall");
            if (finished) yield break;
            yield return WaitFor(
                () => ball.State == BallState.Ready && shotFlow.State == ShotFlowState.Aiming,
                "continuous next shot");
            if (finished) yield break;
            yield return new WaitForSeconds(0.35f);
            if (Vector3.Distance(firstAddressPosition, character.transform.position) < 1f
                || character.CurrentState != CharacterState.Address)
            {
                Fail("Character did not reposition to the next Ball lie and return to Address.");
                yield break;
            }
            if (launchCount != 1)
            {
                Fail($"Expected one launch for one Swing, observed {launchCount}.");
                yield break;
            }

            holeFlow.SetAutomaticFlowSuspended(true);
            TerrainSurface green = FindSurface(TerrainSurfaceType.Green);
            if (green == null)
            {
                Fail("Green surface was not found.");
                yield break;
            }

            Vector3 cup = holeFlow.Hole.CupPosition;
            Vector3 puttStart = new(cup.x, green.GetComponent<Collider>().bounds.max.y + 0.15f, cup.z - 3f);
            ball.PrepareNextShot(puttStart, green.Data);
            shotFlow.PrepareNextShot(cup - puttStart, putter);
            yield return WaitFor(
                () => character.CurrentState == CharacterState.PuttAddress
                      && character.CurrentClubVisual == "Putter",
                "Putt Address and club visual");
            if (finished) yield break;

            if (!shotFlow.TryCommitShot(0.45f, 0f))
            {
                Fail("Putt commit was rejected.");
                yield break;
            }
            yield return WaitFor(() => observedCharacterStates.Contains(CharacterState.PuttSwing), "PuttSwing");
            if (finished) yield break;
            yield return WaitFor(
                () => launchCount == 2 && ball.State == BallState.Rolling,
                "Putt Impact and rolling launch");
            if (finished) yield break;
            yield return WaitFor(
                () => observedCharacterStates.Contains(CharacterState.PuttFollowThrough),
                "PuttFollowThrough");
            if (finished) yield break;
            yield return WaitFor(() => holeFlow.State == HoleFlowState.HoleComplete, "Hole Complete");
            if (finished) yield break;
            yield return WaitFor(
                () => character.CurrentState is CharacterState.Happy
                    or CharacterState.BirdieCelebration
                    or CharacterState.EagleCelebration
                    or CharacterState.HoleInOneCelebration
                    or CharacterState.Sad,
                "result celebration");
            if (finished) yield break;
            yield return WaitFor(() => cameraDirector.CurrentMode == CameraMode.Result, "Result camera");
            if (finished) yield break;

            if (!observedCameraModes.Contains(CameraMode.Swing)
                || !observedCameraModes.Contains(CameraMode.Impact)
                || !observedCameraModes.Contains(CameraMode.BallFollow)
                || !observedCameraModes.Contains(CameraMode.Putt)
                || !observedCameraModes.Contains(CameraMode.HoleComplete))
            {
                Fail($"Camera integration modes incomplete: {string.Join(" -> ", observedCameraModes)}");
                yield break;
            }

            Complete();
        }

        private IEnumerator WaitFor(System.Func<bool> condition, string label)
        {
            float started = Time.time;
            while (!condition())
            {
                if (Time.time - started >= TimeoutSeconds)
                {
                    Fail($"{label} timed out in Character={character.CurrentState}, Shot={shotFlow.State}, Ball={ball.State}, Camera={cameraDirector.CurrentMode}.");
                    yield break;
                }
                yield return null;
            }
        }

        private IEnumerator ApplyAimInput(float input, float duration)
        {
            float started = Time.time;
            while (Time.time - started < duration)
            {
                shotFlow.SetAimInput(input);
                yield return null;
            }
            shotFlow.SetAimInput(0f);
        }

        private static TerrainSurface FindSurface(TerrainSurfaceType type)
        {
            foreach (TerrainSurface surface in Object.FindObjectsByType<TerrainSurface>())
            {
                if (surface.SurfaceType == type)
                {
                    return surface;
                }
            }
            return null;
        }

        private void OnCharacterStateChanged(CharacterState previous, CharacterState next)
        {
            if (observedCharacterStates.Count == 0 || observedCharacterStates[^1] != next)
            {
                observedCharacterStates.Add(next);
            }
        }

        private void OnBallLaunched()
        {
            launchCount++;
        }

        private void OnCameraModeChanged(CameraMode previous, CameraMode next)
        {
            if (observedCameraModes.Count == 0)
            {
                observedCameraModes.Add(previous);
            }
            if (observedCameraModes[^1] != next)
            {
                observedCameraModes.Add(next);
            }
        }

        private void Complete()
        {
            finished = true;
            Debug.Log(
                "SWINGPOP_M7_PLAYMODE_VALIDATION_PASS: "
                + $"Character {string.Join(" -> ", observedCharacterStates)}; "
                + $"Camera {string.Join(" -> ", observedCameraModes)}; "
                + "Aim rotation, delayed single Impact launch, FollowThrough/WatchBall, continuous reposition, Putt, and Celebration passed.");
            StopPlayMode();
        }

        private void Fail(string reason)
        {
            if (finished)
            {
                return;
            }
            finished = true;
            Debug.LogError($"SWINGPOP_M7_PLAYMODE_VALIDATION_FAIL: {reason}");
            StopPlayMode();
        }

        private static void StopPlayMode()
        {
            EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        }
    }
}
