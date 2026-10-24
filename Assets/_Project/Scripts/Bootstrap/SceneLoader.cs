using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dreamy.Template
{
    public sealed class SceneLoader : LiveSingleton<SceneLoader>
    {
        [SerializeField] private UILoadingScreen loadingScreen;
        [SerializeField] private float minimumLoadingDuration = 1.5f;

        // Subscriptions survive loads. Subscribers must unsubscribe on their own teardown.
        public event Action OnScenePreloading;
        public event Action<float> OnSceneLoading;
        public event Action OnSceneLoaded;
        public event Action OnLastSceneHidden;
        public event Action OnScenePresented;

        private bool _isLoading;
        public bool IsPresenting => _isLoading || (loadingScreen != null && loadingScreen.gameObject.activeInHierarchy);

        /// <summary>
        /// Cancellation stops presentation, not Unity's scene operation. A started load
        /// is allowed to activate and drained before another request can start.
        /// </summary>
        public async UniTask LoadScene(string sceneName, CancellationToken cancellationToken = default)
        {
            if (_isLoading)
            {
                throw new InvalidOperationException("SceneLoader is already loading a scene.");
            }

            if (string.IsNullOrWhiteSpace(sceneName)) throw new ArgumentException("Scene name is required.", nameof(sceneName));
            if (loadingScreen == null) throw new InvalidOperationException("Assign a loading screen before loading scenes.");

            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy(), cancellationToken);
            CancellationToken token = lifetime.Token;
            token.ThrowIfCancellationRequested();
            _isLoading = true;
            AsyncOperation loadOperation = null;
            bool presented = false;

            try
            {
                InitializeLoading();

                OnScenePreloading?.Invoke();
                token.ThrowIfCancellationRequested();

                loadOperation = SceneManager.LoadSceneAsync(
                    sceneName,
                    LoadSceneMode.Single);

                if (loadOperation == null) throw new InvalidOperationException($"Could not load scene '{sceneName}'.");
                loadOperation.allowSceneActivation = false;

                float elapsedTime = 0f;

                while (loadOperation.progress < 0.9f)
                {
                    elapsedTime += Time.unscaledDeltaTime;

                    float progress =
                        (loadOperation.progress / 0.9f) * 0.8f;

                    ReportProgress(progress);

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                token.ThrowIfCancellationRequested();
                OnSceneLoaded?.Invoke();
                OnLastSceneHidden?.Invoke();

                while (elapsedTime < minimumLoadingDuration)
                {
                    elapsedTime += Time.unscaledDeltaTime;

                    float t = Mathf.Clamp01(
                        elapsedTime / minimumLoadingDuration);

                    ReportProgress(Mathf.Lerp(0.8f, 1f, t));

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                token.ThrowIfCancellationRequested();
                ReportProgress(1f);

                loadOperation.allowSceneActivation = true;

                await UniTask.WaitUntil(
                    () => loadOperation.isDone, cancellationToken: token);

                await UniTask.Delay(
                    300,
                    ignoreTimeScale: true, cancellationToken: token);

                loadingScreen.Hide();

                OnScenePresented?.Invoke();
                presented = true;
            }
            finally
            {
                try
                {
                    if (loadOperation != null && !loadOperation.isDone)
                    {
                        // Unity cannot cancel a scene load. Never leave its queue blocked
                        // at 0.9, even if an event callback throws or the owner is destroyed.
                        loadOperation.allowSceneActivation = true;
                        await UniTask.WaitUntil(() => loadOperation.isDone);
                    }
                }
                finally
                {
                    _isLoading = false;
                    if (!presented && loadingScreen != null) loadingScreen.Deactive();
                }
            }
        }

        private void InitializeLoading()
        {
            loadingScreen.Show();
            loadingScreen.SetProgress(0f);
        }

        private void ReportProgress(float progress)
        {
            OnSceneLoading?.Invoke(progress);
            loadingScreen.SetProgress(progress);
        }

        protected override void OnDestroy()
        {
            OnScenePreloading = null;
            OnSceneLoading = null;
            OnSceneLoaded = null;
            OnLastSceneHidden = null;
            OnScenePresented = null;
            base.OnDestroy();
        }
    }
}
