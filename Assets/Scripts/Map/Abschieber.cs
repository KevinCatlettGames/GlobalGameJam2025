using UnityEngine;
using Unity.Netcode;

public class Abschieber : MonoBehaviour
{
    [SerializeField] private float damage = .1f;
    [SerializeField] private float knockback = .1f;
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 direction = other.transform.position - transform.position;
            direction.y = 0;
            if (direction == Vector3.zero)
            {
                direction = other.transform.forward;
            }
            else
            {
                direction.Normalize();
            }
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            player.ApplyKnockbackServerRpc(-1, direction, knockback * Time.deltaTime, damage * Time.deltaTime);
        }
    }
}
