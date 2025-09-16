using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RelayLobbyList : MonoBehaviour
{
    [Header("UI References")]
    public GameObject publicLobbyListPrefab; // Prefab for each lobby entry
    public Transform lobbyListContainer;     // Content object under the ScrollRect

    public List<Lobby> CurrentLobbies { get; private set; } = new List<Lobby>();

    public List<GameObject> lobbyItems = new List<GameObject>();
    
    private async void Start()
    {
        RefreshLobbyList();
    }

    public async void RefreshLobbyList()
    {

        foreach (GameObject lobbyItem in lobbyItems)
        {
            Destroy(lobbyItem);
        }
        lobbyItems.Clear();
        
        // Fetch and store the lobbies on startup
        CurrentLobbies = await GetPublicLobbiesAsync(20);

        // Populate the UI
        PopulateLobbyList();
    }

    private void PopulateLobbyList()
    {
        // Clear existing items first
        foreach (Transform child in lobbyListContainer)
        {
            Destroy(child.gameObject);
        }

        // Instantiate new items
        foreach (Lobby lobby in CurrentLobbies)
        {
            GameObject lobbyItem = Instantiate(publicLobbyListPrefab, lobbyListContainer);
            lobbyItems.Add(lobbyItem);
            // Assuming prefab has a LobbyItemUI script
            LobbyItemUI ui = lobbyItem.GetComponent<LobbyItemUI>();
            if (ui != null)
            {
                ui.Setup(lobby);
            }
        }
    }

    public async Task<List<Lobby>> GetPublicLobbiesAsync(int maxResults = 20)
    {
        var options = new QueryLobbiesOptions
        {
            Count = maxResults,
            Filters = new List<QueryFilter>
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0"
                )
            },
            Order = new List<QueryOrder>
            {
                new QueryOrder(
                    asc: false,  // newest first
                    field: QueryOrder.FieldOptions.Created
                )
            }
        };

        try
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            return response.Results;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to query lobbies: {e}");
            return new List<Lobby>();
        }
    }
}
