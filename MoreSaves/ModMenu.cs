using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Modding;
using Modding.Menu;
using Modding.Menu.Config;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo

namespace MoreSaves
{
    internal static class ModMenu
    {
        private static MenuScreen MainMenu;
        private static MenuScreen RestoreSavesMenu;
        private static MenuScreen NameSavesMenu;
        public static MenuOptionHorizontal AutoBackupSelector;
        private static MenuOptionHorizontal ChangeNameHorizontalOption;
        private static GameObject InputTextPanel;
        private static CanvasInput NameInput;
        [UsedImplicitly]
        private static string InputText;

        #region MainMenu
        
        //create the main screen that shows up in mod menu
        public static MenuScreen CreateCustomMenu(MenuScreen modListMenu)
        {
            MoreSaves.Instance.Log("Making new one");
            //Create folder so it doesnt create NREs when looking for stuff in it.
            if (!Directory.Exists(MoreSaves.BackupFolder))
                Directory.CreateDirectory(MoreSaves.BackupFolder);

            MainMenu = new MenuBuilder(UIManager.instance.UICanvas.gameObject, "MoreSavesMenu")
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
                .SetDefaultNavGraph(new GridNavGraph(1))
                .AddContent(
                    RegularGridLayout.CreateVerticalLayout(115f),
                    c =>
                    {
                        c.AddKeybind(
                                "NextPage",
                                MoreSaves.settings.keybinds.NextPage,
                                new KeybindConfig
                                {
                                    Label = "Go to Next Page",
                                    CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu)
                                })
                            .AddKeybind(
                                "PreviousPage",
                                MoreSaves.settings.keybinds.PreviousPage,
                                new KeybindConfig
                                {
                                    Label = "Go to Previous Page",
                                    CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                                })
                            .AddMenuButton(
                                "Make New Page",
                                new MenuButtonConfig
                                {
                                    Label = "Make New Page",
                                    SubmitAction = _ =>
                                    {
                                        MoreSavesComponent._maxPages++;

                                        PlayerPrefs.SetInt("MaxPages", MoreSavesComponent._maxPages);
                                    },
                                    CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                                    Description = new DescriptionInfo
                                    {
                                        Text = "Pressing this will increase the max page amount"
                                    }

                                }).AddMenuButton(
                                "Remove Last Page (if redundant)",
                                new MenuButtonConfig
                                {
                                    Label = "Remove Last Page",
                                    SubmitAction = RemoveLastPage,
                                    CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                                    Description = new DescriptionInfo
                                    {
                                        Text = "Pressing this will delete the last page. Note: it will only delete the page if it is redundant"
                                    }

                                })
                            .AddHorizontalOption(
                                "AutoBackup",
                                new HorizontalOptionConfig
                                {
                                    Label = "Enable Auto Backup",
                                    Options = new[] {"No", "Yes"},
                                    ApplySetting = (_, i) => { MoreSaves.settings.AutoBackup = i != 0; },
                                    RefreshSetting = (s, _) =>
                                        s.optionList.SetOptionTo(MoreSaves.settings.AutoBackup ? 1 : 0),
                                    CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                                    Style = HorizontalOptionStyle.VanillaStyle,
                                    Description = new DescriptionInfo
                                    {
                                        Text = "It will back up your saves just before you quit the game"
                                    }
                                }, out AutoBackupSelector)
                            .AddMenuButton(
                                "BackUpSaves",
                                new MenuButtonConfig
                                {
                                    Label = "Back up Saves",
                                    SubmitAction = BackupSaves,
                                    CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                                    Description = new DescriptionInfo
                                    {
                                        Text = "Click here to back up all saves"
                                    }

                                })
                            .AddMenuButton(
                                "RestoreSavesButton",
                                new MenuButtonConfig
                                {
                                    Label = "Restore Saves",
                                    SubmitAction = _ => UIManager.instance.UIGoToDynamicMenu(RestoreSavesMenu),
                                    CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                                    Proceed = true,
                                    Description = new DescriptionInfo
                                    {
                                        Text = "Pressing this will open a menu, which allows you to restore saves"
                                    }

                                }).AddMenuButton(
                                "NameSaveFilesButton",
                                new MenuButtonConfig
                                {
                                    Label = "Change name on save file",
                                    SubmitAction = _ => { UIManager.instance.UIGoToDynamicMenu(NameSavesMenu); },
                                    CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                                    Proceed = true,
                                    Description = new DescriptionInfo
                                    {
                                        Text = "Pressing this will open a menu, which allows you to change the name on saves"
                                    }
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
                        new MenuButtonConfig
                        {
                            Label = "Back",
                            CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                            SubmitAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                            Style = MenuButtonStyle.VanillaStyle,
                            Proceed = true
                        }
                    )
                )
                .Build();

            //make other screens we need
            //we need to do this after main menu is built or else NREs go brrrr
            RestoreSavesMenu = CreateRestoreSavesMenu(MainMenu);
            NameSavesMenu = CreateNamingSaveFile(MainMenu);

            //This creates the text input panel we need for getting text for save file naming
            CreateInputPanel();

            return MainMenu;
        }

        private static void RemoveLastPage(MenuButton obj)
        {
            if (Enumerable.Range(1, 8).Any(i =>
                File.Exists(
                    $"{Application.persistentDataPath}/user{(MoreSavesComponent._maxPages - 1) * 4 + i}.dat")))
                return;
            PlayerPrefs.SetInt("MaxPages", --MoreSavesComponent._maxPages);
            MoreSaves.PageLabel.text =
                $"Page {MoreSavesComponent._currentPage + 1}/{MoreSavesComponent._maxPages}";
        }

        #endregion

        #region Save File Naming Screen
        private static MenuScreen CreateNamingSaveFile(MenuScreen mainmenu)
        {
            CreateSaveFileDict(MoreSaves.SavesFolder);

            // We need to do this on every cancel to make sure if player presses esc, the file is still updated
            void ReturnToPreviousMenu(MenuSelectable obj)
            {
                SaveNameToFile();
                UIManager.instance.UIGoToDynamicMenu(mainmenu);
            }

            return new MenuBuilder("NameSaveFile")
                .CreateTitle("Name Save Files", MenuTitleStyle.vanillaStyle)
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
                    RegularGridLayout.CreateVerticalLayout(400f),
                    
                    // Create a panel to hold the input field
                    c => c.AddStaticPanel("MainTextPanel",
                            new RelVector2(new Vector2(200, 600)),
                            out InputTextPanel)
                        .AddHorizontalOption(
                            "ChooseSave",
                            new HorizontalOptionConfig
                            {
                                Label = "Choose Save to name",
                                Options = SaveFiles.ToArray(),
                                CancelAction = ReturnToPreviousMenu,
                                ApplySetting = (_, _) =>
                                {
                                    GameManager.instance.StartCoroutine(FindCurrentName());
                                },
                            }, out ChangeNameHorizontalOption)).AddControls(
                    new SingleContentLayout(new AnchoredPosition(
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(-200f, 64f)
                    )),
                    c => c.AddMenuButton(
                        "ClearButton",
                        new MenuButtonConfig
                        {
                            Label = "Clear",
                            CancelAction = ReturnToPreviousMenu,
                            SubmitAction = ClearSaveName,
                            Proceed = false,
                            Style = MenuButtonStyle.VanillaStyle
                        }
                    )).AddControls(
                    new SingleContentLayout(new AnchoredPosition(
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(200f, 64f)
                    )),
                    c => c.AddMenuButton(
                        "ApplyButton",
                        new MenuButtonConfig
                        {
                            Label = "Apply",
                            CancelAction = ReturnToPreviousMenu,
                            SubmitAction = ChangeTheName,
                            Proceed = false,
                            Style = MenuButtonStyle.VanillaStyle
                        }
                    )).AddControls(
                    new SingleContentLayout(new AnchoredPosition(
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -64f)
                    )),
                    c => c.AddMenuButton(
                        "BackButton",
                        new MenuButtonConfig
                        {
                            Label = "Back",
                            CancelAction = ReturnToPreviousMenu,
                            SubmitAction = (Action<MenuSelectable>) ReturnToPreviousMenu,
                            Proceed = true,
                            Style = MenuButtonStyle.VanillaStyle
                        }
                    )
                ).Build();
        }
        
