// Project:     Retro-Frame for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: January 2024

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Utility;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility;


namespace RetroFrame
{

    public class OverlayPanel : Panel
    {
        #region Fields

        public static bool ActionsPanelLocked { get; set; } = true;
        public static int HoveredHotkeyButtonIndex { get; private set; } = -1;
        public static Panel PriorHotkeyGroupButton { get { return instance.priorHotkeyGroupButton; } }
        public static Panel NextHotkeyGroupButton { get { return instance.nextHotkeyGroupButton; } }

        private static OverlayPanel instance;

        readonly Panel mainPanel = new Panel();
        readonly Panel viewPanel = new Panel();
        readonly Panel topBorderPanel = new Panel();
        readonly Panel bottomBorderPanel = new Panel();
        readonly Panel toolTipContainerPanel = new Panel();
        readonly Panel leftPanel = new Panel();
        readonly Panel leftPanelPauseGameOverlay = new Panel();
        readonly Panel rightPanel = new Panel();
        readonly Panel rightPanelPauseGameOverlay = new Panel();
        readonly Panel characterPanel = new Panel();
        readonly Panel headPanel = new Panel();
        readonly Panel headShadePanel = new Panel();
        readonly Panel headTintPanel = new Panel();
        readonly TextLabel nameLabel = new TextLabel();
        readonly Panel inventoryButton = new Panel();
        readonly Panel interactionModeButton = new Panel();
        readonly Panel activeEffectsPanel = new Panel();
        readonly Panel vitalsPanel = new Panel();
        readonly Panel actionsPanel = new Panel();
        readonly Panel spellsButton = new Panel();
        readonly Panel weaponButton = new Panel();
        readonly Panel useButton = new Panel();
        readonly Panel transportButton = new Panel();
        readonly Panel mapButton = new Panel();
        readonly Panel restButton = new Panel();
        readonly Panel hotkeysPanel = new Panel();
        readonly Panel togglePanelButton = new Panel();
        readonly Panel priorHotkeyGroupButton = new Panel();
        readonly Panel nextHotkeyGroupButton = new Panel();
        readonly Panel errorLogIcon = new Panel();
        readonly TextLabel errorLogCountLabel = new TextLabel();
        readonly Panel compassPanel = new Panel();
        readonly Panel compassPointerPanel = new Panel();
        readonly Panel instantaneousSpellIconContainer = new Panel();
        readonly Panel instantaneousSpellIcon = new Panel();
        readonly Panel instantaneousSpellIconOverlay = new Panel();
        readonly TextLabel instantaneousSpellLabel = new TextLabel();

        readonly ToolTip defaultToolTip = new ToolTip();

        float defaultTextScale = 3.5f;

        readonly HotkeyPopupWindow hotkeyPopup = new HotkeyPopupWindow(DaggerfallUI.UIManager);

        bool triggerHeadChange;
        Texture2D shadowHeadTexture;
        Texture2D chameleonHeadTexture;

        const int effectRows = 6;
        const int effectCols = 3;
        readonly Panel[,] effectIcons = new Panel[effectRows, effectCols]; //a grid for active spell effect icons
        readonly TextLabel[] effectLabels = new TextLabel[effectRows];     //one text label per row of spell effect icons

        const int compassFrameCount = 32;
        readonly Texture2D[] compassTextures = new Texture2D[compassFrameCount];

        Texture2D grabModeTexture;
        Texture2D infoModeTexture;
        Texture2D stealModeTexture;
        Texture2D talkModeTexture;
        Texture2D spellTexture;
        Texture2D useMagicItemTexture;
        Texture2D transportTexture;
        Texture2D inventoryTexture;
        Texture2D mapTexture;
        Texture2D weaponTexture;
        Texture2D restTexture;
        Texture2D compassTexture;
        Texture2D headFrameTexture;
        Texture2D vitalsFrameTexture;
        Texture2D hotkeyButtonTexture;
        Texture2D hotkeyIconCutoutTexture;
        Texture2D switchTexture;
        Texture2D priorTexture;
        Texture2D nextTexture;
        Texture2D topBorderTexture;
        Texture2D bottomBorderTexture;

        readonly List<LiveEffectBundle> collapsedActiveEffects = new List<LiveEffectBundle>();
        readonly List<LiveEffectBundle> fullDisplayActiveEffects = new List<LiveEffectBundle>();
        readonly List<LiveEffectBundle> equippedItemBundles = new List<LiveEffectBundle>();
        readonly List<LiveEffectBundle> playerBundles = new List<LiveEffectBundle>();
        readonly List<LiveEffectBundle> otherBundles = new List<LiveEffectBundle>();


        float instantaneousSpellActivationTime;

        bool isOverTogglePanelButton;

        public bool Started = false; //will be false until first game load or character creation is complete

        Mod firstPersonLightingMod;
        Mod dream;
        bool isMonsterUniversityInstalled;

        #endregion


        public OverlayPanel() : base()
        {
            //Instance = this;
            Enabled = false; //The overlay will be disabled until game finishes starting/loading.
            instance = this;
        }


