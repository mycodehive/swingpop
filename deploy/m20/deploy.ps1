param(
    [Parameter(Mandatory = $true)][string]$Server,
    [Parameter(Mandatory = $true)][string]$Domain,
    [string]$SshUser = "root",
    [string]$IdentityFile = "",
    [Parameter(Mandatory = $true)][string]$AuthKeyFile
)

$ErrorActionPreference = "Stop"
if ($Domain -notmatch '^[A-Za-z0-9.-]+$') { throw "Domain contains unsupported characters." }
$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$LobbyBuild = Join-Path $ProjectRoot "Builds\M20Staging\Linux\Lobby"
$ServerBuild = Join-Path $ProjectRoot "Builds\M20Staging\Linux\Server"
if (-not (Test-Path -LiteralPath (Join-Path $LobbyBuild "SwingPopLobby.x86_64"))) {
    throw "Linux Lobby build missing. Run SwingPop > Online > M20 > Build Linux Staging Lobby + Server."
}
if (-not (Test-Path -LiteralPath (Join-Path $ServerBuild "SwingPopServer.x86_64"))) {
    throw "Linux match server build missing."
}
if (-not (Test-Path -LiteralPath $AuthKeyFile)) { throw "Auth key file does not exist." }

$SshOptions = @()
if ($IdentityFile) { $SshOptions += @("-i", $IdentityFile) }
$Target = "$SshUser@$Server"
$Remote = "/tmp/swingpop-m20"

& ssh @SshOptions $Target "mkdir -p $Remote/artifacts/lobby $Remote/artifacts/server"
& scp @SshOptions -r "$PSScriptRoot\*" "${Target}:$Remote/"
& scp @SshOptions -r "$LobbyBuild\*" "${Target}:$Remote/artifacts/lobby/"
& scp @SshOptions -r "$ServerBuild\*" "${Target}:$Remote/artifacts/server/"
& scp @SshOptions $AuthKeyFile "${Target}:$Remote/auth-key.txt"
& ssh @SshOptions $Target "chmod +x $Remote/remote-install.sh $Remote/artifacts/lobby/SwingPopLobby.x86_64 $Remote/artifacts/server/SwingPopServer.x86_64 && SWINGPOP_LOBBY_DOMAIN='$Domain' SWINGPOP_AUTH_KEY_FILE='$Remote/auth-key.txt' bash $Remote/remote-install.sh"

Write-Host "Deployment command completed. Public TLS and gameplay still require the documented external verification."
