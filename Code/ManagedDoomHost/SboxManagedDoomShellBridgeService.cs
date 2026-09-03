using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sandbox;

namespace Sandbox;

internal static class SboxManagedDoomShellBridgeService
{
    private const string BaseUrl = "https://win98.akuji.org";
    private const string ShellChannel = "live";
    private const string ShellBuild = "20260903b";
    private static readonly Dictionary<string, string> EmptyHeaders = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string BuildShellUrl( string sessionId, string playerName = "" )
    {
        var url = $"{BaseUrl}/shell/{ShellChannel}/index.html?session={Uri.EscapeDataString( sessionId )}&client=sbox&channel={ShellChannel}&build={ShellBuild}";
        if ( !string.IsNullOrWhiteSpace( playerName ) )
        {
            // Messenger uses this as the locked chat identity.
            url += $"&player={Uri.EscapeDataString( playerName.Trim() )}";
        }
        return url;
    }

    public static async Task<ShellLaunchRequest> PollPendingLaunchAsync( string sessionId )
    {
        if ( string.IsNullOrWhiteSpace( sessionId ) )
        {
            return ShellLaunchRequest.None;
        }

        try
        {
            var json = await Http.RequestStringAsync(
                $"{BaseUrl}/api/win98-shell/{ShellChannel}/pending?session={Uri.EscapeDataString( sessionId )}",
                "GET",
                null,
                EmptyHeaders,
                CancellationToken.None );

            if ( string.IsNullOrWhiteSpace( json ) )
            {
                return ShellLaunchRequest.None;
            }

            var response = JsonSerializer.Deserialize<ShellPendingLaunchResponse>( json, JsonOptions );
            if ( response?.Ok != true || !response.Pending )
            {
                return ShellLaunchRequest.None;
            }

            return new ShellLaunchRequest(
                response.RequestId?.Trim() ?? string.Empty,
                response.WadPath?.Trim() ?? string.Empty,
                response.Config );
        }
        catch ( Exception ex )
        {
            Log.Warning( $"[ManagedDoomHost] Shell bridge poll failed: {ex}" );
            return ShellLaunchRequest.None;
        }
    }

    public static async Task AcknowledgeLaunchAsync( string sessionId, string requestId )
    {
        if ( string.IsNullOrWhiteSpace( sessionId ) || string.IsNullOrWhiteSpace( requestId ) )
        {
            return;
        }

        try
        {
            await Http.RequestStringAsync(
                $"{BaseUrl}/api/win98-shell/{ShellChannel}/ack?session={Uri.EscapeDataString( sessionId )}&requestId={Uri.EscapeDataString( requestId )}",
                "POST",
                null,
                EmptyHeaders,
                CancellationToken.None );
        }
        catch ( Exception ex )
        {
            Log.Warning( $"[ManagedDoomHost] Shell bridge ack failed: {ex}" );
        }
    }

    internal readonly record struct ShellLaunchRequest( string RequestId, string WadPath, ShellLaunchConfig Config )
    {
        public static ShellLaunchRequest None => new( string.Empty, string.Empty, null );
        public bool HasLaunch => !string.IsNullOrWhiteSpace( WadPath );
    }

    private sealed class ShellPendingLaunchResponse
    {
        public bool Ok { get; set; }
        public bool Pending { get; set; }
        public string RequestId { get; set; } = string.Empty;
        public string WadPath { get; set; } = string.Empty;
        public ShellLaunchConfig Config { get; set; }
    }

    // Mirrors the config object the shell posts with a launch:
    // { controls: { bindings: { forward: "W", ... }, settings: {...} }, shellVolume: 0..1 }
    internal sealed class ShellLaunchConfig
    {
        public ShellControlsPayload Controls { get; set; }
        public double? ShellVolume { get; set; }
    }

    internal sealed class ShellControlsPayload
    {
        public Dictionary<string, string> Bindings { get; set; }
        public ShellControlsSettings Settings { get; set; }
    }

    internal sealed class ShellControlsSettings
    {
        public int MouseSensitivity { get; set; } = 5;
        public bool AlwaysRun { get; set; } = true;
        public bool ShowMessages { get; set; } = true;
        public bool UncappedFps { get; set; }
        public bool Music { get; set; } = true;
        public bool Sfx { get; set; } = true;
    }
}
