using System;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class SafeAreaController : MonoBehaviour
{
    public enum BannerReservationMode
    {
        AddAfterSafeArea,
        FromPhysicalScreenEdge
    }

    [SerializeField] private RectTransform target;
    [SerializeField] private bool previewInEditor = true;

    [SerializeField] private bool applyLeft = true;
    [SerializeField] private bool applyRight = true;
    [SerializeField] private bool applyTop = true;
    [SerializeField] private bool applyBottom = true;

    [SerializeField, Min(0f)] private float leftPadding = 0f;
    [SerializeField, Min(0f)] private float rightPadding = 0f;
    [SerializeField, Min(0f)] private float topPadding = 0f;
    [SerializeField, Min(0f)] private float bottomPadding = 0f;

    [SerializeField] private bool animateBannerSpacing = true;
    [SerializeField, Range(0.05f, 2f)] private float bannerAnimationDuration = 0.35f;

    [SerializeField] private bool supportTopBanner = false;
    [SerializeField, Min(0f)] private float topBannerHeight = 120f;
    [SerializeField, Min(0f)] private float topBannerSpacing = 0f;
    [SerializeField] private BannerReservationMode topBannerReservationMode = BannerReservationMode.AddAfterSafeArea;

    [SerializeField] private bool supportBottomBanner = false;
    [SerializeField, Min(0f)] private float bottomBannerHeight = 120f;
    [SerializeField, Min(0f)] private float bottomBannerSpacing = 0f;
    [SerializeField] private BannerReservationMode bottomBannerReservationMode = BannerReservationMode.AddAfterSafeArea;

    [SerializeField] private UnityEvent onTopBannerShowRequested = new UnityEvent();
    [SerializeField] private UnityEvent onTopBannerHideRequested = new UnityEvent();
    [SerializeField] private UnityEvent onBottomBannerShowRequested = new UnityEvent();
    [SerializeField] private UnityEvent onBottomBannerHideRequested = new UnityEvent();

    public event Action InsetsChanged;
    public event Action TopBannerShowRequested;
    public event Action TopBannerHideRequested;
    public event Action BottomBannerShowRequested;
    public event Action BottomBannerHideRequested;

    private class BannerState
    {
        public bool ready;
        public bool shown;
        public bool pendingShow;
        public bool animating;
        public float current;
        public float start;
        public float target;
        public float elapsed;
        public int showAfterFrame;
    }

    private readonly BannerState topState = new BannerState();
    private readonly BannerState bottomState = new BannerState();

    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private Rect lastSafeArea = new Rect(-1f, -1f, -1f, -1f);
    private Vector2 lastParentSize = new Vector2(-1f, -1f);

    private float currentSafeLeft;
    private float currentSafeRight;
    private float currentSafeTop;
    private float currentSafeBottom;

    private float currentFinalLeft;
    private float currentFinalRight;
    private float currentFinalTop;
    private float currentFinalBottom;

    private bool layoutDirty = true;
    private bool isApplying;

#if UNITY_EDITOR
    private bool editorUpdateQueued;
#endif

    public RectTransform Target => target;
    public RectTransform FullScreenRect => target != null ? target.parent as RectTransform : null;

    public float CurrentSafeLeft => currentSafeLeft;
    public float CurrentSafeRight => currentSafeRight;
    public float CurrentSafeTop => currentSafeTop;
    public float CurrentSafeBottom => currentSafeBottom;

    public float CurrentFinalLeft => currentFinalLeft;
    public float CurrentFinalRight => currentFinalRight;
    public float CurrentFinalTop => currentFinalTop;
    public float CurrentFinalBottom => currentFinalBottom;

    public float CurrentTopBannerOffset => topState.current;
    public float CurrentBottomBannerOffset => bottomState.current;

    public bool IsTopBannerReady => topState.ready;
    public bool IsBottomBannerReady => bottomState.ready;
    public bool IsTopBannerShown => topState.shown;
    public bool IsBottomBannerShown => bottomState.shown;

    private void Reset()
    {
        target = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        Initialize();
        RequestUpdate();
    }

    private void Start()
    {
        RequestUpdate();
    }

    private void OnEnable()
    {
        Initialize();
        RequestUpdate();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;

        HideTopBannerImmediately();
        HideBottomBannerImmediately();
    }

    private void Update()
    {
        if (!Application.isPlaying && !previewInEditor) return;

        Initialize();

        if (ShouldRefresh()) layoutDirty = true;
        if (Application.isPlaying && UpdateBannerAnimations(Time.unscaledDeltaTime)) layoutDirty = true;
        if (layoutDirty && !isApplying) ApplySafeArea();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying) return;

        FlushPendingBannerShows();
    }

    private void OnValidate()
    {
        leftPadding = Mathf.Max(0f, leftPadding);
        rightPadding = Mathf.Max(0f, rightPadding);
        topPadding = Mathf.Max(0f, topPadding);
        bottomPadding = Mathf.Max(0f, bottomPadding);

        topBannerHeight = Mathf.Max(0f, topBannerHeight);
        topBannerSpacing = Mathf.Max(0f, topBannerSpacing);

        bottomBannerHeight = Mathf.Max(0f, bottomBannerHeight);
        bottomBannerSpacing = Mathf.Max(0f, bottomBannerSpacing);

        bannerAnimationDuration = Mathf.Max(0.01f, bannerAnimationDuration);

        RequestUpdate();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isApplying) RequestUpdate();
    }

    private void OnTransformParentChanged()
    {
        RequestUpdate();
    }

    private void Initialize()
    {
        if (target == null) target = GetComponent<RectTransform>();
    }

    private void RequestUpdate()
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
        ApplySafeArea();

        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

