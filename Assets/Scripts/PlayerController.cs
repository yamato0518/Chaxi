using System.Collections;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    private float turnInput;
    private float speedInput;

    [Header("メータ制御スクリプト参照")]
    [SerializeField] private CargeGuageCntl chargeGaugeController;

    [Header("移動設定（斜め移動用）")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float xClamp = 2.5f;


    [Header("移動設定（ワープ用）")]
    [SerializeField] private float warpSpeed = 20f;
    [SerializeField] private float laneDistance = 2.5f; // レーンの幅

    [Header("走行速度")]
    [SerializeField] private float fixedMapSpeed = 50f;

    [Header("移動方法")]
    [SerializeField] private bool isLinearMovement = true; // true: 斜め移動, false: ワープ移動

    [Header("加減速設定(デバッグキー用)")]
    [SerializeField] private float minMapSpeed = 0f;
    [SerializeField] private float maxMapSpeed = 20f;
    [SerializeField] private float accelerationPower = 2f;    // Wキーを1回押した（回した）時の加速の強さ
    [SerializeField] private float deaccelerationSpeed = 5f; // キーを放している時の減速の強さ

    [Header("茶筅コントローラー用設定")]
    [SerializeField] private float chasenSpeedMultiplier = 0.05f; //茶筅のジャイロ値をどれぐらいスピードに変換するか
    [SerializeField] private float chasenEscapeMultiplier = 0.2f; //茶筅でのぬかるみ脱出パワー倍率
    [SerializeField] private float chasenSpeedThreshold = 0.1f; //茶筅を振っているとみなす閾値

    [Header("最低限のクリア速度")]
    [SerializeField] private float clearMinimumSpeed;

    [Header("ぬかるみギミック設定")]
    [SerializeField] private GameObject mudUI;
    [SerializeField] private float escapeRequiredCount = 10f;
    [SerializeField] private float maxBonusMultiplier = 3.0f; // 最大倍率

    [Header("クリア設定")]
    [SerializeField] private GameObject clearUI;

    [Header("デバッグ表示用")]
    public float CurrentMapSpeed;

    [Header("利休メータシステム")]
    [SerializeField] private GameObject guageUI;
    [Tooltip("基準となる回転速度")]
    public float idealRotationSpeed = 3.0f;

    [Tooltip("基準速度を超えた時に、どのくらいゲージを上げるか")]
    [SerializeField] private float gageChargeSpeed = 5f;

    [Tooltip("基準速度を下回った時に、毎秒どれくらい自動でゲージを下げるか")]
    [SerializeField] private float gageDrainSpeed = 3f;
    [SerializeField] float currentSafeZoneRange = 20f;


    [Tooltip("ゲージの最大値")]
    [SerializeField] private float maxGageValue = 100f;

    [Tooltip("カウントダウンが始まる目標値")]
    [SerializeField] private float clearMinimumGage = 80f;

    [Header("現在のゲージ量")]
    public float CurrentGageValue;

    private int targetLane = 1; // 0: 左レーン, 1: 中央レーン, 2: 右レーン
    private Vector3 targetPosition;
    private bool isInputReset = true; // 入力リセットフラグ
    private bool hasAcceleratedThisFrame = false; //1フレーム内の重複防止
    private bool isStuckInMud = false; //今ぬかるみにはまっているか
    private float currentEscapeProgress = 0f; //現在の連打量
    private int lastMudEscapeProgressInteger = 0; // 前回SEを鳴らした進行度の整数部分
    private bool isRunnnig = false;
    [SerializeField] private bool isControlEnabled = true;
    private float oldCurrentSpeed;
    private bool isSlowedByObstacle = false;

    [Header("障害物衝突時の速度制御")]
    [SerializeField] private float obstacleSlowdownMultiplier = 0.4f; // 障害物接触時の減速倍率
    [SerializeField] private float minObstacleSpeed = 10f; // 障害物接触時の最低速度
    [SerializeField] private float minObstacleRecoverySpeed = 3f; // ゲージ0のときの復帰速度
    [SerializeField] private float maxObstacleRecoverySpeed = 15f; // ゲージ最大時の復帰速度

    [Header("タイムマネージャー参照")]
    [SerializeField] private CountDownManager timeManager;
    private bool isCountDownStarted = false;

    private AudioManager audioManager;
    private GoalManager goalManager;

    public int ObstacleHitCount { get; private set; }

    void Start()
    {
        LoadConfig();

        rb = GetComponent<Rigidbody>();

        CurrentMapSpeed = fixedMapSpeed; // 初期速度を設定
        CurrentGageValue = 0f;

        if (mudUI != null) mudUI.SetActive(false);
        if (guageUI != null) guageUI.SetActive(true);

        audioManager = AudioManager.Instance != null ? AudioManager.Instance : FindFirstObjectByType<AudioManager>();
        goalManager = FindFirstObjectByType<GoalManager>();

        ObstacleHitCount = 0;
    }

    void Update()
    {
        if (!isControlEnabled)
        {
            return;
        }

        if (isStuckInMud)
        {
            return;
        }

        //コントローラー入力
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (targetLane < 2 && isInputReset)
            {
                targetLane++;
                isInputReset = false;
            }
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            if (targetLane > 0 && isInputReset)
            {
                targetLane--;
                isInputReset = false;
            }
        }

        if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
        {
            isInputReset = true;
        }

        //デバッグ用キー入力
        if (Input.GetKeyDown(KeyCode.Space) && !isRunnnig)
        {
            StartRunning();
        }
    }

    public void MoveAxis(InputAction.CallbackContext context)
    {
        if (!isControlEnabled || isStuckInMud)
        {
            turnInput = 0f;
            return;
        }

        turnInput = context.ReadValue<float>();
        //Debug.Log(turnInput);
    }

    public void Speed(InputAction.CallbackContext context)
    {
        if (!isControlEnabled || isStuckInMud)
        {
            speedInput = 0f;
            return;
        }

        speedInput = context.ReadValue<float>();
        //Debug.Log($"加減速入力: {speedInput}");
    }


    void FixedUpdate()
    {
        if (!isControlEnabled)
        {
            return;
        }

        if (isStuckInMud)
        {
            HandleMudEscape();
            return;
        }

        CalculateCurrentMapSpeed();

        if (isRunnnig)
        {

            //なめらか移動
            if (isLinearMovement)
            {
                TurnLinear();
            }
            //ワープ移動
            else
            {
                float targetX = (targetLane - 1) * laneDistance;
                float newX = Mathf.MoveTowards(transform.position.x, targetX, warpSpeed * Time.fixedDeltaTime);

                Vector3 finalPosition = new Vector3(newX, rb.position.y, rb.position.z);
                rb.MovePosition(finalPosition);
            }
        }
    }

    public void StartRunning()
    {
        if (!isControlEnabled || isStuckInMud)
        {
            return;
        }

        guageUI.SetActive(false);
        isRunnnig = true;
        if (CurrentMapSpeed <= minMapSpeed + 0.01f)
        {
            CurrentMapSpeed = Mathf.Max(minMapSpeed + 1f, (maxMapSpeed - minMapSpeed) * 0.2f);
        }
        CurrentMapSpeed = Mathf.Clamp(CurrentMapSpeed, minMapSpeed, maxMapSpeed);
    }

    public void SetControlEnabled(bool enabled)
    {
        isControlEnabled = enabled;

        if (!isControlEnabled)
        {
            turnInput = 0f;
            speedInput = 0f;
            isInputReset = true;
        }
    }

    //SerialReceiveから茶筅の回転の強さを取得
    private float GetChasenRotationSpeed()
    {
        if (SerialReceive.SRInstance != null)
        {
            return SerialReceive.SRInstance.accMotion;
        }
        return 0f;
    }

    private void CalculateCurrentMapSpeed()
    {
        if (!isRunnnig)
        {
            // メーターの参照がない場合は処理しない（エラー防止）
            if (chargeGaugeController == null)
            {
                Debug.LogWarning("CalculateCurrentMapSpeed: chargeGaugeController is null, returning early.");
                return;
            }

            float currentAngle = chargeGaugeController.GetCurrentAngle();
            float angleDifference = Mathf.Abs(currentAngle);
            bool isDeviceActive = chargeGaugeController.HasInput();

            // 角度のズレが 20度以内、かつ入力がある場合のみ上昇

            if (angleDifference <= currentSafeZoneRange && isDeviceActive)
            {
                // ピッタリ真上（0度）で1.0、限界（20度）で0.0になるスコア計算
                float score = 1.0f - (angleDifference / currentSafeZoneRange);

                CurrentGageValue += score * gageChargeSpeed * Time.fixedDeltaTime;
                Debug.Log($"【範囲内キープ中↑】角度ズレ: {angleDifference:F1}° | ゲージ: {CurrentGageValue:F1}");
            }
            else
            {
                CurrentGageValue -= gageDrainSpeed * Time.fixedDeltaTime;
                Debug.Log($"【範囲外↓】角度ズレ: {angleDifference:F1}° | ゲージ: {CurrentGageValue:F1}");
            }

            // 0〜最大値の間に値をクランプ
            CurrentGageValue = Mathf.Clamp(CurrentGageValue, 0f, maxGageValue);

            // 目標値までゲージが溜まったらカウントダウン開始
            if (CurrentGageValue >= clearMinimumGage && !isCountDownStarted)
            {
                isCountDownStarted = true;
                if (timeManager != null)
                {
                    timeManager.StartCountDown();
                }
            }

            return;
        }

        // 一定速度で走るため、通常は固定速度を維持
        if (!isSlowedByObstacle)
        {
            CurrentMapSpeed = fixedMapSpeed;
        }
        else
        {
            UpdateObstacleRecovery();
        }

        Debug.Log($"現在の速さ{CurrentMapSpeed}");
    }

    //障害物に接触した後の速度回復処理
    private void UpdateObstacleRecovery()
    {
        float gaugeRatio = (maxGageValue > 0f) ? Mathf.Clamp01(CurrentGageValue / maxGageValue) : 0f;
        float recoverySpeed = Mathf.Lerp(minObstacleRecoverySpeed, maxObstacleRecoverySpeed, gaugeRatio);

        CurrentMapSpeed += recoverySpeed * Time.fixedDeltaTime;
        CurrentMapSpeed = Mathf.Clamp(CurrentMapSpeed, minMapSpeed, fixedMapSpeed);

        if (CurrentMapSpeed >= fixedMapSpeed - 0.01f)
        {
            CurrentMapSpeed = fixedMapSpeed;
            isSlowedByObstacle = false;
        }
    }

    private void TurnLinear()
    {
        Vector3 currentPosition = transform.position;
        Vector3 moveDirection = new Vector3(turnInput, 0, 0); // 左右の移動
        Vector3 newPosition = currentPosition + moveDirection * moveSpeed * Time.fixedDeltaTime;

        newPosition.x = Mathf.Clamp(newPosition.x, -xClamp, xClamp); // X軸の移動範囲を制限

        rb.MovePosition(newPosition);
    }

    private void TurnWarp()
    {
        float tagetX = (targetLane - 1) * laneDistance; // ターゲットレーンのX座標を計算
        targetPosition = new Vector3(tagetX, transform.position.y, transform.position.z);
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, warpSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);
    }

    private void HandleMudEscape()
    {
        float chasenSpeed = GetChasenRotationSpeed();
        float currentInputSpeed = (Input.GetKeyDown(KeyCode.W)) ? 5.0f : chasenSpeed;

        // ぬかるみ脱出も同じく基準速度を超えている間だけゲージが進む仕様に統一
        float speedDifference = currentInputSpeed - idealRotationSpeed;

        if (speedDifference > 0f)
        {
            float gaugeRatio = Mathf.Clamp01(CurrentGageValue / maxGageValue);

            float escapeMultiplerBonus = Mathf.Lerp(1.0f, maxBonusMultiplier, gaugeRatio);

            float baseProgress = speedDifference * chasenEscapeMultiplier * Time.fixedDeltaTime;
            currentEscapeProgress += baseProgress * escapeMultiplerBonus;
            chargeGaugeController.SetMudProgress(currentEscapeProgress, escapeRequiredCount);
            PlayMudEscapeProgressSE();
            Debug.Log($"現在の倍率{escapeMultiplerBonus:F2} ");
            Debug.Log($"【泥ぬけ中】進行度: {currentEscapeProgress:F1} / {escapeRequiredCount}");
        }

        if (currentEscapeProgress >= escapeRequiredCount)
        {
            EscapeFromMud();
        }
    }

    private void PlayMudEscapeProgressSE()
    {
        int currentProgressInteger =
            Mathf.FloorToInt(currentEscapeProgress);

        // 段階が変わったときだけ音を切り替える
        if (currentProgressInteger != lastMudEscapeProgressInteger)
        {
            lastMudEscapeProgressInteger = currentProgressInteger;

            // Index 1は障害物SEなので使用しない
            // Index 2～11がぬかるみ脱出音
            int clipIndex = currentProgressInteger + 2;

            audioManager?.PlayMudEscapeSE(clipIndex);
        }
    }

    private void HandleObstacleHit()
    {
        ObstacleHitCount++;
        audioManager?.PlaySE(1); // 障害物接触音を再生
        isSlowedByObstacle = true;
        CurrentMapSpeed = 10f;
        Debug.Log($"障害物接触: 速度を{CurrentMapSpeed:F1}に減速。復帰速度はゲージ{CurrentGageValue:F1}に依存します。");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Mud"))
        {
            chargeGaugeController.ResetGauge();

            isStuckInMud = true;
            currentEscapeProgress = 0f;
            lastMudEscapeProgressInteger = -1;
            isInputReset = true;
            turnInput = 0f;
            speedInput = 0f;

            chargeGaugeController.SetMudProgress(0f, escapeRequiredCount);
            PlayMudEscapeProgressSE();

            oldCurrentSpeed = CurrentMapSpeed;
            CurrentMapSpeed = 0f; // マップの速度を０に

            if (mudUI != null) mudUI.SetActive(true);
            if (guageUI != null) guageUI.SetActive(true);

            Destroy(other.gameObject);
            return;
        }

        if (other.CompareTag("ClearZone"))
        {
            if (goalManager != null)
            {
                goalManager.StartGoal();
            }
            return;
        }

        if (other.CompareTag("Obstacle"))
        {
            HandleObstacleHit();
            return;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            HandleObstacleHit();
        }
    }

    //脱出に成功した時の処理
    private void EscapeFromMud()
    {
        isStuckInMud = false;
        currentEscapeProgress = 0f;
        lastMudEscapeProgressInteger = -1;
        isInputReset = true;
        audioManager?.StopMudEscapeSE();

        if (mudUI != null) mudUI.SetActive(false);
        if (guageUI != null) guageUI.SetActive(false);

        CurrentMapSpeed = oldCurrentSpeed;
    }
    public bool getIsStuckInMud()
    {
        return isStuckInMud;
    }

    public bool getIsRunning()
    {
        return isRunnnig;
    }

    //configから値を読み取る
    private void LoadConfig()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "config.txt");

        if (!File.Exists(path))
        {
            Debug.LogWarning($"config.txtが見つかりません: {path}");
            return;
        }

        string[] lines = File.ReadAllLines(path);
        var data = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string trimmed = line.Split('#')[0].Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            string[] parts = trimmed.Split('=');
            if (parts.Length != 2) continue;

            data[parts[0].Trim()] = parts[1].Trim();
        }

        if (data.TryGetValue("MaxBonusMultiplier", out string bonus) && float.TryParse(bonus, out float parsedBonus))
        {
            maxBonusMultiplier = parsedBonus;
        }

        if (data.TryGetValue("EscapeRequiredCount", out string escape) &&
       float.TryParse(escape, out float parsedEscape))
        {
            escapeRequiredCount = parsedEscape;
        }

        if (data.TryGetValue("idealRotationSpeed", out string idealSpeed) && float.TryParse(idealSpeed, out float parsedIdealSpeed))
        {
            idealRotationSpeed = parsedIdealSpeed;
        }

        Debug.Log($"Config読込 " +
        $"MaxBonusMultiplier={maxBonusMultiplier}, " +
        $"EscapeRequiredCount={escapeRequiredCount}, " +
        $"IdealRotationSpeed={idealRotationSpeed}");

    }

}