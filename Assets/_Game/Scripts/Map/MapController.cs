using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace WutaiTactics.Map
{
    public class MapController : MonoBehaviour
    {
        public static MapController Instance { get; private set; }

        [Header("Map Settings")]
        public int rows = 5;
        public int nodesPerRow = 2;
        public float horizontalSpacing = 160f;
        public float verticalSpacing = 140f;

        [Header("Visual")]
        public Color startColor = new Color(0.2f, 0.8f, 0.2f);
        public Color battleColor = new Color(0.8f, 0.3f, 0.3f);
        public Color eliteColor = new Color(0.9f, 0.4f, 0.1f);
        public Color restColor = new Color(0.2f, 0.5f, 0.9f);
        public Color eventColor = new Color(0.7f, 0.3f, 0.8f);
        public Color bossColor = new Color(0.9f, 0.1f, 0.1f);
        public Color lineColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        public Color unreachableColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        private MapData mapData;
        private Canvas canvas;
        private Transform mapRoot;
        private List<GameObject> drawnLines = new List<GameObject>();
        private List<GameObject> drawnNodes = new List<GameObject>();

        private static Font cachedFont;

        private void Awake()
        {
            Instance = this;
            EnsureGameSession();

            if (GameSession.Instance.MapData != null)
            {
                mapData = GameSession.Instance.MapData;
            }
            else
            {
                mapData = GenerateMap();
                GameSession.Instance.MapData = mapData;
            }
        }

        private void EnsureGameSession()
        {
            if (GameSession.Instance == null)
            {
                var go = new GameObject("GameSession");
                go.AddComponent<GameSession>();
            }
        }

        private void Start()
        {
            EnsureCamera();
            EnsureCanvas();
            EnsureEventSystem();
            RenderMap();
            BuildReturnButton();

            if (GameSession.Instance != null && GameSession.Instance.ReturningFromBattle)
            {
                ProcessBattleResult();
                GameSession.Instance.ReturningFromBattle = false;
            }
        }

        private void BuildReturnButton()
        {
            var go = new GameObject("ReturnToMenuBtn");
            go.transform.SetParent(canvas.transform);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-10, -10);
            rect.sizeDelta = new Vector2(140, 40);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single));

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(go.transform);
            var txt = txtGO.AddComponent<Text>();
            txt.font = GetFont();
            txt.text = "返回主菜单";
            txt.fontSize = 18;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            var txtRect = txtGO.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
        }

        private void EnsureCamera()
        {
            if (Camera.main == null)
            {
                var camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                var cam = camGO.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 5;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 1000f;
                cam.transform.position = new Vector3(0, 0, -10);
            }
        }

        private MapData GenerateMap()
        {
            var data = new MapData();
            int idCounter = 0;

            // Start node
            var start = new MapNode($"node_{idCounter++}", MapNodeType.Start, 0, 0);
            start.reachable = true;
            data.nodes.Add(start);
            data.currentNodeId = start.id;
            data.currentRow = 0;

            // Intermediate rows
            for (int r = 1; r <= rows; r++)
            {
                for (int c = 0; c < nodesPerRow; c++)
                {
                    MapNodeType type = MapNodeType.Battle;
                    float roll = Random.value;
                    if (roll > 0.85f) type = MapNodeType.Elite;
                    else if (roll > 0.7f) type = MapNodeType.Rest;
                    else if (roll > 0.55f) type = MapNodeType.Event;

                    var node = new MapNode($"node_{idCounter++}", type, r, c);
                    data.nodes.Add(node);
                }
            }

            // Boss node
            var boss = new MapNode($"node_{idCounter++}", MapNodeType.Boss, rows + 1, 0);
            data.nodes.Add(boss);

            // Connect rows
            var prevRowNodes = new List<MapNode> { start };
            for (int r = 1; r <= rows + 1; r++)
            {
                var rowNodes = data.GetNodesInRow(r);
                foreach (var prev in prevRowNodes)
                {
                    if (rowNodes.Count == 1)
                    {
                        prev.nextNodeIds.Add(rowNodes[0].id);
                    }
                    else
                    {
                        int connections = Mathf.Max(1, Random.Range(1, rowNodes.Count + 1));
                        var targets = new List<MapNode>(rowNodes);
                        Shuffle(targets);
                        for (int i = 0; i < connections; i++)
                        {
                            if (!prev.nextNodeIds.Contains(targets[i].id))
                                prev.nextNodeIds.Add(targets[i].id);
                        }
                    }
                }
                prevRowNodes = rowNodes;
            }

            UpdateReachability(data);
            return data;
        }

        private void UpdateReachability(MapData data)
        {
            var current = data.GetNode(data.currentNodeId);
            if (current == null) return;

            foreach (var node in data.nodes)
            {
                node.reachable = false;
            }

            foreach (var nextId in current.nextNodeIds)
            {
                var next = data.GetNode(nextId);
                if (next != null && !next.visited)
                    next.reachable = true;
            }
        }

        private void ProcessBattleResult()
        {
            if (GameSession.Instance == null) return;

            if (GameSession.Instance.LastBattleWon)
            {
                var node = mapData.GetNode(GameSession.Instance.SelectedNodeId);
                if (node != null)
                {
                    node.visited = true;
                    mapData.currentNodeId = node.id;
                    mapData.currentRow = node.row;
                }
                UpdateReachability(mapData);
                RenderMap();
            }
            else
            {
                Debug.Log("战斗失败——回到地图，可重新选择路径。");
            }
        }

        private void EnsureCanvas()
        {
            if (canvas == null)
            {
                var canvasGO = new GameObject("MapCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            if (mapRoot == null)
            {
                var rootGO = new GameObject("MapRoot");
                rootGO.transform.SetParent(canvas.transform);
                var rootRect = rootGO.AddComponent<RectTransform>();
                rootRect.anchorMin = new Vector2(0.5f, 0);
                rootRect.anchorMax = new Vector2(0.5f, 0);
                rootRect.pivot = new Vector2(0.5f, 0);
                rootRect.anchoredPosition = new Vector2(0, 80);
                mapRoot = rootGO.transform;
            }
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
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

        private void RenderMap()
        {
            ClearMap();

            var positions = new Dictionary<string, Vector2>();
            int totalRows = rows + 2; // start + intermediate + boss

            foreach (var node in mapData.nodes)
            {
                Vector2 pos = GetNodePosition(node, totalRows);
                positions[node.id] = pos;

                var btnGO = CreateNodeButton(node, pos);
                drawnNodes.Add(btnGO);
            }

            foreach (var node in mapData.nodes)
            {
                if (!positions.ContainsKey(node.id)) continue;
                Vector2 start = positions[node.id];
                foreach (var nextId in node.nextNodeIds)
                {
                    if (!positions.ContainsKey(nextId)) continue;
                    Vector2 end = positions[nextId];
                    DrawConnection(start, end);
                }
            }
        }

        private Vector2 GetNodePosition(MapNode node, int totalRows)
        {
            float y = node.row * verticalSpacing;
            int countInRow = mapData.GetNodesInRow(node.row).Count;
            float startX = -(countInRow - 1) * horizontalSpacing / 2f;
            float x = startX + node.col * horizontalSpacing;
            return new Vector2(x, y);
        }

        private GameObject CreateNodeButton(MapNode node, Vector2 pos)
        {
            var go = new GameObject($"Node_{node.id}");
            go.transform.SetParent(mapRoot);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(80, 80);

            var img = go.AddComponent<Image>();
            img.color = GetNodeColor(node);
            img.sprite = SpriteUtil.GetWhiteSprite();

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = node.reachable && !node.visited;

            string label = GetNodeLabel(node);
            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(go.transform);
            var txt = txtGO.AddComponent<Text>();
            txt.font = GetFont();
            txt.text = label;
            txt.fontSize = 16;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            var txtRect = txtGO.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            btn.onClick.AddListener(() => OnNodeClicked(node));
            return go;
        }

        private void DrawConnection(Vector2 start, Vector2 end)
        {
            var go = new GameObject("Connection");
            go.transform.SetParent(mapRoot);

            Vector2 dir = end - start;
            float length = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (start + end) / 2f;
            rect.sizeDelta = new Vector2(length, 4);
            rect.rotation = Quaternion.Euler(0, 0, angle);

            var img = go.AddComponent<Image>();
            img.color = lineColor;
            img.sprite = SpriteUtil.GetWhiteSprite();

            drawnLines.Add(go);
        }

        private void ClearMap()
        {
            foreach (var go in drawnNodes)
            {
                if (go != null) Destroy(go);
            }
            drawnNodes.Clear();

            foreach (var go in drawnLines)
            {
                if (go != null) Destroy(go);
            }
            drawnLines.Clear();
        }

        private void OnNodeClicked(MapNode node)
        {
            if (!node.reachable || node.visited) return;

            if (GameSession.Instance != null)
            {
                GameSession.Instance.SelectedNodeId = node.id;
            }

            switch (node.nodeType)
            {
                case MapNodeType.Battle:
                case MapNodeType.Elite:
                case MapNodeType.Boss:
                    LoadBattle();
                    break;
                case MapNodeType.Rest:
                    Debug.Log("进入休整节点（MVP 暂不实现）");
                    node.visited = true;
                    mapData.currentNodeId = node.id;
                    mapData.currentRow = node.row;
                    UpdateReachability(mapData);
                    RenderMap();
                    break;
                case MapNodeType.Event:
                    Debug.Log("进入事件节点（MVP 暂不实现）");
                    node.visited = true;
                    mapData.currentNodeId = node.id;
                    mapData.currentRow = node.row;
                    UpdateReachability(mapData);
                    RenderMap();
                    break;
            }
        }

        private void LoadBattle()
        {
            if (GameSession.Instance != null)
                GameSession.Instance.ReturningFromBattle = false;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
        }

        private Color GetNodeColor(MapNode node)
        {
            if (!node.reachable && !node.visited && node.nodeType != MapNodeType.Start)
                return unreachableColor;

            return node.nodeType switch
            {
                MapNodeType.Start => startColor,
                MapNodeType.Battle => battleColor,
                MapNodeType.Elite => eliteColor,
                MapNodeType.Rest => restColor,
                MapNodeType.Event => eventColor,
                MapNodeType.Boss => bossColor,
                _ => Color.gray
            };
        }

        private string GetNodeLabel(MapNode node)
        {
            return node.nodeType switch
            {
                MapNodeType.Start => "起点",
                MapNodeType.Battle => "战斗",
                MapNodeType.Elite => "精英",
                MapNodeType.Rest => "休整",
                MapNodeType.Event => "事件",
                MapNodeType.Boss => "Boss",
                _ => "?"
            };
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
