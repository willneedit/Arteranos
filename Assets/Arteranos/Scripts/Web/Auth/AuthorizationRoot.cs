/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Cdm.Authentication.Clients;
using Cdm.Authentication.OAuth2;
using System.Threading.Tasks;

namespace Arteranos.Web.Auth
{
    public interface ILoginPackage
    {
        public AuthorizationCodeFlow GetAuthorizationCodeFlow();
        public Task<(string, string)> GetUserIDAsync(AuthenticationSession session);
    }

    public class MockServerPackage : ILoginPackage
    {
        public AuthorizationCodeFlow GetAuthorizationCodeFlow()
        {
            AuthorizationCodeFlow.Configuration configMock = new()
            {
                clientId = "my_client_id",
                clientSecret = "my_client_secret",
#if UNITY_IOS && !UNITY_EDITOR
                redirectUri = "com.cdm.myauthapp:/oauth2",
#else
                redirectUri = "http://localhost:8080/myauthapp/oauth2/",
#endif
                scope = "openid email profile"
            };
            return new MockServerAuth(configMock, "http://localhost:8001");
        }
        public async Task<(string, string)> GetUserIDAsync(AuthenticationSession session) => await Task.Run(() => ("MockServerUser", "MSU#0000"));
    }

    public struct PackageListEntry
    {
        public string name;
        public ILoginPackage pack;
    }
}
