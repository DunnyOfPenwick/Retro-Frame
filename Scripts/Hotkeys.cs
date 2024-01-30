// Project:     Retro-Frame for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: January 2024

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Utility;


namespace RetroFrame
{

    public class Hotkeys
    {

        static readonly HashSet<KeyCode> usableModifierKeyCodes = new HashSet<KeyCode>()
        {
            KeyCode.LeftShift, KeyCode.RightShift,
            KeyCode.LeftAlt, KeyCode.RightAlt,
            KeyCode.LeftControl, KeyCode.RightControl,
        };


        public class Button
        {
            public Panel Panel;
            public TextLabel CharLabel;
            public Panel IconContainer;
            public Panel Icon;
            public Panel AnimationPanel;
            public TextLabel ItemCountLabel;
            public TextLabel DescriptionLabel;
        }


        public class Entry
        {
            public KeyCode KeyPress = KeyCode.None;
            public EffectBundleSettings Spell;
            public DaggerfallUnityItem Item;
            public RetroFrameMod.HotkeyCallbackInfo Callback;
        }


        //Multiple hotkey groups are allowed and can be cycled through by the player.
        private static int _currentGroupIndex;
        public static List<Entry[]> Groups = new List<Entry[]>();
        public static int CurrentGroupIndex
        {
            get { return _currentGroupIndex; }
            set
            {
                _currentGroupIndex = value;
                if (_currentGroupIndex >= Groups.Count)
                    _currentGroupIndex = 0;
                else if (_currentGroupIndex < 0)
                    _currentGroupIndex = Math.Max(0, Groups.Count - 1);

                if (Groups.Count != 0)
                    Entries = Groups[_currentGroupIndex];
            }
        }

        public static Entry[] Entries { get; private set; } = new Entry[0]; //Entries for the currently selected hotkey group

        public static Button[] Buttons = new Button[10]; //The UI elements to display hotkeys on 

        public static KeyCode PriorGroupKey = KeyCode.Minus;
        public static KeyCode NextGroupKey = KeyCode.Equals;

        readonly static EffectBundleSettings emptySpell = new EffectBundleSettings();
        readonly static Dictionary<ulong, string> legacyPowersCache = new Dictionary<ulong, string>();

        static bool isEventListenerInitialized;

        static DaggerfallUnityItem hoverItem; //The hover item returned from the DaggerfallInventoryWindow

        static ImageData magicAnimation;

        static float lastHotkeyActivationTime;
        static int lastHotkeyActivationIndex;

        //Keycodes we should avoid trying to bind to
        static readonly HashSet<KeyCode> daggerfallKeys = new HashSet<KeyCode>();

        static readonly Color charLabelDefaultColor = new Color(0.6f, 0.6f, 0.6f, 1);
        static readonly Color charLabelControlColor = new Color(0.8f, 0.5f, 0);
        static readonly Color charLabelAltColor = new Color(0f, 0.6f, 0.7f);


        /// <summary>
        /// Called soon after mod is initialized.
        /// </summary>
        public static void Setup()
        {
            magicAnimation = ImageReader.GetImageData("TEXTURE.434", 5, 0, true, false, true);

            //Initialize hotkey groups with empty or appropriate values.
            for (int i = 0; i < RetroFrameMod.HotkeyGroups; ++i)
            {
                Entries = new Entry[10];
                Groups.Add(Entries);

                for (int j = 0; j < Entries.Length; ++j)
                {
                    Entries[j] = new Entry();

                    //Bind default hotkey characters to the digit characters initially.
                    KeyCode k = KeyCode.Alpha0 + (j == 9 ? 0 : j + 1);
                    BindChar(j, k);
                }
            }

            CurrentGroupIndex = 0;
        }



        /// <summary>
        /// Checks if any hotkeys are down and updates hotkey display information.
        /// </summary>
        public static void Refresh()
        {
            KeyCode k = GetKeyPress();

            //Keypress must be checked every frame
            if (k != KeyCode.None)
                if (!CheckForGroupSwitch(k))
                    CheckHotkeys(k);

            //Only perform validation occasionally, for efficiency.
            if (Time.frameCount % 5 == 0)
            {
                InitInventoryEventHandler();
                RefreshSystemKeybinds();
                Validate();
            }

            CheckFlashErrorPanels();

        }

        /// <summary>
        /// Attaches item/spell/custom info to a hotkey.
        /// Clears the hotkey if the target argument is null.
        /// </summary>
        public static void Set(int index, System.Object target)
        {
            if (index < 0 || index >= Entries.Length)
                return;

            //Clear existing hotkey record
            Entries[index].Spell = emptySpell;
            Entries[index].Item = null;
            Entries[index].Callback = null;

            //Clear existing UI record
            Buttons[index].Panel.ToolTipText = "";
            Buttons[index].IconContainer.Enabled = false;
            Buttons[index].Icon.BackgroundTexture = null;
            Buttons[index].ItemCountLabel.Enabled = false;
            Buttons[index].AnimationPanel.AnimatedBackgroundTextures = null;
            Buttons[index].DescriptionLabel.Text = "";
            Buttons[index].DescriptionLabel.TextColor = Color.gray;

            if (target is EffectBundleSettings spell)
                Entries[index].Spell = spell;
            else if (target is DaggerfallUnityItem item)
                Entries[index].Item = item;
            else if (target is RetroFrameMod.HotkeyCallbackInfo callback)
                Entries[index].Callback = callback;
        }


