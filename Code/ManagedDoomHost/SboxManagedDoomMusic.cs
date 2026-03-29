using System;
using System.IO;
using MeltySynth;

namespace Sandbox;

public sealed class SboxManagedDoomMusic : ManagedDoom.Audio.IMusic
{
    private const string SoundFontPath = "doom/GeneralUser-GS.sf2";
    private const int SampleRate = 44100;
    private const int BlockLength = SampleRate / 140;
    private const int ChunkFrames = 1024;
    private const float RefillThresholdSeconds = 0.35f;
    private const float TargetBufferedSeconds = 0.75f;
    private const int MaxChunksPerUpdate = 2;

    private readonly ManagedDoom.Config config;
    private readonly ManagedDoom.GameContent content;
    private readonly Synthesizer synthesizer;
    private readonly float[] left;
    private readonly float[] right;
    private readonly short[] interleavedChunk;

    private SoundStream currentStream;
    private SoundHandle currentHandle;
    private ManagedDoom.Bgm currentBgm;
    private bool currentLoop;
    private IDecoder currentDecoder;
    private float queuedSeconds;

    public SboxManagedDoomMusic( ManagedDoom.Config config, ManagedDoom.GameContent content )
    {
        this.config = config;
        this.content = content;

        config.audio_musicvolume = Math.Clamp( config.audio_musicvolume, 0, MaxVolume );

        using var fontStream = new MemoryStream( ManagedDoom.SboxManagedDoomFileSystem.ReadAllBytes( SoundFontPath ), false );
        var soundFont = new SoundFont( fontStream );
        var settings = new SynthesizerSettings( SampleRate )
        {
            BlockSize = BlockLength,
            EnableReverbAndChorus = config.audio_musiceffect
        };

        synthesizer = new Synthesizer( soundFont, settings );
        left = new float[ChunkFrames];
        right = new float[ChunkFrames];
        interleavedChunk = new short[ChunkFrames * 2];
        currentBgm = ManagedDoom.Bgm.NONE;
    }

    public void StartMusic( ManagedDoom.Bgm bgm, bool loop )
    {
        if ( bgm == currentBgm && loop == currentLoop )
        {
            return;
        }

        StopCurrent();

        currentBgm = bgm;
        currentLoop = loop;

        if ( bgm == ManagedDoom.Bgm.NONE )
        {
            return;
        }

        var lumpName = "D_" + ManagedDoom.DoomInfo.BgmNames[(int)bgm].ToString().ToUpperInvariant();
        var data = content.Wad.ReadLump( lumpName );
        currentDecoder = CreateDecoder( data, loop );

        synthesizer.Reset();

        currentStream = new SoundStream( SampleRate );
        queuedSeconds = 0.0f;
        FillBufferToTarget();

        currentHandle = currentStream.Play();
        currentHandle.ListenLocal = true;
        currentHandle.Volume = config.audio_musicvolume / (float)MaxVolume;
    }

    public void Update()
    {
        if ( currentBgm == ManagedDoom.Bgm.NONE || currentDecoder is null )
        {
            return;
        }

        if ( !currentHandle.IsValid() )
        {
            return;
        }

        queuedSeconds = Math.Max( 0.0f, queuedSeconds - Time.Delta );

        if ( queuedSeconds < RefillThresholdSeconds )
        {
            FillBufferToTarget();
        }

        if ( currentHandle.IsStopped )
        {
            if ( !currentLoop && currentDecoder.Ended && queuedSeconds <= 0.0f )
            {
                currentBgm = ManagedDoom.Bgm.NONE;
                StopCurrent();
                return;
            }

            currentHandle = currentStream.Play();
            currentHandle.ListenLocal = true;
            currentHandle.Volume = config.audio_musicvolume / (float)MaxVolume;
        }
    }

    public int MaxVolume => 15;

    public int Volume
    {
        get => config.audio_musicvolume;
        set
        {
            config.audio_musicvolume = Math.Clamp( value, 0, MaxVolume );
            if ( currentHandle.IsValid() )
            {
                currentHandle.Volume = config.audio_musicvolume / (float)MaxVolume;
            }
        }
    }

    private void FillBufferToTarget()
    {
        var chunksWritten = 0;
        while ( queuedSeconds < TargetBufferedSeconds && currentDecoder is not null && chunksWritten < MaxChunksPerUpdate )
        {
            WriteChunk();
            chunksWritten++;

            if ( currentDecoder.Ended && !currentLoop )
            {
                break;
            }
        }
    }

    private void WriteChunk()
    {
        Array.Clear( left, 0, left.Length );
        Array.Clear( right, 0, right.Length );
        currentDecoder.RenderWaveform( synthesizer, left, right );

        var position = 0;
        for ( var i = 0; i < ChunkFrames; i++ )
        {
            interleavedChunk[position++] = ClampToShort( left[i] * 32767.0f );
            interleavedChunk[position++] = ClampToShort( right[i] * 32767.0f );
        }

        currentStream.WriteData( interleavedChunk );
        queuedSeconds += ChunkFrames / (float)SampleRate;
    }