        #region Layout/Setup
        public void Setup()
        {
            dream = ModManager.Instance.GetMod("DREAM - HUD & MENU");
            firstPersonLightingMod = ModManager.Instance.GetMod("First-Person-Lighting");
            isMonsterUniversityInstalled = ModManager.Instance.GetMod("Monster-University") != null;

            LoadAssets();

            Hotkeys.Setup();

            BackgroundTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("Frame");


            // Update active effects for player every round or when a bundle is assigned, removed, or state added
            EntityEffectBroker.OnNewMagicRound += RegisterNewMagicRound;
            GameManager.Instance.PlayerEffectManager.OnAssignBundle += RegisterAssignEffectChanges;
            GameManager.Instance.PlayerEffectManager.OnRemoveBundle += RegisterRemoveEffectChanges;
            GameManager.Instance.PlayerEffectManager.OnAddIncumbentState += RegisterAddIncumbentEffectChanges;
            StartGameBehaviour.OnStartGame += StartGameBehaviour_OnStartGameHandler;
            SaveLoadManager.OnStartLoad += SaveLoadManager_OnStartLoad;
            SaveLoadManager.OnLoad += SaveLoadManager_OnLoad;
            PlayerDeath.OnPlayerDeath += OnPlayerDeath;

            Tag = "OverlayPanel";

            defaultTextScale = DaggerfallUnity.Settings.SDFFontRendering ? 4f : 3.5f;

            //********************Main Panel********************************************************
            Components.Add(mainPanel);
            mainPanel.Tag = "MainPanel";
            mainPanel.Size = new Vector2(1280, 800);
            mainPanel.AutoSize = AutoSizeModes.ScaleFreely;

            //toolTipContainerPanel exists to give defaultToolTip proper scaling.
            //It provides different scaling than the main panel.
            Components.Add(toolTipContainerPanel);
            toolTipContainerPanel.Tag = "TooltipContainerPanel";
            toolTipContainerPanel.Size = new Vector2(384, 216); //reasonable sized/scaled tooltips
            toolTipContainerPanel.AutoSize = AutoSizeModes.ScaleFreely;

            defaultToolTip.ToolTipDelay = DaggerfallUnity.Settings.ToolTipDelayInSeconds;
            defaultToolTip.BackgroundColor = DaggerfallUnity.Settings.ToolTipBackgroundColor;
            defaultToolTip.TextColor = DaggerfallUnity.Settings.ToolTipTextColor;
            //***Have to use reflection to call internal property setter, otherwise only works when running in editor.
            PropertyInfo propInfo = defaultToolTip.GetType().GetProperty("Parent");
            propInfo.SetValue(defaultToolTip, toolTipContainerPanel);
            defaultToolTip.Enabled = DaggerfallUnity.Settings.EnableToolTips;

            //********************Left Panel*********************************************************
            mainPanel.Components.Add(leftPanel);
            leftPanel.Tag = "LeftPanel";
            leftPanel.Position = new Vector2(5.2f, 8);
            leftPanel.Size = new Vector2(148, 784);


            //=======Character/Head Panel=========
            leftPanel.Components.Add(characterPanel);
            characterPanel.Tag = "CharacterPanel";
            characterPanel.VerticalAlignment = VerticalAlignment.Top;
            characterPanel.Size = new Vector2(leftPanel.InteriorWidth, 168);
            characterPanel.BackgroundTexture = headFrameTexture;
            characterPanel.ToolTip = defaultToolTip;
            characterPanel.OnMouseClick += CharacterPanel_OnMouseClick;
            characterPanel.OnRightMouseClick += CharacterPanel_OnMouseClick;
            //---head
            characterPanel.Components.Add(headPanel);
            characterPanel.Tag = "HeadPanel";
            headPanel.HorizontalAlignment = HorizontalAlignment.Center;
            headPanel.Position = new Vector2(16, 18);
            headPanel.Size = new Vector2(leftPanel.Size.x - 30, leftPanel.Size.x - 32);
            headPanel.BackgroundTextureLayout = BackgroundLayout.ScaleToFit;
            //---head shade panel
            headPanel.Components.Add(headShadePanel);
            headShadePanel.Tag = "HeadShadePanel";
            headShadePanel.Size = headPanel.Size;
            //---head tint panel
            headShadePanel.Components.Add(headTintPanel);
            headTintPanel.Tag = "HeadTintPanel";
            headTintPanel.Size = headShadePanel.Size;
            //---name
            characterPanel.Components.Add(nameLabel);
            nameLabel.Tag = "NameLabel";
            nameLabel.Position = new Vector2(0, characterPanel.Size.y - 30);
            nameLabel.MaxWidth = characterPanel.InteriorWidth - 16;
            nameLabel.TextScale = defaultTextScale; //**TextScale is likely changed during Update() for longer names
            nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            nameLabel.HorizontalTextAlignment = TextLabel.HorizontalTextAlignmentSetting.Center;
            nameLabel.TextColor = Color.gray;
            nameLabel.ShadowColor = Color.black;
            nameLabel.ShadowPosition = new Vector2(4, 4);

            //======Inventory Button=========
            leftPanel.Components.Add(inventoryButton);
            inventoryButton.Tag = "InventoryButtonPanel";
            inventoryButton.Position = new Vector2(0, characterPanel.Size.y);
            inventoryButton.Size = new Vector2(leftPanel.InteriorWidth, 100);
            inventoryButton.BackgroundTexture = inventoryTexture;
            inventoryButton.ToolTip = defaultToolTip;
            inventoryButton.OnMouseClick += InventoryButton_OnMouseClick;
            inventoryButton.OnRightMouseClick += InventoryButton_OnMouseClick;

            //======Interaction Mode Button========
            leftPanel.Components.Add(interactionModeButton);
            interactionModeButton.Tag = "InteractionModeButtonPanel";
            interactionModeButton.Position = new Vector2(0, inventoryButton.Position.y + inventoryButton.Size.y);
            interactionModeButton.Size = new Vector2(leftPanel.InteriorWidth, 100);
            interactionModeButton.ToolTip = defaultToolTip;
            interactionModeButton.OnMouseClick += InteractionModeButton_OnMouseClick;
            interactionModeButton.OnRightMouseClick += InteractionModeButton_OnRightMouseClick;

            //======Vitals Panel=============
            leftPanel.Components.Add(vitalsPanel);
            vitalsPanel.Tag = "VitalsPanel";
            vitalsPanel.VerticalAlignment = VerticalAlignment.Bottom;
            vitalsPanel.HorizontalAlignment = HorizontalAlignment.Center;
            vitalsPanel.Size = new Vector2(leftPanel.InteriorWidth, 160);
            vitalsPanel.BackgroundTexture = vitalsFrameTexture;
            vitalsPanel.ToolTip = defaultToolTip;
            //---vitals value bars
            HUDVitals vitals = new HUDVitals();
            vitalsPanel.Components.Add(vitals);
            vitals.Tag = "HUDVitalsBars";
            vitals.HorizontalAlignment = HorizontalAlignment.None;
            vitals.VerticalAlignment = VerticalAlignment.None;
            vitals.AutoSize = AutoSizeModes.None;
            vitals.SetMargins(Margins.All, 0);
            vitals.SetAllAutoSize(AutoSizeModes.None);
            vitals.SetAllHorizontalAlignment(HorizontalAlignment.None);
            vitals.SetAllVerticalAlignment(VerticalAlignment.None);
            if (dream != null)
            {
                Vector2 barSize = new Vector2(19, 112);
                vitals.CustomHealthBarPosition = new Vector2(30, 24);
                vitals.CustomHealthBarSize = barSize;
                vitals.CustomFatigueBarPosition = new Vector2(68, 24);
                vitals.CustomFatigueBarSize = barSize;
                vitals.CustomMagickaBarPosition = new Vector2(105, 24);
                vitals.CustomMagickaBarSize = barSize;
            }
            else
            {
                Vector2 barSize = new Vector2(18, 112);
                vitals.CustomHealthBarPosition = new Vector2(30, 24);
                vitals.CustomHealthBarSize = barSize;
                vitals.CustomFatigueBarPosition = new Vector2(65, 24);
                vitals.CustomFatigueBarSize = barSize;
                vitals.CustomMagickaBarPosition = new Vector2(100, 24);
                vitals.CustomMagickaBarSize = barSize;
            }

            //======Active Effects Panel===============
            leftPanel.Components.Add(activeEffectsPanel);
            activeEffectsPanel.Tag = "ActiveEffectsPanel";
            activeEffectsPanel.Position = new Vector2(0, interactionModeButton.Position.y + interactionModeButton.Size.y);
            float vSize = leftPanel.Size.y - vitalsPanel.Size.y;
            vSize -= interactionModeButton.Position.y + interactionModeButton.Size.y;
            activeEffectsPanel.Size = new Vector2(leftPanel.InteriorWidth, vSize);
            float colSpacing = 8;
            float rowSpacing = 0;
            float rowHeight = activeEffectsPanel.Size.y / effectRows - rowSpacing;

            for (int row = 0; row < effectRows; ++row)
            {
                Panel rowPanel = new Panel();
                activeEffectsPanel.Components.Add(rowPanel);
                rowPanel.Tag = "ActiveEffectsRowPanel" + row;
                rowPanel.Position = new Vector2(0, (row * rowHeight) + (row * rowSpacing));
                rowPanel.Size = new Vector2(activeEffectsPanel.InteriorWidth, rowHeight);
                rowPanel.LeftMargin = rowPanel.RightMargin = 4;

                TextLabel label = new TextLabel();
                rowPanel.Components.Add(label);
                label.Tag = "DescriptionLabel" + row;
                effectLabels[row] = label;
                label.VerticalAlignment = VerticalAlignment.Middle;
                label.HorizontalAlignment = HorizontalAlignment.Left;
                label.TextScale = defaultTextScale;
                label.MaxWidth = rowPanel.InteriorWidth - 4;
                label.TextColor = Color.gray;
                label.ShadowPosition = Vector2.zero;
                label.ToolTip = defaultToolTip;

                for (int col = 0; col < effectCols; ++col)
                {
                    //Icons added right-to-left
                    Panel icon = new Panel();
                    rowPanel.Components.Add(icon);
                    icon.Tag = "Icon" + col;
                    int iconWidth = rowPanel.InteriorHeight;
                    float x = rowPanel.InteriorWidth - ((col + 1) * iconWidth) - (col * colSpacing);
                    icon.Position = new Vector2(x, 0);
                    icon.Size = new Vector2(iconWidth, iconWidth);
                    icon.VerticalAlignment = VerticalAlignment.Middle;
                    icon.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
                    icon.ToolTip = defaultToolTip;
                    Panel iconCutout = new Panel(); //--a cutout panel placed over icon to make the icon look circular
                    icon.Components.Add(iconCutout);
                    iconCutout.AutoSize = AutoSizeModes.ResizeToFill;
                    iconCutout.BackgroundTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("IconCutout");
                    iconCutout.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
                    effectIcons[row, col] = icon;
                }
            }

            //====Instantaneous Spell Icon and Label==========
            mainPanel.Components.Add(instantaneousSpellIconContainer);
            instantaneousSpellIconContainer.Tag = "InstSpellIconContainer";
            instantaneousSpellIconContainer.Enabled = false;
            instantaneousSpellIconContainer.Position = new Vector2(leftPanel.Size.x + 10, mainPanel.Size.y - 110);
            instantaneousSpellIconContainer.Size = new Vector2(50, 50);
            instantaneousSpellIconContainer.Components.Add(instantaneousSpellIcon);
            instantaneousSpellIcon.Tag = "InstSpellIcon";
            instantaneousSpellIcon.Size = new Vector2(10, 10);
            instantaneousSpellIcon.AutoSize = AutoSizeModes.ResizeToFill;
            instantaneousSpellIcon.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
            instantaneousSpellIconContainer.Components.Add(instantaneousSpellIconOverlay);
            instantaneousSpellIconOverlay.Tag = "InstSpellIconOverlay";
            instantaneousSpellIconOverlay.Size = new Vector2(50, 50);
            instantaneousSpellIcon.AutoSize = AutoSizeModes.ResizeToFill;

            mainPanel.Components.Add(instantaneousSpellLabel);
            instantaneousSpellLabel.Tag = "InstSpellLabel";
            instantaneousSpellLabel.Enabled = false;
            instantaneousSpellLabel.Position = new Vector2(leftPanel.Size.x + 60, mainPanel.Size.y - 97);
            instantaneousSpellLabel.TextScale = defaultTextScale;
            instantaneousSpellLabel.ShadowPosition = Vector2.zero;

            //====Left Panel Pause Game Overlay, darkens left panel when game is paused
            leftPanel.Components.Add(leftPanelPauseGameOverlay);
            leftPanelPauseGameOverlay.Tag = "LeftPanelPauseGameOverlay";
            leftPanelPauseGameOverlay.Size = leftPanel.Size;
            leftPanelPauseGameOverlay.BackgroundColor = new Color(0, 0, 0, 0.5f);

            //***********************Right Panel*********************************************************
            mainPanel.Components.Add(rightPanel);
            rightPanel.Tag = "RightPanel";
            rightPanel.Position = new Vector2(1126, 8);
            rightPanel.Size = leftPanel.Size;
            rightPanel.OnMouseLeave += RightPanel_OnMouseLeave;


            //=======Actions Panel=========
            rightPanel.Components.Add(actionsPanel);
            actionsPanel.Tag = "ActionsPanel";
            actionsPanel.Enabled = ActionsPanelLocked;
            actionsPanel.Size = new Vector2(rightPanel.Size.x, rightPanel.Size.y - 200);
            actionsPanel.VerticalAlignment = VerticalAlignment.Top;
            float buttonHeight = actionsPanel.Size.y / 6;
            //--Spells
            actionsPanel.Components.Add(spellsButton);
            spellsButton.Tag = "SpellsButtonPanel";
            spellsButton.Position = new Vector2(0, buttonHeight * 0);
            spellsButton.Size = new Vector2(actionsPanel.InteriorWidth, buttonHeight);
            spellsButton.BackgroundTexture = spellTexture;
            spellsButton.ToolTip = defaultToolTip;
            spellsButton.OnMouseClick += SpellsButton_OnMouseClick;
            spellsButton.OnRightMouseClick += SpellsButton_OnMouseClick;
            //--Use
            actionsPanel.Components.Add(useButton);
            useButton.Tag = "UseButtonPanel";
            useButton.Position = new Vector2(0, buttonHeight * 1);
            useButton.Size = new Vector2(actionsPanel.InteriorWidth, buttonHeight);
            useButton.BackgroundTexture = useMagicItemTexture;
            useButton.ToolTip = defaultToolTip;
            useButton.OnMouseClick += UseButton_OnMouseClick;
            useButton.OnRightMouseClick += UseButton_OnMouseClick;
            //--Weapon
            actionsPanel.Components.Add(weaponButton);
            weaponButton.Tag = "WeaponButtonPanel";
            weaponButton.Position = new Vector2(0, buttonHeight * 2);
            weaponButton.Size = new Vector2(actionsPanel.InteriorWidth, buttonHeight);
            weaponButton.BackgroundTexture = weaponTexture;
            weaponButton.ToolTip = defaultToolTip;
            weaponButton.OnMouseClick += WeaponButton_OnMouseClick;
            weaponButton.OnRightMouseClick += WeaponButton_OnMouseClick;
            //--Transport
            actionsPanel.Components.Add(transportButton);
            transportButton.Tag = "TransportButtonPanel";
            transportButton.Position = new Vector2(0, buttonHeight * 3);
            transportButton.Size = new Vector2(actionsPanel.InteriorWidth, buttonHeight);
            transportButton.BackgroundTexture = transportTexture;
            transportButton.ToolTip = defaultToolTip;
            transportButton.OnMouseClick += TransportButton_OnMouseClick;
            transportButton.OnRightMouseClick += TransportButton_OnMouseClick;
            //--Map
            actionsPanel.Components.Add(mapButton);
            mapButton.Tag = "MapButtonPanel";
            mapButton.Position = new Vector2(0, buttonHeight * 4);
            mapButton.Size = new Vector2(actionsPanel.InteriorWidth, buttonHeight);
            mapButton.BackgroundTexture = mapTexture;
            mapButton.ToolTip = defaultToolTip;
            mapButton.OnMouseClick += MapButton_OnMouseClick;
            mapButton.OnRightMouseClick += MapButton_OnRightMouseClick;
            //--Rest
            actionsPanel.Components.Add(restButton);
            restButton.Tag = "RestButtonPanel";
            restButton.Position = new Vector2(0, buttonHeight * 5);
            restButton.Size = new Vector2(actionsPanel.InteriorWidth, buttonHeight);
            restButton.BackgroundTexture = restTexture;
            restButton.ToolTip = defaultToolTip;
            restButton.OnMouseClick += RestButton_OnMouseClick;
            restButton.OnRightMouseClick += RestButton_OnMouseClick;

            //========Hotkeys Panel===========
            rightPanel.Components.Add(hotkeysPanel);
            hotkeysPanel.Tag = "HotkeysPanel";
            hotkeysPanel.Enabled = !ActionsPanelLocked;
            hotkeysPanel.VerticalAlignment = VerticalAlignment.Top;
            hotkeysPanel.Size = actionsPanel.Size;
            hotkeysPanel.OnMouseLeave += HotkeysPanel_OnMouseLeave;

            buttonHeight = hotkeysPanel.Size.y / Hotkeys.Buttons.Length;

            Vector2 iconSize = new Vector2(buttonHeight - 12, buttonHeight - 4);
            Color iconBackgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            for (int i = 0; i < Hotkeys.Buttons.Length; ++i)
            {
                Hotkeys.Buttons[i] = new Hotkeys.Button();

                Hotkeys.Buttons[i].Panel = new Panel();
                hotkeysPanel.Components.Add(Hotkeys.Buttons[i].Panel);
                Hotkeys.Buttons[i].Panel.Tag = i.ToString();
                Hotkeys.Buttons[i].Panel.BackgroundTexture = hotkeyButtonTexture;
                Hotkeys.Buttons[i].Panel.Size = new Vector2(rightPanel.InteriorWidth, buttonHeight);
                Hotkeys.Buttons[i].Panel.TopMargin = Hotkeys.Buttons[i].Panel.BottomMargin = 4;
                Hotkeys.Buttons[i].Panel.Position = new Vector2(0, i * buttonHeight);
                Hotkeys.Buttons[i].Panel.ToolTip = defaultToolTip;
                Hotkeys.Buttons[i].Panel.OnMouseClick += HotkeyButton_OnMouseClick;
                Hotkeys.Buttons[i].Panel.OnRightMouseClick += HotkeyButton_OnRightMouseClick;
                Hotkeys.Buttons[i].Panel.OnMouseEnter += HotkeyButton_OnMouseEnter;

                //Icons come first because we want them drawn first, beneath everything else
                Panel iconContainer = new Panel();
                Hotkeys.Buttons[i].Panel.Components.Add(iconContainer);
                Hotkeys.Buttons[i].IconContainer = iconContainer;
                iconContainer.Tag = "IconContainer";
                iconContainer.Position = new Vector2(20, 0);
                iconContainer.VerticalAlignment = VerticalAlignment.Middle;
                iconContainer.Size = new Vector2(50, 46);
                iconContainer.BackgroundColor = iconBackgroundColor;

                Panel icon = new Panel();
                Hotkeys.Buttons[i].Icon = icon;
                iconContainer.Components.Add(Hotkeys.Buttons[i].Icon);
                icon.Tag = "Icon";
                icon.VerticalAlignment = VerticalAlignment.Middle;
                icon.HorizontalAlignment = HorizontalAlignment.Center;
                icon.Size = new Vector2(5, 5);
                icon.AutoSize = AutoSizeModes.ScaleToFit;
                icon.BackgroundTextureLayout = BackgroundLayout.StretchToFill;

                Panel hotkeyIconCutout = new Panel(); //--a cutout panel placed over icon to make the icon circular
                iconContainer.Components.Add(hotkeyIconCutout);
                hotkeyIconCutout.Tag = "IconCutout";
                hotkeyIconCutout.Size = new Vector2(10, 10);
                hotkeyIconCutout.VerticalAlignment = VerticalAlignment.Middle;
                hotkeyIconCutout.HorizontalAlignment = HorizontalAlignment.Left;
                hotkeyIconCutout.AutoSize = AutoSizeModes.ResizeToFill;
                hotkeyIconCutout.BackgroundTexture = hotkeyIconCutoutTexture;
                hotkeyIconCutout.BackgroundTextureLayout = BackgroundLayout.StretchToFill;

                Hotkeys.Buttons[i].AnimationPanel = new Panel();
                iconContainer.Components.Add(Hotkeys.Buttons[i].AnimationPanel);
                Hotkeys.Buttons[i].AnimationPanel.Tag = "Animation";
                Hotkeys.Buttons[i].AnimationPanel.Size = new Vector2(10, 10);
                Hotkeys.Buttons[i].AnimationPanel.AutoSize = AutoSizeModes.ScaleToFit;
                Hotkeys.Buttons[i].AnimationPanel.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
                Hotkeys.Buttons[i].AnimationPanel.AnimationDelayInSeconds = 0.15f;

                Hotkeys.Buttons[i].ItemCountLabel = new TextLabel();
                iconContainer.Components.Add(Hotkeys.Buttons[i].ItemCountLabel);
                Hotkeys.Buttons[i].ItemCountLabel.Tag = "ItemCountLabel";
                Hotkeys.Buttons[i].ItemCountLabel.HorizontalAlignment = HorizontalAlignment.Left;
                Hotkeys.Buttons[i].ItemCountLabel.VerticalAlignment = VerticalAlignment.Bottom;
                Hotkeys.Buttons[i].ItemCountLabel.TextScale = 2.5f;
                Hotkeys.Buttons[i].ItemCountLabel.TextColor = new Color(0.75f, 0.75f, 0);
                Hotkeys.Buttons[i].ItemCountLabel.BackgroundColor = new Color(0.4f, 0.4f, 0.4f, 0.3f);

                //charLabel and description label come after icons because we want them displayed on top
                TextLabel charLabel = new TextLabel();
                Hotkeys.Buttons[i].Panel.Components.Add(charLabel);
                Hotkeys.Buttons[i].CharLabel = charLabel;
                charLabel.Tag = "CharLabel";
                charLabel.Position = new Vector2(6, 13);
                charLabel.TextScale = defaultTextScale;
                charLabel.TextColor = Color.gray;
                charLabel.ShadowColor = Color.black;
                charLabel.ShadowPosition = new Vector2(4, 4);

                Hotkeys.Buttons[i].DescriptionLabel = new TextLabel();
                Hotkeys.Buttons[i].Panel.Components.Add(Hotkeys.Buttons[i].DescriptionLabel);
                Hotkeys.Buttons[i].DescriptionLabel.Tag = "DescriptionLabel";
                Hotkeys.Buttons[i].DescriptionLabel.Position = new Vector2(60, charLabel.Position.y);
                Hotkeys.Buttons[i].DescriptionLabel.TextScale = defaultTextScale;
                Hotkeys.Buttons[i].DescriptionLabel.MaxWidth = 80;
                Hotkeys.Buttons[i].DescriptionLabel.TextColor = Color.gray;
                Hotkeys.Buttons[i].DescriptionLabel.ShadowColor = Color.black;
                Hotkeys.Buttons[i].DescriptionLabel.ShadowPosition = new Vector2(3, 3);
            }


            //=======Compass==========
            rightPanel.Components.Add(compassPanel);
            compassPanel.Tag = "CompassPanel";
            compassPanel.VerticalAlignment = VerticalAlignment.Bottom;
            compassPanel.HorizontalAlignment = HorizontalAlignment.Center;
            compassPanel.Size = new Vector2(rightPanel.InteriorWidth, vitalsPanel.Size.y);
            compassPanel.BackgroundTexture = compassTexture;
            compassPanel.ToolTip = defaultToolTip;
            compassPanel.OnMouseClick += CompassPanel_OnMouseClick;
            compassPanel.OnRightMouseClick += CompassPanel_OnMouseClick;
            //--Compass pointer
            compassPanel.Components.Add(compassPointerPanel);
            compassPointerPanel.Tag = "CompassPointerPanel";
            compassPointerPanel.HorizontalAlignment = HorizontalAlignment.Center;
            compassPointerPanel.VerticalAlignment = VerticalAlignment.Middle;
            compassPointerPanel.AutoSize = AutoSizeModes.ResizeToFill;

            //=======Button Panel Toggle Button=========
            rightPanel.Components.Add(togglePanelButton);
            togglePanelButton.Enabled = RetroFrameMod.HotkeyGroups > 0;
            togglePanelButton.Tag = "TogglePanelButtonPanel";
            buttonHeight = rightPanel.Size.y - compassPanel.Size.y - actionsPanel.Size.y;
            togglePanelButton.Size = new Vector2(rightPanel.InteriorWidth / 2, buttonHeight);
            togglePanelButton.Position = new Vector2(0, hotkeysPanel.Size.y);
            togglePanelButton.HorizontalAlignment = HorizontalAlignment.Center;
            togglePanelButton.BackgroundTexture = switchTexture;
            togglePanelButton.OnMouseEnter += TogglePanelButton_OnMouseEnter;
            togglePanelButton.OnMouseClick += TogglePanelButton_OnMouseClick;
            togglePanelButton.OnRightMouseClick += TogglePanelButton_OnMouseClick;

            //=========Prior Hotkey Group Button========
            rightPanel.Components.Add(priorHotkeyGroupButton);
            priorHotkeyGroupButton.Enabled = RetroFrameMod.HotkeyGroups > 1;
            priorHotkeyGroupButton.Tag = "PriorButtonPanel";
            buttonHeight = rightPanel.Size.y - compassPanel.Size.y - actionsPanel.Size.y;
            priorHotkeyGroupButton.Size = new Vector2(rightPanel.InteriorWidth / 4, buttonHeight);
            priorHotkeyGroupButton.Position = new Vector2(0, hotkeysPanel.Size.y);
            priorHotkeyGroupButton.HorizontalAlignment = HorizontalAlignment.Left;
            priorHotkeyGroupButton.BackgroundTexture = priorTexture;
            priorHotkeyGroupButton.ToolTip = defaultToolTip;
            priorHotkeyGroupButton.OnMouseClick += ChangeHotkeyGroupButton_OnMouseClick;
            priorHotkeyGroupButton.OnRightMouseClick += ChangeHotkeyGroupButton_OnMouseClick;

            //=========Next Hotkey Group Button=============
            rightPanel.Components.Add(nextHotkeyGroupButton);
            nextHotkeyGroupButton.Enabled = RetroFrameMod.HotkeyGroups > 1;
            nextHotkeyGroupButton.Tag = "NextButtonPanel";
            buttonHeight = rightPanel.Size.y - compassPanel.Size.y - actionsPanel.Size.y;
            nextHotkeyGroupButton.Size = new Vector2(rightPanel.InteriorWidth / 4, buttonHeight);
            nextHotkeyGroupButton.Position = new Vector2(0, hotkeysPanel.Size.y);
            nextHotkeyGroupButton.HorizontalAlignment = HorizontalAlignment.Right;
            nextHotkeyGroupButton.BackgroundTexture = nextTexture;
            nextHotkeyGroupButton.ToolTip = defaultToolTip;
            nextHotkeyGroupButton.OnMouseClick += ChangeHotkeyGroupButton_OnMouseClick;
            nextHotkeyGroupButton.OnRightMouseClick += ChangeHotkeyGroupButton_OnMouseClick;


            //=========Error/Warning Log Icon=========
            mainPanel.Components.Add(errorLogIcon);
            errorLogIcon.Tag = "ErrorLogIcon";
            errorLogIcon.VerticalAlignment = VerticalAlignment.Bottom;
            errorLogIcon.HorizontalAlignment = HorizontalAlignment.Right;
            errorLogIcon.Size = new Vector2(30, 30);
            errorLogIcon.Enabled = false;
            errorLogIcon.BackgroundTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("ErrorLogIcon");
            errorLogIcon.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
            errorLogIcon.OnMouseClick += ErrorLogIcon_OnMouseClick;
            errorLogIcon.OnRightMouseClick += ErrorLogIcon_OnRightMouseClick;
            errorLogIcon.OnMouseEnter += ErrorLogIcon_OnMouseEnter;
            errorLogIcon.OnMouseLeave += ErrorLogIcon_OnMouseLeave;
            errorLogIcon.ToolTip = defaultToolTip;
            errorLogIcon.ToolTipText = Text.ErrorIconTooltip;
            //--log count label
            errorLogIcon.Components.Add(errorLogCountLabel);
            errorLogCountLabel.Tag = "ErrorLogCountLabel";
            errorLogCountLabel.HorizontalAlignment = HorizontalAlignment.Left;
            errorLogCountLabel.VerticalAlignment = VerticalAlignment.Bottom;
            errorLogCountLabel.TextScale = 2.5f;
            errorLogCountLabel.TextColor = Color.white;
            errorLogCountLabel.BackgroundColor = new Color(0.4f, 0.4f, 0.4f, 0.3f);


            //========Right Panel Pause Game Overlay, to darken right panel when game is paused.
            rightPanel.Components.Add(rightPanelPauseGameOverlay);
            rightPanelPauseGameOverlay.Tag = "RightPanelPauseGameOverlay";
            rightPanelPauseGameOverlay.Size = rightPanel.Size;
            rightPanelPauseGameOverlay.BackgroundColor = new Color(0, 0, 0, 0.5f);


            //***********************View Panel*********************************************************
            mainPanel.Components.Add(viewPanel);
            viewPanel.Tag = "ViewPanel";
            viewPanel.Size = new Vector2(mainPanel.Size.x - leftPanel.Size.x - rightPanel.Size.x - 22, mainPanel.Size.y);
            viewPanel.HorizontalAlignment = HorizontalAlignment.Center;
            viewPanel.VerticalAlignment = VerticalAlignment.Middle;

            //***********************Top and Bottom Borders*********************************************
            viewPanel.Components.Add(topBorderPanel);
            topBorderPanel.Tag = "TopBorderPanel";
            topBorderPanel.Size = new Vector2(viewPanel.Size.x, 55);
            topBorderPanel.VerticalAlignment = VerticalAlignment.Top;
            topBorderPanel.HorizontalAlignment = HorizontalAlignment.Center;
            topBorderPanel.BackgroundTexture = topBorderTexture;
            topBorderPanel.Enabled = RetroFrameMod.ShowBorders;

            viewPanel.Components.Add(bottomBorderPanel);
            bottomBorderPanel.Tag = "BottomBorderPanel";
            bottomBorderPanel.Size = new Vector2(viewPanel.Size.x, 55);
            bottomBorderPanel.VerticalAlignment = VerticalAlignment.Bottom;
            bottomBorderPanel.HorizontalAlignment = HorizontalAlignment.Center;
            bottomBorderPanel.BackgroundTexture = bottomBorderTexture;
            bottomBorderPanel.Enabled = RetroFrameMod.ShowBorders;
        }



