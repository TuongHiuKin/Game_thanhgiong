using UnityEngine;

public class IsometricCameraFollow : MonoBehaviour
{
    public enum Shot { Explore, Combat, Vista, Boss, Close }
    public static IsometricCameraFollow Instance { get; private set; }
    public Transform target;
    public Vector3 offset = new Vector3(-18f, 22f, -18f);
    public float followSmoothness = 7f;
    public float lookHeight = 1.2f;
    public Shot currentShot = Shot.Explore;
    public float fieldOfView = 34f;
    public float lookAhead = 2.5f;
    public float pitch = 35.26439f;
    public Rigidbody targetBody;

    [Header("2.5D isometric framing")]
    public bool useOrthographicIsometric = true;
    [Header("Arena third person camera")]
    public bool arenaPerspective;
    public enum ArenaCameraMode { ThirdPersonFar, ThirdPersonClose, FirstPerson }
    public ArenaCameraMode arenaCameraMode = ArenaCameraMode.ThirdPersonFar;
    public float arenaDistance = 15.5f;
    public float arenaPitch = 28f;
    public float arenaYaw;
    public float arenaAimDistance = 3.4f;
    public float arenaAimHeight = 1.25f;
    public float arenaShoulderOffset = 1.4f;
    public Vector3 ArenaAimPoint
    {
        get
        {
            var ray = new Ray(transform.position, transform.forward);
            var plane = new Plane(Vector3.up, target != null ? target.position + Vector3.up * arenaAimHeight : Vector3.zero);
            return plane.Raycast(ray, out float distance) && distance < 500f ? ray.GetPoint(distance) : ray.GetPoint(50f);
        }
    }
    public Vector3 firstPersonEyeOffset = new Vector3(0f, 3.25f, .65f);
    public float arenaZoomPerNotch = 2f;
    public float isometricPitch = 35.26439f;
    public float isometricYaw = 45f;
    public float cameraDistance = 38f;
    public float exploreSize = 13f, combatSize = 11f, vistaSize = 27f, bossSize = 17f, closeSize = 8f;
    public float zoomSmoothness = 5f;
    public float leadSeconds = .3f;
    public float maximumShake = .32f;

    Camera view;
    CharacterController targetController;
    MountedHorseController targetMovement;
    Transform boundTarget;
    Vector3 smoothPosition, smoothLead;
    float shakeTimer, shakeIntensity, shakeAge;
    bool isOverview, initialized;
    Vector3 overviewPosition, overviewLookAt;

    void Awake()
    {
        Instance = this;
        view = GetComponent<Camera>();
        if (view != null) view.farClipPlane = Mathf.Max(view.farClipPlane, 1000f);
        ApplyProjection();
    }
    void OnDestroy() { if (Instance == this) Instance = null; }
    float ShotSize => currentShot == Shot.Combat ? combatSize : currentShot == Shot.Vista ? vistaSize : currentShot == Shot.Boss ? bossSize : currentShot == Shot.Close ? closeSize : exploreSize;
    Quaternion IsometricRotation => Quaternion.Euler(isometricPitch, isometricYaw, 0f);

    public void SetArenaCameraMode(ArenaCameraMode mode)
    {
        arenaCameraMode = mode;
        switch (mode)
        {
            case ArenaCameraMode.ThirdPersonFar:
                arenaDistance = 15.5f;
                lookHeight = 1.4f;
                arenaPitch = 28f;
                fieldOfView = 62f;
                break;
            case ArenaCameraMode.ThirdPersonClose:
                arenaDistance = 9.0f;
                lookHeight = 1.2f;
                arenaPitch = 24f;
                fieldOfView = 58f;
                break;
            case ArenaCameraMode.FirstPerson:
                arenaDistance = 0f;
                lookHeight = firstPersonEyeOffset.y;
                arenaPitch = 12f;
                fieldOfView = 75f;
                break;
        }
        ApplyProjection();
    }

    public void CycleArenaCameraMode()
    {
        var next = (ArenaCameraMode)(((int)arenaCameraMode + 1) % 3);
        SetArenaCameraMode(next);
    }

    void ApplyProjection()
    {
        if (view == null) view = GetComponent<Camera>();
        if (view == null) return;
        view.orthographic = useOrthographicIsometric && !arenaPerspective;
        view.fieldOfView = fieldOfView;
        if (!initialized) view.orthographicSize = Mathf.Max(1f, ShotSize);
    }

