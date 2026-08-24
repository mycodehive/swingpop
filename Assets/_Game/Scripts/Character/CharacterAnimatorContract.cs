using UnityEngine;

namespace SwingPop.CharacterSystem
{
    /// <summary>
    /// Gameplay에서 clip 이름이나 vendor skeleton path를 알지 않도록 Animator 계약을 한 곳에 둡니다.
    /// 최종 Animator Controller의 Base Layer state 이름은 CharacterState와 이 mapping을 따릅니다.
    /// </summary>
    public static class CharacterAnimatorContract
    {
        public const string StateParameterName = "CharacterState";
        public const string IsPutterParameterName = "IsPutter";
        public const string ImpactEventMethodName = "NotifyImpactAnimationEvent";

        public static readonly int StateParameterHash = Animator.StringToHash(StateParameterName);
        public static readonly int IsPutterParameterHash = Animator.StringToHash(IsPutterParameterName);
        private static readonly int IdleHash = Animator.StringToHash("Idle");
        private static readonly int AddressHash = Animator.StringToHash("Address");
        private static readonly int BackSwingHash = Animator.StringToHash("BackSwing");
        private static readonly int SwingHash = Animator.StringToHash("Swing");
        private static readonly int FollowThroughHash = Animator.StringToHash("FollowThrough");
        private static readonly int WatchBallHash = Animator.StringToHash("WatchBall");
        private static readonly int PuttAddressHash = Animator.StringToHash("PuttAddress");
        private static readonly int PuttBackSwingHash = Animator.StringToHash("PuttBackSwing");
        private static readonly int PuttSwingHash = Animator.StringToHash("PuttSwing");
        private static readonly int PuttFollowThroughHash = Animator.StringToHash("PuttFollowThrough");
        private static readonly int HappyHash = Animator.StringToHash("Happy");
        private static readonly int SadHash = Animator.StringToHash("Sad");
        private static readonly int BirdieCelebrationHash = Animator.StringToHash("BirdieCelebration");
        private static readonly int EagleCelebrationHash = Animator.StringToHash("EagleCelebration");
        private static readonly int HoleInOneCelebrationHash = Animator.StringToHash("HoleInOneCelebration");

        public static string GetStateName(CharacterState state)
        {
            return state switch
            {
                CharacterState.Idle => "Idle",
                CharacterState.Address => "Address",
                CharacterState.BackSwing => "BackSwing",
                CharacterState.Swing => "Swing",
                CharacterState.FollowThrough => "FollowThrough",
                CharacterState.WatchBall => "WatchBall",
                CharacterState.PuttAddress => "PuttAddress",
                CharacterState.PuttBackSwing => "PuttBackSwing",
                CharacterState.PuttSwing => "PuttSwing",
                CharacterState.PuttFollowThrough => "PuttFollowThrough",
                CharacterState.Happy => "Happy",
                CharacterState.Sad => "Sad",
                CharacterState.BirdieCelebration => "BirdieCelebration",
                CharacterState.EagleCelebration => "EagleCelebration",
                CharacterState.HoleInOneCelebration => "HoleInOneCelebration",
                _ => "Idle"
            };
        }

        public static int GetStateHash(CharacterState state)
        {
            return state switch
            {
                CharacterState.Idle => IdleHash,
                CharacterState.Address => AddressHash,
                CharacterState.BackSwing => BackSwingHash,
                CharacterState.Swing => SwingHash,
                CharacterState.FollowThrough => FollowThroughHash,
                CharacterState.WatchBall => WatchBallHash,
                CharacterState.PuttAddress => PuttAddressHash,
                CharacterState.PuttBackSwing => PuttBackSwingHash,
                CharacterState.PuttSwing => PuttSwingHash,
                CharacterState.PuttFollowThrough => PuttFollowThroughHash,
                CharacterState.Happy => HappyHash,
                CharacterState.Sad => SadHash,
                CharacterState.BirdieCelebration => BirdieCelebrationHash,
                CharacterState.EagleCelebration => EagleCelebrationHash,
                CharacterState.HoleInOneCelebration => HoleInOneCelebrationHash,
                _ => IdleHash
            };
        }
    }
}
