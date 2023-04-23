using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Threading;
using Cdm.Authentication.Browser;
using Cdm.Authentication.OAuth2;

using Arteranos.Auth;

namespace Arteranos.Auth
{
#if !ARTERANOS_KEYS
    public static class LoginPackageList
    {
        public static readonly List<PackageListEntry> PackageList = new()
        {
            new PackageListEntry
            {
                name = "Mock",
                pack = new MockServerPackage()
            },
        };
    }
#endif

    public static class LoginPackages
    {
        public static ILoginPackage GetPackage(string name)
        {
            ILoginPackage[] query = (
                from x in LoginPackageList.PackageList
                where x.name == name
                select x.pack).ToArray();

            if(query.Length > 1) throw new ArgumentException($"{name}: ambiguous");

            return query.Length > 0 ? query[0] : null;
        }

        public static string[] GetPackageNames() => (from x in LoginPackageList.PackageList select x.name).ToArray();

    }
}

namespace Arteranos.UI
{
    public class LoginUI : UIBehaviour
    {
        public TMP_Text lbl_Status = null;
        public Spinner spn_Chooser = null;
        public Button btn_SignIn = null;
        public Button btn_GuestLogin = null;
        public Button btn_Cancel = null;
        public RectTransform grp_Cancel = null;

        public bool CancelEnabled = false;

        private string[] PackageNames = null;

        private CrossPlatformBrowser crossPlatformBrowser = null;

        private event Action<string> OnRefreshLoginUI;

        private string status_template = null;
        private string friendlyName = null;

        private readonly SemaphoreSlim loginFinished = new(0, 1);

        public static LoginUI New()
        {
            GameObject go = Instantiate(Resources.Load("UI/UI_Login") as GameObject);
            return go.GetComponent<LoginUI>();
        }

