using UnityEngine;

public class FallingObject : MonoBehaviour
{
    [SerializeField] private float triggerDistanceZ = 35f;

    Rigidbody rb;
    private PlayerController playerController;
    private bool isFall = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerController = FindFirstObjectByType<PlayerController>();

        transform.SetParent(null);

        if(rb != null)
        {
            rb.useGravity = false;
        }
    }

    void Update()
    {
        if(playerController == null) return;

        if (!isFall)
        {
            float distanceToPlayer = transform.position.z - playerController.transform.position.z;

            if(distanceToPlayer <= triggerDistanceZ)
            {
                transform.SetParent(null);

                rb.useGravity = true;
                isFall = true;
            }
        }

        if(transform.position.z < playerController.transform.position.z - 30f)
        {
            Destroy(gameObject);
        }
    }
}
