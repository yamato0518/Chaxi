using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CountDownManager : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private PlayerController player;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private Image churtrialImage;

    [Header("設定")]
    [SerializeField] private int countdownSeconds = 10;

    [SerializeField] private Sprite churtrialImage3;

    private AudioManager audioManager;
    void Start()
    {
        if(countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        if (timerManager == null)
        {
            timerManager = FindFirstObjectByType<TimerManager>();
        }

        if (timerManager == null)
        {
            Debug.LogWarning("[CountDownManager] TimerManager が見つかりません。Inspector かシーン配置を確認してください。");
        }

        audioManager = FindFirstObjectByType<AudioManager>();
    }

    public void StartCountDown()
    {
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        //audioManager?.PlaySE(0); // カウントダウン開始音を再生

        if(countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        int currentCount = countdownSeconds;

        while(currentCount > 0)
        {
            if(countdownText != null) countdownText.text = currentCount.ToString();

            if(currentCount == 8)
            {
                audioManager?.PlaySE(0);
            }

            yield return new WaitForSeconds(1f);
            currentCount--;
        }

        if(countdownText != null) countdownText.text ="開始！";
        churtrialImage.sprite = churtrialImage3;
        yield return new WaitForSeconds(2f);
        churtrialImage.gameObject.SetActive(false);

        if(player != null)
        {
            player.StartRunning();
        }

        timerManager?.StartTimer();

        yield return new WaitForSeconds(0.5f);

        if(countdownText!= null) countdownText.gameObject.SetActive(false);

        yield return new WaitForSeconds(1f);

        if (timerManager != null && timerManager.IsRunning)
        {
            audioManager?.PlayBGM(); // BGMを再生
        }
    }
}
