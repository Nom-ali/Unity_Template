#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ACS.Animations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace ACS.Editor.Animations
{
    [CustomEditor(typeof(AnimationManager))]
    public sealed class AnimationManagerEditor : UnityEditor.Editor
    {
        private ReorderableList list;
        private void OnEnable()
        {
            list=new ReorderableList(serializedObject,serializedObject.FindProperty("children"),true,true,false,false);
            list.drawHeaderCallback=r=>EditorGUI.LabelField(r,"Managed Animations — This Object and Descendants");
            list.drawElementCallback=(r,i,a,f)=>
            {
                var row=list.serializedProperty.GetArrayElementAtIndex(i);
                r.height=EditorGUIUtility.singleLineHeight;
                using (new EditorGUI.DisabledScope(true)) EditorGUI.PropertyField(r,row,new GUIContent((i+1)+"."));
            };
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var manager=(AnimationManager)target;
            using(new EditorGUI.DisabledScope(AnimationPreview.Active))
            {
                EditorGUILayout.LabelField("Child Discovery",EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Includes animations on this GameObject and inactive descendants. Nested managers own their own animations. Ignore Parent leaves an effect independent.",MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useHierarchyOrder"),new GUIContent("Use Hierarchy Order"));
                using(new EditorGUI.DisabledScope(serializedObject.FindProperty("useHierarchyOrder").boolValue)) list.DoLayoutList();
                if(GUILayout.Button("Refresh Children"))
                { serializedObject.ApplyModifiedProperties();Undo.RecordObject(manager,"Refresh Animation Children");manager.RefreshChildren();EditorUtility.SetDirty(manager);serializedObject.Update(); }
                ChildDelays(manager);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnEnable"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("reverseHideOrder"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("deactivateParentAfterHide"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("reducedMotion"),new GUIContent("Reduced Motion","Complete finite effects without movement and leave decorative loops stopped."));
                Group("Show",serializedObject.FindProperty("showSettings"),manager);
                Group("Hide",serializedObject.FindProperty("hideSettings"),manager);
                EditorGUILayout.Space();EditorGUILayout.LabelField("Events",EditorStyles.boldLabel);
                foreach(var name in new[]{"onShowStarted","onShowComplete","onHideStarted","onHideComplete"}) EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
            }
            serializedObject.ApplyModifiedProperties();
            Warnings(manager);
            EditorGUILayout.Space();EditorGUILayout.LabelField("Playback",EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Status",AnimationPreview.Active?"Editor Preview":manager.State.ToString());
            if(manager.CurrentChild!=null) EditorGUILayout.ObjectField("Current Child",manager.CurrentChild,typeof(MonoBehaviour),true);
            using(new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button(Application.isPlaying?"Show":"Preview Show")) {if(Application.isPlaying)manager.Show();else AnimationPreview.Start(manager,true);}
                if(GUILayout.Button(Application.isPlaying?"Hide":"Preview Hide")) {if(Application.isPlaying)manager.Hide();else AnimationPreview.Start(manager,false);}
                if(GUILayout.Button(Application.isPlaying?"Stop":"Stop Preview")) {if(Application.isPlaying)manager.Stop();else AnimationPreview.Stop();}
            }
            if(Application.isPlaying) using(new EditorGUILayout.HorizontalScope())
            { if(GUILayout.Button("Pause"))manager.Pause();if(GUILayout.Button("Resume"))manager.Resume(); }
            if(Application.isPlaying || AnimationPreview.Active) Repaint();
        }
        private static void Group(string title,SerializedProperty settings,AnimationManager manager)
        {
            EditorGUILayout.Space();EditorGUILayout.LabelField(title+" Playback",EditorStyles.boldLabel);
            var mode=settings.FindPropertyRelative("mode");EditorGUILayout.PropertyField(mode);
            var start=settings.FindPropertyRelative("startDelay");EditorGUILayout.PropertyField(start,new GUIContent("Start Delay"));
            int count=manager.Children.Count;
            if(mode.enumValueIndex==(int)ChildPlaybackMode.SeparateSharedDelay)
            {
                var delay=settings.FindPropertyRelative("sharedDelay");EditorGUILayout.PropertyField(delay,new GUIContent("Shared Start Interval"));
                string summary="Starts: ";int shown=Mathf.Min(count,6);
                for(int i=0;i<shown;i++)
                {
                    bool show=title=="Show";
                    int index=!show && new SerializedObject(manager).FindProperty("reverseHideOrder").boolValue ? count-1-i : i;
                    float offset=Mathf.Max(0,start.floatValue)+i*Mathf.Max(0,delay.floatValue)+manager.GetChildDelay(manager.Children[index],show);
                    summary+=(i==0?"":", ")+offset.ToString("0.###")+"s";
                }
                if(count>shown)summary+=", …";
                EditorGUILayout.HelpBox(summary+". Earlier animations keep running.",MessageType.None);
            }
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("unscaled"),new GUIContent("Schedule With Unscaled Time"));
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("overrideChildTimeScale"));
            var duration=settings.FindPropertyRelative("overrideDuration");EditorGUILayout.PropertyField(duration);
            if(duration.boolValue)EditorGUILayout.PropertyField(settings.FindPropertyRelative("duration"));
            var ease=settings.FindPropertyRelative("overrideEase");EditorGUILayout.PropertyField(ease);
            if(ease.boolValue)
            {
                var custom=settings.FindPropertyRelative("useCustomCurve");EditorGUILayout.PropertyField(custom);
                EditorGUILayout.PropertyField(settings.FindPropertyRelative(custom.boolValue?"curve":"ease"));
            }
        }
        private void ChildDelays(AnimationManager manager)
        {
            var delays = serializedObject.FindProperty("childDelays");
            delays.isExpanded = EditorGUILayout.Foldout(delays.isExpanded, "Per-Animation Delays", true);
            if (!delays.isExpanded) return;
            EditorGUILayout.HelpBox("All delays live here. In parallel/shared mode, a delay offsets only that animation. In completion mode, it waits after the preceding animation finishes.", MessageType.None);
            foreach (var child in manager.Children)
            {
                for (int i = 0; i < delays.arraySize; i++)
                {
                    var entry = delays.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("animation").objectReferenceValue != child) continue;
                    EditorGUILayout.LabelField(child.name + " / " + child.GetType().Name, EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("showDelay"), new GUIContent("Show Delay"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("hideDelay"), new GUIContent("Hide Delay"));
                    break;
                }
            }
        }

        private static void Warnings(AnimationManager manager)
        {
            var seen=new HashSet<MonoBehaviour>();var properties=new HashSet<string>();
            foreach(var child in manager.Children)
            {
                if(child==null){EditorGUILayout.HelpBox("A child reference is missing. Refresh the list.",MessageType.Warning);continue;}
                if(!seen.Add(child))EditorGUILayout.HelpBox("Duplicate child: "+child.name,MessageType.Warning);
                if(!(child is IManagedAnimation animation)) {EditorGUILayout.HelpBox("Unsupported child: "+child.name,MessageType.Warning);continue;}
                if(animation.IsLooping)EditorGUILayout.HelpBox(child.name+" runs indefinitely: it counts as started and will not block later children.",MessageType.None);
                if(child is AnimationBase && !(child.transform is RectTransform))EditorGUILayout.HelpBox(child.name+" requires a RectTransform.",MessageType.Warning);
                var channels=new List<string>();
                if(child is DoScale || child is ButtonAnimations)channels.Add("scale");
                if(child is DoDirectional)
                {
                    channels.Add("anchored position");
                    if(new SerializedObject(child).FindProperty("Scaling").boolValue)channels.Add("scale");
                }
                if(child is DoFade)channels.Add("graphic alpha");
                if(child is DoFadeCanvas)channels.Add("canvas alpha");
                if(child is DoFill)channels.Add("image fill");
                foreach(var channel in channels)
                    if(!properties.Add(child.transform.GetInstanceID()+":"+channel))
                        EditorGUILayout.HelpBox("Multiple animations write "+channel+" on "+child.name+". Avoid overlapping effects on the same property.",MessageType.Warning);
            }
        }
    }

    [CustomEditor(typeof(AnimationBase), true)]
    public sealed class PanelAnimationEditor : UnityEditor.Editor
    {
        private bool showEvents, hideEvents;
        private static readonly string[] CommonFields =
        {
            "m_Script", "ignoreParent", "PlayOnEnable", "DisableOnHide", "ShowDuration", "HideDuration",
            "ShowEase", "HideEase", "SelectShowEvents", "SelectHideEvents", "isTimeIndependent", "Loops", "LoopType", "useCustomStartingScale", "startingScale"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var animation = (AnimationBase)target;
            var manager = animation.ResolveManager();
            using (new EditorGUI.DisabledScope(AnimationPreview.Active))
            {
                EditorGUILayout.LabelField("Control", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ignoreParent"), new GUIContent("Ignore Parent"));
                EditorGUILayout.HelpBox(manager != null
                    ? "Show and Hide calls are routed to the AnimationManager. Configure ordering and delays there."
                    : "Independent effect. Call Show and Hide directly, or add an AnimationManager to control the group.", MessageType.Info);
                if (manager != null)
                    using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("Animation Manager", manager, typeof(AnimationManager), true);
                using (new EditorGUI.DisabledScope(manager != null))
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("PlayOnEnable"), new GUIContent("Show On Enable"));
                if (manager == null || manager.transform != animation.transform)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("DisableOnHide"), new GUIContent("Deactivate After Hide", "Managed effects wait until the entire group finishes before deactivation."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("isTimeIndependent"), new GUIContent("Use Unscaled Time"));

                EditorGUILayout.Space();
                DrawPropertiesExcluding(serializedObject, CommonFields);
                var customScale = serializedObject.FindProperty("useCustomStartingScale");
                var scaling = serializedObject.FindProperty("Scaling");
                if (customScale != null && (scaling == null || scaling.boolValue))
                {
                    EditorGUILayout.LabelField("Fresh Show Starting Scale", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(customScale, new GUIContent("Use Custom Starting Scale"));
                    if (customScale.boolValue) EditorGUILayout.PropertyField(serializedObject.FindProperty("startingScale"), new GUIContent("Starting Scale"));
                    EditorGUILayout.HelpBox("Fresh Show starts at zero unless enabled. Interrupted playback continues from current values. Hide still finishes at zero.", MessageType.None);
                }
                var loops = serializedObject.FindProperty("Loops");
                if (loops != null)
                {
                    EditorGUILayout.LabelField("Show Loop", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(loops, new GUIContent("Iterations", "Use -1 for an infinite Show. Hide always finishes."));
                    if (loops.intValue != 1 && loops.intValue != 0) EditorGUILayout.PropertyField(serializedObject.FindProperty("LoopType"));
                }
                DrawPhase("Show", "ShowDuration", "ShowEase", "SelectShowEvents", ref showEvents);
                DrawPhase("Hide", "HideDuration", "HideEase", "SelectHideEvents", ref hideEvents);
            }
            serializedObject.ApplyModifiedProperties();
            if (!(animation.transform is RectTransform))
                EditorGUILayout.HelpBox("Panel animations require a RectTransform.", MessageType.Error);
        }

        private void DrawPhase(string title, string duration, string ease, string eventsName, ref bool expanded)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(duration), new GUIContent("Duration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(ease), new GUIContent("Ease"));
            expanded = EditorGUILayout.Foldout(expanded, title + " Events", true);
            if (!expanded) return;
            var events = serializedObject.FindProperty(eventsName);
            var flags = events.FindPropertyRelative("AddEvents");
            DrawEvent(events, flags, EventTypes.OnStart, "On Start", "m_OnStart");
            DrawEvent(events, flags, EventTypes.OnComplete, "On Complete", "m_OnComplete");
        }

        private static void DrawEvent(SerializedProperty events, SerializedProperty flags, EventTypes flag, string label, string field)
        {
            EditorGUI.showMixedValue = flags.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.ToggleLeft(label, (flags.intValue & (int)flag) != 0);
            if (EditorGUI.EndChangeCheck()) flags.intValue = enabled ? flags.intValue | (int)flag : flags.intValue & ~(int)flag;
            EditorGUI.showMixedValue = false;
            if (enabled) EditorGUILayout.PropertyField(events.FindPropertyRelative(field), new GUIContent(label));
        }
    }
    [InitializeOnLoad]
    internal static class AnimationHierarchyWatcher
    {
        private static bool queued;
        static AnimationHierarchyWatcher()
        { EditorApplication.hierarchyChanged+=Schedule;Undo.undoRedoPerformed+=Schedule;EditorApplication.projectChanged+=Schedule;AnimationManager.ConfigurationChanged+=Schedule;Schedule(); }
        private static void Schedule()
        {if(queued)return;queued=true;EditorApplication.delayCall+=Refresh;}
        private static void Refresh()
        {
            queued=false;if(EditorApplication.isPlayingOrWillChangePlaymode || AnimationPreview.Active)return;
            foreach(var manager in UnityEngine.Object.FindObjectsByType<AnimationManager>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(EditorUtility.IsPersistent(manager))continue;
                string before=EditorJsonUtility.ToJson(manager);manager.RefreshChildren();
                if(before!=EditorJsonUtility.ToJson(manager))
                {EditorUtility.SetDirty(manager);if(manager.gameObject.scene.IsValid())EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);}
            }
        }
    }
}
#endif
