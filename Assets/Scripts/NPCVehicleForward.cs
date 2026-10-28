using UnityEngine;

public class NPCVehicleForward : MonoBehaviour
{
    [Header("基本速度")]
    [SerializeField] float moveSpeed = 20f;

    [Header("前方監視設定")]
    [SerializeField] float rayDistance = 8f;
    [SerializeField] LayerMask npcLayer;


    [Header("車線変更設定")]
    [SerializeField] private float laneChangeSpeed = 6f;
    [SerializeField] private float laneDistance = 2.8f;



    private float currentSpeed;
    private PlayerController playerController;

    //車線管理用の変数
    private int currentLane = 1; //0:左 1:中央 2:右
    private float targetX;
    private bool isChangingLane = false;
    private float laneChangeCooldown = 0f;

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();

        transform.SetParent(null);

        currentSpeed = moveSpeed;

        targetX = transform.position.x;
        if (targetX < -1f) currentLane = 0;
        else if (targetX > 1f) currentLane = 2;
        else currentLane = 1;
    }

    void Update()
    {
        if (playerController == null || playerController.getIsStuckInMud()) return;

        if (laneChangeCooldown > 0f) laneChangeCooldown -= Time.deltaTime;

        CheckForwardAndChangeLane();

        MoveCarForward();
    }

    private void MoveCarForward()
    {
        // 位置計算（自身の前進速度 - 地面の流れる速度）
        Vector3 movePosition = transform.position;
        float relativeSpeedZ = currentSpeed - playerController.CurrentMapSpeed;
        movePosition.z += relativeSpeedZ * Time.deltaTime;

        // 避けるための横移動
        float newX = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);
        movePosition.x = newX;

        if (isChangingLane && Mathf.Abs(transform.position.x - targetX) < 0.05f)
        {
            isChangingLane = false;
            laneChangeCooldown = 1.0f;
        }

        transform.position = movePosition;

        if (transform.position.z > playerController.transform.position.z + 120f)
        {
            Destroy(gameObject);
        }
        else if (transform.position.z < playerController.transform.position.z - 40f)
        {
            Destroy(gameObject);
        }
    }



    public void InitSpeed(float customSpeed)
    {
        if (customSpeed > 0f)
        {
            moveSpeed = customSpeed;
        }
        currentSpeed = moveSpeed;
    }

    //前方に他の車がいないかチェック
    void CheckForwardAndChangeLane()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        Vector3 rayDirection = transform.forward;
        RaycastHit hit;

        Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.red);

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, npcLayer))
        {
            if (!isChangingLane && laneChangeCooldown <= 0f)
            {
                TryChooseEscapeLane();
            }

            float forwardSpeed = 0f;

            if (hit.collider.TryGetComponent<NPCVehicleForward>(out var forwardNPC1))
            {
                forwardSpeed = forwardNPC1.GetCurrentSpeed();
            }
            else if (hit.collider.TryGetComponent<NPCVehicle>(out var forwardNPC2))
            {
                forwardSpeed = forwardNPC2.GetCurrentSpeed();
            }

            if (forwardSpeed < currentSpeed)
            {
                currentSpeed = forwardSpeed;
                return;
            }
        }
        currentSpeed = moveSpeed;
    }

    //前の車を避ける為に隣のレーンを選択
    void TryChooseEscapeLane()
    {
        int newLane = currentLane;

        if (currentLane == 1)
        {
            newLane = (Random.Range(0, 2) == 0) ? 0 : 2;
        }
        else if (currentLane == 0)
        {
            newLane = 1;
        }
        else if (currentLane == 2)
        {
            newLane = 1;
        }

        if (newLane != currentLane)
        {
            currentLane = newLane;
            targetX = (currentLane - 1) * laneDistance;
            isChangingLane = true;
        }
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }
}
