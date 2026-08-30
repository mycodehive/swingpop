using System;
using SwingPop.AudioSystem;
using SwingPop.CameraSystem;
using SwingPop.CharacterSystem;
using SwingPop.Data;
using SwingPop.Debugging;
using SwingPop.Gameplay.Shot;
using SwingPop.Presentation;
using SwingPop.UI;
using SwingPop.VfxSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace SwingPop.Online
{
    public readonly struct ServerPresentationStats
    {
        public ServerPresentationStats(int disabledBehaviours, int disabledRenderers, int disabledParticles,
            int gameplayColliders, int activeCameras, int activeCanvases, int activeAudioSources)
        {
            DisabledBehaviours = disabledBehaviours;
            DisabledRenderers = disabledRenderers;
            DisabledParticles = disabledParticles;
            GameplayColliders = gameplayColliders;
            ActiveCameras = activeCameras;
            ActiveCanvases = activeCanvases;
            ActiveAudioSources = activeAudioSources;
        }

        public int DisabledBehaviours { get; }
        public int DisabledRenderers { get; }
        public int DisabledParticles { get; }
        public int GameplayColliders { get; }
        public int ActiveCameras { get; }
        public int ActiveCanvases { get; }
        public int ActiveAudioSources { get; }
        public bool IsHeadlessSafe => GameplayColliders > 0 && ActiveCameras == 0
                                      && ActiveCanvases == 0 && ActiveAudioSources == 0;
    }

    /// <summary>Explicit component-type policy; it never disables objects by hierarchy name.</summary>
    public static class DedicatedServerPresentationPolicy
    {
        public static bool IsPresentationBehaviour(Behaviour behaviour)
        {
            return behaviour is Camera
                or Canvas
                or AudioSource
                or AudioListener
                or Light
                or Animator
                or EventSystem
                or ShotInputController
                or TemporaryBallInput
                or CameraDirector
                or BallFollowCamera
                or CharacterGolfController
                or CharacterAnimationController
                or CharacterPresentation
                or CharacterVisualAdapter
                or GameplayHudPresenter
                or GameplayHudView
                or HudGaugeView
                or HudPopupView
                or HudResultView
                or ShotPresentationController
                or PuttResultCinematicController
                or SkyIslandAmbienceController
                or SkyIslandEnvironmentMotion
                or GameplayAudioController
                or ImpactVfxController
                or BallTrailController
                or LandingVfxController
                or HoleInVfxController
                or MultiplayerTurnPresenter
                or MultiplayerDebugOverlay
                or ShotDebugOverlay
                or BallDebugTelemetry
                or BallTrajectoryDebug
                or WindDebugInputController
                or WindDebugVisualizer
                or FoundationInputProbe;
        }

        public static ServerPresentationStats Apply()
        {
            int disabledBehaviours = 0;
            foreach (Behaviour behaviour in UnityEngine.Object.FindObjectsByType<Behaviour>(
                         FindObjectsInactive.Include))
            {
                if (behaviour == null || !behaviour.enabled || !IsPresentationBehaviour(behaviour)) continue;
                behaviour.enabled = false;
                disabledBehaviours++;
            }

            int disabledRenderers = 0;
            foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(
                         FindObjectsInactive.Include))
            {
                if (renderer == null || !renderer.enabled) continue;
                renderer.enabled = false;
                disabledRenderers++;
            }

            int disabledParticles = 0;
            foreach (ParticleSystem particle in UnityEngine.Object.FindObjectsByType<ParticleSystem>(
                         FindObjectsInactive.Include))
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                disabledParticles++;
            }

            int colliders = 0;
            foreach (Collider collider in UnityEngine.Object.FindObjectsByType<Collider>(
                         FindObjectsInactive.Include))
                if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy) colliders++;

            return new ServerPresentationStats(
                disabledBehaviours,
                disabledRenderers,
                disabledParticles,
                colliders,
                CountEnabled<Camera>(),
                CountEnabled<Canvas>(),
                CountEnabled<AudioSource>());
        }

        private static int CountEnabled<T>() where T : Behaviour
        {
            int count = 0;
            foreach (T behaviour in UnityEngine.Object.FindObjectsByType<T>(
                         FindObjectsInactive.Include))
                if (behaviour != null && behaviour.enabled && behaviour.gameObject.activeInHierarchy) count++;
            return count;
        }
    }

    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class DedicatedServerBootstrap : MonoBehaviour
    {
        public const string ParentProcessArgument = "-swingpopParentProcess=";
        [SerializeField] private MultiplayerDevelopmentSettings settings;
        private int parentProcessId;
        private float parentCheckElapsed;

        public bool IsDedicatedServer { get; private set; }
        public bool IsNoGraphics => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        public ServerPresentationStats PresentationStats { get; private set; }

        private void Awake()
        {
            NetworkLaunchOptions launch = NetworkLaunchOptions.Parse(
                Environment.GetCommandLineArgs(),
                settings != null ? settings.HostAddress : "127.0.0.1",
                settings != null ? settings.Port : (ushort)7777);
            IsDedicatedServer = launch.Mode == MultiplayerDevelopmentMode.DedicatedServer
                                || settings != null && settings.Mode == MultiplayerDevelopmentMode.DedicatedServer;
            if (!IsDedicatedServer) return;

            string parentValue = MatchReservationFile.ReadArgument(Environment.GetCommandLineArgs(),
                ParentProcessArgument);
            int.TryParse(parentValue, out parentProcessId);

            Application.runInBackground = true;
            if (settings == null || settings.DisableServerPresentation)
                PresentationStats = DedicatedServerPresentationPolicy.Apply();
            Debug.Log($"[M14][Server] Bootstrap headless={IsNoGraphics} " +
                      $"disabledBehaviours={PresentationStats.DisabledBehaviours} " +
                      $"disabledRenderers={PresentationStats.DisabledRenderers} " +
                      $"disabledParticles={PresentationStats.DisabledParticles} " +
                      $"gameplayColliders={PresentationStats.GameplayColliders}", this);
        }

        private void Update()
        {
            if (!IsDedicatedServer || parentProcessId <= 0) return;
            parentCheckElapsed += Time.unscaledDeltaTime;
            if (parentCheckElapsed < 1f) return;
            parentCheckElapsed = 0f;
            try
            {
                using System.Diagnostics.Process parent =
                    System.Diagnostics.Process.GetProcessById(parentProcessId);
                if (!parent.HasExited) return;
            }
            catch (Exception) { }
            Debug.Log("[M18][Server] Parent allocator exited; releasing local server process.", this);
            Application.Quit(0);
        }
    }
}
