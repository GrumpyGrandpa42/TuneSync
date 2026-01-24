using System;
using TuneSync.App.Utils;

namespace TuneSync.App.Models;

public sealed class LyricLine : ObservableObject
{
    private TimeSpan? _start;
    private TimeSpan? _end;

    public LyricLine(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public TimeSpan? Start
    {
        get => _start;
        set
        {
            if (SetProperty(ref _start, value))
            {
                RaisePropertyChanged(nameof(StartDisplay));
                RaisePropertyChanged(nameof(TimingDisplay));
            }
        }
    }

    public TimeSpan? End
    {
        get => _end;
        set
        {
            if (SetProperty(ref _end, value))
            {
                RaisePropertyChanged(nameof(EndDisplay));
                RaisePropertyChanged(nameof(TimingDisplay));
            }
        }
    }

    public string StartDisplay => FormatTime(Start);

    public string EndDisplay => FormatTime(End);

    public string TimingDisplay => $"{StartDisplay} → {EndDisplay}";

    public bool HasTiming => Start.HasValue && End.HasValue && End >= Start;

    public void ClearTiming()
    {
        Start = null;
        End = null;
    }

    private static string FormatTime(TimeSpan? value)
    {
        if (value is null)
        {
            return "--:--:--.---";
        }

        var time = value.Value;
        return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds:000}";
    }
}
