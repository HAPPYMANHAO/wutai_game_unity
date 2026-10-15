using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using WutaiTactics.Map;

namespace WutaiTactics
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI")]
        public string gameTitle = "舞台战旗";
        public Color titleColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        private Canvas canvas;
        private static Font cachedFont;

        private void Start()
        {
            EnsureCamera();
            EnsureCanvas();
            EnsureEventSystem();
            BuildUI();
        }

        private void EnsureCamera()
        {
            if (Camera.main == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                var cam = go.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 5;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 1000f;
                cam.transform.position = new Vector3(0, 0, -10);
            }
        }

        private void EnsureCanvas()
        {
            if (canvas == null)
            {
                var go = new GameObject("MenuCanvas");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
            }
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
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

        private void BuildUI()
        {
            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(canvas.transform);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.7f);
            titleRect.anchorMax = new Vector2(0.5f, 0.7f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(600, 120);

            var titleTxt = titleGO.AddComponent<Text>();
            titleTxt.font = GetFont();
            titleTxt.text = gameTitle;
            titleTxt.fontSize = 64;
            titleTxt.color = titleColor;
            titleTxt.alignment = TextAnchor.MiddleCenter;

            // Buttons
            CreateButton("StartBtn", "开始游戏", new Vector2(0.5f, 0.45f), new Vector2(240, 70), OnStartClicked);
            CreateButton("QuitBtn", "退出", new Vector2(0.5f, 0.30f), new Vector2(240, 70), OnQuitClicked);
        }

        private Button CreateButton(string name, string label, Vector2 anchorPos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(canvas.transform);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorPos;
            rect.anchorMax = anchorPos;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
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
            txt.fontSize = 28;
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

        private void OnStartClicked()
        {
            // 每次开始新游戏都重置地图进度
            var session = GameSession.Instance;
            if (session != null)
            {
                session.MapData = null;
                session.SelectedNodeId = null;
                session.LastBattleWon = false;
                session.ReturningFromBattle = false;
            }

            SceneManager.LoadScene("MapScene", LoadSceneMode.Single);
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