        #endregion

        #region Save File Naming Functions
        private static void ClearSaveName(MenuButton obj) => GameManager.instance.StartCoroutine(ClearSaveName());

        private static IEnumerator ClearSaveName()
        {
            yield return null;
            int filenumber = GetFileNumber();
            if (MoreSaves.SaveSlotNames.ContainsKey(filenumber))
            {
                MoreSaves.SaveSlotNames.Remove(filenumber);
            }
            NameInput.ChangePlaceholder("Write name here");
            NameInput.ClearText();
        }

        private static IEnumerator FindCurrentName()
        {
            yield return null;
            int filenumber = GetFileNumber();
            NameInput.ChangePlaceholder(GetFromNameDict(filenumber));
        }

        private static string GetFromNameDict(int filenumber)
        {
            return MoreSaves.SaveSlotNames.TryGetValue(filenumber, out var newtext) ? newtext : "Write name here";
        }
        
        public static void SaveNameToFile()
        {
            var DictToSave = new SaveNaming
            {
                SaveSlotNames_Saver = MoreSaves.SaveSlotNames
            };

            File.WriteAllText(MoreSaves.SaveNamesFile, 
                Newtonsoft.Json.JsonConvert.SerializeObject(DictToSave));
        }
        
        private static void CreateInputPanel()
        {
            // I dont need to do a null check because when every its called, it shouldnt exist
            NameInput = new CanvasInput(
                InputTextPanel, 
                "IP Input", 
                MoreSaves.PanelImage,
                new Vector2(GetCenter(800,true), GetCenter(100,false)),
                Vector2.zero,
                //dont ask why x,y is 150,400 (im not sure either)
                //800,100 is width and height taht looks good
                new Rect(150, 400, 800, 100),
                CanvasUtil.TrajanBold, 
                InputText, 
                GetFromNameDict(1),
                36);

            //puts in center. dont ask why its divided by 2 (im not sure either)
            static int GetCenter(int size, bool Horizontal) => ((Horizontal ? Screen.width : Screen.height) - size) / 2;
        }
        
