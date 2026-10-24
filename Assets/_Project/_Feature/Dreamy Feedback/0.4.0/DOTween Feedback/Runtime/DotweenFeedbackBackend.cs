using System;
using DG.Tweening;
namespace Dreamy.Feedback.Dotween
{
    public sealed class DotweenFeedbackBackend : IFeedbackAnimationBackend
    {
        public FeedbackHandle Animate(float duration, bool unscaledTime, FeedbackEase ease, Action<float> update)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration)) return default;
            var state = new FeedbackPlayback();
            Tween tween = null;
            state.Bind(() => tween?.Kill(false), () => { tween?.Complete(true); tween?.Kill(false); });
            try
            {
                tween = DOVirtual.Float(0,1,UnityEngine.Mathf.Max(.001f,duration),value =>
                {
                    try { update(value); }
                    catch(Exception e) { state.Fail(); UnityEngine.Debug.LogException(e); }
                }).SetEase(Convert(ease)).SetUpdate(unscaledTime).SetAutoKill(true)
                    .OnComplete(()=>state.Finish()).OnKill(()=> { if(state.Status==FeedbackStatus.Running) state.Stop(); });
            }
            catch { state.Fail(); throw; }
            return state.Handle;
        }
        internal static Ease Convert(FeedbackEase ease)
        {
            switch(ease)
            {
                case FeedbackEase.OutCubic: return Ease.OutCubic;
                case FeedbackEase.InCubic: return Ease.InCubic;
                case FeedbackEase.InOutSine: return Ease.InOutSine;
                case FeedbackEase.OutBack: return Ease.OutBack;
                default: return Ease.Linear;
            }
        }
    }
}
