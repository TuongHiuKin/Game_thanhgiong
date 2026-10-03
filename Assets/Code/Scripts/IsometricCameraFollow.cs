using UnityEngine;

public class IsometricCameraFollow : MonoBehaviour
{
    public enum Shot { Explore, Combat, Vista, Boss, Close }
    public static IsometricCameraFollow Instance { get; private set; }

    public Transform target;
    public Vector3 offset = new Vector3(-18f, 22f, -18f);
    public float followSmoothness = 7f;
    public float lookHeight = 1.2f;
    [Header("Living Painted Camera")]
    public Shot currentShot = Shot.Explore;
    public float fieldOfView = 34f;
    public float lookAhead = 3.5f;
    public float pitch = 52f;
    public Rigidbody targetBody;

    private float shakeTimer;
    private float shakeIntensity;
    private bool isOverview;
    private Vector3 overviewPosition;
    private Vector3 overviewLookAt;

    private void Awake()
    {
        Instance = this;
        Camera cameraComponent = GetComponent<Camera>();
        if (cameraComponent != null) cameraComponent.fieldOfView = fieldOfView;
    }

    public void SetShot(Shot shot)
    {
        currentShot = shot;
        switch (shot)
        {
            case Shot.Combat: offset = new Vector3(-14f, 18f, -14f); lookAhead = 2f; break;
            case Shot.Vista: offset = new Vector3(-34f, 42f, -34f); lookAhead = 0f; break;
            case Shot.Boss: offset = new Vector3(-22f, 28f, -22f); lookAhead = 0f; break;
            case Shot.Close: offset = new Vector3(-8f, 10f, -8f); lookAhead = 0f; break;
            default: offset = new Vector3(-18f, 22f, -18f); lookAhead = 3.5f; break;
        }
    }

    public void Shake(float duration, float intensity)
    {
        shakeTimer = Mathf.Max(shakeTimer, duration);
        shakeIntensity = Mathf.Max(shakeIntensity, intensity);
    }

    public void SetOverview(Vector3 position, Vector3 lookAtPoint)
    {
        isOverview = true;
        overviewPosition = position;
        overviewLookAt = lookAtPoint;
    }

    public void ClearOverview()
    {
        isOverview = false;
    }

    public void SnapToTarget()
    {
        isOverview = false;
        if (target == null) return;
        transform.position = target.position + offset;
        transform.rotation = Quaternion.LookRotation(target.position + Vector3.up * lookHeight - transform.position, Vector3.up);
    }

    private void LateUpdate()
    {
        Vector3 basePos;
        Quaternion baseRot;

        if (isOverview)
        {
            basePos = overviewPosition;
            baseRot = Quaternion.LookRotation(overviewLookAt - overviewPosition, Vector3.up);
            transform.position = Vector3.Lerp(transform.position, basePos, 1f - Mathf.Exp(-followSmoothness * 0.5f * Time.deltaTime));
            transform.rotation = Quaternion.Slerp(transform.rotation, baseRot, 1f - Mathf.Exp(-followSmoothness * 0.5f * Time.deltaTime));
        }
        else
        {
            if (target == null) return;
            if (targetBody == null) targetBody = target.GetComponent<Rigidbody>();
            Vector3 velocity = targetBody != null ? targetBody.linearVelocity : Vector3.zero;
            velocity.y = 0f;
            Vector3 lead = velocity.sqrMagnitude > .01f ? velocity.normalized * lookAhead * Mathf.Clamp01(velocity.magnitude / 4f) : Vector3.zero;
            Vector3 focusPoint = target.position + lead;
            Vector3 desiredPosition = focusPoint + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-followSmoothness * Time.deltaTime));
            Quaternion desiredRotation = Quaternion.LookRotation(focusPoint + Vector3.up * lookHeight - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, 1f - Mathf.Exp(-followSmoothness * Time.deltaTime));
        }

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float trauma = Mathf.Clamp01(shakeIntensity);
            Vector3 randomOffset = Random.insideUnitSphere * trauma * trauma * .45f;
            transform.position += randomOffset;
            shakeIntensity = Mathf.MoveTowards(shakeIntensity, 0f, Time.deltaTime * 3.5f);
        }
    }
}
