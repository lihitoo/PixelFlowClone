using System;
using System.Linq;
using Dreamy.Feature.Shop.Integration;
using Dreamy.Template.Home;
using PixelFlow.Rules;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PixelFlow.Editor
{
    public static class HomeShopAuthoring
    {
        public const string HomePath = "Assets/_Project/Prefabs/Panel/HomePanel.prefab";
        public const string ShopPath = "Assets/_Project/_Feature/Dreamy Shop/0.1.2/Shop Feature/Prefabs/ShopPanel.prefab";
        public const string PreviousShopPath = "Assets/_Project/PixelFlow/Prefabs/Foundation/PixelFlowShopPanel.prefab";
        private static T Find<T>(Transform root, string name) where T : Component => root.GetComponentsInChildren<T>(true).Single(x => x.name == name);
        private static void Ref(SerializedObject so, string name, UnityEngine.Object value) => so.FindProperty(name).objectReferenceValue = value;

        public static void Migrate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Expected stopped Editor and saved scene.");
            const string folder = "Assets/_Project/PixelFlow/Art/Fonts/";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(folder + "LilitaOne SDF.asset");
            if (font == null) throw new InvalidOperationException("Import original Lilita font first.");
            var importer = (TextureImporter)AssetImporter.GetAtPath(folder + "Lilita Atlas.png");
            importer.textureType = TextureImporterType.Default; importer.sRGBTexture = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048; importer.SaveAndReimport();
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "Lilita Atlas.png");
            var material = AssetDatabase.LoadAssetAtPath<Material>(folder + "Lilita Outline Shadow.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("TextMeshPro/Distance Field"));
                material.SetTexture("_MainTex", atlas); material.SetFloat("_TextureWidth", 1024); material.SetFloat("_TextureHeight", 1024); material.SetFloat("_GradientScale", 17);
                material.EnableKeyword("OUTLINE_ON"); material.SetFloat("_OutlineWidth", .16f); material.SetColor("_OutlineColor", new Color(.03f,.04f,.08f,1));
                material.EnableKeyword("UNDERLAY_ON"); material.SetColor("_UnderlayColor", new Color(0,0,0,.65f));
                material.SetFloat("_UnderlayOffsetX", 0); material.SetFloat("_UnderlayOffsetY", -.3f);
                AssetDatabase.CreateAsset(material, folder + "Lilita Outline Shadow.mat");
            }
            font.atlasTextures = new[] { atlas }; font.material = material; font.ReadFontAssetDefinition(); EditorUtility.SetDirty(font);
            MigrateHome(font, material); MigrateShop(font, material);
            var original = EditorSceneManager.GetActiveScene().path;
            var main = EditorSceneManager.OpenScene(PixelFlowFoundationAuthoring.MainScenePath);
            var scaler = main.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<CanvasScaler>(true)).Single(x => x.name == "UICanvas");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080,1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .5f;
            EditorSceneManager.SaveScene(main); EditorSceneManager.OpenScene(original);
            PixelFlowFoundationAuthoring.RegisterPanelAddress(HomePath, PixelFlowPanelAddress.Home);
            PixelFlowFoundationAuthoring.RegisterPanelAddress(ShopPath, PixelFlowPanelAddress.Shop); AssetDatabase.SaveAssets();
        }
        private static void MigrateHome(TMP_FontAsset font, Material material)
        {
            var root = PrefabUtility.LoadPrefabContents(HomePath);
            try
            {
                var panel = root.GetComponent<HomePanel>(); var so = new SerializedObject(panel);
                if (so.FindProperty("content").objectReferenceValue == null) ScaleAndStyle(root,font,material);
                Ref(so,"content",Find<RectTransform>(root.transform,"Home").gameObject);
                Ref(so,"chrome",Find<RectTransform>(root.transform,"Source Home Header and Tabs").gameObject);
                Ref(so,"backdrop",Find<RectTransform>(root.transform,"Original Home Backdrop").gameObject);
                Ref(so,"map",Find<ScrollRect>(root.transform,"Level Path Scroll"));
                string[] fields={"playButton","startButton","shopButton","rankButton","settingsButton","livesButton","coinsButton"};
                string[] roles={"Play","Home Tab 0","Home Tab 1","Home Tab 2","Settings","Hearts","Coins"};
                for(int i=0;i<fields.Length;i++) Ref(so,fields[i],Find<Button>(root.transform,roles[i]));
                string[] labels={"wallet","heartCount","heartTimer","rankLabel"}; string[] texts={"Wallet","Heart Count","Heart Timer","Tab Label 2"};
                for(int i=0;i<labels.Length;i++) Ref(so,labels[i],Find<TMP_Text>(root.transform,texts[i]));
                Ref(so,"heartIcon",Find<Image>(root.transform,"Original Heart")); Ref(so,"demoButton",null);
                var levels=so.FindProperty("levels"); levels.arraySize=10;
                for(int i=0;i<10;i++)
                {
                    var slot=levels.GetArrayElementAtIndex(i); var node=Find<Button>(root.transform,"Source Level Node "+i);
                    slot.FindPropertyRelative("Button").objectReferenceValue=node;
                    slot.FindPropertyRelative("Number").objectReferenceValue=Find<TMP_Text>(node.transform,"Level Number");
                    slot.FindPropertyRelative("Difficulty").objectReferenceValue=Find<TMP_Text>(node.transform,"Difficulty");
                    slot.FindPropertyRelative("Label").objectReferenceValue=Find<Image>(node.transform,"Difficulty Label");
                    slot.FindPropertyRelative("Glow").objectReferenceValue=Find<Image>(root.transform,"Current Level Glow "+i);
                }
                var selected=so.FindProperty("tabSelections"); selected.arraySize=3;
                for(int i=0;i<3;i++) selected.GetArrayElementAtIndex(i).objectReferenceValue=Find<Image>(root.transform,"Selected Tab "+i);
                var flows=new[]{FlowPage.Pass,FlowPage.FireQuest,FlowPage.Arena,FlowPage.NoAds,FlowPage.Daily}; var docks=so.FindProperty("docks"); docks.arraySize=flows.Length;
                for(int i=0;i<flows.Length;i++)
                {
                    var slot=docks.GetArrayElementAtIndex(i); slot.FindPropertyRelative("Destination").intValue=(int)flows[i];
                    slot.FindPropertyRelative("Button").objectReferenceValue=Find<Button>(root.transform,"Home "+flows[i]);
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                foreach(var dock in panel.Docks) foreach(var caption in dock.Button.GetComponentsInChildren<TMP_Text>(true)) caption.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root,HomePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void MigrateShop(TMP_FontAsset font, Material material)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(PreviousShopPath); if(source==null)return;
            var root=(GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
                var legacy=root.GetComponent<PixelFlowShopPrefabView>(); var panel=root.GetComponent<ShopPanel>();
                if(legacy==null)throw new InvalidOperationException("Expected previous serialized skin.");
                var target=new SerializedObject(panel); var previous=new SerializedObject(legacy); var it=previous.GetIterator(); it.NextVisible(true);
                while(it.NextVisible(false)) if(it.name!="m_Script" && target.FindProperty(it.name)!=null)target.CopyFromSerializedProperty(it);
                target.ApplyModifiedPropertiesWithoutUndo();
                var native=Find<RectTransform>(root.transform,"Native PixelFlow Shop");
                var status=target.FindProperty("statusText").objectReferenceValue as TMP_Text;
                if(status!=null){status.transform.SetParent(native,false);status.gameObject.SetActive(false);}
                foreach(var child in root.transform.Cast<Transform>().ToArray())if(child!=native)UnityEngine.Object.DestroyImmediate(child.gameObject);
                UnityEngine.Object.DestroyImmediate(legacy); ScaleAndStyle(root,font,material);
                var background=Find<Image>(native,"Shop Background"); background.transform.SetParent(root.transform,false);background.transform.SetAsFirstSibling();Stretch(background.rectTransform);
                native.name="Safe Area";native.gameObject.AddComponent<Dreamy.UI.SafeArea>();Stretch(native);root.name="ShopPanel";
                target.Update(); target.FindProperty("sectionHeight").floatValue=114;target.FindProperty("goldRowHeight").floatValue=480;
                target.FindProperty("goldColumnSpacing").floatValue=333;target.FindProperty("bannerHeight").floatValue=352.5f;target.FindProperty("bundleHeight").floatValue=435;target.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,ShopPath);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        private static void ScaleAndStyle(GameObject root,TMP_FontAsset font,Material material)
        {
            foreach(var rect in root.GetComponentsInChildren<RectTransform>(true))if(rect!=root.transform){rect.anchoredPosition*=1.5f;rect.sizeDelta*=1.5f;}
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font=font;text.fontSharedMaterial=material;text.fontSize*=1.5f;text.fontSizeMin*=1.5f;text.fontSizeMax*=1.5f;text.fontStyle=FontStyles.Normal;text.UpdateMeshPadding();
            }
            foreach(var image in root.GetComponentsInChildren<Image>(true))
            {
                if(image.type==Image.Type.Tiled)continue;image.type=Image.Type.Simple;image.preserveAspect=true;
                if(image.sprite==null){image.preserveAspect=false;continue;}
                string name=image.sprite.name;
                bool continuous=name.StartsWith("Section")||name.StartsWith("Content")||name.StartsWith("Bundle")||name=="GoldCard"||name=="LabelDark"||name=="BottomBar";
                if(continuous && image.sprite.border!=Vector4.zero){image.type=Image.Type.Sliced;image.preserveAspect=false;}
                if(image.name.Contains("Gradient")||image.name=="Shop Background")image.preserveAspect=false;
            }
        }
        private static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    }
}
