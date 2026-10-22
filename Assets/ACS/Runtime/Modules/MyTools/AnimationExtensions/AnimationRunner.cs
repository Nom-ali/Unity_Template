using System.Collections.Generic;
using UnityEngine;
namespace ACS.Animations
{
    [DefaultExecutionOrder(-90)]
    public sealed class AnimationRunner : MonoBehaviour
    {
        private static AnimationRunner instance;
        private static readonly List<AnimationHandle> active = new(128);
        private static readonly List<AnimationHandle> pending = new(32);
        private static bool ticking;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime()
        { active.Clear(); pending.Clear(); instance = null; ticking = false; }
        internal static void Register(AnimationHandle handle)
        {
            if (Application.isPlaying && instance == null)
            {
                var go = new GameObject("ACS Animation Runner") { hideFlags = HideFlags.HideInHierarchy };
                instance = go.AddComponent<AnimationRunner>(); DontDestroyOnLoad(go);
            }
            if (ticking) pending.Add(handle); else active.Add(handle);
        }
        public static float LastScaledDelta { get; private set; }
        public static float LastUnscaledDelta { get; private set; }
        public static int ActiveCount => active.Count + pending.Count;
        private void Update() => Advance(Time.deltaTime, Time.unscaledDeltaTime);
        // Also drives deterministic tests and the Inspector preview, with explicit time inputs.
        public static void Advance(float scaledDelta, float unscaledDelta)
        {
            LastScaledDelta = scaledDelta; LastUnscaledDelta = unscaledDelta;
            ticking = true;
            try
            {
                for (int i = active.Count-1; i >= 0; i--)
                {
                    var handle = active[i];
                    if (!handle.Nested && handle.IsActive)
                    {
                        try { handle.Tick(scaledDelta, unscaledDelta); }
                        catch (System.Exception error) { handle.Cancel(); Debug.LogException(error); }
                    }
                    if (handle.Nested || !handle.IsActive) active.RemoveAt(i);
                }
            }
            finally { ticking = false; active.AddRange(pending); pending.Clear(); }
        }
        public static void Cancel(UnityEngine.Object target)
        {
            for (int i=0;i<active.Count;i++) if (ReferenceEquals(active[i].Target,target) || ReferenceEquals(active[i].Owner,target)) active[i].Cancel();
            for (int i=0;i<pending.Count;i++) if (ReferenceEquals(pending[i].Target,target) || ReferenceEquals(pending[i].Owner,target)) pending[i].Cancel();
        }
        public static void PauseOwner(MonoBehaviour owner, bool pause)
        {
            for(int i=0;i<active.Count;i++) if(active[i].Owner==owner) { if(pause)active[i].Pause();else active[i].Resume(); }
            for(int i=0;i<pending.Count;i++) if(pending[i].Owner==owner) { if(pause)pending[i].Pause();else pending[i].Resume(); }
        }
        public static void CancelAll()
        {
            for(int i=0;i<active.Count;i++)active[i].Cancel();
            for(int i=0;i<pending.Count;i++)pending[i].Cancel();
        }
        private void OnDestroy() { if (instance == this) { CancelAll(); active.Clear(); pending.Clear(); instance=null; } }
    }
}
