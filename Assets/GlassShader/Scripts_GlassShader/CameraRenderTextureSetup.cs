using UnityEngine;

public class CameraRenderTextureSetup : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private RenderTexture runtimeRT;
    private int lastWidth;
    private int lastHeight;

    void Awake()
    {
        CreateRenderTexture();
    }

    void Update()
    {
        // Safely recreates the texture only if the Switch resolution changes (docking/undocking)
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            CreateRenderTexture();
        }
    }

    void CreateRenderTexture()
    {
        // Clean up the old texture memory before creating a new one
        if (runtimeRT != null)
        {
            targetCamera.targetTexture = null;
            runtimeRT.Release();
            Destroy(runtimeRT);
        }

        // Cache the active screen dimensions
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        // Create the new texture matching the active resolution
        runtimeRT = new RenderTexture(lastWidth, lastHeight, 24);
        runtimeRT.Create();

        // Assign to camera so it updates the background projection automatically every frame
        targetCamera.targetTexture = runtimeRT;

        // Project the texture globally to all shaders under this uniform name
        Shader.SetGlobalTexture("_GlobalCameraRT", runtimeRT);
    }

    void OnDestroy()
    {
        if (targetCamera != null)
        {
            targetCamera.targetTexture = null;
        }

        if (runtimeRT != null)
        {
            runtimeRT.Release();
            Destroy(runtimeRT);
        }
    }
}