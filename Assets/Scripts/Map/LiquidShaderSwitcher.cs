using UnityEngine;
using UnityEngine.Rendering;

public class LiquidShaderSwitcher : MonoBehaviour
{
    public void SwitchToOrtho()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Material m = meshRenderer?.material;
        if (m != null)
            m.SetFloat("_isOrtho", 1);
    }
}
