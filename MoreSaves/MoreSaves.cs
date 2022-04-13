using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Modding;
using UnityEngine;
using UnityEngine.UI;
using static Modding.CanvasUtil;
using Object = UnityEngine.Object;

namespace MoreSaves
{
    public class MoreSaves : Mod, IGlobalSettings<GlobalSettings>, ICustomMenuMod
    {
        private const string VERSION = "0.6.1";
        internal static readonly string SavesFolder = Application.persistentDataPath + "/";
        internal static readonly string BackupFolder = SavesFolder + "Saves Backup (Generated by MoreSaves)/";
        internal static readonly string SaveNamesFile = BackupFolder + "savenames.json";
        
        private static GameObject _canvas;
        internal static MoreSaves Instance;

        public static Texture2D PanelImage;

        public static Text PageLabel;

        public override string GetVersion()
        {
            return VERSION;
        }

        public static Dictionary<int, string> SaveSlotNames = new();


        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            //created new class to make this less bloated
            MenuScreen CustomMenu = ModMenu.CreateCustomMenu(modListMenu);
            return CustomMenu;
        }

        public static GlobalSettings settings { get; set; } = new ();
        public void OnLoadGlobal(GlobalSettings s) => settings = s;
        public GlobalSettings OnSaveGlobal() => settings;

        public override void Initialize()
        {
            Instance ??= this;
            Log("Initializing MoreSaves");

            _canvas = CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920f, 1080f));

            PageLabel = CreateTextPanel
                (
                    _canvas, "Page 1/?", 29, TextAnchor.MiddleCenter,
                    new RectData
                    (
                        new Vector2(200f, 200f),
                        new Vector2(1240f, 870f),
                        new Vector2(0f, 0f),
                        new Vector2(0f, 0f)
                    )
                )
                .GetComponent<Text>();

            PageLabel.enabled = true;

            //load the existing names of saves
            if (File.Exists(SaveNamesFile))
            {
                SaveSlotNames = Newtonsoft.Json.JsonConvert.DeserializeObject <SaveNaming>(File.ReadAllText(SaveNamesFile))?.SaveSlotNames_Saver;
            }

            FadeOut(0f);
            _canvas.AddComponent<MoreSavesComponent>();
            Object.DontDestroyOnLoad(_canvas);
            LoadImage();

            Log("Initialized MoreSaves");
        }

        private void LoadImage()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (string res in asm.GetManifestResourceNames())
            {
                Log(res);
                if (!res.StartsWith("MoreSaves.Resources")) continue;
                
                try
                {
                    using Stream imageStream = asm.GetManifestResourceStream(res);
                    byte[] buffer = new byte[imageStream.Length];
                    imageStream.Read(buffer, 0, buffer.Length);

                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(buffer.ToArray(), true);

                    string[] split = res.Split('.');
                    string internalName = split[split.Length - 2];

                    PanelImage = tex;
                        
                    Log("Loaded image: " + internalName);
                }
                catch (Exception e)
                {
                    Modding.Logger.LogError("Failed to load image: " + res + "\n" + e);
                }
            }
        }

        private static void FadeOut(float t)
        {
            PageLabel.CrossFadeAlpha(0f, t, true);
        }
        
        public bool ToggleButtonInsideMenu { get; }
    }
}
