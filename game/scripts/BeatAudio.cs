using System;
using Godot;
using Vlg.Core.Config;
using Vlg.Core.Run;

namespace Vlg.Game;

/// <summary>
/// Placeholder groove from the sim's beat clock (no music track yet, design/heroes/lowkey.md Open):
/// kick on 1 and 3 (every beat in the Drop), clap on 2 and 4, off-beat hats, riser in the build-up,
/// boom on the Drop; skill casts add their own one-shots (cowbell plays a riff). Samples: tools/audio/make_beat_samples.py.
/// Driven by sim ticks, so it stays in sync and stops while the sim is paused.
/// </summary>
public sealed class BeatAudio
{
    const int Voices = 24;
    /// <summary>Cowbell riff, semitones per cast.</summary>
    static readonly int[] Riff = { 0, 0, 3, 0, 7, 5, 3, -2 };

    readonly AudioStreamPlayer[] _voices = new AudioStreamPlayer[Voices];
    readonly AudioStream _kick, _clap, _hat, _cowbell, _sub, _slap, _thump, _riser, _boom;
    int _next;
    int _riffStep;

    public bool Muted { get; set; }

    public BeatAudio(Node parent)
    {
        _kick = Load("kick");
        _clap = Load("clap");
        _hat = Load("hat");
        _cowbell = Load("cowbell");
        _sub = Load("sub");
        _slap = Load("slap");
        _thump = Load("thump");
        _riser = Load("riser");
        _boom = Load("boom");
        for (int i = 0; i < Voices; i++)
        {
            _voices[i] = new AudioStreamPlayer { Name = $"BeatVoice{i}" };
            parent.AddChild(_voices[i]);
        }
    }

    /// <param name="stepped">The sim stepped this frame: its tick and fire flags are fresh.</param>
    public void Update(RunSim sim, bool stepped)
    {
        if (!stepped || Muted) return;
        var b = sim.Beat;
        if (b.HalfTick && !b.BeatTick) Play(_hat, -20f);
        if (b.BeatTick)
        {
            int inBar = b.BeatInBar;
            if (b.InDrop || inBar % 2 == 0) Play(_kick, -7f);
            if (inBar % 2 == 1) Play(_clap, -13f);
            Play(_hat, -24f);
        }
        if (b.BuildStarted) Play(_riser, -12f);
        if (b.DropStarted) Play(_boom, -4f);

        var skills = sim.Skills;
        for (int slot = 0; slot < skills.Count; slot++)
        {
            if (!skills.FiredThisStep[slot]) continue;
            switch (skills.Def(slot).Kind)
            {
                case SkillKind.Projectile:
                    Play(_cowbell, -12f, MathF.Pow(2f, Riff[_riffStep++ % Riff.Length] / 12f));
                    break;
                case SkillKind.Ring: Play(_sub, -8f); break;
                case SkillKind.Beam: Play(_slap, -9f); break;
                case SkillKind.Speaker: Play(_thump, -6f); break;
            }
        }
    }

    void Play(AudioStream stream, float db, float pitch = 1f)
    {
        var v = _voices[_next];
        _next = (_next + 1) % Voices;
        v.Stream = stream;
        v.VolumeDb = db;
        v.PitchScale = pitch;
        v.Play();
    }

    static AudioStream Load(string name) => GD.Load<AudioStream>($"res://assets/audio/beat/{name}.wav");
}