        /// <summary>
        /// Creates or loads textures and other assets.
        /// </summary>
        void LoadAssets()
        {
            // Read compass animations
            for (int i = 0; i < compassFrameCount; i++)
            {
                compassTextures[i] = ImageReader.GetTexture("CMPA00I0.BSS", 0, i, true);
            }


            //====grabbing sections of dfu textures to use for button graphics
            Texture2D interactionModesTexture = ImageReader.GetTexture("MAIN01I0.IMG");
            DFSize nativeTextureSize = new DFSize(47, 92);

            Rect grabModeSubrect = new Rect(0, 46, 47, 23);
            Rect infoModeSubrect = new Rect(0, 69, 47, 23);
            Rect stealModeSubrect = new Rect(0, 0, 47, 23);
            Rect talkModeSubrect = new Rect(0, 23, 47, 23);

            grabModeTexture = ImageReader.GetSubTexture(interactionModesTexture, grabModeSubrect, nativeTextureSize);
            infoModeTexture = ImageReader.GetSubTexture(interactionModesTexture, infoModeSubrect, nativeTextureSize);
            stealModeTexture = ImageReader.GetSubTexture(interactionModesTexture, stealModeSubrect, nativeTextureSize);
            talkModeTexture = ImageReader.GetSubTexture(interactionModesTexture, talkModeSubrect, nativeTextureSize);


            Texture2D actionButtonTexture = ImageReader.GetTexture("MAIN00I0.IMG");
            nativeTextureSize = new DFSize(320, 46);

            Rect spellSubrect = new Rect(83, 0, 48, 23);
            Rect useMagicItemSubrect = new Rect(83, 23, 48, 23);
            Rect transportSubrect = new Rect(131, 23, 47, 23); //right edge is a pixel off, will stretch
            Rect inventorySubrect = new Rect(178, 0, 47, 23); //right edge is a pixel off
            Rect mapSubrect = new Rect(178, 23, 47, 23); //right edge is a pixel off
            Rect weaponSubrect = new Rect(225, 0, 48, 23);
            Rect restSubrect = new Rect(225, 23, 48, 23);
            Rect compassSubrect = new Rect(273, 0, 47, 46);

            spellTexture = ImageReader.GetSubTexture(actionButtonTexture, spellSubrect, nativeTextureSize);
            useMagicItemTexture = ImageReader.GetSubTexture(actionButtonTexture, useMagicItemSubrect, nativeTextureSize);
            transportTexture = ImageReader.GetSubTexture(actionButtonTexture, transportSubrect, nativeTextureSize);
            inventoryTexture = ImageReader.GetSubTexture(actionButtonTexture, inventorySubrect, nativeTextureSize);
            mapTexture = ImageReader.GetSubTexture(actionButtonTexture, mapSubrect, nativeTextureSize);
            weaponTexture = ImageReader.GetSubTexture(actionButtonTexture, weaponSubrect, nativeTextureSize);
            restTexture = ImageReader.GetSubTexture(actionButtonTexture, restSubrect, nativeTextureSize);
            compassTexture = ImageReader.GetSubTexture(actionButtonTexture, compassSubrect, nativeTextureSize);

            if (dream != null)
            {
                headFrameTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("HeadFrameHi");
                vitalsFrameTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("VitalsFrameHi");
                hotkeyButtonTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("HotkeyButtonHi");
                hotkeyIconCutoutTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("HotkeyIconCutoutHi");
                switchTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("SwitchHi");
                priorTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("PriorHi");
                nextTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("NextHi");
                topBorderTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("TopBorderHi");
                bottomBorderTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("BottomBorderHi");
            }
            else
            {
                headFrameTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("HeadFrame");
                vitalsFrameTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("VitalsFrame");
                hotkeyButtonTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("HotkeyButton");
                hotkeyIconCutoutTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("HotkeyIconCutout");
                switchTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("Switch");
                priorTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("Prior");
                nextTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("Next");
                topBorderTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("TopBorder");
                bottomBorderTexture = RetroFrameMod.Mod.GetAsset<Texture2D>("BottomBorder");
            }
        }



