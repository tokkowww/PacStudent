using System.Collections;
using UnityEngine;

public enum GhostState { Normal = 0, Scared = 1, Recovering = 2, Dead = 3 }

public class GhostController : MonoBehaviour
{
    public float deadDuration = 3f;
    public Vector2 startDirection = Vector2.down;

    public GhostState State { get; private set; } = GhostState.Normal;
    public bool Frozen { get; set; }

    Animator animator;
    Vector3 startPos;
    Coroutine deadRoutine;

    void Awake()
    {
        animator = GetComponent<Animator>();
        startPos = transform.position;
        SetDirection(startDirection);
    }

    public void SetState(GhostState s)
    {
        State = s;
        if (animator) animator.SetInteger("State", (int)s);
    }

    public void SetDirection(Vector2 dir)
    {
        if (animator == null || dir == Vector2.zero) return;
        animator.SetFloat("DirX", dir.x);
        animator.SetFloat("DirY", dir.y);
    }

    public void Die()
    {
        if (deadRoutine != null) StopCoroutine(deadRoutine);
        SetState(GhostState.Dead);
        deadRoutine = StartCoroutine(DeadTimer());
    }

    IEnumerator DeadTimer()
    {
        yield return new WaitForSeconds(deadDuration);

        float left = GameManager.Instance.ScaredTimeLeft;
        if (left <= 0f) SetState(GhostState.Normal);
        else if (left <= GameManager.Instance.recoverAt) SetState(GhostState.Recovering);
        else SetState(GhostState.Scared);

        deadRoutine = null;
    }

    public void ResetGhost()
    {
        if (deadRoutine != null) { StopCoroutine(deadRoutine); deadRoutine = null; }
        transform.position = startPos;
        SetState(GhostState.Normal);
        SetDirection(startDirection);
        Frozen = false;
    }
}