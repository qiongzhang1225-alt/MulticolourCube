using UnityEngine;
using System.Collections;

/// <summary>
/// 自动来回移动的平台
/// </summary>
public class ResettablePlatform : BaseResettable
{
    public Transform pointA;
    public Transform pointB;
    public float moveSpeed = 2f;

    private Vector3 targetPos;
    private Vector3 savedTargetPos;

    protected override void Awake()
    {
        base.Awake();
        if (pointA != null && pointB != null)
        {
            targetPos = pointB.position;
            StartCoroutine(MoveRoutine());
        }
    }

    IEnumerator MoveRoutine()
    {
        while (true)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPos) < 0.05f)
                targetPos = targetPos == pointA.position ? pointB.position : pointA.position;
            yield return null;
        }
    }

    public override void SaveCheckpointState()
    {
        base.SaveCheckpointState();
        savedTargetPos = targetPos;
    }

    public override void ResetToCheckpointState()
    {
        base.ResetToCheckpointState();
        targetPos = savedTargetPos;
    }
}