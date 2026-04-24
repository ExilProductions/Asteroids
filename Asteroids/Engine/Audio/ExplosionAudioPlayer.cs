using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace Asteroids.Engine.Audio;

public sealed class SoundEffectPlayer : IDisposable
{
    private readonly List<MediaPlayer> _activePlayers = new();
    private readonly TimeSpan _cooldown;
    private readonly string _soundPath;
    private readonly Uri _soundUri;
    private readonly double _volume;
    private DateTime _lastPlayAt;

    public SoundEffectPlayer(Stream soundStream, string filePrefix, double volume, TimeSpan? cooldown = null)
    {
        _cooldown = cooldown ?? TimeSpan.FromMilliseconds(60);
        _volume = volume;
        _soundPath = Path.Combine(Path.GetTempPath(), $"{filePrefix}_{Guid.NewGuid():N}.mp3");

        using var destination = File.Create(_soundPath);
        soundStream.CopyTo(destination);
        _soundUri = new Uri(_soundPath);
    }

    public void Play()
    {
        DateTime now = DateTime.UtcNow;
        if (now - _lastPlayAt < _cooldown)
        {
            return;
        }

        _lastPlayAt = now;
        var player = new MediaPlayer();
        _activePlayers.Add(player);
        player.Volume = _volume;
        player.MediaEnded += (_, _) => ReleasePlayer(player);
        player.MediaFailed += (_, _) => ReleasePlayer(player);
        player.Open(_soundUri);
        player.Play();
    }

    private void ReleasePlayer(MediaPlayer player)
    {
        player.Close();
        _activePlayers.Remove(player);
    }

    public void Dispose()
    {
        for (int i = _activePlayers.Count - 1; i >= 0; i--)
        {
            _activePlayers[i].Close();
        }

        _activePlayers.Clear();
        if (File.Exists(_soundPath))
        {
            File.Delete(_soundPath);
        }
    }
}
