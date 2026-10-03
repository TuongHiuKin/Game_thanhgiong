using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThanhGiong.Characters
{
    [SelectionBase]
    [RequireComponent(typeof(Animator))]
    public class ThanhGiongCharacterController : MonoBehaviour
    {
        [Header("Modular Equipment")]
        [Tooltip("Vũ khí Bụi Tre Ngà vàng óng")]
        [SerializeField] private GameObject goldenBambooWeapon;

        [Tooltip("Áo choàng đỏ")]
        [SerializeField] private GameObject capeGroup;

        [Header("Effects & VFX")]
        [Tooltip("Hào quang ánh kim quanh Bụi Tre Ngà")]
        [SerializeField] private ParticleSystem bambooAuraVFX;

        [Tooltip("Sóng xung kích nổ tung mặt đất khi đập Bụi Tre")]
        [SerializeField] private ParticleSystem bambooImpactVFX;

        [Header("Movement State")]
        [Range(0f, 10f)]
        public float moveSpeed = 0f;
        public bool hasCape = true;
        public bool hasWeapon = true;

        [Header("Mounted Pose")]
        [SerializeField] private bool mountedPose = true;
        [SerializeField] private float mountedLegPitch = -58f;
        [SerializeField] private float mountedLegSpread = 10f;
        [SerializeField] private Vector3 mountedBootOffset = new Vector3(0f, -0.22f, 0.25f);

        [Header("Bamboo Combat Motion")]
        [SerializeField] private float proceduralAttackDuration = 0.68f;
        [SerializeField] private float attackBodyTwist = 24f;
        [SerializeField] private float attackArmPower = 78f;
        [SerializeField] private float attackLegBrace = 14f;

        [Header("Facial Expression")]
        [SerializeField] private bool createFacialAccents = true;
        [SerializeField] private Color facialAccentColor = new Color(0.09f, 0.045f, 0.025f, 1f);

        private Animator animator;
        private Coroutine impactRoutine;
        private Transform leftLegJoint;
        private Transform rightLegJoint;
        private Transform leftBoot;
        private Transform rightBoot;
        private Transform hips;
        private Transform spine;
        private Transform chest;
        private Transform head;
        private Transform upperArmLeft;
        private Transform lowerArmLeft;
        private Transform wristLeft;
        private Transform upperArmRight;
        private Transform lowerArmRight;
        private Transform wristRight;
        private Transform upperLegLeft;
        private Transform lowerLegLeft;
        private Transform upperLegRight;
        private Transform lowerLegRight;
        private Transform browLeft;
        private Transform browRight;
        private Transform mouth;
        private float attackElapsed = -1f;
        private static Material facialMaterial;
        private Vector3 heroBaseLocalPosition;
        private Quaternion heroBaseLocalRotation;
        private Quaternion hipsBaseRotation;
        private Quaternion spineBaseRotation;
        private Quaternion chestBaseRotation;
        private Quaternion headBaseRotation;
        private Quaternion upperArmLeftBaseRotation;
        private Quaternion lowerArmLeftBaseRotation;
        private Quaternion wristLeftBaseRotation;
        private Quaternion upperArmRightBaseRotation;
        private Quaternion lowerArmRightBaseRotation;
        private Quaternion wristRightBaseRotation;
        private Quaternion upperLegLeftBaseRotation;
        private Quaternion lowerLegLeftBaseRotation;
        private Quaternion upperLegRightBaseRotation;
        private Quaternion lowerLegRightBaseRotation;
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int AttackTrigger = Animator.StringToHash("Attack");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            leftLegJoint = transform.Find("Leg_Left_Joint");
            rightLegJoint = transform.Find("Leg_Right_Joint");
            leftBoot = leftLegJoint != null ? leftLegJoint.Find("Boot_L") : null;
            rightBoot = rightLegJoint != null ? rightLegJoint.Find("Boot_R") : null;
            CacheCurrentRigBones();
            CacheBasePose();
            // The seat in each scene owns the rider's alignment. Keep its
            // authored offset instead of snapping to an old generic offset.
            if (createFacialAccents) EnsureFacialAccents();
            ApplyEquipment();
        }

        private void Update()
        {
            if (animator != null)
            {
                animator.SetFloat(SpeedParam, moveSpeed);
            }
        }

        private void LateUpdate()
        {
            // The mounted hero root must remain attached to RiderSeat. Old clips
            // were authored for another hierarchy and must never displace or roll
            // the whole character relative to the map.
            transform.localPosition = heroBaseLocalPosition;
            transform.localRotation = heroBaseLocalRotation;

            if (mountedPose)
            {
                // Compatibility with the original modular rig.
                if (leftLegJoint != null)
                    leftLegJoint.localRotation = Quaternion.Euler(mountedLegPitch, 0f, mountedLegSpread);
                if (rightLegJoint != null)
                    rightLegJoint.localRotation = Quaternion.Euler(mountedLegPitch, 0f, -mountedLegSpread);
                if (leftBoot != null)
                    leftBoot.localPosition = new Vector3(-0.02f, mountedBootOffset.y, mountedBootOffset.z);
                if (rightBoot != null)
                    rightBoot.localPosition = new Vector3(0.02f, mountedBootOffset.y, mountedBootOffset.z);
            }

            ApplyProceduralCombatPose();
        }

        [ContextMenu("Swing Golden Bamboo (Đập Bụi Tre)")]
        public void TriggerBambooAttack()
        {
            attackElapsed = 0f;
            if (animator != null)
            {
                animator.SetTrigger(AttackTrigger);
            }
            if (impactRoutine != null) StopCoroutine(impactRoutine);
            impactRoutine = StartCoroutine(ImpactDelayRoutine(0.45f));
        }

        private void CacheCurrentRigBones()
        {
            hips = FindDeep(transform, "hips");
            spine = FindDeep(transform, "spine");
            chest = FindDeep(transform, "chest");
            head = FindDeep(transform, "head");
            upperArmLeft = FindDeep(transform, "upperarm.l");
            lowerArmLeft = FindDeep(transform, "lowerarm.l");
            wristLeft = FindDeep(transform, "wrist.l");
            upperArmRight = FindDeep(transform, "upperarm.r");
            lowerArmRight = FindDeep(transform, "lowerarm.r");
            wristRight = FindDeep(transform, "wrist.r");
            upperLegLeft = FindDeep(transform, "upperleg.l");
            lowerLegLeft = FindDeep(transform, "lowerleg.l");
            upperLegRight = FindDeep(transform, "upperleg.r");
            lowerLegRight = FindDeep(transform, "lowerleg.r");
        }

        private void CacheBasePose()
        {
            heroBaseLocalPosition = transform.localPosition;
            heroBaseLocalRotation = transform.localRotation;
            hipsBaseRotation = LocalRotationOf(hips);
            spineBaseRotation = LocalRotationOf(spine);
            chestBaseRotation = LocalRotationOf(chest);
            headBaseRotation = LocalRotationOf(head);
            upperArmLeftBaseRotation = LocalRotationOf(upperArmLeft);
            lowerArmLeftBaseRotation = LocalRotationOf(lowerArmLeft);
            wristLeftBaseRotation = LocalRotationOf(wristLeft);
            upperArmRightBaseRotation = LocalRotationOf(upperArmRight);
            lowerArmRightBaseRotation = LocalRotationOf(lowerArmRight);
            wristRightBaseRotation = LocalRotationOf(wristRight);
            upperLegLeftBaseRotation = LocalRotationOf(upperLegLeft);
            lowerLegLeftBaseRotation = LocalRotationOf(lowerLegLeft);
            upperLegRightBaseRotation = LocalRotationOf(upperLegRight);
            lowerLegRightBaseRotation = LocalRotationOf(lowerLegRight);
        }

        private static Quaternion LocalRotationOf(Transform bone)
        {
            return bone != null ? bone.localRotation : Quaternion.identity;
        }

        private static Transform FindDeep(Transform root, string objectName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == objectName) return child;
            return null;
        }

        private void ApplyProceduralCombatPose()
        {
            float intensity = 0f;
            float strike = 0f;

            if (attackElapsed >= 0f)
            {
                attackElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(attackElapsed / Mathf.Max(0.05f, proceduralAttackDuration));
                // 0-38%: lift and coil, 38-62%: explosive downward strike,
                // 62-100%: readable follow-through and recovery.
                if (t < 0.38f)
                    intensity = Smooth01(t / 0.38f);
                else if (t < 0.62f)
                {
                    float hitT = Smooth01((t - 0.38f) / 0.24f);
                    intensity = 1f;
                    strike = hitT;
                }
                else
                {
                    float recover = Smooth01((t - 0.62f) / 0.38f);
                    intensity = 1f - recover;
                    strike = 1f - recover;
                }

                if (t >= 1f) attackElapsed = -1f;
            }

            float windup = intensity * (1f - strike);
            float impact = intensity * strike;

            SetRotation(hips, hipsBaseRotation, -5f * impact, attackBodyTwist * (windup - impact), 0f);
            SetRotation(spine, spineBaseRotation, -12f * impact, -attackBodyTwist * 0.45f * windup, 5f * windup);
            SetRotation(chest, chestBaseRotation, -18f * impact, attackBodyTwist * (windup - impact), -7f * impact);

            // Right hand carries the bamboo; left hand pulls and stabilises.
            SetRotation(upperArmRight, upperArmRightBaseRotation, -attackArmPower * windup + attackArmPower * 0.72f * impact, 10f * impact, -32f * intensity);
            SetRotation(lowerArmRight, lowerArmRightBaseRotation, -42f * windup + 24f * impact, 0f, 14f * intensity);
            SetRotation(wristRight, wristRightBaseRotation, -18f * windup + 34f * impact, 0f, -12f * impact);
            SetRotation(upperArmLeft, upperArmLeftBaseRotation, -48f * windup + 38f * impact, -16f * intensity, 30f * intensity);
            SetRotation(lowerArmLeft, lowerArmLeftBaseRotation, -55f * intensity, 0f, -8f * impact);
            SetRotation(wristLeft, wristLeftBaseRotation, 16f * windup - 20f * impact, 0f, 8f * intensity);

            // Mounted attack: legs grip the saddle instead of kicking freely.
            SetRotation(upperLegLeft, upperLegLeftBaseRotation, attackLegBrace * impact, 0f, -attackLegBrace * intensity);
            SetRotation(upperLegRight, upperLegRightBaseRotation, attackLegBrace * impact, 0f, attackLegBrace * intensity);
            SetRotation(lowerLegLeft, lowerLegLeftBaseRotation, -12f * impact, 0f, 0f);
            SetRotation(lowerLegRight, lowerLegRightBaseRotation, -12f * impact, 0f, 0f);

            // Determined face: head follows the weapon, brows pinch inward and
            // the mouth opens briefly on impact.
            SetRotation(head, headBaseRotation, 8f * windup - 13f * impact, -8f * windup + 5f * impact, 0f);
            AnimateFace(intensity, impact);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static void SetRotation(Transform bone, Quaternion baseRotation, float x, float y, float z)
        {
            if (bone != null) bone.localRotation = baseRotation * Quaternion.Euler(x, y, z);
        }

        private void EnsureFacialAccents()
        {
            if (head == null) return;
            browLeft = EnsureFacePart("Expression_Brow_L", new Vector3(-0.12f, 0.13f, 0.305f), new Vector3(0.12f, 0.024f, 0.022f));
            browRight = EnsureFacePart("Expression_Brow_R", new Vector3(0.12f, 0.13f, 0.305f), new Vector3(0.12f, 0.024f, 0.022f));
            mouth = EnsureFacePart("Expression_Mouth", new Vector3(0f, -0.075f, 0.315f), new Vector3(0.105f, 0.016f, 0.018f));
        }

        private Transform EnsureFacePart(string partName, Vector3 localPosition, Vector3 localScale)
        {
            Transform existing = head.Find(partName);
            if (existing != null) return existing;

            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            part.transform.SetParent(head, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider != null) Destroy(partCollider);

            if (facialMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                facialMaterial = new Material(shader) { name = "Runtime_ThanhGiong_FacialAccent" };
                facialMaterial.color = facialAccentColor;
            }
            part.GetComponent<Renderer>().sharedMaterial = facialMaterial;
            return part.transform;
        }

        private void AnimateFace(float intensity, float impact)
        {
            if (browLeft != null)
                browLeft.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(4f, -17f, intensity));
            if (browRight != null)
                browRight.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-4f, 17f, intensity));
            if (mouth != null)
            {
                float open = Mathf.Clamp01(impact * 1.35f);
                mouth.localScale = new Vector3(0.105f + open * 0.025f, 0.016f + open * 0.055f, 0.018f);
            }
        }

        public void SetMoveSpeed(float speed)
        {
            moveSpeed = Mathf.Max(0f, speed);
        }

        private IEnumerator ImpactDelayRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (bambooImpactVFX != null)
            {
                bambooImpactVFX.Play();
            }
            impactRoutine = null;
        }

        [ContextMenu("Toggle Cape")]
        public void ToggleCape()
        {
            hasCape = !hasCape;
            if (capeGroup != null) capeGroup.SetActive(hasCape);
        }

        [ContextMenu("Toggle Weapon")]
        public void ToggleWeapon()
        {
            hasWeapon = !hasWeapon;
            if (goldenBambooWeapon != null) goldenBambooWeapon.SetActive(hasWeapon);
        }

        private void ApplyEquipment()
        {
            if (capeGroup != null) capeGroup.SetActive(hasCape);
            if (goldenBambooWeapon != null) goldenBambooWeapon.SetActive(hasWeapon);
            if (bambooAuraVFX != null && hasWeapon) bambooAuraVFX.Play();
        }

        private void OnValidate()
        {
            ApplyEquipment();
        }
    }
}
