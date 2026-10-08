using System;
using UnityEngine;
using MyBox;
using TMPro;

namespace ACS.Timer
{
    public class Timer : MonoBehaviour
    {
        public float levelTime = 60f; // Set the time for each level in seconds
        public TextMeshProUGUI timerText; // Reference to UI text to display timer
        public RectTransform clockHand;

        [ReadOnly, SerializeField] private float currentTime = 0f;
        [ReadOnly, SerializeField] private bool isLevelRunning = false;

        private Action onTimerFinished;

        public bool GreaterThanZero => currentTime > 0f;
        public void StartTimer(float levelTime)
        {
            this.levelTime = levelTime;
            onTimerFinished = null;
            StartLevelTimer();
        }

        public void StartTimer(float levelTime, Action onTimefinish)
        {
            gameObject.SetActive(true);
            this.levelTime = levelTime;
            onTimerFinished = onTimefinish;
            StartLevelTimer();
        }

        /// <summary>
        /// Updates the timer UI without starting its automatic countdown or failure behaviour.
        /// Use this when another level system owns the timing rules.
        /// </summary>
        public void SetVisualProgress(float remainingTime, float totalTime)
        {
            levelTime = Mathf.Max(0.1f, totalTime);
            currentTime = Mathf.Clamp(remainingTime, 0f, levelTime);
            isLevelRunning = false;
            UpdateTimerDisplay();
            UpdateClockHand();
        }

        public void StopTimer()
        {
            isLevelRunning = false;
            onTimerFinished = null;
        }

        public void PauseTimer()
        {
            isLevelRunning = false;
        }

        public void ResumeTimer()
        {
            if (currentTime > 0f)
                isLevelRunning = true;
        }

        private void Update()
        {
            if (isLevelRunning && UIManager.Instance.CurrentState().Equals(GameStatus.GameState.None))
            {
                currentTime -= Time.deltaTime;
                UpdateTimerDisplay();
                UpdateClockHand();
                if (currentTime <= 0)
                {
                    FinishLevel();
                    currentTime = 0;
                    UpdateTimerDisplay();
                    return;
                }
            }
        }

        void StartLevelTimer()
        {
            currentTime = levelTime;
            isLevelRunning = true;
            UpdateTimerDisplay();
            UpdateClockHand();
        }

        void FinishLevel()
        {
            isLevelRunning = false;
            Action completionCallback = onTimerFinished;
            onTimerFinished = null;

            if (completionCallback != null)
            {
                completionCallback.Invoke();
                return;
            }

            Debug.Log("<color=red>********** Level Failed **********</color>");
            //GameManager.Instance.LevelFail();
        }

        void UpdateTimerDisplay()
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);

            string timerString = string.Format("{0:00}:{1:00}", minutes, seconds);
            if (timerText)
                timerText.text = timerString;
        }
        void UpdateClockHand()
        {
            if (clockHand)
            {
                float rotationFraction = currentTime / levelTime;
                float rotationAngle = rotationFraction * 360f;
                clockHand.localRotation = Quaternion.Euler(0, 0, rotationAngle);
            }
        }

        public void IncreaseTime(float extraTime)
        {
            if (currentTime <= 0)
                currentTime += extraTime;

            isLevelRunning = true;
        }

    }
}



