using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections; 

public class HandleUIElementSelection : MonoBehaviour
{
    public GameObject[] possibleOldElements;
    public GameObject newElement; 

    public void OpenMenu()
    {
        StartCoroutine(OpenMenuRoutine());
    }
    IEnumerator OpenMenuRoutine()
    {
        yield return new WaitForSeconds(.5f);

        foreach(GameObject element in possibleOldElements)
        {
            if(EventSystem.current.currentSelectedGameObject == element)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(newElement);
            }
        }
    }
}