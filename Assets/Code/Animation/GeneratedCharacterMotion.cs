using UnityEngine;

/// <summary>
/// Gives the single-mesh Hyper3D characters a small procedural skin at runtime.
/// The imported GLBs stay untouched so their PBR materials remain reusable.
/// </summary>
[DisallowMultipleComponent]
public sealed class GeneratedCharacterMotion : MonoBehaviour
{
    public enum CharacterKind { IronHorse, ThanhGiong }

    public CharacterKind kind;
    public float runSpeed = 8.5f;

    private MeshRenderer sourceRenderer;
    private Mesh runtimeMesh;
    private Transform[] bones;
    private Transform skinTransform;
    private Transform bambooGrip;
    private Vector3 bambooGripRestPosition;
    private Quaternion bambooGripRestRotation;
    private MountedHorseController mount;
    private Vector3 previousMountPosition;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private bool mountedSourceMesh;
    private float phase;
    private float smoothedSpeed;
    private float reportedSpeed;
    private float verticalSpeed;
    private float attackElapsed = -1f;
    private float attackDuration = .58f;
    private float landingTime;
    private bool hasMotionState;
    private bool hasExternalGaitPhase;
    private float externalGaitPhase;
    private bool grounded = true;
    private bool wasGrounded = true;

    public void SetMotionState(float horizontalSpeed, bool isGrounded, float currentVerticalSpeed, float gaitPhase)
    {
        reportedSpeed = Mathf.Max(0f, horizontalSpeed);
        grounded = isGrounded;
        verticalSpeed = currentVerticalSpeed;
        hasMotionState = true;
        hasExternalGaitPhase = true;
        externalGaitPhase = gaitPhase;
        if (grounded && !wasGrounded) landingTime = .24f;
        wasGrounded = grounded;
    }

    public void TriggerAttack(float duration)
    {
        attackDuration = Mathf.Max(.1f, duration);
        attackElapsed = 0f;
    }

    public void TriggerJump()
    {
        grounded = false;
        wasGrounded = false;
        landingTime = 0f;
    }

    public Transform BambooGrip => bambooGrip;

    private void Awake()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        sourceRenderer = GetComponent<MeshRenderer>();
        if (filter == null || filter.sharedMesh == null || sourceRenderer == null ||
            !filter.sharedMesh.isReadable)
        {
            Debug.LogWarning("GeneratedCharacterMotion needs a readable GLB mesh and MeshRenderer.", this);
            enabled = false;
            return;
        }

