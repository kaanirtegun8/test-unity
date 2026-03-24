using UnityEngine;

[DisallowMultipleComponent]
public class TestPlayerLocalMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float arenaMinX = -9.5f;
    [SerializeField] private float arenaMaxX = 9.5f;
    [SerializeField] private float arenaMinZ = -9.5f;
    [SerializeField] private float arenaMaxZ = 9.5f;

    private Rigidbody playerBody;
    private Vector3 movementInput;
    private float fixedY;

    private void Awake()
    {
        fixedY = transform.position.y;
        playerBody = GetComponent<Rigidbody>();
        if (playerBody == null)
        {
            playerBody = gameObject.AddComponent<Rigidbody>();
        }

        playerBody.useGravity = false;
        playerBody.isKinematic = true;
        playerBody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
    }

    private void Update()
    {
        movementInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (movementInput.sqrMagnitude > 1f)
        {
            movementInput.Normalize();
        }
    }

    private void FixedUpdate()
    {
        if (playerBody == null)
        {
            return;
        }

        Vector3 nextPosition = playerBody.position + (movementInput * moveSpeed * Time.fixedDeltaTime);
        nextPosition.x = Mathf.Clamp(nextPosition.x, arenaMinX, arenaMaxX);
        nextPosition.y = fixedY;
        nextPosition.z = Mathf.Clamp(nextPosition.z, arenaMinZ, arenaMaxZ);

        playerBody.MovePosition(nextPosition);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
    }
#endif
}
