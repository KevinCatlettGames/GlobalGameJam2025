using UnityEngine;

public class ScorePanelBackgroundFrameResizer : MonoBehaviour
{
    [SerializeField] private float[] frameXPositions;
    [SerializeField] private float[] frameWidths;
    [SerializeField] private float[] killCounterPositions;

    [Header("Kills Counter Target")]
    [SerializeField] private RectTransform killsCounterRect;
    private RectTransform myRectTransform;

    private void Awake()
    {
        myRectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (LobbyManager.instance != null &&
            !LobbyManager.instance.playEndless.Value &&
            LobbyManager.instance.winsNeeded.Value > 0 &&
            LobbyManager.instance.winsNeeded.Value <= frameXPositions.Length)
        {
            int index = LobbyManager.instance.winsNeeded.Value - 1;

            float targetX = frameXPositions[index];
            float targetWidth = frameWidths[index];
            float targetKillCounterXPos = killCounterPositions[index];
            // 1. Resize and reposition background frame
            myRectTransform.localPosition = new Vector3(targetX, myRectTransform.localPosition.y, myRectTransform.localPosition.z);
            myRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
            killsCounterRect.localPosition = new Vector3(targetKillCounterXPos, killsCounterRect.localPosition.y, killsCounterRect.localPosition.z);
        }
    }
}