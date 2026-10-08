using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using System;
using MyBox;
using TMPro;

namespace ACS.LoadingManager
{
    public class SceneTransition : MonoBehaviour
    {
        private AnimationBase transitionAnimation;

        [SerializeField] private FillBarType FillBarType = FillBarType.None;
        [ConditionalField(nameof(FillBarType), false, FillBarType.FillImage)]
        [SerializeField] private Image FillBar;
        [ConditionalField(nameof(FillBarType), false, FillBarType.Slider)]
        [SerializeField] private Slider Slider;
        [SerializeField] private TMP_Text ProgressText;
        [SerializeField] private string ProgressTextFormat = "{0}%";

        [Range(0f, 9f)]
        [SerializeField] private float Duration = 1;

        [Range(0f, 1f)]
        [SerializeField] private float FadeDuration = 1;

        [Space]
        [SerializeField] private GameObject[] DotList;

        private HashSet<int> AdditiveLoadedScene = new();

        void Start()
        {
            AutoAssignProgressText();
            StartCoroutine(AnimateDots());
        }

        internal void Show()
        {
            try {
                if (transitionAnimation == null)
                {
                    transitionAnimation = GetComponent<AnimationBase>();
                    if (transitionAnimation == null)
                    {
                        Debug.LogError("ACSSceneTransition AnimationBase is not assigned or found.");
                        return;
                    }
                }
                if(transitionAnimation)
                    transitionAnimation.Show();
                else
                    gameObject.SetActive(true);
            }
            catch
            {
                gameObject.SetActive(true);
            }
        }

        internal void Hide()
        {
            try
            {
                if (transitionAnimation == null)
                {
                    transitionAnimation = GetComponent<AnimationBase>();
                    if (transitionAnimation == null)
                    {
                        Debug.LogError("ACSSceneTransition AnimationBase is not assigned or found.");
                        return;
                    }
                }
                if(transitionAnimation)
                    transitionAnimation.Hide();
                else
                    gameObject.SetActive(false);
            }
            catch
            {
                gameObject.SetActive(false);
            }
        }

        public void LoadScene(int sceneIndex, ButtonActionSimple onComplete = null)
        {
            Show();
            StartCoroutine(Load_Scene(sceneIndex, LoadSceneMode.Single, onComplete));
        }
        public void LoadScene(string sceneName, ButtonActionSimple onComplete = null)
        {
            Show();
            var sceneID = SceneUtility.GetBuildIndexByScenePath(sceneName);
            StartCoroutine(Load_Scene(sceneID, LoadSceneMode.Single, onComplete));
        }

        public void LoadScene(int sceneIndex, LoadSceneMode mode, ButtonActionSimple onComplete = null)
        {
            Show();
            StartCoroutine(Load_Scene(sceneIndex, mode, onComplete));
        }

        public void LoadScene(string sceneName, LoadSceneMode mode, ButtonActionSimple onComplete = null)
        {
            Show();
            var sceneID = SceneUtility.GetBuildIndexByScenePath(sceneName);
            StartCoroutine(Load_Scene(sceneID, mode, onComplete));
        }

        IEnumerator Load_Scene(int sceneIndex, LoadSceneMode mode ,ButtonActionSimple onComplete)
        {
            yield return UnloadAdditiveScene();

            if (FillBar) FillBar.fillAmount = 0;
            if (Slider) Slider.value = 0f;
            UpdateProgressText(0f);

            float TimeElapse = 0;
            while (TimeElapse < Duration)
            {
                TimeElapse += Time.unscaledDeltaTime;
                float value = TimeElapse / Duration;
                Filler(value);
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);
            if (mode == LoadSceneMode.Additive)
                AdditiveLoadedScene.Add(sceneIndex);
            SceneManager.LoadScene(sceneIndex, mode);

            yield return new WaitForSeconds(.3f);
            UIManager.Instance.OnButtonClicked(onComplete);
            yield return FadeOut(0.1f);
        }

        public void LoadingAsync(int sceneIndex, bool manuallyFade = false, ButtonActionSimple onComplete = null)
        {
            Show();
            StartCoroutine(LoadSceneAsync(sceneIndex, manuallyFade, Duration, LoadSceneMode.Single, onComplete));
        }

        public void LoadingAsync(string sceneName, bool manuallyFade = false, ButtonActionSimple onComplete = null)
        {
            Show();
            var sceneID = SceneUtility.GetBuildIndexByScenePath(sceneName);
            StartCoroutine(LoadSceneAsync(sceneID, manuallyFade, Duration, LoadSceneMode.Single, onComplete));
        }
        
