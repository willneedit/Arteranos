using Arteranos.Core;
using System;

/*
    Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
    API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

namespace Arteranos.Services
{
    public interface IArteranosNetworkManager
    {
        void SpawnObject(CTSObjectSpawn wos);
    }
}
