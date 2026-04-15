using UnityEngine;

public class TrapImmunitySurface : MonoBehaviour
{
    private BlockController controller;

    void Start()
    {
        controller = GetComponentInParent<BlockController>();
    }

    public bool IsImmuneToTrap()
    {
        return controller && controller.IsOnFace(gameObject);
    }
}