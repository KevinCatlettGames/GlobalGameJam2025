using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class MatchSettingsUISetup : MonoBehaviour
{
    [SerializeField] GameModeSelection gameModeSelection;
    [SerializeField] LoadoutSelection loadoutSelection;
    [SerializeField] private Toggle playTutorialToggle;
    [SerializeField] private Toggle playEndlessToggle;
    [SerializeField] private Slider winsNeededSlider;
    [SerializeField] private TextMeshProUGUI winsNeededText;

    [SerializeField] private Toggle[] spellToggles;

    [SerializeField] private Toggle plateToggle;
    [SerializeField] private Toggle plateEventToggle;
    [SerializeField] private Slider plateRoundsSlider;
    [SerializeField] private TextMeshProUGUI plateRoundsText;

    [SerializeField] private Toggle potToggle;
    [SerializeField] private Toggle potEventToggle;
    [SerializeField] private Slider potRoundsSlider;
    [SerializeField] private TextMeshProUGUI potRoundsText;

    [SerializeField] private Toggle bucketToggle;
    [SerializeField] private Toggle bucketEventToggle;
    [SerializeField] private Slider bucketRoundsSlider;
    [SerializeField] private TextMeshProUGUI bucketRoundsText;

    [SerializeField] private Toggle tunaToggle;
    [SerializeField] private Toggle tunaEventToggle;
    [SerializeField] private Slider tunaRoundsSlider;
    [SerializeField] private TextMeshProUGUI tunaRoundsText;

    public void OnEnable()
    {
        Setup();
    }

    public void Setup()
    {
        LobbyManager lobbyManager = LobbyManager.instance;

        playTutorialToggle.SetIsOnWithoutNotify(lobbyManager.playTutorial);
        playEndlessToggle.SetIsOnWithoutNotify(lobbyManager.playEndless);
        winsNeededSlider.SetValueWithoutNotify(lobbyManager.winsNeeded);
        winsNeededText.text = lobbyManager.winsNeeded.ToString();

        int gameModeValue = (int)lobbyManager.SelectedGameMode;
        if (gameModeValue == 1)
        {
            gameModeSelection.UpdateBubbles(1);
            gameModeSelection.UpdateGameModeSelectionUI();
        }
        else
        {
            gameModeSelection.UpdateBubbles(0);
            gameModeSelection.UpdateGameModeSelectionUI();
        }

        int loadoutValue = (int)lobbyManager.selectedLoadoutType;
        if (loadoutValue == 1)
        {
            loadoutSelection.OnLoadoutButtonClick();
        }
        else if (loadoutValue == 2)
        {
            loadoutSelection.OnLoadoutButtonClick();
            loadoutSelection.OnLoadoutButtonClick();
        }
        else if(loadoutValue == 0)
        {
            loadoutSelection.ResetLoadoutType();
        }

            for (int i = 0; i < spellToggles.Length; i++)
            {
                if (!lobbyManager.Spells[i].CanUse)
                {
                    spellToggles[i].SetIsOnWithoutNotify(false);
                    spellToggles[i].GetComponentInChildren<ImageSwitchOnBool>().SetImage(false);
                    spellToggles[i].GetComponentInChildren<Outline>().enabled = false;
                }
                else if (!spellToggles[i].isOn && lobbyManager.Spells[i].CanUse)
                {
                    spellToggles[i].SetIsOnWithoutNotify(true);
                    spellToggles[i].GetComponentInChildren<ImageSwitchOnBool>().SetImage(true);
                    spellToggles[i].GetComponentInChildren<Outline>().enabled = true;
                }
            }

        plateToggle.SetIsOnWithoutNotify(lobbyManager.MapSettings[0].PlayMap);
        lobbyManager.ToggleUsageOfPlateMap(lobbyManager.MapSettings[0].PlayMap);
        plateEventToggle.SetIsOnWithoutNotify(lobbyManager.MapSettings[0].PlayWithMapEvent);
        plateRoundsSlider.SetValueWithoutNotify(lobbyManager.MapSettings[0].MapRounds);
        plateRoundsText.text = lobbyManager.MapSettings[0].MapRounds.ToString();

        potToggle.SetIsOnWithoutNotify(lobbyManager.MapSettings[1].PlayMap);
        lobbyManager.ToggleUsageOfPotMap(lobbyManager.MapSettings[1].PlayMap);
        potEventToggle.SetIsOnWithoutNotify(lobbyManager.MapSettings[1].PlayWithMapEvent);
        potRoundsSlider.SetValueWithoutNotify(lobbyManager.MapSettings[1].MapRounds);
        potRoundsText.text = lobbyManager.MapSettings[1].MapRounds.ToString();


        bucketToggle.SetIsOnWithoutNotify(lobbyManager.MapSettings[2].PlayMap);
        lobbyManager.ToggleUsageOfBucketMap(lobbyManager.MapSettings[2].PlayMap);
        bucketEventToggle.SetIsOnWithoutNotify(lobbyManager.MapSettings[2].PlayWithMapEvent);
        bucketRoundsSlider.SetValueWithoutNotify(lobbyManager.MapSettings[2].MapRounds);
        bucketRoundsText.text = lobbyManager.MapSettings[2].MapRounds.ToString();

        tunaToggle.SetIsOnWithoutNotify(lobbyManager.MapSettings[3].PlayMap);
        lobbyManager.ToggleUsageOfTunaMap(lobbyManager.MapSettings[3].PlayMap);
        tunaEventToggle.SetIsOnWithoutNotify(lobbyManager.MapSettings[3].PlayWithMapEvent);
        tunaRoundsSlider.SetValueWithoutNotify(lobbyManager.MapSettings[3].MapRounds);
        tunaRoundsText.text = lobbyManager.MapSettings[3].MapRounds.ToString();
    }
}