    private static short ClampToShort( float sample )
    {
        var value = (int)sample;
        if ( value < short.MinValue )
        {
            return short.MinValue;
        }

        if ( value > short.MaxValue )
        {
            return short.MaxValue;
        }

        return (short)value;
    }

    private IDecoder CreateDecoder( byte[] data, bool loop )
    {
        if ( data.Length >= MusDecoder.MusHeader.Length && MatchesHeader( data, MusDecoder.MusHeader ) )
        {
            return new MusDecoder( data, loop );
        }

        if ( data.Length >= MidiDecoder.MidiHeader.Length && MatchesHeader( data, MidiDecoder.MidiHeader ) )
        {
            return new MidiDecoder( data, loop );
        }

        throw new Exception( "Unknown music format." );
    }

    private static bool MatchesHeader( byte[] data, byte[] header )
    {
        for ( var i = 0; i < header.Length; i++ )
        {
            if ( data[i] != header[i] )
            {
                return false;
            }
        }

        return true;
    }

    private void StopCurrent()
    {
        if ( currentHandle.IsValid() )
        {
            currentHandle.Stop();
        }

        if ( currentStream is not null )
        {
            currentStream.Close();
            currentStream = null;
        }

        currentDecoder = null;
        queuedSeconds = 0.0f;
    }

    private interface IDecoder
    {
        bool Ended { get; }
        void RenderWaveform( Synthesizer synthesizer, Span<float> left, Span<float> right );
    }

    private sealed class MusDecoder : IDecoder
    {
        public static readonly byte[] MusHeader =
        {
            (byte)'M',
            (byte)'U',
            (byte)'S',
            0x1A
        };

        private readonly bool loop;
        private readonly MusEvent[] events;
        private readonly int[] lastVolume;
        private readonly byte[] data;
        private readonly int scoreStart;
        private int position;
        private int delay;
        private int eventCount;
        private int blockWrote;
        private bool ended;

        public MusDecoder( byte[] data, bool loop )
        {
            this.data = data;
            this.loop = loop;
            scoreStart = BitConverter.ToUInt16( data, 6 );
            events = new MusEvent[128];
            for ( var i = 0; i < events.Length; i++ )
            {
                events[i] = new MusEvent();
            }

            lastVolume = new int[16];
            Reset();
            blockWrote = BlockLength;
        }

        public bool Ended => ended && !loop;

        public void RenderWaveform( Synthesizer synthesizer, Span<float> left, Span<float> right )
        {
            var wrote = 0;
            while ( wrote < left.Length )
            {
                if ( blockWrote == synthesizer.BlockSize )
                {
                    ProcessMidiEvents( synthesizer );
                    blockWrote = 0;
                }

                var srcRemaining = synthesizer.BlockSize - blockWrote;
                var dstRemaining = left.Length - wrote;
                var remaining = Math.Min( srcRemaining, dstRemaining );

                synthesizer.Render( left.Slice( wrote, remaining ), right.Slice( wrote, remaining ) );
                blockWrote += remaining;
                wrote += remaining;
            }
        }

        private void ProcessMidiEvents( Synthesizer synthesizer )
        {
            if ( ended && !loop )
            {
                return;
            }

            if ( delay > 0 )
            {
                delay--;
            }

            if ( delay != 0 )
            {
                return;
            }

            delay = ReadSingleEventGroup();
            SendEvents( synthesizer );

            if ( delay == -1 )
            {
                synthesizer.NoteOffAll( false );
                ended = true;
                if ( loop )
                {
                    Reset();
                }
            }
        }

        private void Reset()
        {
            Array.Clear( lastVolume, 0, lastVolume.Length );
            position = scoreStart;
            delay = 0;
            ended = false;
        }

        private int ReadSingleEventGroup()
        {
            eventCount = 0;
            while ( true )
            {
                var result = ReadSingleEvent();
                if ( result == ReadResult.EndOfGroup )
                {
                    break;
                }

                if ( result == ReadResult.EndOfFile )
                {
                    return -1;
                }
            }

            var time = 0;
            while ( true )
            {
                var value = data[position++];
                time = time * 128 + ( value & 127 );
                if ( ( value & 128 ) == 0 )
                {
                    break;
                }
            }

            return time;
        }

