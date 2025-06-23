using UnityEngine;
using System;

public class EnableComponentOnStart : MonoBehaviour
{
    [Tooltip("Specify the type of the component to enable")]
    [SerializeField] private MonoBehaviour component;

    void Start()
    {
        if (component != null)
        {
            component.enabled = true;
        }
    }
}