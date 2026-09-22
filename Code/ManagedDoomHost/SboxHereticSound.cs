// Copyright (C) 2026 s&Doom contributors; SPDX-License-Identifier: GPL-2.0-or-later
using System;
using System.Collections.Generic;
using ManagedDoom;
namespace Sandbox;

/// <summary>Local preview audio; full Heretic positional mixing remains separate.</summary>
public sealed class SboxHereticSound : IDisposable
{
    private readonly HereticSoundAttenuation attenuation;
    private readonly HereticWorldSession session;
    private readonly Dictionary<HereticSoundId, HereticSoundData> cache = new();
    private readonly List<(SoundStream stream, SoundHandle handle, Mobj source)> active = new();
    public float Volume { get; set; } = 0.4f;
    public SboxHereticSound(GameContent content, HereticWorldSession session)
    {
        this.session = session;
        var curve = content.Wad.GetLumpNumber("SNDCURVE");
        if (curve < 0) throw new InvalidOperationException("Missing Heretic SNDCURVE.");
        attenuation = new HereticSoundAttenuation(content.Wad.ReadLump(curve));
        // Validate enabled assets before subscribing, so playback cannot partially initialize.
        foreach (HereticSoundId id in Enum.GetValues(typeof(HereticSoundId)))
        {
            var name = HereticSoundData.LumpName(id);
            if (name == null) continue;
            var lump = content.Wad.GetLumpNumber(name);
            if (lump < 0) throw new InvalidOperationException("Missing Heretic sound: " + name);
            cache[id] = HereticSoundData.Decode(content.Wad.ReadLump(lump));
        }
        session.SoundRequested += Play;
    }
    public void Update()
    {
        for (var i = active.Count - 1; i >= 0; i--)
            if (!active[i].handle.IsValid() || active[i].handle.IsStopped)
            { active[i].stream.Close(); active.RemoveAt(i); }
            else active[i].handle.Volume = OutputVolume(active[i].source);
    }
    private float OutputVolume(Mobj source) => (float.IsFinite(Volume) ? Math.Clamp(Volume, 0, 1) : 0) * attenuation.Gain(source, session.Body);
    private void Play(HereticSoundId id, Mobj source)
    {
        Update();
        if (!cache.TryGetValue(id, out var data) || OutputVolume(source) <= 0) return;
        if (active.Count >= 16)
        { active[0].handle.Stop(); active[0].stream.Close(); active.RemoveAt(0); }
        var stream = new SoundStream(data.SampleRate, 2);
        var handle = stream.Play();
        handle.ListenLocal = true;
        handle.Volume = OutputVolume(source);
        stream.WriteData(HereticSoundAttenuation.Stereo(data.Samples, HereticSoundAttenuation.Separation(source, session.Body))); stream.Close();
        active.Add((stream, handle, source));
    }
    public void Dispose()
    {
        session.SoundRequested -= Play;
        foreach (var sound in active) { sound.handle.Stop(); sound.stream.Close(); }
        active.Clear(); cache.Clear();
    }
}
