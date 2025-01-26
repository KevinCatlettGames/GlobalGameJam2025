using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandleBubbleState : MonoBehaviour
{
    public GameObject bubble;

    public void ToggleBubble()
    {
        bubble.SetActive(!bubble.activeSelf);
    }
}