        #endregion

        #region Update/Draw
        public override void Update()
        {
            if (!Enabled)
                return;

            base.Update();

            //When game is paused, darken the left and right panels to contrast with open windows.
            leftPanelPauseGameOverlay.Enabled = rightPanelPauseGameOverlay.Enabled = GameManager.IsGamePaused;

            //Set name under portrait.
            string playerName = GameManager.Instance.PlayerEntity.Name;
            nameLabel.Text = playerName.Trim();
            if (DaggerfallUnity.Settings.SDFFontRendering == false)
            {
                if (playerName.Length <= 5)
                    nameLabel.TextScale = defaultTextScale * 1.2f;
                else if (playerName.Length <= 7)
                    nameLabel.TextScale = defaultTextScale * 1f;
                else
                    nameLabel.TextScale = defaultTextScale * 0.75f;
            }

            UpdateLogInfo();

            UpdateToolTipText();

            Hotkeys.Refresh();

            UpdateHeadTexture();

            UpdateCharacterStatusColor();

            UpdateHeadFrameShadeAndTint();

            UpdateInteractionModeTexture();

            UpdateActiveEffectIndicators();

            UpdateInstantaneousSpell();

            // Calculate compass rotation percent and update compass pointer.
            float percent = GameManager.Instance.MainCamera.transform.eulerAngles.y / 360f;
            compassPointerPanel.BackgroundTexture = compassTextures[(int)(compassFrameCount * percent)];

            //Update the tooltip component so it can track mouse location
            defaultToolTip.Update();

            //Determine whether the action panel or the hotkeys panel should be shown.
            actionsPanel.Enabled = ActionsPanelLocked ^ isOverTogglePanelButton;
            hotkeysPanel.Enabled = !actionsPanel.Enabled;

        }


