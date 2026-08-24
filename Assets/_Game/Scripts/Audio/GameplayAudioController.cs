using SwingPop.CharacterSystem;
using SwingPop.Data;
using SwingPop.Gameplay.Ball;
using SwingPop.Gameplay.Course;
using SwingPop.Gameplay.Hole;
using SwingPop.Gameplay.Shot;
using SwingPop.Presentation;
using UnityEngine;

namespace SwingPop.AudioSystem
{
    public sealed class GameplayAudioController : MonoBehaviour
    {
        [Header("Gameplay Sources")]
        [SerializeField] private ShotFlowController shotFlow;
        [SerializeField] private GolfBallController ball;
        [SerializeField] private HoleFlowController holeFlow;
        [SerializeField] private CharacterAnimationController characterAnimation;
        [SerializeField] private Transform characterTransform;

        [Header("Audio")]
        [SerializeField] private ShotPresentationTuningData tuning;
        [SerializeField] private PuttResultCinematicTuningData cinematicTuning;
        [SerializeField] private AudioSource swingSource;
        [SerializeField] private AudioSource impactSource;
        [SerializeField] private AudioSource terrainSource;
        [SerializeField] private AudioSource uiResultSource;

        private readonly AudioClip[] generatedClips = new AudioClip[System.Enum.GetValues(typeof(GameplayAudioCue)).Length];
        private readonly int[] cueCounts = new int[System.Enum.GetValues(typeof(GameplayAudioCue)).Length];

        public int TotalCueCount { get; private set; }
        public int GeneratedFallbackCount { get; private set; }
        public GameplayAudioCue LastCue { get; private set; }
        public PuttResultCinematicTuningData CinematicTuning => cinematicTuning;

        private void Awake()
        {
            BuildFallbackLibrary();
        }

        private void OnEnable()
        {
            if (shotFlow != null)
            {
                shotFlow.StateChanged += OnShotStateChanged;
            }
            if (characterAnimation != null)
            {
                characterAnimation.StateChanged += OnCharacterStateChanged;
            }
            if (ball != null)
            {
                ball.Launched += OnBallLaunched;
                ball.SurfaceContacted += OnSurfaceContacted;
                ball.HazardEntered += OnHazardEntered;
            }
            if (holeFlow != null)
            {
                holeFlow.HoleCompleted += OnHoleCompleted;
            }
        }

        private void OnDisable()
        {
            if (shotFlow != null)
            {
                shotFlow.StateChanged -= OnShotStateChanged;
            }
            if (characterAnimation != null)
            {
                characterAnimation.StateChanged -= OnCharacterStateChanged;
            }
            if (ball != null)
            {
                ball.Launched -= OnBallLaunched;
                ball.SurfaceContacted -= OnSurfaceContacted;
                ball.HazardEntered -= OnHazardEntered;
            }
            if (holeFlow != null)
            {
                holeFlow.HoleCompleted -= OnHoleCompleted;
            }
        }

        private void OnDestroy()
        {
            for (int index = 0; index < generatedClips.Length; index++)
            {
                if (generatedClips[index] != null)
                {
                    Destroy(generatedClips[index]);
                    generatedClips[index] = null;
                }
            }
        }

        public int GetCueCount(GameplayAudioCue cue)
        {
            return cueCounts[(int)cue];
        }

        public void Preview(GameplayAudioCue cue)
        {
            PlayCue(cue, transform.position, 1f);
        }

        private void OnShotStateChanged(ShotFlowState previous, ShotFlowState next)
        {
            if (next is ShotFlowState.PowerSelecting or ShotFlowState.ImpactSelecting)
            {
                PlayCue(GameplayAudioCue.UiConfirm, transform.position, tuning != null ? tuning.UiVolume : 0.35f);
            }
        }

        private void OnCharacterStateChanged(CharacterState previous, CharacterState next)
        {
            if (next == CharacterState.Swing)
            {
                PlayCue(GameplayAudioCue.Swing, CharacterPosition(), tuning != null ? tuning.SwingVolume : 0.55f);
            }
            else if (next == CharacterState.PuttSwing)
            {
                PlayCue(GameplayAudioCue.PuttSwing, CharacterPosition(), tuning != null ? tuning.SwingVolume * 0.7f : 0.4f);
            }
        }

        private void OnBallLaunched()
        {
            if (shotFlow == null || !shotFlow.HasLastShotCommand)
            {
                return;
            }

            ShotCommand command = shotFlow.LastShotCommand;
            float volume = tuning != null ? tuning.ImpactVolume : 0.72f;
            if (tuning != null && command.ImpactGrade == ImpactGrade.Great)
            {
                volume *= tuning.GreatImpactVolumeMultiplier;
            }
            if (tuning != null && command.IsPutter)
            {
                volume *= tuning.PutterImpactVolumeMultiplier;
            }
            PlayCue(GameplayAudioCue.NormalImpact, ball.PhysicsPosition, volume);
            if (command.ImpactGrade == ImpactGrade.Perfect && !command.IsPutter)
            {
                PlayCue(
                    GameplayAudioCue.PerfectImpact,
                    ball.PhysicsPosition,
                    tuning != null ? tuning.PerfectAccentVolume : 0.6f);
            }
        }

