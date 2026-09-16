# Security

## Supported versions

Security fixes are provided for the latest release.

## Reporting a vulnerability

Use GitHub's private vulnerability reporting for this repository. If that option
is unavailable, open a public issue requesting private contact without including
exploit details, secrets, license codes or personal data. Please allow reasonable
time for investigation before public disclosure.

Include the affected version, Windows version, reproduction steps and expected
impact. Reports involving unexpected network, persistence, privilege, input
capture or process-injection behavior receive priority.

## Security boundary

The helper intentionally has no network access, persistence, auto-update,
registry writes, filesystem writes or elevated privileges.

It reads one screen pixel at a time and uses a temporary low-level mouse hook
only while a picker session is active. See [ARCHITECTURE.md](ARCHITECTURE.md) for
the full lifecycle and explicit non-goals.
