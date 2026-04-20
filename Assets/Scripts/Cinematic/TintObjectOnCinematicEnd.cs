using UnityEngine;
using EditorAttributes; 

public class TintObjectOnCinematicEnd : MonoBehaviour
{
    [SerializeField] float newBaseColorValue = .65f;
    [SerializeField] float newEmissiveColorValue = .2f;

    MeshRenderer meshRenderer;
    Material mat;

    private int BaseColorID = Shader.PropertyToID("_BaseColor_Value");
    private int EmissiveID = Shader.PropertyToID("_Emissive_Value");

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        mat = meshRenderer.material;

        if (CameraHandler.Instance != null)
            CameraHandler.Instance.onCinematicEnd.AddListener(Tint);
    }

    private void OnDisable()
    {
        if (CameraHandler.Instance != null)
            CameraHandler.Instance.onCinematicEnd.RemoveListener(Tint);
    }

    void Tint()
    {
        if (mat == null) return;

        if (mat.HasProperty(BaseColorID))
            mat.SetFloat(BaseColorID, newBaseColorValue);

        if (mat.HasProperty(EmissiveID))
            mat.SetFloat(EmissiveID, newEmissiveColorValue);
    }
}