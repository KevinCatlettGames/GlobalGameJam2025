using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Video;

public class TutorialPopUp : MonoBehaviour
{
    [SerializeField] private GameObject popUp;
    [SerializeField] private TutorialTextBox tutorialTextBox;
    [SerializeField] private GameObject closePrompt;
    [SerializeField] private DummyController dummy;
    [SerializeField] private DotweenAnchorTransition transition;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoImage;

    private RenderTexture renderTexture;
    private bool isOpened = false;
    private bool isDone = false;
    private bool dummySpawned = false;
    private bool canBeClosed = false;
    private InputAction continueAction;
    List<PlayerController> players; 

    private void Start()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            InputSystemUIInputModule inputModuleUI = eventSystem.gameObject.GetComponent<InputSystemUIInputModule>();
            if (inputModuleUI != null && inputModuleUI.actionsAsset != null)
            {
                continueAction = inputModuleUI.actionsAsset.FindAction("UI/Submit");

                if (continueAction != null)
                {
                    continueAction.performed += OnSubmitInput;
                    continueAction.Enable();
                }
            }
        }
        renderTexture = new RenderTexture(1280, 720, 0);
        renderTexture.Create();
        videoPlayer.targetTexture = renderTexture;
        videoImage.texture = renderTexture;
        videoPlayer.Prepare();
    }

    public void OpenPopUp()
    {
        popUp.SetActive(true);
        isOpened = true;
        tutorialTextBox.ToggleTutorialText(false);

        if(!TransportSwitcher.Instance || TransportSwitcher.Instance && !TransportSwitcher.Instance.isUsingRelay)
            Time.timeScale = 0f;

        videoPlayer.Play();
        StartCoroutine(ShowClosePrompt());
    }
    private IEnumerator ShowClosePrompt()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        closePrompt.SetActive(true);
        canBeClosed = true;
    }
    public void OnSubmitInput(InputAction.CallbackContext context)
    {
        if (!context.performed || !canBeClosed || isDone || !isOpened) return;
        transition.DoOutro();
        continueAction.performed -= OnSubmitInput;
        players = PlayerManager.Instance.GetPlayers();
        isDone = true;
        Time.timeScale = 1f;
        tutorialTextBox.ToggleTutorialText(true);
        tutorialTextBox.AdvanceTextBox();
    }
    public bool CheckForDummySpawn()
    {
        if (dummySpawned || !isDone) return false;
        if (players != null && players.Count > 0)
        {
            bool spawn = false;
            foreach (PlayerController player in players)
            {
                if (player.HasTwoSpells())
                {
                    spawn = true;
                    break;
                }
            }
            if (spawn)
            {
                dummy.StartDummy();
                dummySpawned = true;
                return true;
            }
        }
        return false;
    }
    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }
}
