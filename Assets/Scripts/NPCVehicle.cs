using System.Reflection;
using UnityEngine;

public class NPCVehicle : MonoBehaviour
{
    [SerializeField] float moveSpeed = 10f;

    [Header("前方監視設定")]
    [SerializeField] float rayDistance = 5f;
    [SerializeField] LayerMask npcLayer;

    [Header("車線変更用設定")]
    [SerializeField] private bool isLaneChanger = false; //車線変更タイプか
    [SerializeField] private float laneChangeSpeed = 5f;
    [SerializeField] private float laneDistance = 2.8f;
    [SerializeField] private float triggerDistanceZ = 35f;

    [Header("急ブレーキ車用設定")]
    [SerializeField] private bool isBrakeTrap = false;
    [SerializeField] private float breakTriggerDistance = 30f;
    [SerializeField] private float brakePower = 20f;
    private bool isBraking = false;

    private float myOwnSpeed;
    private float currentSpeed;
    private PlayerController playerController;

    private float targetX;
    private bool hasStartedChange = false;
    private bool isChangeComplete = false;

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        transform.SetParent(null);

        if (currentSpeed == 0f)
        {
            myOwnSpeed = moveSpeed;
            currentSpeed = moveSpeed;
        }

        DetermineTargetLane();
    }

    void Update()
    {
        if (playerController == null)
        {
            return;
        }

        if (playerController.getIsStuckInMud())
        {
            return;
        }

        moveCar();
    }

    private void moveCar()
    {
        if (isBrakeTrap)
        {
            float distanceToPlayer = transform.position.z - playerController.transform.position.z;

            if (!isBraking && distanceToPlayer <= breakTriggerDistance)
            {
                isBraking = true;
            }

            if (isBraking && currentSpeed > 0f)
            {
                currentSpeed -= brakePower * Time.deltaTime;
                if (currentSpeed < 0f) currentSpeed = 0f;
            }
        }
        else
        {
            // 通常車、または車線変更車なら前方監視を行う
            if (!isLaneChanger || isChangeComplete)
            {
                CheckForwardVehicle();
            }
        }

        float finalSpeedZ = currentSpeed + playerController.CurrentMapSpeed;
        Vector3 movePosition = transform.position - transform.forward * finalSpeedZ * Time.deltaTime;

        // 車線変更の処理
        if (isLaneChanger && !isChangeComplete && !isBrakeTrap)
        {
            if (!hasStartedChange)
            {
                float distanceToPlayer = transform.position.z - playerController.transform.position.z;
                if (distanceToPlayer <= triggerDistanceZ)
                {
                    hasStartedChange = true;
                }
            }

            if (hasStartedChange)
            {
                float newX = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);
                movePosition.x = newX;

                if (Mathf.Abs(transform.position.x - targetX) < 0.05f)
                {
                    isChangeComplete = true;
                }
            }
        }

        transform.position = movePosition;

        if (transform.position.z < playerController.transform.position.z - 30f)
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

        myOwnSpeed = moveSpeed;
        currentSpeed = moveSpeed;
    }

    //車線変更
    void DetermineTargetLane()
    {
        float currentX = transform.position.x;

        if (currentX < -1f)
        {
            targetX = 0f;
        }
        else if (currentX > 1f)
        {
            targetX = 0f;
        }
        else
        {
            targetX = (Random.Range(0, 2) == 0) ? -laneDistance : laneDistance;
        }
    }

    //前方に車がいないかチェック
    void CheckForwardVehicle()
    {
        //レーザ開始位置
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        //レーザ方向
        Vector3 rayDirection = -transform.forward;

        RaycastHit hit;

        Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.red);

        //敵の車だけに反応するレイキャスト
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, npcLayer))
        {
            NPCVehicle forwardVehicle = hit.collider.GetComponent<NPCVehicle>();

            if (forwardVehicle != null)
            {
                currentSpeed = forwardVehicle.GetCurrentSpeed();
                return;
            }
        }
        currentSpeed = myOwnSpeed;
    }

    //他の車が自身の速度を読み取れるようにする関数
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }
}