#endif

    [ContextMenu("Apply Safe Area")]
    public void ApplySafeArea()
    {
        if (isApplying) return;
        if (!Application.isPlaying && !previewInEditor) return;

        Initialize();

        if (target == null) return;

        RectTransform parent = target.parent as RectTransform;

        if (parent == null) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        isApplying = true;

        try
        {
            Rect safeArea = Screen.safeArea;
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera uiCamera = null;

            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) uiCamera = canvas.worldCamera;

            CalculateSafeAreaInsets(parent, safeArea, uiCamera, out float safeLeft, out float safeRight, out float safeTop, out float safeBottom);

            currentSafeLeft = safeLeft;
            currentSafeRight = safeRight;
            currentSafeTop = safeTop;
            currentSafeBottom = safeBottom;

            UpdateBannerTargets(safeTop, safeBottom);

            float finalLeft = applyLeft ? safeLeft : 0f;
            float finalRight = applyRight ? safeRight : 0f;
            float finalTop = applyTop ? safeTop : 0f;
            float finalBottom = applyBottom ? safeBottom : 0f;

            finalTop += topState.current;
            finalBottom += bottomState.current;

            finalLeft += leftPadding;
            finalRight += rightPadding;
            finalTop += topPadding;
            finalBottom += bottomPadding;

            currentFinalLeft = finalLeft;
            currentFinalRight = finalRight;
            currentFinalTop = finalTop;
            currentFinalBottom = finalBottom;

            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.offsetMin = new Vector2(finalLeft, finalBottom);
            target.offsetMax = new Vector2(-finalRight, -finalTop);

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastSafeArea = safeArea;
            lastParentSize = parent.rect.size;
            layoutDirty = false;

            InsetsChanged?.Invoke();
        }
        finally
        {
            isApplying = false;
        }
    }

    private void UpdateBannerTargets(float safeTop, float safeBottom)
    {
        UpdateBannerTarget(topState, supportTopBanner, topBannerHeight, topBannerSpacing, topBannerReservationMode, applyTop, safeTop, true);
        UpdateBannerTarget(bottomState, supportBottomBanner, bottomBannerHeight, bottomBannerSpacing, bottomBannerReservationMode, applyBottom, safeBottom, false);
    }

    private void UpdateBannerTarget(BannerState state, bool supported, float height, float spacing, BannerReservationMode mode, bool safeAreaApplied, float safeInset, bool isTop)
    {
        if (!supported && state.shown)
        {
            if (isTop) HideTopBannerImmediately();
            else HideBottomBannerImmediately();
        }

        float desired = 0f;

        if (supported && state.ready)
        {
            float bannerSpace = height + spacing;

            if (mode == BannerReservationMode.AddAfterSafeArea) desired = bannerSpace;
            else desired = safeAreaApplied ? Mathf.Max(0f, bannerSpace - safeInset) : bannerSpace;
        }

        if (Mathf.Approximately(desired, state.target)) return;

        StartBannerTransition(state, desired, supported && state.ready);
    }

    private void StartBannerTransition(BannerState state, float targetValue, bool showAfterCompletion)
    {
        state.start = state.current;
        state.target = targetValue;
        state.elapsed = 0f;

        bool shouldAnimate = Application.isPlaying && animateBannerSpacing && bannerAnimationDuration > 0f;

        if (!shouldAnimate || Mathf.Approximately(state.start, state.target))
        {
            state.current = state.target;
            state.animating = false;

            if (showAfterCompletion && !state.shown) QueueBannerShow(state);

            return;
        }

        state.animating = true;
    }

    private bool UpdateBannerAnimations(float deltaTime)
    {
        bool changed = false;

        if (TickBanner(topState, deltaTime)) changed = true;
        if (TickBanner(bottomState, deltaTime)) changed = true;

        return changed;
    }

    private bool TickBanner(BannerState state, float deltaTime)
    {
        if (!state.animating) return false;

        state.elapsed += deltaTime;

        float t = Mathf.Clamp01(state.elapsed / bannerAnimationDuration);
        float eased = t * t * (3f - 2f * t);

        state.current = Mathf.Lerp(state.start, state.target, eased);

        if (t >= 1f)
        {
            state.current = state.target;
            state.animating = false;

            if (state.ready && !state.shown) QueueBannerShow(state);
        }

        return true;
    }

    private void QueueBannerShow(BannerState state)
    {
        if (!Application.isPlaying) return;

        state.pendingShow = true;
        state.showAfterFrame = Time.frameCount + 1;
    }

    private void FlushPendingBannerShows()
    {
        FlushTopBannerShow();
        FlushBottomBannerShow();
    }

    private void FlushTopBannerShow()
    {
        if (!topState.pendingShow) return;
        if (Time.frameCount < topState.showAfterFrame) return;
        if (!supportTopBanner || !topState.ready || topState.animating) return;

        topState.pendingShow = false;
        topState.shown = true;

        onTopBannerShowRequested.Invoke();
        TopBannerShowRequested?.Invoke();
    }

    private void FlushBottomBannerShow()
    {
        if (!bottomState.pendingShow) return;
        if (Time.frameCount < bottomState.showAfterFrame) return;
        if (!supportBottomBanner || !bottomState.ready || bottomState.animating) return;

        bottomState.pendingShow = false;
        bottomState.shown = true;

        onBottomBannerShowRequested.Invoke();
        BottomBannerShowRequested?.Invoke();
    }

    private void HideTopBannerImmediately()
    {
        topState.pendingShow = false;

        if (!topState.shown) return;

        topState.shown = false;

        onTopBannerHideRequested.Invoke();
        TopBannerHideRequested?.Invoke();
    }

    private void HideBottomBannerImmediately()
    {
        bottomState.pendingShow = false;

        if (!bottomState.shown) return;

        bottomState.shown = false;

        onBottomBannerHideRequested.Invoke();
        BottomBannerHideRequested?.Invoke();
    }

    [ContextMenu("Animate Spacing")]
    void AnimateSpacing()
    {
        SetBottomBannerReady(true);
        SetTopBannerReady(true);
    }

    public void SetTopBannerReady(bool ready)
    {
        if (topState.ready == ready) return;

        topState.ready = ready;

        if (!ready) HideTopBannerImmediately();

        RequestUpdate();
    }

    public void SetBottomBannerReady(bool ready)
    {
        if (bottomState.ready == ready) return;

        bottomState.ready = ready;

        if (!ready) HideBottomBannerImmediately();

        RequestUpdate();
    }

    public void SetTopBannerHeight(float height)
    {
        topBannerHeight = Mathf.Max(0f, height);
        RequestUpdate();
    }

    public void SetBottomBannerHeight(float height)
    {
        bottomBannerHeight = Mathf.Max(0f, height);
        RequestUpdate();
    }

    public void SetTopBannerSpacing(float spacing)
    {
        topBannerSpacing = Mathf.Max(0f, spacing);
        RequestUpdate();
    }

    public void SetBottomBannerSpacing(float spacing)
    {
        bottomBannerSpacing = Mathf.Max(0f, spacing);
        RequestUpdate();
    }

    public void Refresh()
    {
        RequestUpdate();
    }

    private void CalculateSafeAreaInsets(RectTransform parent, Rect safeArea, Camera uiCamera, out float left, out float right, out float top, out float bottom)
    {
        Vector2 safeMin = new Vector2(safeArea.xMin, safeArea.yMin);
        Vector2 safeMax = new Vector2(safeArea.xMax, safeArea.yMax);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, safeMin, uiCamera, out Vector2 safeMinLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, safeMax, uiCamera, out Vector2 safeMaxLocal);

        Rect parentRect = parent.rect;

        left = Mathf.Max(0f, safeMinLocal.x - parentRect.xMin);
        right = Mathf.Max(0f, parentRect.xMax - safeMaxLocal.x);
        bottom = Mathf.Max(0f, safeMinLocal.y - parentRect.yMin);
        top = Mathf.Max(0f, parentRect.yMax - safeMaxLocal.y);
    }

    private bool ShouldRefresh()
    {
        if (layoutDirty) return true;
        if (Screen.width != lastScreenWidth) return true;
        if (Screen.height != lastScreenHeight) return true;
        if (Screen.safeArea != lastSafeArea) return true;
        if (target == null) return false;

        RectTransform parent = target.parent as RectTransform;

        if (parent != null && parent.rect.size != lastParentSize) return true;

        return false;
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(SafeAreaController))]
public class SafeAreaControllerEditor : Editor
{
    private SerializedProperty target;
    private SerializedProperty previewInEditor;

