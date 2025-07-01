# 🌙 Somno
Somno is a demonstration of the capability to evade user-mode anti-cheats via a kernel-mode helper driver, and a C# game modification base. The helper driver is used to transparently read/write to/from game memory, without opening any handles which may be detected by anti-cheats.

> [!NOTE]
> Somno is helpless when it comes to kernel-mode anti-cheats. This POC relies on the kernel-mode helper driver being "out-of-range" from the adversary.

The individual components of Somno are as follows:
- `Somno.ILTransformer`: simple, naive IL bytecode transformations to obfuscate the trainer binary.
- `Somno.Packager`: creates encrypted data blobs.
- `Somno.Portal`: a Windows driver that hooks into a known system call, and transparently reads or writes memory without opening any memory handles.
- `Somno.WindowHost`: handles window rendering. Doesn't contain any game modifications by itself, and only renders overlays.
- `Somno`: the actual game modification itself.

Most of the heavy lifting is done with C# - C++ is used for the kernel-mode driver, which only is used to read/write process memory at the request of the main C# process.

> [!IMPORTANT]
> Somno uses a version of kdmapper that relies on a vulnerable Intel driver to inject unsigned kernel-mode code. This **only** works on Windows 10. Newer versions and other drivers may work on Windows 11, but this is not implemented.

# 🔨 Building
You'll need these pre-requisites before you'll be able to build Somno:
- Windows Driver Kit (WDK)
- .NET 7.0
- An MSBuild installation with MSVC

First, run `dev_setup.bat`. Once you're done with that, you should be able to run `build.bat` in the `src` directory. For a debug build:

```
cd src
build debug
```

For a release build:

```
cd src
build release
```

When running Somno, you'll need a file in the path `C:\Program Files\Somno\auth.lf`. Its SHA-1 hash should be `ffcaa47e66e599ad25b708df348a894b92b9b6be` - the string `SomnoLicenseFile` meet the criteria.

# 🤨 Cheating?
Fortunately, Somno can't quite be used as a CS:GO cheat - at least in its current state. Mainly because it was written for an old version of Counter-Strike Global Offensive - not Counter-Strike 2 - but also because it hasn't really been tested with any kind of anti-cheat. While it _was_ written with VAC evasion in mind, it wasn't tested outside of a few Casual games.

It also kinda sucks as a game cheat. It only offers ESP, (scuffed) grenade trajectories, and waypoints. Boring!
