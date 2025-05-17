using Unity.Netcode;
using UnityEngine;

public class ShaderTimeReset : NetworkBehaviour
{
    [SerializeField] private Material material;

    private void Start()
    {
        if (IsServer)  // Ensure that the server sets the initial time
        {
            SetShaderStartTimeServerRpc();
        }
    }

    [ServerRpc]
    private void SetShaderStartTimeServerRpc()
    {
        // Set the shader's _StartTime on the server, then notify all clients
        SetShaderStartTimeClientRpc(Time.time);
    }

    [ClientRpc]
    private void SetShaderStartTimeClientRpc(float startTime)
    {
        // Set the shader's _StartTime on all clients to the same value
        material.SetFloat("_StartTime", startTime);
    }
}