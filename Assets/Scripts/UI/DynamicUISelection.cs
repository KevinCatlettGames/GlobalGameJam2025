using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class DynamicUISelection : MonoBehaviour
{
    [SerializeField] EventSystem eventSystem;
    [SerializeField] private GameObject[] uiElements;

    private void OnEnable()
    {
        eventSystem.SetSelectedGameObject(uiElements[0]);
    }

    private void OnDisable()
    {
        eventSystem.SetSelectedGameObject(null);
    }
}