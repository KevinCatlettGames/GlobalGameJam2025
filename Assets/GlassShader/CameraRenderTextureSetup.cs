using UnityEngine;

public class CameraRenderTextureSetup : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Material[] targetMaterials;

    private RenderTexture runtimeRT;

    void Awake()
    {
        runtimeRT = new RenderTexture(1920, 1080, 24);
        runtimeRT.Create();
        targetCamera.targetTexture = runtimeRT;
        foreach (Material mat in targetMaterials)
        {          
            mat.mainTexture = runtimeRT;
        }
    }

    void OnDestroy()
    {
        targetCamera.targetTexture = null;

        if (runtimeRT != null)
        {
            runtimeRT.Release();
            Destroy(runtimeRT);
        }
    }
}