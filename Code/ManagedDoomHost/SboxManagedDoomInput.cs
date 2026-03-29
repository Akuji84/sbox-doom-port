using System;

namespace Sandbox;

public sealed class SboxManagedDoomInput : ManagedDoom.UserInput.IUserInput
{
    private const float MaxMouseDeltaSpike = 256.0f;

    private readonly ManagedDoom.Config config;
    private readonly bool[] weaponKeys;
    private int turnHeld;
    private bool mouseGrabbed;

    public SboxManagedDoomInput( ManagedDoom.Config config )
    {
        this.config = config;
        weaponKeys = new bool[7];
        MouseSensitivity = Math.Clamp( config.mouse_sensitivity, 0, MaxMouseSensitivity );
    }

    public void BuildTicCmd( ManagedDoom.TicCmd cmd )
    {
        var keyForward = IsPressed( config.key_forward );
        var keyBackward = IsPressed( config.key_backward );
        var keyStrafeLeft = IsPressed( config.key_strafeleft );
        var keyStrafeRight = IsPressed( config.key_straferight );
        var keyTurnLeft = IsPressed( config.key_turnleft );
        var keyTurnRight = IsPressed( config.key_turnright );
        var keyFire = IsPressed( config.key_fire );
        var keyUse = IsPressed( config.key_use );
        var keyRun = IsPressed( config.key_run );
        var keyStrafe = IsPressed( config.key_strafe );

        weaponKeys[0] = AnyDown( "1", "DIGIT1" );
        weaponKeys[1] = AnyDown( "2", "DIGIT2" );
        weaponKeys[2] = AnyDown( "3", "DIGIT3" );
        weaponKeys[3] = AnyDown( "4", "DIGIT4" );
        weaponKeys[4] = AnyDown( "5", "DIGIT5" );
        weaponKeys[5] = AnyDown( "6", "DIGIT6" );
        weaponKeys[6] = AnyDown( "7", "DIGIT7" );

        cmd.Clear();

        var strafe = keyStrafe;
        var speed = keyRun ? 1 : 0;
        var forward = 0;
        var side = 0;

        if ( config.game_alwaysrun )
        {
            speed = 1 - speed;
        }

        if ( keyTurnLeft || keyTurnRight )
        {
            turnHeld++;
        }
        else
        {
            turnHeld = 0;
        }

        var turnSpeed = turnHeld < ManagedDoom.PlayerBehavior.SlowTurnTics ? 2 : speed;

        if ( strafe )
        {
            if ( keyTurnRight )
            {
                side += ManagedDoom.PlayerBehavior.SideMove[speed];
            }

            if ( keyTurnLeft )
            {
                side -= ManagedDoom.PlayerBehavior.SideMove[speed];
            }
        }
        else
        {
            if ( keyTurnRight )
            {
                cmd.AngleTurn -= (short)ManagedDoom.PlayerBehavior.AngleTurn[turnSpeed];
            }

            if ( keyTurnLeft )
            {
                cmd.AngleTurn += (short)ManagedDoom.PlayerBehavior.AngleTurn[turnSpeed];
            }
        }

        if ( keyForward )
        {
            forward += ManagedDoom.PlayerBehavior.ForwardMove[speed];
        }

        if ( keyBackward )
        {
            forward -= ManagedDoom.PlayerBehavior.ForwardMove[speed];
        }

        if ( keyStrafeLeft )
        {
            side -= ManagedDoom.PlayerBehavior.SideMove[speed];
        }

        if ( keyStrafeRight )
        {
            side += ManagedDoom.PlayerBehavior.SideMove[speed];
        }

        if ( keyFire )
        {
            cmd.Buttons |= ManagedDoom.TicCmdButtons.Attack;
        }

        if ( keyUse )
        {
            cmd.Buttons |= ManagedDoom.TicCmdButtons.Use;
        }

        for ( var i = 0; i < weaponKeys.Length; i++ )
        {
            if ( weaponKeys[i] )
            {
                cmd.Buttons |= ManagedDoom.TicCmdButtons.Change;
                cmd.Buttons |= (byte)( i << ManagedDoom.TicCmdButtons.WeaponShift );
                break;
            }
        }

        var mouseDelta = mouseGrabbed ? Input.MouseDelta : Vector2.Zero;
        var mouseScale = 0.5f * MouseSensitivity;
        if ( MathF.Abs( mouseDelta.x ) > MaxMouseDeltaSpike )
        {
            mouseDelta.x = MathF.Sign( mouseDelta.x ) * MaxMouseDeltaSpike;
        }

        if ( MathF.Abs( mouseDelta.y ) > MaxMouseDeltaSpike )
        {
            mouseDelta.y = MathF.Sign( mouseDelta.y ) * MaxMouseDeltaSpike;
        }

        var mx = (int)MathF.Round( mouseScale * mouseDelta.x );
        var my = config.mouse_disableyaxis ? 0 : (int)MathF.Round( mouseScale * -mouseDelta.y );

        forward += my;
        if ( strafe )
        {
            side += mx * 2;
        }
        else
        {
            cmd.AngleTurn -= (short)( mx * 0x8 );
        }

        forward = Math.Clamp( forward, -ManagedDoom.PlayerBehavior.MaxMove, ManagedDoom.PlayerBehavior.MaxMove );
        side = Math.Clamp( side, -ManagedDoom.PlayerBehavior.MaxMove, ManagedDoom.PlayerBehavior.MaxMove );

        cmd.ForwardMove += (sbyte)forward;
        cmd.SideMove += (sbyte)side;
    }

