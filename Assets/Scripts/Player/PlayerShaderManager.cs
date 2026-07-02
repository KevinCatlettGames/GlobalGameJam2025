using System.Collections;
using UnityEngine;

public enum ShaderState
{
    sober,
    wet,
    sauced,
    inked,
    doomed
}
public class PlayerShaderManager : MonoBehaviour
{
    [SerializeField] private int materialElementID = 0;
    [SerializeField] private SkinnedMeshRenderer bubbleRenderer;
    private PlayerStatusIndicator statusIndicator;
    private Material material;
    private Material bubbleMaterial;
    private bool damageEffectActice = false;
    private bool[] stateArray = new bool[5];
    private string[] enumKeys = { "_STATUS_SOBER", "_STATUS_WET", "_STATUS_SAUCED", "_STATUS_INKED", "_STATUS_BUFFED" };
    void Awake()
    {
        material = GetComponent<SkinnedMeshRenderer>().materials[materialElementID];
        bubbleMaterial = bubbleRenderer.materials[0];
        for (int i = 0; i < stateArray.Length; i++)
        {
            stateArray[i] = false;
        }
    }
    public void SetStatusIndicator(PlayerStatusIndicator s)
    {
        statusIndicator = s;
    }

    public void ResetShader()
    {
        StopAllCoroutines();
        damageEffectActice = false;
        if (!material) return;

        material.SetFloat("_isDamaged", 0);
        bubbleMaterial.SetFloat("_isDamaged", 0);
        for (int i = 0; i < stateArray.Length; i++)
        {
            stateArray[i] = false;
        }
        UpdateShader();
    }

    public void DamageEffect(float effectDuration)
    {
        if (!damageEffectActice) StartCoroutine(DamageCoroutine(effectDuration));
    }

    private IEnumerator DamageCoroutine(float duration)
    {
        material.SetFloat("_isDamaged", 1);
        bubbleMaterial.SetFloat("_isDamaged", 1);
        damageEffectActice = true;
        yield return new WaitForSeconds(duration);
        bubbleMaterial.SetFloat("_isDamaged", 0);
        material.SetFloat("_isDamaged", 0);
        damageEffectActice = false;
    }
    public void SetShaderState(ShaderState changesState, bool newState)
    {
        stateArray[(int)changesState] = newState;
        UpdateShader();
    }
    private void UpdateShader()
    {
        bool isSober = true;
        for (int i = stateArray.Length -1; i > 0; i--)
        {
            if (stateArray[i] && isSober)
            {
                isSober = false;
                material.EnableKeyword(enumKeys[i]);
                statusIndicator.SetStatus((ShaderState)i);
            }
            else
            {
                material.DisableKeyword(enumKeys[i]);
            }
        }
        if (isSober)
        {
            material.EnableKeyword("_STATUS_SOBER");
            statusIndicator.SetStatus(ShaderState.sober);
        }
        else
        {
            material.DisableKeyword("_STATUS_SOBER");
        }
    }
    private void OnDestroy()
    {
        ResetShader();
    }
}
