# Animated Custom Sprites

Allows the usage of animated sprites for **custom** characters & items.

## Animation Clip

To create a clip, simply name each frame with `id_acs_name#interval_index`.

For example, a custom character with id `boxchicken`, you should prepare the following sprites in the **Texture** folder:

+ `boxchicken.png`, base sprite, that gets loaded as a still image, used everywhere in the game.

+ `boxchicken_acs_idle#66_1.png`, 1st frame of clip "idle", which is used when card is **idling**, with an interval of 66ms.
+ repeat follow the same convention to create frames for clip "idle".

+ `boxchicken_acs_combat#66_1.png`, 1st frame of clip "combat", which is used when card is **in combat**, with an interval of 66ms.
+ repeat follow the same convention to create frames for clip "combat".

Note that combat clip is **not required**.

+ `boxchicken_acs_customClipName#66_0.png`, 1st frame of clip "customClipName", this clip needs to be played manually by using Animated Custom Sprites API. 
+ repeat follow the same convention to create frames for clip "customClipName"...

![img](https://i.postimg.cc/25H2KpgX/image.png)

## API

By referencing AnimatedCustomSprites.dll, you can access the entire `ACS.API` namespace, with the following extension methods:
```
// clip control
card.StartAcsClip(clipName)
card.StartAcsClip(clipType)
card.StopAcsClip()

// clip access
card.GetAcsClip(clipName)
card.GetAcsClip(clipType)
card.GetAcsClips(clipName)
card.GetAcsClips(clipType)
card.GetAllAcsClips()

// clip creation
card.CreateAcsClip(sprites[], clipName, clipType, interval)
card.CreateAcsClips(sprites[])
```

## Having a Problem?

If you want to request new features, provide feedback, in need of assistance, feel free to leave comments or reach my at Elona Discord @freshcloth.
[sauce](https://github.com/gottyduke/Elin.Plugins/tree/master/AnimatedCustomSprites)
