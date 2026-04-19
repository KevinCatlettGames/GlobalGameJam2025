using UnityEngine;

public class HarpoonBubble : BasicBubble
{
    [SerializeField] private float pullForce = 45f;
    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;
        if (other.CompareTag("Bubble"))
        {
            BasicBubble bubble = other.GetComponent<BasicBubble>();
            if (bubble != null && bubble.spellType != BasicBubble.SpellType.Wall)
            {
                Physics.IgnoreCollision(sphereCollider, other.GetComponent<Collider>());
            }
        }
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance.PlayingLocal)
                other.GetComponent<PlayerController>().ApplyImpulseLocal(direction * -1, pullForce);
            else
                other.GetComponent<PlayerController>().ApplyImpulseServerRpc(direction * -1, pullForce);
        }
        base.BubbleCollision(other);
    }
}
