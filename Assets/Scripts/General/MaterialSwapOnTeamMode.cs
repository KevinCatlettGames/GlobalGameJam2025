using UnityEngine;
using System.Collections;

public class MaterialSwapOnTeamMode : MonoBehaviour
{
    [SerializeField] private int materialSwapID;
    [SerializeField] private Material teamAMaterial;
    [SerializeField] private Material teamBMaterial;

    private Renderer targetRenderer;
    private PlayerController playerController;

    private IEnumerator Start()
    {
        yield return null;

        Proceed();
    }

    private void Proceed()
    {
        if (GameManager.Instance.GameMode != GameManager.GameModeType.Team)
        {
            enabled = false;
            return;
        }

        targetRenderer = GetComponent<Renderer>();
        playerController = GetComponentInParent<PlayerController>();

        ApplyMaterialSwap();
    }

    private void ApplyMaterialSwap()
    {
        Material matToUse = null;

        if (GameManager.Instance.TeamA.Contains(playerController))
            matToUse = teamAMaterial;
        else if (GameManager.Instance.TeamB.Contains(playerController))
            matToUse = teamBMaterial;

        if (matToUse == null)
        {
            Debug.LogWarning(
                $"{playerController.name} is not assigned to a team.",
                this
            );
            return;
        }

        Material[] mats = targetRenderer.materials;

        if (materialSwapID < 0 || materialSwapID >= mats.Length)
        {
            Debug.LogWarning(
                $"Invalid material index {materialSwapID} on {targetRenderer.name}",
                this
            );
            return;
        }

        mats[materialSwapID] = matToUse;
        targetRenderer.materials = mats;
    }
}