        mount = GetComponentInParent<MountedHorseController>();
        previousMountPosition = mount != null ? mount.transform.position : transform.position;
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
        mountedSourceMesh = filter.sharedMesh.name.Contains("MountedPose");
        BuildSkin(filter.sharedMesh);
    }

    private void BuildSkin(Mesh sourceMesh)
    {
        bool horse = kind == CharacterKind.IronHorse;
        Vector3[] anchors = horse
            ? new[] { Vector3.zero, new Vector3(-.14f, -.10f, .30f),
                new Vector3(.14f, -.10f, .30f), new Vector3(-.14f, -.10f, -.43f),
                new Vector3(.14f, -.10f, -.43f), new Vector3(0f, .27f, .56f),
                new Vector3(0f, .02f, -.72f),
                new Vector3(-.14f, -.55f, .30f), new Vector3(.14f, -.55f, .30f),
                new Vector3(-.14f, -.55f, -.43f), new Vector3(.14f, -.55f, -.43f) }
            : new[] { Vector3.zero, new Vector3(-.25f, .34f, 0f),
                new Vector3(.25f, .34f, 0f), new Vector3(-.14f, -.25f, 0f),
                new Vector3(.14f, -.25f, 0f), new Vector3(0f, .55f, 0f),
                new Vector3(-.14f, -.56f, 0f), new Vector3(.14f, -.56f, 0f) };

        bones = new Transform[anchors.Length];
        Matrix4x4[] bindPoses = new Matrix4x4[anchors.Length];
        for (int i = 0; i < anchors.Length; i++)
        {
            GameObject bone = new GameObject("MotionBone_" + i);
            bone.transform.SetParent(transform, false);
            bone.transform.localPosition = anchors[i];
            bones[i] = bone.transform;
        }

        if (horse)
        {
            for (int i = 0; i < 4; i++)
                bones[7 + i].SetParent(bones[1 + i], true);
        }
        else
        {
            // Calves follow the thighs. A single rotating leg bone lifted the
            // boots in front of the rider instead of letting them hang down.
            bones[6].SetParent(bones[3], true);
            bones[7].SetParent(bones[4], true);
        }
        for (int i = 0; i < bones.Length; i++)
            bindPoses[i] = bones[i].worldToLocalMatrix * transform.localToWorldMatrix;

        if (!horse)
        {
            // The grip is authored beside the visible hand so Edit and Play
            // mode show the same staff placement.
            bambooGrip = transform.Find("BambooGrip");
            if (bambooGrip == null)
            {
                bambooGrip = new GameObject("BambooGrip").transform;
                bambooGrip.SetParent(transform, false);
                bambooGrip.localPosition = new Vector3(-.36f, 0f, .37f);
                bambooGrip.localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(.08f, .25f, .97f).normalized);
            }
            bambooGripRestPosition = bambooGrip.localPosition;
            bambooGripRestRotation = bambooGrip.localRotation;
        }

        Vector3[] vertices = sourceMesh.vertices;
        BoneWeight[] weights = new BoneWeight[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            int limb;
            float influence;
            if (horse) HorseWeight(vertices[i], out limb, out influence);
            else HeroWeight(vertices[i], out limb, out influence);

            bool leg = horse ? limb >= 1 && limb <= 4 : limb == 3 || limb == 4;
            float calf = leg ? horse
                ? Smooth((-.48f - vertices[i].y) / .22f)
                : Smooth((-.43f - vertices[i].y) / .25f) : 0f;
            weights[i] = new BoneWeight
            {
                boneIndex0 = 0,
                weight0 = 1f - influence,
                boneIndex1 = limb,
                weight1 = influence * (1f - calf),
                boneIndex2 = horse ? (leg ? limb + 6 : 0) : (limb == 3 ? 6 : 7),
                weight2 = influence * calf
            };
        }

        runtimeMesh = Instantiate(sourceMesh);
        runtimeMesh.name = sourceMesh.name + " Procedural Skin";
        runtimeMesh.bindposes = bindPoses;
        runtimeMesh.boneWeights = weights;

        GameObject skinObject = new GameObject("Generated_AnimatedSkin");
        skinObject.transform.SetParent(transform, false);
        skinTransform = skinObject.transform;
        CompensateSkinScale();
        SkinnedMeshRenderer skin = skinObject.AddComponent<SkinnedMeshRenderer>();
        skin.sharedMesh = runtimeMesh;
        skin.sharedMaterials = sourceRenderer.sharedMaterials;
        skin.bones = bones;
        skin.rootBone = bones[0];
        Bounds bounds = sourceMesh.bounds;
        bounds.Expand(horse ? .7f : .4f);
        skin.localBounds = bounds;
        skin.updateWhenOffscreen = false;
        sourceRenderer.enabled = false;
    }

    private static float Smooth(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static void HorseWeight(Vector3 v, out int bone, out float weight)
    {
        float leg = Smooth((-.08f - v.y) / .48f) *
                    Smooth((Mathf.Abs(v.x) - .025f) / .105f);
        float front = leg * Smooth((v.z - .08f) / .24f);
        float rear = leg * Smooth((-v.z - .15f) / .26f);
        float head = Smooth((v.z - .42f) / .30f) * Smooth((v.y - .03f) / .30f);
        float tail = Smooth((-v.z - .64f) / .20f) * Smooth((v.y + .40f) / .20f);

        bone = v.x < 0f ? 1 : 2;
        weight = front;
        if (rear > weight) { bone = v.x < 0f ? 3 : 4; weight = rear; }
        if (head > weight) { bone = 5; weight = head; }
        if (tail > weight) { bone = 6; weight = tail; }
        weight = Mathf.Min(weight, .96f);
    }

    private static void HeroWeight(Vector3 v, out int bone, out float weight)
    {
        // Include the lower arm and hand so the visible grip follows the staff.
        float arm = Smooth((Mathf.Abs(v.x) - .24f) / .12f) *
                    (1f - Smooth((-.55f - v.y) / .18f));
        float leg = Smooth((-.08f - v.y) / .30f) *
                    Smooth((Mathf.Abs(v.x) - .045f) / .10f);
        float head = Smooth((v.y - .48f) / .24f) *
                     (1f - Smooth((Mathf.Abs(v.x) - .14f) / .18f));

        bone = v.x < 0f ? 1 : 2;
        weight = arm;
        if (leg > weight) { bone = v.x < 0f ? 3 : 4; weight = leg; }
        if (head > weight) { bone = 5; weight = head; }
        weight = Mathf.Min(weight, .96f);
    }

    private void LateUpdate()
    {
        if (bones == null || Time.deltaTime <= 0f) return;
        CompensateSkinScale();

        Vector3 position = mount != null ? mount.transform.position : transform.position;
        Vector3 displacement = position - previousMountPosition;
        displacement.y = 0f;
        previousMountPosition = position;
        float speed = hasMotionState ? reportedSpeed : displacement.magnitude / Time.deltaTime;
        speed = Mathf.Min(speed, runSpeed);
        smoothedSpeed = Mathf.MoveTowards(smoothedSpeed, speed, Time.deltaTime * 12f);
        float movement = Mathf.Clamp01(smoothedSpeed / runSpeed);
        if (hasExternalGaitPhase) phase = externalGaitPhase;
        else phase += Time.deltaTime * Mathf.Lerp(2f, 12f, movement);
        landingTime = Mathf.Max(0f, landingTime - Time.deltaTime);
        if (attackElapsed >= 0f)
        {
            attackElapsed += Time.deltaTime;
            if (attackElapsed >= attackDuration) attackElapsed = -1f;
        }

        if (kind == CharacterKind.IronHorse) AnimateHorse(movement);
        else AnimateHero(movement);
    }

    private void CompensateSkinScale()
    {
        // SkinnedMeshRenderer applies the nested prefab scale during skinning.
        // Cancel it on the renderer object so the visible model matches the GLB.
        Vector3 scale = transform.lossyScale;
        skinTransform.localScale = new Vector3(
            Mathf.Abs(scale.x) > .0001f ? 1f / scale.x : 1f,
            Mathf.Abs(scale.y) > .0001f ? 1f / scale.y : 1f,
            Mathf.Abs(scale.z) > .0001f ? 1f / scale.z : 1f);
    }

    private void AnimateHorse(float movement)
    {
        float runBlend = Smooth((movement - .45f) / .4f);
        float stride = movement * Mathf.Lerp(20f, 34f, runBlend);
        float rearOffset = Mathf.Lerp(Mathf.PI * .5f, Mathf.PI, runBlend);
        float airborne = grounded ? 0f : Mathf.Clamp01(Mathf.Abs(verticalSpeed) / 5f + .35f);
        float landing = landingTime > 0f ? Mathf.Sin((1f - landingTime / .24f) * Mathf.PI) : 0f;
        SetHorseLeg(1, phase, stride, movement, true, airborne, landing);
        SetHorseLeg(2, phase + Mathf.PI, stride, movement, true, airborne, landing);
        SetHorseLeg(3, phase + rearOffset, stride * .85f, movement, false, airborne, landing);
        SetHorseLeg(4, phase + Mathf.PI + rearOffset, stride * .85f, movement, false, airborne, landing);
        float idle = 1f - movement;
        bones[5].localRotation = Quaternion.Euler(
            Mathf.Sin(Time.time * 1.6f) * 1.2f * idle +
            Mathf.Sin(phase * .5f) * 7f * movement + landing * 5f, 0f, 0f);
        bones[6].localRotation = Quaternion.Euler(0f,
            Mathf.Sin(Time.time * 1.2f) * 3f * idle +
            Mathf.Sin(phase * .75f) * 15f * movement, 0f);
    }

    private void SetHorseLeg(int upperIndex, float legPhase, float stride, float movement,
        bool front, float airborne, float landing)
    {
        float wave = Mathf.Sin(legPhase);
        float swing = Smooth(Mathf.Max(0f, wave));
        float upper = Mathf.Lerp(wave * stride, front ? -27f : 17f, airborne);
        float knee = Mathf.Lerp(swing * movement * (front ? 25f : 31f),
            front ? 39f : 28f, airborne);
        if (front) knee += landing * 6f;
        bones[upperIndex].localRotation = Quaternion.Euler(upper, 0f, 0f);
        bones[upperIndex + 6].localRotation = Quaternion.Euler(knee, 0f, 0f);
    }

    private void AnimateHero(float movement)
    {
        float attack = 0f;
        if (attackElapsed >= 0f)
        {
            float t = attackElapsed / attackDuration;
            float windup = Smooth(t / .32f);
            float strike = Smooth((t - .32f) / .34f);
            float recover = Smooth((t - .66f) / .34f);
            attack = (windup - strike) * 45f + (strike - recover) * -58f;
        }
        float idleBreath = Mathf.Sin(Time.time * 1.8f);
        float sway = Mathf.Sin(phase) * movement * 4.5f + idleBreath * (1f - movement) * .8f;
        bones[1].localRotation = Quaternion.Euler(sway + attack, 0f,
            -2f - movement * 2f - Mathf.Abs(attack) * .2f);
        bones[2].localRotation = Quaternion.Euler(-sway, 0f, 2f);
        if (bambooGrip != null)
        {
            Vector3 shoulder = bones[1].localPosition;
            bambooGrip.localPosition = shoulder + bones[1].localRotation * (bambooGripRestPosition - shoulder);
            bambooGrip.localRotation = bones[1].localRotation * bambooGripRestRotation;
        }
        float legTuck = grounded ? 0f : Mathf.Clamp01(Mathf.Abs(verticalSpeed) / 6f + .35f) * 12f;
        // The mounted mesh already has bent thighs and lowered boots. Keep
        // contact with the saddle while allowing a little independent give.
        float seatPitch = mountedSourceMesh ? 0f : -55f;
        float calfBend = mountedSourceMesh ? 0f : 65f;
        float seatSpread = mountedSourceMesh ? 0f : 8f;
        bones[3].localRotation = Quaternion.Euler(seatPitch + Mathf.Sin(phase) * movement * 2.5f - legTuck, 0f, -seatSpread);
        bones[4].localRotation = Quaternion.Euler(seatPitch - Mathf.Sin(phase) * movement * 2.5f - legTuck, 0f, seatSpread);
        bones[6].localRotation = Quaternion.Euler(calfBend - movement * 2f, 0f, 0f);
        bones[7].localRotation = Quaternion.Euler(calfBend + movement * 2f, 0f, 0f);
        bones[5].localRotation = Quaternion.Euler(
            Mathf.Sin(phase * .5f) * movement * 1.5f +
            Mathf.Sin(Time.time * 1.1f) * (1f - movement), 0f, 0f);
        transform.localPosition = baseLocalPosition + Vector3.up *
            (Mathf.Sin(phase * 2f) * movement * .013f + idleBreath * (1f - movement) * .004f - landingTime * .05f);
        transform.localRotation = baseLocalRotation * Quaternion.Euler(
            -1.5f + Mathf.Sin(phase * 2f) * movement * 1.5f + idleBreath * (1f - movement) * .3f,
            0f,
            Mathf.Sin(phase) * movement * 1.5f);
    }

    private void OnDestroy()
    {
        if (runtimeMesh != null) Destroy(runtimeMesh);
        if (sourceRenderer != null) sourceRenderer.enabled = true;
    }
}
