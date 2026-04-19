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
    private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip windowsClip;
    [SerializeField] private VideoClip linuxClip;
    private bool isSkippable = false;
    private bool played = false;
    public GameObject introParent;
    [SerializeField] private RenderTexture videoTexture;

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

        videoTexture = new RenderTexture(1920, 1080, 0);
        videoTexture.Create();

        videoPlayer.targetTexture = videoTexture;

        GetComponent<RawImage>().texture = videoTexture;

        blackImage.SetActive(true);
    }

    void Start()
    {
        #if UNITY_STANDALONE_WINDOWS || UNITY_EDITOR
        videoPlayer.clip = windowsClip;
        #endif
        #if UNITY_STANDALONE_LINUX
        videoPlayer.clip = linuxClip;
        #endif
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
        MenuSelection.Instance.gameObject.SetActive(false);
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
        MenuSelection.Instance.gameObject.SetActive(true);
        Destroy(introParent);
    }
}
