using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Dreamy.Feedback.Samples
{
    [Serializable]
    public sealed class FeedbackDemoEntry
    {
        public string Label;
        public FeedbackEffectAsset Effect;
        public FeedbackDefinition Definition;
        public bool SelectedReward;
        public FeedbackEffectOverrides Overrides;
        public float Intensity = 1;
    }

    [Serializable]
    public sealed class FeedbackDefinitionBindings
    {
        public FeedbackDefinition Definition;
        public FeedbackStepBinding[] Steps;
    }
    public sealed class FeedbackDemoControls : MonoBehaviour
    {
        [SerializeField] private FeedbackHost host;
        [SerializeField] private Transform target;
        [SerializeField] private RectTransform iconStart, iconEnd;
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite[] rewardIcons;
        [SerializeField] private FeedbackDefinition[] rewardDefinitions;
        [SerializeField] private Button[] rewardButtons;
        [SerializeField] private FeedbackDefinitionBindings[] definitionBindings = Array.Empty<FeedbackDefinitionBindings>();
        [SerializeField] private FeedbackDemoEntry[] entries = Array.Empty<FeedbackDemoEntry>();
        [SerializeField] private Button[] channelButtons;
        [SerializeField] private Button playAllButton, stopButton, completeButton;
        [SerializeField] private Button uiPreviewButton;
        [SerializeField] private VfxFeedbackAsset uiVfxEffect;
        [SerializeField] private FloatingTextFeedbackAsset uiTextEffect;
        [SerializeField] private Toggle hapticToggle;
        [SerializeField] private TMP_Text statusText;
        private UnityEngine.Events.UnityAction[] rewardListeners, listeners;
        private readonly List<FeedbackHandle> handles = new List<FeedbackHandle>();
        private FeedbackHandle observed;
        private string observedLabel;
        private int selectedReward;
        public int ChannelCount => channelButtons.Length;
        public int PlayCount { get; private set; }
        public FeedbackHandle LastHandle { get; private set; }
        public string Status => statusText ? statusText.text : "";
        public void ConfigureEntries(FeedbackDemoEntry[] value) => entries = value;
        public void ConfigureBindings(FeedbackDefinition definition, FeedbackStepBinding[] steps)
        {
            var values = new List<FeedbackDefinitionBindings>(definitionBindings);
            values.RemoveAll(v => v.Definition == definition);
            values.Add(new FeedbackDefinitionBindings { Definition = definition, Steps = steps }); definitionBindings = values.ToArray();
        }
        private FeedbackStepBinding[] BindingsFor(FeedbackDefinition definition)
        { foreach (var value in definitionBindings) if (value.Definition == definition) return value.Steps; return null; }
        private void OnEnable()
        {
            host.Initialize(); listeners = new UnityEngine.Events.UnityAction[channelButtons.Length];
            for (int i = 0; i < channelButtons.Length; i++)
            { int channel = i; listeners[i] = () => PlayChannel(channel); if (channelButtons[i]) channelButtons[i].onClick.AddListener(listeners[i]); }
            rewardListeners = new UnityEngine.Events.UnityAction[rewardButtons.Length];
            for (int i = 0; i < rewardButtons.Length; i++)
            { int reward = i; rewardListeners[i] = () => PlayReward(reward); if (rewardButtons[i]) rewardButtons[i].onClick.AddListener(rewardListeners[i]); }
            if (playAllButton) playAllButton.onClick.AddListener(PlayAll);
            if (stopButton) stopButton.onClick.AddListener(StopAll);
            if (completeButton) completeButton.onClick.AddListener(CompleteAll);
            if (uiPreviewButton) uiPreviewButton.onClick.AddListener(PlayUiPreview);
            if (hapticToggle) { hapticToggle.onValueChanged.AddListener(SetHaptic); SetHaptic(hapticToggle.isOn); }
        }
        public void PlayChannel(int channel)
        {
            if (channel < 0 || channel >= entries.Length || entries[channel] == null)
            { Unavailable("import the updated Basic Feedback sample presets."); return; }
            var entry = entries[channel];
            var definition = entry.SelectedReward ? SelectedReward() : entry.Definition;
            if (!definition && !entry.Effect) { Unavailable("missing preset for " + entry.Label); return; }
            var builder = definition ? FeedbackRequestBuilder.For(definition) : FeedbackRequestBuilder.For(entry.Effect);
            var request = builder.From(this).At(target ? target.position : transform.position).To(iconEnd).IconsFrom(iconStart)
                .WithIcon(icon).WithAmount(100).WithIntensity(entry.Intensity).WithOverrides(entry.Overrides).WithBindings(BindingsFor(definition)).Build();
            string error = Check(request);
            if (error != null) { Unavailable(error); return; }
            Track(host.Feedback.Play(request), entry.Label);
            if (entry.Effect && entry.Effect.Type == FeedbackNodeType.Haptic)
            { observed = default; SetStatus(HapticStatus()); }
        }
        public void PlayReward(int index)
        {
            if (index < 0 || index >= rewardDefinitions.Length || index >= rewardIcons.Length) return;
            selectedReward = index; icon = rewardIcons[index];
            var image = iconEnd ? iconEnd.GetComponent<Image>() : null; if (image) image.sprite = icon;
            var definition = SelectedReward(); if (!definition) { Unavailable("missing reward definition"); return; }
            var request = FeedbackRequestBuilder.For(definition).From(this).At(target ? target.position : transform.position)
                .To(iconEnd).IconsFrom(iconStart).WithIcon(icon).WithAmount(100).WithBindings(BindingsFor(definition)).Build();
            string error = Check(request);
            if (error != null) { Unavailable(error); return; }
            Track(host.Feedback.Play(request), definition.name);
        }
        public void PlayUiPreview()
        {
            if (!iconStart || !uiVfxEffect || !uiTextEffect)
            { Unavailable("UI anchor or preview presets missing"); return; }
            var vfx = FeedbackRequestBuilder.For(uiVfxEffect).From(this).AtUi(iconStart).Build();
            var text = FeedbackRequestBuilder.For(uiTextEffect).From(this).AtUi(iconStart).WithAmount(321).Build();
            var error = Check(vfx) ?? Check(text);
            if (error != null) { Unavailable(error); return; }
            Track(host.Feedback.Play(vfx), "UI VFX");
            Track(host.Feedback.Play(text), "UI reward text");
        }
        private FeedbackDefinition SelectedReward() => selectedReward < rewardDefinitions.Length ? rewardDefinitions[selectedReward] : null;
        private string Check(FeedbackRequest request)
        {
            try
            {
                var node = FeedbackGraphResolver.Resolve(request.Definition ? request.Definition.Root : FeedbackNode.From(request.Effect), request.Overrides);
                var error = FeedbackGraphValidation.ValidateNode(node, false);
                if (error != null) return error;
                return CheckNode(node);
            }
            catch (Exception e) { return e.Message; }
        }
        private string CheckNode(FeedbackNode node)
        {
            switch (node.Type)
            {
                case FeedbackNodeType.Vfx: if (host.Vfx == null) return "VFX service missing"; break;
                case FeedbackNodeType.FloatingText:
                    if (host.FloatingText == null || node.DirectAsset && !node.TextSettings.Prefab.GetComponent<FloatingTextInstance>()) return "text prefab/service missing";
                    break;
                case FeedbackNodeType.CameraShake: if (host.CameraShake == null) return "bind a game camera"; break;
                case FeedbackNodeType.UiPunch: if (host.Ui == null || !iconEnd) return "HUD target missing"; break;
                case FeedbackNodeType.IconFly: if (host.IconFly == null || !iconStart || !iconEnd || !icon) return "icon/HUD anchors missing"; break;
                case FeedbackNodeType.Haptic: if (host.Haptic == null) return "haptics disabled on this rig"; break;
                case FeedbackNodeType.ScreenFlash: if (host.Screen == null) return "screen feedback disabled on this rig"; break;
            }
            if (node.Children != null) foreach (var child in node.Children) { var error = CheckNode(child); if (error != null) return error; }
            return null;
        }
        private void Track(FeedbackHandle handle, string label)
        {
            handles.RemoveAll(item => !item.IsRunning);
            if (handle.IsValid) handles.Add(handle);
            LastHandle = handle; PlayCount++;
            observed = handle; observedLabel = label;
            SetStatus(handle.IsValid ? handle.Status + ": " + label : "Unavailable: " + label);
        }
        private void Update()
        {
            if (!observed.IsValid || observed.IsRunning) return;
            SetStatus(observed.Status + ": " + observedLabel); observed = default;
        }
        public void PlayAll()
        {
            int unavailable = 0;
            for (int i = 0; i < ChannelCount; i++) { PlayChannel(i); if (Status.StartsWith("Unavailable") || LastHandle.Status == FeedbackStatus.Faulted) unavailable++; }
            observed = default; SetStatus(unavailable > 0 ? "Unavailable: " + unavailable + " previews could not play"
                : "Running multiple previews; shared punch/flash/shake channels replace earlier plays.");
        }
        public void CompleteAll()
        { observed = default; foreach (var h in handles.ToArray()) h.Complete(); handles.Clear(); SetStatus("Completed owned feedback"); }
        public void StopAll()
        { observed = default; foreach (var h in handles.ToArray()) h.Stop(); handles.Clear(); host.StopAll(); SetStatus("Stopped / cleared"); }
        private void SetHaptic(bool value)
        { if (host.Haptic != null) host.Haptic.Enabled = value; observed = default; SetStatus(HapticStatus()); }
        private string HapticStatus()
        {
            if (host.Haptic == null || !host.Haptic.Enabled) return "Haptics disabled";
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            return "Haptic requested (device vibration fallback)";
#else
            return "Haptics unsupported in Editor/desktop; test vibration on a mobile device";
#endif
        }
        private void SetStatus(string text) { if (statusText) statusText.text = text; }
        private void Unavailable(string reason) { observed = default; LastHandle = default; SetStatus("Unavailable: " + reason); }
        private void OnDisable()
        {
            if (rewardListeners != null) for (int i = 0; i < rewardListeners.Length; i++) if (rewardButtons[i]) rewardButtons[i].onClick.RemoveListener(rewardListeners[i]);
            if (listeners != null) for (int i = 0; i < listeners.Length; i++) if (channelButtons[i]) channelButtons[i].onClick.RemoveListener(listeners[i]);
            if (playAllButton) playAllButton.onClick.RemoveListener(PlayAll);
            if (stopButton) stopButton.onClick.RemoveListener(StopAll);
            if (completeButton) completeButton.onClick.RemoveListener(CompleteAll);
            if (uiPreviewButton) uiPreviewButton.onClick.RemoveListener(PlayUiPreview);
            if (hapticToggle) hapticToggle.onValueChanged.RemoveListener(SetHaptic);
            foreach (var h in handles.ToArray()) h.Stop(); handles.Clear(); observed = default;
        }
    }
}
