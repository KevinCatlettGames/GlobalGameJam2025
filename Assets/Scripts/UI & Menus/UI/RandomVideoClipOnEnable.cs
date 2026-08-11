using UnityEngine;
using UnityEngine.Video;

public class RandomVideoClipOnEnable : MonoBehaviour
{
    [SerializeField] private VideoClip[] clips;
    [SerializeField] VideoPlayer videoPlayer;

    private void OnEnable()
    {
        int randomInt = Random.Range(0, clips.Length);
        videoPlayer.clip = clips[randomInt];
    }
}