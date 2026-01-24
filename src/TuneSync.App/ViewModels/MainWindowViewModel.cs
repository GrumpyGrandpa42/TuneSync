using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using NAudio.Wave;
using TuneSync.App.Models;
using TuneSync.App.Utils;

namespace TuneSync.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly DispatcherTimer _playbackTimer;
    private WaveOutEvent? _outputDevice;
    private AudioFileReader? _audioFile;
    private string _audioFileName = "No audio loaded";
    private string _lyricsFileName = "No lyrics file loaded";
    private string _lyricsText = string.Empty;
    private string _playbackStatus = "Stopped";
    private string _exportStatus = string.Empty;
    private LyricLine? _selectedLine;
    private bool _isSpaceDown;
    private bool _hasStartedTiming;

    public MainWindowViewModel()
    {
        LyricsLines = new ObservableCollection<LyricLine>();
        LoadAudioCommand = new RelayCommand(async () => await LoadAudioAsync());
        LoadLyricsFileCommand = new RelayCommand(async () => await LoadLyricsFileAsync());
        ParseLyricsCommand = new RelayCommand(ParseLyrics);
        PlayPauseCommand = new RelayCommand(TogglePlayPause);
        StopCommand = new RelayCommand(StopPlayback);
        MarkStartCommand = new RelayCommand(MarkStart);
        MarkEndCommand = new RelayCommand(MarkEnd);
        PreviousLineCommand = new RelayCommand(SelectPreviousLine);
        NextLineCommand = new RelayCommand(SelectNextLine);
        ExportSrtCommand = new RelayCommand(async () => await ExportSrtAsync());

        _playbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        _playbackTimer.Tick += (_, _) => UpdatePlaybackStatus();
    }

    public ObservableCollection<LyricLine> LyricsLines { get; }

    public string AudioFileName
    {
        get => _audioFileName;
        private set => SetProperty(ref _audioFileName, value);
    }

    public string LyricsFileName
    {
        get => _lyricsFileName;
        private set => SetProperty(ref _lyricsFileName, value);
    }

    public string LyricsText
    {
        get => _lyricsText;
        set => SetProperty(ref _lyricsText, value);
    }

    public string PlaybackStatus
    {
        get => _playbackStatus;
        private set => SetProperty(ref _playbackStatus, value);
    }

    public string ExportStatus
    {
        get => _exportStatus;
        private set => SetProperty(ref _exportStatus, value);
    }

    public LyricLine? SelectedLine
    {
        get => _selectedLine;
        set => SetProperty(ref _selectedLine, value);
    }

    public RelayCommand LoadAudioCommand { get; }
    public RelayCommand LoadLyricsFileCommand { get; }
    public RelayCommand ParseLyricsCommand { get; }
    public RelayCommand PlayPauseCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand MarkStartCommand { get; }
    public RelayCommand MarkEndCommand { get; }
    public RelayCommand PreviousLineCommand { get; }
    public RelayCommand NextLineCommand { get; }
    public RelayCommand ExportSrtCommand { get; }

    public void HandleSpacePressed()
    {
        if (_isSpaceDown)
        {
            return;
        }

        _isSpaceDown = true;

        if (!TryGetCurrentTime(out var position))
        {
            return;
        }

        EnsurePlaying();

        if (LyricsLines.Count == 0)
        {
            ExportStatus = "Load and parse lyrics first.";
            return;
        }

        if (!_hasStartedTiming)
        {
            SelectedLine ??= LyricsLines.FirstOrDefault();
            _hasStartedTiming = true;
        }
        else
        {
            SelectedLine = GetNextLine();
        }

        if (SelectedLine is null)
        {
            ExportStatus = "No more lyric lines to mark.";
            return;
        }

        SelectedLine.Start = position;
        ExportStatus = $"Start set to {SelectedLine.StartDisplay}.";
    }

    public void HandleSpaceReleased()
    {
        if (!_isSpaceDown)
        {
            return;
        }

        _isSpaceDown = false;

        if (!TryGetCurrentTime(out var position))
        {
            return;
        }

        if (SelectedLine is null)
        {
            ExportStatus = "Select a lyric line to mark.";
            return;
        }

        SelectedLine.End = position;
        ExportStatus = $"End set to {SelectedLine.EndDisplay}.";
    }

    private async Task LoadAudioAsync()
    {
        var path = await PickFileAsync("Select WAV file", new[] { "wav" });
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        ResetAudio();
        try
        {
            _audioFile = new AudioFileReader(path);
            _outputDevice = new WaveOutEvent();
            _outputDevice.Init(_audioFile);
            _outputDevice.PlaybackStopped += (_, _) => PlaybackStatus = "Stopped";
            AudioFileName = Path.GetFileName(path);
            PlaybackStatus = "Loaded";
            _playbackTimer.Start();
        }
        catch (Exception ex)
        {
            PlaybackStatus = $"Error loading audio: {ex.Message}";
            ResetAudio();
        }
    }

    private async Task LoadLyricsFileAsync()
    {
        var path = await PickFileAsync("Select lyrics file", new[] { "txt", "lrc" });
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        LyricsText = await File.ReadAllTextAsync(path);
        LyricsFileName = Path.GetFileName(path);
        ExportStatus = "Lyrics file loaded. Use Parse Lyrics to create lines.";
    }

    private void ParseLyrics()
    {
        LyricsLines.Clear();
        var lines = LyricsText
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line));

        foreach (var line in lines)
        {
            LyricsLines.Add(new LyricLine(line));
        }

        SelectedLine = LyricsLines.FirstOrDefault();
        _hasStartedTiming = false;
        _isSpaceDown = false;
        ExportStatus = LyricsLines.Count > 0
            ? $"Parsed {LyricsLines.Count} lyric lines."
            : "No lyric lines found.";
    }

    private void TogglePlayPause()
    {
        if (_outputDevice is null || _audioFile is null)
        {
            PlaybackStatus = "Load an audio file first.";
            return;
        }

        if (_outputDevice.PlaybackState == PlaybackState.Playing)
        {
            _outputDevice.Pause();
            PlaybackStatus = "Paused";
        }
        else
        {
            _outputDevice.Play();
            PlaybackStatus = "Playing";
        }
    }

    private void EnsurePlaying()
    {
        if (_outputDevice is null || _audioFile is null)
        {
            PlaybackStatus = "Load an audio file first.";
            return;
        }

        if (_outputDevice.PlaybackState != PlaybackState.Playing)
        {
            _outputDevice.Play();
            PlaybackStatus = "Playing";
        }
    }

    private void StopPlayback()
    {
        if (_outputDevice is null || _audioFile is null)
        {
            PlaybackStatus = "Stopped";
            return;
        }

        _outputDevice.Stop();
        _audioFile.Position = 0;
        PlaybackStatus = "Stopped";
        _hasStartedTiming = false;
        _isSpaceDown = false;
    }

    private void MarkStart()
    {
        if (!TryGetCurrentTime(out var position))
        {
            return;
        }

        if (SelectedLine is null)
        {
            ExportStatus = "Select a lyric line to mark.";
            return;
        }

        SelectedLine.Start = position;
        ExportStatus = $"Start set to {SelectedLine.StartDisplay}.";
    }

    private void MarkEnd()
    {
        if (!TryGetCurrentTime(out var position))
        {
            return;
        }

        if (SelectedLine is null)
        {
            ExportStatus = "Select a lyric line to mark.";
            return;
        }

        SelectedLine.End = position;
        ExportStatus = $"End set to {SelectedLine.EndDisplay}.";
    }

    private void SelectPreviousLine()
    {
        if (LyricsLines.Count == 0 || SelectedLine is null)
        {
            return;
        }

        var index = LyricsLines.IndexOf(SelectedLine);
        if (index > 0)
        {
            SelectedLine = LyricsLines[index - 1];
        }
    }

    private void SelectNextLine()
    {
        if (LyricsLines.Count == 0 || SelectedLine is null)
        {
            return;
        }

        var index = LyricsLines.IndexOf(SelectedLine);
        if (index < LyricsLines.Count - 1)
        {
            SelectedLine = LyricsLines[index + 1];
        }
    }

    private LyricLine? GetNextLine()
    {
        if (SelectedLine is null)
        {
            return LyricsLines.FirstOrDefault();
        }

        var index = LyricsLines.IndexOf(SelectedLine);
        if (index < 0)
        {
            return LyricsLines.FirstOrDefault();
        }

        if (index < LyricsLines.Count - 1)
        {
            return LyricsLines[index + 1];
        }

        return null;
    }

    private async Task ExportSrtAsync()
    {
        if (LyricsLines.Count == 0)
        {
            ExportStatus = "No lyrics to export.";
            return;
        }

        var path = await PickSaveFileAsync("Export SRT", "srt");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var builder = new StringBuilder();
        var index = 1;

        foreach (var line in LyricsLines)
        {
            if (!line.HasTiming)
            {
                continue;
            }

            builder.AppendLine(index.ToString());
            builder.AppendLine($"{FormatSrtTime(line.Start!.Value)} --> {FormatSrtTime(line.End!.Value)}");
            builder.AppendLine(line.Text);
            builder.AppendLine();
            index++;
        }

        await File.WriteAllTextAsync(path, builder.ToString().TrimEnd());
        ExportStatus = $"Exported {index - 1} lines to {Path.GetFileName(path)}.";
    }

    private void UpdatePlaybackStatus()
    {
        if (_audioFile is null)
        {
            return;
        }

        var position = _audioFile.CurrentTime;
        var total = _audioFile.TotalTime;
        PlaybackStatus = $"{FormatTime(position)} / {FormatTime(total)}";
    }

    private bool TryGetCurrentTime(out TimeSpan position)
    {
        if (_audioFile is null)
        {
            ExportStatus = "Load an audio file first.";
            position = default;
            return false;
        }

        position = _audioFile.CurrentTime;
        return true;
    }

    private async Task<string?> PickFileAsync(string title, string[] extensions)
    {
        var window = GetMainWindow();
        if (window?.StorageProvider is null)
        {
            ExportStatus = "Unable to open file picker.";
            return null;
        }

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType(title)
                {
                    Patterns = extensions.Select(ext => $"*.{ext}").ToArray()
                }
            }
        });

        return files.FirstOrDefault()?.Path.LocalPath;
    }

    private async Task<string?> PickSaveFileAsync(string title, string extension)
    {
        var window = GetMainWindow();
        if (window?.StorageProvider is null)
        {
            ExportStatus = "Unable to open save dialog.";
            return null;
        }

        var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            DefaultExtension = extension,
            FileTypeChoices = new[]
            {
                new FilePickerFileType(title)
                {
                    Patterns = new[] { $"*.{extension}" }
                }
            }
        });

        return file?.Path.LocalPath;
    }

    private static string FormatTime(TimeSpan time)
    {
        return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds:000}";
    }

    private static string FormatSrtTime(TimeSpan time)
    {
        return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00},{time.Milliseconds:000}";
    }

    private static Window? GetMainWindow()
    {
        return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
    }

    private void ResetAudio()
    {
        _outputDevice?.Dispose();
        _audioFile?.Dispose();
        _outputDevice = null;
        _audioFile = null;
    }
}
