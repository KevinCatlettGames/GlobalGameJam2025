using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DomeBounds : MonoBehaviour
{
    private void OnTriggerExit(Collider other)
    {
        Debug.Log(other.name);
        SlipBubble slipBubble;
        if (other.TryGetComponent<SlipBubble>(out slipBubble))
        {
            slipBubble.BubbleCollision(this.gameObject);
            return;
        }
    }
}
