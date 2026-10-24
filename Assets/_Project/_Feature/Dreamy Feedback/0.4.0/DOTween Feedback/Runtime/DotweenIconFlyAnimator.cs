using DG.Tweening;
using UnityEngine;
namespace Dreamy.Feedback.Dotween
{
    /// <summary>Per-icon DOTween sequence: stagger, optional scatter phase, collection phase.
    /// Sequence/Tween are owned here and never returned through gameplay APIs.</summary>
    public sealed class DotweenIconFlyAnimator : IIconFlyAnimator
    {
        public FeedbackHandle Animate(Transform icon, CanvasGroup opacity, IconFlyOptions options, Vector3 spread, int index)
        {
            var state=new FeedbackPlayback();
            var sequence=DOTween.Sequence().SetUpdate(options.UnscaledTime).SetAutoKill(true);
            state.Bind(()=>sequence.Kill(false),()=> { sequence.Complete(true); sequence.Kill(false); });
            sequence.AppendInterval(Mathf.Max(0,options.Stagger)*index);
            float duration=Mathf.Max(.01f,options.Duration);
            // Scatter has its own tween followed by an accelerating magnetic collect tween.
            if(options.Style==IconFlyStyle.ScatterMagnet)
            {
                sequence.Append(DOVirtual.Float(0,.3f,duration*.3f,t=>Apply(t)).SetEase(Ease.Linear));
                sequence.Append(DOVirtual.Float(.3f,1,duration*.7f,t=>Apply(t)).SetEase(Ease.Linear));
            }
            else
                sequence.Append(DOVirtual.Float(0,1,duration,t=>Apply(t)).SetEase(Ease.Linear));
            sequence.OnComplete(()=>state.Finish()).OnKill(()=> { if(state.Status==FeedbackStatus.Running) state.Stop(); });
            return state.Handle;

            void Apply(float t)
            {
                if(!icon || !icon.gameObject.activeInHierarchy) { state.Stop(); return; }
                try { IconFlyMotion.Apply(icon,opacity,options,spread,t); }
                catch(System.Exception e) { state.Fail(); Debug.LogException(e); }
            }
        }
    }
}
