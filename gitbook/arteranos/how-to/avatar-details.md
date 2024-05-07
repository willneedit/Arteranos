# Avatar details

Avatars generated with Ready Player Me works fine, and avatars built similar to its skeleton would work.

* GLB file format
* **T-pose**
* Morph targets
  * eyeBlinkLeft
  * eyeBlinkRight
  * mouthOpen
* Skeleton naming and structure like with Ready Player Me and [Mixamo](https://www.mixamo.com/)

Example:

[https://models.readyplayer.me/63c26702e5b9a435587fba51.glb?pose=T\&meshLod=0\&textureAtlas=1024\&textureSizeLimit=1024\&textureChannels=baseColor,normal,metallicRoughness,emissive,occlusion\&morphTargets=eyeBlinkLeft,eyeBlinkRight,mouthOpen\&useHands=false\&useDracoMeshCompression=false\&useMeshOptCompression=false](https://models.readyplayer.me/63c26702e5b9a435587fba51.glb?pose=T\&meshLod=0\&textureAtlas=1024\&textureSizeLimit=1024\&textureChannels=baseColor,normal,metallicRoughness,emissive,occlusion\&morphTargets=eyeBlinkLeft,eyeBlinkRight,mouthOpen\&useHands=false\&useDracoMeshCompression=false\&useMeshOptCompression=false)

The URL breaks down as follows - the :warning: sign means it's important when downloading the avatar file.

* [https://models.readyplayer.me/63c26702e5b9a435587fba51.glb](https://models.readyplayer.me/63c26702e5b9a435587fba51.glb?pose=T\&meshLod=0\&textureAtlas=1024\&textureSizeLimit=1024\&textureChannels=baseColor,normal,metallicRoughness,emissive,occlusion\&morphTargets=eyeBlinkLeft,eyeBlinkRight,mouthOpen\&useHands=false\&useDracoMeshCompression=false\&useMeshOptCompression=false)\
  Ready Player Me's vanilla avatar URL
* pose=T\
  :warning: T-pose, not A-pose
* meshLod=0\
  Highest texture resolution
* textureAtlas=1024\
  Only one texture image, 1024x1024
* textureSuzeLimit=1024\
  Self explanatory :slight\_smile: - RPM rejects higher resolutions
* textureChannels=baseColor,normal,metallicRoughness,emissive,occlusion\
  bump mapping, emissive, and so on.
* morphTargets=eyeBlinkLeft,eyeBlinkRight,mouthOpen\
  :warning: Eye blinking and open mouth morph targets

Note:

Using RPM avatar URLs as-is (like [https://models.readyplayer.me/65abbb0520e510f61aa76dce.glb](https://models.readyplayer.me/65abbb0520e510f61aa76dce.glb)) works, Arteranos adds some adjustments as shown above (like, T-Pose instead of the default A-pose) to the submitted URL for convenience and optimization.

However, **downloading the unmodified Ready Player Me avatar file with your favourite browser** won't quite make it -- the avatar defaults to the A-pose, and with the missing morph targets the avatar would have an unrelenting stare. You'd see it when the avatar preview shows the walking avatar with the arms crossed on its backside.
