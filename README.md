# AudioMirror

AudioMirror is a small Windows 11 WASAPI utility for playing one audio stream
through two or three playback devices at the same time.

It was built for a practical setup with two monitors that both expose HDMI/DP
audio devices, but it can also be useful for USB speakers and other Windows
playback endpoints.

AudioMirror does not install a driver, does not create a virtual audio device,
and does not try to be a full audio mixer. It captures loopback audio from one
selected playback device and renders it to two selected playback devices, with
an optional third target for a USB soundbar or another output. Each target has
its own delay and gain controls.

## Why

Windows normally plays system audio through one default output device. Sending
the same audio to two independent devices is possible in user mode, but the
devices often have different buffering latency. AudioMirror exposes delay
controls so the outputs can be aligned by ear.

## Download / Run

Download the latest Windows build from the repository Releases page, unzip it,
and run `AudioMirrorApp.exe`.

The running app shows its version in the main window title and in Help -> About.

There are two release packages:

- `AudioMirrorSetup-win-x64.zip`: user-level installer. It installs AudioMirror
  into `%LocalAppData%\Programs\AudioMirror`, creates Start Menu shortcuts, and
  registers uninstall in Windows Apps & Features. During upgrades, it closes a
  running AudioMirror instance before replacing files.
- `AudioMirrorApp-win-x64.zip`: portable build. Unzip and run
  `AudioMirrorApp.exe` directly.

Requirements:

- Windows 11
- .NET Desktop Runtime 10, or .NET SDK 10

## Recommended Setup

Use an unused playback endpoint as the Windows default output, then let
AudioMirror send that sound to both real outputs.

Example device list:

```text
0: PL2492H (NVIDIA High Definition Audio)
1: Realtek Digital Output (Realtek USB Audio)
2: PL2492H Left (NVIDIA High Definition Audio)
```

In Windows:

1. Set `Realtek Digital Output` as the default playback device.
2. Restart the browser/player if it keeps using the old output.
3. Open `AudioMirrorApp.exe`.
4. Choose:

```text
Source:   1 Realtek Digital Output
Target 1: 0 PL2492H
Target 2: 2 PL2492H Left
```

Then press `Start`.

## Controls

- `Language`: switches the app UI between English, Russian, German, French,
  Spanish, Italian, Portuguese, Polish, Dutch, Chinese, and Japanese.
- `Target 3`: optional third output for a soundbar or another playback device.
- `Gain`: volume multiplier for each target.
  - `1.0` means unchanged.
  - `2.0` is louder.
  - Values that are too high can distort.
- `Delay ms`: delay added to each target.
  - Try values like `0`, `60`, `75`, `90`, `105`, `120`.
  - If echo gets worse, put the delay on the other target.
- `Split L/R`: sends the source left channel to `Target 1` as dual-mono and
  the source right channel to `Target 2` as dual-mono. Turn it off for normal
  stereo mirroring.
- Default gain is `1.0` and default delay is `0 ms` for all targets.
- `Save`: writes `settings.json` next to the app.
- `Autostart`: registers the app in the current user's Windows startup and
  starts mirroring automatically using saved settings.
- `Test`: opens a built-in speaker test with `Left`, `Right`, optional `Third`,
  `Both`, and `Loop`. It plays directly to the selected targets and animates
  the active speaker.
- `Auto restart`: watches the audio stream and recreates WASAPI streams if a
  display sleep, monitor power-off, or endpoint reset stalls audio. After
  sleep/resume it keeps refreshing devices briefly and restarts when the saved
  source/targets are active again.
- `Help`: opens the built-in user guide.
- `About`: shows the app name, repository, copyright, and MIT license summary.

The main window also has a standard menu bar:

- `File`: Start, Stop, Save settings, Autostart, Exit.
- `Actions`: Refresh devices, Sound settings, Sync, Test speakers, Split L/R.
- `Help`: User help and About.

Minimize or close the window to keep AudioMirror running in the system tray.
The tray menu can open the window, start/stop mirroring, open the speaker test,
open Windows sound settings, show help, or exit the app.

The status area shows captured and written frames. If captured frames stay at
zero, the selected source device is not receiving the app/player audio.

AudioMirror also reads the Windows mix format for the selected source and
targets. If the two target formats differ, the app shows a warning. For cleaner
sound, set both physical target devices to the same Windows format, for example
`48000 Hz, 16 bit` or `48000 Hz, 24 bit`.

The small meters next to each device show live signal level while mirroring is
running. They use a logarithmic dB-style scale so normal listening levels are
visible, and their border color also acts as the format status: green means
aligned, amber means possible resampling or target mismatch. Click a meter to
open Windows sound settings. `Sync` applies the app-side safe defaults without
changing Windows driver settings.

## PowerShell CLI

The original script is still available for quick testing:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\AudioMirror.ps1 -List
```

Mirror one source to two targets:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\AudioMirror.ps1 -SourceIndex 1 -TargetIndex 0 -SecondTargetIndex 2 -Gain 1 -SecondGain 1 -DelayMs 0 -SecondDelayMs 0 -DebugAudio
```

Stop with `Ctrl+C`.

## Build

```powershell
dotnet publish .\AudioMirrorApp\AudioMirrorApp.csproj -c Release -r win-x64 --self-contained false -o .\dist\AudioMirrorApp
```

The app has no NuGet package dependencies.

Build the installer package:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Build-Installer.ps1
```

## Limitations

- AudioMirror cannot make audio play before it is captured, so there is no
  negative delay. Use a silent/unused source endpoint and render to both real
  outputs when you need alignment in both directions.
- Independent audio devices can drift over long periods because they do not
  necessarily share the same hardware clock.
- Some HDMI/DP monitor endpoints show activity in Windows but do not produce
  sound until the monitor's own volume/input/audio-source settings are correct.
