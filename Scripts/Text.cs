// Project:     Retro-Frame for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: January 2024

using System.Reflection;
using UnityEngine;
using UnityEngine.Localization.Settings;

//We're setting field values using reflection, so disabling the 'Field is never assigned to...' warning.
#pragma warning disable 0649

namespace RetroFrame
{

    static class Text
    {
        static readonly string missingTextSubstring;

        public static string
            // General / Mod
            LogFileLocation,
            LogHeader,
            StackTraceHeader,
            Previous10,
            CrosshairToggle,

            //Overlay
            ErrorIconTooltip,

            //Hotkeys
            TapNewKey,

            //HotkeyPopupWindow
            Hotkeys,
            Set,
            Clear,
            Choose,
            ClearHotkey,
            Cancel
            ;

        static Text()
        {
            missingTextSubstring = LocalizationSettings.StringDatabase.NoTranslationFoundMessage.Substring(0, 14);

            //Populate the class fields with localized text from the [??]textdatabase.txt file.
            FieldInfo[] fields = typeof(Text).GetFields();
            foreach (FieldInfo field in fields)
            {
                string value = RetroFrameMod.Mod.Localize(field.Name);
                if (value == missingTextSubstring)
                {
                    value = "???";
                    Debug.LogWarning($"Retro-Frame: Missing textdatabase.txt key '{field.Name}'");
                }
                field.SetValue(null, value);
            }

        }


        /// <summary>
        /// Calls string.Format(this, params args[]) and returns the result.
        /// </summary>
        static string FormatWith(this string fmt, params System.Object[] args)
        {
            return string.Format(fmt, args);
        }


        /// <summary>
        /// Loops through all field/keys in this class and verifies that corresponding keys exist
        /// in the mod's current '[??]textdatabase.txt' file.
        /// Returns true if all entries exist in the file.
        /// </summary>
        public static bool ValidateAllTextKeys()
        {
            bool areAllKeysValid = true;

            FieldInfo[] fields = typeof(Text).GetFields();
            foreach (FieldInfo field in fields)
            {
                string result = RetroFrameMod.Mod.Localize(field.Name);

                bool exists = result.Length > 0 && !result.StartsWith(missingTextSubstring);

                if (!exists)
                {
                    Debug.LogWarningFormat("RetroFrame: The mod's [??]textdatabase.txt file is missing an entry for key '{0}'", field.Name);
                    areAllKeysValid = false;
                }
            }

            return areAllKeysValid;
        }




    } //class Text



} //namespace
