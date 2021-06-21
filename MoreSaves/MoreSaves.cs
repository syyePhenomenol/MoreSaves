using System.IO;
using JetBrains.Annotations;
using Modding;
using UnityEngine;
using UnityEngine.UI;
using static Modding.CanvasUtil;
using Modding.Menu;
using Modding.Menu.Config;

namespace MoreSaves
{
    [UsedImplicitly]
    public class MoreSaves : Mod,IGlobalSettings<Settings>,ICustomMenuMod
    {
        private const string VERSION = "0.4.3";
        private static readonly string SavesFolder = Application.persistentDataPath;
        private static readonly string BackupFolder = SavesFolder + "/Saves Backup (from MoreSaves Mod)";

        private static GameObject _canvas;

        internal static MoreSaves Instance;

        public static Text PageLabel;

        public override string GetVersion()
        {
            return VERSION;
        }
        
        public static Settings settings { get; set; } = new Settings();
        public void OnLoadGlobal(Settings s) => MoreSaves.settings = s;
        public Settings OnSaveGlobal() => MoreSaves.settings;

        public MenuScreen GetMenuScreen(MenuScreen modListMenu)
        {
            return new MenuBuilder(UIManager.instance.UICanvas.gameObject, "MoreSavesMenu")
                .CreateTitle("MoreSaves Settings", MenuTitleStyle.vanillaStyle)
                .CreateContentPane(RectTransformData.FromSizeAndPos(
                    new RelVector2(new Vector2(1920f, 903f)),
                    new AnchoredPosition(
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -60f)
                    )
                ))
                .CreateControlPane(RectTransformData.FromSizeAndPos(
                    new RelVector2(new Vector2(1920f, 259f)),
                    new AnchoredPosition(
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -502f)
                    )
                ))
                .SetDefaultNavGraph(new ChainedNavGraph())
                .AddContent(
                    RegularGridLayout.CreateVerticalLayout(150f),
                    c =>
                    {
                        c.AddKeybind(
                            "NextPage",
                            settings.keybinds.NextPage,
                            new KeybindConfig
                            {
                                Label = "Go to Next Page",
                                CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu)
                            }
                        ).AddKeybind(
                            "PreviousPage",
                            settings.keybinds.PreviousPage,
                            new KeybindConfig
                            {
                                Label = "Go to Previous Page",
                                CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu)
                            }
                        ).AddMenuButton(
                            "BackUpSaves",
                            new MenuButtonConfig
                            {
                                Label = "Back up Saves",
                                SubmitAction = BackupSaves,
                                CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                                
                            }).AddMenuButton(
                            "RestoreSaves",
                            new MenuButtonConfig
                            {
                                Label = "Restore Saves",
                                SubmitAction = RestoreSaves,
                                CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                                
                            });
                    }
                    )
                .AddControls(
                    new SingleContentLayout(new AnchoredPosition(
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -64f)
                    )),
                    c => c.AddMenuButton(
                        "BackButton",
                        new MenuButtonConfig {
                            Label = "Back",
                            CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                            SubmitAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                            Style = MenuButtonStyle.VanillaStyle,
                            Proceed = true
                        }
                    )
                )
                .Build();
        }

        public override void Initialize()
        {
            if (Instance == null) Instance = this;
            Log("Initializing MoreSaves");

            CreateFonts();

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

            FadeOut(0f);
            _canvas.AddComponent<MoreSavesComponent>();
            Object.DontDestroyOnLoad(_canvas);
            Log("Initialized MoreSaves");

        }

        private static void FadeOut(float t)
        {
            PageLabel.CrossFadeAlpha(0f, t, true);
        }

        private void BackupSaves(MenuButton obj)
        {
            if( !Directory.Exists(BackupFolder))
                Directory.CreateDirectory(BackupFolder);
            
            foreach(string saveFile in Directory.EnumerateFiles(SavesFolder))
            {
                string filename = Path.GetFileName(saveFile);

                //ignore the other files in the folder
                if(!filename.StartsWith("user")) continue;
                
                //ignore the .bak files and the user .json files API or QoL creates
                if (!filename.EndsWith(".dat")) continue;
                
                //ignore the version labeled files
                if (filename.Contains("_")) continue;
                
                //ignore any weird userN(1).dat
                if (filename.Contains("(")) continue;

                string dest = BackupFolder +"/"+filename;

                //copy it in
                File.Copy( saveFile, dest, true );

                Debug.Log( "Copied " + saveFile + " to " + dest );
            }
        }

        private void RestoreSaves(MenuButton obj)
        {
            //make sure save files exist
            if (!(Directory.Exists(BackupFolder) && Directory.GetFiles(BackupFolder).Length > 0)) return;
            
            foreach(string saveFile in Directory.EnumerateFiles(BackupFolder))
            {
                string filename = Path.GetFileName(saveFile);

                //ignore the other files in the folder
                if(!filename.StartsWith("user")) continue;
                
                //ignore the .bak files and the user .json files API or QoL creates
                if (!filename.EndsWith(".dat")) continue;
                
                //ignore the version labeled files
                if (filename.Contains("_")) continue;
                
                //ignore any weird userN(1).dat
                if (filename.Contains("(")) continue;
                
                string dest = SavesFolder +"/"+filename;

                //copy it in
                File.Copy( saveFile, dest, true );

                Debug.Log( "Copied " + saveFile + " to " + dest );
            }
        }
    }
}
