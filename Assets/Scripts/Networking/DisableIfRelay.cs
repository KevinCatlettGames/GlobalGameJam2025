using UnityEngine;

public class DisableIfRelay : MonoBehaviour
{
    private void OnEnable()
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
            gameObject.SetActive(false);
    }
}