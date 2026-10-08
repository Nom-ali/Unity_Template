using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach one instance to each authored booster button.
/// It displays that booster's count, shows "+" at zero, and routes clicks to BoosterManager.
/// </summary>
[DisallowMultipleComponent]
public class BoosterButton : MonoBehaviour
{
    [Header("Booster")]
    [SerializeField] private BoosterManager boosterManager;
    [SerializeField] private string boosterId;

    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private string emptyCountLabel = "+";

    [Header("Lookup")]
    [SerializeField] private bool findManagerInParents = true;

    private void Reset()
    {
        button = GetComponent<Button>();
        iconImage = GetComponent<Image>();
        boosterManager = GetComponentInParent<BoosterManager>();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
        BindButton();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
        UnbindButton();
    }

    private void OnValidate()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    /// <summary>
    /// Lets setup code assign a manager and stable catalog ID without Inspector wiring.
    /// </summary>
    public void Configure(BoosterManager manager, string id)
    {
        Unsubscribe();

        boosterManager = manager;
        boosterId = id;

        Subscribe();
        Refresh();
    }

    public void Refresh()
    {
        if (button == null)
        {
            return;
        }

        BoosterManager.BoosterData booster = null;
        bool hasBooster = boosterManager != null &&
            boosterManager.TryGetBooster(boosterId, out booster);

        button.interactable = hasBooster;

        if (!hasBooster)
        {
            if (countText != null)
            {
                countText.text = emptyCountLabel;
            }

            return;
        }

        if (iconImage != null && booster.icon != null)
        {
            iconImage.sprite = booster.icon;
        }

        if (countText != null)
        {
            countText.text = booster.currentCount > 0
                ? booster.currentCount.ToString()
                : emptyCountLabel;
        }
    }

    private void HandleButtonClicked()
    {
        boosterManager?.UseBooster(boosterId);
    }

    private void HandleBoosterCountChanged(string changedBoosterId, int count)
    {
        if (string.Equals(changedBoosterId, boosterId, System.StringComparison.Ordinal))
        {
            Refresh();
        }
    }

    private void ResolveReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (boosterManager == null && findManagerInParents)
        {
            boosterManager = GetComponentInParent<BoosterManager>();
        }

        if (boosterManager == null)
        {
            boosterManager = FindObjectOfType<BoosterManager>();
        }
    }

    private void Subscribe()
    {
        if (boosterManager == null)
        {
            return;
        }

        boosterManager.OnBoosterCountChanged -= HandleBoosterCountChanged;
        boosterManager.OnBoosterCountChanged += HandleBoosterCountChanged;
    }

    private void Unsubscribe()
    {
        if (boosterManager != null)
        {
            boosterManager.OnBoosterCountChanged -= HandleBoosterCountChanged;
        }
    }

    private void BindButton()
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(HandleButtonClicked);
        button.onClick.AddListener(HandleButtonClicked);
    }

    private void UnbindButton()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }
    }
}
