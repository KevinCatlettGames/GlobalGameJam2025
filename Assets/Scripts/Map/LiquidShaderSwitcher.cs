using UnityEngine;
using UnityEngine.Rendering;

public class LiquidShaderSwitcher : MonoBehaviour
{
    private MeshRenderer meshRenderer;

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void SwitchToOrtho()
    {
        Material m = meshRenderer?.material;
        if (m != null)
            m.SetFloat("_isOrtho", 1);
    }
}
