using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamy.Template
{
    public class UILoadingScreen : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private Animator animator;

        [SerializeField] private float progressAnimDuration = 0.3f;
        [SerializeField] private int loadingTextDelayMs = 300;

        private float currentPercent;
        private CancellationTokenSource cts;
        private DG.Tweening.Tween progressTween;

        private readonly string[] loadingTexts = { "Loading", "Loading.", "Loading..", "Loading..." };

        private void Awake()
        {
            progressBarFill.fillAmount = 0;
        }

        public void Show()
        {
            gameObject.SetActive(true);

            CancelLoadingTask();

            cts = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy()
            );

            ShowLoading(cts.Token).Forget();

            currentPercent = 0;
            progressBarFill.fillAmount = 0;
            progressText.text = "0%";
        }

        public void Hide()
        {
            CancelLoadingTask();
            animator.SetTrigger("Close");
        }

        public void Deactive()
        {
            CancelLoadingTask();
            gameObject.SetActive(false);
        }

        private void CancelLoadingTask()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            progressTween?.Kill();

            progressTween = DOVirtual.Float(
                currentPercent,
                progress,
                progressAnimDuration,
                value =>
                {
                    currentPercent = value;
                    progressBarFill.fillAmount = value;
                    progressText.text = Mathf.RoundToInt(value * 100) + "%";
                }
            );
        }

        private async UniTask ShowLoading(CancellationToken token)
        {
            int index = 0;

            while (!token.IsCancellationRequested)
            {
                loadingText.text = loadingTexts[index];
                index = (index + 1) % loadingTexts.Length;

                await UniTask.Delay(loadingTextDelayMs, cancellationToken: token);
            }
        }

        private void OnDestroy()
        {
            CancelLoadingTask();
            progressTween?.Kill();
        }
    }
}