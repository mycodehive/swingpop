using System;
using System.IO;
using SwingPop.Data;
using UnityEngine;

namespace SwingPop.Online
{
    /// <summary>Thin development credential/session adapter. It never owns gameplay or match state.</summary>
    [DefaultExecutionOrder(-110)]
    [DisallowMultipleComponent]
    public sealed class AuthenticationController : MonoBehaviour
    {
        public const string CredentialFileArgument = "-swingpopAuthCredentialFile=";
        public const string ServerKeyFileArgument = "-swingpopAuthKeyFile=";

        [SerializeField] private MultiplayerDevelopmentSettings settings;
        [SerializeField] private UnityTransportMatchTransport networkTransport;

        private string credentialFilePath = string.Empty;
        private string lastResult = "NO CREDENTIAL";

        public AuthenticationClientState State => networkTransport != null
            ? networkTransport.AuthenticationState : AuthenticationClientState.None;
        public PlayerAccountId AccountId => networkTransport != null ? networkTransport.AuthenticatedAccountId : default;
        public AuthSessionId SessionId => networkTransport != null ? networkTransport.AuthenticatedSessionId : default;
        public long SessionExpiryUnixMilliseconds => networkTransport != null
            ? networkTransport.AuthenticationSessionExpiryUnixMilliseconds : 0L;
        public string SessionFingerprint => SessionId.IsValid
            ? DevelopmentAuthenticationProvider.Fingerprint(SessionId.Value) : "none";
        public string AccountLabel => AccountId.IsValid ? AccountId.Value : "anonymous";
        public string LastResult => lastResult;

        private void Awake()
        {
            credentialFilePath = ReadArgument(Environment.GetCommandLineArgs(), CredentialFileArgument);
            if (!string.IsNullOrWhiteSpace(credentialFilePath)) TryLoadDevelopmentCredential(credentialFilePath);
        }

        private void OnEnable()
        {
            if (networkTransport == null) return;
            networkTransport.AuthenticationAccepted += OnAccepted;
            networkTransport.AuthenticationRejected += OnRejected;
            networkTransport.Disconnected += OnDisconnected;
        }

        private void OnDisable()
        {
            if (networkTransport == null) return;
            networkTransport.AuthenticationAccepted -= OnAccepted;
            networkTransport.AuthenticationRejected -= OnRejected;
            networkTransport.Disconnected -= OnDisconnected;
        }

        public void Configure(MultiplayerDevelopmentSettings developmentSettings, UnityTransportMatchTransport transport)
        {
            settings = developmentSettings;
            networkTransport = transport;
        }

        public bool TryLoadDevelopmentCredential(string path)
        {
            try
            {
                if (networkTransport == null || string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
                string credential = File.ReadAllText(path).Trim();
                if (!networkTransport.SetAuthenticationCredential(credential)) return false;
                lastResult = "CREDENTIAL READY " + DevelopmentAuthenticationProvider.Fingerprint(credential);
                return true;
            }
            catch (Exception exception)
            {
                lastResult = "CREDENTIAL LOAD FAILED: " + exception.GetType().Name;
                return false;
            }
        }

        public static bool TryLoadServerSigningKey(string[] arguments, out byte[] key)
        {
            key = null;
            string path = ReadArgument(arguments, ServerKeyFileArgument);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
            try
            {
                byte[] parsed = Convert.FromBase64String(File.ReadAllText(path).Trim());
                if (parsed.Length < 32) return false;
                key = parsed;
                return true;
            }
            catch (Exception)
            {
                key = null;
                return false;
            }
        }

        private void OnAccepted(AuthAcceptedMessage accepted)
        {
            lastResult = $"AUTHENTICATED {accepted.PlayerAccountId} / {DevelopmentAuthenticationProvider.Fingerprint(accepted.AuthSessionId.Value)}";
        }

        private void OnRejected(AuthRejectedMessage rejected)
        {
            lastResult = "AUTH FAILED: " + rejected.Reason;
        }

        private void OnDisconnected(string reason)
        {
            if (State != AuthenticationClientState.Rejected) lastResult = "AUTH CONNECTION LOST";
        }

        private static string ReadArgument(string[] arguments, string prefix)
        {
            if (arguments == null) return string.Empty;
            foreach (string argument in arguments)
                if (argument != null && argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return argument.Substring(prefix.Length).Trim().Trim('"');
            return string.Empty;
        }
    }
}
