using UnityEngine;

public class Persistence : MonoBehaviour
{
    private void Awake() => DontDestroyOnLoad(this);
}
