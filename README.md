# AudioMirror

AudioMirror is a small Windows 11 WASAPI utility for playing one audio stream
through two playback devices at the same time.

It was built for a practical setup with two monitors that both expose HDMI/DP
audio devices, but it can also be useful for USB speakers and other Windows
playback endpoints.

AudioMirror does not install a driver, does not create a virtual audio device,
and does not try to be a full audio mixer. It captures loopback audio from one
selected playback device and renders it to two selected playback devices with
per-output delay and gain controls.

## Why

Windows normally plays system audio through one default output device. Sending
the same audio to two independent devices is possible in user mode, but the
devices often have different buffering latency. AudioMirror exposes delay
controls so the outputs can be aligned by ear.

## Download / Run

Download the latest Windows build from the repository Releases page, unzip it,
and run `AudioMirrorApp.exe`.

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

- `Gain`: volume multiplier for each target.
  - `1.0` means unchanged.
  - `2.0` is louder.
  - Values that are too high can distort.
- `Delay ms`: delay added to each target.
  - Try values like `0`, `60`, `75`, `90`, `105`, `120`.
  - If echo gets worse, put the delay on the other target.
- `Save`: writes `settings.json` next to the app.
- `Autostart`: registers the app in the current user's Windows startup and
  starts mirroring automatically using saved settings.

The status area shows captured and written frames. If captured frames stay at
zero, the selected source device is not receiving the app/player audio.

AudioMirror also reads the Windows mix format for the selected source and
targets. If the two target formats differ, the app shows a warning. For cleaner
sound, set both physical target devices to the same Windows format, for example
`48000 Hz, 16 bit` or `48000 Hz, 24 bit`.

## PowerShell CLI

The original script is still available for quick testing:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\AudioMirror.ps1 -List
```

Mirror one source to two targets:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\AudioMirror.ps1 -SourceIndex 1 -TargetIndex 0 -SecondTargetIndex 2 -Gain 3 -SecondGain 3 -DelayMs 0 -SecondDelayMs 90 -DebugAudio
```

Stop with `Ctrl+C`.

## Build

```powershell
dotnet publish .\AudioMirrorApp\AudioMirrorApp.csproj -c Release -r win-x64 --self-contained false -o .\dist\AudioMirrorApp
```

The app has no NuGet package dependencies.

## Limitations

- AudioMirror cannot make audio play before it is captured, so there is no
  negative delay. Use a silent/unused source endpoint and render to both real
  outputs when you need alignment in both directions.
- Independent audio devices can drift over long periods because they do not
  necessarily share the same hardware clock.
- Some HDMI/DP monitor endpoints show activity in Windows but do not produce
  sound until the monitor's own volume/input/audio-source settings are correct.
