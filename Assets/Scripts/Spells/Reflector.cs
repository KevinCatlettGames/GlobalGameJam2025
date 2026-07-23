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
        StopCoroutine(ReflectCoroutine(duration));
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
}