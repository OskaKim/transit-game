using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TransitGame
{
    /// <summary>Builds the whole HUD from code so the scene needs no UI prefabs.</summary>
    public class HUDController : MonoBehaviour
    {
        Bootstrap _boot;
        Canvas _canvas;
        Font _font;
        Text _statusText;
        GameObject _gameOverPanel;
        Text _gameOverText;
        string _lastStatus;

        public void Bind(Bootstrap boot)
        {
            _boot = boot;
            if (_canvas == null) BuildUI();
            _gameOverPanel.SetActive(false);
            _boot.Engine.GameOverTriggered += ShowGameOver;
            UpdateStatus();
        }

        void Update()
        {
            if (_boot != null && _boot.Engine != null) UpdateStatus();
        }

        void UpdateStatus()
        {
            var e = _boot.Engine;
            string s = $"Score: {e.Score}    Lines: {e.LinesAvailable}/{e.Config.MaxLines}    Time: {(int)e.ElapsedTime}s"
                + (_boot.TimeScale != 1f ? $"    [x{_boot.TimeScale:0.#}]" : "");
            if (s == _lastStatus) return;
            _lastStatus = s;
            _statusText.text = s;
        }

        void ShowGameOver()
        {
            _gameOverPanel.SetActive(true);
            _gameOverText.text = $"GAME OVER\nScore: {_boot.Engine.Score}";
        }

        void BuildUI()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            var canvasGo = new GameObject("HUD");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            _statusText = MakeText(canvasGo.transform, "Status", 36, TextAnchor.UpperLeft);
            SetAnchors(_statusText.rectTransform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(24, -24), new Vector2(900, 50));

            BuildDebugBar(canvasGo.transform);

            var help = MakeText(canvasGo.transform, "Help", 24, TextAnchor.LowerLeft);
            help.text = "左ドラッグ: 駅をなぞって路線を編集(通った駅を追加 / 戻すと取消 / 経路上の駅に触れると除外)    右クリック: 路線を削除";
            help.color = new Color(0.25f, 0.25f, 0.25f);
            SetAnchors(help.rectTransform, new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(24, 18), new Vector2(1000, 36));

            _gameOverPanel = new GameObject("GameOverPanel");
            _gameOverPanel.transform.SetParent(canvasGo.transform, false);
            var panelImage = _gameOverPanel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.65f);
            var panelRect = _gameOverPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

            _gameOverText = MakeText(_gameOverPanel.transform, "GameOverText", 72, TextAnchor.MiddleCenter);
            _gameOverText.color = Color.white;
            SetAnchors(_gameOverText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 60), new Vector2(900, 220));

            var buttonGo = new GameObject("RestartButton");
            buttonGo.transform.SetParent(_gameOverPanel.transform, false);
            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(0.92f, 0.92f, 0.92f);
            var button = buttonGo.AddComponent<Button>();
            var buttonRect = buttonGo.GetComponent<RectTransform>();
            SetAnchors(buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -120), new Vector2(320, 90));
            var buttonText = MakeText(buttonGo.transform, "Label", 40, TextAnchor.MiddleCenter);
            buttonText.text = "もう一度";
            buttonText.color = new Color(0.1f, 0.1f, 0.1f);
            var labelRect = buttonText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            button.onClick.AddListener(() =>
            {
                _gameOverPanel.SetActive(false);
                _boot.StartGame();
            });
        }

        void BuildDebugBar(Transform parent)
        {
            // Debug/tuning bar (top-right): time scale presets + extra train.
            string[] labels = { "x1", "x2", "x4", "+列車" };
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                var go = new GameObject("Debug_" + labels[i]);
                go.transform.SetParent(parent, false);
                var image = go.AddComponent<Image>();
                image.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);
                var button = go.AddComponent<Button>();
                var rect = go.GetComponent<RectTransform>();
                SetAnchors(rect, new Vector2(1, 1), new Vector2(1, 1),
                    new Vector2(-24 - (labels.Length - 1 - i) * 110, -24), new Vector2(100, 48));
                rect.pivot = new Vector2(1, 1);
                var label = MakeText(go.transform, "Label", 26, TextAnchor.MiddleCenter);
                label.text = labels[i];
                var labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                button.onClick.AddListener(() =>
                {
                    switch (index)
                    {
                        case 0: _boot.TimeScale = 1f; break;
                        case 1: _boot.TimeScale = 2f; break;
                        case 2: _boot.TimeScale = 4f; break;
                        case 3: _boot.AddTrainToSparsestLine(); break;
                    }
                });
            }
        }

        Text MakeText(Transform parent, string name, int size, TextAnchor anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = new Color(0.1f, 0.1f, 0.1f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }
    }
}
