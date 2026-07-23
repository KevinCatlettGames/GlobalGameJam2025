using FMODUnity;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerContainerSkinChange : NetworkBehaviour
{
    [SerializeField] private Image avatar;
    [SerializeField] private Image playerTextImage;
    [SerializeField] private Image blurImage;
    [SerializeField] private float blurImageAlpha = .1f;
    public Color initialBlurColor;
    [SerializeField] private StudioEventEmitter cycleEmitter;

    public int currentColorIndex;
    public int playerIndex = 0;
    public bool currentlyOnLocked;
    [SerializeField] private TeamSelection teamSelection;
    public SkinButtonHandler[] allSkinSelections;
    public SkinButtonHandler currentSkinSelection;
    bool init;
    bool wasInit = false;
    public GameObject emptyPlayerContainer;

    private void OnDisable()
    {
        if (LobbyManager.instance != null)
            LobbyManager.instance.OnReadyStateUpdated.RemoveListener(ReadyStateUpdated);

        if (IsServer && TransportSwitcher.Instance.isUsingRelay)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        }

        blurImage.color = initialBlurColor;
    }

    private void Awake()
    {
        initialBlurColor = blurImage.color;
    }

    private void OnEnable()
    {
        Init();
    }

    void Init()
    {
        if (LobbyManager.instance != null)
            LobbyManager.instance.OnReadyStateUpdated.AddListener(ReadyStateUpdated);

        if (init && !wasInit)
        {
            currentSkinSelection = allSkinSelections[playerIndex];
            currentSkinSelection.ChangePlayerIcon(-1, playerIndex, GetComponent<PlayerContainerManager>());
            currentColorIndex = currentSkinSelection.skinSo.Index;
            avatar.GetComponent<ScaleToCorrectSize>().Play();
            UpdateSkin();
            wasInit = true;
        }
        init = true;
        UpdateBlur();
    }


    private void Start()
    {
        if (IsServer && TransportSwitcher.Instance.isUsingRelay)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        }
    }

    void OnClientConnectedCallback(ulong clientID)
    {
        if (clientID == NetworkManager.Singleton.LocalClientId) return;
        if (!IsSpawned || !IsServer) return;
        StartCoroutine(WaitAndShareValues(clientID));
    }

    IEnumerator WaitAndShareValues(ulong clientID)
    {
        yield return new WaitForSeconds(2f);
        ShareValuesClientRpc(clientID, currentColorIndex, currentlyOnLocked, currentSkinSelection.skinButtonHandlerIndex);
    }



    [ClientRpc]
    void ShareValuesClientRpc(ulong clientID, int currentColorIndex, bool currentlyOnLocked, int currentSkinSelectionIndex)
    {
        if (NetworkManager.Singleton.LocalClientId != clientID) return;
        init = true;
        wasInit = true;
        this.currentSkinSelection.ChangePlayerIcon(-1, playerIndex, GetComponent<PlayerContainerManager>());
        this.currentColorIndex = currentColorIndex;
        this.currentlyOnLocked = currentlyOnLocked;
        this.currentSkinSelection = allSkinSelections[currentSkinSelectionIndex];
        currentSkinSelection.ChangePlayerIcon(-1, playerIndex, GetComponent<PlayerContainerManager>());
        avatar.GetComponent<ScaleToCorrectSize>().Play();
        UpdateSkin();
        UpdateBlur();
        foreach(LobbyPlayerInput lobbyPlayerInput in LobbyManager.instance.allLobbyPlayerInputs)
        {
            if (lobbyPlayerInput.IsOwner)
                lobbyPlayerInput.HandleJoinAfterValuesAreShared();
        }
    }

    public void ReadyStateUpdated(int playerIndex, bool state)
    {
        if (playerIndex == this.playerIndex)
        {
            currentSkinSelection.ToggleReadyVisuals();
        }

        Invoke(nameof(UpdateLocked), .2f);
    }

    void UpdateLocked()
    {
        SkinSO skinToUse = LobbyManager.instance.PossibleSkins[currentColorIndex];
        currentlyOnLocked = isSkinLocked(skinToUse);
        playerTextImage.color = skinToUse.Color;
        avatar.sprite = skinToUse.SplashArt;

        if (currentlyOnLocked)
            avatar.color = Color.gray;
        else
            avatar.color = Color.white;
    }

    public void SwapColorWithIncrementation(bool increment)
    {
        if (LobbyManager.instance != null && LobbyManager.instance.players[playerIndex].IsReady) return;

        int totalSkins = LobbyManager.instance.PossibleSkins.Length;
        currentColorIndex = increment
            ? (currentColorIndex + 1) % totalSkins
            : (currentColorIndex - 1 + totalSkins) % totalSkins;
        UpdateSkin();
        cycleEmitter.Play();
    }

    public void ResetContainer()
    {
        currentSkinSelection.ChangePlayerIcon(-1,playerIndex, GetComponent<PlayerContainerManager>());
        currentSkinSelection = null;
        emptyPlayerContainer.SetActive(true);
        wasInit = false;
        init = true;
        gameObject.SetActive(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetContainerServerRpc()
    {
        ResetContainerClientRpc();
    }

    [ClientRpc]
    void ResetContainerClientRpc()
    {
        currentSkinSelection.ChangePlayerIcon(-1, playerIndex, GetComponent<PlayerContainerManager>());
        currentSkinSelection = null;
        emptyPlayerContainer.SetActive(true);
        wasInit = false;
        init = true;
        gameObject.SetActive(false);
    }

    public void ChangeSkin(Vector2 skinChangeInput)
    {
        SkinButtonHandler availableSkin = null;

        if (skinChangeInput.x == 0 && skinChangeInput.y == 0)
        {
            foreach (SkinButtonHandler skinButtonHandler in allSkinSelections)
            {
                if (skinButtonHandler != currentSkinSelection)
                {
                    availableSkin = skinButtonHandler;
                    break;
                }
            }
            currentSkinSelection.ChangePlayerIcon(-1, playerIndex, GetComponent<PlayerContainerManager>());
            currentSkinSelection = availableSkin;
            currentColorIndex = currentSkinSelection.skinSo.Index;
            avatar.GetComponent<ScaleToCorrectSize>().Play();
            UpdateBlur();
            UpdateSkin();
        }
        else if (skinChangeInput.x > 0)
        {
            if (!currentSkinSelection.rightSkinSelection) return;
            SkinButtonHandler skinToCheck = currentSkinSelection.rightSkinSelection;
            availableSkin = skinToCheck;
        }
        else if (skinChangeInput.x < 0)
        {
            if (!currentSkinSelection.leftSkinSelection) return;
            SkinButtonHandler skinToCheck = currentSkinSelection.leftSkinSelection;
            availableSkin = skinToCheck;
        }
        else if (skinChangeInput.y > 0)
        {
            if (!currentSkinSelection.topSkinSelection) return;
            SkinButtonHandler skinToCheck = currentSkinSelection.topSkinSelection;
            availableSkin = skinToCheck;
        }
        else if (skinChangeInput.y < 0)
        {
            if (!currentSkinSelection.bottomSkinSelection) return;
            SkinButtonHandler skinToCheck = currentSkinSelection.bottomSkinSelection;
            availableSkin = skinToCheck;                 
        }

        if (availableSkin == null)
        {
            foreach (SkinButtonHandler skinButtonHandler in allSkinSelections)
            {
                if (!SteamIntegration.instance.IsFullVersion && !skinButtonHandler.skinSo.AvailableInDemo) return;
                if (skinButtonHandler != currentSkinSelection)
                {
                    availableSkin = skinButtonHandler;
                }
            }
        }
        if (availableSkin == null) return; 

        currentSkinSelection.ChangePlayerIcon(-1, playerIndex, GetComponent<PlayerContainerManager>());
        currentSkinSelection = availableSkin;
        currentColorIndex = currentSkinSelection.skinSo.Index;
        avatar.GetComponent<ScaleToCorrectSize>().Play();
        UpdateBlur();
        UpdateSkin();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeSkinServerRpc(Vector2 skinChangeInput, ulong clientThatCalledThis)
    {
        ChangeSkinClientRpc(skinChangeInput, clientThatCalledThis);
    }

    [ClientRpc]
    public void ChangeSkinClientRpc(Vector2 skinChangeInput, ulong clientThatCalledThis)
    {
        if (NetworkManager.Singleton.LocalClientId == clientThatCalledThis) return;
        ChangeSkin(skinChangeInput);
    }


    [ServerRpc(RequireOwnership = false)]
    public void UpdateSkinServerRpc()
    {
        UpdateSkin();
        UpdateSkinClientRpc();
    }

    [ClientRpc]
    private void UpdateSkinClientRpc()
    {
        UpdateSkin();
    }

    public void UpdateSkin()
    {
        if(!gameObject.activeSelf) return;

        SkinSO skinToUse = LobbyManager.instance.PossibleSkins[currentColorIndex];
    
        currentlyOnLocked = isSkinLocked(skinToUse);

        if (!currentlyOnLocked &&
            playerIndex >= 0 &&
            playerIndex < LobbyPlayerValues.Instance.playerValuesList.Count)
        {
            LobbyPlayerValues.Instance.playerValuesList[playerIndex].Skin = skinToUse;
        }       
        ApplySkinVisuals();
    }

    private void ApplySkinVisuals()
    {
        SkinSO skinToUse = LobbyManager.instance.PossibleSkins[currentColorIndex];
        playerTextImage.color = skinToUse.Color;
        avatar.sprite = skinToUse.SplashArt;

        if (currentlyOnLocked)
        {
            avatar.color = Color.gray;
            if (currentSkinSelection)
                currentSkinSelection.ChangePlayerIcon(1, playerIndex, GetComponent<PlayerContainerManager>());
        }
        else
        {
            avatar.color = Color.white;
            if (currentSkinSelection)
                currentSkinSelection.ChangePlayerIcon(1, playerIndex, GetComponent<PlayerContainerManager>());
        }
    }

    bool isSkinLocked(SkinSO skinToCheck)
    {
        for (int i = 0; i < LobbyPlayerValues.Instance.playerValuesList.Count; i++)
        {
            if (i == playerIndex) continue;
            var otherSkin = LobbyPlayerValues.Instance.playerValuesList[i].Skin;
            if (otherSkin != null && otherSkin == skinToCheck &&
                i < LobbyManager.instance.players.Count &&
                LobbyManager.instance.players[i].IsReady)
            {
                return true;
            }
        }

        return false;
    }

    public void UpdateBlur()
    {
        if (!currentSkinSelection) return;
        blurImage.enabled = true;
        Color c = Color.white;

        if (LobbyManager.instance.SelectedGameMode == GameManager.GameModeType.Standard)
            c = currentSkinSelection.skinSo.Color;
        else if (LobbyManager.instance.SelectedGameMode == GameManager.GameModeType.Team)
        {
            if (teamSelection.CurrentTeamIndex == 1)
                c = LobbyManager.instance.TeamColors[0];
            else if (teamSelection.CurrentTeamIndex == 2)
                c = LobbyManager.instance.TeamColors[1];
        }

        c.a = blurImageAlpha;
        blurImage.color = c;
    }
}