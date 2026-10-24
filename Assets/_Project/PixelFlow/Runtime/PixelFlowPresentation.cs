using TMPro;
using UnityEngine;

namespace PixelFlow
{
    [CreateAssetMenu(menuName = "Pixel Flow/Presentation")]
    public sealed class PixelFlowPresentation : ScriptableObject
    {
        public Mesh PigMesh;
        public Material LitMaterial;
        public Texture2D PigTexture;
        public TMP_FontAsset Font;
        public Sprite Logo;
        public Sprite HeroPig;
        public Sprite PlayButton;
        public Sprite BlueButton;
        public AudioClip Tap;
        public AudioClip Pop;
        public AudioClip Launch;
        public AudioClip Win;
        public Color Background = new(.19215687f, .3019608f, .4745098f);
    }
}
