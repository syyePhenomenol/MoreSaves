using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Modding.Menu;
using Modding.Menu.Config;
using UnityEngine;
using UnityEngine.UI;

namespace MoreSaves
{
    internal class ModMenu
    {
        private static MenuScreen AdditionalMenu;
        private static MenuScreen MainMenu;
        public static MenuOptionHorizontal AutoBackupSelector;

        public static MenuScreen CreateCustomMenu(MenuScreen modListMenu)
        {
            if(!Directory.Exists(MoreSaves.BackupFolder))
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
                .SetDefaultNavGraph(new ChainedNavGraph())
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
                            }
                        ).AddKeybind(
                            "PreviousPage",
                            MoreSaves.settings.keybinds.PreviousPage,
                            new KeybindConfig
                            {
                                Label = "Go to Previous Page",
                                CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu)
                            }
                        ).AddHorizontalOption(
                            "AutoBackup",
                            new HorizontalOptionConfig
                            {
                                Label = "Enable Auto Backup",
                                Options = new string []{"No","Yes"},
                                ApplySetting = (_, i) =>
                                {
                                    MoreSaves.settings.AutoBackup = i != 0;
                                },
                                RefreshSetting = (s, _) =>s.optionList.SetOptionTo(MoreSaves.settings.AutoBackup ? 1 : 0),
                                CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                                Style = HorizontalOptionStyle.VanillaStyle
                            }, out AutoBackupSelector)
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

                            }).AddMenuButton(
                            "Remove Last Page (if redundant)",
                            new MenuButtonConfig
                            {
                                Label = "Remove Last Page (if redundant)",
                                SubmitAction = _ =>
                                {
                                    if (Enumerable.Range(1, 8).Any(i =>
                                        File.Exists(
                                            $"{Application.persistentDataPath}/user{(MoreSavesComponent._maxPages - 1) * 4 + i}.dat")))
                                        return;
                                    PlayerPrefs.SetInt("MaxPages", --MoreSavesComponent._maxPages);
                                    MoreSaves.PageLabel.text =
                                        $"Page {MoreSavesComponent._currentPage + 1}/{MoreSavesComponent._maxPages}";
                                },
                                CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),

                            }).AddMenuButton(
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
                                SubmitAction = _ => UIManager.instance.UIGoToDynamicMenu(AdditionalMenu),
                                CancelAction = _ => UIManager.instance.UIGoToDynamicMenu(modListMenu),
                                Proceed = true,

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
            AdditionalMenu = CreateAdditionalMenu(MainMenu);
            return MainMenu;
        }

        private static MenuScreen CreateAdditionalMenu(MenuScreen mainmenu)
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


        //My reason for doing this instead of a simple foreach is ordering. with foreach 10 comes before 2 which is wrong
        private static SortedDictionary<string,DateTime> SaveFiles = new SortedDictionary<string, DateTime>();
        
        private static void AddRestoreSaveFileContent(ContentArea c)
        {
            foreach (string saveFile in Directory.EnumerateFiles(MoreSaves.BackupFolder))
            {
                string filename = Path.GetFileName(saveFile);

                if (!IsSaveFile(filename)) continue;
                
                DateTime lastmodified = File.GetLastWriteTime(saveFile);

                string filenumber = filename.Replace("user", "").Replace(".dat","");

                if (filenumber.Length == 1) filenumber = $"0{filenumber}";
                
                SaveFiles.Add(filenumber,lastmodified);
            }
            
            foreach (KeyValuePair<string, DateTime> BackedUpFiles in SaveFiles)
            {
                MoreSaves.Instance.Log(BackedUpFiles.Key);
                string lastsaved_date = $"{BackedUpFiles.Value.Day}/{BackedUpFiles.Value.Month}/{BackedUpFiles.Value.Year}";
                string lastsaved_time = $"{BackedUpFiles.Value.Hour}:{BackedUpFiles.Value.Minute}:{BackedUpFiles.Value.Second}";

                string lastsaved = $"This backup is from: {lastsaved_time} on {lastsaved_date}";

                string filenumber = BackedUpFiles.Key;
                if (filenumber[0] == '0') filenumber = filenumber.Replace("0", "");

                MoreSaves.Instance.Log(filenumber);
                string source = $"{MoreSaves.BackupFolder}/user{filenumber}";
                string dest = $"{MoreSaves.SavesFolder}/user{filenumber}";

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
            UnityEngine.Object.Destroy(AdditionalMenu);
            AdditionalMenu = CreateAdditionalMenu(MainMenu);
        }
        public static void BackupSaves()
        {
            if(!Directory.Exists(MoreSaves.BackupFolder))
                Directory.CreateDirectory(MoreSaves.BackupFolder);
            
            foreach(string saveFile in Directory.EnumerateFiles(MoreSaves.SavesFolder))
            {
                string filename = Path.GetFileName(saveFile);

                string dest = MoreSaves.BackupFolder +"/"+filename;
                
                if (!IsSaveFile(filename)) continue;
                
                //copy it in
                File.Copy( saveFile, dest, true);

                Debug.Log( "Copied " + saveFile + " to " + dest );
            }
        }
    }
}
