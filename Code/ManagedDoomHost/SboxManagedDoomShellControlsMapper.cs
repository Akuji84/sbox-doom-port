using System;
using System.Collections.Generic;
using ManagedDoom;

namespace Sandbox;

// Applies the CONTROLS.EXE bindings/settings the win98 shell sends with a
// launch onto a freshly created ManagedDoom Config. Unknown or missing
// values always fall back to the config's existing binding, so a partial
// or malformed payload can never leave the player without controls.
internal static class SboxManagedDoomShellControlsMapper
{
    public static void Apply( Config config, SboxManagedDoomShellBridgeService.ShellLaunchConfig shellConfig )
    {
        if ( config is null || shellConfig is null )
        {
            return;
        }

        var bindings = shellConfig.Controls?.Bindings;
        if ( bindings is not null )
        {
            config.key_forward = MapBinding( bindings, "forward", config.key_forward );
            config.key_backward = MapBinding( bindings, "backward", config.key_backward );
            config.key_strafeleft = MapBinding( bindings, "strafeLeft", config.key_strafeleft );
            config.key_straferight = MapBinding( bindings, "strafeRight", config.key_straferight );
            config.key_turnleft = MapBinding( bindings, "turnLeft", config.key_turnleft );
            config.key_turnright = MapBinding( bindings, "turnRight", config.key_turnright );
            config.key_fire = MapBinding( bindings, "fire", config.key_fire );
            config.key_use = MapBinding( bindings, "use", config.key_use );
            config.key_run = MapBinding( bindings, "run", config.key_run );
            config.key_strafe = MapBinding( bindings, "strafe", config.key_strafe );
            config.key_weapon1 = MapBinding( bindings, "weapon1", config.key_weapon1 );
            config.key_weapon2 = MapBinding( bindings, "weapon2", config.key_weapon2 );
            config.key_weapon3 = MapBinding( bindings, "weapon3", config.key_weapon3 );
            config.key_weapon4 = MapBinding( bindings, "weapon4", config.key_weapon4 );
            config.key_weapon5 = MapBinding( bindings, "weapon5", config.key_weapon5 );
            config.key_weapon6 = MapBinding( bindings, "weapon6", config.key_weapon6 );
            config.key_weapon7 = MapBinding( bindings, "weapon7", config.key_weapon7 );
        }

        var settings = shellConfig.Controls?.Settings;
        if ( settings is not null )
        {
            config.mouse_sensitivity = Math.Clamp( settings.MouseSensitivity, 1, 9 );
            config.game_alwaysrun = settings.AlwaysRun;
            config.video_displaymessage = settings.ShowMessages;
            if ( !settings.Music )
            {
                config.audio_musicvolume = 0;
            }
            if ( !settings.Sfx )
            {
                config.audio_soundvolume = 0;
            }
        }

        if ( shellConfig.ShellVolume is double shellVolume )
        {
            var scale = Math.Clamp( shellVolume, 0.0, 1.0 );
            config.audio_soundvolume = Math.Clamp( (int)Math.Round( config.audio_soundvolume * scale ), 0, 15 );
            config.audio_musicvolume = Math.Clamp( (int)Math.Round( config.audio_musicvolume * scale ), 0, 15 );
        }
    }

    private static KeyBinding MapBinding( Dictionary<string, string> bindings, string name, KeyBinding fallback )
    {
        if ( !bindings.TryGetValue( name, out var label ) || string.IsNullOrWhiteSpace( label ) )
        {
            return fallback;
        }

        var trimmed = label.Trim();

        if ( trimmed.StartsWith( "Mouse ", StringComparison.OrdinalIgnoreCase ) )
        {
            if ( int.TryParse( trimmed.Substring( 6 ).Trim(), out var button ) && button >= 1 && button <= 5 )
            {
                return new KeyBinding( Array.Empty<DoomKey>(), new[] { (DoomMouseButton)(button - 1) } );
            }
            return fallback;
        }

        var key = MapKey( trimmed );
        if ( key == DoomKey.Unknown )
        {
            return fallback;
        }

        return new KeyBinding( new[] { key } );
    }

    private static DoomKey MapKey( string label )
    {
        if ( label.Length == 1 )
        {
            var ch = char.ToUpperInvariant( label[0] );
            if ( ch >= 'A' && ch <= 'Z' )
            {
                return DoomKey.A + (ch - 'A');
            }
            if ( ch >= '0' && ch <= '9' )
            {
                return DoomKey.Num0 + (ch - '0');
            }
        }

        switch ( label.ToLowerInvariant() )
        {
            case "left arrow": return DoomKey.Left;
            case "right arrow": return DoomKey.Right;
            case "up arrow": return DoomKey.Up;
            case "down arrow": return DoomKey.Down;
            case "space": return DoomKey.Space;
            case "shift": return DoomKey.LShift;
            case "ctrl": return DoomKey.LControl;
            case "control": return DoomKey.LControl;
            case "alt": return DoomKey.LAlt;
            case "enter": return DoomKey.Enter;
            case "tab": return DoomKey.Tab;
            case "backspace": return DoomKey.Backspace;
            default: return DoomKey.Unknown;
        }
    }
}
