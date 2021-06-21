﻿using InControl;
using Modding.Converters;
using Newtonsoft.Json;

namespace MoreSaves
{
    public class Settings
    {
        [JsonConverter(typeof(PlayerActionSetConverter))]
        public KeyBinds keybinds = new KeyBinds();
    }

    public class KeyBinds : PlayerActionSet
    {
        public PlayerAction NextPage;
        public PlayerAction PreviousPage;

        public KeyBinds()
        {
            NextPage = CreatePlayerAction("NextPage");
            PreviousPage = CreatePlayerAction("PreviousPage");
            DefaultBinds();
        }

        private void DefaultBinds()
        {
            NextPage.AddDefaultBinding(Key.RightBracket);
            PreviousPage.AddDefaultBinding(Key.LeftBracket);
        }
    }
}