using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Dreamy.Feedback.Dotween.Tests
{
    public sealed class DotweenFeedbackTests
    {
        [Test] public void CompleteAndStopAreExactlyOnce()
        {
            int ends=0;var backend=new DotweenFeedbackBackend();
            var h=backend.Animate(10,true,FeedbackEase.Linear,t=>{if(t==1)ends++;});
            h.Complete();h.Complete();h.Stop();Assert.That(h.Status,Is.EqualTo(FeedbackStatus.Completed));Assert.That(ends,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator AllFiveIconPathsCompleteAndReleaseTheirLeases()
        {
            var root=new GameObject("Icons",typeof(RectTransform));var service=new IconFlyService();service.Initialize(root.transform,new DotweenIconFlyAnimator());
            try
            {
                foreach(IconFlyStyle style in System.Enum.GetValues(typeof(IconFlyStyle)))
                {
                    var options=new IconFlyOptions {Style=style,Count=4,Duration=.04f,Stagger=.005f,ArcHeight=80,Spread=30,EndPosition=Vector3.one*100,UnscaledTime=true};
                    var h=service.Fly(options);yield return new WaitForSecondsRealtime(.12f);
                    Assert.That(h.Status,Is.EqualTo(FeedbackStatus.Completed),style.ToString());
                    foreach(Transform icon in root.transform) Assert.That(icon.gameObject.activeSelf,Is.False);
                }
                var a=service.Fly(new IconFlyOptions {Count=4,Duration=10});a.Complete();
                var b=service.Fly(new IconFlyOptions {Count=4,Duration=10});a.Stop();yield return null;
                Assert.That(b.IsRunning,Is.True);Assert.That(root.transform.childCount,Is.EqualTo(4));b.Stop();
            }
            finally {service.Dispose();Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator DestroyedDestinationStopsIconGroup()
        {
            var root=new GameObject("Icons",typeof(RectTransform));var target=new GameObject("Destination",typeof(RectTransform));
            var service=new IconFlyService();service.Initialize(root.transform,new DotweenIconFlyAnimator());
            var h=service.Fly(new IconFlyOptions {Count=3,Duration=10,Target=target.transform});Object.Destroy(target);
            yield return null;yield return null;Assert.That(h.Status,Is.EqualTo(FeedbackStatus.Stopped));service.Dispose();Object.Destroy(root);
        }
    }
}
