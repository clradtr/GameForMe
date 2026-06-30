#if UNITY_EDITOR
using System.IO;
using ArenaPrototype;
using UnityEditor;
using UnityEngine;

namespace ArenaPrototypeEditor
{
    public static class GeneratedArtSetup
    {
        private const string DatabasePath = "Assets/Resources/GeneratedArtDatabase.asset";
        private const string SkillIconFolder = "Assets/Art/Generated/SkillIcons";
        private const string VfxFolder = "Assets/Art/Generated/VFX";
        private const string ItemIconFolder = "Assets/Art/Generated/ItemIcons";
        private const string CharacterFolder = "Assets/Art/Generated/Characters";
        private const string SkillEffectFolder = "Assets/Art/Generated/SkillEffects";
        private const string VfxPrefabFolder = "Assets/Art/Generated/VFX/Prefabs";

        private static readonly ArtMap[] SkillIcons =
        {
            new ArtMap("pulse_bolt", SkillIconFolder + "/pulse_bolt.png"),
            new ArtMap("vector_dash", SkillIconFolder + "/vector_dash.png"),
            new ArtMap("circuit_nova", SkillIconFolder + "/circuit_nova.png"),
            new ArtMap("resonance_ward", SkillIconFolder + "/resonance_ward.png"),
            new ArtMap("shadow_cleave", SkillIconFolder + "/shadow_cleave.png"),
            new ArtMap("blood_lance", SkillIconFolder + "/blood_lance.png"),
            new ArtMap("frost_chain", SkillIconFolder + "/frost_chain.png"),
            new ArtMap("soul_barrier", SkillIconFolder + "/soul_barrier.png")
        };

        private static readonly ArtMap[] ItemIcons =
        {
            new ArtMap("ember_blade", ItemIconFolder + "/ember_blade.png"),
            new ArtMap("guard_plate", ItemIconFolder + "/guard_plate.png"),
            new ArtMap("tempo_charm", ItemIconFolder + "/tempo_charm.png"),
            new ArtMap("spark_core", ItemIconFolder + "/spark_core.png"),
            new ArtMap("hunter_boots", ItemIconFolder + "/hunter_boots.png")
        };

        private static readonly ArtMap[] CharacterSprites =
        {
            new ArtMap("player_hero", CharacterFolder + "/player_hero.png"),
            new ArtMap("ally_resonator", CharacterFolder + "/ally_resonator.png"),
            new ArtMap("enemy_striker", CharacterFolder + "/enemy_striker.png"),
            new ArtMap("enemy_skirmisher", CharacterFolder + "/enemy_skirmisher.png"),
            new ArtMap("enemy_bulwark", CharacterFolder + "/enemy_bulwark.png")
        };

        private static readonly ArtMap[] SkillEffects =
        {
            new ArtMap("pulse_bolt_projectile", SkillEffectFolder + "/skill_pulse_bolt_projectile.png"),
            new ArtMap("enemy_bolt_projectile", SkillEffectFolder + "/skill_enemy_bolt_projectile.png"),
            new ArtMap("vector_dash_slash", SkillEffectFolder + "/skill_vector_dash_slash.png"),
            new ArtMap("circuit_nova_burst", SkillEffectFolder + "/skill_circuit_nova_burst.png"),
            new ArtMap("resonance_ward_aura", SkillEffectFolder + "/skill_resonance_ward_aura.png")
        };

        private static readonly ArtMap[] Vfx =
        {
            new ArtMap("slash_arc", VfxFolder + "/slash_arc.png"),
            new ArtMap("hit_spark", VfxFolder + "/hit_spark.png"),
            new ArtMap("shockwave_ring", VfxFolder + "/shockwave_ring.png"),
            new ArtMap("cast_circle", VfxFolder + "/cast_circle.png"),
            new ArtMap("telegraph_marker", VfxFolder + "/telegraph_marker.png")
        };

        [MenuItem("Arena Prototype/Rebuild Generated Art Assets")]
        public static void BuildGeneratedArtAssets()
        {
            EnsureFolderPath("Assets/Art/Generated");
            EnsureFolderPath("Assets/Resources");
            EnsureFolderPath(VfxPrefabFolder);

            AssetDatabase.StartAssetEditing();
            try
            {
                ConfigureSpriteImports(SkillIcons);
                ConfigureSpriteImports(ItemIcons);
                ConfigureSpriteImports(CharacterSprites);
                ConfigureSpriteImports(SkillEffects);
                ConfigureSpriteImports(Vfx);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GeneratedArtDatabase database = AssetDatabase.LoadAssetAtPath<GeneratedArtDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<GeneratedArtDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            database.skillIcons = BuildSpriteEntries(SkillIcons);
            database.itemIcons = BuildSpriteEntries(ItemIcons);
            database.characterSprites = BuildSpriteEntries(CharacterSprites);
            database.skillEffects = BuildSpriteEntries(SkillEffects);
            database.vfx = BuildVfxEntries(Vfx);

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated art database rebuilt at " + DatabasePath);
        }

        private static SpriteArtEntry[] BuildSpriteEntries(ArtMap[] maps)
        {
            SpriteArtEntry[] entries = new SpriteArtEntry[maps.Length];
            for (int i = 0; i < maps.Length; i++)
            {
                entries[i] = new SpriteArtEntry
                {
                    id = maps[i].id,
                    sprite = LoadSprite(maps[i].path)
                };
            }

            return entries;
        }

        private static VfxArtEntry[] BuildVfxEntries(ArtMap[] maps)
        {
            VfxArtEntry[] entries = new VfxArtEntry[maps.Length];
            for (int i = 0; i < maps.Length; i++)
            {
                Sprite sprite = LoadSprite(maps[i].path);
                entries[i] = new VfxArtEntry
                {
                    id = maps[i].id,
                    sprite = sprite,
                    prefab = CreateVfxPrefab(maps[i], sprite)
                };
            }

            return entries;
        }

        private static GameObject CreateVfxPrefab(ArtMap map, Sprite sprite)
        {
            if (sprite == null)
            {
                return null;
            }

            GameObject obj = new GameObject("VFX - " + map.id);
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 10;
            renderer.color = Color.white;
            obj.AddComponent<GeneratedSpriteVfx>();

            string prefabPath = VfxPrefabFolder + "/" + map.id + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            Object.DestroyImmediate(obj);
            return prefab;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning("Generated sprite missing or not imported as Sprite: " + path);
            }

            return sprite;
        }

        private static void ConfigureSpriteImports(ArtMap[] maps)
        {
            for (int i = 0; i < maps.Length; i++)
            {
                string path = maps[i].path;
                if (!File.Exists(path))
                {
                    Debug.LogWarning("Generated art file is missing: " + path);
                    continue;
                }

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    importer = AssetImporter.GetAtPath(path) as TextureImporter;
                }

                if (importer == null)
                {
                    Debug.LogWarning("Could not configure generated art import settings: " + path);
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 256f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 512;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureFolderPath(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private struct ArtMap
        {
            public readonly string id;
            public readonly string path;

            public ArtMap(string artId, string assetPath)
            {
                id = artId;
                path = assetPath;
            }
        }
    }
}
#endif