        private static void ChangeTheName(MenuButton obj)
        {
            int filenumber = GetFileNumber();

            string NewText = NameInput.GetText();

            if (MoreSaves.SaveSlotNames.ContainsKey(filenumber))
            {
                MoreSaves.SaveSlotNames[filenumber] = NewText;
            }
            else MoreSaves.SaveSlotNames.Add(filenumber,NewText);

            GameManager.instance.StartCoroutine(FindCurrentName());
            NameInput.ClearText();
        }

        private static int GetFileNumber()
        {
            string filenumber_string = ChangeNameHorizontalOption.optionText.text;
                            
            //it is stored in 0x format for numbers < 10, so 11 doesnt come before 2
            if (filenumber_string[0] == '0') filenumber_string = filenumber_string.Replace("0", "");

            return Int32.Parse(filenumber_string);
        }
        
        #endregion
        
        #region Restore Saves Menu
        private static MenuScreen CreateRestoreSavesMenu(MenuScreen mainmenu)
        {
            return new MenuBuilder("Restore Saves")
                .CreateTitle("Restore Saves", MenuTitleStyle.vanillaStyle)
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
                .AddControls(
                    new SingleContentLayout(new AnchoredPosition(
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -64f)
                    )),
                    c => c.AddMenuButton(
                        "BackButton",
                        new MenuButtonConfig
                        {
                            Label = "Back",
                            CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(mainmenu),
                            SubmitAction = _ => UIManager.instance.UIGoToDynamicMenu(mainmenu),
                            Proceed = true,
                            Style = MenuButtonStyle.VanillaStyle
                        }
                    )
                ).AddContent(new NullContentLayout(), c => c.AddScrollPaneContent(
                    new ScrollbarConfig
                    {
                        CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(mainmenu),
                        Navigation = new Navigation
                        {
                            mode = Navigation.Mode.Explicit,
                        },
                        Position = new AnchoredPosition
                        {
                            ChildAnchor = new Vector2(0f, 1f),
                            ParentAnchor = new Vector2(1f, 1f),
                            Offset = new Vector2(-310f, 0f)
                        }
                    },
                    new RelLength(Directory.GetFiles(MoreSaves.BackupFolder).Length * 210f),
                    RegularGridLayout.CreateVerticalLayout(105f),
                    AddRestoreSaveFileContent
                )).Build();
        }
        
        #endregion

        #region Restore Saves Functions
        //My reason for doing this instead of a simple foreach is ordering. with foreach 10 comes before 2 which is wrong
        private static SortedSet<string> SaveFiles = new SortedSet<string>();
        private static SortedDictionary<string,DateTime> BackupSaveFiles = new SortedDictionary<string, DateTime>();

        private static void CreateSaveFileDict(string whichFolder)
        {
            if (whichFolder == MoreSaves.BackupFolder) BackupSaveFiles.Clear();
            if (whichFolder == MoreSaves.SavesFolder) SaveFiles.Clear();
            
            foreach (string saveFile in Directory.EnumerateFiles(whichFolder))
            {
                string filename = Path.GetFileName(saveFile);

                if (!IsSaveFile(filename)) continue;
                
                DateTime lastmodified = File.GetLastWriteTime(saveFile);

                string filenumber = filename.Replace("user", "").Replace(".dat","");

                if (filenumber.Length == 1) filenumber = $"0{filenumber}";
                
                if (whichFolder == MoreSaves.BackupFolder) BackupSaveFiles.Add(filenumber,lastmodified);
                if (whichFolder == MoreSaves.SavesFolder) SaveFiles.Add(filenumber);
            }
        }
        
        private static void AddRestoreSaveFileContent(ContentArea c)
        {
            CreateSaveFileDict(MoreSaves.BackupFolder);
            
            foreach (KeyValuePair<string, DateTime> BackedUpFiles in BackupSaveFiles)
            {
                string lastsaved_date = $"{BackedUpFiles.Value.Day}/{BackedUpFiles.Value.Month}/{BackedUpFiles.Value.Year}";
                string lastsaved_time = $"{BackedUpFiles.Value.Hour}:{BackedUpFiles.Value.Minute}:{BackedUpFiles.Value.Second}";

                string lastsaved = $"This backup is from: {lastsaved_time} on {lastsaved_date}";

                string filenumber = BackedUpFiles.Key;
                if (filenumber[0] == '0') filenumber = filenumber.Replace("0", "");

                string source = $"{MoreSaves.BackupFolder}user{filenumber}";
                string dest = $"{MoreSaves.SavesFolder}user{filenumber}";

                c.AddMenuButton(
                    $"Restore user{filenumber}",
                    new MenuButtonConfig
                    {
                        Label = $"Restore Save {filenumber}",
                        SubmitAction = _ => File.Copy(source, dest, true),
                        Style = MenuButtonStyle.VanillaStyle,
                        Description = new DescriptionInfo
                        {
                            Text = lastsaved
                        }

                    });
            }
        }
        private static bool IsSaveFile(string filename)
        {
            //ignore the other files in the folder
            if(!filename.StartsWith("user")) return false;
                
            //ignore the .bak files and the user .json files API or QoL creates
            if (!filename.EndsWith(".dat")) return false;
                
            //ignore the version labeled files
            if (filename.Contains("_")) return false;
                
            //ignore any weird userN(1).dat
            if (filename.Contains("(")) return false;

            return true;
        }
        
        private static void BackupSaves(MenuButton obj)
        {
            BackupSaves();
            
            //i need to delete and remake it to make sure the new save files that are backed up show up in the menu
            UnityEngine.Object.Destroy(RestoreSavesMenu);
            RestoreSavesMenu = CreateRestoreSavesMenu(MainMenu);
        }
        public static void BackupSaves()
        {
            if(!Directory.Exists(MoreSaves.BackupFolder))
                Directory.CreateDirectory(MoreSaves.BackupFolder);
            
            foreach(string saveFile in Directory.EnumerateFiles(MoreSaves.SavesFolder))
            {
                string filename = Path.GetFileName(saveFile);

                string dest = MoreSaves.BackupFolder +filename;
                
                if (!IsSaveFile(filename)) continue;
                
                //copy it in
                File.Copy( saveFile, dest, true);

                MoreSaves.Instance.Log( "Copied " + saveFile + " to " + dest );
            }
        }
        #endregion
    }
}