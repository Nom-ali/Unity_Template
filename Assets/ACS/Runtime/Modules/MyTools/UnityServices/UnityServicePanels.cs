using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ACS.UnityServices
{
    // Dedicated service canvas; never shares dismiss listeners with the game's general popup.
    public sealed class UnityServicePanels : MonoBehaviour
    {
        private GameObject shade, card;
        private Text title, message;
        private Button primary, secondary, privacy;
        private Text secondaryLabel;
        private float dismissAt;
        private Action dismiss;
        private float nextCanvasScan;
        private readonly Dictionary<CanvasGroup, bool> disabledGroups = new Dictionary<CanvasGroup, bool>();
        private readonly List<CanvasGroup> ownedGroups = new List<CanvasGroup>();
        public object DialogOwner { get; private set; }
        public bool Visible => shade && shade.activeSelf;
        public bool NormalVisible { get; private set; }
        public void Build()
        {
            if (shade) return;
            var canvasObject = new GameObject("Unity Service Panels", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32760;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0.5f;
            shade = new GameObject("Service Input Blocker", typeof(RectTransform), typeof(Image));
            shade.transform.SetParent(canvasObject.transform, false);
            Stretch(shade.GetComponent<RectTransform>());
            shade.GetComponent<Image>().color = new Color(0.025f, 0.04f, 0.08f, 0.95f);
            card = new GameObject("Dialog", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(shade.transform, false);
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.08f, .3f); rect.anchorMax = new Vector2(.92f, .7f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            card.GetComponent<Image>().color = new Color(.1f, .14f, .22f);
            title = Label(card.transform, "Title", "", new Vector2(.08f, .73f), new Vector2(.92f, .93f), 46);
            message = Label(card.transform, "Message", "", new Vector2(.08f, .43f), new Vector2(.92f, .72f), 30);
            primary = Button(card.transform, "Update", new Vector2(.08f, .27f), new Vector2(.92f, .40f));
            secondary = Button(card.transform, "Close", new Vector2(.08f, .11f), new Vector2(.92f, .24f));
            secondaryLabel = secondary.GetComponentInChildren<Text>();
            privacy = Button(card.transform, "Privacy policy", new Vector2(.08f, .01f), new Vector2(.92f, .09f));
            shade.SetActive(false);
        }
        public void ShowChecking()
        {
            Prepare("Checking for updates", "Please wait…");
            primary.gameObject.SetActive(false); secondary.gameObject.SetActive(false);
        }
        public void ShowUpdate(UpdatePolicy policy, bool required, Action update, Action close)
        {
            Prepare(required ? "Update required" : "Update available",
                required ? "Please update to the latest version to continue playing." : "A newer version is available. Would you like to update?");
            primary.GetComponentInChildren<Text>().text = "Update";
            primary.onClick.AddListener(() => update());
            secondary.gameObject.SetActive(!required);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(primary.gameObject);
            NormalVisible = !required;
            if (!required)
            {
                dismissAt = Time.realtimeSinceStartup + 5f; dismiss = close;
                secondary.interactable = false; secondaryLabel.text = "Close (5)";
                secondary.onClick.AddListener(() => { if (Time.realtimeSinceStartup >= dismissAt) dismiss?.Invoke(); });
            }
        }
        public void ShowConsent(Action<bool> choice, string privacyUrl)
        {
            Prepare("Analytics privacy", "Allow usage data to be sent to Unity Analytics to help improve the game? You can change this in Settings.");
            primary.GetComponentInChildren<Text>().text = "Accept";
            primary.onClick.AddListener(() => choice(true));
            secondaryLabel.text = "Decline";
            secondary.onClick.AddListener(() => choice(false));
            privacy.gameObject.SetActive(!string.IsNullOrEmpty(privacyUrl));
            privacy.onClick.AddListener(() => Application.OpenURL(privacyUrl));
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(secondary.gameObject);
        }
        public void ShowDialog(object dialogOwner, string heading, string body,
            string firstLabel, Action firstAction, string secondLabel, Action secondAction,
            string thirdLabel, Action thirdAction)
        {
            Prepare(heading, body); DialogOwner = dialogOwner;
            Configure(primary, firstLabel, firstAction);
            Configure(secondary, secondLabel, secondAction);
            Configure(privacy, thirdLabel, thirdAction);
            if (EventSystem.current)
                EventSystem.current.SetSelectedGameObject(secondary.gameObject.activeSelf ? secondary.gameObject :
                    primary.gameObject.activeSelf ? primary.gameObject : null);
        }
        private static void Configure(Button button, string label, Action action)
        {
            button.gameObject.SetActive(action != null);
            button.GetComponentInChildren<Text>(true).text = label ?? "";
            if (action != null) button.onClick.AddListener(() => action());
        }
        private void Prepare(string heading, string body)
        {
            Build(); DialogOwner = null; NormalVisible = false; dismiss = null;
            if (!EventSystem.current)
            {
                var events = new GameObject("Service EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetParent(transform, false);
            }
            foreach (var b in new[] { primary, secondary, privacy }) { b.onClick.RemoveAllListeners(); b.interactable = true; b.gameObject.SetActive(true); }
            privacy.gameObject.SetActive(false); title.text = heading; message.text = body;
            shade.SetActive(true);
            BlockOtherCanvases();
            // Do not leave a selected gameplay button reachable through keyboard/controller submit.
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }
        private void Update()
        {
            if (Visible && Time.realtimeSinceStartup >= nextCanvasScan)
            { nextCanvasScan = Time.realtimeSinceStartup + .25f; BlockOtherCanvases(); }
            if (!NormalVisible || !Visible) return;
            float remaining = dismissAt - Time.realtimeSinceStartup;
            secondary.interactable = remaining <= 0;
            secondaryLabel.text = remaining > 0 ? "Close (" + Mathf.CeilToInt(remaining) + ")" : "Close";
            if (remaining <= 0 && Input.GetKeyDown(KeyCode.Escape)) dismiss?.Invoke();
        }
        public void Hide()
        {
            if (shade) shade.SetActive(false); DialogOwner = null; NormalVisible = false; dismiss = null;
            RestoreCanvases();
        }
        private void BlockOtherCanvases()
        {
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.transform.IsChildOf(transform)) continue;
                var group = canvas.GetComponent<CanvasGroup>();
                if (!group) { group = canvas.gameObject.AddComponent<CanvasGroup>(); ownedGroups.Add(group); }
                if (!disabledGroups.ContainsKey(group)) disabledGroups.Add(group, group.interactable);
                group.interactable = false;
            }
        }
        private void RestoreCanvases()
        {
            foreach (var pair in disabledGroups) if (pair.Key) pair.Key.interactable = pair.Value;
            disabledGroups.Clear();
            foreach (var group in ownedGroups) if (group) Destroy(group);
            ownedGroups.Clear();
        }
        private void OnDisable() { RestoreCanvases(); }
        private void OnDestroy() { RestoreCanvases(); }
        private static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        private static Text Label(Transform parent, string name, string value, Vector2 min, Vector2 max, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
            var t = go.GetComponent<Text>(); t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = value; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false;
            return t;
        }
        public static Button Button(Transform parent, string label, Vector2 min, Vector2 max)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>(); image.color = new Color(.16f, .36f, .62f);
            var button = go.GetComponent<Button>(); button.targetGraphic = image;
            var text = Label(go.transform, "Label", label, Vector2.zero, Vector2.one, 30);
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 16; text.resizeTextMaxSize = 30;
            return button;
        }
    }
}