        public override void Draw()
        {
            if (!Enabled)
                return;

            base.Draw();

            if ((GameManager.IsGamePaused || GameManager.Instance.PlayerMouseLook.cursorActive))
                defaultToolTip.Draw();
        }


        /// <summary>
        /// Enables/disables the error log icon next to the panel switch button and updates its information.
        /// </summary>
        void UpdateLogInfo()
        {
            //Enable the error icon if an error has been logged in the last 10 seconds.
            errorLogIcon.Enabled = RetroFrameMod.LastLogTime > Time.realtimeSinceStartup - 10;

            //Disable if mod setting ShowErrorLogIndicator is false.
            errorLogIcon.Enabled &= RetroFrameMod.ShowErrorLogIndicator;

            //Keep it enabled while the error log window is shown.
            errorLogIcon.Enabled |= RetroFrameMod.ShowErrorLog || RetroFrameMod.LockErrorLog;


            errorLogCountLabel.Text = RetroFrameMod.LogCount.ToString();

            //Flash the error icon background if recent log activity in last 3 seconds
            if (RetroFrameMod.LastLogTime > Time.realtimeSinceStartup - 3)
                errorLogIcon.BackgroundColor = new Color(1, 1, 0) * Mathf.Cos(Time.realtimeSinceStartup * 10);
            else
                errorLogIcon.BackgroundColor = Color.clear;

        }


        /// <summary>
        /// Sets tooltip text for character frame, buttons, vitals, and compass.
        /// Tooltips for active effects and hotkeys are set elsewhere.
        /// </summary>
        void UpdateToolTipText()
        {
            TextManager textManager = TextManager.Instance;
            PlayerEntity player = GameManager.Instance.PlayerEntity;

            string characterText = "";
            if (player.CurrentHealth < player.MaxHealth / 4)
                characterText += textManager.GetLocalizedText("low") + " " + textManager.GetLocalizedText("health") + "\r";
            if (player.CurrentFatigue < player.MaxFatigue / 8)
                characterText += textManager.GetLocalizedText("low") + " " + textManager.GetLocalizedText("fatigue") + "\r";
            if (HasActivePoison())
                characterText += textManager.GetLocalizedText("Poison") + "\r";
            if (HasCompletedPoison())
                characterText += textManager.GetLocalizedText("Poison") + " " + textManager.GetLocalizedText("damage") + "\r";
            if (HasActiveDisease())
                characterText += textManager.GetLocalizedText("disease") + "\r";
            characterText += GetShortcutText(InputManager.Actions.CharacterSheet);
            characterText = characterText.TrimEnd('\r');
            characterPanel.ToolTipText = characterText;

            inventoryButton.ToolTipText = GetShortcutText(InputManager.Actions.Inventory);

            interactionModeButton.ToolTipText =
                GetShortcutText(
                    InputManager.Actions.StealMode,
                    InputManager.Actions.GrabMode,
                    InputManager.Actions.InfoMode,
                    InputManager.Actions.TalkMode);

            int fatm = DaggerfallEntity.FatigueMultiplier;
            vitalsPanel.ToolTipText =
                textManager.GetLocalizedText("health") + ": " + player.CurrentHealth + "/" + player.MaxHealth + "\r" +
                textManager.GetLocalizedText("fatigue") + ": " + player.CurrentFatigue / fatm + "/" + player.MaxFatigue / fatm + "\r" +
                textManager.GetLocalizedText("spellPoints") + ": " + player.CurrentMagicka + "/" + player.MaxMagicka;

            spellsButton.ToolTipText = GetShortcutText(InputManager.Actions.CastSpell);

            weaponButton.ToolTipText = GetShortcutText(InputManager.Actions.ReadyWeapon);

            useButton.ToolTipText = GetShortcutText(InputManager.Actions.UseMagicItem);

            transportButton.ToolTipText = GetShortcutText(InputManager.Actions.Transport);

            mapButton.ToolTipText = GetShortcutText(InputManager.Actions.AutoMap, InputManager.Actions.TravelMap);

            restButton.ToolTipText = GetShortcutText(InputManager.Actions.Rest);

            PlayerGPS gps = GameManager.Instance.PlayerGPS;
            DaggerfallDateTime dateTime = DaggerfallUnity.Instance.WorldTime.Now;
            if (gps.CurrentLocalizedLocationName != null)
                compassPanel.ToolTipText = gps.CurrentLocalizedLocationName + "\r";
            else
                compassPanel.ToolTipText = "The " + gps.CurrentLocalizedRegionName + " Hinterlands\r";

            if (RetroFrameMod.ShowErrorLog)
                compassPanel.ToolTipText = "";
            else
                compassPanel.ToolTipText +=
                    dateTime.DateString() + "\r" +
                    dateTime.MinTimeString() + "\r" +
                    GetShortcutText(InputManager.Actions.Status);

            //Tooltips for hotkeys are handled in the Hotkeys class
            //Tooltips for active effects are handled in UpdateActiveEffectIndicators
        }


