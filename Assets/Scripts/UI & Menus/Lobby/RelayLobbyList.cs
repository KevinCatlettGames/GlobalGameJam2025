using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles fetching, storing, and displaying a list of public relay lobbies.
/// Populates a UI scroll list with lobby entries and allows refreshing.
/// </summary>
public class RelayLobbyList : MonoBehaviour
{
    [Header("UI References")]
    public GameObject publicLobbyListPrefab; // Prefab representing each lobby entry
    public Transform lobbyListContainer;     // Parent transform for instantiated lobby items
    public GameObject noLobbiesText;
    /// <summary>
    /// List of currently fetched lobbies.
    /// </summary>
    public List<Lobby> CurrentLobbies { get; private set; } = new List<Lobby>();

    /// <summary>
    /// List of instantiated lobby UI items.
    /// </summary>
    public List<GameObject> lobbyItems = new List<GameObject>();
    
    /// <summary>
    /// Unity Start. Automatically refreshes the lobby list on start.
    /// </summary>
    private async void Start()
    {
        RefreshLobbyList();
    }

    /// <summary>
    /// Refreshes the lobby list by fetching public lobbies and populating the UI.
    /// </summary>
    public async void RefreshLobbyList()
    {
        foreach (GameObject lobbyItem in lobbyItems)
            Destroy(lobbyItem);
        
        lobbyItems.Clear();
        CurrentLobbies = await GetPublicLobbiesAsync(20);
        PopulateLobbyList();

        int lobbyAmountinList = lobbyListContainer.childCount;
        noLobbiesText.SetActive(lobbyAmountinList <= 0);
    }

    /// <summary>
    /// Populates the UI list with current lobbies.
    /// </summary>
    private void PopulateLobbyList()
    {
        foreach (Transform child in lobbyListContainer)
        {
            Destroy(child.gameObject);
        }
        
        foreach (Lobby lobby in CurrentLobbies)
        {
            GameObject lobbyItem = Instantiate(publicLobbyListPrefab, lobbyListContainer);
            lobbyItems.Add(lobbyItem);
            
            LobbyItemUI ui = lobbyItem.GetComponent<LobbyItemUI>();
            if (ui != null)
            {
                ui.Setup(lobby);
            }
        }
    }

    /// <summary>
    /// Queries the Unity Lobby Service for public lobbies with available slots.
    /// </summary>
    /// <param name="maxResults">Maximum number of lobbies to return.</param>
    /// <returns>List of lobbies returned by the query.</returns>
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
                    asc: false,
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