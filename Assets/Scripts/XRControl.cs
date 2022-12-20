using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

public class XRControl : MonoBehaviour
{
    public bool enableVR = false;
    private bool currentVR = false;

    public IEnumerator StartXRCoroutine()
    {
        Debug.Log("Initializing XR...");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("Initializing XR Failed. Check Editor or Player log for details.");
            enableVR = false;
            currentVR = false;
        }
        else
        {
            Debug.Log("Starting XR...");
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }
    }

    void StopXR()
    {
        Debug.Log("Stopping XR...");

        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        Debug.Log("XR stopped completely.");
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (enableVR == currentVR) return;

        currentVR = enableVR;

        if(enableVR)
            StartCoroutine(StartXRCoroutine());
        else
            StopXR();
    }

    // Cleanly shut down XR on exit.
    void OnApplicationQuit()
    {
        // Debug.Log("Bye!");
        if(enableVR)
            StopXR();
    }

}
