using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Management;


public class XRControl : MonoBehaviour
{
    private Arteranos.Core.SettingsManager m_SettingsManager = null;


    public XROrigin VRRig;
    public XROrigin NoVRRig;

    public IEnumerator StartXRCoroutine()
    {
        Debug.Log("Initializing XR...");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("Initializing XR Failed. Check Editor or Player log for details.");
            m_SettingsManager.m_Client.VRMode = false;
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
        m_SettingsManager = GetComponent<Arteranos.Core.SettingsManager>();
        m_SettingsManager.m_Client.OnVRModeChanged += OnVRModeChanged;
        if(m_SettingsManager.m_Client.VRMode)
            OnVRModeChanged(false, true);
        else
            UpdateXROrigin(false);
    }

    void OnVRModeChanged(bool old, bool current)
    {
        if(current)
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
        if(m_SettingsManager.m_Client.VRMode)
            StopXR();
    }

}
