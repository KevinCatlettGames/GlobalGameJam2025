using UnityEngine;
using FMODUnity;
using FMOD.Studio; // FMOD integration namespace

public class MusicIntensityManager : MonoBehaviour
{
    [SerializeField]
    private string musicEventPath = "event:/music/choir track/music1"; // FMOD event path

    private EventInstance musicInstance; // FMOD music instance
    private int currentPlayers = 0; // Current number of players
    private void Start()
    {
        // Create and start the FMOD music instance
        musicInstance = RuntimeManager.CreateInstance(musicEventPath);
        musicInstance.start();

        // Initialize the global parameter for PlayerCount
        UpdateMusicIntensity();
    }

    private void OnDisable()
    {
        // Stop and release the music instance when the script is disabled
        musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        musicInstance.release();
    }

    /*public void SpawnPlayer()
    {
        // Increment the player count and update the music intensity
        currentPlayers++;
        UpdateMusicIntensity();
        Debug.Log($"Player spawned. Current players: {currentPlayers}");
    }

    public void RemovePlayer()
    {
        // Decrement the player count and update the music intensity
        currentPlayers = Mathf.Max(currentPlayers - 1, 0);
        UpdateMusicIntensity();
        Debug.Log($"Player removed. Current players: {currentPlayers}");
    }*/

    private void UpdateMusicIntensity()
    {
        // Clamp the player count between 2 and 4 and update the FMOD global parameter
        float clampedPlayers = Mathf.Clamp(currentPlayers, 2, 4);

        // Update the global parameter 'PlayerCount' in FMOD
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("PlayerCount", clampedPlayers);

        // Debug to verify the changes
        Debug.Log($"Music intensity updated. PlayerCount set to: {clampedPlayers}");
    }
}
