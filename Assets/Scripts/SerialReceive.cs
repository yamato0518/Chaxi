using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System;
using TMPro;

public class SerialReceive : MonoBehaviour
{
    // シングルトンインスタンス
    public static SerialReceive SRInstance { get; private set; }

    // 受信データ（10要素の配列）
    private float[] valuesFloat = new float[10];
    public float[] valuesReadOnly => valuesFloat; // 外部読み取り用

    private int lineCount = 0;

    [Header("CHASEN SWING ACTION (茶筅の振り判定)")]
    public bool onChasenSwing = false;      // 一定以上の強さで振られた瞬間に true（トリガー用）
    public bool isChasenMoving = false;     // 現在茶筅が動いている最中なら true（状態用）
    private bool swingCooldown = false;
    public float swingCooldownTime = 0.2f;  // 連打判定防止のクールダウン (200ms)

    [Header("CHASEN SENSOR DATA (茶筅の動きの強さ)")]
    public float gyroNorm = 0;   //茶筅を回す・振るスピードの絶対値（車の加速に使用）
    public float accNorm = 0;    // 加速度の全体の大きさ
    public float accMotion = 0;  // 重力の影響を抜いた、純粋な激しさの量

    [Header("RAW DATA (デバッグ・個別調整用)")]
    public float accX = 0;
    public float accY = 0;
    public float accZ = 0;
    public float gyroX = 0;
    public float gyroY = 0;
    public float gyroZ = 0;
    public float roll = 0;
    public float pitch = 0;
    public float yaw = 0;

    [Header("Threshold (判定の閾値設定)")]
    [Tooltip("茶筅が動いているとみなすジャイロの最低値")]
    public float gyroStartTh = 2.0f;
    [Tooltip("茶筅が止まったとみなすジャイロの数値")]
    public float gyroEndTh = 1.0f;
    [Tooltip("振られた！(瞬間加速) と判定する瞬間の加速度の閾値")]
    public float accMotionTh = 1.5f;

    [Header("SERIAL HANDLER")]
    public SerialHandler serialHandler;

    private bool wasWarning = false;
    public TextMeshProUGUI warningText; // 通信切断時の警告UI（任意）

    private void Awake()
    {
        if (SRInstance == null)
        {
            SRInstance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (serialHandler != null)
        {
            serialHandler.OnDataReceived += OnDataReceived;
        }
    }

    private void Update()
    {
        CalculateChasenMotion();

        // 通信状態のチェック (0.5秒データが来なければ警告)
        bool serialOk = serialHandler != null && serialHandler.IsReceiving(0.5f);

        if (!serialOk)
        {
            if (warningText != null) warningText.gameObject.SetActive(true);
        }
        else
        {
            if (warningText != null) warningText.gameObject.SetActive(false);
        }
        wasWarning = !serialOk;
    }

    // データ受信時の処理（CSV 10要素のパース）
    void OnDataReceived(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        // ★デバッグ：そもそもどんな文字列が届いているかコンソールに出す
        //Debug.Log($"[シリアル受信データ]: '{message}'");

        // CSV（カンマ区切り）の妥当性チェック
        int commaCount = 0;
        for (int i = 0; i < message.Length; i++) if (message[i] == ',') commaCount++;
        if (commaCount < 2)
        {
            //Debug.LogWarning($"【弾かれた】カンマの数が足りません: {commaCount}個");
            return;
        }

        // 先頭が数字、マイナス、空白以外はノイズとして捨てる
        char c0 = message.TrimStart().Length > 0 ? message.TrimStart()[0] : '\0';
        if (!(char.IsDigit(c0) || c0 == '-'))
        {
            //Debug.LogWarning($"【弾かれた】先頭文字が不正です: '{c0}' (アスキーコード: {(int)c0})");
            return;
        }

        string[] splitValues = message.Split(',');
        if (splitValues.Length < 3) return;

        // floatのパース
        for (int i = 0; i < 3; i++)
        {
            if (!float.TryParse(splitValues[i], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out valuesFloat[i]))
            {
                //Debug.LogWarning($"【弾かれた】数値への変換に失敗しました: '{splitValues[i]}'");
                return;
            }
        }

        lineCount++;
        if (lineCount < 3) return;

        // 各変数への代入
        accX = Mathf.Round(valuesFloat[0] * 10f) / 10f;
        accY = Mathf.Round(valuesFloat[1] * 10f) / 10f;
        accZ = Mathf.Round(valuesFloat[2] * 10f) / 10f;

        // ★デバッグ：ここまで来れたら成功ログを出す
        //Debug.Log($"【パース成功】accX:{accX}, accY:{accY}, accZ:{accZ}");

        gyroX = 0; gyroY = 0; gyroZ = 0; roll = 0; pitch = 0; yaw = 0;
    }

    // 茶筅の純粋な動きの強さと、シャカシャカされた判定だけを計算するロジック
    private void CalculateChasenMotion()
    {
        bool isSerialConnected = serialHandler != null && serialHandler.IsReceiving(0.5f);
        if (!isSerialConnected)
        {
            accNorm = 0f;
            accMotion = 0f;
            isChasenMoving = false;
            onChasenSwing = false;
            return;
        }

        // ジャイロと加速度のノルム（3軸の総合的な合成ベクトルの長さ）を計算
        // gyroNorm = Mathf.Round((Mathf.Sqrt(gyroX * gyroX + gyroY * gyroY + gyroZ * gyroZ) * 10)) / 10;
        accNorm = Mathf.Round((Mathf.Sqrt(accX * accX + accY * accY + accZ * accZ) * 10)) / 10;

        // 静止状態(1G)からの差分をとり、純粋に「動かされた激しさ」を抽出
        accMotion = Mathf.Round((Mathf.Abs(accNorm - 1.0f) * 10)) / 10;

        // 茶筅が現在回されているかどうかの状態判定
        if (accMotion > 0.5f)
        {
            isChasenMoving = true;
        }
        else if (accMotion < 0.2f)
        {
            isChasenMoving = false;
            onChasenSwing = false;
        }

        // 茶筅を振ったという瞬間のトリガー判定
        if (!swingCooldown && isChasenMoving && (accMotion > accMotionTh))
        {
            onChasenSwing = true;
            Debug.Log("茶筅の運動を検知！");
            StartCoroutine(SwingCooldownRoutine());
        }
        else
        {
            onChasenSwing = false;
        }
    }

    private IEnumerator SwingCooldownRoutine()
    {
        swingCooldown = true;
        yield return new WaitForSeconds(swingCooldownTime);
        swingCooldown = false;
    }
}