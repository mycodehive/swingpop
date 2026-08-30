using System;
using UnityEngine;

namespace SwingPop.Online
{
    public enum LobbyWireMessageType
    {
        AuthRequest,
        AuthAccepted,
        AuthRejected,
        CreateMatch,
        ListMatches,
        JoinMatch,
        LeaveMatch,
        SetReady,
        StartMatch,
        GetMatch,
        CloseMatch,
        MatchList,
        MatchUpdated,
        AdmissionGranted,
        OperationRejected,
        Ping,
        Pong,
        Disconnect
    }

    public enum LobbyEventType
    {
        MatchCreated,
        MatchUpdated,
        MemberJoined,
        MemberLeft,
        ReadyChanged,
        MatchStarting,
        MatchClosed
    }

    [Serializable]
    public struct LobbyNetworkEnvelope
    {
        [SerializeField] private int protocolVersion;
        [SerializeField] private LobbyWireMessageType messageType;
        [SerializeField] private long sequence;
        [SerializeField] private string payload;

        public LobbyNetworkEnvelope(LobbyWireMessageType messageType, long sequence, string payload)
        {
            protocolVersion = LobbyProtocol.CurrentVersion;
            this.messageType = messageType;
            this.sequence = sequence;
            this.payload = payload ?? string.Empty;
        }

        public int ProtocolVersion => protocolVersion;
        public LobbyWireMessageType MessageType => messageType;
        public long Sequence => sequence;
        public string Payload => payload ?? string.Empty;
    }

    [Serializable]
    public struct LobbyOperationRejectedMessage
    {
        [SerializeField] private string requestId;
        [SerializeField] private LobbyRejectReason reason;

        public LobbyOperationRejectedMessage(string requestId, LobbyRejectReason reason)
        {
            this.requestId = requestId ?? string.Empty;
            this.reason = reason;
        }

        public string RequestId => requestId ?? string.Empty;
        public LobbyRejectReason Reason => reason;
    }

    [Serializable]
    public struct LobbyMatchListMessage
    {
        [SerializeField] private string requestId;
        [SerializeField] private LobbyMatchSnapshot[] matches;

        public LobbyMatchListMessage(string requestId, LobbyMatchSnapshot[] matches)
        {
            this.requestId = requestId ?? string.Empty;
            this.matches = matches ?? Array.Empty<LobbyMatchSnapshot>();
        }

        public string RequestId => requestId ?? string.Empty;
        public LobbyMatchSnapshot[] Matches => matches ?? Array.Empty<LobbyMatchSnapshot>();
    }

    [Serializable]
    public struct LobbyMatchUpdatedMessage
    {
        [SerializeField] private string requestId;
        [SerializeField] private LobbyEventType eventType;
        [SerializeField] private LobbyMatchSnapshot match;

        public LobbyMatchUpdatedMessage(string requestId, LobbyEventType eventType, LobbyMatchSnapshot match)
        {
            this.requestId = requestId ?? string.Empty;
            this.eventType = eventType;
            this.match = match;
        }

        public string RequestId => requestId ?? string.Empty;
        public LobbyEventType EventType => eventType;
        public LobbyMatchSnapshot Match => match;
    }

    [Serializable]
    public struct LobbyAdmissionGrantedMessage
    {
        [SerializeField] private string requestId;
        [SerializeField] private MatchAdmissionGrant grant;

        public LobbyAdmissionGrantedMessage(string requestId, MatchAdmissionGrant grant)
        {
            this.requestId = requestId ?? string.Empty;
            this.grant = grant;
        }

        public string RequestId => requestId ?? string.Empty;
        public MatchAdmissionGrant Grant => grant;
    }

    public static class LobbyNetworkRules
    {
        public static bool IsAllowedFromClient(LobbyWireMessageType type) => type is
            LobbyWireMessageType.AuthRequest or LobbyWireMessageType.CreateMatch
            or LobbyWireMessageType.ListMatches or LobbyWireMessageType.JoinMatch
            or LobbyWireMessageType.LeaveMatch or LobbyWireMessageType.SetReady
            or LobbyWireMessageType.StartMatch or LobbyWireMessageType.GetMatch
            or LobbyWireMessageType.CloseMatch or LobbyWireMessageType.Ping
            or LobbyWireMessageType.Pong or LobbyWireMessageType.Disconnect;

        public static bool IsAllowedFromService(LobbyWireMessageType type) => type is
            LobbyWireMessageType.AuthAccepted or LobbyWireMessageType.AuthRejected
            or LobbyWireMessageType.MatchList or LobbyWireMessageType.MatchUpdated
            or LobbyWireMessageType.AdmissionGranted or LobbyWireMessageType.OperationRejected
            or LobbyWireMessageType.Ping or LobbyWireMessageType.Pong
            or LobbyWireMessageType.Disconnect;
    }
}
