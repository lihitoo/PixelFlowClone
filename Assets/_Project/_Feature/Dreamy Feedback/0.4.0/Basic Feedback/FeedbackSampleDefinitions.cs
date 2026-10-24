using UnityEngine;
namespace Dreamy.Feedback.Samples
{
    public sealed class FeedbackSampleDefinitions : MonoBehaviour
    {
        [SerializeField] private FeedbackDefinition reward;
        [SerializeField] private FeedbackDefinition starReward;
        [SerializeField] private FeedbackDefinition energyReward;
        [SerializeField] private FeedbackDefinition gemReward;
        [SerializeField] private FeedbackDefinition hit;
        [SerializeField] private FeedbackDefinition buttonClick;
        [SerializeField] private Transform iconSource;
        [SerializeField] private Transform iconTarget;
        public FeedbackDefinition Reward => reward;
        public FeedbackDefinition StarReward => starReward;
        public FeedbackDefinition EnergyReward => energyReward;
        public FeedbackDefinition GemReward => gemReward;
        public FeedbackDefinition Hit => hit;
        public FeedbackDefinition ButtonClick => buttonClick;
        public Transform IconSource => iconSource;
        public Transform IconTarget => iconTarget;
    }
}
