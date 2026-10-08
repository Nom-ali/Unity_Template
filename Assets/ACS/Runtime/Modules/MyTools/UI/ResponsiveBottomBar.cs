using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public class ResponsiveBottomBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform bottomBar;
    [Tooltip("Parent containing the buttons/items. Direct children are handled automatically.")]
    [SerializeField] private RectTransform itemsRoot;

    [Header("Editor Preview")]
    [SerializeField] private bool previewInEditor = true;

    [Header("Responsive Width")]
    [Range(0f, 0.2f)][SerializeField] private float horizontalPaddingPercent = 0.01f;
    [SerializeField] private bool allowUpscaling = true;
    [Range(0.1f, 2f)][SerializeField] private float minResponsiveScale = 0.5f;
    [Range(0.1f, 3f)][SerializeField] private float maxResponsiveScale = 1.5f;

    [Header("Background Height")]
    [Tooltip("Extra height added above the smallest button height.")]
    [Range(-100f, 200f)][SerializeField] private float backgroundHeightOffset = 20f;

    [Tooltip("Scale the background height offset with the responsive multiplier.")]
    [SerializeField] private bool scaleBackgroundOffset = true;

    [Header("Children")]
    [Tooltip("Inactive direct children are ignored.")]
    [SerializeField] private bool ignoreInactiveChildren = true;

    [Serializable]
    private class ItemData
    {
        public RectTransform rect;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 rectSize;
        public Vector3 localScale;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
    }

    [SerializeField, HideInInspector] private List<ItemData> baselineItems = new List<ItemData>();

    private readonly List<ItemData> activeItems = new List<ItemData>();

    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private float lastParentWidth = -1f;
    private float lastParentHeight = -1f;
    private int lastHierarchySignature;

    private bool initialized;
    private bool layoutDirty = true;
    private bool isApplying;

#if UNITY_EDITOR
    private bool editorUpdateQueued;
#endif

    private void Reset()
    {
        bottomBar = GetComponent<RectTransform>();
        if (itemsRoot == null) itemsRoot = bottomBar;
    }

    private void Awake()
    {
        Initialize();
        RequestLayout();
    }

    private void Start()
    {
        RequestLayout();
    }

    private void OnEnable()
    {
        Initialize();
        RequestLayout();
    }

    private void Update()
    {
        if (!Application.isPlaying && !previewInEditor) return;

        Initialize();

        if (!initialized) return;
        if (ShouldRefresh()) layoutDirty = true;
        if (layoutDirty && !isApplying) ApplyLayout();
    }

    private void OnValidate()
    {
        minResponsiveScale = Mathf.Max(0.01f, minResponsiveScale);
        maxResponsiveScale = Mathf.Max(0.01f, maxResponsiveScale);

        RequestLayout();
    }

    private void OnTransformChildrenChanged()
    {
        RequestLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isApplying) RequestLayout();
    }

    private void Initialize()
    {
        if (bottomBar == null) bottomBar = GetComponent<RectTransform>();
        if (bottomBar == null) { initialized = false; return; }

        if (itemsRoot == null) itemsRoot = bottomBar;
        if (itemsRoot == null) { initialized = false; return; }

        if (baselineItems.Count == 0) CaptureCurrentLayoutInternal();

        RemoveInvalidBaselineItems();
        CaptureMissingChildren();

        initialized = true;
    }

    private void RequestLayout()
    {
        layoutDirty = true;

#if UNITY_EDITOR
        if (!Application.isPlaying && previewInEditor) QueueEditorUpdate();
#endif
    }

