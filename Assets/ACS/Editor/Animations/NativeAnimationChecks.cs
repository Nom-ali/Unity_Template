#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ACS.Animations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ACS.Editor.Animations
{
    // Deterministic Edit Mode checks plus a separate real-frame Play Mode pass.
    public static class NativeAnimationChecks
    {
        private static int checks;
        public static float Now;
        private static readonly List<GameObject> objects=new();
        private static void Check(bool condition,string name)
        {if(!condition)throw new Exception("ACS animation check failed: "+name);checks++;}
        private static GameObject Object(string name,Transform parent=null)
        {var go=new GameObject(name,typeof(RectTransform));if(parent!=null)go.transform.SetParent(parent,false);objects.Add(go);return go;}
        private static void Set(object target,string name,object value)
        {
            var type=target.GetType();FieldInfo field=null;
            while(type!=null && field==null){field=type.GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);type=type.BaseType;}
            if(field==null)throw new Exception("Missing field "+name);field.SetValue(target,value);
        }
        private static void Pump(float delta,int frames=1)
        {for(int i=0;i<frames;i++){Now+=delta;AnimationRunner.Advance(delta,delta);}}
        private static void Cleanup()
        {
            AnimationPreview.Stop();AnimationRunner.CancelAll();AnimationRunner.Advance(0,0);
            for(int i=objects.Count-1;i>=0;i--)if(objects[i]!=null)UnityEngine.Object.DestroyImmediate(objects[i]);
            objects.Clear();Now=0;
        }
        [MenuItem("ACS/Animations/Run Edit Mode Checks")]
        public static void Run()
        {
            checks=0;
            try
            {
                BasicRuntime();Cleanup();
                Composition();Cleanup();
                ManagerModes();Cleanup();
                PanelLifecycle();Cleanup();
                PanelOwnership();Cleanup();
                MandatoryPreparation();Cleanup();
                PreparationReplacement();Cleanup();
                DecorativePreparation();Cleanup();
                PreparationPreview();Cleanup();
                PrefabMigration();Cleanup();
                Consumers();Cleanup();
                PreviewRestoration();Cleanup();
                Allocations();Cleanup();
                Debug.Log("ACS_NATIVE_ANIMATION_CHECKS_PASSED: "+checks);
            }
            finally{Cleanup();}
        }
        private static void BasicRuntime()
        {
            var target=Object("native runtime").transform;
            target.localScale=Vector3.one;int done=0,cancelled=0;
            var handle=target.AnimateScale(2,1).SetEase(Ease.Linear).SetDelay(.2f).OnComplete(()=>done++).OnCancelled(()=>cancelled++);
            Pump(.1f);Check(target.localScale==Vector3.one,"delay retains original pose");
            Pump(.6f);Check(Mathf.Abs(target.localScale.x-1.5f)<.001f,"linear interpolation after delay");
            handle.Pause();Pump(1);Check(Mathf.Abs(target.localScale.x-1.5f)<.001f,"pause freezes time");handle.Resume();Pump(.5f);
            Check(done==1 && handle.IsComplete && target.localScale==Vector3.one*2,"exact endpoint and one completion");handle.Cancel();Check(cancelled==0,"completed handle cannot become cancelled");
            target.localScale=Vector3.one;
            var old=target.AnimateScale(4,1).OnComplete(()=>done++);old.Cancel();
            var replacement=target.AnimateScale(3,.1f).SetEase(Ease.Linear);old.Cancel();Pump(.1f);
            Check(done==1 && target.localScale==Vector3.one*3 && replacement.IsComplete,"stale cancellation cannot affect replacement");
            var zero=target.AnimateScale(5,0).OnComplete(()=>done++);Pump(0);Check(zero.IsComplete && done==2,"zero duration completes once");
            var time=target.AnimateScale(6,1).SetEase(Ease.Linear).SetUpdate(true);AnimationRunner.Advance(0,.5f);
            Check(Mathf.Abs(target.localScale.x-5.5f)<.001f,"unscaled animation advances at time scale zero");time.Cancel();
            var scaled=target.AnimateScale(9,1).SetEase(Ease.Linear);AnimationRunner.Advance(0,.5f);Check(Mathf.Abs(target.localScale.x-5.5f)<.001f,"scaled animation pauses at time scale zero");scaled.Cancel();
            var owner=Object("owner").AddComponent<AnimationProbe>();var linked=target.AnimateScale(7,1).SetOwner(owner).OnComplete(()=>done++);owner.enabled=false;Pump(.1f);Check(linked.State==AnimationPlaybackState.Cancelled && done==2,"disabled owner cancels without completion");
            var destroyed=Object("destroyed").transform;var missing=destroyed.AnimateScale(3,1);UnityEngine.Object.DestroyImmediate(destroyed.gameObject);Pump(.1f);Check(missing.State==AnimationPlaybackState.Cancelled,"destroyed target cancels safely");
            foreach(Ease ease in Enum.GetValues(typeof(Ease)))Check(AnimationEasing.Evaluate(ease,0)==0 && AnimationEasing.Evaluate(ease,1)==1,"ease endpoints "+ease);
            Check((int)Ease.InBack==26 && (int)Ease.OutBack==27 && (int)LoopType.Yoyo==1,"serialized enum compatibility");
            target.localScale=Vector3.one;target.AnimateScale(2,.2f).SetEase(Ease.Linear).SetLoops(2,LoopType.Yoyo);Pump(.4f);Check(target.localScale==Vector3.one,"finite yoyo returns to start");
            target.AnimateScale(Vector3.one,.2f).SetRelative().SetEase(Ease.Linear).SetLoops(2,LoopType.Incremental);Pump(.4f);Check(target.localScale==Vector3.one*3,"relative incremental endpoint");
            int reentry=0;target.AnimateScale(4,0).OnComplete(()=>{reentry++;target.AnimateScale(5,0).OnComplete(()=>reentry++);});Pump(0);Pump(0);Check(reentry==2,"completion callback can schedule another animation");
            var graphic=Object("graphic").AddComponent<Image>();graphic.color=new Color(.2f,.3f,.4f,.8f);graphic.AnimateFade(0,.1f);Pump(.1f);Check(graphic.color.r==.2f && graphic.color.a==0,"fade preserves RGB");
            graphic.AnimateFillAmount(.25f,0);Pump(0);Check(graphic.fillAmount==.25f,"image fill extension");
            graphic.AnimateColor(new Color(.1f,0,0,0),0).SetRelative();Pump(0);Check(Mathf.Abs(graphic.color.r-.3f)<.001f,"relative color extension");
            var slider=Object("slider").AddComponent<Slider>();slider.AnimateValue(.7f,0);Pump(0);Check(Mathf.Abs(slider.value-.7f)<.001f,"slider extension");
            target.localPosition=new Vector3(1,2,3);target.AnimateShake(Vector3.one,.1f);Pump(.1f);Check(target.localPosition==new Vector3(1,2,3),"shake restores position at endpoint");
            var start=target.localScale;target.AnimatePunch(Vector3.one,.1f);Pump(.1f);Check(target.localScale==start,"punch restores scale at endpoint");
        }
        private static void Composition()
        {
            var target=Object("sequence").transform;target.localScale=Vector3.one;
            var sequence=AnimationTimeline.Sequence();
            sequence.Append(target.AnimateScale(2,.2f).SetEase(Ease.Linear));
            sequence.Append(target.AnimateScale(1,.2f).SetEase(Ease.Linear));sequence.SetLoops(2);
            Pump(.1f,2);Check(Mathf.Abs(target.localScale.x-2)<.001f,"sequence first leg");Pump(.1f,2);Pump(.1f,4);
            Check(sequence.IsComplete && Mathf.Abs(target.localScale.x-1)<.001f,"sequence restart loop and final pose");
            var owner=Object("sequence owner").AddComponent<AnimationProbe>();
            var paused=AnimationTimeline.Sequence().SetOwner(owner);paused.Append(target.AnimateScale(3,.2f).SetOwner(owner));
            AnimationRunner.PauseOwner(owner,true);Pump(.1f,2);AnimationRunner.PauseOwner(owner,false);Pump(.1f,3);
            Check(paused.IsComplete && target.localScale==Vector3.one*3,"sequence pause before first frame resumes nested handles");
        }
        private static AnimationManager Manager(string name,out AnimationProbe a,out AnimationProbe b,out AnimationProbe c)
        {
            var parent=Object(name);var manager=parent.AddComponent<AnimationManager>();
            a=Object("a",parent.transform).AddComponent<AnimationProbe>();b=Object("b",parent.transform).AddComponent<AnimationProbe>();c=Object("c",parent.transform).AddComponent<AnimationProbe>();
            manager.RefreshChildren();return manager;
        }
        private static void Mode(AnimationManager manager,ChildPlaybackMode mode,float delay=.2f)
        {Set(manager,"showSettings",new AnimationGroupSettings {mode=mode,sharedDelay=delay,unscaled=true});}
        private static void ManagerModes()
        {
            var manager=Manager("parallel",out var a,out var b,out var c);Mode(manager,ChildPlaybackMode.AllAtOnce);manager.Show();
            Check(a.Starts.Count==1 && b.Starts[0]==a.Starts[0] && c.Starts[0]==a.Starts[0],"all-at-once invokes every child immediately");manager.Stop();
            a.Starts.Clear();b.Starts.Clear();c.Starts.Clear();Mode(manager,ChildPlaybackMode.SeparateSharedDelay);Now=0;int complete=0;manager.Show(()=>complete++);Pump(.1f);Check(b.Starts.Count==0,"shared interval does not start child early");Pump(.1f);
            Check(b.Starts.Count==1 && Mathf.Abs(b.Starts[0]-.2f)<.001f && a.IsAnimating,"shared interval starts next without interrupting previous");Pump(.1f,2);Check(c.Starts.Count==1 && Mathf.Abs(c.Starts[0]-.4f)<.001f && complete==0,"third invocation at twice shared interval; completion waits");Pump(.1f,8);Check(complete==1,"overlapping group completes once after every finite animation");
            a.Starts.Clear();b.Starts.Clear();c.Starts.Clear();Mode(manager,ChildPlaybackMode.SeparateWaitForCompletion);Now=0;manager.Show();Pump(.1f,4);Check(b.Starts.Count==0,"sequential mode waits for actual completion");Pump(.1f,2);Check(b.Starts.Count==1 && b.Starts[0]>=.5f,"sequential starts after previous endpoint");manager.Stop();
            Set(b,"ignoreParent",true);manager.RefreshChildren();Check(manager.Children.Count==2 && b.Controller==null,"ignored child removed and ownership released");
            c.gameObject.SetActive(false);manager.RefreshChildren();Check(manager.Children.Count==2,"inactive children remain discoverable");
            var nested=Object("nested",manager.transform).AddComponent<AnimationManager>();var nestedChild=Object("owned by nested",nested.transform).AddComponent<AnimationProbe>();manager.RefreshChildren();nested.RefreshChildren();Check(manager.Children.Count==2 && nested.Children.Count==1 && nestedChild.Controller==nested,"nested manager owns its descendants");
            Set(manager,"reducedMotion",true);Mode(manager,ChildPlaybackMode.AllAtOnce);manager.Show();Pump(0,3);Check(!a.IsAnimating && a.transform.localScale==Vector3.one*2,"reduced motion reaches final pose");
            Set(manager,"reducedMotion",false);a.Loop=true;manager.Show();Pump(.1f,7);Check(manager.State==ManagerPlaybackState.Idle && a.IsAnimating,"infinite child does not block finite group completion");manager.Stop();Check(!a.IsAnimating,"stop cancels decorative loops");
            a.Loop=false;manager.Show();manager.Pause();var scale=a.transform.localScale;Pump(1);Check(a.transform.localScale==scale && manager.State==ManagerPlaybackState.Paused,"parent pause freezes children");manager.Resume();Pump(.1f,8);Check(manager.State==ManagerPlaybackState.Idle,"parent resumes cleanly");
            manager.Show();manager.Hide();Pump(.1f,15);Check(manager.State==ManagerPlaybackState.Idle,"hide replaces active show safely");
            a.Starts.Clear();c.Starts.Clear();Mode(manager,ChildPlaybackMode.SeparateSharedDelay,0);manager.Show();
            Check(a.Starts.Count==1 && c.Starts.Count==1 && a.Starts[0]==c.Starts[0],"zero shared delay invokes all children immediately");manager.Stop();
            Set(manager,"useHierarchyOrder",false);Set(manager,"children",new List<MonoBehaviour>{c,a});manager.RefreshChildren();
            var added=Object("new custom child",manager.transform).AddComponent<AnimationProbe>();manager.RefreshChildren();
            Check(manager.Children[0]==c && manager.Children[1]==a && manager.Children[2]==added,"custom order persists and new children append");
            var order=new List<string>();a.Started=()=>order.Add("a");c.Started=()=>order.Add("c");added.Started=()=>order.Add("new");
            Set(manager,"hideSettings",new AnimationGroupSettings{mode=ChildPlaybackMode.SeparateSharedDelay,sharedDelay=.1f});manager.Hide();Pump(.1f,2);
            Check(order.Count==3 && order[0]=="new" && order[1]=="a" && order[2]=="c","hide reverses custom invocation order");manager.Stop();
            Set(manager,"reducedMotion",true);a.Loop=true;manager.Show();Pump(0,3);
            Check(!a.IsAnimating && manager.State==ManagerPlaybackState.Idle,"reduced-motion decorative loop does not stay active");
        }
        private static void PanelLifecycle()
        {
            var go=Object("panel");go.transform.localScale=Vector3.one*1.4f;var panel=go.AddComponent<DoScale>();panel.Awake();
            int show=0,hide=0;panel.Show(()=>show++);Pump(.1f);panel.Hide(()=>hide++);Pump(.1f,5);
            Check(show==0 && hide==1 && !go.activeSelf,"interrupted show never completes; hide disables panel");
            panel.Show(()=>show++);Pump(.1f,5);Check(go.activeSelf && show==1 && Mathf.Abs(go.transform.localScale.x-1.4f)<.001f,"show reactivates and preserves authored scale");
            var group=Object("canvas fade").AddComponent<CanvasGroup>();group.alpha=0;var fade=group.gameObject.AddComponent<DoFadeCanvas>();fade.Awake();fade.Show();Pump(.1f,4);Check(group.alpha==1,"hidden authored canvas fades to visible fallback");
            var slideObject=Object("directional");var rect=(RectTransform)slideObject.transform;rect.anchoredPosition=new Vector2(12,34);var slide=slideObject.AddComponent<DoDirectional>();slide.Awake();slide.Show();Pump(.1f);slide.Show();Pump(.1f,5);Check(Vector2.Distance(rect.anchoredPosition,new Vector2(12,34))<.01f,"interrupted slide retains original destination");
        }
        private static void PanelOwnership()
        {
            var root=Object("owned root");var manager=root.AddComponent<AnimationManager>();
            var parent=root.AddComponent<DoScale>();parent.Awake();Set(parent,"HideDuration",.05f);
            var child=Object("owned child",root.transform).AddComponent<DoFadeCanvas>();child.Awake();Set(child,"HideDuration",.5f);
            manager.RefreshChildren();Check(manager.Children.Count==2 && manager.Children[0]==parent,"manager includes animation on its own object");
            int starts=0,completed=0;var started=new UnityEngine.Events.UnityEvent();started.AddListener(()=>starts++);Set(manager,"onShowStarted",started);
            child.Show(()=>completed++);Check(starts==1 && parent.IsAnimating && child.IsAnimating,"direct child Show routes once through group manager");
            Pump(.1f,5);Check(completed==1,"direct child callback waits for managed group");
            int hide=0;parent.Hide(()=>hide++);Pump(.1f,2);
            Check(root.activeSelf && child.gameObject.activeSelf && manager.State==ManagerPlaybackState.Playing,"fast parent Hide cannot disable unfinished descendants");
            Pump(.1f,6);Check(root.activeSelf && hide==1 && !child.gameObject.activeSelf,"managed root stays active by default and child deactivates after group");
            Set(child,"ignoreParent",true);manager.RefreshChildren();child.Show();manager.Stop();
            Check(child.Controller==null && child.IsAnimating && manager.Children.Count==1,"ignored child plays independently of manager Stop");child.StopManaged();
            var container=Object("inactive container",root.transform);var nested=Object("inactive nested panel",container.transform).AddComponent<DoScale>();
            container.SetActive(false);manager.RefreshChildren();manager.Show();Pump(.1f,5);
            Check(container.activeSelf && nested.gameObject.activeSelf && !nested.IsAnimating,"manager activates inactive ancestor containers before playing their panels");
            manager.Stop();

            var timing=Manager("manager delay",out var a,out var b,out var c);
            Set(timing,"childDelays",new List<AnimationChildDelay>{new AnimationChildDelay{animation=a,showDelay=.4f}});
            Mode(timing,ChildPlaybackMode.AllAtOnce);timing.Show();Check(a.Starts.Count==0 && b.Starts.Count==1,"per-animation delay does not block parallel siblings");
            Pump(.1f,4);Check(a.Starts.Count==1,"manager invokes delayed child at its offset");timing.Stop();
            a.Starts.Clear();b.Starts.Clear();c.Starts.Clear();
            Set(timing,"childDelays",new List<AnimationChildDelay>());Set(timing,"showSettings",new AnimationGroupSettings{startDelay=.2f,mode=ChildPlaybackMode.SeparateSharedDelay,sharedDelay=.2f});
            Now=0;timing.Show();Check(a.Starts.Count==0,"manager start delay postpones first invocation");Pump(.1f,4);
            Check(a.Starts.Count==1 && Mathf.Abs(a.Starts[0]-.2f)<.001f && b.Starts.Count==1 && Mathf.Abs(b.Starts[0]-.4f)<.001f,"group delay shifts shared cascade without changing interval");timing.Stop();

            var events=Object("events panel").AddComponent<DoScale>();events.Awake();int showStart=0,showDone=0,hideStart=0,hideDone=0;
            var onShow=new DefineEvents();Set(onShow,"AddEvents",EventTypes.OnStart|EventTypes.OnComplete);onShow.m_OnStart.AddListener(()=>showStart++);onShow.m_OnComplete.AddListener(()=>showDone++);
            var onHide=new DefineEvents();Set(onHide,"AddEvents",EventTypes.OnStart|EventTypes.OnComplete);onHide.m_OnStart.AddListener(()=>hideStart++);onHide.m_OnComplete.AddListener(()=>hideDone++);
            Set(events,"SelectShowEvents",onShow);Set(events,"SelectHideEvents",onHide);events.Show();Pump(.1f,4);events.Hide();Pump(.1f,4);
            Check(showStart==1 && showDone==1 && hideStart==1 && hideDone==1,"all four panel lifecycle events remain independent");
            onShow.m_OnStart.RemoveAllListeners();onShow.m_OnStart.AddListener(()=>events.Hide());int stale=0;events.Show(()=>stale++);Pump(.1f,4);
            Check(stale==0 && !events.gameObject.activeSelf,"OnStart replacement cancels stale playback and callback");
            var fillRoot=Object("fill root");var fillManager=fillRoot.AddComponent<AnimationManager>();var fill=fillRoot.AddComponent<DoFill>();fill.Awake();Set(fill,"Loops",-1);fillManager.RefreshChildren();
            fillManager.Show();Pump(.1f,4);Check(fillManager.State==ManagerPlaybackState.Idle && fill.IsAnimating,"infinite fill Show does not block group");
            fillManager.Hide();Pump(.1f,4);Check(fillManager.State==ManagerPlaybackState.Idle && !fill.IsAnimating,"infinite fill Show still has finite Hide");
            int fillDone=0;Set(fillManager,"showSettings",new AnimationGroupSettings{overrideDuration=true,duration=0});fillManager.Show(()=>fillDone++);
            Check(fillDone==0,"zero-duration fill waits for its actual endpoint before group completion");Pump(0,2);Check(fillDone==1 && fill.GetComponent<Image>().fillAmount==1,"zero-duration fill finishes once at endpoint");
            var directionalRoot=Object("curve root");var curveManager=directionalRoot.AddComponent<AnimationManager>();var directional=directionalRoot.AddComponent<DoDirectional>();Set(directional,"Scaling",true);directional.Awake();
            Set(curveManager,"showSettings",new AnimationGroupSettings{overrideDuration=true,duration=1,overrideEase=true,useCustomCurve=true,curve=AnimationCurve.Linear(0,0,1,1)});
            curveManager.Show();Pump(.5f);Check(Mathf.Abs(directional.transform.localScale.x-.5f)<.001f,"directional scale/move use custom curve once without compounding easing");curveManager.Stop();
            var uiRoot=Object("UI manager panel without root effect");var uiManager=uiRoot.AddComponent<AnimationManager>();var uiChild=Object("UI child",uiRoot.transform).AddComponent<DoScale>();uiManager.RefreshChildren();
            var playPanel=typeof(ACS.UIManagerBase).GetMethod("PlayPanelAnimation",BindingFlags.Static|BindingFlags.NonPublic);int uiDone=0;
            playPanel.Invoke(null,new object[]{uiRoot,true,(Action)(()=>uiDone++)});Check(uiChild.IsAnimating,"UI panel calls prefer manager even without a root Do script");Pump(.1f,4);Check(uiDone==1,"UI manager completion follows child group");
            foreach(var field in typeof(AnimationBase).GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
                Check(field.Name!="ChildList" && field.Name!="Callings" && field.Name!="InitialDelay","panel base contains no legacy scheduling field "+field.Name);
        }
        private static void MandatoryPreparation()
        {
            var root=Object("MainMenu preparation fixture");root.transform.localScale=Vector3.one*1.3f;
            var manager=root.AddComponent<AnimationManager>();var panel=root.AddComponent<DoScale>();
            var topObject=Object("TopBar",root.transform);var top=(RectTransform)topObject.transform;top.anchoredPosition=new Vector2(12,100);
            var topEffect=topObject.AddComponent<DoDirectional>();Set(topEffect,"OnShowDirection",DoDirectional.Direction.Up);
            var bottomObject=Object("BottomBar",root.transform);var bottom=(RectTransform)bottomObject.transform;bottom.anchoredPosition=new Vector2(5,-100);
            var bottomEffect=bottomObject.AddComponent<DoDirectional>();Set(bottomEffect,"OnShowDirection",DoDirectional.Direction.Down);
            var topTarget=top.anchoredPosition;var bottomTarget=bottom.anchoredPosition;
            Set(manager,"showSettings",new AnimationGroupSettings{mode=ChildPlaybackMode.SeparateWaitForCompletion,startDelay=.5f});
            bool groupSawPrepared=false;var groupStart=new UnityEngine.Events.UnityEvent();groupStart.AddListener(()=>groupSawPrepared=root.transform.localScale==Vector3.zero && top.anchoredPosition.y>topTarget.y && bottom.anchoredPosition.y<bottomTarget.y);Set(manager,"onShowStarted",groupStart);
            int starts=0;foreach(var effect in new AnimationBase[]{panel,topEffect,bottomEffect})
            {var events=new DefineEvents();Set(events,"AddEvents",EventTypes.OnStart);events.m_OnStart.AddListener(()=>starts++);Set(effect,"SelectShowEvents",events);}
            manager.Show();var preparedTop=top.anchoredPosition;var preparedBottom=bottom.anchoredPosition;
            Check(groupSawPrepared,"parent and both bars are prepared before manager Show-start event");
            Check(starts==0 && !panel.IsAnimating && !topEffect.IsAnimating && !bottomEffect.IsAnimating,"preparation starts no effects and sends no child events");
            Pump(.1f,2);Check(top.anchoredPosition==preparedTop && bottom.anchoredPosition==preparedBottom && root.transform.localScale==Vector3.zero,"all prepared poses hold during initial delay");
            Pump(.1f,3);Check(panel.IsAnimating && !topEffect.IsAnimating && starts==1,"only scheduled parent starts after initial delay");
            Pump(.1f,4);Check(bottom.anchoredPosition==preparedBottom,"last bar stays prepared while preceding effects animate");
            Pump(.1f,15);Check(Vector3.Distance(root.transform.localScale,Vector3.one*1.3f)<.001f && Vector2.Distance(top.anchoredPosition,topTarget)<.01f && Vector2.Distance(bottom.anchoredPosition,bottomTarget)<.01f,"prepared parent and bars finish at unchanged authored targets");
            manager.Show();Check(root.transform.localScale==Vector3.zero && top.anchoredPosition==preparedTop && bottom.anchoredPosition==preparedBottom,"every fresh Show prepares the complete group again");manager.Stop();
            Set(panel,"useCustomStartingScale",true);Set(panel,"startingScale",Vector3.one*.2f);manager.Show();Check(root.transform.localScale==Vector3.one*.2f,"custom Show scale is opt-in and prepared immediately");Pump(.1f,25);manager.Hide();Pump(.1f,20);Check(root.transform.localScale==Vector3.zero,"custom Show scale does not change zero Hide endpoint");

            var inactiveRoot=Object("inactive preparation root");inactiveRoot.SetActive(false);inactiveRoot.transform.localScale=Vector3.one*2.4f;
            var inactiveManager=inactiveRoot.AddComponent<AnimationManager>();var inactiveScale=inactiveRoot.AddComponent<DoScale>();
            var inactiveChild=Object("inactive child",inactiveRoot.transform);inactiveChild.transform.localScale=Vector3.one*1.7f;inactiveChild.SetActive(false);var inactiveChildScale=inactiveChild.AddComponent<DoScale>();
            Set(inactiveManager,"showSettings",new AnimationGroupSettings{mode=ChildPlaybackMode.SeparateSharedDelay,sharedDelay=.2f});inactiveManager.Show();
            Check(inactiveRoot.transform.localScale==Vector3.zero && inactiveChild.transform.localScale==Vector3.zero,"inactive parent and child are prepared before activation");
            Pump(.1f,9);Check(Vector3.Distance(inactiveRoot.transform.localScale,Vector3.one*2.4f)<.001f && Vector3.Distance(inactiveChild.transform.localScale,Vector3.one*1.7f)<.001f,"activation cannot recapture a prepared zero scale as its target");

            var ignored=Object("ignored effect",inactiveRoot.transform).AddComponent<DoScale>();Set(ignored,"ignoreParent",true);ignored.transform.localScale=Vector3.one*3;
            var nested=Object("nested manager",inactiveRoot.transform).AddComponent<AnimationManager>();var nestedEffect=Object("nested effect",nested.transform).AddComponent<DoScale>();nestedEffect.transform.localScale=Vector3.one*4;
            inactiveManager.Show();Check(ignored.transform.localScale==Vector3.one*3 && nestedEffect.transform.localScale==Vector3.one*4,"preparation excludes ignored effects and nested manager contents");inactiveManager.Stop();
            Set(topEffect,"Scaling",true);Set(topEffect,"useCustomStartingScale",true);Set(topEffect,"startingScale",Vector3.one*.3f);manager.Show();Check(top.localScale==Vector3.one*.3f,"directional scaling supports custom fresh starting scale");manager.Stop();
        }

        private static void PreparationReplacement()
        {
            var root=Object("toggle group");var manager=root.AddComponent<AnimationManager>();var panel=root.AddComponent<DoScale>();
            var barObject=Object("toggle bar",root.transform);var bar=(RectTransform)barObject.transform;bar.anchoredPosition=new Vector2(10,20);var barTarget=bar.anchoredPosition;
            var directional=barObject.AddComponent<DoDirectional>();Set(directional,"OnShowDirection",DoDirectional.Direction.Up);Set(directional,"OnHideDirection",DoDirectional.Direction.Down);
            var fade=Object("toggle fade",root.transform).AddComponent<DoFadeCanvas>();
            Set(manager,"showSettings",new AnimationGroupSettings{mode=ChildPlaybackMode.SeparateSharedDelay,sharedDelay=.2f,startDelay=.2f,overrideDuration=true,duration=1,overrideEase=true,ease=Ease.Linear});
            Set(manager,"hideSettings",new AnimationGroupSettings{mode=ChildPlaybackMode.SeparateSharedDelay,sharedDelay=.2f,startDelay=.3f,overrideDuration=true,duration=1,overrideEase=true,ease=Ease.Linear});
            int oldShow=0,oldHide=0,newShow=0;manager.Show(()=>oldShow++);Pump(.1f,6);
            var currentScale=root.transform.localScale;var currentPosition=bar.anchoredPosition;float currentAlpha=fade.GetComponent<CanvasGroup>().alpha;
            Check(currentScale.x>0 && currentScale.x<1,"toggle fixture interrupts an unfinished Show");
            manager.Hide(()=>oldHide++);Check(root.transform.localScale==currentScale && bar.anchoredPosition==currentPosition && fade.GetComponent<CanvasGroup>().alpha==currentAlpha,"Show-to-Hide captures current values before cancellation and preparation");
            Pump(.1f,2);Check(root.transform.localScale==currentScale && bar.anchoredPosition==currentPosition,"replacement holds current values through configured delay");
            Pump(.1f,5);manager.Pause();currentScale=root.transform.localScale;currentPosition=bar.anchoredPosition;currentAlpha=fade.GetComponent<CanvasGroup>().alpha;
            manager.Show(()=>newShow++);Check(root.transform.localScale==currentScale && bar.anchoredPosition==currentPosition && fade.GetComponent<CanvasGroup>().alpha==currentAlpha,"paused Hide-to-Show prepares current state without snapping");
            Pump(.1f,25);Check(oldShow==0 && oldHide==0 && newShow==1,"only latest replacement invokes successful group completion");
            Check(Vector3.Distance(root.transform.localScale,Vector3.one)<.001f && Vector2.Distance(bar.anchoredPosition,barTarget)<.01f && fade.GetComponent<CanvasGroup>().alpha==1,"repeated toggles retain scale, movement and alpha targets");
            for(int i=0;i<6;i++){if((i&1)==0)manager.Hide();else manager.Show();Pump(.1f,4);}
            manager.Show();Pump(.1f,25);Check(root.transform.localScale==Vector3.one && Vector2.Distance(bar.anchoredPosition,barTarget)<.01f,"rapid alternating replacements do not accumulate directional drift");

            var fill=Object("toggle fill").AddComponent<DoFill>();fill.Show();Pump(.1f);float value=fill.GetComponent<Image>().fillAmount;fill.Hide();Check(fill.GetComponent<Image>().fillAmount==value,"standalone Hide prepares interrupted fill value");fill.Show();Check(fill.GetComponent<Image>().fillAmount==value,"standalone Show also consumes current state on interruption");fill.StopManaged();
        }

        private static void DecorativePreparation()
        {
            var root=Object("queued decorative group");var manager=root.AddComponent<AnimationManager>();root.AddComponent<DoScale>();
            var buttonObject=Object("queued button",root.transform);buttonObject.SetActive(false);var button=buttonObject.AddComponent<ButtonAnimations>();
            var logoObject=Object("queued logo",root.transform);logoObject.SetActive(false);var logo=logoObject.AddComponent<Logo>();var letter=Object("queued letter",logo.transform);letter.transform.localPosition=Vector3.up*.4f;Set(logo,"logos",new[]{letter});
            Set(manager,"showSettings",new AnimationGroupSettings{startDelay=.3f,mode=ChildPlaybackMode.SeparateSharedDelay,sharedDelay=.2f});
            Set(manager,"hideSettings",new AnimationGroupSettings{startDelay=.3f,mode=ChildPlaybackMode.AllAtOnce});manager.Show();
            Check(!button.IsAnimating && !logo.IsAnimating && button.transform.localScale==new Vector3(1,.95f,1),"decorators prepare starting poses without running loops");
            Pump(.1f,6);Check(button.IsAnimating && !logo.IsAnimating && letter.transform.localPosition==Vector3.up*.4f,"decorative loops wait for their scheduled invocation");
            var buttonScale=button.transform.localScale;var letterPosition=letter.transform.localPosition;manager.Hide();
            Check(!button.IsAnimating && !logo.IsAnimating && button.transform.localScale==buttonScale && letter.transform.localPosition==letterPosition,"decorator stop cleanup cannot overwrite captured replacement values");
            Pump(.1f,2);Check(button.transform.localScale==buttonScale,"queued decorative replacement holds captured scale");manager.Stop();
        }

        private static void PreparationPreview()
        {
            if(Application.isPlaying)return;
            var root=Object("preview preparation");root.transform.localScale=Vector3.one*2;var manager=root.AddComponent<AnimationManager>();root.AddComponent<DoScale>();
            var barObject=Object("preview bar",root.transform);var rect=(RectTransform)barObject.transform;rect.anchoredPosition=new Vector2(3,4);var originalPosition=rect.anchoredPosition;barObject.AddComponent<DoDirectional>();
            Set(manager,"showSettings",new AnimationGroupSettings{overrideDuration=true,duration=1,overrideEase=true,ease=Ease.Linear});
            Set(manager,"hideSettings",new AnimationGroupSettings{overrideDuration=true,duration=1,overrideEase=true,ease=Ease.Linear});
            AnimationPreview.Start(manager,true);Check(root.transform.localScale==Vector3.zero && rect.anchoredPosition!=originalPosition,"preview prepares every eligible object immediately");Pump(.1f,3);
            var scale=root.transform.localScale;var position=rect.anchoredPosition;AnimationPreview.Start(manager,false);
            Check(root.transform.localScale==scale && rect.anchoredPosition==position,"preview Hide replacement preserves current state and snapshot");Pump(.1f,2);
            scale=root.transform.localScale;position=rect.anchoredPosition;AnimationPreview.Start(manager,true);Check(root.transform.localScale==scale && rect.anchoredPosition==position,"preview Show replacement does not reset starting poses");
            AnimationPreview.Stop();Check(root.transform.localScale==Vector3.one*2 && rect.anchoredPosition==originalPosition,"Stop Preview restores original snapshot after multiple replacements");
            root.transform.localScale=Vector3.one*1.6f;AnimationPreview.Start(manager,true);Pump(.1f,12);Check(Vector3.Distance(root.transform.localScale,Vector3.one*1.6f)<.001f,"new preview session recaptures edited authored target");AnimationPreview.Stop();Check(root.transform.localScale==Vector3.one*1.6f,"second preview restores its own original target");
        }

        private static void PrefabMigration()
        {
            if(Application.isPlaying)return;
            string[] paths={"Assets/ACS/Runtime/Prefabs/UIManager.prefab","Assets/ACS/Runtime/Prefabs/UI/Popup.prefab","Assets/ACS/Runtime/Prefabs/UI/4_LoadingPopup.prefab","Assets/ACS/Runtime/Prefabs/UI/3_setting.prefab","Assets/ACS/Runtime/Modules/MyTools/CharacterCreation/CharacterPanel.prefab"};
            int managers=0;
            foreach(var path in paths)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);Check(prefab!=null,"migrated prefab imports "+path);
                foreach(var manager in prefab.GetComponentsInChildren<AnimationManager>(true))
                {
                    managers++;manager.RefreshChildren();var parent=manager.GetComponent<AnimationBase>();bool contains=false;
                    foreach(var animation in manager.Children)if(animation==parent)contains=true;
                    Check(parent!=null && contains,"migrated prefab manager includes original parent effect");
                }
            }
            Debug.Log("ACS_MIGRATED_PREFAB_MANAGER_INSTANCES: "+managers);
            Check(managers>=8,"authored legacy groups and inherited prefab groups have managers");
        }
        private static void Consumers()
        {
            var button=Object("decorative button").AddComponent<ButtonAnimations>();
            string[] methods={"PlayBreathing","PlayPulse","PlayWiggle","PlayBounce","PlayHeartbeat"};
            foreach(var method in methods){button.GetType().GetMethod(method).Invoke(button,null);Pump(.05f,5);Check(button.IsAnimating,"button style "+method);button.StopAnimation();Check(!button.IsAnimating,"button style cleanup "+method);}
            button.PlayManaged(true,default,null);Pump(.1f);Check(button.IsAnimating,"default jelly style");button.StopManaged();
            var logo=Object("logo").AddComponent<Logo>();var letter=Object("letter",logo.transform);letter.transform.localPosition=Vector3.up*.3f;Set(logo,"logos",new[]{letter});logo.PlayManaged(true,default,null);Pump(.1f,4);Check(logo.IsAnimating,"logo loops without coroutine dependency");logo.StopManaged();Check(letter.transform.localPosition==Vector3.up*.3f,"logo stop restores initial position");
            var bar=Object("text bar").AddComponent<TextBarUI>();bar.Show();Pump(.1f,4);bar.Hide();bar.Show();Pump(.1f,4);
            Check(bar.gameObject.activeSelf && bar.transform.localScale==Vector3.one,"text bar replacement cancels stale hide");bar.Hide();Pump(.1f,4);Check(!bar.gameObject.activeSelf,"text bar hides after animation");
            var settingsObject=Object("settings slider");var settingsImage=settingsObject.AddComponent<Image>();var settings=settingsObject.AddComponent<SettingScriptSlider>();Set(settings,"settingSlider",settingsImage);
            settings.Show();Pump(.1f,6);Check(settingsImage.fillAmount==1,"settings slider animates show");
            settings.GetType().GetMethod("Hide",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(settings,null);settings.Show();Pump(.1f,6);Check(settingsImage.fillAmount==1,"settings show cancels stale hide callback");
            var coinObject=Object("coin controller");coinObject.SetActive(false);var coins=coinObject.AddComponent<NextLevelGames.CoinsAnimation>();Set(coins,"PlayOnEnabel",false);
            var pile=Object("coin pile");var coin=Object("coin",pile.transform).transform;coin.localPosition=new Vector3(2,3,0);Set(coins,"pileOfCoins",pile);
            coins.GetType().GetMethod("CacheOriginalStates",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(coins,null);coinObject.SetActive(true);
            coin.AnimateMove(new Vector3(50,50,0),1).SetOwner(coins);Pump(.1f);coins.GetType().GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(coins,null);
            Check(coin.localPosition==new Vector3(2,3,0),"coin cancellation restores cached pose without replaying reward coroutine");
            var value=Object("value transfer").AddComponent<UIValueTransferEffect>();var layer=Object("visual layer",value.transform).GetComponent<RectTransform>();var prefab=Object("visual prefab").GetComponent<RectTransform>();Set(value,"animationLayer",layer);Set(value,"itemPrefab",prefab);
            var spawn=value.GetType().GetMethod("SpawnCluster",BindingFlags.Instance|BindingFlags.NonPublic);
            spawn.Invoke(value,new object[]{2,Vector2.zero});Pump(.1f);value.StopCurrentEffect();Check(layer.GetComponentsInChildren<RectTransform>(false).Length==1,"value-transfer cancellation returns active visuals to pool");
        }
        private static void PreviewRestoration()
        {
            if(Application.isPlaying)return;
            var manager=Manager("preview",out var a,out var b,out var c);var start=a.transform.localScale;
            var panel=Object("preview panel",manager.transform).AddComponent<DoScale>();int events=0;
            var showEvents=new DefineEvents {m_OnStart=new UnityEngine.Events.UnityEvent(),m_OnComplete=new UnityEngine.Events.UnityEvent()};
            Set(showEvents,"AddEvents",EventTypes.OnStart|EventTypes.OnComplete);showEvents.m_OnStart.AddListener(()=>events++);showEvents.m_OnComplete.AddListener(()=>events++);Set(panel,"SelectShowEvents",showEvents);
            AnimationPreview.Start(manager,true);Pump(.1f,5);AnimationPreview.Stop();Check(a.transform.localScale==start && !AnimationPreview.Active,"preview restores serialized pose");
            Check(events==0,"preview does not invoke gameplay UnityEvents");
        }
        private static void Allocations()
        {
            var target=Object("allocation probe").transform;target.AnimateScale(2,1).SetLoops(-1,LoopType.Yoyo);
            Pump(.001f,32);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)AnimationRunner.Advance(.001f,.001f);long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Debug.Log("ACS_ANIMATION_RUNNER_WARMED_ALLOCATED_BYTES_1000_UPDATES: "+allocated);
            Check(allocated==0,"runner has zero warmed per-update managed allocations");
        }
        public static void CompareInstalledEasing()
        {
            Type type=null;
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
                if((type=assembly.GetType("DG.Tweening.Core.Easing.EaseManager"))!=null)break;
            if(type==null)throw new Exception("The optional baseline DLL is not installed in this validation project.");
            MethodInfo evaluate=null;
            foreach(var method in type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static))
                if(method.Name=="Evaluate" && method.GetParameters().Length==6){evaluate=method;break;}
            if(evaluate==null)throw new Exception("Unsupported optional baseline version.");
            Type easeType=evaluate.GetParameters()[0].ParameterType;
            float maximum=0;int samples=0;string failures="";
            foreach(Ease ease in Enum.GetValues(typeof(Ease)))
            {
                float error=0;
                for(int sample=0;sample<=100;sample++)
                {
                    float t=sample/100f;
                    object value=Enum.ToObject(easeType,ease==Ease.Unset?6:(int)ease);
                    float expected=(float)evaluate.Invoke(null,new object[]{value,null,t,1f,(int)ease>=32?1f:1.70158f,0f});
                    error=Mathf.Max(error,Mathf.Abs(expected-AnimationEasing.Evaluate(ease,t)));samples++;
                }
                maximum=Mathf.Max(maximum,error);
                if(error>.00002f)failures+=ease+"="+error+"; ";
            }
            Debug.Log("ACS_OPTIONAL_EASING_BASELINE_SAMPLES: "+samples+" MAX_ERROR: "+maximum);
            if(failures.Length>0)throw new Exception("Easing baseline mismatch: "+failures);
            Debug.Log("ACS_OPTIONAL_EASING_BASELINE_PASSED");
        }
        public static void StartPlayModeChecks()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            SessionState.SetBool("ACS.NativeAnimationPlayChecks",true);
            EditorApplication.playModeStateChanged+=OnPlayMode;
            EditorApplication.isPlaying=true;
        }
        private static void OnPlayMode(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("ACS.NativeAnimationPlayExit",false))
            {
                SessionState.SetBool("ACS.NativeAnimationPlayExit",false);
                int result=SessionState.GetInt("ACS.NativeAnimationPlayResult",1);
                EditorApplication.delayCall+=()=>EditorApplication.Exit(result);
                return;
            }
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("ACS.NativeAnimationPlayChecks",false))return;
            SessionState.SetBool("ACS.NativeAnimationPlayChecks",false);
            var go=new GameObject("ACS real-frame animation checks");go.AddComponent<AnimationRealFrameChecks>();
        }
        [InitializeOnLoadMethod]
        private static void ReattachPlayModeChecks()
        {
            EditorApplication.playModeStateChanged-=OnPlayMode;EditorApplication.playModeStateChanged+=OnPlayMode;
        }
    }
    public sealed class AnimationProbe : MonoBehaviour,IManagedAnimation
    {
        [SerializeField]private bool ignoreParent;
        public bool Loop;
        public readonly List<float> Starts=new();
        public Action Started;
        private AnimationHandle handle;
        public bool IgnoreParent=>ignoreParent;
        public bool IsLooping=>Loop;
        public bool IsAnimating=>handle!=null && handle.IsActive;
        public AnimationManager Controller {get;set;}
        public void PlayManaged(bool show,AnimationPlaybackOptions options,Action complete)
        {
            Starts.Add(NativeAnimationChecks.Now);Started?.Invoke();handle?.Cancel();
            if(Loop && options.reducedMotion){complete?.Invoke();return;}
            handle=transform.AnimateScale(show?2:0,options.reducedMotion?0:options.overrideDuration?options.duration:.5f).SetEase(Ease.Linear).SetUpdate(true).SetOwner(this).SetLoops(Loop && !options.reducedMotion?-1:1).OnComplete(complete);
        }
        public void StopManaged()=>handle?.Cancel();
    }
    public sealed class AnimationRealFrameChecks : MonoBehaviour
    {
        private IEnumerator Start()
        {
            int exit=0;GameObject root=null, preparedRoot=null;AnimationManager manager=null;
            AnimationProbe a=null,b=null;bool overlapped=false;
            try { NativeAnimationChecks.Run(); }
            catch(Exception error){Debug.LogException(error);exit=1;}
            // Allow editor startup and the first player-loop frame to settle before measuring.
            for(int i=0;i<5;i++)yield return null;
            if(exit==0)try
            {
                root=new GameObject("real-frame root",typeof(RectTransform));manager=root.AddComponent<AnimationManager>();
                a=new GameObject("real a",typeof(RectTransform)).AddComponent<AnimationProbe>();a.transform.SetParent(root.transform);
                b=new GameObject("real b",typeof(RectTransform)).AddComponent<AnimationProbe>();b.transform.SetParent(root.transform);
                b.Started=()=>overlapped=a.IsAnimating;
                var field=typeof(AnimationManager).GetField("showSettings",BindingFlags.Instance|BindingFlags.NonPublic);
                field.SetValue(manager,new AnimationGroupSettings {mode=ChildPlaybackMode.SeparateSharedDelay,sharedDelay=.1f,unscaled=true,overrideDuration=true,duration=2});
                manager.RefreshChildren();manager.Show();
            }
            catch(Exception error){Debug.LogException(error);exit=1;}
            float deadline=Time.realtimeSinceStartup+10;
            while(exit==0 && b.Starts.Count==0 && Time.realtimeSinceStartup<deadline)yield return null;
            if(exit==0 && (a.Starts.Count!=1 || b.Starts.Count!=1 || !overlapped))
            { Debug.LogError("ACS real-frame overlap failed: a="+a.Starts.Count+" b="+b.Starts.Count+" overlapped="+overlapped+" state="+manager.State);exit=1; }
            while(exit==0 && manager.State!=ManagerPlaybackState.Idle && Time.realtimeSinceStartup<deadline)yield return null;
            if(exit==0 && manager.State!=ManagerPlaybackState.Idle){Debug.LogError("ACS real-frame completion timed out");exit=1;}
            AnimationManager preparedManager=null;RectTransform preparedBar=null;Vector2 barTarget=default;Vector3 beforeToggle=default;
            if(exit==0)try
            {
                preparedRoot=new GameObject("real preparation root",typeof(RectTransform));preparedManager=preparedRoot.AddComponent<AnimationManager>();preparedRoot.AddComponent<DoScale>();
                var barObject=new GameObject("real preparation bar",typeof(RectTransform));barObject.transform.SetParent(preparedRoot.transform,false);preparedBar=(RectTransform)barObject.transform;preparedBar.anchoredPosition=new Vector2(8,20);barTarget=preparedBar.anchoredPosition;barObject.AddComponent<DoDirectional>();
                typeof(AnimationManager).GetField("showSettings",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(preparedManager,new AnimationGroupSettings{startDelay=.1f,mode=ChildPlaybackMode.SeparateSharedDelay,sharedDelay=.2f,overrideDuration=true,duration=2,overrideChildTimeScale=true,unscaled=true});
                typeof(AnimationManager).GetField("hideSettings",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(preparedManager,new AnimationGroupSettings{startDelay=.1f,mode=ChildPlaybackMode.SeparateSharedDelay,sharedDelay=.2f,overrideDuration=true,duration=.3f,overrideChildTimeScale=true,unscaled=true});
                preparedManager.Show();
                if(preparedRoot.transform.localScale!=Vector3.zero || preparedBar.anchoredPosition==barTarget)throw new Exception("Real-frame Show did not immediately prepare root and queued bar.");
            }
            catch(Exception error){Debug.LogException(error);exit=1;}
            deadline=Time.realtimeSinceStartup+10;
            while(exit==0 && preparedRoot.transform.localScale.x<=0 && Time.realtimeSinceStartup<deadline)yield return null;
            if(exit==0)try
            {
                beforeToggle=preparedRoot.transform.localScale;var barBefore=preparedBar.anchoredPosition;preparedManager.Hide();
                if(preparedRoot.transform.localScale!=beforeToggle || preparedBar.anchoredPosition!=barBefore)throw new Exception("Real-frame replacement reset a current pose.");
            }
            catch(Exception error){Debug.LogException(error);exit=1;}
            while(exit==0 && preparedManager.State!=ManagerPlaybackState.Idle && Time.realtimeSinceStartup<deadline)yield return null;
            if(exit==0 && preparedManager.State!=ManagerPlaybackState.Idle){Debug.LogError("Real-frame prepared replacement timed out");exit=1;}
            if(exit==0)Debug.Log("ACS_NATIVE_PREPARATION_REALFRAME_PASSED");
            Debug.Log(exit==0?"ACS_NATIVE_ANIMATION_PLAYMODE_PASSED":"ACS_NATIVE_ANIMATION_PLAYMODE_FAILED");
            if(root!=null)Destroy(root);
            if(preparedRoot!=null)Destroy(preparedRoot);
            SessionState.SetInt("ACS.NativeAnimationPlayResult",exit);
            SessionState.SetBool("ACS.NativeAnimationPlayExit",true);
            EditorApplication.isPlaying=false;
        }
    }
}
#endif