        private void OnSurfaceContacted(BallSurfaceContact contact)
        {
            if (tuning == null || contact.ImpactSpeed < tuning.MinimumLandingSpeed || contact.Sequence > 2)
            {
                return;
            }
            float intensity = contact.IsFirstLanding ? 1f : tuning.SecondaryBounceIntensity;
            if (!contact.IsFirstLanding && contact.ImpactSpeed < tuning.MinimumSecondaryBounceSpeed)
            {
                return;
            }
            PlayCue(
                ShotPresentationResolver.ResolveLandingAudio(contact.SurfaceType),
                contact.Position,
                tuning.TerrainVolume * intensity);
        }

        private void OnHazardEntered(TerrainSurfaceType hazard)
        {
            PlayCue(
                ShotPresentationResolver.ResolveHazardAudio(hazard),
                ball.PhysicsPosition,
                tuning != null ? tuning.HazardVolume : 0.65f);
        }

        private void OnHoleCompleted(ScoreResult result)
        {
            if (cinematicTuning != null)
            {
                return;
            }
            Vector3 cupPosition = holeFlow.Hole.CupPosition;
            PlayHoleInCue(cupPosition);
            PlayResultCue(cupPosition);
        }

        public void PlayHoleInCue(Vector3 cupPosition)
        {
            PlayCue(GameplayAudioCue.HoleIn, cupPosition, tuning != null ? tuning.HoleVolume : 0.7f);
        }

        public void PlayResultCue(Vector3 cupPosition)
        {
            PlayCue(GameplayAudioCue.Result, cupPosition, tuning != null ? tuning.ResultVolume : 0.6f);
        }

        private void PlayCue(GameplayAudioCue cue, Vector3 position, float volume)
        {
            // Record dispatch independently from the platform audio device so headless validation can
            // verify event routing even when Unity cannot create an audible output channel.
            cueCounts[(int)cue]++;
            TotalCueCount++;
            LastCue = cue;

            AudioSource source = ResolveSource(cue);
            AudioClip clip = ResolveConfiguredClip(cue) ?? generatedClips[(int)cue];
            if (source == null || clip == null)
            {
                return;
            }

            source.transform.position = position;
            source.pitch = cue == GameplayAudioCue.PerfectImpact ? 1.04f : 1f;
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private AudioSource ResolveSource(GameplayAudioCue cue)
        {
            return cue switch
            {
                GameplayAudioCue.UiConfirm or GameplayAudioCue.Result => uiResultSource,
                GameplayAudioCue.Swing or GameplayAudioCue.PuttSwing => swingSource,
                GameplayAudioCue.NormalImpact or GameplayAudioCue.PerfectImpact => impactSource,
                _ => terrainSource
            };
        }

        private AudioClip ResolveConfiguredClip(GameplayAudioCue cue)
        {
            if (tuning == null)
            {
                return null;
            }
            return cue switch
            {
                GameplayAudioCue.UiConfirm => tuning.UiConfirmClip,
                GameplayAudioCue.Swing => tuning.SwingClip,
                GameplayAudioCue.PuttSwing => tuning.PuttSwingClip,
                GameplayAudioCue.NormalImpact => tuning.NormalImpactClip,
                GameplayAudioCue.PerfectImpact => tuning.PerfectImpactClip,
                GameplayAudioCue.FairwayLanding => tuning.FairwayLandingClip,
                GameplayAudioCue.RoughLanding => tuning.RoughLandingClip,
                GameplayAudioCue.BunkerLanding => tuning.BunkerLandingClip,
                GameplayAudioCue.GreenLanding => tuning.GreenLandingClip,
                GameplayAudioCue.WaterHazard => tuning.WaterHazardClip,
                GameplayAudioCue.OutOfBounds => tuning.OutOfBoundsClip,
                GameplayAudioCue.HoleIn => tuning.HoleInClip,
                GameplayAudioCue.Result => tuning.ResultClip,
                _ => null
            };
        }

        private void BuildFallbackLibrary()
        {
            foreach (GameplayAudioCue cue in System.Enum.GetValues(typeof(GameplayAudioCue)))
            {
                if (ResolveConfiguredClip(cue) != null)
                {
                    continue;
                }
                generatedClips[(int)cue] = ProceduralAudioLibrary.Create(cue);
                GeneratedFallbackCount++;
            }
        }

        private Vector3 CharacterPosition()
        {
            return characterTransform != null ? characterTransform.position : transform.position;
        }
    }
}
