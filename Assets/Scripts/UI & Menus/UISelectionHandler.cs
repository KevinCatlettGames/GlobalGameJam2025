using UnityEngine;
using UnityEngine.EventSystems;

public class UISelectionHandler : MonoBehaviour
{
    public void UnselectCurrent()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}