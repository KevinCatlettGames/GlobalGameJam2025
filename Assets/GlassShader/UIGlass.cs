using UnityEngine;
using UnityEngine.UI;

public class UIGlass : MonoBehaviour
{
    public RectTransform targetUI;
    public Material baseMaterial;

    private Material instanceMaterial;

    void Start()
    {
        instanceMaterial = Instantiate(baseMaterial);
        GetComponent<Image>().material = instanceMaterial;
    }

    void Update()
    {
        if (targetUI == null || instanceMaterial == null)
            return;

        Vector3[] corners = new Vector3[4];
        targetUI.GetWorldCorners(corners);

        Vector3 center = (corners[0] + corners[2]) * 0.5f;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, center);

        Vector2 uv = new Vector2(
            screenPos.x / Screen.width,
            screenPos.y / Screen.height
        );

        instanceMaterial.SetVector("_CenterUV", uv);

        Vector2 screenBL = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector2 screenTR = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

        Vector2 size = screenTR - screenBL;

        Vector2 normalizedSize = new Vector2(
            size.x / Screen.width,
            size.y / Screen.height
        );

        instanceMaterial.SetVector("_Size", normalizedSize);
    }
}