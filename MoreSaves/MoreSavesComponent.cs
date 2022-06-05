using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlobalEnums;
using InControl;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo

namespace MoreSaves
{
    internal class MoreSavesComponent : MonoBehaviour
    {
        private const int MIN_PAGES = 2;
        private const float  TRANSISTION_TIME = 0.5f;
        private const float INPUT_WINDOW = 0.4f;
        
        private string scene = Constants.MENU_SCENE;
        public static int _currentPage;
        public static int _maxPages;
        private bool _pagesHidden;
        private float _lastPageTransition;
        private float _lastInput;
        private float _firstInput;
        private int _queueRight;
        private int _queueLeft;

        private static IEnumerable<SaveSlotButton> Slots => new[]
        {
            _uim.slotOne, _uim.slotTwo, _uim.slotThree, _uim.slotFour
        };

        private static GameManager _gm => GameManager.instance;

        private static UIManager _uim => UIManager.instance;

        private void Start()
        {
            _pagesHidden = false;

            _maxPages = PlayerPrefs.GetInt("MaxPages", MIN_PAGES);

            _maxPages = Math.Max(_maxPages, MIN_PAGES);

            MoreSaves.PageLabel.text = $"Page {_currentPage + 1}/{_maxPages}";

            DontDestroyOnLoad(this);

            UnLoadHooks();
            LoadHooks();
        }
        
        private void UnLoadHooks()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= SceneChanged;
            ModHooks.SavegameSaveHook -= CheckAddMaxPages;
            ModHooks.SavegameClearHook -= CheckRemoveMaxPages;
            ModHooks.ApplicationQuitHook -= SaveFileNames;
            On.UnityEngine.UI.SaveSlotButton.PresentSaveSlot -= ChangeSaveFileText;
            On.UnityEngine.UI.SaveSlotButton.AnimateToSlotState -= FixNewSaveNumber;
            On.MappableKey.OnBindingFound -= IHateMouse1;
            On.UnityEngine.UI.SaveSlotButton.OnSubmit -= SaveSlotButton_OnSubmit;
            On.Platform.GetSaveSlotFileName -= Platform_GetSaveSlotFileName;
            On.GameManager.LoadGame -= GameManager_LoadGame;
        }

        private void LoadHooks()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneChanged;
            ModHooks.SavegameSaveHook += CheckAddMaxPages;
            ModHooks.SavegameClearHook += CheckRemoveMaxPages;
            ModHooks.ApplicationQuitHook += SaveFileNames;
            
            //reconstruct some functions to facilitate changing saves name and number
            On.UnityEngine.UI.SaveSlotButton.PresentSaveSlot += ChangeSaveFileText;
            On.MappableKey.OnBindingFound += IHateMouse1;
            On.UnityEngine.UI.SaveSlotButton.AnimateToSlotState += FixNewSaveNumber;

