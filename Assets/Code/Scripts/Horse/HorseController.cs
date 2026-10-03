using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThanhGiong.Mounts
{
    [SelectionBase]
    [RequireComponent(typeof(Animator))]
    public class HorseController : MonoBehaviour
    {
        [Header("Modular Equipment Groups")]
        [Tooltip("Cụm yên ngựa và thảm lót lưng")]
        [SerializeField] private GameObject saddleGroup;

        [Tooltip("Cụm dây cương và khớp ngậm mõm")]
        [SerializeField] private GameObject bridleGroup;

        [Tooltip("Giáp bảo vệ đầu (Champron)")]
        [SerializeField] private GameObject armorHeadGroup;

        [Tooltip("Giáp bảo vệ ngực (Peytral)")]
        [SerializeField] private GameObject armorChestGroup;

        [Header("Effects & VFX")]
        [Tooltip("Hiệu ứng bụi móng khi chạy")]
        [SerializeField] private ParticleSystem hoofDustVFX;

        [Tooltip("Hào quang / Tia lửa Ngựa Sắt Thánh Gióng")]
        [SerializeField] private ParticleSystem ironAuraVFX;

        [Tooltip("Hiệu ứng phun lửa từ miệng Ngựa Sắt")]
        [SerializeField] private ParticleSystem fireBreathVFX;

        [Header("Materials Swap")]
        [SerializeField] private Renderer[] bodyRenderers;
        [SerializeField] private Material standardBodyMat;
        [SerializeField] private Material ironHorseBodyMat;

        [Header("Runtime State")]
        [Range(0f, 10f)]
        public float moveSpeed = 0f;
        public bool hasSaddle = true;
        public bool hasArmor = false;
        public bool isIronHorse = false;

        private Animator animator;
        private Coroutine fireRoutine;
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int RearingTrigger = Animator.StringToHash("IsRearing");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            ApplyEquipmentState();
        }

        private void Update()
        {
            if (animator != null)
            {
                animator.SetFloat(SpeedParam, moveSpeed);
            }

            // Quản lý hạt bụi chân ngựa
            if (hoofDustVFX != null)
            {
                if (moveSpeed > 0.5f && !hoofDustVFX.isPlaying)
                {
                    hoofDustVFX.Play();
                }
                else if (moveSpeed <= 0.5f && hoofDustVFX.isPlaying)
                {
                    hoofDustVFX.Stop();
                }
            }
        }

        public void SetMoveSpeed(float speed)
        {
            moveSpeed = Mathf.Max(0f, speed);
        }

        [ContextMenu("Toggle Saddle")]
        public void ToggleSaddle()
        {
            SetSaddle(!hasSaddle);
        }

        public void SetSaddle(bool active)
        {
            hasSaddle = active;
            if (saddleGroup != null) saddleGroup.SetActive(active);
            if (bridleGroup != null) bridleGroup.SetActive(active);
        }

        [ContextMenu("Toggle Armor")]
        public void ToggleArmor()
        {
            SetArmor(!hasArmor);
        }

        public void SetArmor(bool active)
        {
            hasArmor = active;
            if (armorHeadGroup != null) armorHeadGroup.SetActive(active);
            if (armorChestGroup != null) armorChestGroup.SetActive(active);
        }

        [ContextMenu("Convert to Iron Horse (Ngựa Sắt)")]
        public void SetIronHorse(bool active)
        {
            isIronHorse = active;
            SetArmor(active);

            Material targetMat = active ? ironHorseBodyMat : standardBodyMat;
            if (targetMat != null && bodyRenderers != null)
            {
                foreach (var rend in bodyRenderers)
                {
                    if (rend != null) rend.sharedMaterial = targetMat;
                }
            }

            if (ironAuraVFX != null)
            {
                if (active) ironAuraVFX.Play();
                else ironAuraVFX.Stop();
            }
        }

        [ContextMenu("Play Rearing Animation (Hí Vang)")]
        public void TriggerRearing()
        {
            if (animator != null)
            {
                animator.SetTrigger(RearingTrigger);
            }
            if (isIronHorse || fireBreathVFX != null)
            {
                StartCoroutine(FireBreathRoutine(1.2f));
            }
        }

        [ContextMenu("Breathe Fire (Phun Lửa)")]
        public void BreatheFire()
        {
            if (fireBreathVFX != null)
            {
                StartCoroutine(FireBreathRoutine(2.0f));
            }
        }

        private IEnumerator FireBreathRoutine(float duration)
        {
            if (fireBreathVFX != null)
            {
                if (fireRoutine != null) StopCoroutine(fireRoutine);
                fireRoutine = StartCoroutine(RunFireBreath(duration));
            }
            yield break;
        }

        private IEnumerator RunFireBreath(float duration)
        {
            if (fireBreathVFX != null)
            {
                fireBreathVFX.Play();
                yield return new WaitForSeconds(duration);
                fireBreathVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            fireRoutine = null;
        }

        private void ApplyEquipmentState()
        {
            SetSaddle(hasSaddle);
            SetArmor(hasArmor);
            if (isIronHorse)
            {
                SetIronHorse(true);
            }
        }

        private void OnValidate()
        {
            ApplyEquipmentState();
        }
    }
}
