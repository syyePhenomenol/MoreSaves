using System;
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
        private static MenuScreen menu;
        public static MenuOptionHorizontal AutoBackupSelector;

        public static MenuScreen CreateCustomMenu(MenuScreen modListMenu)
        {
            if(!Directory.Exists(MoreSaves.BackupFolder))
                Directory.CreateDirectory(MoreSaves.BackupFolder);

            var mainmenu = new MenuBuilder(UIManager.instance.UICanvas.gameObject, "MoreSavesMenu")
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
                                SubmitAction = _ => UIManager.instance.UIGoToDynamicMenu(menu),
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
            menu = new MenuBuilder("Restore Saves")
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
                    new RelLength((Directory.GetFiles(MoreSaves.BackupFolder).Length * (210f)) + 180f),
                    RegularGridLayout.CreateVerticalLayout(105f),
                    AddModMenuContent
                )).Build();
            
            

            return mainmenu;
        }
        
        private static void AddModMenuContent(ContentArea c)
        {
            foreach(string saveFile in Directory.EnumerateFiles(MoreSaves.BackupFolder))
            {
                string filename = Path.GetFileName(saveFile);

                DateTime lastmodified = File.GetLastWriteTime(saveFile);

                string lastsaved_date = $"{lastmodified.Day}/{lastmodified.Month}/{lastmodified.Year}";
                string lastsaved_time = $"{lastmodified.Hour}:{lastmodified.Minute}:{lastmodified.Second}";

                string lastsaved = $"This backup is from: {lastsaved_time} on {lastsaved_date}";
                    
                if (!IsSaveFile(filename)) continue;
                
                string dest = MoreSaves.SavesFolder +"/"+filename;

                c.AddMenuButton(
                    $"Restore {filename}",
                    new MenuButtonConfig
                    {
                        Label = $"Restore Save {filename.Replace("user","").Replace(".dat","")}",
                        SubmitAction = _ => File.Copy(saveFile, dest, true),
                        Style = MenuButtonStyle.VanillaStyle

                    }).AddTextPanel("lastsave",
                    new RelVector2(new Vector2(1000, 30)),
                    new TextPanelConfig
                    {
                        Anchor = TextAnchor.UpperCenter,
                        Size = 30,
                        Font = TextPanelConfig.TextFont.TrajanBold,
                        Text = lastsaved,
                    });
            }
            c.AddTextPanel(
                "Refresh Pls",
                new RelVector2(new Vector2(800, 180)),
                new TextPanelConfig
                {
                    Anchor = TextAnchor.MiddleCenter,
                    Size = 45,
                    Font = TextPanelConfig.TextFont.TrajanBold,
                    Text = "Note: You may need to open and close a save to see backups made in this session",
                });
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


        private static void BackupSaves(MenuButton obj) => BackupSaves();
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