        /// <summary>
        /// Plays a note of a pitch corresponding to current hotkeys group, if the option is enabled
        /// in mod settings.
        /// </summary>
        public static void PlayGroupSwitchNote()
        {
            if (!RetroFrameMod.SilentGroupSwitching)
            {
                //Play appropriate note indicating switch to new hotkey group.
                float[] pitches = new float[] { 24f / 24, 27f / 24, 30f / 24, 32f / 24, 36f / 24, 40f / 24, 45f / 24 };
                float pitch = pitches[(CurrentGroupIndex % pitches.Length)];

                float volume = 0.7f;
                PlayerEnterExit where = GameManager.Instance.PlayerEnterExit;
                if (where.IsPlayerInsideDungeon || where.IsPlayerInsideDungeonCastle || where.IsPlayerInHolyPlace)
                    volume = 0.4f; //keep it quieter in dungeons
                else if (where.IsPlayerInsideOpenShop || where.IsPlayerInsideTavern)
                    volume = 1f; //it tends to be loud in there

                RetroFrameMod.AudioSource.volume = volume;
                RetroFrameMod.AudioSource.pitch = pitch;

                RetroFrameMod.AudioSource.PlayOneShot(RetroFrameMod.Mod.GetAsset<AudioClip>("Note"));
            }
        }


        /// <summary>
        /// Gets key pressed this frame, and any key modifiers.
        /// </summary>
        static KeyCode GetKeyPress()
        {
            //KeyCode k = InputManager.Instance.GetAnyKeyDownIgnoreAxisBinds(true);
            KeyCode k = DaggerfallUI.Instance.LastKeyCode;

            if (k == KeyCode.None || k == KeyCode.Mouse0 || k == KeyCode.Mouse1)
                return KeyCode.None; //ignoring standard mouse buttons
            else if (usableModifierKeyCodes.Contains(k))
                return KeyCode.None; //ignore lone modifier keys
            else if (InputManager.Instance.IsUsedInAxisBinding(k))
                return KeyCode.None;

            int modifiers = (int)DaggerfallUI.Instance.LastKeyModifiers;

            if (0 != (modifiers & (int)HotkeySequence.KeyModifiers.LeftAlt))
                k = InputManager.Instance.GetComboCode(KeyCode.LeftAlt, k);
            else if (0 != (modifiers & (int)HotkeySequence.KeyModifiers.RightAlt))
                k = InputManager.Instance.GetComboCode(KeyCode.RightAlt, k);
            else if (0 != (modifiers & (int)HotkeySequence.KeyModifiers.LeftCtrl))
                k = InputManager.Instance.GetComboCode(KeyCode.LeftControl, k);
            else if (0 != (modifiers & (int)HotkeySequence.KeyModifiers.RightCtrl))
                k = InputManager.Instance.GetComboCode(KeyCode.RightControl, k);

            return k;
        }



        static void CheckFlashErrorPanels()
        {
            foreach (Button button in Buttons)
                UpdateFlashErrorPanel(button.Panel);
            
            UpdateFlashErrorPanel(OverlayPanel.PriorHotkeyGroupButton);
            UpdateFlashErrorPanel(OverlayPanel.NextHotkeyGroupButton);
        }


        static void UpdateFlashErrorPanel(Panel containerPanel)
        {
            for (int i = 0; i < containerPanel.Components.Count; ++i)
            {
                BaseScreenComponent comp = containerPanel.Components[i];
                if ("flasher".Equals(comp.Tag))
                    if (Time.realtimeSinceStartup - 0.25f > comp.AnimationDelayInSeconds)
                        containerPanel.Components.Remove(comp);
            }
        }


        /// <summary>
        /// Check if one of the hotkey group switching keycodes was used.
        /// Also check for new group switch keybinds.
        /// Returns true if a switch key was hit or bound.
        /// </summary>
        static bool CheckForGroupSwitch(KeyCode keyPress)
        {
            if (Groups.Count < 2)
                return false;

            //Check for new key binding attempt
            bool isHoveringOverSwapButton = OverlayPanel.PriorHotkeyGroupButton.MouseOverComponent ||
                                            OverlayPanel.NextHotkeyGroupButton.MouseOverComponent;
            
            if (isHoveringOverSwapButton)
            {
                if (!daggerfallKeys.Contains(keyPress))
                {
                    if (OverlayPanel.NextHotkeyGroupButton.MouseOverComponent)
                        NextGroupKey = keyPress;
                    else
                        PriorGroupKey = keyPress;
                }
                else
                {
                    Panel panel = OverlayPanel.PriorHotkeyGroupButton;
                    if (OverlayPanel.NextHotkeyGroupButton.MouseOverComponent)
                        panel = OverlayPanel.NextHotkeyGroupButton;
                    CreateFlashErrorPanel(panel);
                }
            }
            else
            {
                //Check if group swap buttons have been activated
                if (keyPress == NextGroupKey)
                    ++CurrentGroupIndex;
                else if (keyPress == PriorGroupKey)
                    --CurrentGroupIndex;
                else
                    return false;

                PlayGroupSwitchNote();
            }

            return true;
        }


