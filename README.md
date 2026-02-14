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

## Using the app
1. Click **Load WAV** and choose a `.wav` file to sync.
2. Load lyrics:
   - Click **Load Lyrics File** to open a `.txt` or `.lrc`, or
   - Paste lyrics directly into the **Lyrics Source** box.
3. Click **Parse Lyrics** to split the text into lines.
4. Start playback with **Play / Pause**.
5. Mark timings for the active line:
   - Click **Mark Start** and **Mark End**, or
   - Press and hold the **Spacebar**: press sets the start time, release sets the end time, and advances to the next line.
6. Use **Previous Line** / **Next Line** to adjust selection as needed.
7. Click **Export SRT** to save the timed lines as an `.srt` file (only lines with both start and end times are exported).

## Next steps
- Add waveform visualization for easier line timing.
- Support MP3 and other formats.
- Add auto-scroll and hotkeys for marking lines.
