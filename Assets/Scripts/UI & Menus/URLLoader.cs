using UnityEngine;

public class URLLoader : MonoBehaviour
{
    [SerializeField] string url;

    public void Load()
    {
        Application.OpenURL(url);
    }
}