    private SerializedProperty applyLeft;
    private SerializedProperty applyRight;
    private SerializedProperty applyTop;
    private SerializedProperty applyBottom;

    private SerializedProperty leftPadding;
    private SerializedProperty rightPadding;
    private SerializedProperty topPadding;
    private SerializedProperty bottomPadding;

    private SerializedProperty animateBannerSpacing;
    private SerializedProperty bannerAnimationDuration;

    private SerializedProperty supportTopBanner;
    private SerializedProperty topBannerHeight;
    private SerializedProperty topBannerSpacing;
    private SerializedProperty topBannerReservationMode;

    private SerializedProperty supportBottomBanner;
    private SerializedProperty bottomBannerHeight;
    private SerializedProperty bottomBannerSpacing;
    private SerializedProperty bottomBannerReservationMode;

    private SerializedProperty onTopBannerShowRequested;
    private SerializedProperty onTopBannerHideRequested;
    private SerializedProperty onBottomBannerShowRequested;
    private SerializedProperty onBottomBannerHideRequested;

    private bool showPadding;
    private bool showCallbacks;

    private void OnEnable()
    {
        target = serializedObject.FindProperty("target");
        previewInEditor = serializedObject.FindProperty("previewInEditor");

        applyLeft = serializedObject.FindProperty("applyLeft");
        applyRight = serializedObject.FindProperty("applyRight");
        applyTop = serializedObject.FindProperty("applyTop");
        applyBottom = serializedObject.FindProperty("applyBottom");

        leftPadding = serializedObject.FindProperty("leftPadding");
        rightPadding = serializedObject.FindProperty("rightPadding");
        topPadding = serializedObject.FindProperty("topPadding");
        bottomPadding = serializedObject.FindProperty("bottomPadding");

        animateBannerSpacing = serializedObject.FindProperty("animateBannerSpacing");
        bannerAnimationDuration = serializedObject.FindProperty("bannerAnimationDuration");

        supportTopBanner = serializedObject.FindProperty("supportTopBanner");
        topBannerHeight = serializedObject.FindProperty("topBannerHeight");
        topBannerSpacing = serializedObject.FindProperty("topBannerSpacing");
        topBannerReservationMode = serializedObject.FindProperty("topBannerReservationMode");

        supportBottomBanner = serializedObject.FindProperty("supportBottomBanner");
        bottomBannerHeight = serializedObject.FindProperty("bottomBannerHeight");
        bottomBannerSpacing = serializedObject.FindProperty("bottomBannerSpacing");
        bottomBannerReservationMode = serializedObject.FindProperty("bottomBannerReservationMode");

        onTopBannerShowRequested = serializedObject.FindProperty("onTopBannerShowRequested");
        onTopBannerHideRequested = serializedObject.FindProperty("onTopBannerHideRequested");
        onBottomBannerShowRequested = serializedObject.FindProperty("onBottomBannerShowRequested");
        onBottomBannerHideRequested = serializedObject.FindProperty("onBottomBannerHideRequested");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(target);
        EditorGUILayout.PropertyField(previewInEditor, new GUIContent("Preview In Editor"));

        EditorGUILayout.Space(6f);

        EditorGUILayout.LabelField("Safe Area Edges", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(applyLeft, new GUIContent("Left"));
        EditorGUILayout.PropertyField(applyRight, new GUIContent("Right"));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(applyTop, new GUIContent("Top"));
        EditorGUILayout.PropertyField(applyBottom, new GUIContent("Bottom"));
        EditorGUILayout.EndHorizontal();

        showPadding = EditorGUILayout.Foldout(showPadding, "Additional Padding", true);

        if (showPadding)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(leftPadding, new GUIContent("Left"));
            EditorGUILayout.PropertyField(rightPadding, new GUIContent("Right"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(topPadding, new GUIContent("Top"));
            EditorGUILayout.PropertyField(bottomPadding, new GUIContent("Bottom"));
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(6f);

        EditorGUILayout.LabelField("Banner Animation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(animateBannerSpacing, new GUIContent("Animate Spacing"));

        if (animateBannerSpacing.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(bannerAnimationDuration, new GUIContent("Duration"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(6f);

        DrawTopBanner();
        EditorGUILayout.Space(6f);
        DrawBottomBanner();

        EditorGUILayout.Space(6f);

        showCallbacks = EditorGUILayout.Foldout(showCallbacks, "Banner Callbacks", true);

        if (showCallbacks)
        {
            EditorGUI.indentLevel++;

            if (supportTopBanner.boolValue)
            {
                EditorGUILayout.PropertyField(onTopBannerShowRequested, new GUIContent("Show Top Banner"));
                EditorGUILayout.PropertyField(onTopBannerHideRequested, new GUIContent("Hide Top Banner"));
            }

            if (supportBottomBanner.boolValue)
            {
                EditorGUILayout.PropertyField(onBottomBannerShowRequested, new GUIContent("Show Bottom Banner"));
                EditorGUILayout.PropertyField(onBottomBannerHideRequested, new GUIContent("Hide Bottom Banner"));
            }

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawTopBanner()
    {
        EditorGUILayout.LabelField("Top Banner", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(supportTopBanner, new GUIContent("Enable"));

        if (!supportTopBanner.boolValue) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(topBannerHeight, new GUIContent("Height"));
        EditorGUILayout.PropertyField(topBannerSpacing, new GUIContent("Spacing"));
        EditorGUILayout.PropertyField(topBannerReservationMode, new GUIContent("Reservation Mode"));
        EditorGUI.indentLevel--;
    }

    private void DrawBottomBanner()
    {
        EditorGUILayout.LabelField("Bottom Banner", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(supportBottomBanner, new GUIContent("Enable"));

        if (!supportBottomBanner.boolValue) return;

        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(bottomBannerHeight, new GUIContent("Height"));
        EditorGUILayout.PropertyField(bottomBannerSpacing, new GUIContent("Spacing"));
        EditorGUILayout.PropertyField(bottomBannerReservationMode, new GUIContent("Reservation Mode"));
        EditorGUI.indentLevel--;
    }
}

#endif