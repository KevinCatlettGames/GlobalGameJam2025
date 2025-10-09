using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapRotationSystem : MonoBehaviour
{
    [SerializeField] private int maxRounds = 3;
    [SerializeField] private string[] sceneNames;

    public static MapRotationSystem Instance;
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool CheckForMapSwitch(int roundCount)
    {
        if (roundCount < maxRounds || sceneNames.Length <= 1)
            return false; 
        
        string i;
        do
        {
            int r = Random.Range(0, sceneNames.Length);
            i = sceneNames[r];
        }
        while (i == SceneManager.GetActiveScene().name);
        NetworkManager.Singleton.SceneManager.LoadScene(i, LoadSceneMode.Single);
        return true;
    }
}
