using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime; // FMOD integration namespace

public class MusicIntensityManager : MonoBehaviour
{
    private int currentRegisteredPlayers;
    [SerializeField]
    private const string musicEventPath = "event:/music/choir track/music1"; // FMOD event path

    private bool canUpdateAudio = false;
    private EventInstance musicInstance; // FMOD music instance
    private PlayerManager currentPlayer; // Reference to PlayerManager

    private int musicDelay = 10; // Delay before the music intensity starts adjusting

    private void Start()
    {
        StartCoroutine(HandleFirstPlayerDelay()); // Start the delay coroutine
        currentPlayer = PlayerManager.Instance; // Get the PlayerManager instance

        // Create and start the FMOD music instance
        musicInstance = RuntimeManager.CreateInstance(musicEventPath);
        musicInstance.start(); // Start the music

        // Initialize the global parameter for PlayerCount
        UpdateMusicIntensity();
        SetInitialMusicIntensity();

    }

    private void SetInitialMusicIntensity()
    {
        // Set the music intensity to 4 (maximum) when all players are active
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("PlayerCount", 4);
        Debug.Log("Initial music intensity set to maximum (level 4).");
    }

    private IEnumerator HandleFirstPlayerDelay()
    {
        // Wait for the delay time before starting to adjust the music intensity
        yield return new WaitForSeconds(musicDelay);

        canUpdateAudio = true;
    }

    private void OnDisable()
    {
        // Stop and release the music instance when the script is disabled
        if (musicInstance.isValid())
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            musicInstance.release();
        }
    }

    private void Update()
    {
        // Only update intensity if the number of players has changed
        if (currentRegisteredPlayers != currentPlayer.activePlayers)
        {
            currentRegisteredPlayers = currentPlayer.activePlayers;
            UpdateMusicIntensity();
        }
    }

    private void UpdateMusicIntensity()
    {
        if (canUpdateAudio == false)
        {
            return;
        }
        // Reverse the intensity logic: fewer players = higher intensity, more players = lower intensity
        // You can modify the mapping logic here for smoother transitions if needed
        float clampedPlayers = Mathf.Clamp(currentPlayer.activePlayers, 2, 4);
        float intensityValue = 4 - clampedPlayers; // Intensity is higher with fewer players

        // Update the global parameter 'PlayerCount' in FMOD
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("PlayerCount", intensityValue);
    }
}
