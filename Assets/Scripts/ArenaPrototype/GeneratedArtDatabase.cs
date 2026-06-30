using System;
using UnityEngine;

namespace ArenaPrototype
{
    [Serializable]
    public class SpriteArtEntry
    {
        public string id;
        public Sprite sprite;
    }

    [Serializable]
    public class VfxArtEntry
    {
        public string id;
        public Sprite sprite;
        public GameObject prefab;
    }

    public class GeneratedArtDatabase : ScriptableObject
    {
        public SpriteArtEntry[] skillIcons = new SpriteArtEntry[0];
        public SpriteArtEntry[] itemIcons = new SpriteArtEntry[0];
        public SpriteArtEntry[] characterSprites = new SpriteArtEntry[0];
        public SpriteArtEntry[] skillEffects = new SpriteArtEntry[0];
        public VfxArtEntry[] vfx = new VfxArtEntry[0];

        public Sprite GetSkillIcon(string id)
        {
            return FindSprite(skillIcons, id);
        }

        public Sprite GetItemIcon(string id)
        {
            return FindSprite(itemIcons, id);
        }

        public Sprite GetCharacterSprite(string id)
        {
            return FindSprite(characterSprites, id);
        }

        public Sprite GetSkillEffectSprite(string id)
        {
            return FindSprite(skillEffects, id);
        }

        public GameObject GetVfxPrefab(string id)
        {
            if (string.IsNullOrEmpty(id) || vfx == null)
            {
                return null;
            }

            for (int i = 0; i < vfx.Length; i++)
            {
                VfxArtEntry entry = vfx[i];
                if (entry != null && entry.id == id)
                {
                    return entry.prefab;
                }
            }

            return null;
        }

        public Sprite GetVfxSprite(string id)
        {
            if (string.IsNullOrEmpty(id) || vfx == null)
            {
                return null;
            }

            for (int i = 0; i < vfx.Length; i++)
            {
                VfxArtEntry entry = vfx[i];
                if (entry != null && entry.id == id)
                {
                    return entry.sprite;
                }
            }

            return null;
        }

        private static Sprite FindSprite(SpriteArtEntry[] entries, string id)
        {
            if (string.IsNullOrEmpty(id) || entries == null)
            {
                return null;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                SpriteArtEntry entry = entries[i];
                if (entry != null && entry.id == id)
                {
                    return entry.sprite;
                }
            }

            return null;
        }
    }
}
