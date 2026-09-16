# Architecture and security boundaries

Screen Color Picker Helper is a small developer component launched by a parent
desktop application. The complete code of the executable is in this repository.

## Runtime behavior

1. The parent process starts the helper only after a user requests color picking.
2. The helper reads the single desktop pixel under the pointer with Windows GDI.
3. During an active pick, a temporary low-level mouse hook captures the confirming
   click so it is not also delivered to the application underneath the pointer.
4. The selected color is written to standard output as `#RRGGBB`.
5. The hook, desktop device context, cursor override and preview window are removed
   before the process exits.

The optional hotkey-server mode registers one Windows hotkey while its parent keeps
standard input open. It creates a picker session only after that hotkey is pressed
and exits when the parent closes standard input.

## Explicit non-goals

The helper does not capture screenshots, record pointer activity, inspect keyboard
input, inject code into other processes, elevate privileges, install services,
start with Windows, write files or registry values, update itself, or access the
network. It contains no licensing or product-protection code.

## Build and release boundary

Release binaries are compiled from this repository on GitHub-hosted Windows
runners. Starting with v1.0.1, the tag workflow verifies the version, runs tests,
publishes the exact runner-built executable and publishes its SHA-256 checksum.
After SignPath onboarding, that same workflow artifact will be submitted for
signing; locally built files will not be submitted.
