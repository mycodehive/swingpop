using UnityEngine;

namespace SwingPop.CameraSystem
{
    public readonly struct CameraPose
    {
        public CameraPose(Vector3 position, Quaternion rotation, float fieldOfView)
        {
            Position = position;
            Rotation = rotation;
            FieldOfView = fieldOfView;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public float FieldOfView { get; }

        public static CameraPose Lerp(CameraPose from, CameraPose to, float progress)
        {
            float smoothProgress = CameraMath.SmoothStep01(progress);
            return new CameraPose(
                Vector3.Lerp(from.Position, to.Position, smoothProgress),
                Quaternion.Slerp(from.Rotation, to.Rotation, smoothProgress),
                Mathf.Lerp(from.FieldOfView, to.FieldOfView, smoothProgress));
        }
    }
}
