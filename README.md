# Screen Color Picker Helper

Small reusable open-source Windows helper that samples a pixel anywhere on
screen. It is designed for desktop applications whose embedded browser cannot
access pixels outside its own window. Any project may use it under the MIT
license; Iridea is one consumer, not a required dependency.

The helper contains no licensing, analytics, networking, update mechanism or
Iridea product-protection code. It uses Windows `user32.dll` and `gdi32.dll`
only for cursor tracking, pixel sampling, a temporary mouse hook and an optional
global hotkey.

The helper is a developer component: a parent desktop application starts it in
response to a user action and reads the selected `#RRGGBB` value from standard
output. See [ARCHITECTURE.md](ARCHITECTURE.md) for its runtime and security
boundaries.

## Build

Requirements: Windows and .NET Framework 4.x.

```powershell
.\build.ps1
```

Output: `dist/screen-color-picker-helper.exe`.

Run metadata, command-interface and interactive hook checks:

```powershell
.\test.ps1
```

## Interface

- No arguments: started while the left mouse button is held. Drag and release
  to print `#RRGGBB`. A quick click switches to next-click capture mode.
- `--picker-hook-check`: checks whether screen capture and the mouse hook can be
  initialized. Exit code `0` means ready.
- `--hotkey-server <modifiers> <virtual-key>`: registers a Windows hotkey and
  emits line-based events on stdout. Closing stdin stops the server.
- Escape or right click cancels capture.

## Installation and removal

The helper has no installer. A parent application may place the executable next
to its own files and launch it when needed. Removing the executable completely
removes the helper; it creates no services, startup entries, scheduled tasks,
files, registry values or other persistent system changes.

## Privacy

This program does not transfer information to any networked system. It samples
only the pixel currently under the pointer and prints the selected hexadecimal
color to its parent process. It does not save screenshots or sampled colors.

The temporary mouse hook exists only during an active picker session. It observes
mouse-button events solely to confirm or cancel the pick and suppresses the
confirming click so the application underneath is not activated accidentally.
It never records or transmits pointer activity. The optional global hotkey exists
only while the parent process keeps the helper running.

## Code signing policy

Free code signing provided by [SignPath.io](https://signpath.io/), certificate
by [SignPath Foundation](https://signpath.org/).

- Author: [Vladimir Tsiko](https://github.com/TsikoVladimir)
- Reviewer: [Vladimir Tsiko](https://github.com/TsikoVladimir)
- Approver: [Vladimir Tsiko](https://github.com/TsikoVladimir)

Release binaries must be built from this public repository by GitHub-hosted
runners. Every release signing request requires manual approval by Vladimir
Tsiko. Starting with v1.0.1, release assets are built and uploaded by the tagged
GitHub Actions workflow, with a SHA-256 checksum. The v1.0.0 binary was an
initial unsigned bootstrap release and will not be submitted for signing.

Only `screen-color-picker-helper.exe`, built entirely from this repository, may
be submitted under this policy. Iridea and any other consuming application are
outside the signing project and are never included in a signing request.

This program will not transfer any information to other networked systems unless
specifically requested by the user or the person installing or operating it.

## Security

See [SECURITY.md](SECURITY.md) for vulnerability reporting and
[CONTRIBUTING.md](CONTRIBUTING.md) for review rules. GitHub Actions performs the
build checks on every change, and CodeQL analyzes the C# source.

## License

MIT. See [LICENSE](LICENSE).
