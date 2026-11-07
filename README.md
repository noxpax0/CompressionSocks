# Mercury

Mercury is an offline-first Windows desktop application for recording, reviewing, printing, and securely retaining compression-garment orders.

## Install and support

Download the signed `Mercury.exe` from the latest GitHub release. It is a self-contained Windows x64 executable; the Microsoft Edge WebView2 Runtime must be installed. Windows 11 is recommended. Windows 10 is supported only where the operating-system edition still receives Microsoft security updates (for example ESU or applicable LTSC editions).

Normal order entry, history, printing, theme selection, and WhatsApp Desktop hand-off work without internet. Internet is used only to check GitHub for application updates and to hand a supplier summary to WhatsApp.

## Privacy and storage

- Drafts and completed orders are encrypted for the signed-in Windows user with DPAPI.
- Database writes are transactional and retain one recovery backup.
- “Save Draft” replaces the current draft. It does not create a completed history entry.
- “Complete Order” is available after full validation and creates an immutable ID in Order History.
- WhatsApp receives a limited supplier summary; it excludes patient name, contact number, notes, and measurements.
- Diagnostic logs contain technical error types only and must not contain order contents.

See [PRIVACY.md](PRIVACY.md) and [SECURITY.md](SECURITY.md).

## Build and test

Requirements: .NET 10 SDK on Windows and the free WebView2 SDK package restored by NuGet.

```powershell
dotnet restore .\CompressionGarmentOrder.csproj -r win-x64
dotnet build .\CompressionGarmentOrder.csproj -c Release --no-restore
dotnet run --project .\Mercury.Tests\Mercury.Tests.csproj -c Release
dotnet publish .\CompressionGarmentOrder.csproj -c Release -r win-x64 --self-contained true -o .\dist
```

The published application remains one executable. WebView2 uses multiple sandboxed processes at runtime, so an under-50 MB memory target is not realistic; the engineering budget is approximately 300–350 MB private memory with near-zero idle CPU.

## Releases and automatic updates

The release workflow accepts semantic tags such as `v2.0.0`, runs the quality gate, publishes one executable, Authenticode-signs and timestamps it, creates a SHA-256 checksum, and generates a GitHub provenance attestation. It requires these repository secrets:

- `WINDOWS_SIGNING_CERTIFICATE_BASE64` — a trusted code-signing PFX encoded as Base64.
- `WINDOWS_SIGNING_CERTIFICATE_PASSWORD` — the PFX password.

Mercury installs an update only when the GitHub asset URL, SHA-256 checksum, and Windows publisher trust all pass verification. It asks before closing, restarts automatically on acceptance, and restores the previous executable if the updated process fails during startup.

See [CONTRIBUTING.md](CONTRIBUTING.md) for the release checklist.
