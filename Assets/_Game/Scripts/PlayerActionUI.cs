using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace WutaiTactics
{
    public class PlayerActionUI : MonoBehaviour
    {
        [Header("UI References")]
        public Canvas canvas;
        public Text phaseText;
        public Text hpText;
        public Text logText;

        private Transform infoPanel;
        private Transform playerBar;
        private Transform actionBar;

        private List<Button> playerButtons = new List<Button>();
        private List<Text> playerButtonTexts = new List<Text>();
        private List<Image> playerButtonImages = new List<Image>();

        private Button attackButton;
        private Button skillButton;
        private Button passButton;

        private Transform targetBar;
        private List<Button> targetButtons = new List<Button>();

        private PlayerCharacter selectedPlayer;
        private bool pendingSkill = false;

        private GameObject resultPanel;
        private Text resultText;
        private Button resultRetryBtn;
        private Button resultMapBtn;

        private static Font cachedFont;

        private void Start()
        {
            EnsureCanvas();
            BuildInfoPanel();
            BuildPlayerBar();
            BuildActionBar();
            BuildTargetBar();
            BuildResultPanel();
            UpdateUI();
        }

        private void Update()
        {
            HandleKeyboardInput();
            HandleWorldClick();
            UpdateUI();
        }

        private void HandleWorldClick()
        {
            if (!targetBar.gameObject.activeSelf) return;
            if (!Input.GetMouseButtonDown(0)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (Camera.main == null) return;

            Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.OverlapPoint(worldPos);
            if (hit == null) return;

            var enemy = hit.GetComponent<EnemyCharacter>();
            if (enemy != null && enemy.IsAlive && selectedPlayer != null && enemy.rowIndex == selectedPlayer.rowIndex)
            {
                OnTargetSelected(enemy);
            }
        }

        private Font GetFont()
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource(typeof(Font), "LegacyRuntime.ttf") as Font;
                if (cachedFont == null)
                    cachedFont = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            }
            return cachedFont;
        }

        private void EnsureCanvas()
        {
            if (canvas == null)
            {
                var canvasGO = new GameObject("BattleCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
            }
        }

        private void BuildInfoPanel()
        {
            var go = new GameObject("InfoPanel");
            go.transform.SetParent(canvas.transform);
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10, -10);
            rect.sizeDelta = new Vector2(420, 140);

            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.5f);

            phaseText = CreateText(go.transform, "PhaseText", new Vector2(10, -10), new Vector2(400, 30), 18);
            hpText = CreateText(go.transform, "HPText", new Vector2(10, -45), new Vector2(400, 30), 18);
            logText = CreateText(go.transform, "LogText", new Vector2(10, -80), new Vector2(400, 30), 18);

            infoPanel = go.transform;
        }

        private Text CreateText(Transform parent, string name, Vector2 anchoredPos, Vector2 size, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            var txt = go.AddComponent<Text>();
            txt.font = GetFont();
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.text = "";
            txt.alignment = TextAnchor.MiddleLeft;

            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            return txt;
        }

        private void BuildPlayerBar()
        {
            var go = new GameObject("PlayerBar");
            go.transform.SetParent(canvas.transform);
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(0, 90);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            playerBar = go.transform;

            int count = 3;
            float padding = 10f;
            float spacing = 10f;
            float btnWidth = (Screen.width - padding * 2 - spacing * (count - 1)) / count;
            float btnHeight = 70f;

            for (int i = 0; i < count; i++)
            {
                int index = i;
                float x = padding + i * (btnWidth + spacing);
                float y = 10f;
                var btn = CreateBarButton(playerBar, $"PlayerBtn_{i}", "", new Vector2(x, y), new Vector2(btnWidth, btnHeight), () => OnPlayerButtonClicked(index));
                playerButtons.Add(btn);
                playerButtonImages.Add(btn.GetComponent<Image>());
                playerButtonTexts.Add(btn.GetComponentInChildren<Text>());
            }
        }

        private void BuildActionBar()
        {
            var go = new GameObject("ActionBar");
            go.transform.SetParent(canvas.transform);
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.offsetMin = new Vector2(0, 90);
            rect.offsetMax = new Vector2(0, 150);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.05f, 0.05f, 0.05f, 0.7f);

            actionBar = go.transform;
            actionBar.gameObject.SetActive(false);

            float btnWidth = 100f;
            float btnHeight = 40f;
            float spacing = 20f;
            float totalWidth = btnWidth * 3 + spacing * 2;
            float startX = (Screen.width - totalWidth) / 2f;

            attackButton = CreateBarButton(actionBar, "AttackBtn", "普攻", new Vector2(startX, 10), new Vector2(btnWidth, btnHeight), OnAttackClicked);
            skillButton = CreateBarButton(actionBar, "SkillBtn", "技能", new Vector2(startX + btnWidth + spacing, 10), new Vector2(btnWidth, btnHeight), OnSkillClicked);
            passButton = CreateBarButton(actionBar, "PassBtn", "跳过", new Vector2(startX + (btnWidth + spacing) * 2, 10), new Vector2(btnWidth, btnHeight), OnPassClicked);
        }

        private void BuildTargetBar()
        {
            var go = new GameObject("TargetBar");
            go.transform.SetParent(canvas.transform);
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.offsetMin = new Vector2(0, 150);
            rect.offsetMax = new Vector2(0, 210);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.05f, 0.05f, 0.05f, 0.7f);

            targetBar = go.transform;
            targetBar.gameObject.SetActive(false);
        }

        private void BuildResultPanel()
        {
            var go = new GameObject("ResultPanel");
            go.transform.SetParent(canvas.transform);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var dim = go.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.65f);

            var box = new GameObject("Box");
            box.transform.SetParent(go.transform);
            var boxRect = box.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.anchoredPosition = Vector2.zero;
            boxRect.sizeDelta = new Vector2(480, 260);
            var boxImg = box.AddComponent<Image>();
            boxImg.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

            var titleGO = new GameObject("ResultText");
            titleGO.transform.SetParent(box.transform);
            resultText = titleGO.AddComponent<Text>();
            resultText.font = GetFont();
            resultText.fontSize = 44;
            resultText.color = Color.white;
            resultText.alignment = TextAnchor.MiddleCenter;
            resultText.text = "";
            var titleRect = titleGO.GetComponent<RectTransform>();
            if (titleRect == null) titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -30);
            titleRect.sizeDelta = new Vector2(-40, 90);

            resultRetryBtn = CreateBarButton(box.transform, "RetryBtn", "重新开始", new Vector2(70, 40), new Vector2(160, 50), OnRetryClicked);
            resultMapBtn = CreateBarButton(box.transform, "MapBtn", "返回地图", new Vector2(250, 40), new Vector2(160, 50), OnReturnToMapClicked);

            go.SetActive(false);
            resultPanel = go;
        }

        private void ShowResultPanel(CombatManager cm)
        {
            resultPanel.SetActive(true);
            resultText.text = cm.BattleWon ? "胜利！" : "失败……";

            var session = WutaiTactics.Map.GameSession.Instance;
            bool hasMap = session != null && session.MapData != null;

            resultMapBtn.gameObject.SetActive(hasMap);

            var retryText = resultRetryBtn.GetComponentInChildren<Text>();
            if (retryText != null)
                retryText.text = hasMap ? "重新挑战" : "重新开始";

            var retryRect = resultRetryBtn.GetComponent<RectTransform>();
            retryRect.anchoredPosition = hasMap ? new Vector2(70, 40) : new Vector2(160, 40);
        }

        private void OnRetryClicked()
        {
            CombatManager.Instance?.RestartBattle();
        }

        private void OnReturnToMapClicked()
        {
            CombatManager.Instance?.ReturnToMap();
        }

        private Button CreateBarButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);

            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(go.transform);
            var txt = txtGO.AddComponent<Text>();
            txt.font = GetFont();
            txt.text = label;
            txt.fontSize = 22;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            var txtRect = txtGO.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            btn.onClick.AddListener(onClick);
            return btn;
        }

        private void OnPlayerButtonClicked(int index)
        {
            var cm = CombatManager.Instance;
            if (cm == null || cm.currentPhase != BattlePhase.Player) return;
            if (index < 0 || index >= cm.players.Count) return;

            var player = cm.players[index];
            if (cm.actedPlayers.Contains(player))
            {
                Debug.Log($"{RoleName(player.role)} 本回合已经行动过");
                return;
            }

            selectedPlayer = player;
            pendingSkill = false;
            cm.SetCurrentPlayer(player);

            ClearTargetButtons();
            targetBar.gameObject.SetActive(false);
            actionBar.gameObject.SetActive(true);

            Debug.Log($"选中玩家角色：{RoleName(player.role)}");
        }

        private void OnAttackClicked()
        {
            pendingSkill = false;
            ShowTargetBar();
        }

        private void OnSkillClicked()
        {
            if (selectedPlayer != null && !selectedPlayer.CanUseSkill)
            {
                Debug.Log("技能还在冷却中");
                return;
            }
            pendingSkill = true;
            ShowTargetBar();
        }

        private void ShowTargetBar()
        {
            var cm = CombatManager.Instance;
            if (cm == null || selectedPlayer == null) return;

            actionBar.gameObject.SetActive(false);
            targetBar.gameObject.SetActive(true);
            ClearTargetButtons();

            var aliveEnemies = new List<EnemyCharacter>();
            foreach (var enemy in cm.enemies)
            {
                if (enemy != null && enemy.IsAlive && enemy.rowIndex == selectedPlayer.rowIndex)
                    aliveEnemies.Add(enemy);
            }

            if (aliveEnemies.Count == 0)
            {
                Debug.Log($"本行（第 {selectedPlayer.rowIndex + 1} 行）没有可指定的目标");
                HideTargetBar();
                return;
            }

            float padding = 10f;
            float spacing = 10f;
            float btnWidth = Mathf.Min(120f, (Screen.width - padding * 2 - spacing * (aliveEnemies.Count - 1)) / aliveEnemies.Count);
            float btnHeight = 40f;
            float totalWidth = btnWidth * aliveEnemies.Count + spacing * (aliveEnemies.Count - 1);
            float startX = (Screen.width - totalWidth) / 2f;

            for (int i = 0; i < aliveEnemies.Count; i++)
            {
                var enemy = aliveEnemies[i];
                float x = startX + i * (btnWidth + spacing);
                string label = $"{RoleName(enemy.role)}\n({enemy.columnIndex},{enemy.rowIndex})\n{enemy.currentHealth}/{enemy.maxHealth}";
                var btn = CreateBarButton(targetBar, $"TargetBtn_{i}", label, new Vector2(x, 10), new Vector2(btnWidth, btnHeight), () => OnTargetSelected(enemy));
                targetButtons.Add(btn);
            }

            // Cancel button
            var cancelBtn = CreateBarButton(targetBar, "CancelTargetBtn", "取消", new Vector2(Screen.width - 90 - padding, 10), new Vector2(80, btnHeight), HideTargetBar);
            targetButtons.Add(cancelBtn);
        }

        private void HideTargetBar()
        {
            ClearTargetButtons();
            targetBar.gameObject.SetActive(false);
            if (selectedPlayer != null)
                actionBar.gameObject.SetActive(true);
        }

        private void ClearTargetButtons()
        {
            foreach (var btn in targetButtons)
            {
                if (btn != null && btn.gameObject != null)
                    Destroy(btn.gameObject);
            }
            targetButtons.Clear();
        }

        private void OnTargetSelected(EnemyCharacter target)
        {
            var cm = CombatManager.Instance;
            if (cm == null || selectedPlayer == null || target == null) return;

            bool success = pendingSkill
                ? cm.PlayerUseSkill(selectedPlayer, target)
                : cm.PlayerAttack(selectedPlayer, target);

            ClearTargetButtons();
            targetBar.gameObject.SetActive(false);
            actionBar.gameObject.SetActive(false);
            selectedPlayer = null;
            pendingSkill = false;

            if (success)
                cm.OnPlayerActionSubmitted();
        }

        private void OnPassClicked()
        {
            var cm = CombatManager.Instance;
            if (cm == null || selectedPlayer == null) return;

            selectedPlayer = null;
            pendingSkill = false;
            actionBar.gameObject.SetActive(false);
            targetBar.gameObject.SetActive(false);
            ClearTargetButtons();
            cm.OnPlayerActionSubmitted();
        }

        private EnemyCharacter FindNearestEnemyInRow(int row)
        {
            var cm = CombatManager.Instance;
            EnemyCharacter nearest = null;
            int nearestCol = int.MaxValue;

            foreach (var enemy in cm.enemies)
            {
                if (!enemy.IsAlive) continue;
                if (enemy.rowIndex != row) continue;
                if (enemy.columnIndex < nearestCol)
                {
                    nearestCol = enemy.columnIndex;
                    nearest = enemy;
                }
            }
            return nearest;
        }

        private void HandleKeyboardInput()
        {
            var cm = CombatManager.Instance;
            if (cm == null || cm.currentPhase != BattlePhase.Player) return;

            var player = cm.GetCurrentPlayer();
            if (player == null) return;

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                var target = FindNearestEnemyInRow(player.rowIndex);
                if (target != null && cm.PlayerAttack(player, target))
                    cm.OnPlayerActionSubmitted();
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                var target = FindNearestEnemyInRow(player.rowIndex);
                if (target != null && player.CanUseSkill && cm.PlayerUseSkill(player, target))
                    cm.OnPlayerActionSubmitted();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                cm.OnPlayerActionSubmitted();
            }
        }

        private void UpdateUI()
        {
            var cm = CombatManager.Instance;
            if (cm == null) return;

            phaseText.text = $"阶段: {PhaseName(cm.currentPhase)} | 回合: {cm.roundNumber}";
            hpText.text = $"共用血条: {cm.sharedCurrentHealth}/{cm.sharedMaxHealth}";

            var current = cm.GetCurrentPlayer();
            if (targetBar.gameObject.activeSelf)
            {
                logText.text = $"{RoleName(selectedPlayer.role)}: 请选择目标 (技能CD: {selectedPlayer.currentSkillCooldown})";
            }
            else if (selectedPlayer != null)
            {
                logText.text = $"已选中: {RoleName(selectedPlayer.role)} (技能CD: {selectedPlayer.currentSkillCooldown})";
            }
            else if (current != null)
            {
                logText.text = $"当前: {RoleName(current.role)} (技能CD: {current.currentSkillCooldown})";
            }
            else
            {
                logText.text = "等待敌方阶段...";
            }

            // Update player buttons
            for (int i = 0; i < cm.players.Count && i < playerButtons.Count; i++)
            {
                var p = cm.players[i];
                bool acted = cm.actedPlayers.Contains(p);
                bool isSelected = selectedPlayer == p;

                playerButtonTexts[i].text = $"{RoleName(p.role)}\n{(acted ? "已行动" : $"CD:{p.currentSkillCooldown}")}";

                var colors = playerButtonImages[i].color;
                if (acted)
                    playerButtonImages[i].color = new Color(0.15f, 0.15f, 0.15f, 1f);
                else if (isSelected)
                    playerButtonImages[i].color = new Color(0.2f, 0.6f, 0.9f, 1f);
                else
                    playerButtonImages[i].color = new Color(0.25f, 0.25f, 0.25f, 1f);

                playerButtons[i].interactable = !acted && cm.currentPhase == BattlePhase.Player;
            }

            // Action bar availability
            if (actionBar.gameObject.activeSelf && selectedPlayer != null)
            {
                skillButton.interactable = selectedPlayer.CanUseSkill;
            }

            // 战斗结算面板
            if (cm.BattleEnded && !resultPanel.activeSelf)
                ShowResultPanel(cm);
        }

        private void OnGUI()
        {
            var cm = CombatManager.Instance;
            if (cm == null || cm.BattleEnded || Camera.main == null) return;

            var cam = Camera.main;
            var style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            style.font = GetFont();

            foreach (var enemy in cm.enemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;

                Vector3 worldPos = enemy.transform.position + Vector3.up * 0.7f;
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                if (screenPos.z < 0) continue;

                float x = screenPos.x;
                float y = Screen.height - screenPos.y;
                Vector2 size = new Vector2(80, 22);
                Rect rect = new Rect(x - size.x / 2, y - size.y / 2, size.x, size.y);

                GUI.Box(rect, "");
                GUI.Label(rect, $"{enemy.currentHealth}/{enemy.maxHealth}", style);
            }
        }

        private string RoleName(CombatRole role)
        {
            return role switch
            {
                CombatRole.Melee => "近战",
                CombatRole.Polearm => "长柄",
                CombatRole.Arrow => "矢石",
                _ => role.ToString()
            };
        }

        private string PhaseName(BattlePhase phase)
        {
            return phase switch
            {
                BattlePhase.Player => "玩家阶段",
                BattlePhase.Enemy => "敌人阶段",
                BattlePhase.Resolution => "结算",
                _ => phase.ToString()
            };
        }
    }
}
