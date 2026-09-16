# Screen Color Picker Helper

Small reusable open-source Windows helper that samples a pixel anywhere on
screen. It is designed for desktop applications whose embedded browser cannot
access pixels outside its own window. Any project may use it under the MIT
license; Iridea is one consumer, not a required dependency.

The helper contains no licensing, analytics, networking, update mechanism or
Iridea product-protection code. It uses Windows `user32.dll` and `gdi32.dll`
only for cursor tracking, pixel sampling, a temporary mouse hook and an optional
global hotkey.

## Build

Requirements: Windows and .NET Framework 4.x.

```powershell
.\build.ps1
```

Output: `dist/screen-color-picker-helper.exe`.

## Interface

- No arguments: started while the left mouse button is held. Drag and release
  to print `#RRGGBB`. A quick click switches to next-click capture mode.
- `--picker-hook-check`: checks whether screen capture and the mouse hook can be
  initialized. Exit code `0` means ready.
- `--hotkey-server <modifiers> <virtual-key>`: registers a Windows hotkey and
  emits line-based events on stdout. Closing stdin stops the server.
- Escape or right click cancels capture.

## Privacy

This program does not transfer information to any networked system. It samples
only the pixel currently under the pointer and prints the selected hexadecimal
color to its parent process. It does not save screenshots or sampled colors.

## Code signing policy

Free code signing provided by [SignPath.io](https://signpath.io/), certificate
by [SignPath Foundation](https://signpath.org/).

- Committer and reviewer: [Vladimir Tsiko](https://github.com/TsikoVladimir)
- Approver: [Vladimir Tsiko](https://github.com/TsikoVladimir)

Release binaries must be built from this public repository by GitHub-hosted
runners. Every release signing request requires manual approval by Vladimir
Tsiko.

This program will not transfer any information to other networked systems unless
specifically requested by the user or the person installing or operating it.

## License

MIT. See [LICENSE](LICENSE).
