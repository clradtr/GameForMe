using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ArenaPrototype
{
    public class ArenaGame : MonoBehaviour
    {
        [SerializeField] private float arenaRadius = 13f;

        private ArenaHUD hud;
        private AllyUnit ally;
        private Material floorMaterial;
        private Material wallMaterial;
        private Material playerMaterial;
        private Material allyMaterial;
        private Material projectileMaterial;
        private Shader arenaColorShader;
        private Shader spriteShader;

        public CombatTuning Tuning { get; private set; } = new CombatTuning();
        public PlayerController Player { get; private set; }
        public WaveSpawner WaveSpawner { get; private set; }
        public LootSystem Loot { get; private set; }
        public EquipmentSystem Equipment { get; private set; }
        public PlayerProgression Progression { get; private set; }
        public ArenaAudio Audio { get; private set; }
        public GeneratedArtDatabase Art { get; private set; }

        public float ArenaRadius
        {
            get { return arenaRadius; }
        }

        private void Start()
        {
            BuildRuntimeArena();
            StartStandaloneSmokeTestIfRequested();
        }

        private void Update()
        {
            if (Player != null && Player.Health != null && !Player.Health.IsAlive && Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        public void StartNextWave()
        {
            if (hud != null)
            {
                hud.HideRewards();
            }

            WaveSpawner.StartNextWave();
            Notify("Wave " + WaveSpawner.CurrentWave + " started", 1.4f);
        }

        public void OnEnemyKilled(EnemyController enemy)
        {
            Progression.AddExperience(enemy.ExperienceValue);
            Loot.TryDropEnemyLoot(enemy, WaveSpawner.CurrentWave);
        }

        public void OnWaveComplete(int wave)
        {
            Progression.AddExperience(28 + wave * 8);
            Audio.PlayReward();
            Notify("Wave " + wave + " clear", 1.7f);
            hud.ShowRewards(Loot.GenerateRewardChoices(wave));

            if (wave == 5)
            {
                Notify("Prototype target reached: 5-wave run complete. You can keep pushing.", 4f);
            }
        }

        public void OnPlayerDied()
        {
            WaveSpawner.StopWave();
            Notify("Run ended. Press R to restart the arena.", 6f);
        }

        public void Notify(string message, float duration)
        {
            Debug.Log(message);
            if (hud != null)
            {
                hud.ShowMessage(message, duration);
            }
        }

        public void BoostAlly(float duration)
        {
            if (ally != null)
            {
                ally.Boost(duration);
            }
        }

        public EnemyController SpawnEnemy(EnemyArchetype type, Vector3 position, int wave)
        {
            GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = type + " Enemy";
            enemyObject.transform.position = position;
            EnemyController enemy = enemyObject.AddComponent<EnemyController>();
            enemy.Setup(this, type, wave);
            return enemy;
        }

        public void SpawnProjectile(Vector3 origin, Vector3 direction, float radius, float speed, float maxDistance, bool pierce, DamageInfo info)
        {
            SpawnProjectile(origin, direction, radius, speed, maxDistance, pierce ? 99 : 0, 0, 0.58f, info);
        }

        public void SpawnProjectile(Vector3 origin, Vector3 direction, float radius, float speed, float maxDistance, int pierceCount, int splitCount, float splitDamageMultiplier, DamageInfo info)
        {
            GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = info.sourceTeam == Team.Player ? "Player Projectile" : "Enemy Projectile";
            projectileObject.transform.position = origin;
            projectileObject.transform.localScale = Vector3.one * radius * 2f;
            Renderer renderer = projectileObject.GetComponent<Renderer>();
            renderer.material = CreateMaterial(info.color, false);
            Collider collider = projectileObject.GetComponent<Collider>();
            collider.isTrigger = true;
            Rigidbody body = projectileObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            AttachProjectileSprite(projectileObject, info, radius);
            Projectile projectile = projectileObject.AddComponent<Projectile>();
            projectile.Setup(this, info, direction, radius, speed, maxDistance, pierceCount, splitCount, splitDamageMultiplier);
        }

        public void SpawnAreaPulse(Vector3 center, float radius, float delay, float lifetime, DamageInfo info, Color color)
        {
            GameObject pulseObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pulseObject.name = "Area Pulse";
            pulseObject.transform.position = new Vector3(center.x, 0.04f, center.z);
            Collider collider = pulseObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = pulseObject.GetComponent<Renderer>();
            renderer.material = CreateMaterial(new Color(color.r, color.g, color.b, 0.32f), true);
            AreaPulse pulse = pulseObject.AddComponent<AreaPulse>();
            pulse.Setup(this, info, radius, delay, lifetime, color);
        }

        public void SpawnVfx(string id, Vector3 position, float startSize, float endSize, float lifetime, Color color, bool groundAligned, float rollDegrees)
        {
            if (Art == null)
            {
                return;
            }

            GameObject prefab = Art.GetVfxPrefab(id);
            GameObject instance = null;
            if (prefab != null)
            {
                instance = Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                Sprite sprite = Art.GetVfxSprite(id);
                if (sprite == null)
                {
                    return;
                }

                instance = new GameObject("VFX - " + id);
                instance.transform.position = position;
                SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 10;
                instance.AddComponent<GeneratedSpriteVfx>();
            }

            GeneratedSpriteVfx vfx = instance.GetComponent<GeneratedSpriteVfx>();
            if (vfx != null)
            {
                vfx.Setup(lifetime, startSize, endSize, color, groundAligned, rollDegrees);
            }
        }

        public void SpawnSkillEffect(string id, Vector3 position, float startSize, float endSize, float lifetime, Color color, bool groundAligned, float rollDegrees)
        {
            if (Art == null)
            {
                return;
            }

            Sprite sprite = Art.GetSkillEffectSprite(id);
            if (sprite == null)
            {
                return;
            }

            GameObject instance = new GameObject("Skill Effect - " + id);
            instance.transform.position = position;
            SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = groundAligned ? 8 : 14;
            renderer.color = Color.white;

            GeneratedSpriteVfx vfx = instance.AddComponent<GeneratedSpriteVfx>();
            Color untinted = new Color(1f, 1f, 1f, Mathf.Clamp01(color.a <= 0f ? 1f : color.a));
            vfx.Setup(lifetime, startSize, endSize, untinted, groundAligned, rollDegrees);
        }

        public SpriteRenderer AttachEnemySprite(GameObject enemyObject, EnemyArchetype type)
        {
            switch (type)
            {
                case EnemyArchetype.Skirmisher:
                    return AttachCharacterSprite(enemyObject, "enemy_skirmisher", 2.15f, 0.72f, 6);
                case EnemyArchetype.Bulwark:
                    return AttachCharacterSprite(enemyObject, "enemy_bulwark", 3.05f, 0.92f, 5);
                default:
                    return AttachCharacterSprite(enemyObject, "enemy_striker", 2.1f, 0.72f, 5);
            }
        }

        public void DamageTeamInRadius(Vector3 center, float radius, Team targetTeam, DamageInfo info)
        {
            Vector3 bottom = new Vector3(center.x, 0.05f, center.z);
            Vector3 top = new Vector3(center.x, 2.6f, center.z);
            Collider[] colliders = Physics.OverlapCapsule(bottom, top, radius);
            DamageColliders(colliders, center, targetTeam, info, null);
        }

        public void DamageTeamInCapsule(Vector3 start, Vector3 end, float radius, Team targetTeam, DamageInfo info, HashSet<Health> alreadyDamaged)
        {
            Vector3 capsuleStart = new Vector3(start.x, 1f, start.z);
            Vector3 capsuleEnd = new Vector3(end.x, 1f, end.z);
            Collider[] colliders = Physics.OverlapCapsule(capsuleStart, capsuleEnd, radius);
            DamageColliders(colliders, (start + end) * 0.5f, targetTeam, info, alreadyDamaged);
        }

        private void DamageColliders(Collider[] colliders, Vector3 center, Team targetTeam, DamageInfo info, HashSet<Health> alreadyDamaged)
        {
            List<Health> damaged = new List<Health>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Health health = colliders[i].GetComponentInParent<Health>();
                if (health == null
                    || !health.IsAlive
                    || health.Team != targetTeam
                    || damaged.Contains(health)
                    || (alreadyDamaged != null && alreadyDamaged.Contains(health)))
                {
                    continue;
                }

                damaged.Add(health);
                if (alreadyDamaged != null)
                {
                    alreadyDamaged.Add(health);
                }

                Vector3 direction = health.transform.position - center;
                DamageInfo hit = info;
                hit.point = health.transform.position;
                hit.direction = direction.sqrMagnitude > 0.001f ? direction.normalized : info.direction;
                health.TakeDamage(hit);
            }
        }

        public void SpawnDamageNumber(Vector3 position, string text, Color color, float scale)
        {
            GameObject number = new GameObject("Damage Number");
            number.transform.position = position;
            DamageNumber damageNumber = number.AddComponent<DamageNumber>();
            damageNumber.Setup(text, color, scale);
        }

        public Health FindClosestHealth(Vector3 position, Team team, float range)
        {
            Collider[] colliders = Physics.OverlapSphere(position, range);
            Health closest = null;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < colliders.Length; i++)
            {
                Health health = colliders[i].GetComponentInParent<Health>();
                if (health == null || !health.IsAlive || health.Team != team)
                {
                    continue;
                }

                float distance = (health.transform.position - position).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closest = health;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        public Transform GetPreferredEnemyTarget(Vector3 enemyPosition)
        {
            Transform playerTarget = Player != null && Player.Health.IsAlive ? Player.transform : null;
            Transform allyTarget = ally != null && ally.Health != null && ally.Health.IsAlive ? ally.transform : null;
            if (playerTarget == null)
            {
                return allyTarget;
            }

            if (allyTarget == null)
            {
                return playerTarget;
            }

            float playerDistance = (playerTarget.position - enemyPosition).sqrMagnitude;
            float allyDistance = (allyTarget.position - enemyPosition).sqrMagnitude;
            return allyDistance < playerDistance * 0.8f ? allyTarget : playerTarget;
        }

        public bool IsInsideArena(Vector3 position, float padding)
        {
            return Mathf.Abs(position.x) <= arenaRadius - padding && Mathf.Abs(position.z) <= arenaRadius - padding;
        }

        public Vector3 ClampToArena(Vector3 position, float padding)
        {
            position.x = Mathf.Clamp(position.x, -arenaRadius + padding, arenaRadius - padding);
            position.z = Mathf.Clamp(position.z, -arenaRadius + padding, arenaRadius - padding);
            return position;
        }

        public Material CreateMaterial(Color color, bool transparent)
        {
            Shader shader = GetVisibleRuntimeShader();
            Material material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", Texture2D.whiteTexture);
            }

            if (transparent)
            {
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.renderQueue = 3000;
                material.SetOverrideTag("RenderType", "Transparent");
            }
            else
            {
                material.SetInt("_SrcBlend", (int)BlendMode.One);
                material.SetInt("_DstBlend", (int)BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.renderQueue = 2000;
                material.SetOverrideTag("RenderType", "Opaque");
            }

            return material;
        }

        private Shader GetVisibleRuntimeShader()
        {
            Shader shader = GetArenaColorShader();
            if (shader != null && shader.isSupported)
            {
                return shader;
            }

            if (spriteShader == null)
            {
                spriteShader = Shader.Find("Sprites/Default");
            }

            if (spriteShader != null)
            {
                Debug.LogWarning("Falling back to Sprites/Default because ArenaColor is unavailable.");
                return spriteShader;
            }

            return shader;
        }

        private Shader GetArenaColorShader()
        {
            if (arenaColorShader == null)
            {
                arenaColorShader = Resources.Load<Shader>("ArenaColor");
            }

            if (arenaColorShader == null)
            {
                arenaColorShader = Shader.Find("ArenaPrototype/Color");
            }

            if (arenaColorShader == null)
            {
                Debug.LogError("ArenaColor shader is missing. Runtime materials cannot be created.");
            }

            return arenaColorShader;
        }

        private void BuildRuntimeArena()
        {
            Audio = gameObject.AddComponent<ArenaAudio>();
            Audio.Setup();
            Art = Resources.Load<GeneratedArtDatabase>("GeneratedArtDatabase");
            if (Art == null)
            {
                Debug.LogWarning("GeneratedArtDatabase is missing. The arena will run, but generated icons and VFX will not appear.");
            }

            floorMaterial = CreateMaterial(new Color(0.12f, 0.14f, 0.13f), false);
            wallMaterial = CreateMaterial(new Color(0.28f, 0.29f, 0.31f), false);
            playerMaterial = CreateMaterial(new Color(0.05f, 0.58f, 1f), false);
            allyMaterial = CreateMaterial(new Color(1f, 0.82f, 0.12f), false);
            projectileMaterial = CreateMaterial(new Color(0.25f, 0.95f, 1f), false);

            BuildCameraAndLight();
            BuildFloor();
            CreatePlayer();
            CreateAlly();

            Progression = gameObject.AddComponent<PlayerProgression>();
            Progression.Setup(this, Player);

            Equipment = gameObject.AddComponent<EquipmentSystem>();
            Equipment.Setup(this, Player);

            Loot = gameObject.AddComponent<LootSystem>();
            Loot.Setup(this);

            WaveSpawner = gameObject.AddComponent<WaveSpawner>();
            WaveSpawner.Setup(this);

            hud = gameObject.AddComponent<ArenaHUD>();
            hud.Setup(this);

            Notify("Arena ready. Press Start Wave to test the combat loop.", 3f);
        }

        private void StartStandaloneSmokeTestIfRequested()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-arenaSmokeTest")
                {
                    StartCoroutine(StandaloneSmokeTestRoutine());
                    return;
                }

                if (args[i] == "-arenaScreenshot" && i + 1 < args.Length)
                {
                    StartCoroutine(StandaloneScreenshotRoutine(args[i + 1]));
                    return;
                }
            }
        }

        private IEnumerator StandaloneSmokeTestRoutine()
        {
            yield return null;

            StartNextWave();
            yield return new WaitForSeconds(2.5f);

            bool passed = Player != null
                && Player.Health != null
                && Player.Health.IsAlive
                && Player.Skills != null
                && Player.Skills.Skills.Count == 4
                && WaveSpawner != null
                && WaveSpawner.WaveActive
                && WaveSpawner.AliveCount > 0
                && GameObject.Find("Arena Floor") != null;

            if (passed)
            {
                Debug.Log("Arena standalone smoke test passed.");
                Application.Quit(0);
            }
            else
            {
                Debug.LogError("Arena standalone smoke test failed.");
                Application.Quit(1);
            }
        }

        private IEnumerator StandaloneScreenshotRoutine(string path)
        {
            yield return new WaitForSeconds(2f);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("Arena screenshot saved to " + path);
            yield return new WaitForSeconds(0.5f);
            Application.Quit(0);
        }

        private void BuildCameraAndLight()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.position = new Vector3(0f, 18f, -14f);
            camera.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = 12.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.028f, 0.032f);

            if (FindFirstObjectByType<Light>() == null)
            {
                GameObject lightObject = new GameObject("Directional Light");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            }
        }

        private void BuildFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Arena Floor";
            floor.transform.position = new Vector3(0f, -0.08f, 0f);
            floor.transform.localScale = new Vector3(arenaRadius * 2f, 0.12f, arenaRadius * 2f);
            floor.GetComponent<Renderer>().material = floorMaterial;

            CreateWall("North Wall", new Vector3(0f, 0.65f, arenaRadius + 0.25f), new Vector3(arenaRadius * 2f + 1f, 1.3f, 0.5f));
            CreateWall("South Wall", new Vector3(0f, 0.65f, -arenaRadius - 0.25f), new Vector3(arenaRadius * 2f + 1f, 1.3f, 0.5f));
            CreateWall("East Wall", new Vector3(arenaRadius + 0.25f, 0.65f, 0f), new Vector3(0.5f, 1.3f, arenaRadius * 2f + 1f));
            CreateWall("West Wall", new Vector3(-arenaRadius - 0.25f, 0.65f, 0f), new Vector3(0.5f, 1.3f, arenaRadius * 2f + 1f));
        }

        private void CreateWall(string wallName, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = wallName;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().material = wallMaterial;
        }

        private void CreatePlayer()
        {
            GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = "Player";
            playerObject.transform.position = new Vector3(0f, 1f, 0f);
            playerObject.GetComponent<Renderer>().material = playerMaterial;
            AttachCharacterSprite(playerObject, "player_hero", 2.35f, 0.82f, 7);
            CreateMarkerDisc("Player Marker", playerObject.transform, 1.15f, new Color(0.05f, 0.58f, 1f, 0.34f));
            Player = playerObject.AddComponent<PlayerController>();
            Player.Setup(this);
        }

        private void CreateAlly()
        {
            GameObject allyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            allyObject.name = "Ally Resonator";
            allyObject.transform.position = new Vector3(-1.6f, 0.9f, -1.2f);
            allyObject.transform.localScale = new Vector3(0.62f, 0.62f, 0.62f);
            allyObject.GetComponent<Renderer>().material = allyMaterial;
            AttachCharacterSprite(allyObject, "ally_resonator", 1.65f, 0.42f, 6);
            CreateMarkerDisc("Ally Marker", allyObject.transform, 0.8f, new Color(1f, 0.82f, 0.12f, 0.28f));
            ally = allyObject.AddComponent<AllyUnit>();
            ally.Setup(this);
        }

        private SpriteRenderer AttachCharacterSprite(GameObject target, string spriteId, float worldSize, float heightOffset, int sortingOrder)
        {
            if (target == null || Art == null)
            {
                return null;
            }

            Sprite sprite = Art.GetCharacterSprite(spriteId);
            if (sprite == null)
            {
                return null;
            }

            Renderer primitiveRenderer = target.GetComponent<Renderer>();
            if (primitiveRenderer != null)
            {
                primitiveRenderer.enabled = false;
            }

            GameObject visual = new GameObject("Sprite - " + spriteId);
            visual.transform.SetParent(target.transform, false);
            visual.transform.localPosition = new Vector3(0f, heightOffset, 0f);

            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = Color.white;

            SpriteBillboardVisual billboard = visual.AddComponent<SpriteBillboardVisual>();
            billboard.Setup(worldSize, true, false, 0f);
            return renderer;
        }

        private void AttachProjectileSprite(GameObject projectileObject, DamageInfo info, float radius)
        {
            if (projectileObject == null || Art == null)
            {
                return;
            }

            string spriteId = info.sourceTeam == Team.Player ? "pulse_bolt_projectile" : "enemy_bolt_projectile";
            Sprite sprite = Art.GetSkillEffectSprite(spriteId);
            if (sprite == null)
            {
                return;
            }

            Renderer primitiveRenderer = projectileObject.GetComponent<Renderer>();
            if (primitiveRenderer != null)
            {
                primitiveRenderer.enabled = false;
            }

            GameObject visual = new GameObject("Projectile Sprite - " + spriteId);
            visual.transform.SetParent(projectileObject.transform, false);
            visual.transform.localPosition = Vector3.zero;

            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = info.sourceTeam == Team.Player ? 15 : 14;
            renderer.color = Color.white;

            SpriteBillboardVisual billboard = visual.AddComponent<SpriteBillboardVisual>();
            billboard.Setup(Mathf.Max(0.85f, radius * 5.2f), true, true, 0f);
        }

        private void CreateMarkerDisc(string markerName, Transform parent, float size, Color color)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = markerName;
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(0f, -0.95f, 0f);
            marker.transform.localScale = new Vector3(size, 0.02f, size);
            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
            }

            marker.GetComponent<Renderer>().material = CreateMaterial(color, true);
        }
    }
}
