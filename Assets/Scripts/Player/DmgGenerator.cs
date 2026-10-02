using UnityEngine;

public class DmgGenerator : MonoBehaviour
{
    [SerializeField] private GameObject damagePopup;

    public void SpawnDamagePopup(int damage, bool isCrit)
    {
        GameObject popup = Instantiate(damagePopup, transform.position, Quaternion.identity);
        popup.GetComponent<DamagePopup>().InitialiseDamagePopup(damage, isCrit);
    }
}
