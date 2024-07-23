/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Arteranos.Services
{
    public class TransitionProgressRunner : MonoBehaviour, ITransitionProgress
    {

        public GameObject[] ProgressBarObjects = null;
        public TMP_Text ProgressNotificationOb = null;

        public string ProgressNotification { 
            get => ProgressNotificationOb.text;
            private set => ProgressNotificationOb.text = value;
        }

        private void Awake()
        {
            G.TransitionProgress = this;
        }

        private void Start()
        {
            foreach(var progress in ProgressBarObjects) progress.SetActive(false);
        }

        private void OnDestroy()
        {
            G.TransitionProgress = null;
        }

        // async safe
        public void OnProgressChanged(float progress, string progressText)
        {
            IEnumerator ProgessCoroutine(float progress, string progressText)
            {
                // Even if there are three bars on your smartphone,
                // there is a fourth state -- zero bars.
                int lit = (int)(progress * (ProgressBarObjects.Length + 1));
                for (int i = 0; i < ProgressBarObjects.Length; i++)
                    ProgressBarObjects[i].SetActive(i < lit);

                ProgressNotification = progressText;

                yield return null;
            }

            TaskScheduler.ScheduleCoroutine(() => ProgessCoroutine(progress, progressText));
        }

    }
}
