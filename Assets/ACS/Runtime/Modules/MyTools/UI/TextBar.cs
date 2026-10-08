using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ACS_DOTWEEN
using DG.Tweening;
#endif

public class TextBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text textField;
    [SerializeField] private Image icon;

    [Header("Layout")]
    [SerializeField, Range(0.1f, 0.5f)]
    private float iconSpace = 0.25f;

    [SerializeField] private RectOffset textPadding;
    [SerializeField] private RectOffset iconPadding;

    [Header("Animation")]
    [SerializeField] private float showDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.25f;

    private RectTransform _rectTransform;

    private RectTransform rectTransform
    {
        get
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            return _rectTransform;
        }
    }

#if ACS_DOTWEEN
    private Tween scaleTween;
#endif

    private void Reset()
    {
        textPadding = new RectOffset(12, 12, 6, 6);
        iconPadding = new RectOffset(8, 8, 8, 8);
    }

    private void Awake()
    {
        // RectOffset is a Unity engine type, so create defaults here
        // instead of in MonoBehaviour field initializers.
        if (textPadding == null)
            textPadding = new RectOffset(12, 12, 6, 6);

        if (iconPadding == null)
            iconPadding = new RectOffset(8, 8, 8, 8);

        if (textField == null)
            textField = GetComponentInChildren<TMP_Text>(true);

        if (icon == null)
            icon = transform.Find("BarIcon")?.GetComponent<Image>();

        bool hasIcon =
            icon != null &&
            icon.gameObject.activeSelf &&
            icon.sprite != null;

        UpdateLayout(hasIcon);
    }

    /// <summary>
    /// Update message without an icon.
    /// </summary>
    public void UpdateMessage(params object[] messages)
    {
        SetMessage(null, messages);
    }

    /// <summary>
    /// Update message with an icon.
    /// </summary>
    public void UpdateMessage(Sprite iconSprite, params object[] messages)
    {
        SetMessage(iconSprite, messages);
    }

    private void SetMessage(Sprite iconSprite, params object[] messages)
    {
        if (textField == null)
        {
            Debug.LogWarning(
                $"{nameof(TextBarUI)}: Text field is missing.",
                this
            );

            return;
        }

        string messageText = BuildMessage(messages);

        // No valid message -> hide complete bar.
        if (string.IsNullOrWhiteSpace(messageText))
        {
            Hide();
            return;
        }

        // Set content BEFORE showing.
        textField.text = messageText;

        bool hasIcon = icon != null && iconSprite != null;

        if (icon != null)
        {
            icon.gameObject.SetActive(hasIcon);

            if (hasIcon)
                icon.sprite = iconSprite;
        }

        UpdateLayout(hasIcon);

        Show();
    }

    /// <summary>
    /// Converts all passed objects into displayable text.
    /// </summary>
    private string BuildMessage(params object[] messages)
    {
        if (messages == null || messages.Length == 0)
            return string.Empty;

        StringBuilder builder = new StringBuilder();

        foreach (object message in messages)
        {
            if (message == null)
                continue;

            string value = GetDisplayText(message);

            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (builder.Length > 0)
                builder.Append(' ');

            builder.Append(value);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Converts an object into useful display text.
    /// Unity object references use their object name.
    /// Other values use ToString().
    /// </summary>
    private string GetDisplayText(object value)
    {
        if (value == null)
            return string.Empty;

        // GameObject, Transform, Component, ScriptableObject, etc.
        if (value is Object unityObject)
        {
            return unityObject != null
                ? unityObject.name
                : string.Empty;
        }

        return value.ToString();
    }

    private void UpdateLayout(bool hasIcon)
    {
        if (textField == null)
            return;

        RectTransform textRect = textField.transform as RectTransform;
        if (textRect == null)
            return;

        if (!hasIcon)
        {
            // No icon:
            // Text uses the full parent, with padding from the edges.
            SetRect(
                textRect,
                Vector2.zero,
                Vector2.one,
                textPadding
            );

            return;
        }

        // With icon:
        // |   ICON 25%   |          TEXT 75%          |
        //
        // Each area gets its own padding so neither element touches the UI edges.
        SetRect(
            textRect,
            new Vector2(iconSpace, 0f),
            Vector2.one,
            textPadding
        );

        if (icon != null)
        {
            SetRect(
                icon.rectTransform,
                Vector2.zero,
                new Vector2(iconSpace, 1f),
                iconPadding
            );
        }
    }

    /// <summary>
    /// Stretches a RectTransform between normalized anchors
    /// and applies pixel padding inside that area.
    /// </summary>
    private void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        RectOffset padding)
    {
        if (rect == null)
            return;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;

        if (padding == null)
        {
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return;
        }

        rect.offsetMin = new Vector2(
            padding.left,
            padding.bottom
        );

        rect.offsetMax = new Vector2(
            -padding.right,
            -padding.top
        );
    }

    /// <summary>
    /// Hides the complete bar.
    /// </summary>
    public void Hide()
    {
        if (!gameObject.activeSelf)
        {
            ClearContent();
            return;
        }

#if ACS_DOTWEEN

        scaleTween?.Kill();

        scaleTween = rectTransform
            .DOScale(Vector3.zero, hideDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                scaleTween = null;

                ClearContent();

                gameObject.SetActive(false);
            });

#else

        rectTransform.localScale = Vector3.zero;

        ClearContent();

        gameObject.SetActive(false);

#endif
    }

    /// <summary>
    /// Shows the complete bar.
    /// </summary>
    public void Show()
    {
#if ACS_DOTWEEN

        scaleTween?.Kill();

        if (!gameObject.activeSelf)
        {
            rectTransform.localScale = Vector3.zero;
            gameObject.SetActive(true);
        }

        scaleTween = rectTransform
            .DOScale(Vector3.one, showDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                scaleTween = null;
            });

#else

        gameObject.SetActive(true);
        rectTransform.localScale = Vector3.one;

#endif
    }

    private void ClearContent()
    {
        if (textField != null)
            textField.text = string.Empty;
    }

    private void OnDisable()
    {
#if ACS_DOTWEEN
        scaleTween?.Kill();
        scaleTween = null;
#endif
    }
}
