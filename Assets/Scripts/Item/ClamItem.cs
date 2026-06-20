using System.Collections;
using FMODUnity;
using UnityEngine;

public class ClamItem : Item
{
    protected override IEnumerator DelayedDestroy()
    {
        yield return new WaitForEndOfFrame();
        StopAllCoroutines();
        if (pickUpEffect != null) 
            Instantiate(pickUpEffect, transform.position, Quaternion.identity);
        RuntimeManager.PlayOneShotAttached(pickUpEvent, gameObject);
        gameObject.SetActive(false);
    }

    private void ToggleItem(bool isActive)
    {
        gameObject.SetActive(isActive);
    }
}