        /// <summary>
        /// Check if a hotkey character was pressed.
        /// If so, either:
        ///    -bind the key to a hotkey if the mouse cursor is over the hotkeys panel
        ///    -set the hotkey action, if in inventory window or spellbook window
        ///    -activate the hotkey action if game not paused
        /// </summary>
        static void CheckHotkeys(KeyCode keyPress)
        {
            if (Groups.Count < 1)
                return; //hotkeys disabled

            //If user is hovering over hotkey panel area, check for keybind attempts.
            if (CheckIsBindingKey(keyPress))
                return;

            //Is a hotkey being pressed?
            for (int i = 0; i < Entries.Length; ++i)
            {
                if (keyPress == Entries[i].KeyPress)
                {
                    if (GameManager.IsGamePaused)
                        GatherHotkeyEntry(i);
                    else //if (DaggerfallUI.UIManager.TopWindow == DaggerfallUI.Instance.DaggerfallHUD)
                        ActivateHotkey(i);
                }
            }

        }

        /// <summary>
        /// Check if mouse cursor is over hotkey panel and a character key is pressed.
        /// Returns true if over hotkey panel.
        /// </summary>
        static bool CheckIsBindingKey(KeyCode keyPress)
        {
            int index = OverlayPanel.HoveredHotkeyButtonIndex;

            if (index < 0)
                return false; //Not hovering over hotkeys panel

            //Ignore keystrokes matching the group swap key binds.
            if (keyPress == NextGroupKey || keyPress == PriorGroupKey)
                return true;

            //Flash error panel if attempting to bind keys already used for Daggerfall action keys.
            if (daggerfallKeys.Contains(keyPress))
            {
                CreateFlashErrorPanel(Buttons[index].Panel);
                return true;
            }

            BindChar(index, keyPress);

            return true;
        }


        public static string GetKeyText(KeyCode keyPress, bool shortened)
        {
            string text = ControlsConfigManager.Instance.GetButtonText(keyPress, true);

            if (!shortened || text.Length == 1)
            {
                return text;
            }
            else
            {
                (_, KeyCode k) = InputManager.Instance.GetCombo(keyPress);

                if (k >= KeyCode.Alpha0 && k <= KeyCode.Alpha9)
                    return ((char)('0' + k - KeyCode.Alpha0)).ToString();
                else if (k >= KeyCode.Keypad0 && k <= KeyCode.Keypad9)
                    return ((char)('0' + k - KeyCode.Keypad0)).ToString();
                else if (text.StartsWith("KPAD") || text.StartsWith("KPD"))
                    return text.Substring(4);
                else if (text.StartsWith("Keypad"))
                    return text.Substring(6);
                else
                    return text;
            }
        }


        /// <summary>
        /// Sets the hotkey character for the specified hotkey index.
        /// </summary>
        static void BindChar(int index, KeyCode keyPress)
        {
            if (index >= Entries.Length || index < 0)
                return;

            Entries[index].KeyPress = keyPress;
        }


        static void CreateFlashErrorPanel(Panel panel)
        {
            Panel flashPanel = new Panel();
            panel.Components.Add(flashPanel);
            flashPanel.Tag = "flasher";
            flashPanel.Size = panel.Size;
            flashPanel.AutoSize = AutoSizeModes.ScaleToFit;
            flashPanel.HorizontalAlignment = HorizontalAlignment.Center;
            flashPanel.VerticalAlignment = VerticalAlignment.Middle;
            flashPanel.BackgroundTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("Invalid");

            flashPanel.AnimationDelayInSeconds = Time.realtimeSinceStartup; //storing creation time here
        }


        /// <summary>
        /// Gather item/spell info for hovered item and attach it to the hotkey specified by the index argument.
        /// </summary>
        static void GatherHotkeyEntry(int index)
        {
            IUserInterfaceWindow window = DaggerfallUI.Instance.UserInterfaceManager.TopWindow;
            if (window == null)
                return;

            if (window is DaggerfallInventoryWindow)
                GatherHotkeyInventory(index, window as DaggerfallInventoryWindow);
            else if (window is DaggerfallSpellBookWindow)
                GatherHotkeyFromSpellbook(index, window as DaggerfallSpellBookWindow);
        }


        /// <summary>
        /// Determine what item is hovered over in the inventory window and attach it to the hotkey
        /// specified by the index argument.
        /// </summary>
        static void GatherHotkeyInventory(int hotkeyIndex, DaggerfallInventoryWindow window)
        {
            //hoverItem comes from DaggerfallInventoryWindow, recieved by an event handler.
            //This doesn't always work right.
            //'hoverItem' was the last item hovered over, but the player may have moved the mouse cursor since then.
            if (hoverItem != null)
                Set(hotkeyIndex, hoverItem);
        }


