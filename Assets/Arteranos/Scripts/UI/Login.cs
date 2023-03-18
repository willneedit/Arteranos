using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


using Arteranos.Core;
using Arteranos.Auth;
using Cdm.Authentication.Browser;
using Cdm.Authentication.Clients;
using Cdm.Authentication.OAuth2;
using System.Threading.Tasks;
using System;
using Cdm.Authentication;
using Codice.Client.BaseCommands;

namespace Arteranos.UI
{
    public class Login : UIBehaviour
    {
        public TMP_Dropdown Chooser = null;
        public Button SignIn = null;
        public Button GuestLogin = null;
        public Button JoinServer = null;
        public Button CreateServer = null;

        protected override void Awake()
        {
            base.Awake();

            SignIn.onClick.AddListener(SignIn_btn);
            //GuestLogin.onClick += null;
            //JoinServer.onClick += null;
            //CreateServer += null;
        }

        protected override void Start() => base.Start();

        private void SignIn_btn()
        {
            LoginProvider lp = LoginProvider.Guest;

            switch(Chooser.value)
            {
                case 0:
                    lp = LoginProvider.Google; break;
                case 1:
                    lp = LoginProvider.Native; break;
                default:
                    Debug.LogError($"Unknown choice of the login provider: {Chooser.value}");
                    break;
            }

            _ = CommitSignin(lp);
        }

        private async Task CommitSignin(LoginProvider new_lp)
        {

            ILoginPackage lpack = OAuth2ClientKeys.GetOAuthData(new_lp);
            AuthorizationCodeFlow auth = lpack.GetAuthorizationCodeFlow();

            CrossPlatformBrowser crossPlatformBrowser = new();
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsEditor, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.WindowsPlayer, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXEditor, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.OSXPlayer, new StandaloneBrowser());
            crossPlatformBrowser.platformBrowsers.Add(RuntimePlatform.IPhonePlayer, new ASWebAuthenticationSessionBrowser());

            using AuthenticationSession authenticationSession = new(auth, crossPlatformBrowser);

            try
            {
                // Opens a browser to log user in
                AccessTokenResponse accessTokenResponse = await authenticationSession.AuthenticateAsync();
                string id = await lpack.GetUserIDAsync(authenticationSession);

                Debug.Log("Login successful.");
                Debug.Log("Saving...");
                SaveLogin(new_lp, accessTokenResponse.accessToken, id);
                Debug.Log("Saving done.");
            }
            catch(Exception e)
            {
                Debug.LogError($"Login failed: {e.Message}, falling back to a guest login");
                SaveLogin(LoginProvider.Guest, null, null);
                return;
            }
        }

        private void SaveLogin(LoginProvider lp, string token, string Username)
        {
            ClientSettings cs = SettingsManager.Client;

            Debug.Log($"Saving lp={lp}, token={token}, Username={Username}");
            cs.BearerToken = token;
            cs.LoginProvider = lp;
            cs.Username = Username;
            cs.RefreshAuthentication();

            cs.SaveSettings();
            Debug.Log("Saving succeeded");
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
