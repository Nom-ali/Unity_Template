using ACS.SaveManager;
using ACS.Animations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NextLevelGames
{
    public class CoinsAnimation : MonoBehaviour
    {
        [SerializeField] internal bool PlayOnEnabel = true;
        [SerializeField] private GameObject pileOfCoins;
        [SerializeField] internal UnityEvent OnComplete;

        private Vector3 targetPos;
        private Coroutine giveCoinsCoroutine;
        private Coroutine countCoinsCoroutine;
        private float animationDuration;

        private readonly Dictionary<Transform, TransformState> originalStates =
            new Dictionary<Transform, TransformState>();

        private struct TransformState
        {
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;

            public TransformState(Transform target)
            {
                LocalPosition = target.localPosition;
                LocalRotation = target.localRotation;
                LocalScale = target.localScale;
            }
        }

        private void Awake()
        {
            CacheOriginalStates();
        }

        private void OnEnable()
        {
            if (PlayOnEnabel)
            {
                Init();
            }
        }

        internal void Init()
        {
            if (pileOfCoins == null)
            {
                Debug.LogWarning(
                    "Pile Of Coins is not assigned.",
                    this
                );

                return;
            }

            if (UIManager.Instance == null ||
                UIManager.Instance.coinAnimationTarget == null)
            {
                Debug.LogWarning(
                    "Coin animation target is not available.",
                    this
                );

                return;
            }

            StopCurrentAnimation();
            CacheOriginalStates();
            ResetCoins();

            targetPos = UIManager.Instance.coinAnimationTarget.position;

            countCoinsCoroutine = StartCoroutine(CountCoins());
        }

        private IEnumerator CountCoins()
        {
            pileOfCoins.SetActive(true);

            float delay = 0f;
            int childCount = pileOfCoins.transform.childCount;

            yield return new WaitForSeconds(0.5f);

            for (int i = 0; i < childCount; i++)
            {
                Transform coin = pileOfCoins.transform.GetChild(i);
                coin.AnimateScale(0.8f, 0.3f).SetOwner(this)
                    .SetDelay(delay)
                    .SetEase(Ease.OutBack);

                coin.AnimateMove(targetPos, 0.8f).SetOwner(this)
                    .SetDelay(delay + 0.5f)
                    .SetEase(Ease.InBack);

                coin.AnimateRotate(Vector3.zero, 0.5f).SetOwner(this)
                    .SetDelay(delay + 0.5f)
                    .SetEase(Ease.Flash);

                coin.AnimateScale(0f, 0.3f).SetOwner(this)
                    .SetDelay(delay + 1.5f)
                    .SetEase(Ease.OutBack);

                delay += 0.1f;

            }

            // Last coin completes at its delay + 1.8 seconds.
            animationDuration = childCount > 0
                ? ((childCount - 1) * 0.1f) + 1.8f
                : 0f;

            Invoke(nameof(PlayCoinSound), 1f);

            giveCoinsCoroutine = StartCoroutine(GiveCoins());
        }

        private void CacheOriginalStates()
        {
            if (pileOfCoins == null)
            {
                return;
            }

            for (int i = 0;
                 i < pileOfCoins.transform.childCount;
                 i++)
            {
                Transform coin =
                    pileOfCoins.transform.GetChild(i);

                if (!originalStates.ContainsKey(coin))
                {
                    originalStates.Add(
                        coin,
                        new TransformState(coin)
                    );
                }
            }
        }

        private void ResetCoins()
        {
            foreach (KeyValuePair<Transform, TransformState> entry
                     in originalStates)
            {
                Transform coin = entry.Key;

                if (coin == null)
                {
                    continue;
                }
                coin.CancelAnimations();
                coin.localPosition =
                    entry.Value.LocalPosition;

                coin.localRotation =
                    entry.Value.LocalRotation;

                coin.localScale =
                    entry.Value.LocalScale;
            }
        }

        private void StopCurrentAnimation()
        {
            CancelInvoke(nameof(PlayCoinSound));
            if (countCoinsCoroutine != null) { StopCoroutine(countCoinsCoroutine); countCoinsCoroutine = null; }

            if (giveCoinsCoroutine != null)
            {
                StopCoroutine(giveCoinsCoroutine);
                giveCoinsCoroutine = null;
            }

            foreach (Transform coin in originalStates.Keys)
            {
                if (coin != null)
                {
                    coin.CancelAnimations();
                }
            }
        }

        private void PlayCoinSound()
        {
            AudioPlayer.Instance.PlayOneShot(AudioType.Coins);
        }

        private IEnumerator GiveCoins()
        {
            const int rewardIterations = 25;
            const float rewardInterval = 0.1f;

            for (int i = 0; i < rewardIterations; i++)
            {
                SaveManager.Currency.Value += 2;

                yield return new WaitForSeconds(
                    rewardInterval
                );
            }

            float rewardDuration =
                rewardIterations * rewardInterval;

            float remainingAnimationTime =
                animationDuration - rewardDuration;

            if (remainingAnimationTime > 0f)
            {
                yield return new WaitForSeconds(
                    remainingAnimationTime
                );
            }

            giveCoinsCoroutine = null;

            // Restore before invoking completion so it is ready
            // for the next call.
            ResetCoins();

            OnComplete?.Invoke();
        }

        private void OnDisable()
        {
            StopCurrentAnimation();
            ResetCoins();
        }
    }
}