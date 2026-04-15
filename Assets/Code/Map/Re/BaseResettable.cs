using UnityEngine;

public abstract class BaseResettable : MonoBehaviour, IResettable
{
    protected Vector3 savedPosition;
    protected Quaternion savedRotation;
    protected Vector2 savedVelocity;
    protected float savedAngularVelocity;

    protected Rigidbody2D rb;
    protected SpriteRenderer sr;
    protected Collider2D col;

    protected virtual void Awake()
    {
        if (this == null) return;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    public virtual void SaveCheckpointState()
    {
        if (this == null) return;
        savedPosition = transform.position;
        savedRotation = transform.rotation;
        if (rb != null)
        {
            savedVelocity = rb.velocity;
            savedAngularVelocity = rb.angularVelocity;
        }
    }

    public virtual void ResetToCheckpointState()
    {
        if (this == null) return;
        transform.position = savedPosition;
        transform.rotation = savedRotation;
        if (rb != null)
        {
            rb.velocity = savedVelocity;
            rb.angularVelocity = savedAngularVelocity;
            rb.Sleep();
        }
    }
}