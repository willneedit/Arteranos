using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Management;


public class XRControl : MonoBehaviour
{
    public bool enableVR = false;
    private bool currentVR = false;


    public XROrigin VRRig;
    public XROrigin NoVRRig;

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
            UpdateXROrigin(true);
        }
    }

    void StopXR()
    {
        Debug.Log("Stopping XR...");

        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        Debug.Log("XR stopped completely.");
        UpdateXROrigin(false);
    }
    
    // Start is called before the first frame update
    void Start()
    {
        Core.SettingsManager sm = GetComponent<Core.SettingsManager>();
        enableVR = sm.m_Client.VRMode;
        UpdateXROrigin(false);  // Current VR mode is initially false, to enter it if necessary.
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

    void UpdateXROrigin(bool useVR)
    {
        XROrigin oldXROigin = Object.FindObjectOfType<XROrigin>();

        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        if(oldXROigin != null)
        {
            position = oldXROigin.transform.position;
            rotation = oldXROigin.transform.rotation;

            Destroy(oldXROigin);
        }

        Instantiate(useVR ? VRRig : NoVRRig, position, rotation);
    }

    // Cleanly shut down XR on exit.
    void OnApplicationQuit()
    {
        // Debug.Log("Bye!");
        if(enableVR)
            StopXR();
    }

}
