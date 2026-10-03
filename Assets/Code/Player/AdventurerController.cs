using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class AdventurerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4.5f;
    public float runSpeed = 7.5f;
    public float rotationSpeed = 14f;
    public float gravity = -24f;
    public float jumpHeight = 2.2f;
    public float clickStopDistance = 0.25f;
    public float accelerationTime = 0.14f;
    public float decelerationTime = 0.2f;

    [Header("References")]
    public Animator animator;
    public Camera gameplayCamera;

    private CharacterController characterController;
    private Vector3 clickDestination;
    private bool hasClickDestination;
    private float verticalVelocity;
    private Vector3 planarVelocity;
    private Vector3 planarVelocityDamp;

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
        Vector3 desiredPlanarVelocity = movement.sqrMagnitude > .001f ? movement.normalized * speed : Vector3.zero;
        float smoothTime = desiredPlanarVelocity.sqrMagnitude > planarVelocity.sqrMagnitude ? accelerationTime : decelerationTime;
        planarVelocity = Vector3.SmoothDamp(planarVelocity, desiredPlanarVelocity, ref planarVelocityDamp, smoothTime, 40f, Time.deltaTime);

        if (planarVelocity.sqrMagnitude > 0.025f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(planarVelocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-rotationSpeed * Time.deltaTime));
        }

        if (characterController.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        if (characterController.isGrounded && Input.GetKeyDown(KeyCode.Space))
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        verticalVelocity += gravity * Time.deltaTime;
        Vector3 velocity = planarVelocity + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        if (animator != null)
        {
            float normalizedSpeed = Mathf.Clamp01(planarVelocity.magnitude / Mathf.Max(runSpeed, .01f));
            animator.SetFloat(SpeedHash, normalizedSpeed, 0.18f, Time.deltaTime);
            if (Input.GetMouseButtonDown(0)) animator.SetTrigger(AttackHash);
        }
    }

    private void HandleClickDestination()
    {
        // Left mouse is reserved for combat. Right mouse keeps the optional
        // click-to-move navigation without competing with attacks.
        if (!Input.GetMouseButtonDown(1) || gameplayCamera == null) return;
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
