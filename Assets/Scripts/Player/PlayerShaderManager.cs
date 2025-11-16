using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public enum ShaderState
{
    sober,
    wet,
    sauced,
    inked,
    buffed
}
public class PlayerShaderManager : MonoBehaviour
{
    [SerializeField] private int materialElementID = 0;
    private Material material;
    private bool damageEffectActice = false;
    private ShaderState currentShaderState = ShaderState.sober;
    private string[] enumKeys = { "_STATUS_SOBER", "_STATUS_WET", "_STATUS_SAUCED", "_STATUS_INKED", "_STATUS_BUFFED" };
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
    public void SetShaderState(ShaderState newState)
    {
        material.DisableKeyword(enumKeys[(int)currentShaderState]);
        currentShaderState = newState;
        material.EnableKeyword(enumKeys[(int)currentShaderState]);
    }
    private void OnDestroy()
    {
        ResetShader();
    }
}