    public void SetShot(Shot shot)
    {
        currentShot = shot;
        // Retain the perspective API for legacy consumers that explicitly disable isometric mode.
        switch (shot) {
            case Shot.Combat: offset = new Vector3(-14f, 18f, -14f); lookAhead = 2f; break;
            case Shot.Vista: offset = new Vector3(-34f, 42f, -34f); lookAhead = 0f; break;
            case Shot.Boss: offset = new Vector3(-22f, 28f, -22f); lookAhead = 0f; break;
            case Shot.Close: offset = new Vector3(-8f, 10f, -8f); lookAhead = 0f; break;
            default: offset = new Vector3(-18f, 22f, -18f); lookAhead = 2.5f; break;
        }
    }
    public void Shake(float duration, float intensity) { shakeTimer = Mathf.Max(shakeTimer, Mathf.Max(0, duration)); shakeIntensity = Mathf.Max(shakeIntensity, Mathf.Clamp01(intensity)); }
    public void SetOverview(Vector3 position, Vector3 lookAtPoint) { isOverview = true; overviewPosition = position; overviewLookAt = lookAtPoint; }
    public void ClearOverview() { isOverview = false; }
    public void SnapToTarget()
    {
        isOverview = false;
        if (target == null) return;
        smoothLead = Vector3.zero; ApplyProjection();
        Quaternion rotation = arenaPerspective ? Quaternion.Euler(arenaPitch, target.eulerAngles.y + arenaYaw, 0) : useOrthographicIsometric ? IsometricRotation : Quaternion.LookRotation(Vector3.up * lookHeight - offset, Vector3.up);
        smoothPosition = useOrthographicIsometric ? target.position + Vector3.up * lookHeight - rotation * Vector3.forward * cameraDistance : target.position + offset;
        if (arenaPerspective)
        {
            if (arenaCameraMode == ArenaCameraMode.FirstPerson)
                smoothPosition = FirstPersonEyePosition;
            else
            {
                smoothPosition = ArenaThirdPersonPosition(rotation);
            }
        }
        transform.SetPositionAndRotation(smoothPosition, rotation);
        if (view != null) view.orthographicSize = Mathf.Max(1f, ShotSize);
        shakeTimer = shakeIntensity = shakeAge = 0f; initialized = true;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        ApplyProjection();
        if (arenaPerspective && !isOverview)
        {
            if (ThanhGiongArenaInput.Down(KeyCode.V))
            {
                CycleArenaCameraMode();
            }

            float scroll = ThanhGiongArenaInput.ScrollDelta;
            if (Mathf.Abs(scroll) > 0.001f && arenaCameraMode != ArenaCameraMode.FirstPerson)
            {
                arenaDistance = Mathf.Clamp(arenaDistance - scroll * arenaZoomPerNotch, 4.5f, 28f);
            }

            var arena = target != null ? target.GetComponent<ThanhGiongArenaFlow>() : null;
            bool mouseLook = arena != null ? arena.MouseLookActive : ThanhGiongArenaInput.OrbitHeld;
            if (mouseLook)
            {
                Vector2 delta = ThanhGiongArenaInput.OrbitDelta;
                arenaYaw = Mathf.Repeat(arenaYaw + delta.x * 3f + 180f, 360f) - 180f;
                arenaPitch = Mathf.Clamp(arenaPitch - delta.y * 2f, -35f, 75f);
            }
        }
        if (!isOverview && target == null) return;
        Vector3 focus, desiredPosition;
        Quaternion desiredRotation;
        float size = ShotSize;
        if (isOverview)
        {
            focus = overviewLookAt;
            desiredRotation = useOrthographicIsometric ? IsometricRotation : Quaternion.LookRotation(overviewLookAt - overviewPosition, Vector3.up);
            desiredPosition = useOrthographicIsometric ? focus - desiredRotation * Vector3.forward * Mathf.Max(cameraDistance, Vector3.Distance(overviewPosition, overviewLookAt)) : overviewPosition;
            size = Mathf.Max(vistaSize, Vector3.Distance(overviewPosition, overviewLookAt) * .4f);
        }
        else
        {
            if (boundTarget != target) { boundTarget = target; targetController = target.GetComponent<CharacterController>(); targetMovement = target.GetComponent<MountedHorseController>(); targetBody = target.GetComponent<Rigidbody>(); }
            Vector3 velocity = targetMovement != null && targetMovement.enabled ? targetMovement.WorldVelocity : targetController != null && targetController.enabled ? targetController.velocity : targetBody != null ? targetBody.linearVelocity : Vector3.zero;
            velocity.y = 0;
            Vector3 lead = Vector3.ClampMagnitude(velocity * leadSeconds, Mathf.Max(0, lookAhead));
            smoothLead = Vector3.Lerp(smoothLead, lead, 1f - Mathf.Exp(-5f * dt));
            focus = target.position + Vector3.up * lookHeight + smoothLead;
            desiredRotation = useOrthographicIsometric ? IsometricRotation : Quaternion.LookRotation(Vector3.up * lookHeight - offset, Vector3.up);
            desiredPosition = useOrthographicIsometric ? focus - desiredRotation * Vector3.forward * cameraDistance : target.position + smoothLead + offset;
            if (arenaPerspective)
            {
                if (arenaCameraMode == ArenaCameraMode.FirstPerson)
                {
                    desiredRotation = Quaternion.Euler(arenaPitch, target.eulerAngles.y + arenaYaw, 0);
                    desiredPosition = FirstPersonEyePosition;
                }
                else
                {
                    // Orbit offset follows the rider's heading. The level is
                    // stationary; turning the view makes it rotate on screen.
                    focus = target.position + Vector3.up * lookHeight;
                    desiredRotation = Quaternion.Euler(arenaPitch, target.eulerAngles.y + arenaYaw, 0);
                    desiredPosition = ArenaThirdPersonPosition(desiredRotation);
                    Vector3 direction = desiredPosition - focus;
                    float distance = direction.magnitude;
                    foreach (RaycastHit hit in Physics.SphereCastAll(focus, .3f, direction.normalized, distance, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider == null || hit.collider.isTrigger) continue;
                        if (hit.collider.transform.root == target.root || hit.collider.GetComponentInParent<ThanhGiongEnemy>() != null) continue;
                        string n = hit.collider.name;
                        if (n.IndexOf("boundary", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            n.IndexOf("ground", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            n.IndexOf("plain", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            n.IndexOf("grass", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            n.IndexOf("battlefield", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            continue;
                        distance = Mathf.Min(distance, Mathf.Max(2.5f, hit.distance - .25f));
                    }
                    desiredPosition = focus + direction.normalized * distance;
                }
            }
        }
        if (!initialized) { smoothPosition = desiredPosition; transform.rotation = desiredRotation; initialized = true; }
        float blendSpeed = arenaPerspective ? 24f : followSmoothness;
        float blend = 1f - Mathf.Exp(-blendSpeed * (isOverview ? .5f : 1f) * dt);
        smoothPosition = Vector3.Lerp(smoothPosition, desiredPosition, blend);
        // Campaign isometric views retain their fixed axes.
        transform.rotation = useOrthographicIsometric ? desiredRotation : Quaternion.Slerp(transform.rotation, desiredRotation, blend);
        if (view != null) view.orthographicSize = Mathf.Lerp(view.orthographicSize, Mathf.Max(1f, size), 1f - Mathf.Exp(-zoomSmoothness * dt));
        Vector3 shake = Vector3.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer = Mathf.Max(0, shakeTimer - dt); shakeAge += dt;
            float amplitude = Mathf.Clamp01(shakeIntensity) * Mathf.Clamp01(shakeIntensity) * Mathf.Max(0, maximumShake);
            shake = (transform.right * Mathf.Sin(shakeAge * 67f) + transform.up * Mathf.Sin(shakeAge * 83f + .8f)) * (amplitude * .7071f);
            shakeIntensity = Mathf.MoveTowards(shakeIntensity, 0f, dt * 3.5f);
        }
        transform.position = smoothPosition + shake;
        // Arena rotation comes from mouse yaw/pitch. Looking back at the horse's
        // forward strike point here would cancel mouse aiming every frame.
    }
    Vector3 FirstPersonEyePosition => target.position + Vector3.up * firstPersonEyeOffset.y
        + target.right * firstPersonEyeOffset.x + target.forward * firstPersonEyeOffset.z;
    Vector3 ArenaThirdPersonPosition(Quaternion orbitRotation)
    {
        Vector3 direction = -(orbitRotation * Vector3.forward) * arenaDistance + (orbitRotation * Vector3.right) * arenaShoulderOffset;
        Vector3 forward = Quaternion.Euler(0, target.eulerAngles.y + arenaYaw, 0) * Vector3.forward;
        Vector3 position = target.position + Vector3.up * lookHeight + forward * arenaAimDistance + direction.normalized * arenaDistance;
        position.y = Mathf.Max(position.y, target.position.y + .75f);
        return position;
    }
}
