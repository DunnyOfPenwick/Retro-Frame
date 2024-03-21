// Project:     Retro-Frame for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: January 2024

using System;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop;
using Wenzil.Console;
using DaggerfallWorkshop.Utility;


namespace RetroFrame
{
    public class RetroFrameMod : MonoBehaviour
    {
        public static Mod Mod;

        //Mod settings 
        public static int HotkeyGroups { get; private set; }
        public static bool SilentGroupSwitching { get; private set; }
        public static bool ShowBorders { get; private set; }
        public static bool ShowShortcutKeyInTooltip { get; private set; }
        public static int ShowLogMessages { get; set; }
        public static int Mode640Replacement { get; private set; }


        public static bool ShowErrorLog { get; set; }
        public static bool LockErrorLog { get; set; }
        public static int LogCount { get; private set; }
        public static float LastLogTime { get; private set; }


        public static AudioSource AudioSource;


        class LogEntry
        {
            public LogType Type;
            public string Text;
            public DateTime Time;
            public int Count;
        }
        static readonly LinkedList<LogEntry> logEntries = new LinkedList<LogEntry>();
        static string errorDisplayText;


        /// <summary>
        /// Class used to contain hotkey callback info sent by other mods.
        /// </summary>
        public class HotkeyCallbackInfo : IEquatable<HotkeyCallbackInfo>, IComparable<HotkeyCallbackInfo>
        {
            public string Description;
            public string DeclarerName;
            public string MethodName;
            public Texture2D Icon;
            public DFModMessageCallback Callback;

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;
                else if (obj is HotkeyCallbackInfo callback)
                    return Equals(callback);
                else
                    return false;
            }

            public bool Equals(HotkeyCallbackInfo other)
            {
                if (other == null)
                    return false;
                else
                    return Description == other.Description &&
                        DeclarerName == other.DeclarerName &&
                        MethodName == other.MethodName;
            }

            public int CompareTo(HotkeyCallbackInfo other)
            {
                if (other == null)
                    return 1;
                else
                    return Description.CompareTo(other.Description);
            }

            public override int GetHashCode()
            {
                return Description.GetHashCode() * DeclarerName.GetHashCode() * MethodName.GetHashCode();
            }
            
        }


        public static readonly List<HotkeyCallbackInfo> HotkeyCallbacks = new List<HotkeyCallbackInfo>();

        static OverlayPanel overlayPanel;

        bool showCompass;
        bool showVitals;
        bool showInteractionMode;
        bool showActiveSpells;

        ConsoleController consoleController;



        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            Mod = initParams.Mod;

            var go = new GameObject(Mod.Title);
            go.AddComponent<RetroFrameMod>();

            AudioSource = go.AddComponent<AudioSource>();

            //Mod Settings
            HotkeyGroups = Mod.GetSettings().GetInt("Options", "HotkeyGroups");
            HotkeyGroups = Mathf.Clamp(HotkeyGroups, 0, 7);

            SilentGroupSwitching = Mod.GetSettings().GetBool("Options", "SilentGroupSwitching");
            ShowBorders = Mod.GetSettings().GetBool("Options", "ShowBorders");
            ShowShortcutKeyInTooltip = Mod.GetSettings().GetBool("Options", "ShowShortcutKeyInTooltip");
            ShowLogMessages = Mod.GetSettings().GetInt("Options", "ShowLogMessages");

            Mode640Replacement = Mod.GetSettings().GetInt("Options", "Mode640x400Replacement");
            Swap640Mode();

            //Handle log entries produced by Unity logger
            Application.logMessageReceived += HandleLog;

            //Establish this mod's message receiver, to get messages from other mods
            Mod.MessageReceiver = MessageReceiver;

            //Creating the overlay panel.  Doing it early to make sure it exists before messages come in.
            overlayPanel = new OverlayPanel();
            overlayPanel.Setup();

