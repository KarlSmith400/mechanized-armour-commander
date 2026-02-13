using System.IO;
using System.Media;
using System.Windows;

namespace MechanizedArmourCommander.UI;

/// <summary>
/// Centralized sound manager. Loads WAV files from embedded resources
/// and provides fire-and-forget playback methods.
/// </summary>
public static class AudioService
{
    private static readonly Dictionary<string, SoundPlayer> _sounds = new();
    private static bool _muted;

    static AudioService()
    {
        LoadSound("ui_click");
        LoadSound("weapon_fire");
        LoadSound("hit_confirm");
        LoadSound("miss");
        LoadSound("mech_destroyed");
        LoadSound("turn_start");
        LoadSound("error");
        LoadSound("victory");
        LoadSound("defeat");
    }

    private static void LoadSound(string name)
    {
        try
        {
            var uri = new Uri($"pack://application:,,,/Resources/Sounds/{name}.wav");
            var streamInfo = Application.GetResourceStream(uri);
            if (streamInfo != null)
            {
                var player = new SoundPlayer(streamInfo.Stream);
                player.Load();
                _sounds[name] = player;
            }
        }
        catch { /* Sound missing — will silently skip playback */ }
    }

    private static void Play(string name)
    {
        if (_muted) return;
        if (_sounds.TryGetValue(name, out var player))
        {
            try { player.Play(); }
            catch { /* Ignore playback errors */ }
        }
    }

    // UI
    public static void PlayClick() => Play("ui_click");
    public static void PlayError() => Play("error");

    // Combat
    public static void PlayFire() => Play("weapon_fire");
    public static void PlayHit() => Play("hit_confirm");
    public static void PlayMiss() => Play("miss");
    public static void PlayDestroyed() => Play("mech_destroyed");
    public static void PlayTurnStart() => Play("turn_start");

    // Outcomes
    public static void PlayVictory() => Play("victory");
    public static void PlayDefeat() => Play("defeat");

    // Settings
    public static bool IsMuted
    {
        get => _muted;
        set => _muted = value;
    }
}
