using System.Collections;
using System;
using Arteranos.Core;
using Ipfs;
using Arteranos.Avatar;

namespace Arteranos.PlayTest
{
    public class StartupManagerMock : SettingsManager
    {
        protected override event Action<UserID, ServerUserState> OnClientReceivedServerUserStateAnswer_;
        protected override event Action<ServerJSON> OnClientReceivedServerConfigAnswer_;

        protected override void Awake()
        {
            Instance = this;

            base.Awake();
        }

        protected override void EmitToClientCTSPacket_(CTSPacket packet, IAvatarBrain to = null)
        {
            throw new NotImplementedException();
        }

        protected override void EmitToServerCTSPacket_(CTSPacket packet)
        {
            throw new NotImplementedException();
        }

        protected override void OnDestroy()
        {
            Instance = null;
        }

        protected override void StartCoroutineAsync_(Func<IEnumerator> action)
        {
            return;
        }
    }


}
