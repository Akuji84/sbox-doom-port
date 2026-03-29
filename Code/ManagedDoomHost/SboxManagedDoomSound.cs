using System;
using System.Collections.Generic;
namespace Sandbox;

public sealed class SboxManagedDoomSound : ManagedDoom.Audio.ISound
{
    private readonly Dictionary<ManagedDoom.Sfx, CachedSfx> cached = new();
    private readonly List<ActiveSound> active = new();
    private readonly ManagedDoom.Config config;
    private readonly ManagedDoom.GameContent content;
    private int volume;

    public SboxManagedDoomSound( ManagedDoom.Config config, ManagedDoom.GameContent content )
    {
        this.config = config;
        this.content = content;
        volume = Math.Clamp( config.audio_soundvolume, 0, MaxVolume );
    }

    public void SetListener( ManagedDoom.Mobj listener )
    {
    }

    public void Update()
    {
        for ( var i = active.Count - 1; i >= 0; i-- )
        {
            var sound = active[i];
            if ( !sound.Handle.IsValid() || sound.Handle.IsStopped )
            {
                sound.Stream.Close();
                active.RemoveAt( i );
            }
        }
    }

    public void StartSound( ManagedDoom.Sfx sfx )
    {
        PlaySfx( sfx, 1.0f, 1.0f );
    }

    public void StartSound( ManagedDoom.Mobj mobj, ManagedDoom.Sfx sfx, ManagedDoom.SfxType type )
    {
        StartSound( mobj, sfx, type, 100 );
    }

    public void StartSound( ManagedDoom.Mobj mobj, ManagedDoom.Sfx sfx, ManagedDoom.SfxType type, int sfxVolume )
    {
        var normalized = Math.Clamp( sfxVolume / 100.0f, 0.0f, 1.0f );
        PlaySfx( sfx, normalized, 1.0f );
    }

    public void StopSound( ManagedDoom.Mobj mobj )
    {
    }

    public void Reset()
    {
        for ( var i = 0; i < active.Count; i++ )
        {
            active[i].Stream.Close();
        }

        active.Clear();
    }

    public void Pause()
    {
        for ( var i = 0; i < active.Count; i++ )
        {
            active[i].Handle.Paused = true;
        }
    }

    public void Resume()
    {
        for ( var i = 0; i < active.Count; i++ )
        {
            active[i].Handle.Paused = false;
        }
    }

    public int MaxVolume => 15;

    public int Volume
    {
        get => volume;
        set => volume = Math.Clamp( value, 0, MaxVolume );
    }

    private void PlaySfx( ManagedDoom.Sfx sfx, float sourceVolume, float pitch )
    {
        if ( !TryGetCachedSfx( sfx, out var data ) )
        {
            return;
        }

        var stream = new SoundStream( data.SampleRate );
        var handle = stream.Play();
        handle.ListenLocal = true;
        handle.Volume = ( volume / (float)MaxVolume ) * sourceVolume;
        handle.Pitch = pitch;
        stream.WriteData( data.Samples );
        stream.Close();
        active.Add( new ActiveSound( stream, handle ) );
    }

    private bool TryGetCachedSfx( ManagedDoom.Sfx sfx, out CachedSfx data )
    {
        if ( cached.TryGetValue( sfx, out data ) )
        {
            return true;
        }

        var lumpName = "DS" + ManagedDoom.DoomInfo.SfxNames[(int)sfx].ToString().ToUpperInvariant();
        var lumpNumber = content.Wad.GetLumpNumber( lumpName );
        if ( lumpNumber == -1 )
        {
            data = default;
            return false;
        }

        var lump = content.Wad.ReadLump( lumpNumber );
        if ( lump.Length < 8 )
        {
            data = default;
            return false;
        }

        var sampleRate = BitConverter.ToUInt16( lump, 2 );
        var sampleCount = BitConverter.ToInt32( lump, 4 );
        var offset = 8;

        if ( ContainsDmxPadding( lump, sampleCount ) )
        {
            offset += 16;
            sampleCount -= 32;
        }

        if ( sampleRate <= 0 || sampleCount <= 0 || offset + sampleCount > lump.Length )
        {
            data = default;
            return false;
        }

        var samples = new short[sampleCount];
        for ( var i = 0; i < sampleCount; i++ )
        {
            samples[i] = (short)((lump[offset + i] - 128) << 8);
        }

        data = new CachedSfx( sampleRate, samples );
        cached[sfx] = data;
        return true;
    }

    private static bool ContainsDmxPadding( byte[] data, int sampleCount )
    {
        if ( sampleCount < 32 || data.Length < 8 + sampleCount )
        {
            return false;
        }

        var first = data[8];
        for ( var i = 1; i < 16; i++ )
        {
            if ( data[8 + i] != first )
            {
                return false;
            }
        }

        var last = data[8 + sampleCount - 1];
        for ( var i = 1; i < 16; i++ )
        {
            if ( data[8 + sampleCount - i - 1] != last )
            {
                return false;
            }
        }

        return true;
    }

    private readonly record struct CachedSfx( int SampleRate, short[] Samples );
    private readonly record struct ActiveSound( SoundStream Stream, SoundHandle Handle );
}