        /// <summary>
        /// Use reflection to inspect internal members of the DaggerfallSpellBookWindow to determine what
        /// spell is currently hovered, if any, and apply it to the hotkey specified by the index argument.
        /// </summary>
        static void GatherHotkeyFromSpellbook(int hotkeyIndex, DaggerfallSpellBookWindow window)
        {
            //The DFU code suggests the spellbook window can be used for buying as well.  Will add check just to be sure.
            FieldInfo info = window.GetType().GetField("buyMode", BindingFlags.NonPublic | BindingFlags.Instance);
            if (info == null)
                return;
            bool buyMode = (bool)info.GetValue(window);
            if (buyMode)
                return;

            info = window.GetType().GetField("spellsListBox", BindingFlags.NonPublic | BindingFlags.Instance);
            if (info == null)
                return;

            ListBox listBox = (ListBox)info.GetValue(window);
            info = listBox.GetType().GetField("listItems", BindingFlags.NonPublic | BindingFlags.Instance);
            if (info == null)
                return;

            List<ListBox.ListItem> list = (List<ListBox.ListItem>)info.GetValue(listBox);

            int index = -1;
            int i = 0;
            foreach (ListBox.ListItem item in list)
            {
                //If an item in the spellbook list has highlighted colors, the player must be hovering over it.
                Color c = item.textLabel.TextColor;
                if (c == item.highlightedTextColor || c == item.highlightedSelectedTextColor || c == item.highlightedDisabledTextColor)
                {
                    index = i;
                    break;
                }
                i++;
            }

            if (index >= 0)
            {
                EffectBundleSettings[] spellbook = GameManager.Instance.PlayerEntity.GetSpells();
                Set(hotkeyIndex, spellbook[index]);
            }
            else
            {
                Set(hotkeyIndex, null);  //clear the hotkey
            }

        }


        /// <summary>
        /// Activate action bound to hotkey unless we are in the process of changing weapons.
        /// </summary>
        static void ActivateHotkey(int index)
        {
            //No activations allowed until equipment change is finished.
            WeaponManager wm = GameManager.Instance.WeaponManager;
            if (wm.EquipCountdownLeftHand > 0 || wm.EquipCountdownRightHand > 0)
                return; 

            lastHotkeyActivationTime = Time.realtimeSinceStartup;
            lastHotkeyActivationIndex = index;
            if (Entries[index].Item != null)
                ActivateItem(Entries[index].Item);
            else if (Entries[index].Spell.Name != null)
                ActivateSpell(Entries[index].Spell);
            else if (Entries[index].Callback != null)
                ActivateCallback(Entries[index].Callback, index);
        }



