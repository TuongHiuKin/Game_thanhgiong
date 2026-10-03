using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class AdventurerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4.5f;
    public float runSpeed = 7.5f;
    public float rotationSpeed = 14f;
    public float gravity = -24f;
    public float clickStopDistance = 0.25f;

    [Header("References")]
    public Animator animator;
    public Camera gameplayCamera;

    private CharacterController characterController;
    private Vector3 clickDestination;
    private bool hasClickDestination;
    private float verticalVelocity;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (gameplayCamera == null) gameplayCamera = Camera.main;
        clickDestination = transform.position;
    }

    private void Update()
    {
        HandleClickDestination();

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector3 movement = input.sqrMagnitude > 0.01f ? CameraRelativeDirection(input) : ClickMovement();
        bool running = Input.GetKey(KeyCode.LeftShift) && input.sqrMagnitude > 0.01f;
        float speed = running ? runSpeed : walkSpeed;

        if (movement.sqrMagnitude > 0.001f)
        {
            movement.Normalize();
            Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (characterController.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        Vector3 velocity = movement * speed + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        if (animator != null)
        {
            float normalizedSpeed = movement.sqrMagnitude > 0.01f ? (running ? 1f : 0.55f) : 0f;
            animator.SetFloat(SpeedHash, normalizedSpeed, 0.12f, Time.deltaTime);
            if (Input.GetKeyDown(KeyCode.Space)) animator.SetTrigger(AttackHash);
        }
    }

    private void HandleClickDestination()
    {
        if (!Input.GetMouseButtonDown(0) || gameplayCamera == null) return;
        Ray ray = gameplayCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 250f, ~0, QueryTriggerInteraction.Ignore))
        {
            clickDestination = hit.point;
            hasClickDestination = true;
        }
    }

    private Vector3 CameraRelativeDirection(Vector2 input)
    {
        hasClickDestination = false;
        if (gameplayCamera == null) return new Vector3(input.x, 0, input.y);
        Vector3 forward = gameplayCamera.transform.forward;
        Vector3 right = gameplayCamera.transform.right;
        forward.y = 0;
        right.y = 0;
        return (forward.normalized * input.y + right.normalized * input.x).normalized;
    }

    private Vector3 ClickMovement()
    {
        if (!hasClickDestination) return Vector3.zero;
        Vector3 delta = clickDestination - transform.position;
        delta.y = 0;
        if (delta.magnitude <= clickStopDistance)
        {
            hasClickDestination = false;
            return Vector3.zero;
        }
        return delta.normalized;
    }
}
