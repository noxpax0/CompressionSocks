# Mercury

Mercury is a lightweight, offline-first Windows application for compression garment orders.

## Install

Download Mercury.exe from the latest GitHub release. It is a self-contained Windows x64 executable. The Microsoft Edge WebView2 Runtime must be installed on the computer.

## Automatic updates

Mercury checks the public GitHub repository once after startup. When a newer stable release exists, it downloads Mercury.exe, verifies the published SHA-256 checksum, closes the current version, installs the update in place, and restarts automatically.

Order entry, saved orders, printing, and all other normal functions remain available without internet. No update is installed if the connection is unavailable or verification fails.

## Build locally

Run: dotnet publish .\CompressionGarmentOrder.csproj -c Release -r win-x64 --self-contained true

## Release a version

1. Update Version, AssemblyVersion, and FileVersion in CompressionGarmentOrder.csproj.
2. Commit the change.
3. Create and push a matching semantic version tag such as v1.0.1.
4. GitHub Actions publishes Mercury.exe and Mercury.exe.sha256 to a new GitHub release.

The release workflow has only contents: write permission.