            // The following fixes save/load behavior for mod settings
            On.UnityEngine.UI.SaveSlotButton.OnSubmit += SaveSlotButton_OnSubmit;
            On.Platform.GetSaveSlotFileName += Platform_GetSaveSlotFileName;
            On.GameManager.LoadGame += GameManager_LoadGame;
        }


        //checks for mouse 1 being input when mapping keys and yeets it if found
        private bool IHateMouse1(On.MappableKey.orig_OnBindingFound orig, MappableKey self, PlayerAction action,
            BindingSource binding)
        {
            if (!(action.Name == MoreSaves.settings.keybinds.NextPage.Name ||
                  action.Name == MoreSaves.settings.keybinds.PreviousPage.Name)) return orig(self, action, binding);

            if (binding.Name != "LeftButton") return orig(self, action, binding);

            ReflectionHelper.GetField<MappableKey, UIButtonSkins>(self, "uibs").FinishedListeningForKey();
            action.StopListeningForBinding();
            self.AbortRebind();
            return false;
        }

        private void ChangeSaveFileText(On.UnityEngine.UI.SaveSlotButton.orig_PresentSaveSlot orig,SaveSlotButton self,
            SaveStats saveStats)
        {
            orig(self,saveStats);
            
            //change name and number
            self.locationText.text = GetSaveFileText(self,saveStats);
        }

        private string GetSaveFileText(SaveSlotButton SaveSlotButton,SaveStats saveStats)
        {
            string text;

            int slotnumber = ConvertSlotToNumber(SaveSlotButton);
            int pagenumber = _currentPage + 1;

            
            //change the save number
            SaveSlotButton.slotNumberText.GetComponent<Text>().text = ((pagenumber - 1) * 4 + slotnumber).ToString();

            if (MoreSaves.SaveSlotNames.TryGetValue(((pagenumber - 1) * 4 + slotnumber), out var newtext))
            {
                text = newtext;
            }
            else
            {
                //make the text the normal text if not present in dictionary
                text = GameManager.instance.GetFormattedMapZoneString(saveStats.mapZone)
                    .Replace("<br>", Environment.NewLine);
            }
            return text;
        }

        //I use this because the saveslobutton contains a enum that'll tell the slot number. we cant rely
        //on the text because it can change
        private int ConvertSlotToNumber(SaveSlotButton saveSlotButton)
        {
            return saveSlotButton.saveSlot switch
            {
                SaveSlotButton.SaveSlot.SLOT_1 => 1,
                SaveSlotButton.SaveSlot.SLOT_2 => 2,
                SaveSlotButton.SaveSlot.SLOT_3 => 3,
                SaveSlotButton.SaveSlot.SLOT_4 => 4,
                _ => 0,
            };
        }
        
        private void SceneChanged(Scene arg0, Scene arg1)
        {
            scene = arg1.name;
        }

        public void Update()
        {
            if (scene != Constants.MENU_SCENE) return;
            float currentTime = Time.realtimeSinceStartup;
            
            if (_uim.menuState != MainMenuState.SAVE_PROFILES)
            {
                MoreSaves.PageLabel.CrossFadeAlpha(0, 0.25f, false);

                return;
            }
            bool updateSaves = false;
            bool holdingLeft =  MoreSaves.settings.keybinds.PreviousPage.IsPressed;
            bool holdingRight = MoreSaves.settings.keybinds.NextPage.IsPressed;

            if (MoreSaves.settings.keybinds.NextPage.WasPressed && currentTime - _lastInput > 0.05f)
            {
                _firstInput = currentTime;
                _queueRight++;
            }

            if (MoreSaves.settings.keybinds.PreviousPage.WasPressed && currentTime - _lastInput > 0.05f)
            {
                _firstInput = currentTime;
                _queueLeft++;
            }

            if (_queueRight == 0 && holdingRight && currentTime - _firstInput > INPUT_WINDOW)
                _queueRight = 1;
            if (_queueLeft == 0 && holdingLeft && currentTime - _firstInput > INPUT_WINDOW)
                _queueLeft = 1;

            if (_pagesHidden || !_pagesHidden && currentTime - _lastPageTransition > TRANSISTION_TIME)
            {
                if (_queueRight > 0 && currentTime - _lastInput > INPUT_WINDOW / 2)
                {
                    _lastInput = currentTime;
                    _currentPage += _queueRight;
                    _queueRight = 0;
                    updateSaves = true;
                }

                if (_queueLeft > 0 && currentTime - _lastInput > INPUT_WINDOW / 2)
                {
                    _lastInput = currentTime;
                    _currentPage -= _queueLeft;
                    _queueLeft = 0;
                    updateSaves = true;
                }

                _currentPage %= _maxPages;

                if (_currentPage < 0) _currentPage = _maxPages - 1;

                MoreSaves.PageLabel.text = $"Page {_currentPage + 1}/{_maxPages}";
            }

            if (!_pagesHidden && updateSaves && currentTime - _lastPageTransition > TRANSISTION_TIME)
            {
                _lastPageTransition = currentTime;
                _pagesHidden = true;
                HideAllSaves();
            }

            if (_pagesHidden && currentTime - _lastInput > INPUT_WINDOW && currentTime - _lastPageTransition > TRANSISTION_TIME)
            {
                _lastPageTransition = currentTime;
                _pagesHidden = false;
                ShowAllSaves();
            }

            if (currentTime - _lastPageTransition < TRANSISTION_TIME * 2) return;

            if (_pagesHidden || Slots.All(x => x.state != UnityEngine.UI.SaveSlotButton.SlotState.HIDDEN))
                MoreSaves.PageLabel.CrossFadeAlpha(1, 0.25f, false);
            else
                MoreSaves.PageLabel.CrossFadeAlpha(0, 0.25f, false);
        }
        
        public void HideOne() => _uim.slotOne.HideSaveSlot();
        public void HideTwo() => _uim.slotTwo.HideSaveSlot();
        public void HideThree() => _uim.slotThree.HideSaveSlot();
        public void HideFour() => _uim.slotFour.HideSaveSlot();
        

        public void HideAllSaves()
        {
            Invoke(nameof(HideOne), 0);
            Invoke(nameof(HideTwo), 0);
            Invoke(nameof(HideThree), 0);
            Invoke(nameof(HideFour), 0);
        }

        public void ShowAllSaves()
        {
            MoreSaves.Instance.Log("[MoreSaves] Showing All Saves");

            foreach (SaveSlotButton s in Slots)
            {
                s._prepare(_gm);
                s.ShowRelevantModeForSaveFileState();
            }

            _uim.StartCoroutine(_uim.GoToProfileMenu());
        }

        private void CheckAddMaxPages(int x)
        {
            if (_currentPage == _maxPages - 1) _maxPages++;

            PlayerPrefs.SetInt("MaxPages", _maxPages);
        }

        private void CheckRemoveMaxPages(int x)
        {
            if
            (
                (_currentPage == _maxPages || _currentPage == _maxPages - 1) &&
                Enumerable.Range(1, 8).Any(i => File.Exists($"{Application.persistentDataPath}/user{(_maxPages - 1) * 4 + i}.dat"))
            )
                return;

            PlayerPrefs.SetInt("MaxPages", --_maxPages);
            MoreSaves.PageLabel.text = $"Page {_currentPage + 1}/{_maxPages}";
        }

        private void SaveFileNames()
        {
            ModMenu.SaveNameToFile();
        }

        // Patch profileID when a save slot button is clicked. Need it here for mods like Randomizer to save properly
        private void SaveSlotButton_OnSubmit(On.UnityEngine.UI.SaveSlotButton.orig_OnSubmit orig, SaveSlotButton self, UnityEngine.EventSystems.BaseEventData eventData)
        {
            orig(self, eventData);
            GameManager.instance.profileID = GetNewSaveSlot(GameManager.instance.profileID);
        }

        // This may not be necessary, but I will leave it here just in case
        private string Platform_GetSaveSlotFileName(On.Platform.orig_GetSaveSlotFileName orig, Platform self, int slotIndex, int usage)
        {
            slotIndex = GetNewSaveSlot(slotIndex);
            return orig(self, slotIndex, usage);
        }

        // Patch profileID before loading a save
        private void GameManager_LoadGame(On.GameManager.orig_LoadGame orig, GameManager self, int saveSlot, Action<bool> callback)
        {
            saveSlot = GetNewSaveSlot(saveSlot);
            orig(self, saveSlot, callback);
        }

        private IEnumerator FixNewSaveNumber(On.UnityEngine.UI.SaveSlotButton.orig_AnimateToSlotState orig, SaveSlotButton self, SaveSlotButton.SlotState nextState)
        {
            yield return orig(self, nextState);
            
            //fix file numbers for empty slots
            if (nextState == SaveSlotButton.SlotState.EMPTY_SLOT && self.slotNumberText != null)
            {
                int slotnumber = ConvertSlotToNumber(self);
                Text slotNumberText = self.slotNumberText.GetComponent<Text>();
                if (slotNumberText != null)
                {
                    slotNumberText.text = (_currentPage * 4 + slotnumber).ToString();
                }
                
            }
        }

        private int GetNewSaveSlot(int x)
        {
            x = x % 4 == 0 ? 4 : x % 4;

            return _currentPage * 4 + x;
        }
    }
}