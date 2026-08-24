using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SwingPop.Online
{
    public interface IMatchTransport
    {
        event Action<ApprovedShot> ShotApprovedReceived;
        event Action<ShotRejection> ShotRejectedReceived;
        event Action<MatchSnapshot> SnapshotReceived;
        int PendingMessageCount { get; }
        int MessageCount { get; }
        int LastShotSubmissionBytes { get; }
        int LastSnapshotBytes { get; }
        void ConfigureLatency(int milliseconds);
        bool SubmitShot(ShotSubmission submission);
        bool SubmitShotResult(NetworkShotResult result);
        void PublishSnapshot(MatchSnapshot snapshot);
        void Tick(float deltaTime);
        void CancelPending();
    }

    [DisallowMultipleComponent]
    public sealed class LocalLoopbackTransport : MonoBehaviour, IMatchTransport
    {
        private enum MessageKind
        {
            ShotRequest,
            ApprovedResponse,
            RejectedResponse,
            ResultRequest,
            SnapshotResponse
        }

        private struct PendingMessage
        {
            public MessageKind Kind;
            public string Payload;
            public float RemainingSeconds;
        }

        [SerializeField] private LocalMatchAuthority authority;
        [SerializeField, Min(0)] private int simulatedLatencyMs;
        [SerializeField] private bool verboseLogging;

        private readonly List<PendingMessage> pending = new(8);
        private readonly JsonMatchMessageSerializer serializer = new();

        public event Action<ApprovedShot> ShotApprovedReceived;
        public event Action<ShotRejection> ShotRejectedReceived;
        public event Action<MatchSnapshot> SnapshotReceived;

        public int PendingMessageCount => pending.Count;
        public int MessageCount { get; private set; }
        public int DispatchedMessageCount => MessageCount;
        public long DispatchedPayloadBytes { get; private set; }
        public int MaximumPayloadBytes { get; private set; }
        public int LastShotSubmissionBytes { get; private set; }
        public int LastSnapshotBytes { get; private set; }
        public int SimulatedLatencyMs => simulatedLatencyMs;
        public LocalMatchAuthority Authority => authority;

        private void Update()
        {
            if (pending.Count > 0) Tick(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            CancelPending();
        }

        public void Configure(LocalMatchAuthority matchAuthority, int latencyMilliseconds, bool verbose)
        {
            authority = matchAuthority;
            ConfigureLatency(latencyMilliseconds);
            verboseLogging = verbose;
            MessageCount = 0;
            DispatchedPayloadBytes = 0;
            MaximumPayloadBytes = 0;
        }

        public void ConfigureLatency(int milliseconds)
        {
            simulatedLatencyMs = Mathf.Clamp(milliseconds, 0, 2000);
        }

        public bool SubmitShot(ShotSubmission submission)
        {
            if (authority == null) return false;
            string payload = serializer.Serialize(submission);
            LastShotSubmissionBytes = Encoding.UTF8.GetByteCount(payload);
            Enqueue(MessageKind.ShotRequest, payload);
            return true;
        }

        public bool SubmitShotResult(NetworkShotResult result)
        {
            if (authority == null) return false;
            Enqueue(MessageKind.ResultRequest, serializer.Serialize(result));
            return true;
        }

        public void PublishSnapshot(MatchSnapshot snapshot)
        {
            if (snapshot == null) return;
            Enqueue(MessageKind.SnapshotResponse, SerializeSnapshot(snapshot), false);
        }

        public void Tick(float deltaTime)
        {
            float safeDelta = Mathf.Max(0f, deltaTime);
            for (int index = pending.Count - 1; index >= 0; index--)
            {
                PendingMessage message = pending[index];
                message.RemainingSeconds -= safeDelta;
                if (message.RemainingSeconds > 0f)
                {
                    pending[index] = message;
                    continue;
                }

                pending.RemoveAt(index);
                Dispatch(message);
            }
        }

        public void CancelPending()
        {
            pending.Clear();
        }

        private void Dispatch(PendingMessage message)
        {
            int payloadBytes = Encoding.UTF8.GetByteCount(message.Payload);
            MessageCount++;
            DispatchedPayloadBytes += payloadBytes;
            MaximumPayloadBytes = Mathf.Max(MaximumPayloadBytes, payloadBytes);
            switch (message.Kind)
            {
                case MessageKind.ShotRequest:
                {
                    ShotSubmission submission = serializer.Deserialize<ShotSubmission>(message.Payload);
                    ShotSubmissionDecision decision = authority.SubmitShot(submission);
                    if (decision.Accepted)
                        Enqueue(MessageKind.ApprovedResponse, serializer.Serialize(decision.Approved));
                    else
                        Enqueue(MessageKind.RejectedResponse, serializer.Serialize(decision.Rejection));
                    break;
                }
                case MessageKind.ApprovedResponse:
                {
                    ApprovedShot approved = serializer.Deserialize<ApprovedShot>(message.Payload);
                    if (!authority.BeginShotPlayback(approved))
                    {
                        ShotSubmission synthetic = new(approved.MatchId, approved.PlayerId, approved.TurnIndex,
                            approved.ShotSequence, approved.CommandVersion, approved.Command);
                        ShotRejectedReceived?.Invoke(new ShotRejection(synthetic, ShotRejectReason.InvalidTurn));
                        break;
                    }
                    Log($"[M12][Shot] Approved P={approved.PlayerId} Seq={approved.ShotSequence}");
                    ShotApprovedReceived?.Invoke(approved);
                    PublishSnapshot(authority.CurrentSnapshot);
                    break;
                }
                case MessageKind.RejectedResponse:
                {
                    ShotRejection rejection = serializer.Deserialize<ShotRejection>(message.Payload);
                    Log($"[M12][Shot] Rejected {rejection.Reason}");
                    ShotRejectedReceived?.Invoke(rejection);
                    break;
                }
                case MessageKind.ResultRequest:
                {
                    NetworkShotResult result = serializer.Deserialize<NetworkShotResult>(message.Payload);
                    if (authority.ResolveShot(result))
                        PublishSnapshot(authority.CurrentSnapshot);
                    break;
                }
                case MessageKind.SnapshotResponse:
                {
                    MatchSnapshot snapshot = serializer.Deserialize<MatchSnapshot>(message.Payload);
                    SnapshotReceived?.Invoke(snapshot);
                    break;
                }
            }
        }

        private string SerializeSnapshot(MatchSnapshot snapshot)
        {
            string payload = serializer.Serialize(snapshot);
            LastSnapshotBytes = Encoding.UTF8.GetByteCount(payload);
            return payload;
        }

        private void Enqueue(MessageKind kind, string payload, bool applyLatency = true)
        {
            if (pending.Count >= 64)
                throw new InvalidOperationException("Local loopback pending message budget exceeded.");
            pending.Add(new PendingMessage
            {
                Kind = kind,
                Payload = payload,
                RemainingSeconds = applyLatency ? simulatedLatencyMs * 0.001f : 0f
            });
        }

        private void Log(string message)
        {
            if (verboseLogging) Debug.Log(message, this);
        }
    }
}