#if UNITY_EDITOR

    private void QueueEditorUpdate()
    {
        if (editorUpdateQueued) return;

        editorUpdateQueued = true;
        EditorApplication.delayCall += DelayedEditorUpdate;
    }

    private void DelayedEditorUpdate()
    {
        editorUpdateQueued = false;

        if (this == null || Application.isPlaying || !previewInEditor) return;

        Initialize();

        if (!initialized) return;

        ApplyLayout();

        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

#endif

    [ContextMenu("Apply Layout")]
    public void ApplyLayout()
    {
        if (isApplying) return;
        if (!Application.isPlaying && !previewInEditor) return;

        Initialize();

        if (!initialized || bottomBar == null || itemsRoot == null) return;

        isApplying = true;

        try
        {
            Canvas.ForceUpdateCanvases();

            RemoveInvalidBaselineItems();
            CaptureMissingChildren();
            CollectActiveItems();

            if (activeItems.Count == 0) return;

            RectTransform parent = bottomBar.parent as RectTransform;
            if (parent == null) return;

            float parentWidth = parent.rect.width;
            if (parentWidth <= 0f) return;

            SetupBottomBar();

            CalculateOriginalHorizontalBounds(out float originalMinX, out float originalMaxX);

            float originalLayoutWidth = originalMaxX - originalMinX;
            if (originalLayoutWidth <= 0.0001f) return;

            float originalLayoutCenter = (originalMinX + originalMaxX) * 0.5f;

            float horizontalPadding = parentWidth * horizontalPaddingPercent;
            float availableWidth = Mathf.Max(1f, parentWidth - horizontalPadding * 2f);

            float responsiveScale = availableWidth / originalLayoutWidth;

            if (!allowUpscaling) responsiveScale = Mathf.Min(responsiveScale, 1f);

            float minimumScale = Mathf.Min(minResponsiveScale, maxResponsiveScale);
            float maximumScale = Mathf.Max(minResponsiveScale, maxResponsiveScale);

            responsiveScale = Mathf.Clamp(responsiveScale, minimumScale, maximumScale);

            float smallestButtonHeight = CalculateSmallestButtonHeight(responsiveScale);

            float finalBackgroundOffset = scaleBackgroundOffset ? backgroundHeightOffset * responsiveScale : backgroundHeightOffset;
            float visibleBackgroundHeight = Mathf.Max(0f, smallestButtonHeight + finalBackgroundOffset);

            bottomBar.sizeDelta = new Vector2(0f, visibleBackgroundHeight);

            foreach (ItemData item in activeItems)
            {
                if (item == null || item.rect == null) continue;

                RectTransform child = item.rect;

                child.anchorMin = item.anchorMin;
                child.anchorMax = item.anchorMax;
                child.pivot = item.pivot;
                child.sizeDelta = item.sizeDelta;

                child.localScale = new Vector3(
                    item.localScale.x * responsiveScale,
                    item.localScale.y * responsiveScale,
                    item.localScale.z
                );

                float relativeX = item.anchoredPosition.x - originalLayoutCenter;
                float finalX = relativeX * responsiveScale;
                float finalY = item.anchoredPosition.y * responsiveScale;

                child.anchoredPosition = new Vector2(finalX, finalY);
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastParentWidth = parent.rect.width;
            lastParentHeight = parent.rect.height;
            lastHierarchySignature = GetHierarchySignature();

            layoutDirty = false;
        }
        finally
        {
            isApplying = false;
        }
    }

    private float CalculateSmallestButtonHeight(float responsiveScale)
    {
        float smallestHeight = float.MaxValue;

        foreach (ItemData item in activeItems)
        {
            if (item == null || item.rect == null) continue;

            float visualHeight = item.rectSize.y * Mathf.Abs(item.localScale.y) * responsiveScale;
            smallestHeight = Mathf.Min(smallestHeight, visualHeight);
        }

        return smallestHeight == float.MaxValue ? 0f : smallestHeight;
    }

    private void CalculateOriginalHorizontalBounds(out float minX, out float maxX)
    {
        minX = float.MaxValue;
        maxX = float.MinValue;

        foreach (ItemData item in activeItems)
        {
            if (item == null || item.rect == null) continue;

            float visualWidth = item.rectSize.x * Mathf.Abs(item.localScale.x);

            float left = item.anchoredPosition.x - visualWidth * item.pivot.x;
            float right = item.anchoredPosition.x + visualWidth * (1f - item.pivot.x);

            minX = Mathf.Min(minX, left);
            maxX = Mathf.Max(maxX, right);
        }

        if (minX == float.MaxValue) minX = 0f;
        if (maxX == float.MinValue) maxX = 0f;
    }

    private void CollectActiveItems()
    {
        activeItems.Clear();

        if (itemsRoot == null) return;

        for (int i = 0; i < itemsRoot.childCount; i++)
        {
            RectTransform child = itemsRoot.GetChild(i) as RectTransform;

            if (child == null) continue;
            if (ShouldIgnoreChild(child)) continue;
            if (ignoreInactiveChildren && !child.gameObject.activeInHierarchy) continue;

            ItemData data = FindBaseline(child);

            if (data == null)
            {
                data = CreateItemData(child);
                baselineItems.Add(data);
            }

            activeItems.Add(data);
        }
    }

    private void CaptureMissingChildren()
    {
        if (itemsRoot == null) return;

        for (int i = 0; i < itemsRoot.childCount; i++)
        {
            RectTransform child = itemsRoot.GetChild(i) as RectTransform;

            if (child == null) continue;
            if (ShouldIgnoreChild(child)) continue;
            if (FindBaseline(child) != null) continue;

            baselineItems.Add(CreateItemData(child));
        }
    }

    private ItemData FindBaseline(RectTransform rect)
    {
        for (int i = 0; i < baselineItems.Count; i++)
        {
            ItemData item = baselineItems[i];

            if (item != null && item.rect == rect) return item;
        }

        return null;
    }

    private ItemData CreateItemData(RectTransform child)
    {
        Canvas.ForceUpdateCanvases();

        return new ItemData
        {
            rect = child,
            anchoredPosition = child.anchoredPosition,
            sizeDelta = child.sizeDelta,
            rectSize = child.rect.size,
            localScale = child.localScale,
            anchorMin = child.anchorMin,
            anchorMax = child.anchorMax,
            pivot = child.pivot
        };
    }

    private void CaptureCurrentLayoutInternal()
    {
        if (itemsRoot == null) return;

        baselineItems.Clear();

        Canvas.ForceUpdateCanvases();

        for (int i = 0; i < itemsRoot.childCount; i++)
        {
            RectTransform child = itemsRoot.GetChild(i) as RectTransform;

            if (child == null || ShouldIgnoreChild(child)) continue;

            baselineItems.Add(CreateItemData(child));
        }
    }

    private void RemoveInvalidBaselineItems()
    {
        for (int i = baselineItems.Count - 1; i >= 0; i--)
        {
            ItemData item = baselineItems[i];

            if (item == null || item.rect == null || itemsRoot == null || item.rect.parent != itemsRoot)
            {
                baselineItems.RemoveAt(i);
            }
        }
    }

    private bool ShouldIgnoreChild(RectTransform child)
    {
        LayoutElement layoutElement = child.GetComponent<LayoutElement>();
        return layoutElement != null && layoutElement.ignoreLayout;
    }

    private void SetupBottomBar()
    {
        bottomBar.anchorMin = new Vector2(0f, 0f);
        bottomBar.anchorMax = new Vector2(1f, 0f);
        bottomBar.pivot = new Vector2(0.5f, 0f);
        bottomBar.anchoredPosition = Vector2.zero;
    }

    private bool ShouldRefresh()
    {
        if (layoutDirty) return true;
        if (Screen.width != lastScreenWidth) return true;
        if (Screen.height != lastScreenHeight) return true;

        RectTransform parent = bottomBar != null ? bottomBar.parent as RectTransform : null;

        if (parent != null)
        {
            if (!Mathf.Approximately(parent.rect.width, lastParentWidth)) return true;
            if (!Mathf.Approximately(parent.rect.height, lastParentHeight)) return true;
        }

        if (GetHierarchySignature() != lastHierarchySignature) return true;

        return false;
    }

    private int GetHierarchySignature()
    {
        if (itemsRoot == null) return 0;

        unchecked
        {
            int hash = 17;

            hash = hash * 31 + itemsRoot.childCount;

            for (int i = 0; i < itemsRoot.childCount; i++)
            {
                Transform child = itemsRoot.GetChild(i);

                hash = hash * 31 + child.GetInstanceID();
                hash = hash * 31 + (child.gameObject.activeSelf ? 1 : 0);

                LayoutElement element = child.GetComponent<LayoutElement>();

                if (element != null) hash = hash * 31 + (element.ignoreLayout ? 1 : 0);
            }

            return hash;
        }
    }

    [ContextMenu("Capture Current Layout As Baseline")]
    public void CaptureCurrentLayoutAsBaseline()
    {
        if (bottomBar == null) bottomBar = GetComponent<RectTransform>();
        if (itemsRoot == null) itemsRoot = bottomBar;

        CaptureCurrentLayoutInternal();

        initialized = true;
        layoutDirty = true;

        ApplyLayout();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }

    [ContextMenu("Restore Baseline")]
    public void RestoreBaseline()
    {
        Initialize();

        foreach (ItemData item in baselineItems)
        {
            if (item == null || item.rect == null) continue;

            item.rect.anchorMin = item.anchorMin;
            item.rect.anchorMax = item.anchorMax;
            item.rect.pivot = item.pivot;
            item.rect.sizeDelta = item.sizeDelta;
            item.rect.anchoredPosition = item.anchoredPosition;
            item.rect.localScale = item.localScale;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }
}