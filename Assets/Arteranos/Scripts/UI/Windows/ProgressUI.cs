/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;
using System.Threading.Tasks;

namespace Arteranos.UI
{
    public class ProgressUI : UIBehaviour, IProgressUI
    {
        public CanvasRenderer Panel = null;

        // How many seconds to start to show the progress dialog.
        public float PatienceThreshold = 2.0f;

        // But, if the operation would last less than these seconds, it'd be no sense to show it up.
        public float AlmostFinishedThreshold = 1.0f;

        // Tip of the day. null if there is no tip to show.
        public string Tip = null;

        // Allow canceling the process.
        private bool allowCancel = false;

        public event Action<Context> Completed;
        public event Action<Exception, Context> Faulted;

        private AsyncOperationExecutor<Context> executor = null;
        private Context context = null;

        private DateTime startTime = DateTime.MinValue;

        private TMP_Text txt_caption = null;
        private Slider sld_progress = null;
        private TMP_Text txt_tip = null;
        private GameObject go_buttonArea = null;
        private Button btn_cancelButton = null;

        public bool AllowCancel { get => allowCancel; set => allowCancel = value; }
        public AsyncOperationExecutor<Context> Executor { get => executor; set => executor = value; }
        public Context Context { get => context; set => context = value; }

        protected override void Awake()
        {
            base.Awake();

            Transform t = Panel.transform;

            txt_caption = t.GetChild(0).GetComponent<TMP_Text>();
            sld_progress = t.GetChild(1).GetComponent<Slider>();
            txt_tip = t.GetChild(2).GetComponent<TMP_Text>();
            go_buttonArea = t.GetChild(3).gameObject;
            btn_cancelButton = go_buttonArea.transform.GetChild(1).GetComponent<Button>();

            // Invisible. For now.
            Panel.gameObject.SetActive(false);
        }

        protected override void Start()
        {
            IEnumerator ExecuteCoroutine()
            {
                AggregateException status;
                yield return Executor.ExecuteCoroutine(Context, (_status, _context) =>
                {
                    status = _status;
                    Context = _context;

                    if (status != null)
                        Faulted?.Invoke(status, Context);
                });

                Destroy(gameObject);
            }

            base.Start();

            startTime = DateTime.Now;

            if(string.IsNullOrEmpty(Tip)) txt_tip.gameObject.SetActive(false);
            else txt_tip.text = Tip;

            if(!AllowCancel) go_buttonArea.SetActive(false);

            if(Executor == null)
                throw new NullReferenceException("No executor for this Progress Dialog.");

            if(Context == null)
                throw new NullReferenceException("No context for this Progress Dialog.");

            btn_cancelButton.onClick.AddListener(OnCancelButtonClicked);

            Executor.ProgressChanged += OnProgressChanged;
            Executor.Completed += OnCompleted;

            StartCoroutine(ExecuteCoroutine());
        }

        protected void Update()
        {
            TimeSpan elapsed = DateTime.Now - startTime;


            if(!Panel.gameObject.activeSelf)
            {
                // Not visible and too soon yet.
                if(elapsed.TotalSeconds < PatienceThreshold) return;

                // Too long for having no progress whatsoever since the very start.
                if(sld_progress.value == 0)
                {
                    Panel.gameObject.SetActive(true);
                    return;
                }

                TimeSpan projected = elapsed / sld_progress.value;
                TimeSpan remaining = projected - elapsed;

                // Not visible and it could be any second now.
                if(remaining.TotalSeconds < AlmostFinishedThreshold) return;

                // Then, it could be still taking a while...
                Panel.gameObject.SetActive(true);
            }
        }

        public void SetupAsyncOperations(Func<(AsyncOperationExecutor<Context>, Context)> setupFunc, bool cancelable = true, string tip = null)
        {
            (Executor, Context) = setupFunc();
            AllowCancel = cancelable;
            Tip = tip;
        }

        private void OnProgressChanged(float progress, string caption)
        {
            sld_progress.value= progress;
            txt_caption.text= caption;
        }

        private void OnCompleted(Context context) => Completed?.Invoke(context);

        private void OnCancelButtonClicked() => Executor.Cancel();

        public void SetupResultCallbacks(Action<Context> success, Action<Exception, Context> failure)
        {
            Completed += success;
            Faulted += failure;
        }
    }
}