        public void LoadingAsync(int sceneIndex, LoadSceneMode mode, bool manuallyFade = false, ButtonActionSimple onComplete = null)
        {
            Show();
            StartCoroutine(LoadSceneAsync(sceneIndex, manuallyFade, Duration, mode, onComplete));
        }

        public void LoadingAsync(string sceneName, LoadSceneMode mode, bool manuallyFade = false, ButtonActionSimple onComplete = null)
        {
            Show();
            var sceneID = SceneUtility.GetBuildIndexByScenePath(sceneName);
            StartCoroutine(LoadSceneAsync(sceneID, manuallyFade, Duration, mode, onComplete));
        }

        IEnumerator LoadSceneAsync(int sceneIndex, bool manuallyFade, float delay, LoadSceneMode mode, ButtonActionSimple onComplete)
        {
            yield return UnloadAdditiveScene();
#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY
            AdsManager.AdsManager.Instance.ShowBigBannerAds();
#endif
            AudioPlayer.Instance?.StopMusic();

            if (FillBar) FillBar.fillAmount = 0;
            if (Slider) Slider.value = 0f;
            UpdateProgressText(0f);

            float value = 0;
            float oldDuration = delay;
            while (delay > 0)
            {
                delay -= Time.deltaTime;
                value = (oldDuration - delay) / oldDuration * 0.5f;
                Filler(value);
                yield return null;
            }

            if (mode == LoadSceneMode.Additive)
                AdditiveLoadedScene.Add(sceneIndex);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex, mode);
            operation.allowSceneActivation = false;

            yield return new WaitUntil(() => operation.progress >= value);

            while (!operation.isDone && operation.progress >= value)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                Filler(progress);
                if (operation.progress >= 0.9f)
                {
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }

            UIManager.Instance.OnButtonClicked(onComplete);
            
            if (!manuallyFade)
            {
#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY
                ACS.AdsManager.AdsManager.Instance.DestroyBigBanner();
#endif

                yield return FadeOut(FadeDuration);
            }
        }

        internal void FakeLoading(Action onComplete)
        {
            CallFakeLoading(onComplete);
        }

        internal void FakeLoading(ButtonActionSimple onComplete)
        {
            CallFakeLoading(onComplete);
        }
            
        internal void CallFakeLoading(Action onComplete)
        {
            Show();
            StartCoroutine(_FakeLoading(new(), onComplete));
        }
        
        internal void CallFakeLoading(ButtonActionSimple onComplete)
        {
            Show();
            StartCoroutine(_FakeLoading(onComplete, null));
        }

        private IEnumerator _FakeLoading(ButtonActionSimple onComplete, Action action)
        {

            if (FillBar) FillBar.fillAmount = 0;
            if (Slider) Slider.value = 0f;
            UpdateProgressText(0f);

            yield return new WaitUntil(() => gameObject.activeInHierarchy);

            float TimeElapse = 0;
            while (TimeElapse < Duration)
            {
                TimeElapse += Time.unscaledDeltaTime;
                float value = TimeElapse / Duration;
                Filler(value);
                yield return null;
            }
            yield return new WaitForSeconds(0.1f);

            // OnComplete Ation
            action?.Invoke();
            Hide();
            UIManager.Instance.OnButtonClicked(onComplete);
        }

        public IEnumerator UnloadAdditiveScene()
        {
            if (AdditiveLoadedScene.Count <= 0)
            {
                Debug.Log("No Additive Scene Loaded");
                yield break;
            }
            var toRemove = new List<int>();
            foreach (var sceneIndex in AdditiveLoadedScene)
            {
                Scene sceneToUnload = SceneManager.GetSceneByBuildIndex(sceneIndex);

                if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
                {
                    AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(sceneIndex);
                    while (!unloadOp.isDone)
                    {
                        yield return null;
                    }
                    toRemove.Add(sceneIndex);
                }
                else
                {
                    Debug.LogWarning($"Scene with index {sceneIndex} is not valid or not loaded.");
                }
            }
            foreach (var idx in toRemove)
                AdditiveLoadedScene.Remove(idx);
        }

        internal void UnloadAdditiveScene(string sceneName, ButtonActionSimple onComplete = null)
        {
            Show();
            StartCoroutine(_UnloadAdditiveScene(sceneName, onComplete));
        }

