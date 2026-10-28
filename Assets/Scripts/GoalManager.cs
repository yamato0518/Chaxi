using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;


public class GoalManager : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private PlayerController player;
    [SerializeField] private RectTransform resultPanel;
    [SerializeField] private TMP_Text remainingTimeText;
    [SerializeField] private TMP_Text accidentCountText;
    [SerializeField] private GoalImageManager goalImageManager;
    [SerializeField] private GoalImageManager rankImageManager;
    [SerializeField] private TimerManager timerManager;
    private AudioManager audioManager;

    [Header("停止設定")]
    [SerializeField] private float stopDuration = 2f;
    [SerializeField] private float resultPanelSlideDuration = 0.8f;
    [SerializeField] private float resultPanelStartOffsetY = 1000f;
    [SerializeField] private float resultTextRevealDelay = 1f;
    [SerializeField] private float resultImageRevealDelay = 3f;


    [SerializeField] private float goalTime = 70f;

    private bool isGoalTriggered = false;
    private Coroutine stopCoroutine;
    private bool pendingIsTimeUp = false;
    private float pendingRemainingTime = 0f;
    private int pendingObstacleCount = 0;


    private void Awake()
    {
        if (timerManager == null)
        {
            timerManager = FindFirstObjectByType<TimerManager>();
        }


        audioManager = AudioManager.Instance;


        if (resultPanel != null)
        {
            GoalImageManager[] imageManagers = resultPanel.GetComponentsInChildren<GoalImageManager>(true);

            if (goalImageManager == null && imageManagers.Length > 0)
            {
                goalImageManager = imageManagers[0];
            }

            if (rankImageManager == null && imageManagers.Length > 1)
            {
                rankImageManager = imageManagers[1];
            }
        }

        if (goalImageManager != null)
        {
            goalImageManager.gameObject.SetActive(false);
        }

        if (rankImageManager != null)
        {
            rankImageManager.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (timerManager != null)
        {
            timerManager.OnTimerFinished += HandleTimeUp;
        }
    }

    private void OnDisable()
    {
        if (timerManager != null)
        {
            timerManager.OnTimerFinished -= HandleTimeUp;
        }
    }

    public void StartGoal()
    {
        TriggerResult(false);
    }

    private void HandleTimeUp()
    {
        TriggerResult(true);
    }

    private void TriggerResult(bool isTimeUp)
    {
        if (isGoalTriggered)
        {
            return;
        }

        isGoalTriggered = true;
        pendingIsTimeUp = isTimeUp;
        pendingRemainingTime = timerManager != null ? timerManager.CurrentTime : 0f;
        pendingObstacleCount = player != null ? player.ObstacleHitCount : 0;

        timerManager?.StopTimer();
        timerManager?.HideTimerText();

        if (audioManager != null)
        {
            audioManager.StopAllGameAudio();

            if (isTimeUp)
            {
                audioManager.PlayFail();
            }
            else
            {
                audioManager.PlayClear();
            }
        }

        ShowBackgroundImage();

        if (player != null)
        {
            player.SetControlEnabled(false);

            if (stopCoroutine != null)
            {
                StopCoroutine(stopCoroutine);
            }

            stopCoroutine = StartCoroutine(SmoothStopAndShowResult());
        }
        else if (resultPanel != null)
        {
            resultPanel.gameObject.SetActive(true);
            StartCoroutine(ShowResultContentSequence(pendingIsTimeUp));
        }
        else if (ChangeScene.Instance != null)
        {
            //audioManager.StopAllGameAudio();
            ChangeScene.Instance.BeginResultBackToTitleMode();
        }
    }



    private void ShowBackgroundImage()
    {
        int resultIndex = ResolveResultImageIndex(pendingIsTimeUp, pendingRemainingTime, pendingObstacleCount);

        if (goalImageManager != null)
        {
            goalImageManager.gameObject.SetActive(true);
            goalImageManager.ShowResultImage(resultIndex);
        }

        if (rankImageManager != null)
        {
            rankImageManager.gameObject.SetActive(false);
        }
    }

    private IEnumerator SmoothStopAndShowResult()
    {
        float startSpeed = player.CurrentMapSpeed;
        float elapsed = 0f;

        while (elapsed < stopDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / stopDuration);
            player.CurrentMapSpeed = Mathf.Lerp(startSpeed, 0f, t);
            yield return null;
        }

        player.CurrentMapSpeed = 0f;

        if (resultPanel != null)
        {
            resultPanel.gameObject.SetActive(true);

            Vector2 targetPosition = resultPanel.anchoredPosition;
            Vector2 startPosition = targetPosition + new Vector2(0f, resultPanelStartOffsetY);
            resultPanel.anchoredPosition = startPosition;

            float slideElapsed = 0f;
            while (slideElapsed < resultPanelSlideDuration)
            {
                slideElapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, slideElapsed / resultPanelSlideDuration);
                resultPanel.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            resultPanel.anchoredPosition = targetPosition;
            StartCoroutine(ShowResultContentSequence(pendingIsTimeUp));
        }
    }

    private IEnumerator ShowResultContentSequence(bool isTimeUp)
    {
        int obstacleCount = pendingObstacleCount;
        float remainingTime = pendingRemainingTime;
        int resultImageIndex = ResolveResultImageIndex(isTimeUp, remainingTime, obstacleCount);

        if (remainingTimeText != null)
        {
            remainingTimeText.text = "残り時間：";
            yield return new WaitForSeconds(resultTextRevealDelay);
            remainingTimeText.text = $"残り時間：{FormatTime(remainingTime)}";
        }

        if (accidentCountText != null)
        {
            accidentCountText.text = "事故回数：";
            yield return new WaitForSeconds(resultTextRevealDelay);
            accidentCountText.text = $"事故回数：{obstacleCount}";
        }

        yield return new WaitForSeconds(resultImageRevealDelay);

        if (rankImageManager != null)
        {
            rankImageManager.gameObject.SetActive(true);
            rankImageManager.ShowResultImage(resultImageIndex);
        }

        if (ChangeScene.Instance != null)
        {
            audioManager.StopAllGameAudio();
            ChangeScene.Instance.BeginResultBackToTitleMode();
        }
    }


    private int ResolveResultImageIndex(bool isTimeUp, float remainingTime, int obstacleCount)
    {
        if (isTimeUp)
        {
            return 2;
        }

        if (remainingTime >= goalTime)
        {
            return 0;
        }

        if (obstacleCount >= 1)
        {
            return 1;
        }

        return 1;
    }

    private string FormatTime(float totalSeconds)
    {
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        return string.Format("{0}:{1:00}", minutes, seconds);
    }
}
