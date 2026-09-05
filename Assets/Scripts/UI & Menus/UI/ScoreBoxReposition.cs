using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScoreBoxReposition : MonoBehaviour
{
    [SerializeField] private float[] xPositions;

    private void OnEnable()
    {
        if (LobbyManager.instance && !LobbyManager.instance.playEndless && LobbyManager.instance.winsNeeded <= xPositions.Length)
            GetComponent<RectTransform>().localPosition = new Vector3(xPositions[LobbyManager.instance.winsNeeded - 1], transform.localPosition.y, transform.localPosition.z);
    }
}