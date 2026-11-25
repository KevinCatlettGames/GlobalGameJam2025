using UnityEngine;

public class GhostExplode : MonoBehaviour
{
    [SerializeField] private float damage = 37f;
    [SerializeField] private float knockback = 10f;
    [SerializeField] private float explosionRadius = 10f;
    [SerializeField] private float primaryTargetFactor = .5f;
    [SerializeField] private GameObject explosionEffect;
    private bool hasExploded = false;
    private LeafGhost leafGhost;
    private int ownerID = -1;

    private void Start()
    {
        leafGhost = GetComponent<LeafGhost>();
    }

    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        Collider[] explosionOverlaps = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask("Bubble", "Player"));
        Vector3 origin;
        Vector3 direction;
        foreach (Collider col in explosionOverlaps)
        {
            if (col == null || col.gameObject == this.gameObject) continue;
            origin = transform.position;
            direction = col.transform.position - transform.position;
            if (!Physics.Raycast(origin, direction, direction.magnitude, LayerMask.GetMask("Wall")))
            {
                if (col.CompareTag("Player"))
                {
                    PlayerController player = col.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        if (player.PlayerID == ownerID)
                        {
                            damage *= primaryTargetFactor;
                            knockback *= primaryTargetFactor;
                        }
                           

                        if (GameManager.Instance.PlayingLocal)
                            player.ApplyKnockbackLocal(ownerID, direction, knockback, damage, false);
                        else
                            player.ApplyKnockbackServerRpc(ownerID, direction, knockback, damage, false);


                        if (player.PlayerID == ownerID)
                        {
                            damage /= primaryTargetFactor;
                            knockback /= primaryTargetFactor;
                        }
                    }
                }
                else
                {
                    BasicBubble bubble = col.GetComponent<BasicBubble>();
                    if (bubble != null)
                    {
                        bubble.BubbleCollision(this.gameObject);
                    }
                }
            }
        }


        Instantiate(explosionEffect, transform.position, Quaternion.identity);
        
        leafGhost.StopGhost();
    }

    public void Rearm()
    {
        hasExploded = false;
        ownerID = -1;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        if (collision.gameObject.TryGetComponent<Reflector>(out var reflector) && reflector.GetIsReflecting())
        {
            ownerID = reflector.OwnerID;
            Vector3 reflectNormal = collision.GetContact(0).normal;
            leafGhost.ReflectGhost(reflectNormal);
            return;
        }

        if (collision != null && collision.gameObject.CompareTag("Bubble"))
        {
            var otherBubble = collision.gameObject.GetComponent<BasicBubble>();
            if (otherBubble != null)
            {
                ownerID = otherBubble.OwnerID.Value;
            }
        }
        Explode();
    }
}
