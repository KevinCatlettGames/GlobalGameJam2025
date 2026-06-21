using UnityEngine;

public class DisableGameObject : MonoBehaviour
{
    [SerializeField] private GameObject objectToDisable;

    public void DisableObject()
    {
        if (objectToDisable)
            objectToDisable.SetActive(false);
    }
}