        /// <summary>
        /// Gets the comma-separated (text) of the shortcut key(s) of the specified action.
        /// </summary>
        string GetShortcutText(params InputManager.Actions[] actions)
        {
            string text = "";

            if (!RetroFrameMod.ShowShortcutKeyInTooltip)
                return text;

            foreach (InputManager.Actions action in actions)
            {
                KeyCode code = InputManager.Instance.GetBinding(action);
                text += ControlsConfigManager.Instance.GetButtonText(code);
                text += ",";
            }
            text = text.TrimEnd(',');

            return "(" + text + ")";
        }


        /// <summary>
        /// Update head texture from the head in the LargeHUD.
        /// Create and/or apply alternate head textures for magical concealment effects.
        /// We don't need any headless players running around.
        /// </summary>
        void UpdateHeadTexture()
        {
            HUDLarge hud = DaggerfallUI.Instance.DaggerfallHUD.LargeHUD;

            if (triggerHeadChange || hud.HeadTexture == null)
            {
                //Steal the head texture from the LargeHUD character panel.
                hud.Enabled = true;
                hud.Update();
                hud.Enabled = false;

                if (hud.HeadTexture == null)
                    return;

                triggerHeadChange = false;

                //Create additional head textures to handle magical concealment effects.
                shadowHeadTexture = AlterPixels(hud.HeadTexture, 120, true);
                chameleonHeadTexture = AlterPixels(hud.HeadTexture, 40, false);
            }

            headPanel.Enabled = true; //but gets disabled for invisibility

            PlayerEntity player = GameManager.Instance.PlayerEntity;

            if (player.IsInvisible)
                headPanel.Enabled = false;
            else if (player.IsBlending)
                headPanel.BackgroundTexture = chameleonHeadTexture;
            else if (player.IsAShade)
                headPanel.BackgroundTexture = shadowHeadTexture;
            else
                headPanel.BackgroundTexture = hud.HeadTexture;

        }


        /// <summary>
        /// Used to lower pixel alpha values of a texture, and possibly darken as well.
        /// Useful if a character is using magical concealment.
        /// </summary>
        Texture2D AlterPixels(Texture2D tex, byte alpha, bool blacken)
        {
            Color32[] pixels = tex.GetPixels32();

            for (var i = 0; i < pixels.Length; ++i)
            {
                if (pixels[i].a != 0)
                {
                    pixels[i].a = alpha;

                    if (blacken)
                        pixels[i].r = pixels[i].g = pixels[i].b = 0;
                }
            }

            Texture2D newTexture = new Texture2D(tex.width, tex.height);
            newTexture.SetPixels32(pixels);
            newTexture.Apply(false);

            return newTexture;
        }



        /// <summary>
        /// Sets shading for the character portrait panel depending on local lighting conditions.
        /// Only used if First-Person-Lighting and Monster-University are installed.
        /// </summary>
        void UpdateHeadFrameShadeAndTint()
        {
            //If Monster-University is not installed, lighting has no impact on visibility,
            //which might confuse players.
            if (isMonsterUniversityInstalled)
            {
                DaggerfallEntityBehaviour entity = GameManager.Instance.PlayerEntityBehaviour;
                float grayScale = GetEntityLighting(entity).grayscale;
                float shading = 1f - grayScale;
                headShadePanel.BackgroundColor = new Color(0, 0, 0, shading);
            }
        }


        /// <summary>
        /// Can apply appropriate varying color effects to the character panel to indicate status, such as:
        ///  -low health
        ///  -low fatigue
        ///  -poison
        ///  -disease
        /// </summary>
        void UpdateCharacterStatusColor()
        {
            PlayerEntity player = GameManager.Instance.PlayerEntity;

            Color statusColor = Color.clear;

            if (player.CurrentHealth < player.MaxHealth / 4)
                statusColor = new Color(0.5f, 0, 0, 0.7f);
            else if (HasActivePoison())
                statusColor = new Color(0, 0.3f, 0, 0.7f);
            else if (player.CurrentFatigue < player.MaxFatigue / 8)
                statusColor = new Color(0, 0, 0, 0.7f);
            else if (HasActiveDisease())
                statusColor = new Color(0.41f, 0.22f, 0.15f, 0.7f);

            if (statusColor == Color.clear && HasCompletedPoison())
            {
                //For inactive but completed poisons, show unvarying green color.
                headTintPanel.BackgroundColor = new Color(0, 0.3f, 0, 0.3f);
                return;
            }

            const float cycleRate = 2;// 4;
            float colorLerpVal = Mathf.Abs(Mathf.Cos(Time.time * cycleRate));

            headTintPanel.BackgroundColor = statusColor * colorLerpVal;
        }


        /// <summary>
        /// Returns true if player-character has at least one active poison.
        /// </summary>
        bool HasActivePoison()
        {
            LiveEffectBundle[] bundles = GameManager.Instance.PlayerEffectManager.EffectBundles;

            foreach (LiveEffectBundle bundle in bundles)
            {
                if (bundle.bundleType != BundleTypes.Poison)
                    continue;

                foreach (IEntityEffect effect in bundle.liveEffects)
                    if (effect is PoisonEffect poison)
                        if (poison.CurrentState == PoisonEffect.PoisonStates.Active)
                            return true;
            }

            return false;
        }


        /// <summary>
        /// Returns true if player-character has at least one completed poison.
        /// </summary>
        bool HasCompletedPoison()
        {
            LiveEffectBundle[] bundles = GameManager.Instance.PlayerEffectManager.EffectBundles;

            foreach (LiveEffectBundle bundle in bundles)
                if (bundle.bundleType == BundleTypes.Poison)
                    foreach (IEntityEffect effect in bundle.liveEffects)
                        if (effect is PoisonEffect poison)
                            if (poison.CurrentState == PoisonEffect.PoisonStates.Complete)
                                return true;

            return false;
        }


        /// <summary>
        /// Returns true if player-character has at least disease that has finished incubating.
        /// </summary>
        bool HasActiveDisease()
        {
            LiveEffectBundle[] bundles = GameManager.Instance.PlayerEffectManager.EffectBundles;

            foreach (LiveEffectBundle bundle in bundles)
                if (bundle.bundleType == BundleTypes.Disease)
                    foreach (IEntityEffect effect in bundle.liveEffects)
                        if (effect is DiseaseEffect disease)
                            if (disease.IncubationOver)
                                return true;

            return false;
        }


        /// <summary>
        /// Updates the interaction mode button with the correct texture for the current activation mode.
        /// </summary>
        void UpdateInteractionModeTexture()
        {
            switch (GameManager.Instance.PlayerActivate.CurrentMode)
            {
                case PlayerActivateModes.Grab:
                    interactionModeButton.BackgroundTexture = grabModeTexture;
                    break;
                case PlayerActivateModes.Info:
                    interactionModeButton.BackgroundTexture = infoModeTexture;
                    break;
                case PlayerActivateModes.Steal:
                    interactionModeButton.BackgroundTexture = stealModeTexture;
                    break;
                case PlayerActivateModes.Talk:
                    interactionModeButton.BackgroundTexture = talkModeTexture;
                    break;
            }
        }


        /// <summary>
        /// Updates the active spell effect icons/descriptions in the left panel area.
        /// </summary>
        void UpdateActiveEffectIndicators()
        {
            //Clear the active effect panel
            foreach (Panel panel in effectIcons)
                panel.Enabled = false;

            foreach (TextLabel label in effectLabels)
            {
                label.Enabled = false;
                label.TextColor = Color.gray;
            }

            if (collapsedActiveEffects.Count == 0 && fullDisplayActiveEffects.Count == 0)
                return;

            bool blinkState = (Time.time * 100) % 50 < 25;

            int row = 0;
            int col = 0;
            foreach (LiveEffectBundle bundle in collapsedActiveEffects)
            {
                if (col >= effectCols)
                {
                    col = 0;
                    ++row;
                }

                if (row >= effectRows)
                    break;

                bool enabled = true;
                if (bundle.fromEquippedItem == null)
                {
                    bool expiring = (GetMaxRoundsRemaining(bundle) < 2);
                    enabled = !expiring || blinkState;
                }
                effectIcons[row, col].Enabled = enabled;
                effectIcons[row, col].BackgroundTexture = DaggerfallUI.Instance.SpellIconCollection.GetSpellIcon(bundle.icon);
                effectIcons[row, col].ToolTipText = bundle.name.TrimStart('!');

                ++col;
            }

            if (col != 0)
                ++row;

            foreach (LiveEffectBundle bundle in fullDisplayActiveEffects)
            {
                if (row >= effectRows)
                    break;

                effectLabels[row].Enabled = true;
                effectLabels[row].Text = bundle.name.TrimStart('!'); // Non-vendor spells start with !, don't show this on the UI
                Color color = Color.gray;
                if (bundle.caster == GameManager.Instance.PlayerEntityBehaviour)
                    color = new Color(0.4f, 0.4f, 0.7f, 1);
                else if (bundle.caster != null && bundle.caster.Entity.Team != MobileTeams.PlayerAlly)
                    color = new Color(1f, 0.2f, 0.2f, 1);
                effectLabels[row].TextColor = color;
                effectLabels[row].ToolTipText = bundle.name.TrimStart('!');

                bool enabled = true;
                if (bundle.fromEquippedItem == null)
                {
                    bool expiring = (GetMaxRoundsRemaining(bundle) < 2);
                    enabled = !expiring || blinkState;
                }
                effectIcons[row, 0].Enabled = enabled;
                effectIcons[row, 0].BackgroundTexture = DaggerfallUI.Instance.SpellIconCollection.GetSpellIcon(bundle.icon);
                effectIcons[row, 0].ToolTipText = bundle.name.TrimStart('!');

                ++row;
            }

        }


