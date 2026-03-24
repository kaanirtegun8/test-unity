using UnityEngine;

[DisallowMultipleComponent]
public class TestPlayerLocalMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float arenaMinX = -9.5f;
    [SerializeField] private float arenaMaxX = 9.5f;
    [SerializeField] private float arenaMinZ = -9.5f;
    [SerializeField] private float arenaMaxZ = 9.5f;

    private float fixedY;

    private void Awake()
    {
        fixedY = transform.position.y;
    }

    private void Update()
    {
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        Vector3 nextPosition = transform.position + (input * moveSpeed * Time.deltaTime);
        nextPosition.x = Mathf.Clamp(nextPosition.x, arenaMinX, arenaMaxX);
        nextPosition.y = fixedY;
        nextPosition.z = Mathf.Clamp(nextPosition.z, arenaMinZ, arenaMaxZ);

        transform.position = nextPosition;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
    }
#endif
}
