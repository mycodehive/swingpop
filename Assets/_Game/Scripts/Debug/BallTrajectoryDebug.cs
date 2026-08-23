using System.Collections.Generic;
using SwingPop.Gameplay.Ball;
using UnityEngine;

namespace SwingPop.Debugging
{
    public sealed class BallTrajectoryDebug : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GolfBallController ball;
        [SerializeField] private LineRenderer trajectoryLine;

        [Header("Trace")]
        [SerializeField] private bool showTrajectory = true;
        [SerializeField, Min(0.01f)] private float sampleInterval = 0.04f;
        [SerializeField, Min(0.01f)] private float minimumPointDistance = 0.12f;
        [SerializeField, Min(16)] private int maximumPoints = 512;

        private readonly List<Vector3> points = new(512);
        private float sampleElapsed;

        public bool IsTrajectoryVisible => showTrajectory;

        private void OnEnable()
        {
            if (ball != null)
            {
                ball.Launched += BeginTrace;
                ball.ResetPerformed += ClearTrace;
            }
        }

        private void Update()
        {
            if (!showTrajectory || ball == null || trajectoryLine == null
                || ball.State == BallState.Ready || points.Count >= maximumPoints)
            {
                return;
            }

            sampleElapsed += Time.deltaTime;
            if (sampleElapsed < sampleInterval)
            {
                return;
            }

            sampleElapsed = 0f;
            AddPoint(ball.transform.position);
        }

        private void OnDisable()
        {
            if (ball != null)
            {
                ball.Launched -= BeginTrace;
                ball.ResetPerformed -= ClearTrace;
            }
        }

        private void BeginTrace()
        {
            points.Clear();
            sampleElapsed = 0f;
            trajectoryLine.positionCount = 0;
            trajectoryLine.enabled = showTrajectory;
            AddPoint(ball.transform.position);
        }

        public void SetTrajectoryVisible(bool visible)
        {
            showTrajectory = visible;
            if (trajectoryLine == null)
            {
                return;
            }

            trajectoryLine.enabled = visible && points.Count > 0;
            if (!visible)
            {
                trajectoryLine.positionCount = 0;
                points.Clear();
            }
        }

        private void ClearTrace()
        {
            points.Clear();
            sampleElapsed = 0f;
            if (trajectoryLine != null)
            {
                trajectoryLine.positionCount = 0;
                trajectoryLine.enabled = false;
            }
        }

        private void AddPoint(Vector3 point)
        {
            if (points.Count > 0
                && Vector3.Distance(points[^1], point) < minimumPointDistance)
            {
                return;
            }

            points.Add(point);
            trajectoryLine.positionCount = points.Count;
            trajectoryLine.SetPosition(points.Count - 1, point);
        }
    }
}
