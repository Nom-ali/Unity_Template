#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using ACS.Animations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ACS.Editor.Animations
{
    [InitializeOnLoad]
    public static class AnimationPreview
    {
        private sealed class Snapshot
        {
            public Transform Target;public Vector3 Position,Scale;public Quaternion Rotation;public Vector2 Anchored;
            public GameObject Object;public bool Active;
        }
        private static readonly List<Snapshot> poses=new();
        private static readonly Dictionary<Component,string> serialized=new();
        private static AnimationManager manager;
        private static double lastTime;
        public static bool Active=>manager!=null;
        static AnimationPreview()
        {
            Selection.selectionChanged+=Stop;
            AssemblyReloadEvents.beforeAssemblyReload+=Stop;
            EditorApplication.playModeStateChanged+=_=>Stop();
            EditorApplication.quitting+=Stop;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving+=(scene,path)=>Stop();
        }
        public static void Start(AnimationManager target,bool show)
        {
            if(Application.isPlaying || target==null)return;
            if(manager == target)
            {
                lastTime = EditorApplication.timeSinceStartup;
                if(show)target.Show();else target.Hide();
                return;
            }
            Stop();
            if(EditorUtility.IsPersistent(target)) { Debug.LogWarning("Open the prefab in Prefab Mode, or use a scene instance, to preview animations.");return; }
            manager=target;AnimationManager.IsPreviewing=true;
            try
            {
                foreach(var transform in target.GetComponentsInChildren<Transform>(true))
                {
                    poses.Add(new Snapshot {Target=transform,Position=transform.localPosition,Rotation=transform.localRotation,
                        Scale=transform.localScale,Anchored=transform is RectTransform rect?rect.anchoredPosition:default,
                        Object=transform.gameObject,Active=transform.gameObject.activeSelf});
                    foreach(var component in transform.GetComponents<Component>())
                        if(component is IManagedAnimation || component is Graphic || component is CanvasGroup || component is Slider)
                            serialized[component]=EditorJsonUtility.ToJson(component);
                }
                target.RefreshChildren();
                foreach(var child in target.Children)
                {
                    var refresh = child.GetType().GetMethod("RefreshPreviewTargets", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                    if (refresh != null) refresh.Invoke(child, null);
                    else child.GetType().GetMethod("Awake", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.Invoke(child, null);
                }
                lastTime=EditorApplication.timeSinceStartup;
                EditorApplication.update+=Update;
                if(show)target.Show();else target.Hide();
            }
            catch(Exception error){Stop();Debug.LogException(error);}
        }
        private static void Update()
        {
            if(manager==null){Stop();return;}
            double now=EditorApplication.timeSinceStartup;float delta=(float)Math.Min(.1,now-lastTime);lastTime=now;
            AnimationRunner.Advance(delta,delta);SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();
        }
        public static void Stop()
        {
            EditorApplication.update-=Update;
            if(manager==null && poses.Count==0)return;
            if(manager!=null)manager.Stop();
            AnimationRunner.CancelAll();
            foreach(var pair in serialized)if(pair.Key!=null)EditorJsonUtility.FromJsonOverwrite(pair.Value,pair.Key);
            // Restore parent states last, after child transforms and serialized visuals.
            for(int i=poses.Count-1;i>=0;i--)
            {
                var pose=poses[i];if(pose.Target==null)continue;
                pose.Target.localPosition=pose.Position;pose.Target.localRotation=pose.Rotation;pose.Target.localScale=pose.Scale;
                if(pose.Target is RectTransform rect)rect.anchoredPosition=pose.Anchored;
                if(pose.Object.activeSelf!=pose.Active)pose.Object.SetActive(pose.Active);
            }
            foreach(var pair in serialized)
                if(pair.Key != null)
                    pair.Key.GetType().GetMethod("InvalidatePreviewTargets", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.Invoke(pair.Key, null);
            poses.Clear();serialized.Clear();manager=null;AnimationManager.IsPreviewing=false;
        }
    }
}
#endif