        /// <summary>
        /// A spell bundle can have multiple effects with different durations.
        /// This method returns the largest duration value for the specified spell bundle.
        /// </summary>
        int GetMaxRoundsRemaining(LiveEffectBundle bundle)
        {
            int maxRoundsRemaining = 0;

            foreach (IEntityEffect effect in bundle.liveEffects)
                if (effect.RoundsRemaining > maxRoundsRemaining)
                    maxRoundsRemaining = effect.RoundsRemaining;

            return maxRoundsRemaining;
        }



        /// <summary>
        /// The left panel shows icons/descriptions for incumbent spell effects.
        /// Instantaneous (non-incumbent) spells will briefly show an icon and descriptive text label
        /// in the lower left of the screen instead.
        /// </summary>
        void UpdateInstantaneousSpell()
        {
            if (Time.time > instantaneousSpellActivationTime + 1.5f)
            {
                instantaneousSpellIconContainer.Enabled = false;
                instantaneousSpellLabel.Enabled = false;
                instantaneousSpellLabel.Text = "";
            }
            else
            {
                if (instantaneousSpellIcon.BackgroundTexture != null)
                    instantaneousSpellIconContainer.Enabled = true;
                instantaneousSpellLabel.Enabled = true;
                //Gradually darken
                float alpha = Mathf.Clamp(Time.time - instantaneousSpellActivationTime - 0.5f, 0, 1);
                instantaneousSpellIconOverlay.BackgroundColor = new Color(0, 0, 0, alpha);
                instantaneousSpellLabel.TextColor *= 1 - (Time.deltaTime / 2);
            }
        }


        /// <summary>
        /// Gets the light levels around the player-character if the First-Person-Lighting mod is installed.
        /// Otherwise returns White.
        /// </summary>
        Color GetEntityLighting(DaggerfallEntityBehaviour entity)
        {
            Color lighting = Color.white;

            if (firstPersonLightingMod == null || !firstPersonLightingMod.IsReady)
                return lighting;

            firstPersonLightingMod.MessageReceiver("entityLighting", entity, (string message, object data) =>
            {
                lighting = (Color)data;
            });

            return lighting;
        }


        #endregion

        #region Event Handlers

        /// <summary>
        /// Enable the overlay and trigger head texture change.
        /// </summary>
        void StartGameBehaviour_OnStartGameHandler(object sender, EventArgs e)
        {
            Started = true;
            Enabled = true;
            RegisterActiveEffectChanges(null);
            triggerHeadChange = true;
        }


        /// <summary>
        /// Disable the overlay when loading starts.
        /// </summary>
        void SaveLoadManager_OnStartLoad(SaveData_v1 saveData)
        {
            Enabled = false;
        }


        /// <summary>
        /// Record currently active incumbent effects for display in the left panel area
        /// and trigger a head texture change.
        /// Enable the overlay.
        /// </summary>
        void SaveLoadManager_OnLoad(SaveData_v1 saveData)
        {
            Started = true;
            Enabled = true;
            RegisterActiveEffectChanges(null);
            triggerHeadChange = true;
        }


        /// <summary>
        /// Disable on player death.
        /// </summary>
        void OnPlayerDeath(object sender, EventArgs e)
        {
            Enabled = false;
        }


        /// <summary>
        /// Update currently active incumbent effects for display.
        /// </summary>
        void RegisterNewMagicRound()
        {
            RegisterActiveEffectChanges(null);
        }


        /// <summary>
        /// Update currently active incumbent effects for display.
        /// </summary>
        void RegisterAssignEffectChanges(LiveEffectBundle bundle)
        {
            RegisterActiveEffectChanges(null);
        }


        /// <summary>
        /// Update currently active incumbent effects for display.
        /// </summary>
        void RegisterRemoveEffectChanges(LiveEffectBundle bundle)
        {
            RegisterActiveEffectChanges(bundle);
        }


        /// <summary>
        /// Update currently active incumbent effects for display.
        /// </summary>
        void RegisterAddIncumbentEffectChanges()
        {
            RegisterActiveEffectChanges(null);
        }


        /// <summary>
        /// Update currently active incumbent effects to be displayed in the left panel area.
        /// </summary>
        void RegisterActiveEffectChanges(LiveEffectBundle bundleBeingRemoved)
        {
            collapsedActiveEffects.Clear();
            fullDisplayActiveEffects.Clear();

            // Get all effect bundles currently operating on player
            EntityEffectManager playerEffectManager = GameManager.Instance.PlayerEffectManager;
            LiveEffectBundle[] effectBundles = playerEffectManager.EffectBundles;
            if (effectBundles == null || effectBundles.Length == 0)
                return;

            equippedItemBundles.Clear();
            playerBundles.Clear();
            otherBundles.Clear();

            for (int i = 0; i < effectBundles.Length; i++)
            {
                LiveEffectBundle bundle = effectBundles[i];

                if (bundle == bundleBeingRemoved)
                    continue; //Ignore bundles that are being removed

                // Don't add effect icon for instant spells, must have at least 1 round remaining or be from an equipped item
                if (IsInstantaneous(bundle))
                {
                    instantaneousSpellIcon.BackgroundTexture = null;
                    if (ShowIcon(bundle))
                    {
                        Texture2D tex = DaggerfallUI.Instance.SpellIconCollection.GetSpellIcon(bundle.icon);
                        instantaneousSpellIcon.BackgroundTexture = tex;
                    }
                    instantaneousSpellLabel.Text = bundle.name;
                    if (bundle.caster == GameManager.Instance.PlayerEntityBehaviour)
                        instantaneousSpellLabel.TextColor = new Color(0.4f, 0.4f, 1f, 1);
                    else if (bundle.caster == null || bundle.caster.Entity.Team == MobileTeams.PlayerAlly)
                        instantaneousSpellLabel.TextColor = new Color(0.7f, 0.7f, 0.7f, 1);
                    else
                        instantaneousSpellLabel.TextColor = new Color(1f, 0.2f, 0.2f, 1);

                    instantaneousSpellActivationTime = Time.time;
                    continue;
                }

                if (!ShowIcon(bundle))
                    continue;

                if (bundle.fromEquippedItem != null)
                    equippedItemBundles.Add(bundle);
                else if (bundle.caster == GameManager.Instance.PlayerEntityBehaviour)
                    playerBundles.Add(bundle);
                else
                    otherBundles.Add(bundle);
            }

            int rowsNeeded = equippedItemBundles.Count + playerBundles.Count + otherBundles.Count;
            if (rowsNeeded == 0)
                return;

            //If too many rows, collapse the effects from equipped items.
            if (rowsNeeded > effectRows)
            {
                collapsedActiveEffects.AddRange(equippedItemBundles);

                int compressedRows = Mathf.CeilToInt((float)collapsedActiveEffects.Count / effectCols);
                rowsNeeded = compressedRows + playerBundles.Count + otherBundles.Count;
            }
            else
            {
                fullDisplayActiveEffects.AddRange(equippedItemBundles);
            }

            //If still too many rows, collapse the effects from player.
            if (rowsNeeded > effectRows)
            {
                collapsedActiveEffects.AddRange(playerBundles);

                int compressedRows = Mathf.CeilToInt((float)collapsedActiveEffects.Count / effectCols);
                rowsNeeded = compressedRows + otherBundles.Count;
            }
            else
            {
                fullDisplayActiveEffects.AddRange(playerBundles);
            }

            //If still too many rows, collapse the rest of the effects.
            if (rowsNeeded > effectRows)
                collapsedActiveEffects.AddRange(otherBundles);
            else
                fullDisplayActiveEffects.AddRange(otherBundles);

        }


        /// <summary>
        /// Returns false if:
        ///   -spell effects in the bundle support duration
        ///   -is from an equipped item
        /// Returns true otherwise.
        /// </summary>
        bool IsInstantaneous(LiveEffectBundle bundle)
        {
            if (bundle.fromEquippedItem != null)
                return false;

            bool supportsDuration = false;

            foreach (IEntityEffect effect in bundle.liveEffects)
            {
                supportsDuration |= effect.Properties.SupportDuration;
                supportsDuration |= effect.RoundsRemaining > 0;
            }

            return !supportsDuration;
        }


        /// <summary>
        /// Returns true if at least one effect in the spell bundle wants to show the icon and either has remaining rounds
        /// or is an equipped item effect.
        /// </summary>
        bool ShowIcon(LiveEffectBundle bundle)
        {
            foreach (IEntityEffect effect in bundle.liveEffects)
                if (effect.Properties.ShowSpellIcon && (effect.RoundsRemaining >= 0 || bundle.fromEquippedItem != null))
                    return true;

            return false;
        }


