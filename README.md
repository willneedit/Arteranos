# Arteranos

## Under heavy development
__*Currently it's in alpha - so don't expect too much!*__

This is intended to be a VR social app - To meet amd greet people in the metaverse, conduct or attend events, create worlds, and much more, built upon a decentralized architecture to host individual users in numerous small(er) servers as an alternative to the singular "mega-servers" which are administered or switched off by a single person's royal whim.

### System requirements

- Known to work: Windows 10 and newer, 64-Bit.
  - It's designed to be architecture agnostic, so more recent versions of Windows should likely work.
  - Other architectures (e.g. Linux) are in the planning stage
- OpenXR-supported tethered headset.
  - Known to work: Oculus Quest 2, linked (Oculus Link, or using Virtual Desktop)
- For building you need:
  - **Unity 2021.3.15f1** with the installed modules for the desired target platform
  - [WiX Toolset 3.11.2](https://github.com/wixtoolset/wix3/releases/tag/wix3112rtm)

### Quick Building (Windows 64)
 1. Download the source using git: `git clone --recurse-submodules --depth=1 https://github.com/willneedit/Arteranos.git`
 2. `cd Arteranos`
 3. **OPTIONAL** - Only if the OAuth2 keys are accessible:
    1. cd Assets
    2. git clone \[**REDACTED**\]
 4. Edit the `build.bat` script to find the Unity Editor you've installed in your build machine
 5. `build.bat`
 6. If all goes well, please find the newly created `build` folder including the installation wizard, `Arteranos.msi`, amongst other files.
    - Also, you'll find the `Arteranos.exe` and `Arteranos-Server.exe`, respectively.

### Installing
 - Start the installation wizard, `Arteranos.msi`, and follow its steps.

### Now what _are_ these apps?
`Arteranos.exe` is the full-fledged client. You can use it in the desktop mode or VR, as you like, both for connecting to a server, or to host your own server, with or without your presence in the world you set up.

`Arteranos-Server.exe` is the dedicated server. It won't show anything, and it's intended to persist in the backgound and to consume minimal resouces and to be connected to from other's clients, maybe you.

### And now?
1. Run `Arteranos.exe`.
2. In the desktop mode, you can use the ESC button for the system menu.
3. In the VR mode (Oh, rightly junping in the deep end?), one of the controller buttons, besides the trigger or the grip buttons, should invoke the system menu, too.
4. Explore :) At this stage, you're offline, but you're able to change it anytime with the "Travel" section.

- In the default settings, if you're switched to the server or the host mode, you can look up the http://localhost:9779/metadata.json in any browser to see the server's public metadata.
- You can use https://readyplayer.me/ , then **Enter Hub** , walk through that gauntlet, up to the **Claim it now** stage. Click the `Capture` button. For example, it shows a URL like https://readyplayer.me/gallery/645cdd53d2d833d5908b3378 - the last part - `645cdd53d2d833d5908b3378` in this example - is this what you'd be interested for.

## Getting in Touch
To report issues and feature requests: [Github issues page](https://github.com/willneedit/Arteranos/issues).

To chat with the team and other users: join the [Arteranos Development Discussion](https://discord.gg/jHYFFd78B9).

---
## License

This product is copyright 2023 by willneedit and licensed under the [Mozilla Public License 2.0](LICENSE.md).

This package contains third-party software components, owned and licensed by [this list](Third%20Party%20Notices.md).
