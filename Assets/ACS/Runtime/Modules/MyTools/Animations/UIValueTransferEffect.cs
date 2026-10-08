#if ACS_DOTWEEN
using DG.Tweening;
#endif
using ACS.SaveManager;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class UIValueTransferEffect : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform animationLayer;

    [Header("Visuals")]
    [SerializeField] private RectTransform itemPrefab;
    [SerializeField] private RectTransform targetIcon;
    [SerializeField] private RectTransform spawnOriginOverride;

    [Header("Texts")]
    [SerializeField] private TMP_Text earnedValueText;
    [SerializeField] private TMP_Text walletValueText;

    [Header("Starting Values")]
    [SerializeField] private int walletStartValue => SaveManager.Currency.Value;

    [Header("Value To Visual Mapping")]
    [SerializeField, Min(1)] private int maxVisualItems = 20;

    [Header("Sequence Timing")]
    [SerializeField, Min(0.01f)] private float earnedFillDuration = 0.5f;
    [SerializeField, Min(0f)] private float clusterHoldDuration = 1.2f;
    [SerializeField, Min(0f)] private float launchInterval = 0.05f;
    [SerializeField, Min(0.01f)] private float travelDuration = 0.7f;

    [Header("Cluster Spawn")]
    [SerializeField] private Vector2 clusterSize = new Vector2(180f, 110f);
    [SerializeField] private Vector2 clusterScaleRange = new Vector2(0.9f, 1.1f);

    [Header("Flight")]
    [SerializeField] private Vector2 arcHeightRange = new Vector2(120f, 180f);
    [SerializeField] private Vector2 horizontalArcJitter = new Vector2(-60f, 60f);
    [SerializeField] private AnimationCurve moveEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField]
    private AnimationCurve scaleOverFlight = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.35f, 1.12f),
        new Keyframe(1f, 0.45f));

    [Header("Formatting")]
    [SerializeField] private string valueFormat = "N0";

    private readonly Stack<RectTransform> _pool = new();
    private readonly List<RectTransform> _activeVisuals = new();

    private Coroutine _playRoutine;
    private int _currentWalletValue;
    private int _currentEarnedValue;

    private Camera CanvasCamera
    {
        get
        {
            if (targetCanvas == null)
                return null;

            return targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : targetCanvas.worldCamera;
        }
    }

    private void Awake()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        if (animationLayer == null && targetCanvas != null)
            animationLayer = targetCanvas.transform as RectTransform;

        _currentWalletValue = Mathf.Max(0, walletStartValue);
        _currentEarnedValue = 0;
        RefreshTexts();
    }

    public void SetWalletValue(int value)
    {
        _currentWalletValue = Mathf.Max(0, value);
        RefreshTexts();
    }

    public IEnumerator PlayUntil(int earnedValue)
    {
        yield return Play(earnedValue, null, null, null, null);
    }

    public void Play(int earnedValue)
    {
        StartCoroutine(Play(earnedValue, null, null, null, null));
    }

    public void Play(int earnedValue, TextMeshProUGUI  earnedValueText, Action onComplete = null)
    {
        StartCoroutine(Play(earnedValue, null, null, null, onComplete));
    }

    public IEnumerator PlayUntil(int earnedValue, TextMeshProUGUI  earnedValueText, Action onComplete = null)
    {
        yield return Play(earnedValue, null, null, null, onComplete);
    }

    public IEnumerator Play(
        int earnedValue,
        TextMeshProUGUI earnedValueText,
        RectTransform spawnOrigin,
        RectTransform targetOverride,
        Action onCompleted)
    {
        StopCurrentEffect();
        
        if (earnedValueText != null)
            this.earnedValueText = earnedValueText;

        if (earnedValue <= 0)
        {
            _currentEarnedValue = 0;
            RefreshTexts();
            onCompleted?.Invoke();
            yield break;
        }

        Debug.Log($"Earned Coins: {earnedValue}");

        _playRoutine = StartCoroutine(
            PlayRoutine(
                Mathf.Max(0, earnedValue),
                spawnOrigin != null ? spawnOrigin : spawnOriginOverride,
                targetOverride != null ? targetOverride : targetIcon,
                onCompleted));
        yield return new WaitUntil(() => _playRoutine == null);
    }

    public void StopCurrentEffect()
    {
        StopAllCoroutines();

        for (int i = 0; i < _activeVisuals.Count; i++)
        {
            if (_activeVisuals[i] != null)
                ReturnToPool(_activeVisuals[i]);
        }

        _activeVisuals.Clear();
        _playRoutine = null;
    }

    private IEnumerator PlayRoutine(
        int earnedValue,
        RectTransform spawnOrigin,
        RectTransform target,
        Action onCompleted)
    {
        if (targetCanvas == null)
        {
            Debug.LogError("UIValueTransferEffect: Target Canvas is missing.");
            yield break;
        }

        if (animationLayer == null)
        {
            Debug.LogError("UIValueTransferEffect: Animation Layer is missing.");
            yield break;
        }

        if (itemPrefab == null)
        {
            Debug.LogError("UIValueTransferEffect: Item Prefab is missing.");
            yield break;
        }

        if (target == null)
        {
            Debug.LogError("UIValueTransferEffect: Target Icon is missing.");
            yield break;
        }

        // Phase 1: fill earned text on panel
        yield return AnimateValue(
            from: 0,
            to: earnedValue,
            duration: earnedFillDuration,
            onValueChanged: value =>
            {
                _currentEarnedValue = value;
                RefreshTexts();
            });

        // Phase 2: spawn visual items in a center cluster
        int visualCount = ResolveVisualCount(earnedValue);
        int[] valueChunks = BuildValueChunks(earnedValue, visualCount);

        Vector2 clusterCenter = ResolveSpawnCenterLocal(spawnOrigin);
        Vector2 targetLocal = ResolveLocalPoint(target.position);

        List<RectTransform> visuals = SpawnCluster(visualCount, clusterCenter);

        if (clusterHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(clusterHoldDuration);

        // Phase 3: transfer visuals one by one
        int completed = 0;

        for (int i = 0; i < visuals.Count; i++)
        {
            RectTransform visual = visuals[i];
            int chunkValue = valueChunks[i];

            StartCoroutine(AnimateSingleVisual(
                visual: visual,
                targetLocal: targetLocal,
                transferValue: chunkValue,
                onArrived: transferred =>
                {
                    _currentEarnedValue = Mathf.Max(0, _currentEarnedValue - transferred);
                    _currentWalletValue += transferred;
                    RefreshTexts();
                    completed++;
                }));

            if (launchInterval > 0f)
                yield return new WaitForSecondsRealtime(launchInterval);
            else
                yield return null;
        }

        while (completed < visuals.Count)
            yield return null;

        _currentEarnedValue = 0;
        RefreshTexts();

        _playRoutine = null;
        onCompleted?.Invoke();
    }

    private IEnumerator AnimateValue(
        int from,
        int to,
        float duration,
        Action<int> onValueChanged)
    {
        if (duration <= 0f)
        {
            onValueChanged?.Invoke(to);
            yield break;
        }

        float elapsed = 0f;
        int lastValue = from;

        onValueChanged?.Invoke(from);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            int currentValue = Mathf.RoundToInt(Mathf.Lerp(from, to, t));

            if (currentValue != lastValue)
            {
                lastValue = currentValue;
                onValueChanged?.Invoke(currentValue);
            }

            yield return null;
        }

        onValueChanged?.Invoke(to);
    }

    private List<RectTransform> SpawnCluster(int count, Vector2 clusterCenter)
    {
        List<RectTransform> result = new List<RectTransform>(count);

        for (int i = 0; i < count; i++)
        {
            RectTransform item = GetFromPool();
            item.SetParent(animationLayer, false);
            item.gameObject.SetActive(true);

            item.anchorMin = new Vector2(0.5f, 0.5f);
            item.anchorMax = new Vector2(0.5f, 0.5f);
            item.pivot = new Vector2(0.5f, 0.5f);

            Vector2 offset = new Vector2(
                UnityEngine.Random.Range(-clusterSize.x * 0.5f, clusterSize.x * 0.5f),
                UnityEngine.Random.Range(-clusterSize.y * 0.5f, clusterSize.y * 0.5f));

            item.localScale = Vector3.zero;

            float startScale = UnityEngine.Random.Range(clusterScaleRange.x, clusterScaleRange.y);

            item.anchoredPosition = clusterCenter + offset;
            item.localRotation = Quaternion.identity;
#if ACS_DOTWEEN
            item.DOScale(Vector3.one * startScale, 0.35f);
#else
            item.localScale = Vector3.one * startScale;
#endif
            _activeVisuals.Add(item);
            result.Add(item);
        }

        return result;
    }

    private IEnumerator AnimateSingleVisual(
        RectTransform visual,
        Vector2 targetLocal,
        int transferValue,
        Action<int> onArrived)
    {
        if (visual == null)
            yield break;

        Vector2 start = visual.anchoredPosition;
        float baseScale = visual.localScale.x;

        Vector2 mid = (start + targetLocal) * 0.5f;
        Vector2 control = mid + new Vector2(
            UnityEngine.Random.Range(horizontalArcJitter.x, horizontalArcJitter.y),
            UnityEngine.Random.Range(arcHeightRange.x, arcHeightRange.y));

        float elapsed = 0f;

        while (elapsed < travelDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / travelDuration);
            float easedT = moveEase.Evaluate(t);

            visual.anchoredPosition = EvaluateQuadraticBezier(start, control, targetLocal, easedT);
            visual.localScale = Vector3.one * (baseScale * EvaluateScale(t));

            yield return null;
        }

        visual.anchoredPosition = targetLocal;
        visual.localScale = Vector3.zero;

        ReturnToPool(visual);
        _activeVisuals.Remove(visual);

        onArrived?.Invoke(transferValue);
    }

    private RectTransform GetFromPool()
    {
        if (_pool.Count > 0)
            return _pool.Pop();

        RectTransform instance = Instantiate(itemPrefab);
        instance.SetParent(animationLayer, false);
        instance.transform.localScale = Vector3.zero;
        instance.gameObject.SetActive(false);
        return instance;
    }

    private void ReturnToPool(RectTransform item)
    {
        if (item == null)
            return;

        item.gameObject.SetActive(false);
        item.SetParent(animationLayer, false);
        _pool.Push(item);
    }

    private int ResolveVisualCount(int earnedValue)
    {
        if (earnedValue <= 0)
            return 0;

        return Mathf.Clamp(earnedValue, 1, maxVisualItems);
    }

    private int[] BuildValueChunks(int totalValue, int visualCount)
    {
        int[] chunks = new int[visualCount];

        if (visualCount <= 0)
            return chunks;

        int baseValue = totalValue / visualCount;
        int remainder = totalValue % visualCount;

        for (int i = 0; i < visualCount; i++)
        {
            chunks[i] = baseValue + (i < remainder ? 1 : 0);
        }

        return chunks;
    }

    private Vector2 ResolveSpawnCenterLocal(RectTransform spawnOrigin)
    {
        if (spawnOrigin != null)
            return ResolveLocalPoint(spawnOrigin.position);

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        return ScreenToLocal(screenCenter);
    }

    private Vector2 ResolveLocalPoint(Vector3 worldPosition)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(CanvasCamera, worldPosition);
        return ScreenToLocal(screenPoint);
    }

    private Vector2 ScreenToLocal(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            animationLayer,
            screenPoint,
            CanvasCamera,
            out Vector2 localPoint);

        return localPoint;
    }

    private Vector2 EvaluateQuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float u = 1f - t;
        return (u * u * a) + (2f * u * t * b) + (t * t * c);
    }

    private float EvaluateScale(float t)
    {
        if (scaleOverFlight == null || scaleOverFlight.length == 0)
            return 1f;

        return Mathf.Max(0f, scaleOverFlight.Evaluate(t));
    }

    private void RefreshTexts()
    {
        if (earnedValueText != null)
            earnedValueText.text = FormatValue(_currentEarnedValue);

        if (walletValueText != null)
            walletValueText.text = FormatValue(_currentWalletValue);
    }

    private string FormatValue(int value)
    {
        return value.ToString(valueFormat);
    }

    private void OnDestroy()
    {
        StopCurrentEffect();

        while (_pool.Count > 0)
        {
            RectTransform item = _pool.Pop();
            if (item != null)
                Destroy(item.gameObject);
        }
    }
}