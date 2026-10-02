using System.Collections;
using UnityEngine;

public class DamageAfterDelay : MonoBehaviour
{
    public IEnumerator DealDamageAfterDelay(PlayerController player, int OwnerID, float damage, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (GameManager.Instance.PlayingLocal)
            player.ApplyKnockbackLocal(OwnerID, Vector3.zero, 0, damage, false);
        else
            player.ApplyKnockbackServerRpc(OwnerID, Vector3.zero, 0, damage, false);
    }

    public void StartDamageAfterDelay(PlayerController player, int OwnerID, float damage, float delay)
    {
        StartCoroutine(DealDamageAfterDelay(player, OwnerID, damage, delay));
    }
}
