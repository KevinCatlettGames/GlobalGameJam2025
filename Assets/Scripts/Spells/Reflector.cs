using System.Collections;
using UnityEngine;

public class Reflector : MonoBehaviour
{
    [SerializeField] private bool isReflecting = false;
    private Coroutine refletEffect;
    public int OwnerID = -1;

    public void SetReflect(bool reflect)
    {
        isReflecting = reflect;
    }

    public void ReflectForDuration(float duration)
    {
        if (refletEffect != null) StopCoroutine(refletEffect);
        refletEffect = StartCoroutine(ReflectCoroutine(duration)); // Fixed bug: was previously calling StopCoroutine on a new iterator instance
    }

    public IEnumerator ReflectCoroutine(float time)
    {
        SetReflect(true);
        yield return new WaitForSeconds(time);
        SetReflect(false);
        refletEffect = null;
    }

    public bool GetIsReflecting()
    {
        return isReflecting;
    }

    // --- NEW: EXPLICIT DETECTOR FOR FAKE BUBBLES ---
    private void OnTriggerEnter(Collider other)
    {
        if (!isReflecting || other == null) return;

        // Check if the object entering our shield zone is flagged as a bubble
        if (other.CompareTag("Bubble"))
        {
            BasicBubble bubble = other.GetComponent<BasicBubble>();
            if (bubble != null && bubble.isLocalFake)
            {
                // Locally update ownership so it stops tracking the player who originally shot it
                bubble.OwnerID = this.OwnerID;

                // Force the fake bubble to process a collision with this reflector object immediately
                bubble.BubbleCollision(gameObject);
            }
        }
    }
}