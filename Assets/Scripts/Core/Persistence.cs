using UnityEngine;

public class Persistence : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        MonoBehaviour.DontDestroyOnLoad(this);
    }
}
