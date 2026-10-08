using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public class SafeAreaIgnore : MonoBehaviour
{
    public enum ChildLayoutMode
    {
        FollowIgnoredParent,
        FollowSafeAreaRoot
    }

    [Header("References")]
    [Tooltip("SafeAreaController used to find the SafeAreaRoot. Automatically searched in parents if empty.")]
    [SerializeField] private SafeAreaController safeAreaController;

    [Tooltip("Object that should ignore the safe area and expand toward the physical screen. If empty, this object's RectTransform is used.")]
    [SerializeField] private RectTransform target;

    [Header("Ignored Edges")]
    [Tooltip("Expand this object to the physical screen's left edge.")]
    [SerializeField] private bool ignoreLeft = true;

    [Tooltip("Expand this object to the physical screen's right edge.")]
    [SerializeField] private bool ignoreRight = true;

    [Tooltip("Expand this object to the physical screen's top edge.")]
    [SerializeField] private bool ignoreTop = true;

    [Tooltip("Expand this object to the physical screen's bottom edge.")]
    [SerializeField] private bool ignoreBottom = true;

    [Header("Children Layout")]
    [Tooltip("Follow Ignored Parent: children simply follow this expanded parent normally.\n\nFollow Safe Area Root: only child anchors are remapped to SafeAreaController.Target. Child size, offsets, pivot, and scale remain authored values.")]
    [SerializeField] private ChildLayoutMode childLayoutMode = ChildLayoutMode.FollowSafeAreaRoot;

    [Tooltip("Also apply SafeAreaRoot compensation to inactive direct children.")]
    [SerializeField] private bool includeInactiveChildren = true;

    [Header("Editor Preview")]
    [SerializeField] private bool previewInEditor = true;

    [Serializable]
    private class ChildBaseline
    {
        public RectTransform rect;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 offsetMin;
        public Vector2 offsetMax;
        public Vector2 pivot;
        public Vector3 localScale;
    }

    [SerializeField, HideInInspector] private List<ChildBaseline> childBaselines = new List<ChildBaseline>();

    private bool initialized;
    private bool isApplying;
    private ChildLayoutMode lastChildLayoutMode;

    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

#if UNITY_EDITOR
    private bool editorUpdateQueued;
#endif

    private void Reset()
    {
        target = GetComponent<RectTransform>();
        safeAreaController = GetComponentInParent<SafeAreaController>();
        CaptureCurrentChildrenAsBaselineInternal();
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        Subscribe();
        RequestUpdate();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (!Application.isPlaying && !previewInEditor) return;

        Initialize();

        if (!initialized) return;

        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight) RequestUpdate();

        if (childLayoutMode != lastChildLayoutMode)
        {
            RestoreChildren();
            lastChildLayoutMode = childLayoutMode;
            RequestUpdate();
        }
    }

    private void OnValidate()
    {
        Initialize();

        if (childLayoutMode != lastChildLayoutMode)
        {
            RestoreChildren();
            lastChildLayoutMode = childLayoutMode;
        }

        RequestUpdate();
    }

    private void OnTransformParentChanged()
    {
        Unsubscribe();
        Initialize();
        Subscribe();
        RequestUpdate();
    }

    private void OnTransformChildrenChanged()
    {
        if (isApplying) return;

        RemoveInvalidChildren();
        CaptureMissingChildren();
        RequestUpdate();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isApplying) RequestUpdate();
    }

    private void Initialize()
    {
        if (target == null) target = GetComponent<RectTransform>();
        if (safeAreaController == null) safeAreaController = GetComponentInParent<SafeAreaController>();

        if (target == null || safeAreaController == null)
        {
            initialized = false;
            return;
        }

        RemoveInvalidChildren();

        if (childBaselines.Count == 0) CaptureCurrentChildrenAsBaselineInternal();
        else CaptureMissingChildren();

        lastChildLayoutMode = childLayoutMode;
        initialized = true;
    }

    private void Subscribe()
    {
        if (safeAreaController == null) return;

        safeAreaController.InsetsChanged -= HandleSafeAreaChanged;
        safeAreaController.InsetsChanged += HandleSafeAreaChanged;
    }

    private void Unsubscribe()
    {
        if (safeAreaController == null) return;

        safeAreaController.InsetsChanged -= HandleSafeAreaChanged;
    }

    private void HandleSafeAreaChanged()
    {
        RequestUpdate();
    }

    private void RequestUpdate()
    {
        if (Application.isPlaying)
        {
            ApplyIgnoredArea();
            return;
        }

#if UNITY_EDITOR
        if (previewInEditor) QueueEditorUpdate();
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
        ApplyIgnoredArea();

        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

#endif

    [ContextMenu("Apply Ignored Area")]
    public void ApplyIgnoredArea()
    {
        if (isApplying) return;
        if (!Application.isPlaying && !previewInEditor) return;

        Initialize();

        if (!initialized || target == null || safeAreaController == null) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        RectTransform targetParent = target.parent as RectTransform;

        if (targetParent == null) return;

        isApplying = true;

        try
        {
            ExpandTargetToPhysicalScreen(targetParent);

            if (childLayoutMode == ChildLayoutMode.FollowIgnoredParent)
            {
                RestoreChildren();
            }
            else
            {
                ApplyChildrenToSafeAreaRoot();
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }
        finally
        {
            isApplying = false;
        }
    }

    private void ExpandTargetToPhysicalScreen(RectTransform targetParent)
    {
        // Screen.width / Screen.height are physical pixels. They cannot safely be
        // converted directly into a Canvas Scaler's local coordinates in editor
        // previews or nested UI hierarchies. Use the controller's full-screen UI
        // container instead, then express its corners in this target's parent.
        RectTransform fullScreenRect = safeAreaController.FullScreenRect;

        if (fullScreenRect == null) return;

        Vector3[] fullScreenCorners = new Vector3[4];
        fullScreenRect.GetWorldCorners(fullScreenCorners);

        Vector3 fullScreenBottomLeft = targetParent.InverseTransformPoint(fullScreenCorners[0]);
        Vector3 fullScreenTopRight = targetParent.InverseTransformPoint(fullScreenCorners[2]);

        Rect parentRect = targetParent.rect;

        float desiredLeft = ignoreLeft ? fullScreenBottomLeft.x : parentRect.xMin;
        float desiredRight = ignoreRight ? fullScreenTopRight.x : parentRect.xMax;
        float desiredBottom = ignoreBottom ? fullScreenBottomLeft.y : parentRect.yMin;
        float desiredTop = ignoreTop ? fullScreenTopRight.y : parentRect.yMax;

        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;

        target.offsetMin = new Vector2(desiredLeft - parentRect.xMin, desiredBottom - parentRect.yMin);
        target.offsetMax = new Vector2(desiredRight - parentRect.xMax, desiredTop - parentRect.yMax);
    }

    private void ApplyChildrenToSafeAreaRoot()
    {
        CaptureMissingChildren();
        RemoveInvalidChildren();

        if (!TryGetSafeAnchorBounds(out Vector2 safeAnchorMin, out Vector2 safeAnchorMax))
        {
            return;
        }

        foreach (ChildBaseline baseline in childBaselines)
        {
            if (baseline == null || baseline.rect == null) continue;
            if (!includeInactiveChildren && !baseline.rect.gameObject.activeInHierarchy) continue;

            RectTransform child = baseline.rect;

            child.anchorMin = MapToSafeRoot(baseline.anchorMin, safeAnchorMin, safeAnchorMax);
            child.anchorMax = MapToSafeRoot(baseline.anchorMax, safeAnchorMin, safeAnchorMax);

            // Intentionally leave every non-anchor property untouched. This component
            // positions children relative to the SafeAreaRoot; it must not overwrite
            // their authored size, offsets, pivot, or scale.
        }
    }

    private bool TryGetSafeAnchorBounds(
        out Vector2 safeAnchorMin,
        out Vector2 safeAnchorMax)
    {
        safeAnchorMin = Vector2.zero;
        safeAnchorMax = Vector2.one;

        if (target == null || safeAreaController == null)
        {
            return false;
        }

        RectTransform safeAreaRoot = safeAreaController.Target;
        if (safeAreaRoot == null)
        {
            return false;
        }

        Rect targetRect = target.rect;
        if (Mathf.Approximately(targetRect.width, 0f) ||
            Mathf.Approximately(targetRect.height, 0f))
        {
            return false;
        }

        Vector3[] safeCorners = new Vector3[4];
        safeAreaRoot.GetWorldCorners(safeCorners);

        Vector3 safeBottomLeft = target.InverseTransformPoint(safeCorners[0]);
        Vector3 safeTopRight = target.InverseTransformPoint(safeCorners[2]);

        safeAnchorMin = new Vector2(
            Mathf.InverseLerp(targetRect.xMin, targetRect.xMax, safeBottomLeft.x),
            Mathf.InverseLerp(targetRect.yMin, targetRect.yMax, safeBottomLeft.y)
        );

        safeAnchorMax = new Vector2(
            Mathf.InverseLerp(targetRect.xMin, targetRect.xMax, safeTopRight.x),
            Mathf.InverseLerp(targetRect.yMin, targetRect.yMax, safeTopRight.y)
        );

        return true;
    }

    private void CaptureMissingChildren()
    {
        if (target == null) return;

        for (int i = 0; i < target.childCount; i++)
        {
            RectTransform child = target.GetChild(i) as RectTransform;

            if (child == null) continue;
            if (FindBaseline(child) != null) continue;

            childBaselines.Add(CreateBaseline(child));
        }
    }

    private void CaptureCurrentChildrenAsBaselineInternal()
    {
        if (target == null) return;

        RemoveInvalidChildren();

        Vector2 safeAnchorMin = Vector2.zero;
        Vector2 safeAnchorMax = Vector2.zero;

        bool protectAppliedLayout =
            childLayoutMode == ChildLayoutMode.FollowSafeAreaRoot &&
            TryGetSafeAnchorBounds(out safeAnchorMin, out safeAnchorMax);

        for (int i = 0; i < target.childCount; i++)
        {
            RectTransform child = target.GetChild(i) as RectTransform;

            if (child == null) continue;

            ChildBaseline baseline = FindBaseline(child);

            if (baseline == null)
            {
                childBaselines.Add(CreateBaseline(child));
                continue;
            }

            // Do not save a layout that SafeAreaIgnore has already transformed.
            // Saving it as a baseline would map the safe area a second time.
            if (protectAppliedLayout &&
                IsDrivenSafeAreaLayout(child, baseline, safeAnchorMin, safeAnchorMax))
            {
                continue;
            }

            CopyBaseline(baseline, child);
        }
    }

    private ChildBaseline CreateBaseline(RectTransform child)
    {
        var baseline = new ChildBaseline();
        CopyBaseline(baseline, child);
        return baseline;
    }

    private static void CopyBaseline(ChildBaseline baseline, RectTransform child)
    {
        baseline.rect = child;
        baseline.anchorMin = child.anchorMin;
        baseline.anchorMax = child.anchorMax;
    }

    private static Vector2 MapToSafeRoot(
        Vector2 baselineAnchor,
        Vector2 safeAnchorMin,
        Vector2 safeAnchorMax)
    {
        return new Vector2(
            Mathf.Lerp(safeAnchorMin.x, safeAnchorMax.x, baselineAnchor.x),
            Mathf.Lerp(safeAnchorMin.y, safeAnchorMax.y, baselineAnchor.y)
        );
    }

    private static bool IsDrivenSafeAreaLayout(
        RectTransform child,
        ChildBaseline baseline,
        Vector2 safeAnchorMin,
        Vector2 safeAnchorMax)
    {
        return Approximately(child.anchorMin, MapToSafeRoot(baseline.anchorMin, safeAnchorMin, safeAnchorMax)) &&
               Approximately(child.anchorMax, MapToSafeRoot(baseline.anchorMax, safeAnchorMin, safeAnchorMax));
    }

    private static bool Approximately(Vector2 first, Vector2 second)
    {
        return Mathf.Approximately(first.x, second.x) &&
               Mathf.Approximately(first.y, second.y);
    }

    private ChildBaseline FindBaseline(RectTransform rect)
    {
        for (int i = 0; i < childBaselines.Count; i++)
        {
            ChildBaseline baseline = childBaselines[i];

            if (baseline != null && baseline.rect == rect) return baseline;
        }

        return null;
    }

    private void RemoveInvalidChildren()
    {
        for (int i = childBaselines.Count - 1; i >= 0; i--)
        {
            ChildBaseline baseline = childBaselines[i];

            if (baseline == null || baseline.rect == null || target == null || baseline.rect.parent != target)
            {
                childBaselines.RemoveAt(i);
            }
        }
    }

    private void RestoreChildren()
    {
        foreach (ChildBaseline baseline in childBaselines)
        {
            if (baseline == null || baseline.rect == null) continue;

            RectTransform child = baseline.rect;

            child.anchorMin = baseline.anchorMin;
            child.anchorMax = baseline.anchorMax;
            // Only anchors belong to SafeAreaIgnore's layout responsibility.
        }
    }

    [ContextMenu("Capture Current Children As Baseline")]
    public void CaptureCurrentChildrenAsBaseline()
    {
        Initialize();

        if (target == null) return;

        CaptureCurrentChildrenAsBaselineInternal();
        ApplyIgnoredArea();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }

    [ContextMenu("Restore Children Baseline")]
    public void RestoreChildrenBaseline()
    {
        RestoreChildren();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }
}
