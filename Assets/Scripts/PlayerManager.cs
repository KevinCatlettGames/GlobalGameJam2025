using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [SerializeField]
    private GameObject playerPrefab; // Prefab for the player
    [SerializeField]
    private Transform spawnPointsParent; // Parent object containing spawn points
    [SerializeField]
    private int maxPlayers = 2; // Limit to 2 players for now

    private Transform[] spawnPoints; // Array of spawn points
    private List<int> usedSpawnIndices = new List<int>(); // Track used spawn indices

    private void Awake()
    {
        // Collect all child spawn points under the parent
        int spawnCount = spawnPointsParent.childCount;
        spawnPoints = new Transform[spawnCount];
        for (int i = 0; i < spawnCount; i++)
        {
            spawnPoints[i] = spawnPointsParent.GetChild(i);
        }
    }

    private void Start()
    {
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.onPlayerJoined += HandlePlayerJoined;
        }
        else
        {
            Debug.LogError("PlayerInputManager is not present in the scene. Please add it.");
        }
    }

    private void OnDisable()
    {
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.onPlayerJoined -= HandlePlayerJoined;
        }
    }

    private void HandlePlayerJoined(PlayerInput playerInput)
    {
        // Ensure we do not exceed the player limit
        if (PlayerInput.all.Count > maxPlayers)
        {
            Debug.LogWarning("Max player count reached. No more players can join.");
            Destroy(playerInput.gameObject);
            return;
        }

        // Get an unused spawn point
        int spawnIndex = GetRandomSpawnIndex();
        if (spawnIndex == -1)
        {
            Debug.LogWarning("No available spawn points.");
            return;
        }

        // Move the player to the chosen spawn point
        Transform spawnPoint = spawnPoints[spawnIndex];
        playerInput.transform.position = spawnPoint.position;
        playerInput.transform.rotation = spawnPoint.rotation;

        // Optional: Customize the player
        CustomizePlayer(playerInput.gameObject);
    }

    private int GetRandomSpawnIndex()
    {
        if (usedSpawnIndices.Count >= spawnPoints.Length)
            return -1; // No spawn points left

        int index;
        do
        {
            index = Random.Range(0, spawnPoints.Length);
        } while (usedSpawnIndices.Contains(index));

        usedSpawnIndices.Add(index);
        return index;
    }

    private void CustomizePlayer(GameObject player)
    {
        // Example: Assign a random color to differentiate players
        Renderer renderer = player.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Random.ColorHSV();
        }
    }
}
