using System.Collections;
using System.Collections.Generic;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class TutorialPopUp : MonoBehaviour
{
    [SerializeField] private GameObject popUp;
    [SerializeField] private GameObject closePrompt;
    [SerializeField] private DummyController dummy;

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
    }

    public void OpenPopUp()
    {
        popUp.SetActive(true);
        isOpened = true;
        Time.timeScale = 0f;
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
        popUp.SetActive(false);
        continueAction.performed -= OnSubmitInput;
        players = PlayerManager.Instance.GetPlayers();
        isDone = true;
        Time.timeScale = 1f;
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
}
