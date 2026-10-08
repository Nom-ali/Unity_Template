using System;
using UnityEngine;

namespace ACS
{
    public class ACSTimer : MonoBehaviour
    {
        [SerializeField] private float elapsedTime;
        [SerializeField] private bool isRunning = true;

        public float ElapsedTime => elapsedTime;
        public bool IsRunning => isRunning;

        public event Action<float> OnTick;

        private void Update()
        {
            if (!isRunning)
            {
                return;
            }

            elapsedTime += Time.deltaTime;
            OnTick?.Invoke(elapsedTime);
        }

        public void ResetTimer()
        {
            elapsedTime = 0f;
        }

        public void SetRunning(bool running)
        {
            isRunning = running;
        }
    }
}