        protected override void Awake()
        {
            base.Awake();

            StandaloneBrowser sb = new()
            {
                closePageResponse = "<html><body><script>window.close();</script><b>DONE!</b><br>(You can close this tab/window now)</body></html>"
            };

            crossPlatformBrowser = new();
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsEditor, sb);
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsPlayer, sb);
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXEditor, sb);
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXPlayer, sb);
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.IPhonePlayer, new ASWebAuthenticationSessionBrowser());

            PackageNames = LoginPackages.GetPackageNames();

            string[] options = (from x in PackageNames select $"Login with {x}").ToArray();
            spn_Chooser.Options = options;

            status_template = lbl_Status.text;

            btn_SignIn.onClick.AddListener(() => _ = CommitSigninAsync(PackageNames[spn_Chooser.value]));
            btn_GuestLogin.onClick.AddListener(CommitSignOut);
            btn_Cancel.onClick.AddListener(CancelLogin);
        }

        protected override void Start()
        {
            base.Start();

            grp_Cancel.gameObject.SetActive(CancelEnabled);

            OnRefreshLoginUI += UpdateLoginUI;
            _ = AttemptRefreshTokenAsync();
        }

        private async Task AttemptRefreshTokenAsync()
        {
            string lp;
            string old_token;
            string id;

            (lp, old_token, id) = RetrieveLogin();

            ILoginPackage lpack = LoginPackages.GetPackage(lp);

            if(lpack == null)
            {
                OnRefreshLoginUI?.Invoke(null);
                return;
            }

            if(string.IsNullOrEmpty(old_token))
            {
                Debug.LogWarning($"No refresh token for this login, continuing without authorization backup.");
                friendlyName = "(unverified)";
                OnRefreshLoginUI?.Invoke(lp);
                return;
            }

            AuthorizationCodeFlow auth = lpack.GetAuthorizationCodeFlow();

            using AuthenticationSession authenticationSession = new(auth, crossPlatformBrowser);
            try
            {
                AccessTokenResponse accessTokenResponse = await authenticationSession.RefreshTokenAsync(old_token);
                (id, friendlyName) = await lpack.GetUserIDAsync(authenticationSession);

                Debug.Log("Login renewal successful.");
                SaveLogin(lp, accessTokenResponse.HasRefreshToken() ? accessTokenResponse.refreshToken : null, id);
            }
            catch(Exception e) 
            {
                Debug.LogError($"Login renewal failed: {e.Message}, reverting to a guest login");
                SaveLogin(null, null, null);
            }
        }

        private void UpdateLoginUI(string lp)
        {
            if(lp != null)
            {
                lbl_Status.text = string.Format(status_template, lp, friendlyName);
                lbl_Status.enabled = true;
                btn_SignIn.GetComponentInChildren<TextMeshProUGUI>().text = "Switch account";
                btn_GuestLogin.GetComponentInChildren<TextMeshProUGUI>().text = "Log out";
                btn_GuestLogin.gameObject.SetActive(true);
            }
            else
            {
                lbl_Status.enabled = false;
                btn_SignIn.GetComponentInChildren<TextMeshProUGUI>().text = "Log in";
                btn_GuestLogin.gameObject.SetActive(false); // Reminder: to entirely hide the button, not just disable it.
            }

        }

        CancellationTokenSource CancelSource = null;

        private async Task CommitSigninAsync(string new_lp)
        {
            if(CancelSource != null)
            {
                CancelSource.Cancel();
                CancelSource= null;
                return;
            }

            ILoginPackage lpack = LoginPackages.GetPackage(new_lp);
            AuthorizationCodeFlow auth = lpack.GetAuthorizationCodeFlow();

            using AuthenticationSession authenticationSession = new(auth, crossPlatformBrowser);

            try
            {
                CancelSource = new();
                btn_SignIn.GetComponentInChildren<TextMeshProUGUI>().text = "Abort login attempt";
                btn_GuestLogin.gameObject.SetActive(false);
                btn_Cancel.interactable = false;

                ManageVRLoginFlow(true);

                string id;
                // Opens a browser to log user in
                AccessTokenResponse accessTokenResponse = await authenticationSession.AuthenticateAsync(CancelSource.Token);
                (id, friendlyName) = await lpack.GetUserIDAsync(authenticationSession);

                Debug.Log("Login successful.");
                SaveLogin(new_lp, accessTokenResponse.HasRefreshToken() ? accessTokenResponse.refreshToken : null, id);
            }
            catch(Exception e)
            {
                Debug.LogError($"Login failed: {e.Message}, falling back to a guest login");
                SaveLogin(null, null, null);
            }

            ManageVRLoginFlow(false);

            btn_Cancel.interactable = true;
            CancelSource.Dispose();
            CancelSource = null;

            loginFinished.Release();
        }

        private DialogUI m_DialogUI = null;

        private void ManageVRLoginFlow(bool inProgress)
        {
            ClientSettings cs = SettingsManager.Client;
            if(inProgress)
            {
                // Already in Desktop mode, nothing to do.
                if(!cs.VRMode) return;

                m_DialogUI = DialogUI.New();
                m_DialogUI.text =
                    "Put down your VR headset,\n" +
                    "or switch into Desktop mode.\n" +
                    "Taking to the login window in a\n" +
                    "separate browser.";
                m_DialogUI.buttons = null;

                // Leave it in VR mode to be able to show the message.
            }
            else
            {
                if(m_DialogUI != null)
                {
                    Destroy(m_DialogUI.gameObject);
                    cs.VRMode = true;
                }
            }
        }

        private void CommitSignOut()
        {
            SaveLogin(null, null, null);
            loginFinished.Release();
        }

        private void CancelLogin() =>
            // Nothing more to do...
            loginFinished.Release();

        private (string, string, string) RetrieveLogin()
        {
            ClientSettings cs = SettingsManager.Client;
            return (cs.LoginProvider, cs.BearerToken, cs.Username);
        }

        private void SaveLogin(string lp, string token, string Username)
        {
            ClientSettings cs = SettingsManager.Client;

            Debug.Log($"Saving lp={lp}, token={token}, Username={Username}");
            cs.BearerToken = token;
            cs.LoginProvider = lp;
            cs.Username = Username;
            cs.RefreshAuthentication();

            cs.SaveSettings();
            Debug.Log("Saving succeeded");
            OnRefreshLoginUI?.Invoke(lp);
        }

        public async Task PerformLoginAsync()
        {
            await loginFinished.WaitAsync();

            Destroy(gameObject);
        }
    }
}