        /// <summary>
        /// Opens character sheet if game not paused.
        /// </summary>
        void CharacterPanel_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!GameManager.IsGamePaused && GameManager.Instance.PlayerMouseLook.cursorActive)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PostMessage(DaggerfallUIMessages.dfuiOpenCharacterSheetWindow);
            }
        }


        /// <summary>
        /// Opens inventory window if game not paused.
        /// </summary>
        void InventoryButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!GameManager.IsGamePaused && GameManager.Instance.PlayerMouseLook.cursorActive)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PostMessage(DaggerfallUIMessages.dfuiOpenInventoryWindow);
            }
        }


        /// <summary>
        /// Changes interaction mode to the next in sequence.
        /// </summary>
        void InteractionModeButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (GameManager.IsGamePaused || !GameManager.Instance.PlayerMouseLook.cursorActive)
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);

            // Cycle interaction mode forward on left click
            // Using same cycling pattern as original Daggerfall.
            switch (GameManager.Instance.PlayerActivate.CurrentMode)
            {
                case PlayerActivateModes.Grab:
                    GameManager.Instance.PlayerActivate.ChangeInteractionMode(PlayerActivateModes.Info);
                    break;
                case PlayerActivateModes.Info:
                    GameManager.Instance.PlayerActivate.ChangeInteractionMode(PlayerActivateModes.Steal);
                    break;
                case PlayerActivateModes.Steal:
                    GameManager.Instance.PlayerActivate.ChangeInteractionMode(PlayerActivateModes.Talk);
                    break;
                case PlayerActivateModes.Talk:
                    GameManager.Instance.PlayerActivate.ChangeInteractionMode(PlayerActivateModes.Grab);
                    break;
            }
        }


        /// <summary>
        /// Changes interaction mode to the previous in sequence.
        /// </summary>
        void InteractionModeButton_OnRightMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (GameManager.IsGamePaused || !GameManager.Instance.PlayerMouseLook.cursorActive)
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);

            // Cycle interaction mode backward on right click.
            // Using same cycling pattern as original Daggerfall.
            switch (GameManager.Instance.PlayerActivate.CurrentMode)
            {
                case PlayerActivateModes.Grab:
                    GameManager.Instance.PlayerActivate.ChangeInteractionMode(PlayerActivateModes.Talk);
                    break;
                case PlayerActivateModes.Info:
                    GameManager.Instance.PlayerActivate.ChangeInteractionMode(PlayerActivateModes.Grab);
                    break;
                case PlayerActivateModes.Steal:
                    GameManager.Instance.PlayerActivate.ChangeInteractionMode(PlayerActivateModes.Info);
                    break;
                case PlayerActivateModes.Talk:
                    GameManager.Instance.PlayerActivate.ChangeInteractionMode(PlayerActivateModes.Steal);
                    break;
            }
        }


        /// <summary>
        /// Set right button panel to its default state.
        /// </summary>
        void RightPanel_OnMouseLeave(BaseScreenComponent sender)
        {
            isOverTogglePanelButton = false;
        }


        /// <summary>
        /// Opens/Closes the spellbook window.
        /// </summary>
        void SpellsButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!GameManager.Instance.PlayerMouseLook.cursorActive)
            {
                return;
            }
            else if (DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallSpellBookWindow)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PopWindow();
            }
            else if (!GameManager.IsGamePaused)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PostMessage(DaggerfallUIMessages.dfuiOpenSpellBookWindow);
            }
        }


        /// <summary>
        /// Sheathes/Unsheathes currently equipped weapon.
        /// </summary>
        void WeaponButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!GameManager.IsGamePaused && GameManager.Instance.PlayerMouseLook.cursorActive)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                GameManager.Instance.WeaponManager.ToggleSheath();
            }
        }


        /// <summary>
        /// Opens/Closes the UseMagicItem window.
        /// </summary>
        void UseButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!GameManager.Instance.PlayerMouseLook.cursorActive)
            {
                return;
            }
            else if (DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallUseMagicItemWindow)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PopWindow();
            }
            else if (!GameManager.IsGamePaused)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PostMessage(DaggerfallUIMessages.dfuiOpenUseMagicItemWindow);
            }
        }


        /// <summary>
        /// Opens/Closes the transport window.
        /// </summary>
        void TransportButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!GameManager.Instance.PlayerMouseLook.cursorActive)
            {
                return;
            }
            else if (DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallTransportWindow)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PopWindow();
            }
            else if (!GameManager.IsGamePaused)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PostMessage(DaggerfallUIMessages.dfuiOpenTransportWindow);
            }
        }


        /// <summary>
        /// Opens/Closes the automap window.
        /// </summary>
        void MapButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!GameManager.Instance.PlayerMouseLook.cursorActive)
            {
                return;
            }
            else if (!GameManager.IsGamePaused)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PostMessage(DaggerfallUIMessages.dfuiOpenAutomap);
            }
            else if (DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallAutomapWindow ||
                DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallExteriorAutomapWindow)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PopWindow();
            }
            else if (DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallTravelMapWindow)
            {
                DaggerfallUI.Instance.UserInterfaceManager.PopWindow();
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PostMessage(DaggerfallUIMessages.dfuiOpenAutomap);
            }
        }


        /// <summary>
        /// Opens/Closes the travel map window.
        /// </summary>
        void MapButton_OnRightMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!GameManager.Instance.PlayerMouseLook.cursorActive)
            {
                return;
            }
            else if (DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallTravelMapWindow)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PopWindow();
            }
            else if (DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallAutomapWindow ||
                DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallExteriorAutomapWindow)
            {
                DaggerfallUI.Instance.UserInterfaceManager.PopWindow();
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PostMessage(DaggerfallUIMessages.dfuiOpenTravelMapWindow);
            }
            else if (!GameManager.IsGamePaused)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PostMessage(DaggerfallUIMessages.dfuiOpenTravelMapWindow);
            }
        }


        /// <summary>
        /// Opens/Closes the rest window.
        /// </summary>
        void RestButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!GameManager.Instance.PlayerMouseLook.cursorActive)
            {
                return;
            }
            else if (DaggerfallUI.Instance.UserInterfaceManager.TopWindow is DaggerfallRestWindow)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PopWindow();
            }
            else if (!GameManager.IsGamePaused)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PostMessage(DaggerfallUIMessages.dfuiOpenRestWindow);
            }
        }



        /// <summary>
        /// Show popup dialog for hotkey button.
        /// </summary>
        void HotkeyButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!GameManager.Instance.PlayerMouseLook.cursorActive)
            {
                return;
            }

            int index = int.Parse((string)sender.Tag);

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);

            hotkeyPopup.Show(index);
        }



        /// <summary>
        /// Deletes the hotkey.
        /// </summary>
        void HotkeyButton_OnRightMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!GameManager.Instance.PlayerMouseLook.cursorActive)
            {
                return;
            }
            else
            {
                int index = int.Parse((string)sender.Tag);
                Hotkeys.Set(index, null);  //delete the hotkey
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            }
        }


        /// <summary>
        /// Set HoveredHotkeyButtonIndex for the sending button.
        /// </summary>
        void HotkeyButton_OnMouseEnter(BaseScreenComponent sender)
        {
            HoveredHotkeyButtonIndex = int.Parse((string)sender.Tag);
        }


        /// <summary>
        /// Set HoveredHotKeyButtonIndex to -1 when leaving hotkeys panel.
        /// </summary>
        void HotkeysPanel_OnMouseLeave(BaseScreenComponent sender)
        {
            HoveredHotkeyButtonIndex = -1;
        }




        /// <summary>
        /// Toggles which button panel (actions or hotkeys) is shown by default in the right panel.
        /// </summary>
        void TogglePanelButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!GameManager.Instance.PlayerMouseLook.cursorActive)
            {
                return;
            }
            else
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                ActionsPanelLocked = !ActionsPanelLocked;
                isOverTogglePanelButton = false;
            }
        }


        /// <summary>
        /// Temporarily shows alternate buttons in right panel, until the mouse leaves the right panel.
        /// </summary>
        void TogglePanelButton_OnMouseEnter(BaseScreenComponent sender)
        {
            if (GameManager.Instance.PlayerMouseLook.cursorActive)
            {
                isOverTogglePanelButton = true;
            }
        }


        /// <summary>
        /// Changes current hotkey group.
        /// </summary>
        void ChangeHotkeyGroupButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!GameManager.Instance.PlayerMouseLook.cursorActive)
                return;
            else if (sender.Tag.Equals("NextButtonPanel"))
                ++Hotkeys.CurrentGroupIndex;
            else
                --Hotkeys.CurrentGroupIndex;

            Hotkeys.PlayGroupSwitchNote();
        }



        /// <summary>
        /// Opens character status winow.
        /// </summary>
        void CompassPanel_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (RetroFrameMod.ShowErrorLog)
                return;

            if (!GameManager.IsGamePaused && GameManager.Instance.PlayerMouseLook.cursorActive)
            {
                DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
                DaggerfallUI.Instance.UserInterfaceManager.PostMessage(DaggerfallUIMessages.dfuiStatusInfo);
            }
        }


        void ErrorLogIcon_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!GameManager.Instance.PlayerMouseLook.cursorActive)
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);

            RetroFrameMod.LockErrorLog = !RetroFrameMod.LockErrorLog;
        }

        void ErrorLogIcon_OnRightMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!GameManager.Instance.PlayerMouseLook.cursorActive)
                return;

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);

            RetroFrameMod.ShowErrorLogIndicator = false;
        }


        void ErrorLogIcon_OnMouseEnter(BaseScreenComponent sender)
        {
            if (GameManager.Instance.PlayerMouseLook.cursorActive)
                RetroFrameMod.ShowErrorLog = true;
        }

        void ErrorLogIcon_OnMouseLeave(BaseScreenComponent sender)
        {
            RetroFrameMod.ShowErrorLog = false;
        }

        #endregion




    } //class OverlayPanel


} //namespace
