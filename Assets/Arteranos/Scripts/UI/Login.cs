using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


using Arteranos.Core;
using Arteranos.Auth;
using Cdm.Authentication.Browser;
using Cdm.Authentication.OAuth2;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Threading;

namespace Arteranos.UI
{
    public class Login : UIBehaviour
    {
        public TMP_Text Status = null;
        public TMP_Dropdown Chooser = null;
        public Button SignIn = null;
        public Button GuestLogin = null;
        public Button JoinServer = null;
        public Button CreateServer = null;

        private string[] PackageNames = null;

        private CrossPlatformBrowser crossPlatformBrowser = null;

        private event Action<string> OnRefreshLoginUI;

        private string status_template = null;
        private string friendlyName = null;

        protected override void Awake()
        {
            base.Awake();

            crossPlatformBrowser = new();
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsEditor, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsPlayer, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXEditor, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXPlayer, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.IPhonePlayer, new ASWebAuthenticationSessionBrowser());

            PackageNames = LoginPackages.GetPackageNames();

            List<string> options = (from x in PackageNames select $"Login with {x}").ToList();
            Chooser.ClearOptions();
            Chooser.AddOptions( options );

            status_template = Status.text;

            SignIn.onClick.AddListener(() => _ = CommitSigninAsync(PackageNames[Chooser.value]));
            GuestLogin.onClick.AddListener(() => CommitSignOut());
            //JoinServer.onClick += null;
            //CreateServer += null;

        }

        protected override void Start()
        {
            base.Start();

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
                Status.text = string.Format(status_template, lp, friendlyName);
                Status.enabled = true;
                SignIn.GetComponentInChildren<TextMeshProUGUI>().text = "Switch account";
                GuestLogin.GetComponentInChildren<TextMeshProUGUI>().text = "Log out";
                GuestLogin.gameObject.SetActive(true);
            }
            else
            {
                Status.enabled = false;
                SignIn.GetComponentInChildren<TextMeshProUGUI>().text = "Log in";
                GuestLogin.gameObject.SetActive(false); // Reminder: to entirely hide the button, not just disable it.
            }

        }

        CancellationTokenSource source = null;

        private async Task CommitSigninAsync(string new_lp)
        {
            if(source != null)
            {
                source.Cancel();
                source= null;
                return;
            }

            ILoginPackage lpack = LoginPackages.GetPackage(new_lp);
            AuthorizationCodeFlow auth = lpack.GetAuthorizationCodeFlow();

            using AuthenticationSession authenticationSession = new(auth, crossPlatformBrowser);

            try
            {
                source = new();
                SignIn.GetComponentInChildren<TextMeshProUGUI>().text = "Abort login attempt";
                GuestLogin.gameObject.SetActive(false);

                string id;
                // Opens a browser to log user in
                AccessTokenResponse accessTokenResponse = await authenticationSession.AuthenticateAsync(source.Token);
                (id, friendlyName) = await lpack.GetUserIDAsync(authenticationSession);

                Debug.Log("Login successful.");
                SaveLogin(new_lp, accessTokenResponse.HasRefreshToken() ? accessTokenResponse.refreshToken : null, id);
            }
            catch(Exception e)
            {
                Debug.LogError($"Login failed: {e.Message}, falling back to a guest login");
                SaveLogin(null, null, null);
            }

            source.Dispose();
            source = null;
        }

        private void CommitSignOut() => SaveLogin(null, null, null);

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


#if false
var crossPlatformBrowser = new CrossPlatformBrowser();
var crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsEditor, new StandaloneBrowser());
var crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsPlayer, new StandaloneBrowser());
var crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXEditor, new StandaloneBrowser());
var crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXPlayer, new StandaloneBrowser());
var crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.IPhonePlayer, new ASWebAuthenticationSessionBrowser());

using var authenticationSession = new AuthenticationSession(auth, crossPlatformBrowser);

// Opens a browser to log user in
AccessTokenResponse accessTokenResponse = await authenticationSession.AuthenticateAsync();

// Authentication header can be used to make authorized http calls.
AuthenticationHeaderValue authenticationHeader = accessTokenResponse.GetAuthenticationHeader();

// Gets the current acccess token, or refreshes if it is expired.
accessTokenResponse = await authenticationSession.GetOrRefreshTokenAsync();

// Gets new access token by using the refresh token.
AccessTokenResponse newAccessTokenResponse = await authenticationSession.RefreshTokenAsync();

// Or you can get new access token with specified refresh token (i.e. stored on the local disk to prevent multiple sign-in for each app launch)
newAccessTokenResponse = await authenticationSession.RefreshTokenAsync("my_refresh_token");
#endif
    }
}
