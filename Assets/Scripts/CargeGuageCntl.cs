using Unity.IO.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

public class CargeGuageCntl : MonoBehaviour
{
    [SerializeField] private RectTransform rikyuPivot;
    [SerializeField] private PlayerController playerController;

    [Header("コントローラー倍率")]
    [SerializeField] private float controllerSpeedMultiplier = 1.0f;

    [Header("ゲージの最大扇角度")]
    [SerializeField] private float maxGaugeAngle = 90f;

    [Header("動きの滑らかさ")]
    [SerializeField] private float smoothSpeed = 8f;

    [Header("デバッグ用連打設定")]
    [SerializeField] private float debugWKeyPower = 1.8f;
    [SerializeField] private float debugDecaySpeed = 4.0f;

    private float targetZAngle = 0f;
    private float currentZAngle = 0f;
    private float virtualInputSpeed = 0f;

    private float idealSpeed = 3.0f;
    private float maxSpeedLimit = 6.0f;

    private bool hasInputThisFrame = false;

    [SerializeField] private float chargeSoundThreshold = 5f;
    [SerializeField] private int chargeSoundIndex = 13;
    private bool hasPlayedChargeSound = false;

    private float mudProgress = 0f;
    private float mudMaxProgress = 10f;

    void Start()
    {
        hasPlayedChargeSound = false;

        if (rikyuPivot == null)
        {
            rikyuPivot = GetComponent<RectTransform>();
        }

        currentZAngle = maxGaugeAngle;

        if (playerController != null)
        {
            idealSpeed = playerController.idealRotationSpeed;
            maxSpeedLimit = idealSpeed * 2f;
        }
    }

    void Update()
    {
        if (playerController == null) return;

        float chasenSpeed = 0f;

        if (SerialReceive.SRInstance != null)
        {
            chasenSpeed = SerialReceive.SRInstance.accMotion * controllerSpeedMultiplier;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            virtualInputSpeed += debugWKeyPower;
        }

        virtualInputSpeed -= debugDecaySpeed * Time.deltaTime;

        virtualInputSpeed = Mathf.Clamp(virtualInputSpeed, 0f, maxSpeedLimit + 1f);

        float currentInputSpeed = (chasenSpeed > 0.1f) ? chasenSpeed : virtualInputSpeed;

        hasInputThisFrame = (chasenSpeed > 0.1f) || (virtualInputSpeed > 0.1f);

        if (playerController.getIsStuckInMud())
        {
            float progressRatio = Mathf.Clamp01(mudProgress / mudMaxProgress);

            // 90° → 0°
            targetZAngle = Mathf.Lerp(maxGaugeAngle, 0f, progressRatio);
        }
        else
        {

            float speedRatio = currentInputSpeed / maxSpeedLimit;
            speedRatio = Mathf.Clamp01(speedRatio);

            targetZAngle = Mathf.Lerp(maxGaugeAngle, -maxGaugeAngle, speedRatio);
        }


        currentZAngle = Mathf.LerpAngle(currentZAngle, targetZAngle, Time.deltaTime * smoothSpeed);

        rikyuPivot.localRotation = Quaternion.Euler(0f, 0f, currentZAngle);
   
        if(!playerController.getIsStuckInMud())
        {
            CheckChargeSound();
        }
    }

    public void ResetGauge()
    {
        currentZAngle = maxGaugeAngle;
        targetZAngle = maxGaugeAngle;
        hasPlayedChargeSound = false;
        rikyuPivot.localRotation = Quaternion.Euler(0f, 0f, currentZAngle);
    }

    public void SetMudProgress(float progress, float maxProgress)
    {
        mudProgress = progress;
        mudMaxProgress = maxProgress;
    }

    public float GetCurrentAngle()
    {
        return currentZAngle;
    }

    public bool HasInput()
    {
        return hasInputThisFrame;
    }

    private void CheckChargeSound()
    {
        if(hasPlayedChargeSound)
        {
            return;
        }

        if (!hasInputThisFrame)
        {
            return;
        }

        if(currentZAngle <= chargeSoundThreshold)
        {
            AudioManager.Instance.PlaySE(chargeSoundIndex);
            hasPlayedChargeSound = true;
        }
    }
}
