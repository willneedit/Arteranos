using System.Threading;
using UnityEngine;

namespace ReadyPlayerMe
{
    public class CameraZoom : MonoBehaviour
    {
        [SerializeField] private Transform nearTransform;
        [SerializeField] private Transform halfBodyTransform;
        [SerializeField] private Transform farTransform;
        [SerializeField] private float defaultDuration = 0.25f;

        private CancellationTokenSource ctx;

        private void OnDestroy()
        {
            ctx?.Cancel();
        }

        public void MoveToNear()
        {
            ctx?.Cancel();
            ctx = new CancellationTokenSource();
            _ = Camera.main.transform.LerpPosition(nearTransform.position, defaultDuration, ctx.Token);
        }

        public void MoveToFar()
        {
            ctx?.Cancel();
            ctx = new CancellationTokenSource();
            _ = Camera.main.transform.LerpPosition(farTransform.position, defaultDuration, ctx.Token);
        }

        public void MoveToHalfBody()
        {
            Camera.main.transform.position = halfBodyTransform.transform.position;
        }
    }
}