            Mod.IsReady = true;
        }


        /// <summary>
        /// Finds and returns the callback with matching description, declarer, and method name.
        /// </summary>
        public static HotkeyCallbackInfo FindMatchingCallback(string descr, string declarer, string methodName)
        {
            foreach (HotkeyCallbackInfo info in HotkeyCallbacks)
                if (info.Description == descr && info.DeclarerName == declarer && info.MethodName == methodName)
                    return info;

            return null;
        }


        static void Swap640Mode()
        {
            RenderTexture presentationTarget = Mod.GetAsset<RenderTexture>("RetroPresentation");
            RenderTexture rt = null;
            RenderTexture rt_hud = null;

            if (Mode640Replacement == 1)
            {
                rt = Mod.GetAsset<RenderTexture>("RetroTarget720x540");
                rt_hud = Mod.GetAsset<RenderTexture>("RetroTarget720x540_HUD");
            }
            else if (Mode640Replacement == 2)
            {
                rt = Mod.GetAsset<RenderTexture>("RetroTarget1440x1080");
                rt_hud = Mod.GetAsset<RenderTexture>("RetroTarget1440x1080_HUD");
            }

            RetroRenderer retroRenderer = GameManager.GetMonoBehaviour<RetroRenderer>(false);
            if (retroRenderer && presentationTarget != null && rt != null && rt_hud != null)
            {
                retroRenderer.RetroTexture640x400 = rt;
                retroRenderer.RetroTexture640x400_HUD = rt_hud;
                retroRenderer.RetroPresentationTarget = presentationTarget;
                GameManager.Instance.RetroPresenter.RetroPresentationSource = retroRenderer.RetroPresentationTarget;
            }

        }


        /// <summary>
        /// Handles messages sent by other mods.
        /// See https://www.dfworkshop.net/projects/daggerfall-unity/modding/features/#mods-interaction
        /// </summary>
        static void MessageReceiver(string message, object data, DFModMessageCallback callBack)
        {
            const string registerHotkeyMessage = "registerCustomHotkeyHandler";
            const string getOverlayMessage = "getOverlay";

            if (message.Equals(registerHotkeyMessage, StringComparison.OrdinalIgnoreCase))
            {
                if (callBack == null)
                {
                    Debug.LogError($"Retro-Frame: '{registerHotkeyMessage}': provided callback was null");
                }
                else if (data is ValueTuple<string, Texture2D> tuple)
                {
                    HotkeyCallbackInfo hkCallback = new HotkeyCallbackInfo();

                    hkCallback.Description = tuple.Item1;
                    hkCallback.Icon = tuple.Item2;
                    hkCallback.DeclarerName = callBack.Method.DeclaringType.FullName;
                    hkCallback.MethodName = callBack.Method.Name;
                    hkCallback.Callback = callBack;

                    if (hkCallback.Description == null)
                        Debug.LogError($"Retro-Frame: '{registerHotkeyMessage}': the tuple string value from {hkCallback.DeclarerName} was null");
                    else if (hkCallback.Icon == null)
                        Debug.LogError($"Retro-Frame: '{registerHotkeyMessage}': the tuple Texture2D value from {hkCallback.DeclarerName} was null");
                    else if (HasDuplicateCustomHotkey(hkCallback))
                        Debug.LogError($"Retro-Frame: '{hkCallback.DeclarerName}' has already registered a hotkey handler using method '{hkCallback.MethodName}' and description '{hkCallback.Description}'");
                    else
                    {
                        HotkeyCallbacks.Add(hkCallback);
                        HotkeyCallbacks.Sort();
                    }
                }
                else
                {
                    Debug.LogError($"Retro-Frame: '{registerHotkeyMessage}' expects a (string,Texture2D) tuple");
                }
            }
            else if (message.Equals(getOverlayMessage, StringComparison.OrdinalIgnoreCase))
            {
                callBack("panelReply", overlayPanel);
            }
            else
            {
                Debug.LogError($"Retro-Frame: MessageReceiver has no message handler for '{message}'");
            }

        }


        /// <summary>
        /// Returns true if a callback record exists with same description, declarer, and method name.
        /// </summary>
        static bool HasDuplicateCustomHotkey(HotkeyCallbackInfo callbackInfo)
        {
            foreach (HotkeyCallbackInfo info in HotkeyCallbacks)
                if (info.Equals(callbackInfo))
                    return true;

            return false;
        }




        /// <summary>
        /// Event handler for Unity logging events.
        /// </summary>
        static void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (ShowLogMessages == 0)
                return;

            //ShowLogMessages: 0=None, 1=Errors, 2=Warnings, 3=All
            if (type == LogType.Error ||
                type == LogType.Exception ||
                (type == LogType.Warning && ShowLogMessages == 2) ||
                ShowLogMessages == 3)
            {
                LogCount++;
                LastLogTime = Time.realtimeSinceStartup;
                logString = logString ?? "(??)";

                AddLogEntry(type, logString);

                if (!LockErrorLog)
                    UpdateLogWindowText(stackTrace ?? "(??)");
            }
        }


        static void AddLogEntry(LogType type, string logString)
        {
            if (logEntries.Count > 0 && logEntries.First.Value.Text == logString)
            {
                //a repeat entry, just update the count
                ++logEntries.First.Value.Count;
                logEntries.First.Value.Time = DateTime.Now;
            }
            else
            {
                LogEntry newEntry = new LogEntry
                {
                    Type = type,
                    Text = logString,
                    Time = DateTime.Now,
                    Count = 1
                };

                logEntries.AddFirst(newEntry);

                if (logEntries.Count > 1 + 10)
                    logEntries.RemoveLast();
            }
        }


        static void UpdateLogWindowText(string stackTrace)
        {
            string text = "";

            if (!DaggerfallUnity.Settings.HideLoginName)
                text += $"{Text.LogFileLocation}: {DaggerfallUnity.Settings.PersistentDataPath}\n\n";
            
            foreach (LogEntry entry in logEntries)
            {
                string logTypeSymbol;
                switch (entry.Type)
                {
                    case LogType.Exception: logTypeSymbol = "!!"; break;
                    case LogType.Error: logTypeSymbol = "!*"; break;
                    case LogType.Warning: logTypeSymbol = "**"; break;
                    case LogType.Assert: logTypeSymbol = "^^"; break;
                    default: logTypeSymbol = "??"; break;
                }

                if (entry == logEntries.First.Value)
                {
                    text += $"{Text.LogHeader}\n";

                    text += $"{logTypeSymbol}[{entry.Time:HH:mm:ss}]";
                    text += $" +{entry.Count - 1}";

                    text += $"\n{entry.Text}\n\n";

                    text += $"{Text.StackTraceHeader}\n";
                    text += stackTrace;

                    text += $"\n\n{Text.Previous10}\n";
                }
                else
                {
                    text += $"{logTypeSymbol}[{entry.Time:HH:mm:ss}] +{entry.Count - 1} {entry.Text}\n\n";
                }
            }

            errorDisplayText = text;
        }



        void Start()
        {
            Debug.Log("Start(): Retro-Frame");

            //Checks that all localization keys are present in [??]textdatabase.txt...logs error if not.
            Text.ValidateAllTextKeys();

            //Set the IHasModSaveData object responsible for storing and restoring save data.
            Mod.SaveDataInterface = new ModSave();

            //Register our pre-made custom hotkey callback
            Texture2D crosshair = Mod.GetAsset<Texture2D>("CrosshairIcon");
            MessageReceiver("registerCustomHotkeyHandler", (Text.CrosshairToggle, crosshair), ToggleCrosshair);

            //Remember original HUD settings for toggling between normal and retro mode.
            showCompass = DaggerfallUI.Instance.DaggerfallHUD.ShowCompass;
            showVitals = DaggerfallUI.Instance.DaggerfallHUD.ShowVitals;
            showInteractionMode = DaggerfallUI.Instance.DaggerfallHUD.ShowInteractionModeIcon;
            showActiveSpells = DaggerfallUI.Instance.DaggerfallHUD.ShowActiveSpells;

            GameObject console = GameObject.Find("Console");
            consoleController = console.GetComponent<ConsoleController>();

            Debug.Log("Finished Start(): Retro-Frame");
        }


        void Update()
        {
            if (IsRetroMode())
            {
                //Get access to entire screen while overlay is updated.
                Rect? originalCustomScreenRect = DaggerfallUI.Instance.CustomScreenRect;
                DaggerfallUI.Instance.CustomScreenRect = new Rect?(new Rect(0, 0, Screen.width, Screen.height));
                try
                {
                    overlayPanel.Update();

                    //Prevent errors that occur during startup from disappearing too quickly.
                    if (!overlayPanel.Started)
                        if (LastLogTime != 0)
                            LastLogTime = Time.realtimeSinceStartup;

                    DaggerfallUI.Instance.DaggerfallHUD.ShowCompass = false;
                    DaggerfallUI.Instance.DaggerfallHUD.ShowVitals = false;
                    DaggerfallUI.Instance.DaggerfallHUD.ShowInteractionModeIcon = false;
                    DaggerfallUI.Instance.DaggerfallHUD.ShowActiveSpells = false;
                }
                finally
                {
                    DaggerfallUI.Instance.CustomScreenRect = originalCustomScreenRect;
                }
            }
            else
            {
                DaggerfallUI.Instance.DaggerfallHUD.ShowCompass = showCompass;
                DaggerfallUI.Instance.DaggerfallHUD.ShowVitals = showVitals;
                DaggerfallUI.Instance.DaggerfallHUD.ShowInteractionModeIcon = showInteractionMode;
                DaggerfallUI.Instance.DaggerfallHUD.ShowActiveSpells = showActiveSpells;
            }
        }



        void OnGUI()
        {
            //Only draw when retro mode is set to 4:3 aspect ratio.
            if (IsRetroMode())
            {
                //We don't want any part of the overlay covering open windows.
                GUI.depth = GameManager.IsGamePaused ? 1 : 0;

                if (Event.current.type == EventType.Repaint && !consoleController.ui.isConsoleOpen)
                {
                    //Allow access to entire screen while drawing overlay.
                    Rect? originalCustomScreenRect = DaggerfallUI.Instance.CustomScreenRect;
                    DaggerfallUI.Instance.CustomScreenRect = new Rect?(new Rect(0, 0, Screen.width, Screen.height));

                    try
                    {
                        if (overlayPanel != null)
                            overlayPanel.Draw();
                    }
                    finally
                    {
                        DaggerfallUI.Instance.CustomScreenRect = originalCustomScreenRect;
                    }
                }


                if (ShowErrorLog || LockErrorLog)
                {
                    GUI.depth = -1; //Error log should show on top of everything else

                    //Put log text area in center of screen.
                    int width = Screen.width / 2;
                    int height = Screen.height / 2;
                    int x = Screen.width / 2 - width / 2;
                    int y = Screen.height / 2 - height / 2;
                    GUI.TextArea(new Rect(x, y, width, height), errorDisplayText);
                }
            }

        }



        /// <summary>
        /// Returns true if DFU RetroRenderingMode is active with 4:3 aspect ratio.
        /// </summary>
        bool IsRetroMode()
        {
            if (DaggerfallUnity.Settings.RetroRenderingMode != 0)
                if (DaggerfallUnity.Settings.RetroModeAspectCorrection == (int)RetroModeAspects.FourThree)
                    return true;

            return false;
        }



        /// <summary>
        /// Custom hotkey callback to toggle the HUD crosshair.
        /// </summary>
        void ToggleCrosshair(string description, object index)
        {
            DaggerfallUI.Instance.DaggerfallHUD.ShowCrosshair = !DaggerfallUI.Instance.DaggerfallHUD.ShowCrosshair;
        }



    } //class RetroFrame



} //namespace
