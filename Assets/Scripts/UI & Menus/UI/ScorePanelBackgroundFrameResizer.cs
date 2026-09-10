using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScorePanelBackgroundFrameResizer : MonoBehaviour
{
    [SerializeField] private float[] frameXPositions;
    [SerializeField] private float[] frameWidths;

    private void OnEnable()
    {

        if (LobbyManager.instance && !LobbyManager.instance.playEndless && LobbyManager.instance.winsNeeded <= frameXPositions.Length)
        {
            GetComponent<RectTransform>().localPosition = new Vector3(frameXPositions[LobbyManager.instance.winsNeeded - 1], transform.localPosition.y, transform.localPosition.z);
            GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, frameWidths[LobbyManager.instance.winsNeeded -1]);
        }
    }
}