using SkillShaper.AutoDj.Audio;
using SkillShaper.AutoDj.Models;
using SkillShaper.AutoDj.Services;

namespace SkillShaper.AutoDj;

public partial class MainPage : ContentPage
{
    private readonly AudioLibraryService _libraryService = new();
    private DeckController[] _decks = [];

    private readonly Dictionary<int, DeckUiRefs> _deckUi = new();
    private readonly Random _rng = new();
    private List<TrackInfo> _library = [];
    private CancellationTokenSource? _autoMixCts;
    private readonly Queue<int> _nextDeckOrder = new();
    private bool _uiBuilt;

    private const double PhraseBars = 8;
    private const double FeatureFadeBars = 4;
    private const double AutoLoopBars = 1;
    private const double BedLoopBars = 0.5;

    private const double LeadGain = 1.0;
    private const double SupportGain = 0.82;
    private const double MidBedGain = 0.62;
    private const double LowBedGain = 0.46;

    public MainPage()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("SkillShaper.AutoDj", $"InitializeComponent failed: {ex}");
            throw;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_uiBuilt)
        {
            return;
        }

        try
        {
            _decks =
            [
                new DeckController(1),
                new DeckController(2),
                new DeckController(3),
                new DeckController(4)
            ];

            BuildDeckUi();
            _uiBuilt = true;
        }
        catch (Exception ex)
        {
            if (StatusLabel is not null)
            {
                StatusLabel.Text = $"UI init error: {ex.GetType().Name} - {ex.Message}";
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _autoMixCts?.Cancel();
        foreach (var deck in _decks)
        {
            deck.Dispose();
        }
    }

    private async void OnScanClicked(object? sender, EventArgs e)
    {
        StatusLabel.Text = "Requesting storage permission...";

        var granted = await _libraryService.EnsureAudioPermissionAsync();
        if (!granted)
        {
            StatusLabel.Text = "Audio storage permission denied.";
            return;
        }

        StatusLabel.Text = "Scanning .mp3 files from phone storage...";
        _library = (await _libraryService.LoadPhoneMp3LibraryAsync()).ToList();
        StatusLabel.Text = _library.Count == 0
            ? "No MP3 files found."
            : $"Loaded {_library.Count} MP3 files.";
    }

    private async void OnStartAutoMixClicked(object? sender, EventArgs e)
    {
        if (_library.Count == 0)
        {
            StatusLabel.Text = "Scan MP3 tracks first.";
            return;
        }

        _autoMixCts?.Cancel();
        _autoMixCts = new CancellationTokenSource();

        try
        {
            await RunAutoMixAsync(_autoMixCts.Token);
        }
        catch (OperationCanceledException)
        {
            StatusLabel.Text = "AutoMix stopped.";
        }
    }

    private void OnStopAutoMixClicked(object? sender, EventArgs e)
    {
        _autoMixCts?.Cancel();
        foreach (var deck in _decks)
        {
            deck.SetMixGain(1);
            deck.Stop();
        }

        StatusLabel.Text = "Stopped all decks.";
    }

    private void BuildDeckUi()
    {
        DeckHost.Children.Clear();

        foreach (var deck in _decks)
        {
            var title = new Label
            {
                Text = $"Deck {deck.DeckId}: (no track)",
                FontAttributes = FontAttributes.Bold,
                FontSize = 16,
                TextColor = Colors.White
            };

            var playButton = new Button { Text = "Play / Pause" };
            playButton.Clicked += (_, _) =>
            {
                if (deck.IsPlaying)
                {
                    deck.Pause();
                }
                else
                {
                    deck.Play();
                }
            };

            var loadRandomButton = new Button { Text = "Load Random" };
            loadRandomButton.Clicked += async (_, _) => await LoadRandomIntoDeckAsync(deck, title, true);

            var reloadButton = new Button { Text = "Reload Random" };
            reloadButton.Clicked += async (_, _) => await LoadRandomIntoDeckAsync(deck, title, false);

            var loopPicker = new Picker { Title = "Loop Bars" };
            loopPicker.ItemsSource = new List<string> { "1/4", "1/2", "1", "2", "4" };
            loopPicker.SelectedIndex = 2;

            var loopSwitch = new Switch();
            loopSwitch.Toggled += (_, args) =>
            {
                var bars = loopPicker.SelectedIndex switch
                {
                    0 => 0.25,
                    1 => 0.5,
                    2 => 1,
                    3 => 2,
                    _ => 4
                };

                deck.SetLoopEnabled(args.Value, bars, deck.GetEstimatedBpm(), Dispatcher);
            };

            var volumeSlider = new Slider(0, 1, 0.85);
            volumeSlider.ValueChanged += (_, args) => deck.SetUserVolume(args.NewValue);

            var chopSwitch = new Switch();
            var chopRateSlider = new Slider(2, 24, 10);
            chopSwitch.Toggled += (_, args) =>
            {
                deck.SetChopEnabled(args.Value, chopRateSlider.Value, Dispatcher);
            };
            chopRateSlider.ValueChanged += (_, args) =>
            {
                if (chopSwitch.IsToggled)
                {
                    deck.SetChopEnabled(true, args.NewValue, Dispatcher);
                }
            };

            var transitionButton = new Button { Text = "Transition To Next Deck" };
            transitionButton.Clicked += async (_, _) =>
            {
                var next = _decks[(deck.DeckId % _decks.Length)];
                await EnsureDeckLoadedAsync(next, _deckUi[next.DeckId].TitleLabel);
                var masterBpm = GetMasterBpm();
                ApplyAutoLoopForTransition(deck, next, masterBpm);
                await SmoothRoleTransitionAsync(deck, next, masterBpm, CancellationToken.None);
                deck.DisableLoop();
                next.DisableLoop();
            };

            var transportGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new(GridLength.Star), new(GridLength.Star), new(GridLength.Star)
                },
                ColumnSpacing = 8
            };
            transportGrid.Children.Add(loadRandomButton);
            Grid.SetColumn(loadRandomButton, 0);
            transportGrid.Children.Add(playButton);
            Grid.SetColumn(playButton, 1);
            transportGrid.Children.Add(reloadButton);
            Grid.SetColumn(reloadButton, 2);

            var loopGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto)
                },
                ColumnSpacing = 8
            };
            var loopLabel = new Label { Text = "Loop", VerticalTextAlignment = TextAlignment.Center };
            loopGrid.Children.Add(loopPicker);
            Grid.SetColumn(loopPicker, 0);
            loopGrid.Children.Add(loopLabel);
            Grid.SetColumn(loopLabel, 1);
            loopGrid.Children.Add(loopSwitch);
            Grid.SetColumn(loopSwitch, 2);

            var chopGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)
                },
                ColumnSpacing = 8
            };
            var chopLabel = new Label { Text = "Chop" };
            chopGrid.Children.Add(chopLabel);
            Grid.SetColumn(chopLabel, 0);
            chopGrid.Children.Add(chopRateSlider);
            Grid.SetColumn(chopRateSlider, 1);
            chopGrid.Children.Add(chopSwitch);
            Grid.SetColumn(chopSwitch, 2);

            var card = new Frame
            {
                BorderColor = Color.FromArgb("#2D3748"),
                BackgroundColor = Color.FromArgb("#161B24"),
                CornerRadius = 16,
                Padding = new Thickness(12),
                Content = new VerticalStackLayout
                {
                    Spacing = 9,
                    Children =
                    {
                        title,
                        transportGrid,
                        loopGrid,
                        new Label { Text = "Volume" },
                        volumeSlider,
                        chopGrid,
                        transitionButton
                    }
                }
            };

            _deckUi[deck.DeckId] = new DeckUiRefs(title);
            DeckHost.Children.Add(card);
        }
    }

    private async Task RunAutoMixAsync(CancellationToken token)
    {
        StatusLabel.Text = "AutoMix running...";

        _nextDeckOrder.Clear();

        foreach (var deck in _decks)
        {
            await EnsureDeckLoadedAsync(deck, _deckUi[deck.DeckId].TitleLabel, forceMidTrackStart: true);
            if (!deck.IsPlaying)
            {
                deck.Play();
            }
        }

        var currentLead = _decks[0];
        ApplyRoleGains(currentLead);

        while (!token.IsCancellationRequested)
        {
            var masterBpm = GetMasterBpm();
            var cycleDelayMs = BarsToMilliseconds(PhraseBars, masterBpm);
            await Task.Delay(TimeSpan.FromMilliseconds(cycleDelayMs), token);

            await EnsureBackgroundDecksRunningAsync(currentLead);
            await RefreshDecksNearTrackEndAsync(currentLead);
            ApplyAggressiveBedLoops(currentLead, masterBpm);

            var nextDeck = GetNextDeckInRandomRotation(currentLead);
            await EnsureDeckLoadedAsync(nextDeck, _deckUi[nextDeck.DeckId].TitleLabel, forceMidTrackStart: false);

            if (!nextDeck.IsPlaying)
            {
                nextDeck.Play();
            }

            ApplyAutoLoopForTransition(currentLead, nextDeck, masterBpm);

            StatusLabel.Text = $"Phrase mix Deck {currentLead.DeckId} -> Deck {nextDeck.DeckId}";
            await SmoothRoleTransitionAsync(currentLead, nextDeck, masterBpm, token);

            DisableAllLoops();
            currentLead.DisableLoop();
            nextDeck.DisableLoop();

            currentLead = nextDeck;
        }
    }

    private async Task SmoothRoleTransitionAsync(DeckController fromDeck, DeckController toDeck, double masterBpm, CancellationToken token)
    {
        var startGains = BuildRoleGainMap(fromDeck);
        var targetGains = BuildRoleGainMap(toDeck);

        const int steps = 64;
        var totalFadeMs = BarsToMilliseconds(FeatureFadeBars, masterBpm);
        var stepDelayMs = Math.Max(35, totalFadeMs / steps);

        for (var i = 0; i <= steps; i++)
        {
            token.ThrowIfCancellationRequested();
            var p = i / (double)steps;

            foreach (var deck in _decks)
            {
                var startGain = startGains[deck.DeckId];
                var endGain = targetGains[deck.DeckId];
                deck.SetMixGain(startGain + ((endGain - startGain) * p));
            }

            fromDeck.ApplyTransitionEq(1 - p);
            toDeck.ApplyTransitionEq(p);

            await Task.Delay(stepDelayMs, token);
        }

        ApplyRoleGains(toDeck);
    }

    private async Task EnsureDeckLoadedAsync(DeckController deck, Label titleLabel, bool forceMidTrackStart = false)
    {
        if (deck.CurrentTrack is null)
        {
            await LoadRandomIntoDeckAsync(deck, titleLabel, true, forceMidTrackStart);
        }
    }

    private async Task LoadRandomIntoDeckAsync(DeckController deck, Label titleLabel, bool autoPlay, bool forceMidTrackStart = false)
    {
        if (_library.Count == 0)
        {
            StatusLabel.Text = "No tracks loaded. Press Scan MP3 first.";
            return;
        }

        var previousPath = deck.CurrentTrack?.Path;

        var activeDeckPaths = _decks
            .Where(d => d.DeckId != deck.DeckId)
            .Select(d => d.CurrentTrack?.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidatePool = _library
            .Where(t => !string.Equals(t.Path, previousPath, StringComparison.OrdinalIgnoreCase))
            .Where(t => !activeDeckPaths.Contains(t.Path))
            .ToList();

        if (candidatePool.Count == 0)
        {
            candidatePool = _library.Where(t => !string.Equals(t.Path, previousPath, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (candidatePool.Count == 0)
        {
            candidatePool = _library;
        }

        var track = candidatePool[_rng.Next(candidatePool.Count)];
        await deck.LoadTrackAsync(track);
        titleLabel.Text = $"Deck {deck.DeckId}: {track.Title}";

        if (forceMidTrackStart)
        {
            SeekDeckPastIntro(deck);
        }

        if (autoPlay)
        {
            deck.Play();
        }
    }

    private void SeekDeckPastIntro(DeckController deck)
    {
        var durationMs = deck.GetDurationMs();
        if (durationMs < 90_000)
        {
            return;
        }

        var minStart = (int)(durationMs * 0.45);
        var maxStart = (int)(durationMs * 0.90);

        if (maxStart <= minStart)
        {
            return;
        }

        var startMs = _rng.Next(minStart, maxStart);
        deck.SeekToMs(startMs);
    }

    private async Task EnsureBackgroundDecksRunningAsync(DeckController currentDeck)
    {
        foreach (var deck in _decks)
        {
            if (deck.DeckId == currentDeck.DeckId)
            {
                continue;
            }

            if (deck.CurrentTrack is null || !deck.IsPlaying)
            {
                await LoadRandomIntoDeckAsync(deck, _deckUi[deck.DeckId].TitleLabel, true, forceMidTrackStart: true);
            }
        }
    }

    private async Task RefreshDecksNearTrackEndAsync(DeckController leadDeck)
    {
        foreach (var deck in _decks)
        {
            if (deck.DeckId == leadDeck.DeckId)
            {
                continue;
            }

            if (IsNearTrackEnd(deck, thresholdMs: 75_000))
            {
                await LoadRandomIntoDeckAsync(deck, _deckUi[deck.DeckId].TitleLabel, true, forceMidTrackStart: true);
            }
        }
    }

    private static bool IsNearTrackEnd(DeckController deck, int thresholdMs)
    {
        var duration = deck.GetDurationMs();
        if (duration <= 0)
        {
            return false;
        }

        return (duration - deck.GetCurrentPositionMs()) <= thresholdMs;
    }

    private DeckController GetNextDeckInRandomRotation(DeckController currentDeck)
    {
        if (_nextDeckOrder.Count == 0)
        {
            var randomizedIds = _decks
                .Where(d => d.DeckId != currentDeck.DeckId)
                .OrderBy(_ => _rng.Next())
                .Select(d => d.DeckId);

            foreach (var id in randomizedIds)
            {
                _nextDeckOrder.Enqueue(id);
            }
        }

        while (_nextDeckOrder.Count > 0)
        {
            var nextId = _nextDeckOrder.Dequeue();
            if (nextId != currentDeck.DeckId)
            {
                return _decks.First(d => d.DeckId == nextId);
            }
        }

        return _decks.First(d => d.DeckId != currentDeck.DeckId);
    }

    private void ApplyAutoLoopForTransition(DeckController fromDeck, DeckController toDeck, double masterBpm)
    {
        var loopLengthMs = BarsToMilliseconds(AutoLoopBars, masterBpm);

        var fromStart = QuantizeToBeat(fromDeck.GetCurrentPositionMs(), masterBpm);
        var toStart = QuantizeToBeat(toDeck.GetCurrentPositionMs(), masterBpm);

        fromDeck.SetLoopWindow(fromStart, loopLengthMs, Dispatcher);
        toDeck.SetLoopWindow(toStart, loopLengthMs, Dispatcher);
    }

    private void ApplyAggressiveBedLoops(DeckController leadDeck, double masterBpm)
    {
        var leadIndex = Array.FindIndex(_decks, d => d.DeckId == leadDeck.DeckId);
        if (leadIndex < 0)
        {
            leadIndex = 0;
        }

        var supportDeck = _decks[(leadIndex + 1) % _decks.Length];
        var midDeck = _decks[(leadIndex + 2) % _decks.Length];
        var bedLoopMs = BarsToMilliseconds(BedLoopBars, masterBpm);

        var supportStart = QuantizeToBeat(supportDeck.GetCurrentPositionMs(), masterBpm);
        var midStart = QuantizeToBeat(midDeck.GetCurrentPositionMs(), masterBpm);

        supportDeck.SetLoopWindow(supportStart, bedLoopMs, Dispatcher);
        midDeck.SetLoopWindow(midStart, bedLoopMs, Dispatcher);
    }

    private void DisableAllLoops()
    {
        foreach (var deck in _decks)
        {
            deck.DisableLoop();
        }
    }

    private Dictionary<int, double> BuildRoleGainMap(DeckController leadDeck)
    {
        var leadIndex = Array.FindIndex(_decks, d => d.DeckId == leadDeck.DeckId);
        if (leadIndex < 0)
        {
            leadIndex = 0;
        }

        var gains = new Dictionary<int, double>(_decks.Length);
        gains[_decks[leadIndex].DeckId] = LeadGain;
        gains[_decks[(leadIndex + 1) % _decks.Length].DeckId] = SupportGain;
        gains[_decks[(leadIndex + 2) % _decks.Length].DeckId] = MidBedGain;
        gains[_decks[(leadIndex + 3) % _decks.Length].DeckId] = LowBedGain;

        return gains;
    }

    private void ApplyRoleGains(DeckController leadDeck)
    {
        var roleGains = BuildRoleGainMap(leadDeck);
        foreach (var deck in _decks)
        {
            if (roleGains.TryGetValue(deck.DeckId, out var gain))
            {
                deck.SetMixGain(gain);
            }
        }
    }

    private double GetMasterBpm()
    {
        var bpms = _decks
            .Select(d => d.CurrentTrack?.EstimatedBpm ?? 120)
            .Where(bpm => bpm is >= 60 and <= 190)
            .DefaultIfEmpty(120)
            .ToList();

        return Math.Clamp(bpms.Average(), 60, 190);
    }

    private static int BarsToMilliseconds(double bars, double bpm)
    {
        var safeBars = Math.Max(0.25, bars);
        var safeBpm = Math.Clamp(bpm, 60, 190);
        return (int)(safeBars * 4 * 60000 / safeBpm);
    }

    private static int QuantizeToBeat(int positionMs, double bpm)
    {
        var beatMs = Math.Max(120, (int)(60000 / Math.Clamp(bpm, 60, 190)));
        var safePos = Math.Max(0, positionMs);
        return (safePos / beatMs) * beatMs;
    }

    private sealed record DeckUiRefs(Label TitleLabel);
}
