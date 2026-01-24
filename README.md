# TuneSync

TuneSync is a desktop app concept built with Avalonia UI and .NET that helps you sync song lyrics to a WAV file and export the timings as an SRT subtitle file.

## What it does
- Load a WAV file for playback.
- Load or paste lyrics, then parse them into line items.
- Play audio while marking line start/end times.
- Export synced lines to an SRT file.

## Project layout
```
TuneSync.sln
src/
  TuneSync.App/
    TuneSync.App.csproj
    App.axaml
    App.axaml.cs
    Program.cs
    Models/
    ViewModels/
    Views/
```

## Running (when .NET SDK is installed)
```bash
dotnet restore
dotnet run --project src/TuneSync.App/TuneSync.App.csproj
```

## Next steps
- Add waveform visualization for easier line timing.
- Support MP3 and other formats.
- Add auto-scroll and hotkeys for marking lines.
