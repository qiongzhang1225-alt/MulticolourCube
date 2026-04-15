using UnityEngine;

public class ResettablePhysicsBomb : BaseResettable
{
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.CompareTag("Player"))
        {
            other.collider.GetComponent<PlayerRespawn>()?.Die();
        }
    }
}