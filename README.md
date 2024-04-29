# Arteranos

## Under heavy development
__*So don't expect too much!*__

This is intended to be a VR social app - To meet and greet people in the metaverse, conduct or attend events, create worlds, and much more, built upon a decentralized architecture to host individual users in numerous small(er) servers as an alternative to the singular "mega-servers" which are administered or switched off by a single person's royal whim.

### System requirements

- Windows 10/11, 64-Bit.
  - It's designed to be architecture agnostic, so more recent versions of Windows should likely work.
  - Other architectures (e.g. Linux) are in the planning stage

- OpenXR-supported tethered headset, _if applicable_<sup>*</sup>
  - ~~Oculus~~ Meta Quest 2, using Oculus Link (both Air and cable)
  - Meta Quest 2, Virtual Desktop
  - Meta Quest 2, Steam Link
  
- For building you need:
  - **Unity 2021.3.15f1** with the installed modules for the desired target platform
  - **Visual Studio**, together installed with Unity
  - [git for Windows](https://gitforwindows.org/), _usable in the command line_, installed separately or with Visual Studio
  - [WiX Toolset 3.11.2](https://github.com/wixtoolset/wix3/releases/tag/wix3112rtm)
  - [7zip](https://www.7-zip.org/download.html)

<sup>*</sup>) This application both supports VR and 2D (aka Desktop) mode, and it's intended to smoothly switch the modes back and forth, _anytime_.

### Quick Building (Windows 64)
 1. Download the source using git: `git clone --recurse-submodules https://github.com/willneedit/Arteranos.git`
    - Of course you can use Github Desktop. But using a command line is much safer than a GUI where you'd surely prone to having misclicks and forgetting option checks... like using submodules. Or not.
 2. Open the Arteranos as the Unity project (adding Arteranos's directory in Unity Hub, then open it)
 3. In Unity's menu, there's a custom menu heading named `Arteranos`. From there, select `Build Installation Package`
 4. If all goes well, please find the newly created `build` folder including the installation wizard, `ArteranosSetup.msi` (or `ArteranosSetup.exe` as an alternative), amongst other files.
    - Also, you'll find the `Arteranos.exe` and `Arteranos-Server.exe`, respectively.

### Installing
 - Start the installation wizard, `ArteranosSetup.exe`, and follow its steps.

### Now what _are_ these apps?
`Arteranos.exe` is the full-fledged client. You can use it in the desktop mode or VR, as you like, both for connecting to a server, or to host your own server, with or without your presence in the world you set up.

`Arteranos-Server.exe` is the dedicated server. It won't show anything, and it's intended to persist in the backgound and to consume minimal resouces and to be connected to from other's clients, maybe you.

### And now?

You can jump into the deep end of the pool with starting Arteranos, or just read further a bit :)

For those who look before leaping, you can [browse the documentation](https://arteranos.gitbook.io/arteranos-documentation) (in the works...)
The documentation is meant both to be read end-to-end, and for looking up the needed information.

## Getting in Touch
To report issues and feature requests: [Github issues page](https://github.com/willneedit/Arteranos/issues).

To chat with the team and other users: join the [Arteranos Development Discussion](https://discord.gg/jHYFFd78B9).

---
## License

This product is copyright 2023 by willneedit and licensed under the [Mozilla Public License 2.0](LICENSE.md).

This package contains third-party software components, owned and licensed by [this list](Third%20Party%20Notices.md).
