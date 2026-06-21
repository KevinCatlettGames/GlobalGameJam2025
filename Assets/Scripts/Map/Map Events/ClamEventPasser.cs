using UnityEngine;

public class ClamEventPasser : MonoBehaviour
{
    [SerializeField] private Clam clam;

    public void Snap()
    {
        clam.Snap();
    }

    public void DisableClam()
    {
        clam.DisableClam();
    }
}