        private IEnumerator _UnloadAdditiveScene(string sceneName, ButtonActionSimple onComplete)
        {
            var sceneID = SceneUtility.GetBuildIndexByScenePath(sceneName);
            Scene sceneToUnload = SceneManager.GetSceneByBuildIndex(sceneID);

            if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(sceneName);
                while (!unloadOp.isDone)
                {
                    yield return null;
                }
                AdditiveLoadedScene.Remove(sceneID);
            }
            else
            {
                Debug.LogWarning($"Scene with Name: {sceneName} is not valid or not loaded.");
            }

            UIManager.Instance.OnButtonClicked(onComplete);
            yield return FadeOut(0.1f);
        }

        private IEnumerator UnloadScene(int sceneIndex)
        {
            if (!SceneManager.GetSceneByBuildIndex(sceneIndex).isLoaded)
                yield break;

            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(sceneIndex);
            if (unloadOp == null)
            {
                Debug.LogWarning($"SceneTransition could not unload scene index {sceneIndex}. Unity requires at least one scene to remain loaded.");
                yield break;
            }

            while (!unloadOp.isDone)
                yield return null;

            AdditiveLoadedScene.Remove(sceneIndex);
        }

        public void ReloadScene(ButtonActionSimple onComplete = null)
        {
            Show();
            StartCoroutine(ReloadGenericScene(onComplete));
        }

        public void LoadSceneByName(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        public void LoadSceneByIndex(int buildIndex)
        {
            if (buildIndex >= 0)
            {
                SceneManager.LoadScene(buildIndex);
            }
        }

        public void ReloadCurrentScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadNextScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        public void LoadPreviousScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
        }

        private IEnumerator ReloadGenericScene(ButtonActionSimple onComplete)
        {
            // Try to find the first valid loaded additive scene
            int additiveSceneIndex = -1;
            foreach (var index in AdditiveLoadedScene)
            {
                Scene scene = SceneManager.GetSceneByBuildIndex(index);
                if (scene.IsValid() && scene.isLoaded)
                {
                    additiveSceneIndex = index;
                    break;
                }
            }

            if (additiveSceneIndex != -1)
            {
                Debug.Log($"[ReloadScene] Reloading additive scene: {SceneManager.GetSceneByBuildIndex(additiveSceneIndex).name}");
                yield return UnloadScene(additiveSceneIndex);
                yield return new WaitForSeconds(0.2f);
                LoadScene(additiveSceneIndex, LoadSceneMode.Additive, onComplete);
                yield break;
            }

            int activeSceneIndex = SceneManager.GetActiveScene().buildIndex;
            Debug.Log($"[ReloadScene] Reloading active scene: {SceneManager.GetActiveScene().name}");
            LoadScene(activeSceneIndex, LoadSceneMode.Single, onComplete);
        }

        void Filler(float value)
        {
            value = Mathf.Clamp01(value);

            switch (FillBarType)
            {
                case FillBarType.FillImage:
                    if (FillBar) FillBar.fillAmount = value;
                    break;

                case FillBarType.Slider:
                    if (Slider) Slider.value = value;
                    break;

                default:
                    break;
            }

            UpdateProgressText(value);
        }

        private void UpdateProgressText(float value)
        {
            if (!ProgressText)
                return;

            int percentage = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
            ProgressText.text = string.Format(ProgressTextFormat, percentage);
        }

        private void AutoAssignProgressText()
        {
            if (ProgressText)
                return;

            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name.Contains("Progress", StringComparison.OrdinalIgnoreCase) ||
                    text.name.Contains("LoadingText", StringComparison.OrdinalIgnoreCase) ||
                    text.name.Contains("Loading", StringComparison.OrdinalIgnoreCase))
                {
                    ProgressText = text;
                    UpdateProgressText(0f);
                    return;
                }
            }
        }

        public void FadeOutLoadingScreen(float duration = 1)
        {

#if ACS_ADS || ACS_ADMOB || ACS_APPLOVIN || ACS_UNITY_ADS_LEGACY
            AdsManager.AdsManager.Instance.DestroyBigBanner();
#endif

            StartCoroutine(FadeOut(duration));
        }

        IEnumerator FadeOut(float duration = 1)
        {
            Hide();
            yield return new WaitForSeconds(duration);
        }

        IEnumerator AnimateDots()
        {
            if (DotList == null || DotList.Length <= 0)
                yield break;

            Array.ForEach(DotList, dot => dot.SetActive(false));

            while (true)
            {
                for (int i = 0; i < DotList.Length; i++)
                {
                    DotList[i].SetActive(true);
                    yield return new WaitForSeconds(0.4f);
                }
                Array.ForEach(DotList, dot => dot.SetActive(false));
                yield return null;
            }
        }
    }
}

[Serializable]
public enum FillBarType
{
    None, FillImage, Slider
}



