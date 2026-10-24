using System;
using System.Threading;
using Dreamy.Audio;
using Dreamy.Core;
using Dreamy.DataConfig;
using Dreamy.Datasave;
using Dreamy.Feedback;
using Dreamy.UI;

namespace Dreamy.Template
{
    /// <summary>Optional game composition; the template remains the single root.</summary>
    public interface IGameCompositionProfile
    {
        DatasaveOptions CreateSaveOptions(bool prettyPrint);
        IDataConfigSource CreateConfigSource();
        void RegisterConfiguration(IDataConfigService configuration);
        void Install(GameCompositionContext context);
    }

    /// <summary>Explicit game dependencies with the root's existing ownership scope.</summary>
    public sealed class GameCompositionContext
    {
        private readonly GameInstaller owner;
        public IDatasaveService Saves { get; }
        public IDataConfigService Configuration { get; }
        public FeedbackHost Feedback { get; }
        public PanelPresenterFactory Presenters { get; }
        public CancellationToken Lifetime { get; }
        internal GameCompositionContext(GameInstaller owner, IDatasaveService saves, IDataConfigService configuration,
            FeedbackHost feedback, PanelPresenterFactory presenters, CancellationToken lifetime)
        { this.owner = owner; Saves = saves; Configuration = configuration; Feedback = feedback; Presenters = presenters; Lifetime = lifetime; }
        public void Register<T>(T service, bool ownsInstance = false) where T : class => owner.RegisterGameService(service, ownsInstance);
        public void OnTeardown(Action action) => owner.TrackGameTeardown(action ?? throw new ArgumentNullException(nameof(action)));
    }
}