        private ReadResult ReadSingleEvent()
        {
            var channelNumber = data[position] & 0xF;
            if ( channelNumber == 15 )
            {
                channelNumber = 9;
            }
            else if ( channelNumber >= 9 )
            {
                channelNumber++;
            }

            var eventType = ( data[position] & 0x70 ) >> 4;
            var last = ( data[position] >> 7 ) != 0;
            position++;

            var musEvent = events[eventCount++];

            switch ( eventType )
            {
                case 0:
                    musEvent.Type = 0;
                    musEvent.Channel = channelNumber;
                    musEvent.Data1 = data[position++];
                    musEvent.Data2 = 0;
                    break;

                case 1:
                    musEvent.Type = 1;
                    musEvent.Channel = channelNumber;
                    var playNote = data[position++];
                    var noteNumber = playNote & 127;
                    var noteVolume = ( playNote & 128 ) != 0 ? data[position++] : -1;
                    musEvent.Data1 = noteNumber;
                    if ( noteVolume == -1 )
                    {
                        musEvent.Data2 = lastVolume[channelNumber];
                    }
                    else
                    {
                        musEvent.Data2 = noteVolume;
                        lastVolume[channelNumber] = noteVolume;
                    }
                    break;

                case 2:
                    musEvent.Type = 2;
                    musEvent.Channel = channelNumber;
                    var pitchWheel = data[position++];
                    var pw2 = ( pitchWheel << 7 ) / 2;
                    musEvent.Data1 = pw2 & 127;
                    musEvent.Data2 = pw2 >> 7;
                    break;

                case 3:
                    musEvent.Type = 3;
                    musEvent.Channel = channelNumber;
                    musEvent.Data1 = data[position++];
                    musEvent.Data2 = 0;
                    break;

                case 4:
                    musEvent.Type = 4;
                    musEvent.Channel = channelNumber;
                    musEvent.Data1 = data[position++];
                    musEvent.Data2 = data[position++];
                    break;

                case 6:
                    return ReadResult.EndOfFile;

                default:
                    throw new Exception( "Unknown MUS event type." );
            }

            return last ? ReadResult.EndOfGroup : ReadResult.Ongoing;
        }

        private void SendEvents( Synthesizer synthesizer )
        {
            for ( var i = 0; i < eventCount; i++ )
            {
                var musEvent = events[i];
                switch ( musEvent.Type )
                {
                    case 0:
                        synthesizer.NoteOff( musEvent.Channel, musEvent.Data1 );
                        break;

                    case 1:
                        synthesizer.NoteOn( musEvent.Channel, musEvent.Data1, musEvent.Data2 );
                        break;

                    case 2:
                        synthesizer.ProcessMidiMessage( musEvent.Channel, 0xE0, musEvent.Data1, musEvent.Data2 );
                        break;

                    case 3:
                        switch ( musEvent.Data1 )
                        {
                            case 11:
                                synthesizer.NoteOffAll( musEvent.Channel, false );
                                break;
                            case 14:
                                synthesizer.ResetAllControllers( musEvent.Channel );
                                break;
                        }
                        break;

                    case 4:
                        switch ( musEvent.Data1 )
                        {
                            case 0:
                                synthesizer.ProcessMidiMessage( musEvent.Channel, 0xC0, musEvent.Data2, 0 );
                                break;
                            case 1:
                                synthesizer.ProcessMidiMessage( musEvent.Channel, 0xB0, 0x00, musEvent.Data2 );
                                break;
                            case 2:
                                synthesizer.ProcessMidiMessage( musEvent.Channel, 0xB0, 0x01, musEvent.Data2 );
                                break;
                            case 3:
                                synthesizer.ProcessMidiMessage( musEvent.Channel, 0xB0, 0x07, musEvent.Data2 );
                                break;
                            case 4:
                                synthesizer.ProcessMidiMessage( musEvent.Channel, 0xB0, 0x0A, musEvent.Data2 );
                                break;
                            case 5:
                                synthesizer.ProcessMidiMessage( musEvent.Channel, 0xB0, 0x0B, musEvent.Data2 );
                                break;
                            case 6:
                                synthesizer.ProcessMidiMessage( musEvent.Channel, 0xB0, 0x5B, musEvent.Data2 );
                                break;
                            case 7:
                                synthesizer.ProcessMidiMessage( musEvent.Channel, 0xB0, 0x5D, musEvent.Data2 );
                                break;
                            case 8:
                                synthesizer.ProcessMidiMessage( musEvent.Channel, 0xB0, 0x40, musEvent.Data2 );
                                break;
                        }
                        break;
                }
            }
        }

        private sealed class MusEvent
        {
            public int Type;
            public int Channel;
            public int Data1;
            public int Data2;
        }

        private enum ReadResult
        {
            Ongoing,
            EndOfGroup,
            EndOfFile
        }
    }

    private sealed class MidiDecoder : IDecoder
    {
        public static readonly byte[] MidiHeader =
        {
            (byte)'M',
            (byte)'T',
            (byte)'h',
            (byte)'d'
        };

        private readonly MidiFile midi;
        private readonly bool loop;
        private MidiFileSequencer sequencer;

        public MidiDecoder( byte[] data, bool loop )
        {
            midi = new MidiFile( new MemoryStream( data, false ) );
            this.loop = loop;
        }

        public bool Ended => sequencer is not null && sequencer.EndOfSequence && !loop;

        public void RenderWaveform( Synthesizer synthesizer, Span<float> left, Span<float> right )
        {
            if ( sequencer is null )
            {
                sequencer = new MidiFileSequencer( synthesizer );
                sequencer.Play( midi, loop );
            }

            sequencer.Render( left, right );
        }
    }
}
