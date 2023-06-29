# Version 0.4.0

 **Breaking change - not backwards compatible prior to this version**

## New features

- People Panel: Incoming and outgoing friend requests - keep track of your outstanding friend requests
- Text messaging - send messages to online users.
  - Full end-to-end encryption, no snooping!
  - Online status setting - if you don't like to be disturbed, the messages will put on hold when you change your mind.
- Virtual Keyboard support
  - Always, VR only, never (if you can blind type with wearing a VR headset...)
  - Additional keyboard layouts? More to be added soon
- Control configuration
  - Separately left/right (in VR) configuration
  - Completely switching off the individual controller
  - Ray type: Straight line, Arc
  - Ray visibility: Always (red/white), only valid (white)
- Movement configuration
  - Snap turn / Smooth turn (slow, fast, as you like)
  - Instant teleport / Sliding "Zipline" teleport (the same...)

**Finally, the system menu panels are filled, no more "Reserved for future use"  placeholders!**

## Fixes

- People Panel: Hide buttons, not just disable them.

# Version 0.3.0

 **Breaking change - not backwards compatible prior to this version**

## New features

- Embedded avatar creation
- Avatar gallery to store and retrieve your avatars
- Nameplates
  - Button for muting/unmuting
  - Button for blocking
  - Offering and accepting friendships
- User lists
  - Online in the current server
  - Friends
  - Blocked users
- Privacy bubble, with adjustable size
  - For friends
  - For strangers
  
## Fixes

- Added Network Authenticator for access control and version check


# Version 0.2.0

## New features

- Browser integration
  - Starting Arteranos just by clicking a link in web page
  - Link provides the server address and/or the targeted world.
    - **Good for publishing, like for an event's contact details**
- **Setup wizard**
- Audio system menu pane functional
  - Master/Voice/Environment volume sliders
  - Microphone device selection
  - Microphone gain slider
  - AGC (Automatic Gain Control) to adaptively adjust the microphone gain

## Fixes

- Major code cleanup
- Addad a notification for failed server connection
- UPnP NAT traversal now on-demand
  - Now you can use the dedicated server _and_ the desktop client at the same time.
- New server entry improved - a simple server like 'localhost' should suffice in the default cases

# Version 0.1.0

## New features

- Core functionality
  - OpenXR capability
  - Server running
  - Client running
  - Loading Worlds
  - Connecting to remote servers
  - Voice chat
  - Avatar selection
  - Client & Server preferences
  