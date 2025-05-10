using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialSwapper : MonoBehaviour
{
    private SkinnedMeshRenderer meshRenderer;
    private List<Material> materials = new List<Material>();
    private List<Material> swapMaterials = new List<Material>();
    [SerializeField] private Material swapMaterial;
    [SerializeField] private int swapMaterialIndex = 0;
    private bool swapped = false;

    private void Start()
    {
        meshRenderer = GetComponent<SkinnedMeshRenderer>();
        meshRenderer.GetMaterials(materials);
        for (int i = 0; i < materials.Count; i++)
        {
            if (i == swapMaterialIndex)
            {
                swapMaterials.Add(swapMaterial);
            }
            else 
            {
                swapMaterials.Add(materials[i]);
            }
        }
    }
    public void SwapMaterials(float duration)
    {
        if (!swapped) StartCoroutine(MaterialSwap(duration));
    }
    
    private IEnumerator MaterialSwap(float duration)
    {
        swapped = true;
        meshRenderer.SetMaterials(swapMaterials);
        yield return new WaitForSeconds(duration);
        meshRenderer.SetMaterials(materials);
        swapped = false;
        Debug.Log("SwapBack");
    }
}
