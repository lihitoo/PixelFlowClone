using System;
using System.Collections.Generic;
using System.IO;
using Dreamy.Audio;
using Dreamy.Feedback;
using Dreamy.Template;
using Dreamy.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PixelFlow.Editor
{
    public static partial class PixelFlowFoundationAuthoring
    {
        private const string Root = "Assets/_Project/PixelFlow";
        private const string AudioRoot = Root + "/Generated/Audio";
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private static T Asset<T>(string path) where T : ScriptableObject
        {
            var value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value == null) { value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); }
            return value;
        }
        private static void File(AudioFileObject value, string key, AudioClip clip, float gain, bool music)
        {
            if (clip == null) throw new InvalidOperationException("Missing source clip: " + key);
            var so = new SerializedObject(value);
            so.FindProperty("key").stringValue = key; so.FindProperty("bus").stringValue = music ? "music" : "sfx";
            so.FindProperty("volume").floatValue = gain; so.FindProperty("randomPitch").floatValue = 0;
            so.FindProperty("eventType").enumValueIndex = (int)(music ? AudioEventType.Music : AudioEventType.OneShot);
            var clips = so.FindProperty("clips"); clips.arraySize = 1; clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(value);
        }
        [MenuItem("Pixel Flow/Foundation/Author Audio Profile")]
        public static void AuthorAudio()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            EnsureFolder(AudioRoot);
            var art = AssetDatabase.LoadAssetAtPath<PixelFlowPresentation>(Root + "/Generated/PixelFlowPresentation.asset");
            if (art == null) throw new InvalidOperationException("Missing PixelFlow presentation.");
            var cues = Asset<PixelFlowAudioCues>(AudioRoot + "/PixelFlowAudioCues.asset");
            var library = Asset<AudioLibrary>(AudioRoot + "/PixelFlowAudioLibrary.asset");
            var profile = Asset<DreamyAudioProfile>(AudioRoot + "/PixelFlowAudioProfile.asset");
            var sounds = new List<SoundAudioFile>(); cues.Sounds.Clear();
            void Sound(string key, AudioClip clip, float volume)
            {
                var file = Asset<SoundAudioFile>(AudioRoot + "/" + key + ".asset"); File(file, key, clip, volume, false);
                sounds.Add(file); cues.Sounds.Add(new PixelFlowAudioCues.SoundCue { Source = clip, GainHint = volume, File = file });
            }
            Sound("ui-tap", art.Tap, .55f); Sound("pig-launch", art.Launch, .5f); Sound("booster-launch", art.Launch, .55f);
            Sound("pixel-pop", art.Pop, .35f); Sound("level-win", art.Win, .65f);
            Sound("feature", Resources.Load<AudioClip>("PixelFlow/Audio/Feature"), .35f);
            cues.MenuMusic = Asset<MusicAudioFile>(AudioRoot + "/menu-music.asset");
            cues.GameMusic = Asset<MusicAudioFile>(AudioRoot + "/game-music.asset");
            File(cues.MenuMusic, "menu-music", Resources.Load<AudioClip>("PixelFlow/Audio/MenuMusic"), .22f, true);
            File(cues.GameMusic, "game-music", Resources.Load<AudioClip>("PixelFlow/Audio/LevelMusic"), .22f, true);
            var librarySo = new SerializedObject(library); librarySo.FindProperty("libraryId").stringValue = "pixel-flow";
            var soundList = librarySo.FindProperty("sounds"); soundList.arraySize = sounds.Count;
            for (int i = 0; i < sounds.Count; i++) soundList.GetArrayElementAtIndex(i).objectReferenceValue = sounds[i];
            var musicList = librarySo.FindProperty("music"); musicList.arraySize = 2;
            musicList.GetArrayElementAtIndex(0).objectReferenceValue = cues.MenuMusic; musicList.GetArrayElementAtIndex(1).objectReferenceValue = cues.GameMusic;
            librarySo.ApplyModifiedPropertiesWithoutUndo();
            var profileSo = new SerializedObject(profile); var libraries = profileSo.FindProperty("libraries"); libraries.arraySize = 1; libraries.GetArrayElementAtIndex(0).objectReferenceValue = library;
            profileSo.FindProperty("initialPoolSize").intValue = 8; profileSo.FindProperty("maxPoolSize").intValue = 16;
            profileSo.ApplyModifiedPropertiesWithoutUndo(); cues.Profile = profile;
            EditorUtility.SetDirty(cues); EditorUtility.SetDirty(library); EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
        }
        // Startup authoring lives in PixelFlowFoundationAuthoring.Scenes.cs.
    }
}
