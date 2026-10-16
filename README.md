# DeMONOized Banshee

A port of the [Banshee](https://github.com/BansheeMediaPlayer/banshee) media player
from Mono to **Microsoft .NET 10** on Linux.

Banshee's development stopped in 2019, while it was still tied to Mono, Mono-specific
libraries (gtk-sharp, dbus-sharp, Mono.Addins…) and an autotools build. This project
keeps Banshee's code and history, and replaces the Mono-only pieces so it builds with
the `dotnet` SDK and runs on the regular .NET runtime.

> **Status: early work in progress.** The core application builds and starts, but
> many extensions and features are not ported yet. See [Status](#status).

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- GTK+ 3 and SQLite 3 (runtime libraries)
- GStreamer 1.0 with its base and good plugins
- To build the native audio backend: a C compiler, `pkg-config` and the GStreamer
  development headers

On Debian/Ubuntu (the .NET SDK package may require
[Microsoft's package repository](https://learn.microsoft.com/dotnet/core/install/linux)):

```sh
sudo apt install dotnet-sdk-10.0 build-essential pkg-config libgtk-3-dev \
    libgstreamer1.0-dev libgstreamer-plugins-base1.0-dev \
    gstreamer1.0-plugins-base gstreamer1.0-plugins-good
```

## Building

```sh
dotnet build Banshee.DotNet.sln
./build/dotnet/build-libbanshee.sh
```

The first command builds all managed assemblies into `bin/`. The second compiles
`libbanshee.so`, Banshee's native GStreamer backend, into the same directory.

## Running

```sh
cd bin
dotnet Banshee.dll
```

Add `--debug` for verbose logging. Banshee stores its library and settings in
`~/.config/banshee-1`. To try it without touching an existing Banshee profile, point
the XDG directories somewhere else:

```sh
XDG_CONFIG_HOME=/tmp/banshee-test/config XDG_CACHE_HOME=/tmp/banshee-test/cache \
XDG_DATA_HOME=/tmp/banshee-test/data dotnet Banshee.dll --debug
```

## Status

Working:

- Main window (Nereid client) on GTK 3 via [GtkSharp](https://github.com/GtkSharp/GtkSharp)
- Native GStreamer playback engine (`libbanshee`): gapless playback and equalizer
- SQLite library database, including creation and migration
- Single-instance handling and remote services over D-Bus
- Add-in loading through Mono.Addins

Not working yet / to do:

- **Hardware support** (CDs, USB devices, portable players): gudev-sharp has no .NET
  version, so the GIO hardware backend is disabled and an error about it is logged at
  startup.
- **Extensions** (Last.fm, cover art, podcasts, MPRIS, notifications, devices…): none
  are ported yet.
- **Banshee's test suite** has not been run under .NET yet (Hyena's has, see below).
- **Libraries from the Mono version:** collation keys should be rebuilt once when an
  existing `banshee.db` created under Mono is opened (see *Sort keys* below).
- The managed GStreamer# backend, the WebKit-based browser and store integrations,
  and the Windows/macOS backends are not part of the .NET build.
- The old autotools build (`configure.ac`, `Makefile.am`) is no longer maintained and
  does not work with these changes.

## What changed compared to Banshee

- **Build:** SDK-style projects targeting `net10.0`, built through `Banshee.DotNet.sln`.
  The `.csproj` files are generated from each `Makefile.am` by
  `build/dotnet/gen-csproj.py`, so they compile exactly the sources and resources the
  original build did. Change the script rather than editing the generated projects.
- **Dependencies from NuGet:** GtkSharp 3.24, Mono.Posix 7.1, Mono.Addins 1.4 (with
  its Cecil reflector), TagLibSharp and SharpZipLib.
- **Native libraries:** Mono's `<dllmap>` config files are replaced by
  `Hyena.NativeLibraryMap`, which maps the library names used in `[DllImport]` to
  their Linux sonames.
- **API updates:** newer GTK/ATK interface members, `Requisition` →
  `ChildRequisition`/`GetPreferredSize`, `using` aliases where new GTK types clash
  with Banshee's, `Thread.Abort` → cooperative cancellation, and the booter starts
  the client in-process instead of using `AppDomain.ExecuteAssembly`.
- **Language version** is pinned to C# 12, because C# 14 turns identifiers named
  `field` inside properties into a keyword, which silently changes old code.
- **Sort keys:** .NET generates collation keys with ICU, so they differ from Mono's.
  New databases are fine, but a database created by the Mono version of Banshee keeps
  its old keys (Banshee only regenerates them when the locale changes), so sorting may
  be off until they are rebuilt.

## Bundled libraries

`deps/` contains copies of libraries Banshee needs, with the changes required to run
on .NET:

| Library | Origin | Changes |
|---|---|---|
| `deps/hyena` | [BansheeMediaPlayer/hyena](https://github.com/BansheeMediaPlayer/hyena) (GTK 3 branch) | SDK projects, newer GTK/ATK APIs, `NativeLibraryMap`, NUnit 3 tests |
| `deps/dbus-sharp` | [mono/dbus-sharp](https://github.com/mono/dbus-sharp) | SDK project; removed the Remoting-based proxy; `AssemblyBuilder` instead of `AppDomain.DefineDynamicAssembly`; `libc.so.6` |
| `deps/dbus-sharp-glib` | [mono/dbus-sharp-glib](https://github.com/mono/dbus-sharp-glib) | SDK project; GLib library name |

Hyena's tests can be run with:

```sh
cd deps/hyena
LC_ALL=it_IT.UTF-8 TZ=America/Chicago dotnet test Hyena.sln -p:EnableTests=true
```

Some tests assume that locale and time zone, as the original test setup did.

## License

Banshee, Hyena, dbus-sharp and dbus-sharp-glib are released under the MIT license.
See `COPYING` in this directory and in each folder under `deps/` for the copyright
holders. This project is not affiliated with the original Banshee developers.
