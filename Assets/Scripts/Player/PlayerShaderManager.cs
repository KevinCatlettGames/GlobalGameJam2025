using System.Collections;
using UnityEngine;

public class PlayerShaderManager : MonoBehaviour
{
    [SerializeField] private int materialElementID = 0;
    private Material material;
    private bool damageEffectActice = false;
    void Start()
    {
        material = GetComponent<SkinnedMeshRenderer>().materials[materialElementID];
    }

    public void ResetShader()
    {
        StopAllCoroutines();
        damageEffectActice = false;
        if (!material) return; 
        
        material.SetFloat("_isDamaged", 0);
        material.SetFloat("_isWet", 0);
    }

    public void DamageEffect(float effectDuration)
    {
        if (!damageEffectActice) StartCoroutine(DamageCoroutine(effectDuration));
    }

    private IEnumerator DamageCoroutine(float duration)
    {
        material.SetFloat("_isDamaged", 1);
        damageEffectActice = true;
        yield return new WaitForSeconds(duration);
        material.SetFloat("_isDamaged", 0);
        damageEffectActice = false;
    }
    public void WetEffect(bool isWet)
    {
        float w = (isWet) ? 1f : 0f;
        material.SetFloat("_isWet", w);   
    }

    private void OnDestroy()
    {
        ResetShader();
    }
}
