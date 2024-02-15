/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Arteranos.Core
{
    [Serializable]
    public struct EmojiButton
    {
        public Texture2D Image;
        public string HoverTip;
        public ParticleSystem Appearance;
        [NonSerialized] public Material Material;
    }

    [CreateAssetMenu(fileName = "Emojis", menuName = "Scriptable Objects/Application/Emoji Settings")]
    public class EmojiSettings : ScriptableObject
    {
        [SerializeField] private EmojiButton[] EmojiButtons;
        [SerializeField] private Material MaterialTemplate;

        private readonly Dictionary<string, EmojiButton> KnownEmojis = new();

        private static EmojiSettings Instance = null;

        public static EmojiSettings Load()
        {
            if(Instance != null) return Instance;

            EmojiSettings emojiSettings = BP.I.EmojiSettings;

            for(int i = 0; i < emojiSettings.EmojiButtons.Length; i++)
            {
                EmojiButton emojiButton = emojiSettings.EmojiButtons[i];
                Material mat = new(emojiSettings.MaterialTemplate) { mainTexture = emojiButton.Image };
                emojiButton.Material = mat;

                emojiSettings.KnownEmojis.Add(emojiButton.Image.name, emojiButton);
            }

            Instance = emojiSettings;

            return emojiSettings;
        }

        public ParticleSystem GetEmotePS(string emoteName)
        {
            if(!KnownEmojis.TryGetValue(emoteName, out EmojiButton emojiButton)) return null;

            ParticleSystem ps = Instantiate(emojiButton.Appearance);
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = emojiButton.Material;

            return ps;
        }

        public IEnumerable<EmojiButton> EnumerateEmotes() => from emoji in EmojiButtons select emoji;
    }
}
