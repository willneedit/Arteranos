using System.Collections;
using System;
using Arteranos.Core;
using Ipfs;

namespace Arteranos.PlayTest
{
    public class StartupManagerMock : SettingsManager
    {
        protected override void Awake()
        {
            Instance = this;

            base.Awake();
        }

        protected override bool IsSelf_(MultiHash ServerPeerID)
        {
            throw new NotImplementedException();
        }

        protected override void OnDestroy()
        {
            Instance = null;
        }

        protected override void PingServerChangeWorld_(string invoker, Cid cid)
        {
            throw new NotImplementedException();
        }

        protected override void StartCoroutineAsync_(Func<IEnumerator> action)
        {
            return;
        }
    }


}
