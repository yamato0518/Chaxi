using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class SerialHandler : MonoBehaviour
{
    public delegate void SerialDataReceivedEventHandler(string message);
    public event SerialDataReceivedEventHandler OnDataReceived;

    [Header("Config")]
    [Tooltip("config.txt のキー名（例：PortR / PortL）")]
    public string configKey = "PortR";

    [Tooltip("config.txt が無い時のフォールバック")]
    public string portName = "COM9";

    public int baudRate = 115200;

    private SerialPort serialPort_;
    private Thread thread_;
    private bool isRunning_ = false;

    private string message_;
    private bool isNewMessageReceived_ = false;

    private bool needReconnect = false;
    private float reconnectTimer = 0f;
    public float reconnectInterval = 2f;

    public float LastReceivedTime { get; private set; } = -999f;
    public bool IsPortOpen => serialPort_ != null && serialPort_.IsOpen;

    private volatile bool receivedPing_ = false;

    // 例：0.5秒以上データが来ないなら “通信なし”
    public bool IsReceiving(float timeoutSec = 0.5f)
    {
        return IsPortOpen && (Time.time - LastReceivedTime) <= timeoutSec;
    }

    private void Awake()
    {
        Debug.Log($"[SerialHandler] StreamingAssetsPath={Application.streamingAssetsPath}");

        LoadConfig();
        DontDestroyOnLoad(gameObject);

        TryOpen();
    }

    private void Update()
    {
        if (receivedPing_)
        {
            receivedPing_ = false;
            LastReceivedTime = Time.time;
        }

        if (isNewMessageReceived_)
        {
            isNewMessageReceived_ = false;
            OnDataReceived?.Invoke(message_);
        }

        // 再接続処理
        if (needReconnect)
        {
            reconnectTimer += Time.deltaTime;
            if (reconnectTimer > reconnectInterval)
            {
                reconnectTimer = 0f;
                TryOpen();
            }
        }
    }

    private void OnDestroy()
    {
        Close();
    }

    private void TryOpen()
    {
        // 既に開いてたら閉じる
        Close();

        try
        {
            serialPort_ = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            serialPort_.ReadTimeout = 1000; // 無限待ち防止
            serialPort_.Open();

            isRunning_ = true;
            thread_ = new Thread(Read);
            thread_.Start();

            needReconnect = false;
            reconnectTimer = 0f;

            Debug.Log($"[SerialHandler] Opened {portName} @ {baudRate} (key={configKey})");
        }
        catch (Exception e)
        {
            needReconnect = true;
            reconnectTimer = 0f;
            Debug.LogError($"[SerialHandler] Failed to open {portName} (key={configKey}) : {e.Message}");
        }
    }

    private void Close()
    {
        isNewMessageReceived_ = false;
        isRunning_ = false;

        try
        {
            if (thread_ != null && thread_.IsAlive)
            {
                thread_.Join(200);
            }
        }
        catch { /* ignore */ }

        try
        {
            if (serialPort_ != null)
            {
                if (serialPort_.IsOpen) serialPort_.Close();
                serialPort_.Dispose();
                serialPort_ = null;
            }
        }
        catch { /* ignore */ }
    }

    private void Read()
    {
        // 1文字ずつ読み込んで溜めておくためのバッファ
        string inputBuffer = "";

        while (isRunning_ && serialPort_ != null && serialPort_.IsOpen)
        {
            try
            {
                // ❌ message_ = serialPort_.ReadLine(); // これを廃止

                // 【修正】データが届いているか確認し、届いていればすべて読み取る
                if (serialPort_.BytesToRead > 0)
                {
                    string incoming = serialPort_.ReadExisting();
                    inputBuffer += incoming;

                    // バッファの中にカンマが最低2つ（データ3個分）あるか確認
                    // 改行コード（\n）の代わりに、カンマの数をデータの区切り基準にします
                    int commaCount = 0;
                    for (int i = 0; i < inputBuffer.Length; i++) if (inputBuffer[i] == ',') commaCount++;

                    // 3つのデータ（例: "0.1,0.5,-0.9"）が揃ったら処理に回す
                    if (commaCount >= 2)
                    {
                        // 念のため前後の不要な空白などをカット
                        message_ = inputBuffer.Trim();
                        isNewMessageReceived_ = true;
                        receivedPing_ = true;

                        // 次のデータ受信のためにバッファを空にする
                        inputBuffer = "";
                    }
                }
                else
                {
                    // CPUへの負荷を減らすため、データが来ていない時は一瞬だけスレッドを休ませる
                    Thread.Sleep(1);
                }
            }
            catch (TimeoutException)
            {
                // 何もしない（次ループへ）
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SerialHandler] Read error {portName}: {e.Message}");
                needReconnect = true;
                Close();
                break; // スレッド終了
            }
        }
    }

    public void Write(string message)
    {
        try
        {
            serialPort_?.Write(message);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }

    private void LoadConfig()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "config.txt");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SerialHandler] config.txt not found: {path} (use inspector value)");
            return;
        }

        string[] lines = File.ReadAllLines(path);
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // コメント対応 (# 以降無視)
            string trimmed = line.Split('#')[0].Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            string[] parts = trimmed.Split('=');
            if (parts.Length != 2) continue;

            data[parts[0].Trim()] = parts[1].Trim();
        }

        // Port
        if (data.TryGetValue(configKey, out string pn))
        {
            portName = pn;
        }
        else
        {
            Debug.LogWarning($"[SerialHandler] Key '{configKey}' not found in config.txt (use {portName})");
        }

        // BaudRate（任意）
        if (data.TryGetValue("BaudRate", out string br) && int.TryParse(br, out int parsed))
        {
            baudRate = parsed;
        }
    }
}