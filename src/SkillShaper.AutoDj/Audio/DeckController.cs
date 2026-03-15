using SkillShaper.AutoDj.Models;

#if ANDROID
using Android.Media;
using Android.Media.Audiofx;
#endif

namespace SkillShaper.AutoDj.Audio;

public sealed class DeckController : IDisposable
{
    private readonly object _gate = new();
    private readonly int _deckId;

#if ANDROID
    private MediaPlayer? _player;
    private Equalizer? _equalizer;
#endif

    private IDispatcherTimer? _loopTimer;
    private IDispatcherTimer? _chopTimer;
    private int _loopStartMs;
    private int _loopLengthMs;
    private bool _isChopMuted;

    private double _userVolume = 0.85;
    private double _mixGain = 1.0;

    public DeckController(int deckId)
    {
        _deckId = deckId;
    }

    public int DeckId => _deckId;

    public TrackInfo? CurrentTrack { get; private set; }

    public bool IsPlaying
    {
        get
        {
#if ANDROID
            return _player?.IsPlaying == true;
#else
            return false;
#endif
        }
    }

    public void Dispose()
    {
        StopInternal();
        _loopTimer?.Stop();
        _chopTimer?.Stop();
        _loopTimer = null;
        _chopTimer = null;
    }

    public async Task LoadTrackAsync(TrackInfo track)
    {
#if ANDROID
        await Task.Run(() =>
        {
            lock (_gate)
            {
                StopInternal();

                _player = new MediaPlayer();
                _player.SetDataSource(track.Path);
                _player.Prepare();
                _player.Looping = false;

                try
                {
                    _equalizer = new Equalizer(0, _player.AudioSessionId)
                    {
                    };
                    _equalizer.SetEnabled(true);
                }
                catch
                {
                    _equalizer = null;
                }

                CurrentTrack = track;
                ApplyVolume();
            }
        });
#else
        CurrentTrack = track;
        await Task.CompletedTask;
#endif
    }

    public void Play()
    {
#if ANDROID
        lock (_gate)
        {
            _player?.Start();
        }
#endif
    }

    public void Pause()
    {
#if ANDROID
        lock (_gate)
        {
            _player?.Pause();
        }
#endif
    }

    public void Stop()
    {
        lock (_gate)
        {
            StopInternal();
        }
    }

    public int GetCurrentPositionMs()
    {
#if ANDROID
        lock (_gate)
        {
            return _player?.CurrentPosition ?? 0;
        }
#else
        return 0;
#endif
    }

    public int GetDurationMs()
    {
#if ANDROID
        lock (_gate)
        {
            if (_player is not null)
            {
                try
                {
                    return Math.Max(0, _player.Duration);
                }
                catch
                {
                    return 0;
                }
            }
        }
#endif

        return (int)Math.Max(0, CurrentTrack?.Duration.TotalMilliseconds ?? 0);
    }

    public double GetEstimatedBpm() => CurrentTrack?.EstimatedBpm ?? 120;

    public void SetUserVolume(double volume)
    {
        _userVolume = Math.Clamp(volume, 0, 1);
        ApplyVolume();
    }

    public void SetMixGain(double gain)
    {
        _mixGain = Math.Clamp(gain, 0, 1);
        ApplyVolume();
    }

    public void SetChopEnabled(bool enabled, double rateHz, IDispatcher dispatcher)
    {
        _chopTimer?.Stop();
        _isChopMuted = false;

        if (!enabled)
        {
            ApplyVolume();
            return;
        }

        var safeRate = Math.Clamp(rateHz, 2, 24);
        var intervalMs = Math.Max(20, (int)(1000 / (safeRate * 2)));

        _chopTimer = dispatcher.CreateTimer();
        _chopTimer.Interval = TimeSpan.FromMilliseconds(intervalMs);
        _chopTimer.Tick += (_, _) =>
        {
            _isChopMuted = !_isChopMuted;
            ApplyVolume();
        };
        _chopTimer.Start();
    }

    public void SetLoopEnabled(bool enabled, double bars, double bpm, IDispatcher dispatcher)
    {
        _loopTimer?.Stop();

        if (!enabled)
        {
            return;
        }

        var safeBars = Math.Clamp(bars, 0.25, 4);
        var safeBpm = Math.Clamp(bpm, 60, 190);

        var loopLengthMs = (int)(safeBars * 4 * 60000 / safeBpm);
        SetLoopWindow(GetCurrentPositionMs(), loopLengthMs, dispatcher);
    }

    public void SetLoopWindow(int startMs, int lengthMs, IDispatcher dispatcher)
    {
        _loopTimer?.Stop();

        _loopStartMs = Math.Max(0, startMs);
        _loopLengthMs = Math.Max(120, lengthMs);

        _loopTimer = dispatcher.CreateTimer();
        _loopTimer.Interval = TimeSpan.FromMilliseconds(45);
        _loopTimer.Tick += (_, _) =>
        {
#if ANDROID
            lock (_gate)
            {
                if (_player is null || !IsPlaying)
                {
                    return;
                }

                var current = _player.CurrentPosition;
                if (current >= _loopStartMs + _loopLengthMs)
                {
                    _player.SeekTo(_loopStartMs);
                }
            }
#endif
        };
        _loopTimer.Start();
    }

    public void DisableLoop()
    {
        _loopTimer?.Stop();
    }

    public void SeekToMs(int positionMs)
    {
#if ANDROID
        lock (_gate)
        {
            if (_player is null)
            {
                return;
            }

            var duration = 0;
            try
            {
                duration = Math.Max(0, _player.Duration);
            }
            catch
            {
                duration = 0;
            }

            var target = duration > 0
                ? Math.Clamp(positionMs, 0, Math.Max(0, duration - 1))
                : Math.Max(0, positionMs);

            _player.SeekTo(target);
        }
#endif
    }

    // Applies a broad three-zone EQ shape for softer transition edges.
    public void ApplyTransitionEq(double progress)
    {
#if ANDROID
        lock (_gate)
        {
            if (_equalizer is null)
            {
                return;
            }

            var p = Math.Clamp(progress, 0, 1);
            var bass = (short)Lerp(200, 800, p);
            var mids = (short)Lerp(400, 1200, p);
            var highs = (short)Lerp(600, 1300, p);

            for (short band = 0; band < _equalizer.NumberOfBands; band++)
            {
                var center = _equalizer.GetCenterFreq(band) / 1000;
                var level = center switch
                {
                    < 250 => bass,
                    < 2500 => mids,
                    _ => highs
                };

                var range = _equalizer.GetBandLevelRange();
                level = (short)Math.Clamp(level, range[0], range[1]);
                _equalizer.SetBandLevel(band, level);
            }
        }
#endif
    }

    private void ApplyVolume()
    {
#if ANDROID
        lock (_gate)
        {
            if (_player is null)
            {
                return;
            }

            var finalGain = _isChopMuted ? 0 : (float)(_userVolume * _mixGain);
            _player.SetVolume(finalGain, finalGain);
        }
#endif
    }

    private void StopInternal()
    {
#if ANDROID
        _loopTimer?.Stop();
        _chopTimer?.Stop();

        if (_player is not null)
        {
            if (_player.IsPlaying)
            {
                _player.Stop();
            }

            _player.Release();
            _player.Dispose();
            _player = null;
        }

        _equalizer?.Release();
        _equalizer?.Dispose();
        _equalizer = null;
#endif

        CurrentTrack = null;
    }

    private static int Lerp(int start, int end, double t)
    {
        return (int)(start + ((end - start) * t));
    }
}
