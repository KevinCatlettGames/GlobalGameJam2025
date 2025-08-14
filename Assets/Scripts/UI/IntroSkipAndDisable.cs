using FMODUnity;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class IntroSkipAndDisable : MonoBehaviour
{
    public static IntroSkipAndDisable instance;
    
    [SerializeField] float skippableAfterSeconds = 3f;
    [SerializeField] StudioEventEmitter eventEmitter;
    [SerializeField] StudioEventEmitter bubbleEmitter;
    [SerializeField] private GameObject blackImage;
    [SerializeField] private GameObject mainMenu;
    private VideoPlayer videoPlayer;
    private bool isSkippable = false;
    private bool played = false;
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        Cursor.visible = false;
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            Debug.LogError("No VideoPlayer component found!");
        }
        blackImage.SetActive(true);
    }

    void Start()
    {
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnVideoPrepared;

        Invoke(nameof(MakeSkippable), skippableAfterSeconds);
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        GetComponent<RawImage>().enabled = true;
        videoPlayer.Play();
        videoPlayer.time = 0;
        eventEmitter.Play();
        mainMenu.SetActive(false);
    }

    void Update()
    {
        if (isSkippable && Input.anyKeyDown && !played)
        {
            StopAndCleanUp();
        }
    }

    void MakeSkippable()
    {
        isSkippable = true;
    }

    void StopAndCleanUp()
    {
        Cursor.visible = true;
        transform.SetParent(null);  
        DontDestroyOnLoad(this.gameObject);
        RuntimeManager.PlayOneShot(bubbleEmitter.EventReference);
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        played = true;
        videoPlayer.enabled = false;
        GetComponent<RawImage>().enabled = false;
        blackImage.SetActive(false);
        Invoke(nameof(ActivateMainMenu), .1f);
    }
    void ActivateMainMenu()
    {
        mainMenu.SetActive(true);
    }
}
