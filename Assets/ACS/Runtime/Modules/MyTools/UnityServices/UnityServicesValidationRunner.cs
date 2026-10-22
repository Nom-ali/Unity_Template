#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace ACS.UnityServices
{
    [AddComponentMenu("")]
    public sealed class UnityServicesValidationRunner : MonoBehaviour
    {
        public void Run(IEnumerator checks) { StartCoroutine(Guard(checks)); }
        private IEnumerator Guard(IEnumerator checks)
        {
            while (true)
            {
                object next;
                try { if (!checks.MoveNext()) break; next = checks.Current; }
                catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; }
                yield return next;
            }
            EditorApplication.Exit(0);
        }
    }
}
#endif
