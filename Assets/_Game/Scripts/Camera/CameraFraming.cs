using UnityEngine;

namespace SwingPop.CameraSystem
{
    public readonly struct CameraFraming
    {
        public CameraFraming(Vector3 position, Vector3 target, float fieldOfView, string targetName)
        {
            Position = position;
            Target = target;
            FieldOfView = fieldOfView;
            TargetName = targetName;
        }

        public Vector3 Position { get; }
        public Vector3 Target { get; }
        public float FieldOfView { get; }
        public string TargetName { get; }
    }
}
