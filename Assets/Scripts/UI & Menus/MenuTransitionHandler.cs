using System;
using UnityEngine;
using EditorAttributes;
using UnityEngine.SceneManagement;
using System.Collections; 

public class MenuTransitionHandler : MonoBehaviour
{
    public static MenuTransitionHandler Instance;
    [SerializeField] private GameObject parentObject;
    [SerializeField] private Animator transitionAnimator;
    [SerializeField] private GameObject transitionIcon;
    public Action OnFadeComplete;
    public bool fadeIsOn = false; 

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
            Destroy(parentObject);

        parentObject.transform.parent = null;
        DontDestroyOnLoad(parentObject);

        if(!transitionAnimator && GetComponent<Animator>())
            transitionAnimator = GetComponent<Animator>();

        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    }

    [Button]
    public void TriggerFade()
    {
        StartCoroutine(PlayFadeOutSmoothly());
    }

    private IEnumerator PlayFadeOutSmoothly()
    {
        Cursor.visible = false;
        yield return null;
        yield return new WaitForEndOfFrame();

        transitionAnimator.SetTrigger("Fade");

        if (!fadeIsOn)
            fadeIsOn = true;
    }

    private IEnumerator PlayFadeAfterSceneChangeSmoothly()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        transitionAnimator.SetTrigger("Fade");
    }

    public void InvokeOnFadeComplete()
    {
        OnFadeComplete?.Invoke();
    }

    public void ChangeTransitionIconActiveState()
    {
        transitionIcon.SetActive(!transitionIcon.activeSelf);
    }

    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (fadeIsOn)
        {
            fadeIsOn = false; 

            if (!TransportSwitcher.Instance || !TransportSwitcher.Instance.isUsingRelay)
                StartCoroutine(PlayFadeAfterSceneChangeSmoothly());
        }
    }
}