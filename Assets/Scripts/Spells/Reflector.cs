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
        refletEffect = StartCoroutine(ReflectCoroutine(duration));
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

    private void OnTriggerEnter(Collider other)
    {
        if (!isReflecting || other == null) return;

        if (other.CompareTag("Bubble"))
        {
            BasicBubble bubble = other.GetComponent<BasicBubble>();
            if (bubble != null && bubble.isLocalFake)
            {
                bubble.OwnerID.Value = this.OwnerID;
                bubble.BubbleCollision(gameObject);
            }
        }
    }
}