    public void Reset()
    {
    }

    public void GrabMouse()
    {
        mouseGrabbed = true;
    }

    public void ReleaseMouse()
    {
        mouseGrabbed = false;
    }

    public int MaxMouseSensitivity => 9;

    public int MouseSensitivity { get; set; }

    private bool IsPressed( ManagedDoom.KeyBinding keyBinding )
    {
        foreach ( var key in keyBinding.Keys )
        {
            if ( IsPressed( key ) )
            {
                return true;
            }
        }

        if ( mouseGrabbed )
        {
            foreach ( var mouseButton in keyBinding.MouseButtons )
            {
                if ( IsPressed( mouseButton ) )
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsPressed( ManagedDoom.DoomMouseButton mouseButton )
    {
        return mouseButton switch
        {
            ManagedDoom.DoomMouseButton.Mouse1 => AnyDown( "MOUSE1" ),
            ManagedDoom.DoomMouseButton.Mouse2 => AnyDown( "MOUSE2" ),
            ManagedDoom.DoomMouseButton.Mouse3 => AnyDown( "MOUSE3" ),
            ManagedDoom.DoomMouseButton.Mouse4 => AnyDown( "MOUSE4" ),
            ManagedDoom.DoomMouseButton.Mouse5 => AnyDown( "MOUSE5" ),
            _ => false
        };
    }

    private static bool IsPressed( ManagedDoom.DoomKey key )
    {
        return key switch
        {
            ManagedDoom.DoomKey.A => AnyDown( "A" ),
            ManagedDoom.DoomKey.B => AnyDown( "B" ),
            ManagedDoom.DoomKey.C => AnyDown( "C" ),
            ManagedDoom.DoomKey.D => AnyDown( "D" ),
            ManagedDoom.DoomKey.E => AnyDown( "E" ),
            ManagedDoom.DoomKey.F => AnyDown( "F" ),
            ManagedDoom.DoomKey.G => AnyDown( "G" ),
            ManagedDoom.DoomKey.H => AnyDown( "H" ),
            ManagedDoom.DoomKey.I => AnyDown( "I" ),
            ManagedDoom.DoomKey.J => AnyDown( "J" ),
            ManagedDoom.DoomKey.K => AnyDown( "K" ),
            ManagedDoom.DoomKey.L => AnyDown( "L" ),
            ManagedDoom.DoomKey.M => AnyDown( "M" ),
            ManagedDoom.DoomKey.N => AnyDown( "N" ),
            ManagedDoom.DoomKey.O => AnyDown( "O" ),
            ManagedDoom.DoomKey.P => AnyDown( "P" ),
            ManagedDoom.DoomKey.Q => AnyDown( "Q" ),
            ManagedDoom.DoomKey.R => AnyDown( "R" ),
            ManagedDoom.DoomKey.S => AnyDown( "S" ),
            ManagedDoom.DoomKey.T => AnyDown( "T" ),
            ManagedDoom.DoomKey.U => AnyDown( "U" ),
            ManagedDoom.DoomKey.V => AnyDown( "V" ),
            ManagedDoom.DoomKey.W => AnyDown( "W" ),
            ManagedDoom.DoomKey.X => AnyDown( "X" ),
            ManagedDoom.DoomKey.Y => AnyDown( "Y" ),
            ManagedDoom.DoomKey.Z => AnyDown( "Z" ),
            ManagedDoom.DoomKey.Num0 => AnyDown( "0", "DIGIT0" ),
            ManagedDoom.DoomKey.Num1 => AnyDown( "1", "DIGIT1" ),
            ManagedDoom.DoomKey.Num2 => AnyDown( "2", "DIGIT2" ),
            ManagedDoom.DoomKey.Num3 => AnyDown( "3", "DIGIT3" ),
            ManagedDoom.DoomKey.Num4 => AnyDown( "4", "DIGIT4" ),
            ManagedDoom.DoomKey.Num5 => AnyDown( "5", "DIGIT5" ),
            ManagedDoom.DoomKey.Num6 => AnyDown( "6", "DIGIT6" ),
            ManagedDoom.DoomKey.Num7 => AnyDown( "7", "DIGIT7" ),
            ManagedDoom.DoomKey.Num8 => AnyDown( "8", "DIGIT8" ),
            ManagedDoom.DoomKey.Num9 => AnyDown( "9", "DIGIT9" ),
            ManagedDoom.DoomKey.Escape => AnyDown( "ESCAPE" ),
            ManagedDoom.DoomKey.LControl => AnyDown( "LCONTROL", "LCTRL", "CTRL" ),
            ManagedDoom.DoomKey.RControl => AnyDown( "RCONTROL", "RCTRL" ),
            ManagedDoom.DoomKey.LShift => AnyDown( "LSHIFT", "SHIFT" ),
            ManagedDoom.DoomKey.RShift => AnyDown( "RSHIFT" ),
            ManagedDoom.DoomKey.LAlt => AnyDown( "LALT", "ALT" ),
            ManagedDoom.DoomKey.RAlt => AnyDown( "RALT" ),
            ManagedDoom.DoomKey.Space => AnyDown( "SPACE" ),
            ManagedDoom.DoomKey.Enter => AnyDown( "ENTER", "RETURN" ),
            ManagedDoom.DoomKey.Backspace => AnyDown( "BACKSPACE" ),
            ManagedDoom.DoomKey.Tab => AnyDown( "TAB" ),
            ManagedDoom.DoomKey.PageUp => AnyDown( "PAGEUP" ),
            ManagedDoom.DoomKey.PageDown => AnyDown( "PAGEDOWN" ),
            ManagedDoom.DoomKey.End => AnyDown( "END" ),
            ManagedDoom.DoomKey.Home => AnyDown( "HOME" ),
            ManagedDoom.DoomKey.Insert => AnyDown( "INSERT" ),
            ManagedDoom.DoomKey.Delete => AnyDown( "DELETE" ),
            ManagedDoom.DoomKey.Left => AnyDown( "LEFT" ),
            ManagedDoom.DoomKey.Right => AnyDown( "RIGHT" ),
            ManagedDoom.DoomKey.Up => AnyDown( "UP" ),
            ManagedDoom.DoomKey.Down => AnyDown( "DOWN" ),
            ManagedDoom.DoomKey.F1 => AnyDown( "F1" ),
            ManagedDoom.DoomKey.F2 => AnyDown( "F2" ),
            ManagedDoom.DoomKey.F3 => AnyDown( "F3" ),
            ManagedDoom.DoomKey.F4 => AnyDown( "F4" ),
            ManagedDoom.DoomKey.F5 => AnyDown( "F5" ),
            ManagedDoom.DoomKey.F6 => AnyDown( "F6" ),
            ManagedDoom.DoomKey.F7 => AnyDown( "F7" ),
            ManagedDoom.DoomKey.F8 => AnyDown( "F8" ),
            ManagedDoom.DoomKey.F9 => AnyDown( "F9" ),
            ManagedDoom.DoomKey.F10 => AnyDown( "F10" ),
            ManagedDoom.DoomKey.F11 => AnyDown( "F11" ),
            ManagedDoom.DoomKey.F12 => AnyDown( "F12" ),
            ManagedDoom.DoomKey.Pause => AnyDown( "PAUSE" ),
            _ => false
        };
    }

    private static bool AnyDown( params string[] keys )
    {
        foreach ( var key in keys )
        {
            if ( Input.Keyboard.Down( key ) )
            {
                return true;
            }
        }

        return false;
    }
}
