using UnityEngine;

public class DmgGenerator : MonoBehaviour
{
    [SerializeField] private GameObject damagePopup;

    public void SpawnDamagePopup(int damage)
    {
        GameObject popup = Instantiate(damagePopup, transform.position, Quaternion.identity, transform);
        popup.GetComponent<DamagePopup>().InitialiseDamagePopup(damage);
    }
}
