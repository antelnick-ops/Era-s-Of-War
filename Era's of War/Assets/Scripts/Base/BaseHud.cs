using System;
using System.Collections.Generic;
using ErasOfWar.Simulation.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ErasOfWar.Unity.Base
{
    /// <summary>
    /// Portrait HUD built in code (no prefabs): info bar on top, toast, context bar and a scrolling
    /// build menu at the bottom. Raises events; the controller decides what they do.
    /// </summary>
    public sealed class BaseHud : MonoBehaviour
    {
        public const string ActMove = "move", ActUpgrade = "upgrade", ActRemove = "remove", ActClose = "close",
            ActPlace = "place", ActCancel = "cancel", ActDone = "done", ActReset = "reset";

        private static readonly Color BarColor = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color ButtonColor = new Color(0.22f, 0.25f, 0.22f, 1f);
        private static readonly Color ButtonConfirmColor = new Color(0.75f, 0.20f, 0.18f, 1f);
        private static readonly Color ItemColor = new Color(0.16f, 0.18f, 0.16f, 1f);

        public event Action<BuildingDef> BuildItemTapped;
        public event Action<string> ActionTapped;

        private RectTransform _safeRoot;
        private Text _info;
        private Text _toast;
        private GameObject _toastPanel;
        private float _toastUntil;
        private Text _contextTitle;
        private RectTransform _contextButtons;
        private string _contextKey;
        private Button _resetButton;
        private readonly List<RaycastResult> _hits = new List<RaycastResult>();
        private readonly Dictionary<string, BuildItem> _items = new Dictionary<string, BuildItem>();
        private string _armedConfirm;
        private float _armedUntil;
        private Rect _lastSafeArea;

        private sealed class BuildItem
        {
            public Button Button;
            public Image Background;
            public Image Swatch;
            public Text Status;
            public Text Name;
        }

        // ------------------------------------------------------------------ setup

        public void Build(BuildingCatalog catalog)
        {
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.AddComponent<InputSystemUIInputModule>();
            }

            var canvasGo = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            _safeRoot = NewRect("Safe Area", canvasGo.transform);
            Stretch(_safeRoot, Vector2.zero, Vector2.one);
            ApplySafeArea();

            // Top bar
            var top = Panel("Top Bar", _safeRoot, BarColor);
            Stretch(top, new Vector2(0, 1 - BaseCameraRig.TopHudFraction), Vector2.one);
            _info = Label("Info", top, "", 34, TextAnchor.MiddleLeft);
            Stretch(_info.rectTransform, new Vector2(0.03f, 0), new Vector2(0.74f, 1));
            _resetButton = MakeButton(top, "Reset base", () => Tapped(ActReset, "!"));
            Stretch((RectTransform)_resetButton.transform, new Vector2(0.76f, 0.15f), new Vector2(0.97f, 0.85f));

            // Toast
            var toastRect = Panel("Toast", _safeRoot, new Color(0, 0, 0, 0.8f));
            Stretch(toastRect, new Vector2(0.04f, 1 - BaseCameraRig.TopHudFraction - 0.075f),
                new Vector2(0.96f, 1 - BaseCameraRig.TopHudFraction - 0.01f));
            _toast = Label("Text", toastRect, "", 32, TextAnchor.MiddleCenter);
            Stretch(_toast.rectTransform, new Vector2(0.03f, 0), new Vector2(0.97f, 1));
            _toastPanel = toastRect.gameObject;
            _toastPanel.SetActive(false);
            toastRect.GetComponent<Image>().raycastTarget = false;
            _toast.raycastTarget = false;

            // Bottom area: context bar + build menu
            var bottom = Panel("Bottom", _safeRoot, BarColor);
            Stretch(bottom, Vector2.zero, new Vector2(1, BaseCameraRig.BottomHudFraction));

            var context = NewRect("Context", bottom);
            Stretch(context, new Vector2(0, 0.62f), Vector2.one);
            _contextTitle = Label("Title", context, "", 32, TextAnchor.MiddleCenter);
            Stretch(_contextTitle.rectTransform, new Vector2(0.02f, 0.55f), new Vector2(0.98f, 1));
            _contextButtons = NewRect("Buttons", context);
            Stretch(_contextButtons, new Vector2(0.02f, 0.06f), new Vector2(0.98f, 0.55f));
            var row = _contextButtons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 16;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = true;

            BuildMenu(bottom, catalog);
        }

        private void BuildMenu(RectTransform parent, BuildingCatalog catalog)
        {
            var scrollRect = NewRect("Build Menu", parent);
            Stretch(scrollRect, Vector2.zero, new Vector2(1, 0.62f));
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;

            var viewport = NewRect("Viewport", scrollRect);
            Stretch(viewport, Vector2.zero, Vector2.one);
            viewport.gameObject.AddComponent<RectMask2D>();
            var vpImage = viewport.gameObject.AddComponent<Image>();
            vpImage.color = new Color(0, 0, 0, 0.01f); // catches drags between items
            scroll.viewport = viewport;

            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0, 0);
            content.anchorMax = new Vector2(0, 1);
            content.pivot = new Vector2(0, 0.5f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.padding = new RectOffset(16, 16, 14, 18);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            foreach (var def in catalog.Buildings)
            {
                var d = def;
                var button = MakeButton(content, "", () => BuildItemTapped?.Invoke(d));
                var bg = button.GetComponent<Image>();
                bg.color = ItemColor;
                button.gameObject.AddComponent<LayoutElement>().preferredWidth = 220;
                Destroy(button.GetComponentInChildren<Text>().gameObject);

                var swatch = new GameObject("Swatch", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                swatch.transform.SetParent(button.transform, false);
                swatch.color = PlaceholderArt.ColorFor(def);
                swatch.raycastTarget = false;
                Stretch(swatch.rectTransform, new Vector2(0.18f, 0.47f), new Vector2(0.82f, 0.94f));
                var shortName = Label("Short", swatch.rectTransform, def.IsWall ? "WALL" : PlaceholderArt.ShortName(def), 40, TextAnchor.MiddleCenter);
                shortName.color = PlaceholderArt.TextOn(swatch.color);
                Stretch(shortName.rectTransform, Vector2.zero, Vector2.one);

                var name = Label("Name", (RectTransform)button.transform, $"{def.Name} ({def.Width}x{def.Height})", 24, TextAnchor.MiddleCenter);
                Stretch(name.rectTransform, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.46f));
                var status = Label("Status", (RectTransform)button.transform, "", 26, TextAnchor.MiddleCenter);
                Stretch(status.rectTransform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.2f));

                _items[def.Id] = new BuildItem { Button = button, Background = bg, Swatch = swatch, Status = status, Name = name };
            }
        }

        // ------------------------------------------------------------------ updates

        public void SetInfo(string text) => _info.text = text;

        /// <summary>Show a title and buttons. Buttons whose id starts with "!" need a second tap to confirm.</summary>
        public void SetContext(string title, params (string id, string label)[] buttons)
        {
            _contextTitle.text = title;
            var key = string.Join("|", Array.ConvertAll(buttons, b => b.id + b.label));
            if (key == _contextKey) return;
            _contextKey = key;
            _armedConfirm = null;

            for (var i = _contextButtons.childCount - 1; i >= 0; i--)
                Destroy(_contextButtons.GetChild(i).gameObject);
            foreach (var (id, label) in buttons)
            {
                var needsConfirm = id.StartsWith("!", StringComparison.Ordinal);
                var act = needsConfirm ? id.Substring(1) : id;
                var b = MakeButton(_contextButtons, label, null);
                b.name = act;
                b.onClick.AddListener(() => Tapped(act, needsConfirm ? "!" : "", b, label));
            }
        }

        public void RefreshBuildMenu(BaseState state)
        {
            foreach (var def in state.Catalog.Buildings)
            {
                if (!_items.TryGetValue(def.Id, out var item)) continue;
                var max = def.MaxCountAt(state.CcLevel);
                var count = state.CountOf(def.Id);
                bool available;
                if (max <= 0)
                {
                    item.Status.text = def.UnlockCc > 0 ? $"Unlocks CC {def.UnlockCc}" : "Locked";
                    available = false;
                }
                else
                {
                    item.Status.text = $"{count}/{max}";
                    available = count < max;
                }
                var a = available ? 1f : 0.35f;
                item.Swatch.color = WithAlpha(PlaceholderArt.ColorFor(def), a);
                item.Name.color = WithAlpha(Color.white, available ? 1f : 0.5f);
                item.Status.color = available ? new Color(0.75f, 1f, 0.75f) : new Color(1f, 0.7f, 0.6f);
            }
        }

        public void Toast(string message, bool error = false)
        {
            if (string.IsNullOrEmpty(message)) return;
            _toast.text = message;
            _toast.color = error ? new Color(1f, 0.6f, 0.55f) : Color.white;
            _toastPanel.SetActive(true);
            _toastUntil = Time.unscaledTime + (error ? 3f : 2f);
        }

        /// <summary>True if a screen point is over any HUD element (so the world should ignore it).</summary>
        public bool IsOverUi(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            _hits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = screenPos }, _hits);
            return _hits.Count > 0;
        }

        private void Update()
        {
            if (_toastPanel != null && _toastPanel.activeSelf && Time.unscaledTime > _toastUntil)
                _toastPanel.SetActive(false);

            if (_armedConfirm != null && Time.unscaledTime > _armedUntil)
                Disarm();

            if (Screen.safeArea != _lastSafeArea) ApplySafeArea();
        }

        // ------------------------------------------------------------------ confirm-twice buttons

        private void Tapped(string act, string confirmFlag, Button source = null, string label = null)
        {
            if (confirmFlag == "!")
            {
                if (_armedConfirm != act)
                {
                    Disarm();
                    _armedConfirm = act;
                    _armedUntil = Time.unscaledTime + 3f;
                    var b = source != null ? source : (act == ActReset ? _resetButton : null);
                    if (b != null)
                    {
                        b.GetComponent<Image>().color = ButtonConfirmColor;
                        var t = b.GetComponentInChildren<Text>();
                        if (t != null) t.text = "Tap again";
                    }
                    return;
                }
                Disarm();
            }
            ActionTapped?.Invoke(act);
        }

        private void Disarm()
        {
            _armedConfirm = null;
            if (_resetButton != null)
            {
                _resetButton.GetComponent<Image>().color = ButtonColor;
                _resetButton.GetComponentInChildren<Text>().text = "Reset base";
            }
            // Context buttons get rebuilt with their labels on the next SetContext; restore colors now.
            for (var i = 0; i < _contextButtons.childCount; i++)
            {
                var img = _contextButtons.GetChild(i).GetComponent<Image>();
                if (img != null) img.color = ButtonColor;
            }
            _contextKey = null; // force label rebuild
        }

        // ------------------------------------------------------------------ uGUI helpers

        private void ApplySafeArea()
        {
            if (_safeRoot == null) return;
            var safe = Screen.safeArea;
            _lastSafeArea = safe;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            _safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            _safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            _safeRoot.offsetMin = _safeRoot.offsetMax = Vector2.zero;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static RectTransform Panel(string name, Transform parent, Color color)
        {
            var r = NewRect(name, parent);
            r.gameObject.AddComponent<Image>().color = color;
            return r;
        }

        private static void Stretch(RectTransform r, Vector2 min, Vector2 max)
        {
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        private static Text Label(string name, RectTransform parent, string text, int size, TextAnchor align)
        {
            var t = NewRect(name, parent).gameObject.AddComponent<Text>();
            t.font = PlaceholderArt.Font;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 14;
            t.resizeTextMaxSize = size;
            t.raycastTarget = false;
            return t;
        }

        private static Button MakeButton(Transform parent, string label, Action onClick)
        {
            var r = NewRect("Button", parent);
            var img = r.gameObject.AddComponent<Image>();
            img.color = ButtonColor;
            var b = r.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            var t = Label("Text", r, label, 34, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform, new Vector2(0.04f, 0), new Vector2(0.96f, 1));
            return b;
        }

        private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
