using Unity.Netcode;
using UnityEngine;

public class TutorialPopUpTrigger : MonoBehaviour
{
    private bool isActive = false;
    [SerializeField] private TutorialPopUp tutorialPopUp;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay
        && other.GetComponent<NetworkObject>().OwnerClientId != NetworkManager.Singleton.LocalClientId) return;

        if (!isActive)
        {
            isActive = true;
            tutorialPopUp.OpenPopUp();
        }
    }
}