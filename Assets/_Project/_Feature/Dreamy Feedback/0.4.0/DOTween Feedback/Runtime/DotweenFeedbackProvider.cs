namespace Dreamy.Feedback.Dotween
{
    public sealed class DotweenFeedbackProvider : FeedbackAnimationProvider
    {
        public override IFeedbackAnimationBackend CreateBackend() => new DotweenFeedbackBackend();
        public override IIconFlyAnimator CreateIconAnimator(IFeedbackAnimationBackend backend) => new DotweenIconFlyAnimator();
    }
}
