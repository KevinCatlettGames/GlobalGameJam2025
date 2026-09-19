using UnityEngine;

public class DisableIfRelay : MonoBehaviour
{
    public bool disableIfNotRelayInstead = false;

    private void OnEnable()
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && !disableIfNotRelayInstead)
            gameObject.SetActive(false);
        else if(TransportSwitcher.Instance && !TransportSwitcher.Instance.isUsingRelay && disableIfNotRelayInstead)
            gameObject.SetActive(false);
    }
}