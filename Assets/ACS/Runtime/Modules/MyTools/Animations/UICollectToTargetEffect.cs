using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public sealed class UICollectToTargetEffect : MonoBehaviour
{
    [Header("Canvas Setup")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform animationLayer;

    [Header("Default Setup")]
    [SerializeField] private RectTransform defaultItemPrefab;
    [SerializeField] internal RectTransform defaultTarget;

    [Header("Animation")]
    [SerializeField, Min(1)] private int defaultItemCount = 12;
    [SerializeField, Min(0f)] private float spawnInterval = 0.05f;
    [SerializeField, Min(0.01f)] private float travelDuration = 0.7f;
    [SerializeField] private Vector2 spawnJitter = new Vector2(35f, 20f);
    [SerializeField] private Vector2 arcHeightRange = new Vector2(120f, 180f);
    [SerializeField] private Vector2 horizontalArcJitter = new Vector2(-60f, 60f);

    [Header("Scale")]
    [SerializeField]
    private AnimationCurve scaleOverLife = new AnimationCurve(
        new Keyframe(0f, 0.25f),
        new Keyframe(0.35f, 1.15f),
        new Keyframe(1f, 0.55f));

    [Header("Movement")]
    [SerializeField] private AnimationCurve moveEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Pooling")]
    [SerializeField, Min(0)] private int prewarmPerPrefab = 0;

    [Header("Events")]
    [SerializeField] private UnityEvent onEffectCompleted;
    [SerializeField] private Action OnEffectCompleted = null;

    private readonly Dictionary<int, Stack<RectTransform>> _pool = new();
    private readonly Dictionary<int, RectTransform> _prefabLookup = new();

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

        if (defaultItemPrefab != null && prewarmPerPrefab > 0)
            Prewarm(defaultItemPrefab, prewarmPerPrefab);
    }

    public Coroutine Play()
    {
        return Play(defaultItemPrefab, defaultTarget, defaultItemCount);
    }

    public Coroutine Play(RectTransform itemPrefab, RectTransform target, int itemCount)
    {
        return StartCoroutine(PlayRoutine(itemPrefab, target, itemCount, null, null));
    }

    public Coroutine Play(
        RectTransform itemPrefab,
        RectTransform target,
        int itemCount,
        Action<int> onSingleItemArrived,
        Action onCompleted)
    {
        return StartCoroutine(PlayRoutine(itemPrefab, target, itemCount, onSingleItemArrived, onCompleted));
    }

    public void Prewarm(RectTransform prefab, int count)
    {
        if (prefab == null || count <= 0)
            return;

        int key = prefab.GetInstanceID();

        if (!_pool.TryGetValue(key, out Stack<RectTransform> stack))
        {
            stack = new Stack<RectTransform>(count);
            _pool[key] = stack;
            _prefabLookup[key] = prefab;
        }

        for (int i = 0; i < count; i++)
        {
            RectTransform instance = CreateNewInstance(prefab);
            instance.gameObject.SetActive(false);
            stack.Push(instance);
        }
    }

    private IEnumerator PlayRoutine(
        RectTransform itemPrefab,
        RectTransform target,
        int itemCount,
        Action<int> onSingleItemArrived,
        Action onCompleted)
    {
        if (targetCanvas == null)
        {
            Debug.LogError("UICollectToTargetEffect: Target Canvas is missing.");
            yield break;
        }

        if (animationLayer == null)
        {
            Debug.LogError("UICollectToTargetEffect: Animation Layer is missing.");
            yield break;
        }

        if (itemPrefab == null)
        {
            Debug.LogError("UICollectToTargetEffect: Item prefab is missing.");
            yield break;
        }

        if (target == null)
        {
            Debug.LogError("UICollectToTargetEffect: Target is missing.");
            yield break;
        }

        if (itemCount <= 0)
            yield break;

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 startBaseLocal = ScreenToLayerLocal(screenCenter);

        int completedCount = 0;

        for (int i = 0; i < itemCount; i++)
        {
            RectTransform item = GetFromPool(itemPrefab);

            Vector2 targetLocal = GetTargetLocalPoint(target);
            Vector2 startLocal = startBaseLocal + new Vector2(
                UnityEngine.Random.Range(-spawnJitter.x, spawnJitter.x),
                UnityEngine.Random.Range(-spawnJitter.y, spawnJitter.y));

            StartCoroutine(AnimateSingleItem(
                prefabKey: itemPrefab.GetInstanceID(),
                item: item,
                start: startLocal,
                end: targetLocal,
                onArrived: () =>
                {
                    completedCount++;
                    onSingleItemArrived?.Invoke(completedCount);

                    if (completedCount >= itemCount)
                    {
                        onCompleted?.Invoke();
                        onEffectCompleted?.Invoke();
                    }
                }));

            if (spawnInterval > 0f)
                yield return new WaitForSecondsRealtime(spawnInterval);
            else
                yield return null;
        }

        while (completedCount < itemCount)
            yield return null;
    }

    private IEnumerator AnimateSingleItem(
        int prefabKey,
        RectTransform item,
        Vector2 start,
        Vector2 end,
        Action onArrived)
    {
        item.gameObject.SetActive(true);
        item.SetParent(animationLayer, false);

        // Keep UI movement predictable in local canvas space.
        item.anchorMin = new Vector2(0.5f, 0.5f);
        item.anchorMax = new Vector2(0.5f, 0.5f);
        item.pivot = new Vector2(0.5f, 0.5f);
        item.anchoredPosition = start;
        item.localRotation = Quaternion.identity;
        item.localScale = Vector3.one * EvaluateScale(0f);

        Vector2 mid = (start + end) * 0.5f;
        Vector2 control = mid + new Vector2(
            UnityEngine.Random.Range(horizontalArcJitter.x, horizontalArcJitter.y),
            UnityEngine.Random.Range(arcHeightRange.x, arcHeightRange.y));

        float elapsed = 0f;

        while (elapsed < travelDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / travelDuration);
            float easedT = moveEase.Evaluate(t);

            item.anchoredPosition = EvaluateQuadraticBezier(start, control, end, easedT);
            item.localScale = Vector3.one * EvaluateScale(t);

            yield return null;
        }

        item.anchoredPosition = end;
        item.localScale = Vector3.one * EvaluateScale(1f);

        ReturnToPool(prefabKey, item);
        onArrived?.Invoke();
    }

    private RectTransform GetFromPool(RectTransform prefab)
    {
        int key = prefab.GetInstanceID();

        if (!_pool.TryGetValue(key, out Stack<RectTransform> stack))
        {
            stack = new Stack<RectTransform>();
            _pool[key] = stack;
            _prefabLookup[key] = prefab;
        }

        if (stack.Count > 0)
            return stack.Pop();

        return CreateNewInstance(prefab);
    }

    private void ReturnToPool(int prefabKey, RectTransform instance)
    {
        if (instance == null)
            return;

        if (!_pool.TryGetValue(prefabKey, out Stack<RectTransform> stack))
        {
            stack = new Stack<RectTransform>();
            _pool[prefabKey] = stack;
        }

        instance.gameObject.SetActive(false);
        instance.SetParent(animationLayer, false);
        stack.Push(instance);
    }

    private RectTransform CreateNewInstance(RectTransform prefab)
    {
        RectTransform instance = Instantiate(prefab);
        instance.SetParent(animationLayer, false);
        return instance;
    }

    private Vector2 ScreenToLayerLocal(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            animationLayer,
            screenPoint,
            CanvasCamera,
            out Vector2 localPoint);

        return localPoint;
    }

    private Vector2 GetTargetLocalPoint(RectTransform target)
    {
        Vector2 targetScreenPoint = RectTransformUtility.WorldToScreenPoint(CanvasCamera, target.position);
        return ScreenToLayerLocal(targetScreenPoint);
    }

    private Vector2 EvaluateQuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float u = 1f - t;
        return (u * u * a) + (2f * u * t * b) + (t * t * c);
    }

    private float EvaluateScale(float t)
    {
        if (scaleOverLife == null || scaleOverLife.length == 0)
            return 1f;

        return scaleOverLife.Evaluate(t);
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<int, Stack<RectTransform>> pair in _pool)
        {
            while (pair.Value.Count > 0)
            {
                RectTransform item = pair.Value.Pop();
                if (item != null)
                    Destroy(item.gameObject);
            }
        }

        _pool.Clear();
        _prefabLookup.Clear();
    }
}