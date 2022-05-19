using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using JetBrains.Annotations;
using Modding;
using Modding.Menu;
using Modding.Menu.Config;
using Modding.Patches;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Logger = Modding.Logger;

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
        private static MenuScreen EditChooseMenu;
        private static MenuScreen EditSavesMenu;
        private static MenuScreen ModListMenu;

        private static MenuOptionHorizontal ChangeNameHorizontalOption;
        private static GameObject InputTextPanel;
        private static CanvasInput NameInput;
        private static CanvasInput SearchInput;

        [UsedImplicitly] private static string InputText;
        [UsedImplicitly] private static string InputText_EditSaves;

        private static int EditSaveFileNumber;
        private static SceneData EditSaveFileSceneData;
        private static PlayerData EditSaveFilePlayerData;

        private static List<GameObject> AllPDFields = new();
        private static List<InputFieldInfo> AllInputs = new();

        #region MainMenu

        private static MenuBuilder CreateMenuBuilder(string Title)
        {
            return new MenuBuilder(UIManager.instance.UICanvas.gameObject, Title)
                .CreateTitle(Title, MenuTitleStyle.vanillaStyle)
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
                .SetDefaultNavGraph(new ChainedNavGraph());
        }

        private static MenuBuilder AddBackButton(this MenuBuilder builder, MenuScreen returnScreen)
        {
            return builder.AddControls(
                new SingleContentLayout(new AnchoredPosition(
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -64f)
                )), c => c.AddMenuButton(
                    "BackButton",
                    new MenuButtonConfig
                    {
                        Label = "Back",
                        CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(returnScreen),
                        SubmitAction = _ => UIManager.instance.UIGoToDynamicMenu(returnScreen),
                        Style = MenuButtonStyle.VanillaStyle,
                        Proceed = true
                    }));
        }

        private static MenuBuilder AddControlButton(this MenuBuilder builder, string name, Vector2 offset,
            Action<MenuSelectable> cancelAction, Action<MenuButton> submitAction)
        {
            return builder.AddControls(
                new SingleContentLayout(new AnchoredPosition(
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    offset
                )), c => c.AddMenuButton(
                    name,
                    new MenuButtonConfig
                    {
                        Label = name,
                        CancelAction = cancelAction,
                        SubmitAction = submitAction,
                        Style = MenuButtonStyle.VanillaStyle,
                        Proceed = true
                    }));
        }

        //create the main screen that shows up in mod menu
        public static MenuScreen CreateCustomMenu(MenuScreen modListMenu)
        {
            ModListMenu = modListMenu;
            //Create folder so it doesnt create NREs when looking for stuff in it.
            if (!Directory.Exists(MoreSaves.BackupFolder))
                Directory.CreateDirectory(MoreSaves.BackupFolder);

            MainMenu = CreateMenuBuilder("MoreSaves Settings")
                .AddContent(
                    RegularGridLayout.CreateVerticalLayout(105f),
                    c =>
                    {
                        c.AddScrollPaneContent(
                            new ScrollbarConfig
                            {
                                CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
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
                            }, new RelLength(Directory.GetFiles(MoreSaves.BackupFolder).Length * 210f),
                            RegularGridLayout.CreateVerticalLayout(105f),
                            AddMainMenuContent);
                    }
                )
                .AddBackButton(modListMenu)
                .Build();

            //make other screens we need
            //we need to do this after main menu is built or else NREs go brrrr
            RestoreSavesMenu = CreateRestoreSavesMenu(MainMenu);
            NameSavesMenu = CreateNamingSaveFile(MainMenu);
            EditChooseMenu = CreateChooseEditSavesMenu(MainMenu);

            //This creates the text input panel we need for getting text for save file naming
            CreateInputPanel();

            return MainMenu;
        }

        private static void AddMainMenuContent(ContentArea c)
        {
            c.AddKeybind(
                    "NextPage",
                    MoreSaves.settings.keybinds.NextPage,
                    new KeybindConfig
                    {
                        Label = "Go to Next Page",
                        CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(ModListMenu)
                    })
                .AddKeybind(
                    "PreviousPage",
                    MoreSaves.settings.keybinds.PreviousPage,
                    new KeybindConfig
                    {
                        Label = "Go to Previous Page",
                        CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(ModListMenu),
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
                        CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(ModListMenu),
                        Style = HorizontalOptionStyle.VanillaStyle,
                        Description = new DescriptionInfo
                        {
                            Text = "It will back up your saves just before you quit the game"
                        }
                    }, out var AutoBackupSelector);
            AutoBackupSelector.menuSetting.RefreshValueFromGameSettings();

            c.AddMenuButton(
                    "BackUpSaves",
                    new MenuButtonConfig
                    {
                        Label = "Back up Saves",
                        SubmitAction = BackupSaves,
                        CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(ModListMenu),
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
                        CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(ModListMenu),
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
                        CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(ModListMenu),
                        Proceed = true,
                        Description = new DescriptionInfo
                        {
                            Text =
                                "Pressing this will open a menu, which allows you to change the name on saves"
                        }
                    }).AddMenuButton(
                    "EditSaveFile",
                    new MenuButtonConfig
                    {
                        Label = "Edit a save",
                        SubmitAction = _ => { UIManager.instance.UIGoToDynamicMenu(EditChooseMenu); },
                        CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(ModListMenu),
                        Proceed = true,
                        Description = new DescriptionInfo
                        {
                            Text = "Pressing this will open a menu, which allows you to edit saves"
                        }
                    })
                .AddMenuButton(
                "DiscordButton",
                new MenuButtonConfig
                {
                    Label = "Need More Help? or Have Suggestions?",
                    CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(ModListMenu),
                    SubmitAction = _ => Application.OpenURL("https://discord.gg/F6Y5TeFQ8j"),
                    Proceed = true,
                    Style = MenuButtonStyle.VanillaStyle,
                    Description = new DescriptionInfo
                    {
                        Text = "Join the Hollow Knight Modding Discord."
                    }
                })
                .AddMenuButton(
                    "Saves",
                    new MenuButtonConfig
                    {
                        Label = "Open Saves Folder",
                        CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(ModListMenu),
                        SubmitAction = _ => Process.Start(Application.persistentDataPath),
                        Proceed = true,
                        Style = MenuButtonStyle.VanillaStyle,
                        Description = new DescriptionInfo
                        {
                            Text = "Click to open saves folder"
                        }
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
                        CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(ModListMenu),
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
                        CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(ModListMenu),
                        Description = new DescriptionInfo
                        {
                            Text = "Pressing this will delete the last page. Note: it will only delete the page if it is redundant"
                        }
                    });    
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

            return CreateMenuBuilder("Name Save Files")
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
                                ApplySetting = (_, _) => { GameManager.instance.StartCoroutine(FindCurrentName()); },
                            }, out ChangeNameHorizontalOption))
                .AddControlButton("Clear", new Vector2(-200f, 64f), ReturnToPreviousMenu, ClearSaveName)
                .AddControlButton("Apply", new Vector2(200f, 64f), ReturnToPreviousMenu, ChangeTheName)
                .AddControlButton("Back", new Vector2(0f, -64f), ReturnToPreviousMenu, ReturnToPreviousMenu)
                .Build();
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

            File.WriteAllText(MoreSaves.SaveNamesFile, JsonConvert.SerializeObject(DictToSave));
        }

        private static void CreateInputPanel()
        {
            // I dont need to do a null check because when every its called, it shouldnt exist
            NameInput = new CanvasInput(
                InputTextPanel,
                "IP Input",
                MoreSaves.PanelImage,
                new Vector2(GetCenter(800, true), GetCenter(100, false)),
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
            else MoreSaves.SaveSlotNames.Add(filenumber, NewText);

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
            static void AddRestoreSaveFileContent(ContentArea c)
            {
                CreateSaveFileDict(MoreSaves.BackupFolder);

                foreach (KeyValuePair<string, DateTime> BackedUpFiles in BackupSaveFiles)
                {
                    string lastsaved_date =
                        $"{BackedUpFiles.Value.Day}/{BackedUpFiles.Value.Month}/{BackedUpFiles.Value.Year}";
                    string lastsaved_time =
                        $"{BackedUpFiles.Value.Hour}:{BackedUpFiles.Value.Minute}:{BackedUpFiles.Value.Second}";

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

            return CreateMenuBuilder("Restore Saves")
                .AddBackButton(mainmenu)
                .AddContent(new NullContentLayout(), c => c.AddScrollPaneContent(
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
        private static SortedDictionary<string, DateTime> BackupSaveFiles = new SortedDictionary<string, DateTime>();

        private static void CreateSaveFileDict(string whichFolder)
        {
            if (whichFolder == MoreSaves.BackupFolder) BackupSaveFiles.Clear();
            if (whichFolder == MoreSaves.SavesFolder) SaveFiles.Clear();

            foreach (string saveFile in Directory.EnumerateFiles(whichFolder))
            {
                string filename = Path.GetFileName(saveFile);

                if (!IsSaveFile(filename)) continue;

                DateTime lastmodified = File.GetLastWriteTime(saveFile);

                string filenumber = filename.Replace("user", "").Replace(".dat", "");

                if (filenumber.Length == 1) filenumber = $"0{filenumber}";

                if (whichFolder == MoreSaves.BackupFolder) BackupSaveFiles.Add(filenumber, lastmodified);
                if (whichFolder == MoreSaves.SavesFolder) SaveFiles.Add(filenumber);
            }
        }

        private static bool IsSaveFile(string filename)
        {
            //ignore the other files in the folder
            if (!filename.StartsWith("user")) return false;

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

            //i need to delete and remake it to make sure the new save files that are backed up show up in the menu. its easier to do this than to add buttons
            UnityEngine.Object.Destroy(RestoreSavesMenu);
            RestoreSavesMenu = CreateRestoreSavesMenu(MainMenu);
        }

        public static void BackupSaves()
        {
            if (!Directory.Exists(MoreSaves.BackupFolder))
                Directory.CreateDirectory(MoreSaves.BackupFolder);

            foreach (string saveFile in Directory.EnumerateFiles(MoreSaves.SavesFolder))
            {
                string filename = Path.GetFileName(saveFile);

                string dest = MoreSaves.BackupFolder + filename;

                if (!IsSaveFile(filename)) continue;

                //copy it in
                File.Copy(saveFile, dest, true);

                MoreSaves.Instance.Log("Copied " + saveFile + " to " + dest);
            }
        }

        #endregion

        #region Edit Saves

        private static MenuScreen CreateChooseEditSavesMenu(MenuScreen mainmenu)
        {
            CreateSaveFileDict(MoreSaves.SavesFolder);

            static void AddEditSaveFileContent(ContentArea c)
            {
                foreach (var filenumber in SaveFiles)
                {
                    c.AddMenuButton(
                        $"Restore user{filenumber}",
                        new MenuButtonConfig
                        {
                            Label = $"Edit  Save {filenumber}",
                            SubmitAction = _ =>
                            {
                                CreateEditSavesMenu(EditChooseMenu, filenumber);
                                UIManager.instance.UIGoToDynamicMenu(EditSavesMenu);
                            },
                            Style = MenuButtonStyle.VanillaStyle,
                        });
                }
            }

            return CreateMenuBuilder("Choose Edit Saves")
                .AddBackButton(mainmenu)
                .AddContent(new NullContentLayout(), c => c.AddScrollPaneContent(
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
                    new RelLength(105f * SaveFiles.Count),
                    RegularGridLayout.CreateVerticalLayout(105f),
                    AddEditSaveFileContent
                )).Build();
        }

        private static void CreateEditSavesMenu(MenuScreen editChooseMenu, string filenumber)
        {
            EditSaveFileNumber = Int32.Parse(filenumber);
            if (GameManager.instance.profileID == EditSaveFileNumber)
            {
                EditSaveFilePlayerData = PlayerData.instance;
                EditSaveFileSceneData = SceneData.instance;
            }
            else
            {
                var SaveData = ReadFromSaveFile(EditSaveFileNumber);
                EditSaveFilePlayerData = SaveData.PlayerData;
                EditSaveFileSceneData = SaveData.SceneData;
            }


            if (EditSavesMenu != null)
            {
                var contentgo = EditSavesMenu.transform.GetChild(2).GetChild(0).GetChild(0); //get the gameobject of the parent of all content gos
                foreach (MenuOptionHorizontal option in contentgo.GetComponentsInChildren<MenuOptionHorizontal>())
                {
                    option.menuSetting.RefreshValueFromGameSettings();
                }

                foreach (Text InputPanel in contentgo.GetComponentsInChildren<Text>())
                {
                    if (!InputPanel.gameObject.name.StartsWith("$")) continue;
                    var placeholder = InputPanel.gameObject.transform.GetChild(0).GetChild(1);//the placeholder text;
                    
                    string name = InputPanel.gameObject.name.Substring(1); //remove first letter
                    FieldInfo field = PDFields.First(f => f.Name == name);
                    
                    Text placeHolderText = placeholder.GetComponent<Text>();
                    if (field.FieldType.ToString() == "System.String")
                    {
                        placeHolderText.text = (string)field.GetValue(EditSaveFilePlayerData);
                    }
                    else if (field.FieldType.ToString() == "System.Single")
                    {
                        placeHolderText.text = ((float)field.GetValue(EditSaveFilePlayerData)).ToString();
                    }
                    else if (field.FieldType.ToString() == "UnityEngine.Vector3")
                    {
                        placeHolderText.text = ((Vector3)field.GetValue(EditSaveFilePlayerData)).ToString();
                    }
                }
            }
            else
            {
                EditSavesMenu = CreateMenuBuilder("Edit Saves")
                    .AddControlButton("Back", new Vector2(0f, -64f), SaveChanges, SaveChanges)
                    .AddContent(new NullContentLayout(), c => c.AddScrollPaneContent(
                        new ScrollbarConfig
                        {
                            CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(editChooseMenu),
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
                        new RelLength(105 * EditSaveFilePlayerData.GetType().GetFields().ToArray().Length),
                        RegularGridLayout.CreateVerticalLayout(105f),
                        AddSaveFileContent
                    )).Build();
            }
        }
        
        private static FieldInfo[] PDFields = typeof(PlayerData).GetFields();

        private static void AddSaveFileContent(ContentArea c)
        {
            string[] BoolArray = { "True", "False" };
            string[] IntArray = Enumerable.Range(-100, 99999).Select(x => x.ToString()).ToArray();

            c.AddStaticPanel("TextPanel",
                new RelVector2(new Vector2(200, 105)),
                out var TextPanel);

            SearchInput = new CanvasInput(
                TextPanel,
                "TextPanel",
                MoreSaves.PanelImage,
                new Vector2(GetCenter(0, true), GetCenter(0, false)),
                Vector2.zero,
                new Rect(0, 0, 600, 80),
                CanvasUtil.TrajanBold,
                InputText_EditSaves,
                "Search bar",
                36);

            static int GetCenter(int size, bool Horizontal) =>
                ((Horizontal ? Screen.width : Screen.height) - size) / 2;

            c.AddMenuButton(
                "SearchButton",
                new MenuButtonConfig
                {
                    Label = "Search",
                    SubmitAction = DoSearch,
                    CancelAction = SaveChanges,
                    Proceed = false,
                    Style = MenuButtonStyle.VanillaStyle
                });

            foreach (var field in PDFields)
            {
                //to make sure text doesnt overlap
                string Name = field.Name;
                if (Name.Length > 26)
                {
                    Name = Name.Remove(25);
                    Name += "...";

                }

                if (field.FieldType.ToString() == "System.Boolean")
                {
                    c.AddHorizontalOption(field.Name,
                        new HorizontalOptionConfig
                        {
                            Label = Name,
                            Options = BoolArray,
                            ApplySetting = (_, i) => { field.SetValue(EditSaveFilePlayerData, i == 0); },
                            RefreshSetting = (s, _) =>
                                s.optionList.SetOptionTo(
                                    (bool)field.GetValue(EditSaveFilePlayerData) ? 0 : 1
                                ),
                            CancelAction = SaveChanges,
                            Style = HorizontalOptionStyle.VanillaStyle,
                        }, out var BoolOption);
                    BoolOption.menuSetting.RefreshValueFromGameSettings();
                    AllPDFields.Add(BoolOption.gameObject);
                }
                else if (field.FieldType.ToString() == "System.Int32")
                {
                    c.AddHorizontalOption(field.Name,
                        new HorizontalOptionConfig
                        {
                            Label = Name,
                            Options = IntArray,
                            ApplySetting = (_, i) => { field.SetValue(EditSaveFilePlayerData, i - 100); },
                            RefreshSetting = (s, _) =>
                                s.optionList.SetOptionTo(
                                    (int)field.GetValue(EditSaveFilePlayerData) + 100
                                ),
                            CancelAction = SaveChanges,
                            Style = HorizontalOptionStyle.VanillaStyle,
                        }, out var IntOption);
                    IntOption.menuSetting.RefreshValueFromGameSettings();
                    AllPDFields.Add(IntOption.gameObject);
                }
                else if (field.FieldType.ToString() == "System.String")
                {
                    c.AddTextPanel(
                        $"${field.Name}",
                        new RelVector2(new RelLength(1000), new RelLength(105f)),
                        new TextPanelConfig
                        {
                            Text = Name,
                            Font = TextPanelConfig.TextFont.TrajanBold,
                            Size = 46,
                            Anchor = TextAnchor.MiddleLeft
                        }, out var TextPanelOption);

                    var NewInput = new CanvasInput(
                        TextPanelOption.gameObject,
                        "IP Input",
                        MoreSaves.PanelImage,
                        new Vector2(Screen.width - TextPanelOption.gameObject.transform.position.x - 500,
                            Screen.height / 2f),
                        Vector2.zero,
                        new Rect(0, 0, 500, 60),
                        CanvasUtil.TrajanBold,
                        InputText,
                        (string)field.GetValue(EditSaveFilePlayerData),
                        36);
                    AllPDFields.Add(TextPanelOption.gameObject);
                    AllInputs.Add(new InputFieldInfo(field, NewInput, typeof(string)));
                }
                else if (field.FieldType.ToString() == "System.Single")
                {
                    c.AddTextPanel($"${field.Name}",
                        new RelVector2(new RelLength(1000), new RelLength(105f)),
                        new TextPanelConfig
                        {
                            Text = Name,
                            Font = TextPanelConfig.TextFont.TrajanBold,
                            Size = 46,
                            Anchor = TextAnchor.MiddleLeft
                        }, out var TextPanelOption);

                    var NewInput = new CanvasInput(
                        TextPanelOption.gameObject,
                        "IP Input",
                        MoreSaves.PanelImage,
                        new Vector2(Screen.width - TextPanelOption.gameObject.transform.position.x - 500,
                            Screen.height / 2f),
                        Vector2.zero,
                        new Rect(0, 0, 500, 60),
                        CanvasUtil.TrajanBold,
                        InputText,
                        ((float)field.GetValue(EditSaveFilePlayerData)).ToString(),
                        36);
                    AllPDFields.Add(TextPanelOption.gameObject);
                    AllInputs.Add(new InputFieldInfo(field, NewInput, typeof(float)));
                }
                else if (field.FieldType.ToString() == "UnityEngine.Vector3")
                {
                    c.AddTextPanel($"${field.Name}",
                        new RelVector2(new RelLength(1000), new RelLength(105f)),
                        new TextPanelConfig
                        {
                            Text = Name,
                            Font = TextPanelConfig.TextFont.TrajanBold,
                            Size = 46,
                            Anchor = TextAnchor.MiddleLeft
                        }, out var TextPanelOption);

                    var NewInput = new CanvasInput(
                        TextPanelOption.gameObject,
                        "IP Input",
                        MoreSaves.PanelImage,
                        new Vector2(Screen.width - TextPanelOption.gameObject.transform.position.x - 500,
                            Screen.height / 2f),
                        Vector2.zero,
                        new Rect(0, 0, 500, 60),
                        CanvasUtil.TrajanBold,
                        InputText,
                        ((Vector3)field.GetValue(EditSaveFilePlayerData)).ToString(),
                        36);
                    AllPDFields.Add(TextPanelOption.gameObject);
                    AllInputs.Add(new InputFieldInfo(field, NewInput, typeof(Vector3)));
                }
                else if (field.FieldType.ToString().Contains("List")) //string/int/vector3
                {
                    c.AddTextPanel(field.Name,
                        new RelVector2(new RelLength(1000), new RelLength(105f)),
                        new TextPanelConfig
                        {
                            Text = Name + " (Cannot Edit List)",
                            Font = TextPanelConfig.TextFont.TrajanBold,
                            Size = 46,
                            Anchor = TextAnchor.MiddleLeft,
                        }, out var ListObj);
                    AllPDFields.Add(ListObj.gameObject);
                }
            }
        }

        private static void SaveChanges(MenuSelectable obj)
        {
            UIManager.instance.UIGoToDynamicMenu(EditChooseMenu);

            foreach (var inputFieldInfo in AllInputs.Where(inputFieldInfo => inputFieldInfo.Input.GetText().Trim(' ') != ""))
            {
                if (inputFieldInfo.InputType == typeof(string))
                {
                    inputFieldInfo.Field.SetValue(EditSaveFilePlayerData, inputFieldInfo.Input.GetText());
                }
                else if (inputFieldInfo.InputType == typeof(float))
                {
                    if (float.TryParse(inputFieldInfo.Input.GetText(), out float newValue))
                    {
                        inputFieldInfo.Field.SetValue(EditSaveFilePlayerData, newValue);
                    }
                }
                else if (inputFieldInfo.InputType == typeof(Vector3))
                {
                    Vector3? newValue = StringToVector3(inputFieldInfo.Input.GetText());
                    if (newValue != null)
                    {
                        inputFieldInfo.Field.SetValue(EditSaveFilePlayerData, (Vector3) newValue);
                    }
                }
            }
            AllInputs.Clear();

            WriteToSaveFile(EditSaveFileNumber, new SaveFileData(EditSaveFilePlayerData, EditSaveFileSceneData));
        }


        private static void DoSearch(MenuSelectable obj)
        {
            //no pd feild name has space in it
            var searchText = SearchInput.GetText().ToLower().Trim(' ');

            foreach (GameObject pdField in AllPDFields)
            {
                pdField.SetActive(pdField.name.ToLower().Contains(searchText) || searchText == "");
            }

            //0 => search bar, 1=> search button hence start from 2
            int Index = 2;
            RelVector2 ItemAdvance = new RelVector2(new Vector2(0.0f, -105f));
            AnchoredPosition Start = new AnchoredPosition
            {
                ChildAnchor = new Vector2(0.5f, 1f),
                ParentAnchor = new Vector2(0.5f, 1f),
                Offset = default
            };

            foreach (GameObject pdField in AllPDFields.Where(x => x.activeInHierarchy))
            {
                (Start + ItemAdvance * new Vector2Int(Index, Index)).Reposition(pdField.gameObject
                    .GetComponent<RectTransform>());
                Index += 1;
            }

        }

        private static SaveFileData ReadFromSaveFile(int saveslot)
        {
            byte[] fileBytes = File.ReadAllBytes(Application.persistentDataPath + $"/user{saveslot}.dat");

            string json =
                (!GameManager.instance.gameConfig.useSaveEncryption
                    ? 0
                    : (!Platform.Current.IsFileSystemProtected ? 1 : 0)) == 0
                    ? Encoding.UTF8.GetString(fileBytes)
                    : Encryption.Decrypt(
                        (string) new BinaryFormatter().Deserialize(new MemoryStream(fileBytes)));

            SaveGameData data;
            try
            {
                data = JsonConvert.DeserializeObject<SaveGameData>(json, new JsonSerializerSettings()
                {
                    ContractResolver = ShouldSerializeContractResolver.Instance,
                    TypeNameHandling = TypeNameHandling.Auto,
                    ObjectCreationHandling = ObjectCreationHandling.Replace,
                    Converters = JsonConverterTypes.ConverterTypes
                });
            }
            catch (Exception ex)
            {
                MoreSaves.Instance.LogError(
                    "Failed to read save using Json.NET (GameManager::LoadGame), falling back.");
                MoreSaves.Instance.LogError(ex);
                data = JsonUtility.FromJson<SaveGameData>(json);
            }


            if (data == null)
            {
                return new SaveFileData(null, null);
            }

            var playerData = data.playerData;
            var sceneData = data.sceneData;

            return new SaveFileData(playerData, sceneData);
        }

        private static void WriteToSaveFile(int saveSlot, SaveFileData saveFileData)
        {
            var GM = GameManager.instance;
            PlayerData playerData = saveFileData.PlayerData;
            SceneData sceneData = saveFileData.SceneData;

            if (GM.profileID == saveSlot)
            {
                Logger.Log($"profile ID is {GM.profileID}");
                PlayerData.instance = GameManager.instance.playerData = HeroController.instance.playerData = saveFileData.PlayerData;
                return;
            }

            try
            {
                SaveGameData data = new SaveGameData(playerData, sceneData);

                string str3;
                try
                {
                    str3 = JsonConvert.SerializeObject(data, Formatting.Indented,
                        new JsonSerializerSettings
                        {
                            ContractResolver = ShouldSerializeContractResolver.Instance,
                            TypeNameHandling = TypeNameHandling.Auto,
                            Converters = JsonConverterTypes.ConverterTypes
                        });
                }
                catch (Exception ex)
                {
                    Modding.Logger.LogError("Failed to serialize save using Json.NET, trying fallback.");
                    Modding.Logger.LogError(ex);
                    str3 = JsonUtility.ToJson(data);
                }

                if ((!GM.gameConfig.useSaveEncryption
                    ? 0
                    : (!Platform.Current.IsFileSystemProtected ? 1 : 0)) != 0)
                {
                    string str1 = Encryption.Encrypt(str3);
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    MemoryStream memoryStream1 = new MemoryStream();
                    MemoryStream memoryStream2 = memoryStream1;
                    string str2 = str1;
                    binaryFormatter.Serialize(memoryStream2, str2);
                    byte[] array = memoryStream1.ToArray();
                    memoryStream1.Close();
                    File.WriteAllBytes(Application.persistentDataPath + $"/user{saveSlot}.dat", array);
                }
                else
                {
                    File.WriteAllBytes(Application.persistentDataPath + $"/user{saveSlot}.dat",
                        Encoding.UTF8.GetBytes(str3));
                }
            }
            catch (Exception ex)
            {
                MoreSaves.Instance.LogError(("There was an error saving the game: " + ex));
            }
        }

        private static Vector3? StringToVector3(string sVector)
        {
            if (string.IsNullOrEmpty(sVector)) return null;
            try
            {
                // Remove the parentheses
                if (sVector.StartsWith("(") && sVector.EndsWith(")"))
                {
                    sVector = sVector.Substring(1, sVector.Length - 2);
                }
                
                // split the items
                string[] sArray = sVector.Split(',');

                // store as a Vector3
                Vector3 result = new Vector3(
                    float.Parse(sArray[0]),
                    float.Parse(sArray[1]),
                    float.Parse(sArray[2]));

                return result;
            }
            catch (Exception e)
            {
                MoreSaves.Instance.Log(e);
                return null;
            }
        }

        #endregion
        
        private struct SaveFileData
        {
            public PlayerData PlayerData;
            public SceneData SceneData;

            public SaveFileData(PlayerData playerData,SceneData sceneData)
            {
                PlayerData = playerData;
                SceneData = sceneData;
            }
        }
        private struct InputFieldInfo
        {
            public FieldInfo Field;
            public CanvasInput Input;
            public Type InputType;

            public InputFieldInfo(FieldInfo field,CanvasInput input, Type inputType)
            {
                this.Field = field;
                this.Input = input;
                this.InputType = inputType;
            }
        }
    }
}