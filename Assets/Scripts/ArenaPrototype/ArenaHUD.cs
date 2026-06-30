using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaPrototype
{
    public class ArenaHUD : MonoBehaviour
    {
        private class SkillView
        {
            public Text title;
            public Text cooldown;
            public Image fill;
        }

        private readonly List<SkillView> skillViews = new List<SkillView>();
        private readonly List<Button> rewardButtons = new List<Button>();
        private readonly List<Text> rewardButtonLabels = new List<Text>();

        private ArenaGame game;
        private Font font;
        private Text waveText;
        private Text healthText;
        private Text statsText;
        private Text equipmentText;
        private Text objectiveText;
        private GameObject guidePanel;
        private Text messageText;
        private Text startButtonText;
        private Button startButton;
        private GameObject rewardPanel;
        private Text rewardHeader;
        private GameObject debugPanel;
        private List<RewardChoice> activeRewards;
        private float messageUntil;
        private bool debugOverlayOpen;
        private GUIStyle overlayBoxStyle;
        private GUIStyle overlayHeaderStyle;
        private GUIStyle overlayLabelStyle;
        private GUIStyle overlaySmallStyle;
        private GUIStyle overlayButtonStyle;
        private GUIStyle overlayRewardButtonStyle;
        private GUIStyle overlayRewardTitleStyle;
        private GUIStyle overlayRewardDescriptionStyle;
        private GUIStyle overlayIconBoxStyle;
        private string overlayMessage = "";

        public bool RewardOpen
        {
            get { return activeRewards != null && activeRewards.Count > 0; }
        }

        public void Setup(ArenaGame owner)
        {
            game = owner;
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            Build();
        }

        public void ShowMessage(string message, float duration)
        {
            if (messageText == null)
            {
                return;
            }

            messageText.text = message;
            messageText.enabled = true;
            messageUntil = Time.time + duration;
            overlayMessage = message;
        }

        public void ShowRewards(List<RewardChoice> rewards)
        {
            activeRewards = rewards;
            if (rewardPanel == null)
            {
                return;
            }

            rewardPanel.SetActive(true);
            rewardHeader.text = string.Format("Wave {0} clear - choose one reward", game.WaveSpawner.CurrentWave);

            for (int i = 0; i < rewardButtons.Count; i++)
            {
                bool active = rewards != null && i < rewards.Count;
                rewardButtons[i].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                RewardChoice reward = rewards[i];
                rewardButtonLabels[i].text = reward.title + "\n" + reward.description;
                Color buttonColor = Color.Lerp(reward.color, Color.black, 0.48f);
                rewardButtons[i].GetComponent<Image>().color = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.94f);
            }
        }

        public void HideRewards()
        {
            activeRewards = null;
            if (rewardPanel != null)
            {
                rewardPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (game == null || game.Player == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F1) && debugPanel != null)
            {
                debugPanel.SetActive(!debugPanel.activeSelf);
                debugOverlayOpen = !debugOverlayOpen;
            }

            RefreshStatus();
            RefreshSkills();

            if (messageText != null && messageText.enabled && Time.time > messageUntil)
            {
                messageText.enabled = false;
                overlayMessage = "";
            }
        }

        private void OnGUI()
        {
            if (game == null || game.Player == null || game.Player.Health == null)
            {
                return;
            }

            EnsureOverlayStyles();

            bool alive = game.Player.Health.IsAlive;
            bool canStart = alive && !game.WaveSpawner.WaveActive && !RewardOpen;
            float width = Screen.width;
            float height = Screen.height;

            GUI.Box(new Rect(12f, 12f, 330f, 198f), GUIContent.none, overlayBoxStyle);
            GUI.Label(new Rect(28f, 24f, 300f, 30f), game.WaveSpawner.WaveActive ? string.Format("WAVE {0}   ENEMIES {1}", game.WaveSpawner.CurrentWave, game.WaveSpawner.AliveCount) : string.Format("WAVE {0} READY", game.WaveSpawner.CurrentWave + 1), overlayHeaderStyle);
            GUI.Label(new Rect(28f, 58f, 300f, 24f), string.Format("HP {0:0}/{1:0}    LV {2}    XP {3}/{4}", game.Player.Health.CurrentHealth, game.Player.Health.MaxHealth, game.Progression.Level, game.Progression.Experience, game.Progression.NextLevelExperience), overlayLabelStyle);
            GUI.Label(new Rect(28f, 84f, 300f, 22f), string.Format("ATK {0:0.#}  DEF {1:0.#}  SPD {2:0.#}  CDR {3:0.#}%", game.Player.CurrentStats.attack, game.Player.CurrentStats.defense, game.Player.CurrentStats.moveSpeed, game.Player.CurrentStats.cooldownReduction * 100f), overlaySmallStyle);
            GUI.Label(new Rect(28f, 108f, 300f, 22f), GetBuildSummary(), overlaySmallStyle);
            GUI.Label(new Rect(28f, 132f, 300f, 22f), game.Equipment.Summary(), overlaySmallStyle);
            DrawEquippedItemIcons(new Rect(28f, 160f, 300f, 34f));

            GUI.Box(new Rect(360f, 12f, width - 372f, 44f), GUIContent.none, overlayBoxStyle);
            GUI.Label(new Rect(378f, 21f, width - 408f, 28f), GetObjectiveText(alive), overlayLabelStyle);

            if (canStart)
            {
                if (GUI.Button(new Rect(width * 0.5f - 130f, 66f, 260f, 54f), game.WaveSpawner.CurrentWave == 0 ? "START WAVE" : "NEXT WAVE", overlayButtonStyle))
                {
                    game.StartNextWave();
                }
            }

            if (canStart && game.WaveSpawner.CurrentWave == 0)
            {
                GUI.Box(new Rect(width * 0.5f - 355f, height * 0.5f - 160f, 710f, 270f), GUIContent.none, overlayBoxStyle);
                GUI.Label(new Rect(width * 0.5f - 320f, height * 0.5f - 138f, 640f, 42f), "WHAT TO DO", overlayHeaderStyle);
                GUI.Label(new Rect(width * 0.5f - 320f, height * 0.5f - 90f, 640f, 190f),
                    "1. Click START WAVE.\n" +
                    "2. Move with WASD. Aim with the mouse.\n" +
                    "3. Left Click / 1 fires Pulse Bolt.\n" +
                    "4. Space = dash, Q = area blast, E = shield + buff.\n" +
                    "5. Clear enemies, then choose one reward card.",
                    overlayLabelStyle);
            }

            if (RewardOpen)
            {
                DrawRewardOverlay(width, height);
            }

            if (!string.IsNullOrEmpty(overlayMessage) && Time.time <= messageUntil)
            {
                GUI.Box(new Rect(width * 0.5f - 360f, 128f, 720f, 42f), GUIContent.none, overlayBoxStyle);
                GUI.Label(new Rect(width * 0.5f - 345f, 136f, 690f, 28f), overlayMessage, overlayLabelStyle);
            }

            DrawSkillOverlay(width, height);

            if (debugOverlayOpen)
            {
                DrawDebugOverlay(width, height);
            }
        }

        private void DrawRewardOverlay(float width, float height)
        {
            GUI.Box(new Rect(width * 0.5f - 385f, height * 0.5f - 165f, 770f, 330f), GUIContent.none, overlayBoxStyle);
            GUI.Label(new Rect(width * 0.5f - 350f, height * 0.5f - 142f, 700f, 34f), string.Format("WAVE {0} CLEAR - CHOOSE ONE REWARD", game.WaveSpawner.CurrentWave), overlayHeaderStyle);

            float cardWidth = 230f;
            float cardHeight = 210f;
            float spacing = 16f;
            float startX = width * 0.5f - (cardWidth * 3f + spacing * 2f) * 0.5f;
            float y = height * 0.5f - 82f;

            for (int i = 0; i < activeRewards.Count && i < 3; i++)
            {
                RewardChoice reward = activeRewards[i];
                Rect card = new Rect(startX + i * (cardWidth + spacing), y, cardWidth, cardHeight);
                if (GUI.Button(card, GUIContent.none, overlayRewardButtonStyle))
                {
                    reward.apply();
                    HideRewards();
                }

                DrawRewardCard(reward, card);
            }
        }

        private void DrawSkillOverlay(float width, float height)
        {
            IReadOnlyList<SkillRuntime> skills = game.Player.Skills.Skills;
            float boxWidth = 220f;
            float spacing = 12f;
            float total = boxWidth * 4f + spacing * 3f;
            float startX = width * 0.5f - total * 0.5f;
            float y = height - 112f;

            for (int i = 0; i < skills.Count && i < 4; i++)
            {
                SkillRuntime skill = skills[i];
                Rect rect = new Rect(startX + i * (boxWidth + spacing), y, boxWidth, 94f);
                GUI.Box(rect, GUIContent.none, overlayBoxStyle);
                Rect iconRect = new Rect(rect.x + 12f, rect.y + 15f, 64f, 64f);
                DrawIconFrame(iconRect, skill.Color);
                Sprite skillIcon = game.Art != null ? game.Art.GetSkillIcon(skill.IconId) : null;
                DrawSprite(skillIcon, new Rect(iconRect.x + 3f, iconRect.y + 3f, iconRect.width - 6f, iconRect.height - 6f), Color.white);

                float cooldownFraction = skill.GetCooldownFraction(game.Tuning, game.Player.CurrentStats);
                if (cooldownFraction > 0.01f)
                {
                    Color previousColor = GUI.color;
                    GUI.color = new Color(0f, 0f, 0f, 0.62f * cooldownFraction);
                    GUI.DrawTexture(iconRect, Texture2D.whiteTexture);
                    GUI.color = previousColor;
                }

                GUI.Label(new Rect(rect.x + 88f, rect.y + 10f, boxWidth - 100f, 22f), skill.InputLabel, overlayRewardTitleStyle);
                GUI.Label(new Rect(rect.x + 88f, rect.y + 36f, boxWidth - 100f, 24f), skill.DisplayName, overlayLabelStyle);
                GUI.Label(new Rect(rect.x + 88f, rect.y + 64f, boxWidth - 100f, 22f), skill.CooldownRemaining > 0.05f ? string.Format("Cooldown {0:0.0}s", skill.CooldownRemaining) : GetSkillReadyHint(i), overlaySmallStyle);
            }
        }

        private void DrawRewardCard(RewardChoice reward, Rect card)
        {
            Sprite icon = game.Art != null ? game.Art.GetItemIcon(reward.iconId) : null;
            Rect iconRect = new Rect(card.x + card.width * 0.5f - 36f, card.y + 16f, 72f, 72f);
            DrawIconFrame(iconRect, reward.color);
            DrawSprite(icon, new Rect(iconRect.x + 4f, iconRect.y + 4f, iconRect.width - 8f, iconRect.height - 8f), Color.white);
            GUI.Label(new Rect(card.x + 12f, card.y + 98f, card.width - 24f, 42f), reward.title, overlayRewardTitleStyle);
            GUI.Label(new Rect(card.x + 16f, card.y + 142f, card.width - 32f, 56f), reward.description, overlayRewardDescriptionStyle);
        }

        private void DrawEquippedItemIcons(Rect area)
        {
            if (game == null || game.Equipment == null)
            {
                return;
            }

            EquipmentSlot[] slots = { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Charm };
            float size = 32f;
            float spacing = 8f;
            for (int i = 0; i < slots.Length; i++)
            {
                Rect rect = new Rect(area.x + i * (size + spacing), area.y, size, size);
                EquipmentItem item = game.Equipment.GetEquipped(slots[i]);
                Color color = item != null ? item.tint : new Color(0.18f, 0.2f, 0.22f, 1f);
                DrawIconFrame(rect, color);
                if (item != null && game.Art != null)
                {
                    DrawSprite(game.Art.GetItemIcon(item.iconId), new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), Color.white);
                }
                else
                {
                    GUI.Label(rect, slots[i].ToString().Substring(0, 1), overlayRewardDescriptionStyle);
                }
            }
        }

        private void DrawIconFrame(Rect rect, Color color)
        {
            GUI.Box(rect, GUIContent.none, overlayIconBoxStyle);
            Color previousColor = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, 0.22f);
            GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void DrawSprite(Sprite sprite, Rect rect, Color color)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            Texture texture = sprite.texture;
            Rect textureRect = sprite.textureRect;
            Rect texCoords = new Rect(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height);
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(rect, texture, texCoords, true);
            GUI.color = previousColor;
        }

        private string GetObjectiveText(bool alive)
        {
            if (!alive)
            {
                return "Run ended. Press R to restart.";
            }

            if (RewardOpen)
            {
                return "Choose one reward card to change the next wave.";
            }

            if (game.WaveSpawner.WaveActive)
            {
                return "Fight enemies. Keep moving, aim with mouse, use Q when enemies group up.";
            }

            return "Press START WAVE to begin. Clear enemies, choose reward, repeat.";
        }

        private string GetBuildSummary()
        {
            ArenaStats stats = game.Player.CurrentStats;
            return string.Format("BOLTS {0}  PIERCE {1}  SPLIT {2}  AREA +{3:0.#}", 1 + stats.projectileCount, stats.pierceCount, stats.splitCount, stats.areaRadius);
        }

        private void DrawDebugOverlay(float width, float height)
        {
            Rect panel = new Rect(width - 386f, 68f, 370f, 442f);
            GUI.Box(panel, GUIContent.none, overlayBoxStyle);
            GUI.Label(new Rect(panel.x + 16f, panel.y + 12f, panel.width - 32f, 28f), "DEBUG TUNING", overlayHeaderStyle);

            float y = panel.y + 52f;
            DrawDebugSlider(panel.x + 16f, ref y, "Enemy HP", 0.35f, 3f, game.Tuning.enemyHealthMultiplier, v => game.Tuning.enemyHealthMultiplier = v, v => v.ToString("0.00") + "x");
            DrawDebugSlider(panel.x + 16f, ref y, "Enemy Damage", 0.2f, 3f, game.Tuning.enemyDamageMultiplier, v => game.Tuning.enemyDamageMultiplier = v, v => v.ToString("0.00") + "x");
            DrawDebugSlider(panel.x + 16f, ref y, "Player Damage", 0.4f, 3f, game.Tuning.playerDamageMultiplier, v => game.Tuning.playerDamageMultiplier = v, v => v.ToString("0.00") + "x");
            DrawDebugSlider(panel.x + 16f, ref y, "Spawn Rate", 0.35f, 3f, game.Tuning.spawnRateMultiplier, v => game.Tuning.spawnRateMultiplier = v, v => v.ToString("0.00") + "x");
            DrawDebugSlider(panel.x + 16f, ref y, "Basic CD", 0.12f, 1.5f, game.Tuning.basicCooldown, v => game.Tuning.basicCooldown = v, v => v.ToString("0.00") + "s");
            DrawDebugSlider(panel.x + 16f, ref y, "Area CD", 1.5f, 13f, game.Tuning.areaCooldown, v => game.Tuning.areaCooldown = v, v => v.ToString("0.0") + "s");
            DrawDebugSlider(panel.x + 16f, ref y, "Bolt Hitbox", 0f, 0.75f, game.Tuning.projectileHitboxPadding, v => game.Tuning.projectileHitboxPadding = v, v => "+" + v.ToString("0.00"));

            bool showHitboxes = GUI.Toggle(new Rect(panel.x + 18f, y + 8f, panel.width - 36f, 26f), game.Tuning.showProjectileHitboxes, "Show projectile hitboxes", overlaySmallStyle);
            game.Tuning.showProjectileHitboxes = showHitboxes;
        }

        private void DrawDebugSlider(float x, ref float y, string label, float min, float max, float value, Action<float> setter, Func<float, string> formatter)
        {
            GUI.Label(new Rect(x, y, 220f, 20f), label + ": " + formatter(value), overlaySmallStyle);
            float nextValue = GUI.HorizontalSlider(new Rect(x, y + 24f, 330f, 18f), value, min, max);
            if (!Mathf.Approximately(nextValue, value))
            {
                setter(nextValue);
            }

            y += 48f;
        }

        private void EnsureOverlayStyles()
        {
            if (overlayBoxStyle != null)
            {
                return;
            }

            Texture2D darkTexture = new Texture2D(1, 1);
            darkTexture.SetPixel(0, 0, new Color(0.015f, 0.018f, 0.022f, 1f));
            darkTexture.Apply();

            Texture2D buttonTexture = new Texture2D(1, 1);
            buttonTexture.SetPixel(0, 0, new Color(0.06f, 0.45f, 0.25f, 1f));
            buttonTexture.Apply();

            overlayBoxStyle = new GUIStyle(GUI.skin.box);
            overlayBoxStyle.normal.background = darkTexture;
            overlayBoxStyle.border = new RectOffset(4, 4, 4, 4);

            overlayHeaderStyle = new GUIStyle(GUI.skin.label);
            overlayHeaderStyle.fontSize = 22;
            overlayHeaderStyle.fontStyle = FontStyle.Bold;
            overlayHeaderStyle.normal.textColor = Color.white;
            overlayHeaderStyle.alignment = TextAnchor.MiddleCenter;
            overlayHeaderStyle.wordWrap = true;

            overlayLabelStyle = new GUIStyle(GUI.skin.label);
            overlayLabelStyle.fontSize = 20;
            overlayLabelStyle.fontStyle = FontStyle.Bold;
            overlayLabelStyle.normal.textColor = Color.white;
            overlayLabelStyle.alignment = TextAnchor.UpperLeft;
            overlayLabelStyle.wordWrap = true;

            overlaySmallStyle = new GUIStyle(GUI.skin.label);
            overlaySmallStyle.fontSize = 16;
            overlaySmallStyle.normal.textColor = new Color(0.9f, 0.96f, 1f);
            overlaySmallStyle.alignment = TextAnchor.UpperLeft;
            overlaySmallStyle.wordWrap = true;

            overlayButtonStyle = new GUIStyle(GUI.skin.button);
            overlayButtonStyle.fontSize = 26;
            overlayButtonStyle.fontStyle = FontStyle.Bold;
            overlayButtonStyle.normal.background = buttonTexture;
            overlayButtonStyle.hover.background = buttonTexture;
            overlayButtonStyle.active.background = buttonTexture;
            overlayButtonStyle.normal.textColor = Color.white;
            overlayButtonStyle.hover.textColor = Color.white;
            overlayButtonStyle.active.textColor = Color.white;

            overlayRewardButtonStyle = new GUIStyle(overlayButtonStyle);
            overlayRewardButtonStyle.fontSize = 18;
            overlayRewardButtonStyle.alignment = TextAnchor.MiddleCenter;
            overlayRewardButtonStyle.wordWrap = true;
            overlayRewardButtonStyle.padding = new RectOffset(10, 10, 10, 10);

            overlayRewardTitleStyle = new GUIStyle(GUI.skin.label);
            overlayRewardTitleStyle.fontSize = 18;
            overlayRewardTitleStyle.fontStyle = FontStyle.Bold;
            overlayRewardTitleStyle.normal.textColor = Color.white;
            overlayRewardTitleStyle.alignment = TextAnchor.MiddleCenter;
            overlayRewardTitleStyle.wordWrap = true;

            overlayRewardDescriptionStyle = new GUIStyle(GUI.skin.label);
            overlayRewardDescriptionStyle.fontSize = 15;
            overlayRewardDescriptionStyle.normal.textColor = new Color(0.9f, 0.96f, 1f);
            overlayRewardDescriptionStyle.alignment = TextAnchor.MiddleCenter;
            overlayRewardDescriptionStyle.wordWrap = true;

            overlayIconBoxStyle = new GUIStyle(GUI.skin.box);
            overlayIconBoxStyle.normal.background = darkTexture;
            overlayIconBoxStyle.border = new RectOffset(3, 3, 3, 3);
        }

        private void Build()
        {
            GameObject canvasObject = new GameObject("Arena HUD");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform statusPanel = CreatePanel(canvasObject.transform, "Status Panel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(430f, 220f), new Color(0.015f, 0.018f, 0.022f, 0.94f));
            VerticalLayoutGroup statusLayout = statusPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            statusLayout.padding = new RectOffset(14, 14, 12, 12);
            statusLayout.spacing = 4f;
            waveText = CreateLayoutText(statusPanel, "Wave", 28, Color.white, TextAnchor.MiddleLeft, 38f);
            healthText = CreateLayoutText(statusPanel, "Health", 22, new Color(0.86f, 1f, 0.86f), TextAnchor.MiddleLeft, 32f);
            statsText = CreateLayoutText(statusPanel, "Stats", 18, Color.white, TextAnchor.UpperLeft, 74f);
            equipmentText = CreateLayoutText(statusPanel, "Equipment", 16, new Color(1f, 0.96f, 0.78f), TextAnchor.UpperLeft, 48f);

            startButton = CreateButton(canvasObject.transform, "Start Button", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(300f, 64f), "START WAVE", 26, new Color(0.05f, 0.45f, 0.26f, 1f), game.StartNextWave);
            startButtonText = startButton.GetComponentInChildren<Text>();

            objectiveText = CreateText(canvasObject.transform, "Objective", "", 24, Color.white, TextAnchor.MiddleCenter);
            RectTransform objectiveRect = objectiveText.GetComponent<RectTransform>();
            objectiveRect.anchorMin = new Vector2(0.5f, 1f);
            objectiveRect.anchorMax = new Vector2(0.5f, 1f);
            objectiveRect.pivot = new Vector2(0.5f, 1f);
            objectiveRect.anchoredPosition = new Vector2(0f, -96f);
            objectiveRect.sizeDelta = new Vector2(980f, 42f);

            messageText = CreateText(canvasObject.transform, "Message", "", 22, new Color(1f, 0.95f, 0.72f), TextAnchor.MiddleCenter);
            RectTransform messageRect = messageText.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0.5f, 1f);
            messageRect.anchorMax = new Vector2(0.5f, 1f);
            messageRect.pivot = new Vector2(0.5f, 1f);
            messageRect.anchoredPosition = new Vector2(0f, -140f);
            messageRect.sizeDelta = new Vector2(980f, 42f);
            messageText.enabled = false;

            BuildGuidePanel(canvasObject.transform);
            BuildSkillBar(canvasObject.transform);
            BuildRewardPanel(canvasObject.transform);
            BuildDebugPanel(canvasObject.transform);
            canvasObject.SetActive(false);
        }

        private void BuildGuidePanel(Transform parent)
        {
            RectTransform panel = CreatePanel(parent, "Start Guide Panel", new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 280f), new Color(0.012f, 0.014f, 0.018f, 0.96f));
            guidePanel = panel.gameObject;
            VerticalLayoutGroup layout = guidePanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 18, 18);
            layout.spacing = 8f;

            CreateLayoutText(panel, "Guide Header", "WHAT TO DO", 32, Color.white, TextAnchor.MiddleCenter, 48f);
            Text guide = CreateLayoutText(panel, "Guide Body",
                "1. Click START WAVE at the top.\n" +
                "2. Move with WASD. Aim with the mouse.\n" +
                "3. Left Click / 1 fires Pulse Bolt.\n" +
                "4. Space = dash, Q = area blast, E = shield + buff.\n" +
                "5. Clear enemies, then choose one reward card.",
                22,
                new Color(0.94f, 0.97f, 1f),
                TextAnchor.UpperLeft,
                180f);
            guide.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void BuildSkillBar(Transform parent)
        {
            RectTransform skillBar = CreatePanel(parent, "Skill Bar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1000f, 124f), new Color(0.012f, 0.014f, 0.018f, 0.92f));
            HorizontalLayoutGroup layout = skillBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 10f;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            IReadOnlyList<SkillRuntime> skills = game.Player.Skills.Skills;
            for (int i = 0; i < skills.Count; i++)
            {
                SkillRuntime skill = skills[i];
                RectTransform slot = CreatePanel(skillBar, "Skill " + i, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230f, 100f), new Color(0.055f, 0.06f, 0.07f, 0.98f));
                LayoutElement layoutElement = slot.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = 235f;
                Image fill = CreateImage(slot, "Cooldown Fill", new Color(skill.Color.r, skill.Color.g, skill.Color.b, 0.34f));
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Radial360;
                fill.fillOrigin = (int)Image.Origin360.Top;
                fill.fillAmount = 0f;

                Text title = CreateText(slot, "Title", skill.InputLabel + "\n" + skill.DisplayName, 17, Color.white, TextAnchor.UpperCenter);
                Stretch(title.GetComponent<RectTransform>(), new Vector2(6f, 7f), new Vector2(-6f, -5f));

                Text cooldown = CreateText(slot, "Cooldown", GetSkillReadyHint(i), 15, new Color(0.92f, 0.98f, 1f), TextAnchor.LowerCenter);
                Stretch(cooldown.GetComponent<RectTransform>(), new Vector2(6f, 6f), new Vector2(-6f, -7f));

                skillViews.Add(new SkillView { title = title, cooldown = cooldown, fill = fill });
            }
        }

        private void BuildRewardPanel(Transform parent)
        {
            RectTransform panel = CreatePanel(parent, "Reward Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 360f), new Color(0.012f, 0.014f, 0.018f, 0.98f));
            rewardPanel = panel.gameObject;
            VerticalLayoutGroup vertical = rewardPanel.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(16, 16, 16, 16);
            vertical.spacing = 14f;

            rewardHeader = CreateLayoutText(panel, "Reward Header", 28, Color.white, TextAnchor.MiddleCenter, 46f);
            RectTransform row = CreatePanel(panel, "Reward Row", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 220f), new Color(0f, 0f, 0f, 0f));
            HorizontalLayoutGroup horizontal = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 12f;
            horizontal.childForceExpandHeight = true;
            horizontal.childForceExpandWidth = true;

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                Button button = CreateButton(row, "Reward " + i, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 210f), "Reward", 20, new Color(0.15f, 0.16f, 0.2f, 0.96f), () => SelectReward(index));
                LayoutElement layoutElement = button.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = 245f;
                layoutElement.preferredHeight = 220f;
                rewardButtons.Add(button);
                rewardButtonLabels.Add(button.GetComponentInChildren<Text>());
            }

            rewardPanel.SetActive(false);
        }

        private void BuildDebugPanel(Transform parent)
        {
            RectTransform panel = CreatePanel(parent, "Debug Tuning Panel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(360f, 604f), new Color(0.035f, 0.04f, 0.045f, 0.88f));
            debugPanel = panel.gameObject;
            VerticalLayoutGroup layout = debugPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 7f;

            CreateLayoutText(panel, "Debug Header", "Debug Tuning (F1)", 22, Color.white, TextAnchor.MiddleLeft, 30f);
            CreateSlider(panel, "Enemy HP", 0.35f, 3f, game.Tuning.enemyHealthMultiplier, v => game.Tuning.enemyHealthMultiplier = v, v => v.ToString("0.00") + "x");
            CreateSlider(panel, "Enemy Damage", 0.2f, 3f, game.Tuning.enemyDamageMultiplier, v => game.Tuning.enemyDamageMultiplier = v, v => v.ToString("0.00") + "x");
            CreateSlider(panel, "Player Damage", 0.4f, 3f, game.Tuning.playerDamageMultiplier, v => game.Tuning.playerDamageMultiplier = v, v => v.ToString("0.00") + "x");
            CreateSlider(panel, "Spawn Rate", 0.35f, 3f, game.Tuning.spawnRateMultiplier, v => game.Tuning.spawnRateMultiplier = v, v => v.ToString("0.00") + "x");
            CreateSlider(panel, "Basic CD", 0.12f, 1.5f, game.Tuning.basicCooldown, v => game.Tuning.basicCooldown = v, v => v.ToString("0.00") + "s");
            CreateSlider(panel, "Dash CD", 0.8f, 8f, game.Tuning.dashCooldown, v => game.Tuning.dashCooldown = v, v => v.ToString("0.0") + "s");
            CreateSlider(panel, "Area CD", 1.5f, 13f, game.Tuning.areaCooldown, v => game.Tuning.areaCooldown = v, v => v.ToString("0.0") + "s");
            CreateSlider(panel, "Ward CD", 2f, 18f, game.Tuning.wardCooldown, v => game.Tuning.wardCooldown = v, v => v.ToString("0.0") + "s");
            CreateSlider(panel, "Bolt Hitbox", 0f, 0.75f, game.Tuning.projectileHitboxPadding, v => game.Tuning.projectileHitboxPadding = v, v => "+" + v.ToString("0.00"));
        }

        private void RefreshStatus()
        {
            bool alive = game.Player.Health.IsAlive;
            waveText.text = game.WaveSpawner.WaveActive
                ? string.Format("Wave {0}  Enemies {1}", game.WaveSpawner.CurrentWave, game.WaveSpawner.AliveCount)
                : string.Format("Wave {0} ready", game.WaveSpawner.CurrentWave + 1);
            healthText.text = string.Format("HP {0:0}/{1:0}", game.Player.Health.CurrentHealth, game.Player.Health.MaxHealth);
            statsText.text = string.Format("Lv {0}  XP {1}/{2}\nATK {3:0.#}  DEF {4:0.#}  SPD {5:0.#}  CDR {6:0.#}%",
                game.Progression.Level,
                game.Progression.Experience,
                game.Progression.NextLevelExperience,
                game.Player.CurrentStats.attack,
                game.Player.CurrentStats.defense,
                game.Player.CurrentStats.moveSpeed,
                game.Player.CurrentStats.cooldownReduction * 100f);
            equipmentText.text = game.Equipment.Summary();

            bool canStart = alive && !game.WaveSpawner.WaveActive && !RewardOpen;
            startButton.gameObject.SetActive(canStart);
            if (guidePanel != null)
            {
                guidePanel.SetActive(canStart && game.WaveSpawner.CurrentWave == 0);
            }

            if (objectiveText != null)
            {
                if (!alive)
                {
                    objectiveText.text = "Run ended. Press R to restart.";
                }
                else if (RewardOpen)
                {
                    objectiveText.text = "Choose one reward card to change the next wave.";
                }
                else if (game.WaveSpawner.WaveActive)
                {
                    objectiveText.text = "Fight enemies. Keep moving, aim with mouse, use Q when enemies group up.";
                }
                else
                {
                    objectiveText.text = "Press START WAVE to begin. Clear enemies, choose reward, repeat.";
                }
            }

            if (startButtonText != null)
            {
                startButtonText.text = game.WaveSpawner.CurrentWave == 0 ? "START WAVE" : "NEXT WAVE";
            }
        }

        private void RefreshSkills()
        {
            IReadOnlyList<SkillRuntime> skills = game.Player.Skills.Skills;
            for (int i = 0; i < skillViews.Count && i < skills.Count; i++)
            {
                SkillRuntime skill = skills[i];
                float fraction = skill.GetCooldownFraction(game.Tuning, game.Player.CurrentStats);
                skillViews[i].fill.fillAmount = fraction;
                skillViews[i].cooldown.text = skill.CooldownRemaining > 0.05f
                    ? string.Format("Cooldown {0:0.0}s", skill.CooldownRemaining)
                    : GetSkillReadyHint(i);
            }
        }

        private string GetSkillReadyHint(int index)
        {
            switch (index)
            {
                case 0:
                    return "Shoot skillshot";
                case 1:
                    return "Dash / evade";
                case 2:
                    return "Area blast";
                case 3:
                    return "Shield + buff";
                default:
                    return "Ready";
            }
        }

        private void SelectReward(int index)
        {
            if (activeRewards == null || index < 0 || index >= activeRewards.Count)
            {
                return;
            }

            activeRewards[index].apply();
            HideRewards();
        }

        private RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            Image image = obj.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            Stretch(rect, Vector2.zero, Vector2.zero);
            Image image = obj.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Text CreateText(Transform parent, string name, string content, int size, Color color, TextAnchor anchor)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            Text text = obj.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            Shadow shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        private Text CreateLayoutText(Transform parent, string name, int size, Color color, TextAnchor anchor, float height)
        {
            return CreateLayoutText(parent, name, name, size, color, anchor, height);
        }

        private Text CreateLayoutText(Transform parent, string name, string content, int size, Color color, TextAnchor anchor, float height)
        {
            Text text = CreateText(parent, name, content, size, color, anchor);
            LayoutElement element = text.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            RectTransform rect = text.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, height);
            return text;
        }

        private Button CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, string label, int fontSize, Color color, Action onClick)
        {
            RectTransform rect = CreatePanel(parent, name, anchorMin, anchorMax, pivot, anchoredPosition, size, color);
            Button button = rect.gameObject.AddComponent<Button>();
            Text text = CreateText(rect, "Label", label, fontSize, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.GetComponent<RectTransform>(), new Vector2(8f, 6f), new Vector2(-8f, -6f));
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(() => onClick());
            return button;
        }

        private void CreateSlider(Transform parent, string label, float min, float max, float value, Action<float> setter, Func<float, string> formatter)
        {
            RectTransform row = CreatePanel(parent, label + " Row", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 48f), new Color(0f, 0f, 0f, 0f));
            LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 48f;

            Text title = CreateText(row, label + " Label", label + ": " + formatter(value), 15, new Color(0.86f, 0.92f, 1f), TextAnchor.UpperLeft);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.52f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(0f, 0f);
            titleRect.offsetMax = new Vector2(0f, 0f);

            GameObject sliderObj = new GameObject(label + " Slider", typeof(RectTransform));
            sliderObj.transform.SetParent(row, false);
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0f);
            sliderRect.anchorMax = new Vector2(1f, 0.46f);
            sliderRect.offsetMin = new Vector2(0f, 4f);
            sliderRect.offsetMax = new Vector2(0f, -2f);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;

            Image background = CreateImage(sliderRect, "Background", new Color(0.12f, 0.13f, 0.15f, 1f));
            Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            RectTransform fillArea = CreatePanel(sliderRect, "Fill Area", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            Stretch(fillArea, new Vector2(5f, 0f), new Vector2(-5f, 0f));
            Image fill = CreateImage(fillArea, "Fill", new Color(0.22f, 0.76f, 0.7f, 1f));

            RectTransform handle = CreatePanel(sliderRect, "Handle", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16f, 24f), new Color(0.95f, 0.95f, 0.88f, 1f));
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.onValueChanged.AddListener(v =>
            {
                setter(v);
                title.text = label + ": " + formatter(v);
            });
        }

        private void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
