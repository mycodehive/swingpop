using System;
using System.IO;
using SwingPop.Data;
using UnityEngine;

namespace SwingPop.Online
{
    /// <summary>
    /// Thin client session coordinator. Gameplay state remains owned by MatchSessionController and the server.
    /// Ticket files are an explicit local-development bridge for replacing a killed client process.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class ReconnectController : MonoBehaviour
    {
        private const string InputArgument = "-swingpopReconnectFile=";
        private const string OutputArgument = "-swingpopReconnectOutput=";

        [SerializeField] private MultiplayerDevelopmentSettings settings;
        [SerializeField] private UnityTransportMatchTransport networkTransport;

        private string ticketInputPath = string.Empty;
        private string ticketOutputPath = string.Empty;
        private float retryElapsed;
        private int attemptCount;
        private bool retryPending;
        private string lastResult = "NONE";

        public ReconnectClientState State => networkTransport != null ? networkTransport.ReconnectState : ReconnectClientState.None;
        public int AttemptCount => attemptCount;
        public int SessionGeneration => networkTransport != null && networkTransport.HasReconnectTicket
            ? networkTransport.CurrentReconnectTicket.SessionGeneration : 0;
        public string LastResult => lastResult;
        public long GraceRemainingMilliseconds
        {
            get
            {
                if (networkTransport == null) return 0L;
                long deadline = networkTransport.LatestLifecycle.GraceDeadlineUnixMilliseconds;
                return deadline <= 0L ? 0L : Math.Max(0L, deadline - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
        }

        private void Awake()
        {
            ParseDevelopmentTicketArguments(Environment.GetCommandLineArgs());
            if (!string.IsNullOrEmpty(ticketInputPath)) TryLoadDevelopmentTicket(ticketInputPath);
        }

        private void OnEnable()
        {
            if (networkTransport == null) return;
            networkTransport.ReconnectTicketChanged += OnTicketChanged;
            networkTransport.ReconnectAccepted += OnReconnectAccepted;
            networkTransport.ReconnectRejected += OnReconnectRejected;
            networkTransport.Disconnected += OnDisconnected;
        }

        private void OnDisable()
        {
            if (networkTransport == null) return;
            networkTransport.ReconnectTicketChanged -= OnTicketChanged;
            networkTransport.ReconnectAccepted -= OnReconnectAccepted;
            networkTransport.ReconnectRejected -= OnReconnectRejected;
            networkTransport.Disconnected -= OnDisconnected;
            retryPending = false;
        }

        private void Update()
        {
            if (!retryPending || networkTransport == null || settings == null) return;
            retryElapsed += Time.unscaledDeltaTime;
            if (retryElapsed < settings.ReconnectRetryDelaySeconds) return;
            retryElapsed = 0f;
            if (attemptCount >= settings.ReconnectAttemptLimit)
            {
                retryPending = false;
                lastResult = "RECONNECT FAILED: ATTEMPT LIMIT";
                return;
            }
            attemptCount++;
            lastResult = $"RECONNECTING {attemptCount}/{settings.ReconnectAttemptLimit}";
            retryPending = false;
            if (!networkTransport.StartClient(networkTransport.Address, networkTransport.Port,
                    settings.ConnectionTimeoutSeconds))
                ScheduleRetry();
        }

        public void Configure(MultiplayerDevelopmentSettings developmentSettings,
            UnityTransportMatchTransport transport)
        {
            settings = developmentSettings;
            networkTransport = transport;
        }

        public bool RequestReconnectNow()
        {
            if (networkTransport == null || !networkTransport.HasReconnectTicket) return false;
            retryPending = false;
            attemptCount++;
            lastResult = "MANUAL RECONNECT";
            float timeout = settings != null ? settings.ConnectionTimeoutSeconds : 8f;
            return networkTransport.StartClient(networkTransport.Address, networkTransport.Port, timeout);
        }

        public bool TryLoadDevelopmentTicket(string path)
        {
            try
            {
                if (networkTransport == null || string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
                ReconnectTicket ticket = JsonUtility.FromJson<ReconnectTicket>(File.ReadAllText(path));
                if (!networkTransport.SetPendingReconnectTicket(ticket)) return false;
                lastResult = $"TICKET LOADED: {ticket.PlayerId} G{ticket.SessionGeneration}";
                return true;
            }
            catch (Exception exception)
            {
                lastResult = $"TICKET LOAD FAILED: {exception.GetType().Name}";
                return false;
            }
        }

        private void ParseDevelopmentTicketArguments(string[] arguments)
        {
            if (arguments == null) return;
            foreach (string argument in arguments)
            {
                if (argument != null && argument.StartsWith(InputArgument, StringComparison.OrdinalIgnoreCase))
                    ticketInputPath = argument.Substring(InputArgument.Length).Trim().Trim('"');
                else if (argument != null && argument.StartsWith(OutputArgument, StringComparison.OrdinalIgnoreCase))
                    ticketOutputPath = argument.Substring(OutputArgument.Length).Trim().Trim('"');
            }
        }

        private void OnTicketChanged(ReconnectTicket ticket)
        {
            lastResult = $"TICKET READY: {ticket.PlayerId} G{ticket.SessionGeneration}";
            if (string.IsNullOrWhiteSpace(ticketOutputPath)) return;
            try
            {
                string directory = Path.GetDirectoryName(ticketOutputPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(ticketOutputPath, JsonUtility.ToJson(ticket));
            }
            catch (Exception exception)
            {
                lastResult = $"TICKET SAVE FAILED: {exception.GetType().Name}";
            }
        }

        private void OnReconnectAccepted(ReconnectAcceptedMessage accepted)
        {
            retryPending = false;
            attemptCount = 0;
            lastResult = $"RECONNECTED: {accepted.PlayerId} G{accepted.RotatedTicket.SessionGeneration}";
        }

        private void OnReconnectRejected(ReconnectRejectedMessage rejected)
        {
            lastResult = $"RECONNECT FAILED: {rejected.Reason}";
            retryPending = false;
        }

        private void OnDisconnected(string reason)
        {
            lastResult = $"CONNECTION LOST: {reason}";
            if (settings != null && settings.AutoReconnectEnabled && networkTransport.HasReconnectTicket
                && networkTransport.ReconnectState != ReconnectClientState.ReconnectFailed
                && networkTransport.ReconnectState != ReconnectClientState.Ended)
                ScheduleRetry();
        }

        private void ScheduleRetry()
        {
            retryPending = true;
            retryElapsed = 0f;
        }
    }
}
