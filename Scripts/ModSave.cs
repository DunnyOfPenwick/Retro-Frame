// Project:     Retro-Frame for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: January 2024

using System;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Utility.ModSupport;


namespace RetroFrame
{


    public class ModSave : IHasModSaveData
    {
        enum HotkeyActionType { None, Item, Spell, Callback };

        class HotkeyRecord
        {
            public KeyCode KeyPress;
            public HotkeyActionType HotkeyActionType;
            public string Id; //this will be the UID for items, spellbook index for spells, or callback info
        }


        class SaveData
        {
            public int ActiveButtonPanel;
            public int CurrentHotkeyGroupIndex;
            public KeyCode PriorGroupKey = KeyCode.Minus;
            public KeyCode NextGroupKey = KeyCode.Equals;
            public List<List<HotkeyRecord>> HotkeyGroups = new List<List<HotkeyRecord>>();
        }


        public Type SaveDataType
        {
            get { return typeof(SaveData); }
        }


        public object NewSaveData()
        {
            return new SaveData();
        }


        public object GetSaveData()
        {
            SaveData dataToSave = new SaveData();

            //Record the current state of the actions/hotkey panel activation.
            //It's an 'int' to allow for future expansion.
            dataToSave.ActiveButtonPanel = OverlayPanel.ActionsPanelLocked ? 0 : 1;

            //Record the currently active hotkey set.
            dataToSave.CurrentHotkeyGroupIndex = Hotkeys.CurrentGroupIndex;

            dataToSave.PriorGroupKey = Hotkeys.PriorGroupKey;
            dataToSave.NextGroupKey = Hotkeys.NextGroupKey;

            //Record all hotkey sets.
            for (int i = 0; i < Hotkeys.Groups.Count; ++i)
                dataToSave.HotkeyGroups.Add(GatherHotkeyRecords(i));


            return dataToSave;
        }


        List<HotkeyRecord> GatherHotkeyRecords(int setIndex)
        {
            List<HotkeyRecord> records = new List<HotkeyRecord>();

            EffectBundleSettings[] spellbook = GameManager.Instance.PlayerEntity.GetSpells();

            for (int i = 0; i < Hotkeys.Groups[setIndex].Length; ++i)
            {
                Hotkeys.Entry hotkey = Hotkeys.Groups[setIndex][i];

                HotkeyRecord record = new HotkeyRecord();

                record.KeyPress = hotkey.KeyPress;

                if (hotkey.Item != null)
                {
                    record.HotkeyActionType = HotkeyActionType.Item;
                    record.Id = hotkey.Item.UID.ToString();
                    records.Add(record);
                }
                else if (hotkey.Spell.Name != null && hotkey.Spell.Name.Length > 0)
                {
                    record.HotkeyActionType = HotkeyActionType.Spell;
                    for (int spellbookIndex = 0; spellbookIndex < spellbook.Length; ++spellbookIndex)
                    {
                        if (spellbook[spellbookIndex].Equals(hotkey.Spell))
                        {
                            record.Id = spellbookIndex.ToString();
                            records.Add(record);
                            break;
                        }
                    }
                }
                else if (hotkey.Callback != null)
                {
                    record.HotkeyActionType = HotkeyActionType.Callback;
                    record.Id = hotkey.Callback.Description + "#" + hotkey.Callback.DeclarerName + "#" + hotkey.Callback.MethodName;
                    records.Add(record);
                }
                else
                {
                    record.HotkeyActionType = HotkeyActionType.None;
                    record.Id = "";
                    records.Add(record);
                }
            }
            
            return records;
        }


        public void RestoreSaveData(object obj)
        {
            SaveData dataToRestore = (SaveData)obj;

            OverlayPanel.ActionsPanelLocked = (dataToRestore.ActiveButtonPanel == 0);

            Hotkeys.PriorGroupKey = dataToRestore.PriorGroupKey;
            Hotkeys.NextGroupKey = dataToRestore.NextGroupKey;

            if (Hotkeys.Groups.Count < 1)
            {
                OverlayPanel.ActionsPanelLocked = true;
                return;
            }


            //First, clear all current hotkey entries.
            Hotkeys.CurrentGroupIndex = 0;
            for (int g = 0; g < Hotkeys.Groups.Count; ++g, ++Hotkeys.CurrentGroupIndex)
                for (int i = 0; i < Hotkeys.Entries.Length; ++i)
                    Hotkeys.Set(i, null);


            try
            {
                for (int i = 0; i < Hotkeys.Groups.Count && i < dataToRestore.HotkeyGroups.Count; ++i)
                    RestoreHotkeyRecords(i, dataToRestore.HotkeyGroups[i]);
            }
            catch (Exception e)
            {
                Debug.LogError("Retro-Frame: exception while restoring save: " + e.ToString());
            }

            if (dataToRestore.CurrentHotkeyGroupIndex < 0 || dataToRestore.CurrentHotkeyGroupIndex >= Hotkeys.Groups.Count)
                Hotkeys.CurrentGroupIndex = 0;
            else
                Hotkeys.CurrentGroupIndex = dataToRestore.CurrentHotkeyGroupIndex;

        }


        void RestoreHotkeyRecords(int groupIndex, List<HotkeyRecord> records)
        {
            ItemCollection playerItems = GameManager.Instance.PlayerEntity.Items;
            EffectBundleSettings[] spellbook = GameManager.Instance.PlayerEntity.GetSpells();

            Hotkeys.CurrentGroupIndex = groupIndex;

            for (int i = 0; i < records.Count; ++i)
            {
                HotkeyRecord record = records[i];

                Hotkeys.Entries[i].KeyPress = record.KeyPress;

                if (record.HotkeyActionType == HotkeyActionType.Item)
                {
                    ulong uid = ulong.Parse(record.Id);
                    if (playerItems.Contains(uid))
                    {
                        DaggerfallUnityItem item = playerItems.GetItem(uid);
                        Hotkeys.Set(i, item);
                    }
                }
                else if (record.HotkeyActionType == HotkeyActionType.Spell)
                {
                    int spellbookIndex = int.Parse(record.Id);
                    if (spellbookIndex < spellbook.Length)
                        Hotkeys.Set(i, spellbook[spellbookIndex]);
                }
                else if (record.HotkeyActionType == HotkeyActionType.Callback)
                {
                    string[] info = record.Id.Split('#');
                    RetroFrameMod.HotkeyCallbackInfo callback = RetroFrameMod.FindMatchingCallback(info[0], info[1], info[2]);
                    if (callback != null)
                        Hotkeys.Set(i, callback);
                }
                else
                {
                    Hotkeys.Set(i, null); //clear it
                }
            }
        }



    } //class ModSave




} //namespace
