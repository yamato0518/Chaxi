using UnityEngine;
using TMPro;
using System;

public class TimerManager : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;

    [SerializeField] private float timerLimit = 120f;

    private float currentTime;
    private bool isRunning = false;

    public event Action OnTimerFinished;

    public bool IsRunning => isRunning;
    public bool IsTimeUp => currentTime <= 0f;
    public float CurrentTime => currentTime;
    private bool hasFinished = false;

    void Start()
    {
        currentTime = timerLimit;
        hasFinished = false;
        SetTimerTextVisible(true);
        UpdateTimerText();
    }

    void Update()
    {
        if(!isRunning || hasFinished)
        {
            return;
        }

        currentTime -= Time.deltaTime;
        if(currentTime <= 0)
        {
            currentTime = 0;
            isRunning = false;
            hasFinished = true;
            UpdateTimerText();
            OnTimerFinished?.Invoke();
            return;
        }   

        UpdateTimerText();
    }

    public void StartTimer()
    {
        currentTime = timerLimit;
        isRunning = true;
        hasFinished = false;
        SetTimerTextVisible(true);
        UpdateTimerText();
    }

    public void StopTimer()
    {
        isRunning = false;
        hasFinished = true;
    }

    public void HideTimerText()
    {
        SetTimerTextVisible(false);
    }

    public void ShowTimerText()
    {
        SetTimerTextVisible(true);
    }

    private void SetTimerTextVisible(bool visible)
    {
        if (timerText != null)
        {
            timerText.gameObject.SetActive(visible);
        }
    }

    void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);

        timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
    }
}
