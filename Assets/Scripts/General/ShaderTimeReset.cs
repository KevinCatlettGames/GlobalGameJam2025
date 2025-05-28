using Unity.Netcode;
using UnityEngine;

public class ShaderTimeReset : NetworkBehaviour
{
    [SerializeField] private Material material;

    private void Start()
    {
        if (IsServer)
        {
            SetShaderStartTimeServerRpc();
        }
    }

    [ServerRpc]
    private void SetShaderStartTimeServerRpc()
    {
        SetShaderStartTimeClientRpc(Time.time);
    }

    [ClientRpc]
    private void SetShaderStartTimeClientRpc(float startTime)
    {
        if (material != null)
        {
            material.SetFloat("_StartTime", startTime);
        }
        else
        {
            Debug.LogWarning("Material reference is missing in ShaderTimeReset.");
        }
    }
}