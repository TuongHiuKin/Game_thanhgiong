using UnityEngine;

/// <summary>
/// Drives any KayKit/retargeted enemy Animator from real physical motion and combat state.
/// Missing parameters are ignored, so the same component is safe on every invader prefab.
/// </summary>
public class ThanhGiongEnemyAnimationDriver : MonoBehaviour
{
    public float speedBlendTime = .2f;
    public float stateBlendTime = .1f;

    private ThanhGiongEnemy enemy;
    private Animator animator;
    private Rigidbody body;
    private bool hasSpeed, hasAttack, hasHit, hasDead, hasStuck;
    private ThanhGiongEnemy.EnemyState previousState;
    private static readonly int SpeedHash=Animator.StringToHash("Speed");
    private static readonly int AttackHash=Animator.StringToHash("Attack");
    private static readonly int HitHash=Animator.StringToHash("Hit");
    private static readonly int DeadHash=Animator.StringToHash("Dead");
    private static readonly int StuckHash=Animator.StringToHash("Stuck");

    private void Awake()
    {
        enemy=GetComponent<ThanhGiongEnemy>();
        body=GetComponent<Rigidbody>();
        animator=GetComponentInChildren<Animator>();
        if(animator==null) return;
        animator.applyRootMotion=false;
        animator.updateMode=AnimatorUpdateMode.Normal;
        animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
        foreach(AnimatorControllerParameter p in animator.parameters)
        {
            if(p.nameHash==SpeedHash)hasSpeed=true;
            else if(p.nameHash==AttackHash)hasAttack=true;
            else if(p.nameHash==HitHash)hasHit=true;
            else if(p.nameHash==DeadHash)hasDead=true;
            else if(p.nameHash==StuckHash)hasStuck=true;
        }
        previousState=enemy.CurrentState;
    }

    private void Update()
    {
        if(animator==null || enemy==null) return;
        Vector3 velocity=body!=null?body.linearVelocity:Vector3.zero; velocity.y=0f;
        float normalized=Mathf.Clamp01(velocity.magnitude/Mathf.Max(enemy.moveSpeed,.01f));
        if(hasSpeed) animator.SetFloat(SpeedHash,normalized,speedBlendTime,Time.deltaTime);

        ThanhGiongEnemy.EnemyState state=enemy.CurrentState;
        if(state!=previousState)
        {
            if((state==ThanhGiongEnemy.EnemyState.TelegraphingAttack || state==ThanhGiongEnemy.EnemyState.Slamming) && hasAttack) animator.SetTrigger(AttackHash);
            if(state==ThanhGiongEnemy.EnemyState.Stunned && hasHit) animator.SetTrigger(HitHash);
            if(state==ThanhGiongEnemy.EnemyState.Dead && hasDead) animator.SetTrigger(DeadHash);
            previousState=state;
        }
        if(hasStuck) animator.SetBool(StuckHash,state==ThanhGiongEnemy.EnemyState.StuckInGround);

        float targetPlayback = state==ThanhGiongEnemy.EnemyState.StuckInGround ? .38f : 1f;
        animator.speed=Mathf.MoveTowards(animator.speed,targetPlayback,Time.deltaTime/stateBlendTime);
    }

    private void OnDisable()
    {
        if(animator!=null) animator.speed=1f;
    }
}
