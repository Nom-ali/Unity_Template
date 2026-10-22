using ACS.Animations;
using System;
using UnityEngine;

public class Logo : MonoBehaviour, IManagedAnimation, IManagedAnimationPreparation
{
    [SerializeField] private GameObject[] logos;
    [SerializeField] private bool ignoreParent;
    private Vector3[] originals;
    private AnimationHandle logoAnimation;
    public bool IgnoreParent => ignoreParent;
    public bool IsLooping => true;
    public bool IsAnimating => logoAnimation != null && logoAnimation.IsActive;
    public AnimationManager Controller { get; set; }
    private void Awake() => Cache();
    private void Cache()
    {
        if (logos == null || originals != null) return;
        originals = new Vector3[logos.Length];
        for (int i=0;i<logos.Length;i++) if(logos[i]!=null) originals[i]=logos[i].transform.localPosition;
    }
    private void OnEnable()
    { if (Controller == null && !AnimationManager.IsPreviewing) StartAnimation(.5f,false); }
    private Vector3[] preparedPositions;
    private bool prepared;

    public bool CapturePreparation(bool show, AnimationPlaybackOptions options, bool preserveCurrentState)
    {
        Cache();
        prepared = logos != null;
        if (!prepared) return false;
        if (preparedPositions == null || preparedPositions.Length != logos.Length) preparedPositions = new Vector3[logos.Length];
        for (int i = 0; i < logos.Length; i++)
            if (logos[i] != null) preparedPositions[i] = preserveCurrentState ? logos[i].transform.localPosition : originals[i];
        return true;
    }

    public void ApplyPreparation()
    {
        if (!prepared || logos == null) return;
        for (int i = 0; i < logos.Length; i++)
            if (logos[i] != null) logos[i].transform.localPosition = preparedPositions[i];
    }

    public void ClearPreparation() => prepared = false;

    public void InvalidatePreviewTargets()
    {
        if (Application.isPlaying) return;
        originals = null; prepared = false;
    }

    public void RefreshPreviewTargets()
    {
        if (Application.isPlaying) return;
        originals = null;
        Cache();
    }

    public void PlayManaged(bool show, AnimationPlaybackOptions options, Action complete)
    {
        if (!prepared)
        {
            CapturePreparation(show, options, IsAnimating);
            logoAnimation?.Cancel();
            ApplyPreparation();
        }
        else logoAnimation?.Cancel();
        logoAnimation = null;
        prepared = false;
        if (show && !options.reducedMotion)
            StartAnimation(options.overrideDuration ? Mathf.Max(.001f, options.duration) : .5f,
                options.overrideTimeScale && options.unscaled, true);
        if (options.useCustomCurve && options.curve != null) logoAnimation?.SetEase(options.curve);
        else if (options.overrideEase) logoAnimation?.SetEase(options.ease);
        complete?.Invoke();
    }
    public void StopManaged()
    {
        logoAnimation?.Cancel();logoAnimation=null;
        if (logos == null || originals == null) return;
        for(int i=0;i<logos.Length;i++) if(logos[i]!=null) logos[i].transform.localPosition=originals[i];
    }
    private void StartAnimation(float legDuration,bool unscaled, bool usePreparedPose = false)
    {
        if (!usePreparedPose) StopManaged();
        Cache();
        var sequence=AnimationTimeline.Sequence().SetOwner(this).SetUpdate(unscaled);
        int count=logos==null?0:logos.Length;
        for(int i=0;i<count;i++)
        {
            if(logos[i]==null)continue;
            var target=logos[i].transform;
            var wave=AnimationTimeline.Sequence().SetOwner(this);
            wave.Append(target.AnimateLocalMoveY(.1f,legDuration).SetOwner(this));
            wave.Append(target.AnimateLocalMoveY(originals[i].y,legDuration).SetOwner(this));
            sequence.Insert(i*.2f,wave);
        }
        float cycle=Mathf.Max(count*.2f+2,sequence.TotalDuration);
        sequence.AppendInterval(cycle-sequence.TotalDuration);
        logoAnimation=sequence.SetLoops(-1);
    }
    private void OnDisable()=>StopManaged();
    private void OnDestroy()=>StopManaged();
    private void OnValidate() => AnimationManager.NotifyConfigurationChanged();

}