        /// <summary>
        /// Modified version of DaggerfallInventory.UseItem()
        /// </summary>
        static void ActivateItem(DaggerfallUnityItem item)
        {
            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;

            IUserInterfaceManager uiManager = DaggerfallUI.UIManager;

            ItemCollection collection = playerEntity.Items;

            // Allow item to handle its own use.
            if (item.UseItem(collection))
                return;

            const int noSpellsTextId = 12;

            // Handle quest items on use clicks
            if (item.IsQuestItem)
            {
                // Get the quest this item belongs to
                Quest quest = QuestMachine.Instance.GetQuest(item.QuestUID);
                if (quest == null)
                    return;

                // Get the Item resource from quest
                Item questItem = quest.GetItem(item.QuestItemSymbol);

                // Use quest item
                if (!questItem.UseClicked && questItem.ActionWatching)
                {
                    questItem.UseClicked = true;

                    // Non-parchment and non-clothing items pop back to HUD so quest system has first shot at a custom click action in game world
                    // This is usually the case when actioning most quest items (e.g. a painting, bell, holy item, etc.)
                    // But when clicking a parchment or clothing item, this behaviour is usually incorrect (e.g. a letter to read)
                    if (!questItem.DaggerfallUnityItem.IsParchment && !questItem.DaggerfallUnityItem.IsClothing)
                    {
                        return;
                    }
                }

                // Check for an on use value
                if (questItem.UsedMessageID != 0)
                {
                    // Display the message popup
                    quest.ShowMessagePopup(questItem.UsedMessageID, true);
                }
            }

            // Try to handle use with a registered delegate
            if (DaggerfallUnity.Instance.ItemHelper.GetItemUseHandler(item.TemplateIndex, out ItemHelper.ItemUseHandler itemUseHandler))
            {
                if (itemUseHandler(item, collection))
                    return;
            }

            if (HasOnUseEnchantment(item))
            {
                //Try to activate 'On Use' magic items.
                GameManager.Instance.PlayerEffectManager.DoItemEnchantmentPayloads(EnchantmentPayloadFlags.Used, item, collection);
            }
            else if (item.ItemGroup == ItemGroups.Books && !item.IsArtifact)
            {
                DaggerfallUI.Instance.BookReaderWindow.OpenBook(item);
                if (DaggerfallUI.Instance.BookReaderWindow.IsBookOpen)
                {
                    DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenBookReaderWindow);
                }
                else
                {
                    var messageBox = new DaggerfallMessageBox(uiManager);
                    messageBox.SetText(TextManager.Instance.GetLocalizedText("bookUnavailable"));
                    messageBox.ClickAnywhereToClose = true;
                    uiManager.PushWindow(messageBox);
                }
            }
            else if (item.IsPotion)
            {   // Handle drinking magic potions
                GameManager.Instance.PlayerEffectManager.DrinkPotion(item);
                collection.RemoveOne(item);
            }
            else if (item.IsPotionRecipe)
            {
                // TODO: There may be other objects that result in this dialog box, but for now I'm sure this one says it.
                // -IC122016
                DaggerfallMessageBox cannotUse = new DaggerfallMessageBox(uiManager);
                cannotUse.SetText(TextManager.Instance.GetLocalizedText("cannotUseThis"));
                cannotUse.ClickAnywhereToClose = true;
                cannotUse.Show();
            }
            else if ((item.IsOfTemplate(ItemGroups.MiscItems, (int)MiscItems.Map) ||
                      item.IsOfTemplate(ItemGroups.Maps, (int)Maps.Map)) && collection != null)
            {
                //do nothing
            }
            else if (item.TemplateIndex == (int)MiscItems.Spellbook)
            {
                if (playerEntity.SpellbookCount() == 0)
                {
                    // Player has no spells
                    TextFile.Token[] textTokens = DaggerfallUnity.Instance.TextProvider.GetRSCTokens(noSpellsTextId);
                    DaggerfallMessageBox noSpells = new DaggerfallMessageBox(uiManager);
                    noSpells.SetTextTokens(textTokens);
                    noSpells.ClickAnywhereToClose = true;
                    noSpells.Show();
                }
                else
                {
                    // Show spellbook
                    DaggerfallUI.UIManager.PostMessage(DaggerfallUIMessages.dfuiOpenSpellBookWindow);
                }
            }
            else if (item.ItemGroup == ItemGroups.Drugs && collection != null)
            {
                // Drug poison IDs are 136 through 139. Template indexes are 78 through 81, so add to that.
                FormulaHelper.InflictPoison(playerEntity, playerEntity, (Poisons)item.TemplateIndex + 66, true);
                collection.RemoveItem(item);
            }
            else if (item.IsLightSource)
            {
                if (item.currentCondition > 0)
                {
                    if (playerEntity.LightSource == item)
                        playerEntity.LightSource = null;
                    else
                        playerEntity.LightSource = item;
                }
                else
                    DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("lightEmpty"), false, item);
            }
            else if (item.ItemGroup == ItemGroups.UselessItems2 && item.TemplateIndex == (int)UselessItems2.Oil && collection != null)
            {
                DaggerfallUnityItem lantern = collection.GetItem(ItemGroups.UselessItems2, (int)UselessItems2.Lantern, allowQuestItem: false);
                if (lantern != null && lantern.currentCondition <= lantern.maxCondition - item.currentCondition)
                {   // Re-fuel lantern with the oil.
                    lantern.currentCondition += item.currentCondition;
                    collection.RemoveItem(item.IsAStack() ? collection.SplitStack(item, 1) : item);
                    DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("lightRefuel"), false, lantern);
                }
                else
                    DaggerfallUI.MessageBox(TextManager.Instance.GetLocalizedText("lightFull"), false, lantern);
            }
            else if (item.ItemGroup == ItemGroups.Transportation)
            {
                TransportManager transportManager = GameManager.Instance.TransportManager;
                TransportModes mode = transportManager.TransportMode;

                bool cart = (item.TemplateIndex == (int)Transportation.Small_cart);
                bool horse = (item.TemplateIndex == (int)Transportation.Horse);

                if (cart || horse)
                {
                    bool isOutside = !GameManager.Instance.PlayerEnterExit.IsPlayerInside;

                    if (mode == TransportModes.Cart || mode == TransportModes.Horse)
                        transportManager.TransportMode = TransportModes.Foot;
                    else if (isOutside && mode == TransportModes.Foot)
                        transportManager.TransportMode = cart ? TransportModes.Cart : TransportModes.Horse;
                }
            }
            else if (playerEntity.ItemEquipTable.GetEquipSlot(item) != EquipSlots.None)
            {
                //Equip/unequip an equippable item.
                //There  is additional logic associated with equipping/unequipping items, such as equip delays.
                //For that reason, equip/unequip will be performed through the DaggerfallInventoryWindow.

                DaggerfallInventoryWindow window = DaggerfallUI.Instance.InventoryWindow;

                //Call OnPush() so the inventory window can initialize itself to a proper state.
                MethodInfo onPush = window.GetType().GetMethod("OnPush", BindingFlags.Instance | BindingFlags.Public);
                onPush.Invoke(window, new object[] { });

                MethodInfo unequip = window.GetType().GetMethod("UnequipItem", BindingFlags.Instance | BindingFlags.NonPublic);

                if (playerEntity.ItemEquipTable.IsEquipped(item))
                {
                    unequip.Invoke(window, new object[] { item, false });
                }
                else
                {
                    if (item.ItemGroup == ItemGroups.Weapons)
                    {
                        EquipSlots slot = GameManager.Instance.WeaponManager.UsingRightHand ? EquipSlots.RightHand : EquipSlots.LeftHand;
                        DaggerfallUnityItem current = playerEntity.ItemEquipTable.GetItem(slot);
                        if (current != null)
                            unequip.Invoke(window, new object[] { current, false });
                    }
                    MethodInfo equip = window.GetType().GetMethod("EquipItem", BindingFlags.Instance | BindingFlags.NonPublic);
                    equip.Invoke(window, new object[] { item });
                }

                //Call OnPop() so weapon arming delays and other tasks are handled.
                MethodInfo onPop = window.GetType().GetMethod("OnPop", BindingFlags.Instance | BindingFlags.Public);
                onPop.Invoke(window, new object[] { });
            }

        }


        static void ActivateSpell(EffectBundleSettings spell)
        {
            // Lycanthrope spells are free
            bool noSpellPointCost = spell.Tag == PlayerEntity.lycanthropySpellTag;

            // Assign to player effect manager as ready spell
            DaggerfallEntityBehaviour caster = GameManager.Instance.PlayerEntityBehaviour;

            EntityEffectBundle effectBundle = new EntityEffectBundle(spell, caster);

            EntityEffectManager playerEffectManager = GameManager.Instance.PlayerEffectManager;

            playerEffectManager.SetReadySpell(effectBundle, noSpellPointCost);
        }


        static void ActivateCallback(RetroFrameMod.HotkeyCallbackInfo callback, int index)
        {
            callback.Callback(callback.Description, index);
        }


        /// <summary>
        /// The InventoryWindow gets created multiple times during game startup.
        /// Wait until the window is first opened before attaching event handler to the inventory window.
        /// </summary>
        static void InitInventoryEventHandler()
        {
            if (!isEventListenerInitialized)
            {
                if (DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallInventoryWindow)
                {
                    DaggerfallUI.Instance.InventoryWindow.OnItemHover += OnItemHoverHandler;
                    isEventListenerInitialized = true;
                }
            }
        }



        /// <summary>
        /// Gets the current collection of daggerfall action key bindings.
        /// </summary>
        static void RefreshSystemKeybinds()
        {
            daggerfallKeys.Clear();

            foreach (InputManager.Actions action in Enum.GetValues(typeof(InputManager.Actions)))
            {
                //Get both primary and any secondary keybindings.
                daggerfallKeys.Add(InputManager.Instance.GetBinding(action, true));
                daggerfallKeys.Add(InputManager.Instance.GetBinding(action, false));
            }

            if (daggerfallKeys.Contains(PriorGroupKey))
                PriorGroupKey = KeyCode.None;

            if (daggerfallKeys.Contains(NextGroupKey))
                NextGroupKey = KeyCode.None;
        }


        /// <summary>
        /// Make sure all hotkeyed items/spells/custom still exist and refresh their display information.
        /// </summary>
        static void Validate()
        {
            //Update tooltip text for group swap buttons.
            string keyText = GetKeyText(PriorGroupKey, false);
            OverlayPanel.PriorHotkeyGroupButton.ToolTipText = $"[{keyText}]\r{Text.TapNewKey}";

            keyText = GetKeyText(NextGroupKey, false);
            OverlayPanel.NextHotkeyGroupButton.ToolTipText = $"[{keyText}]\r{Text.TapNewKey}";

            //Validate each hotkey entry and update its UI button.
            for (int i = 0; i < Entries.Length; ++i)
            {
                Buttons[i].IconContainer.Enabled = true;
                Buttons[i].DescriptionLabel.Enabled = true;
                Buttons[i].Panel.ToolTipText = "";
                Buttons[i].AnimationPanel.AnimatedBackgroundTextures = null;

                if (Entries[i].Item != null)
                    ValidateItemHotkey(i);
                else if (Entries[i].Spell.Name != null)
                    ValidateSpellHotkey(i);
                else if (Entries[i].Callback != null)
                    ValidateCallbackHotkey(i);
                else
                    Buttons[i].IconContainer.Enabled = Buttons[i].DescriptionLabel.Enabled = false;

                //Use short key text for button text.
                (KeyCode modifier, KeyCode k) = InputManager.Instance.GetCombo(Entries[i].KeyPress);
                string text = GetKeyText(k, true);
                if (k == KeyCode.None)
                    text = "";
                Buttons[i].CharLabel.Text = text;
                Buttons[i].CharLabel.TextColor = charLabelDefaultColor;

                text = ControlsConfigManager.Instance.GetButtonText(Entries[i].KeyPress);

                if (modifier == KeyCode.LeftControl || modifier == KeyCode.RightControl)
                    Buttons[i].CharLabel.TextColor = charLabelControlColor;
                else if (modifier == KeyCode.LeftAlt || modifier == KeyCode.RightAlt)
                    Buttons[i].CharLabel.TextColor = charLabelAltColor;
                else if (modifier != KeyCode.None)
                    Buttons[i].CharLabel.TextColor = new Color(0.4f, 0.4f, 1f);

                
                //Get longer text description for tooltips.
                text = GetKeyText(Entries[i].KeyPress, false);
                if (k == KeyCode.None)
                    text = "";
                if (Buttons[i].Panel.ToolTipText.Length > 0)
                    Buttons[i].Panel.ToolTipText += "\r";
                Buttons[i].Panel.ToolTipText += $"[{text}]\r{Text.TapNewKey}";
            }
        }


        /// <summary>
        /// Make sure hotkeyed item still exists in player inventory.
        /// Set/Update icon, text, text color, and tooltip text.
        /// </summary>
        static void ValidateItemHotkey(int index)
        {
            DaggerfallUnityItem item = Entries[index].Item;

            PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;

            if (GameManager.Instance.PlayerEntity.Items.Contains(item))
            {
                ItemHelper itemHelper = DaggerfallUnity.Instance.ItemHelper;
                ImageData image = itemHelper.GetInventoryImage(item);
                Buttons[index].Icon.BackgroundTexture = image.texture;

                // Use texture size if base image size is zero (i.e. new images that are not present in classic data)
                if (image.width != 0 && image.height != 0)
                    Buttons[index].Icon.Size = new Vector2(image.width, image.height);
                else
                    Buttons[index].Icon.Size = new Vector2(image.texture.width, image.texture.height);

                Buttons[index].Icon.MaxAutoScale = 2; //to prevent small textures from being scaled too high

                if (item.IsEnchanted)
                    Buttons[index].AnimationPanel.AnimatedBackgroundTextures = magicAnimation.animatedTextures;

                if (item.IsStackable())
                {
                    Buttons[index].ItemCountLabel.Enabled = true;
                    Buttons[index].ItemCountLabel.Text = item.stackCount.ToString();
                }
                else
                {
                    Buttons[index].ItemCountLabel.Enabled = false;
                }

                string shortened = item.LongName;
                string longName = item.LongName;

                if (item.IsPotion)
                {
                    //Skipping the 'Potion Of' part
                    string[] words = item.LongName.Split(' ');
                    if (words.Length > 2)
                    {
                        shortened = "";
                        for (int i = 2; i < words.Length; ++i)
                            shortened += words[i] + " ";
                    }
                }

                //Get power description for magic items with legacy magic powers.
                string legacyPowersText = GetLegacyPowersText(item);
                if (legacyPowersText.Length > 0)
                {
                    shortened = legacyPowersText;
                    longName += "\r(" + legacyPowersText + ")";
                }

                Buttons[index].DescriptionLabel.Text = shortened;
                Buttons[index].Panel.ToolTipText = longName;

                //Has this item been activated in the last fraction of a second?
                bool wasActivated = lastHotkeyActivationIndex == index && lastHotkeyActivationTime > Time.realtimeSinceStartup - 0.2f;

                if (wasActivated)
                    Buttons[index].DescriptionLabel.TextColor = Color.white;
                else if (playerEntity.ItemEquipTable.IsEquipped(item))
                    Buttons[index].DescriptionLabel.TextColor = new Color(0, 0.7f, 0.4f);
                else if (item == GameManager.Instance.PlayerEntity.LightSource)
                    Buttons[index].DescriptionLabel.TextColor = new Color(0, 0.7f, 0.4f);
                else if (item.currentCondition <= 0)
                    Buttons[index].DescriptionLabel.TextColor = new Color(0.8f, 0.4f, 0);
                else
                    Buttons[index].DescriptionLabel.TextColor = Color.gray;
            }
            else
            {
                //Hotkey no longer valid, clear it
                Set(index, null);
            }

        }


        /// <summary>
        /// Make sure hotkeyed spell still exists in the spellbook.
        /// Set/Update hotkey icon, description, description color, and tooltip text.
        /// </summary>
        static void ValidateSpellHotkey(int index)
        {
            PlayerEntity player = GameManager.Instance.PlayerEntity;
            EffectBundleSettings[] spellbook = player.GetSpells();
            SpellIconCollection icons = DaggerfallUI.Instance.SpellIconCollection;

            foreach (EffectBundleSettings spell in spellbook)
            {
                bool matches;

                if (HasMultipleSpellsWithSameName(spell.Name))
                    matches = spell.Equals(Entries[index].Spell); //compare entire spell record
                else
                    matches = (spell.Name == Entries[index].Spell.Name); //Just compare spell names

                if (matches)
                {
                    Entries[index].Spell = spell; //refresh value to absorb any changes made (like spell icon)
                    Texture2D tex = icons.GetSpellIcon(spell.Icon);
                    Buttons[index].Icon.BackgroundTexture = tex;
                    Buttons[index].Icon.Size = new Vector2(tex.width, tex.height);
                    Buttons[index].Icon.MaxAutoScale = 0; //to nullify any changes made by ValidateItemHotkey
                    Buttons[index].DescriptionLabel.Text = spell.Name;
                    Buttons[index].AnimationPanel.AnimatedBackgroundTextures = null;
                    Buttons[index].ItemCountLabel.Enabled = false;

                    bool wasActivated = lastHotkeyActivationIndex == index && lastHotkeyActivationTime > Time.realtimeSinceStartup - 0.2f;
                    (int _, int cost) = FormulaHelper.CalculateTotalEffectCosts(spell.Effects, spell.TargetType, null, spell.MinimumCastingCost);
                    if (spell.Tag == PlayerEntity.lycanthropySpellTag)
                        cost = 0;

                    if (wasActivated)
                        Buttons[index].DescriptionLabel.TextColor = Color.white;
                    else if (cost > player.CurrentMagicka)
                        Buttons[index].DescriptionLabel.TextColor = new Color(0.8f, 0.4f, 0);
                    else
                        Buttons[index].DescriptionLabel.TextColor = Color.gray;
                    Buttons[index].Panel.ToolTipText = spell.Name;
                    return;
                }
            }

            //Hotkey no longer valid, clear it
            Set(index, null);
        }


        /// <summary>
        /// Returns true if multiple spells have the specified name.
        /// </summary>
        static bool HasMultipleSpellsWithSameName(string name)
        {
            EffectBundleSettings[] playerSpells = GameManager.Instance.PlayerEntity.GetSpells();

            int count = 0;

            foreach (EffectBundleSettings spell in playerSpells)
                if (spell.Name == name)
                    ++count;

            return count > 1;
        }


        /// <summary>
        /// Updates UI elements to match callback record.
        /// </summary>
        static void ValidateCallbackHotkey(int index)
        {
            Texture2D tex = Entries[index].Callback.Icon;
            Buttons[index].Icon.BackgroundTexture = tex;
            Buttons[index].Icon.Size = new Vector2(tex.width, tex.height);
            Buttons[index].Icon.MaxAutoScale = 0; //to nullify any changes made by ValidateItemHotikey
            Buttons[index].DescriptionLabel.Text = Entries[index].Callback.Description;
            bool wasActivated = lastHotkeyActivationIndex == index && lastHotkeyActivationTime > Time.realtimeSinceStartup - 0.25f;
            Buttons[index].DescriptionLabel.TextColor = wasActivated ? Color.white : Color.gray;
            Buttons[index].AnimationPanel.AnimatedBackgroundTextures = null;
            Buttons[index].ItemCountLabel.Enabled = false;

            Buttons[index].Panel.ToolTipText = Entries[index].Callback.Description;

        }



        /// <summary>
        /// Check if the item has any 'OnUse' enchantments.
        /// </summary>
        static bool HasOnUseEnchantment(DaggerfallUnityItem item)
        {
            if (!item.IsEnchanted)
                return false;

            EnchantmentSettings[] enchantments = item.GetCombinedEnchantmentSettings();

            foreach (EnchantmentSettings settings in enchantments)
            {
                // Get effect template
                IEntityEffect effectTemplate = GameManager.Instance.EntityEffectBroker.GetEffectTemplate(settings.EffectKey);
                if (effectTemplate == null)
                    continue;

                if (effectTemplate.HasEnchantmentPayloadFlags(EnchantmentPayloadFlags.Used))
                    return true;
            }

            return false;
        }


        /// <summary>
        /// Gets the abbreviated text for a legacy magic item so that it just shows the magic power,
        /// e.g. instead of 'Bracelet of Ass Whup' it will just say 'Ass Whup'.
        /// This is useful when there is a very limited amount of text space available.
        /// </summary>
        static string GetLegacyPowersText(DaggerfallUnityItem item)
        {
            string text = "";

            if (item.legacyMagic == null || item.IsArtifact || !item.IsIdentified || item.ItemGroup == ItemGroups.Weapons)
                return text;

            if (legacyPowersCache.TryGetValue(item.UID, out text))
                return text;

            IMacroContextProvider mcp = item as IMacroContextProvider;
            TextFile.Token[] tokens = mcp.GetMacroDataSource().MagicPowers(TextFile.Formatting.Nothing);

            string fullText = tokens[0].text; //'Cast when used: Ass Whup'

            //Gets the 'Cast when used: ' part
            string firstPart = TextManager.Instance.GetLocalizedTextList("itemPowers")[(int)item.legacyMagic[0].type] + " ";

            //...and removes it
            text = fullText.Substring(firstPart.Length);

            //Cache the result for efficiency.
            legacyPowersCache.Add(item.UID, text);

            return text;
        }


        /// <summary>
        /// Handles event sent by DaggerfallInventoryWindow.
        /// Records the last item hovered over in the DaggerfallInventoryWindow.
        /// </summary>
        static void OnItemHoverHandler(DaggerfallUnityItem item, DaggerfallInventoryWindow.ItemHoverLocation loc)
        {
            hoverItem = item;
        }


    } //class Hotkeys




} //namespace
