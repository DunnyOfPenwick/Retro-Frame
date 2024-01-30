# Retro-Frame
 Source for the Retro-Frame mod for Daggerfall Unity.
 I build with the pre-compile option.

The Retro-Frame mod offers a couple of message handlers to allow other mods to register
custom hotkey actions as well as to retrieve the content of the overlay panel.
See https://www.dfworkshop.net/projects/daggerfall-unity/modding/features/#mods-interaction


## Registering Custom Hotkey Action Handlers
Use the "registerCustomHotkeyHandler" message, providing a tuple with a ('Description' string, a Texture2D icon), and then the handler method.
The handler will be passed the original Description string you provided and the index number of the hotkey that was activated.
Example:
```
void Start()
{
    Mod retroFrame = ModManager.Instance.GetMod("Retro-Frame");
    if (retroFrame != null)
    {
        retroFrame.MessageReceiver("registerCustomHotkeyHandler", ("MyDescription", myIconTexture2D), MyHandler);
    }
}

void MyHandler(string description, object index)
{
    //your handling code here
}
```


## Getting The Overlay Panel Content
Use the "getOverlay" message.  It will return the overlay Panel which you can then modify.
The overlay Panel contains numerous nested child panels, the structure is shown below the example code.
Example of changing the Weapon button background texture:
```
void Start()
{
    Mod retroFrame = ModManager.Instance.GetMod("Retro-Frame");
    if (retroFrame != null)
    {
        Panel overlay = null;

        retroFrame.MessageReceiver("getOverlay", null, (string message, object data) => { overlay = (Panel)data; } );
        
        if (overlay != null)
        {
            Panel main = (Panel)FindChild(overlay, "MainPanel");
            Panel right = (Panel)FindChild(main, "RightPanel");
            Panel actions = (Panel)FindChild(right, "ActionsPanel");
            Panel weapon = (Panel)FindChild(actions, "WeaponButtonPanel");
            weapon.BackgroundTexture = MyReplacementTexture;
        }

    }
}

BaseScreenComponent FindChild(Panel parent, string childTag)
{
    foreach (BaseScreenComponent child in parent.Components)
    {
        if (child.Tag != null && child.Tag.ToString().Equals(childTag, StringComparison.OrdinalIgnoreCase))
            return child;
    }

    throw new Exception($"Could not find child component with tag '{childTag}'.");
}

```

### Structure of Overlay Panel Content
- OverlayPanel
    MainPanel
        LeftPanel
            CharacterPanel
                HeadPanel
                NameLabel
            InventoryButtonPanel
            InteractionModeButtonPanel
            VitalsPanel
                HUDVitalsBars
            ActiveEffectsPanel
                ActiveEffectsRowPanel_ (where _ is 0 through 5)
                    DescriptionLabel_
                    Icon_ (3 icons per row)
            LeftPanelPauseGameOverlay
        InstSpellIconContainer
            (untagged icon)
            (untagged icon overlay)
        InstSpellLabel
        RightPanel
            ActionsPanel
                SpellsButtonPanel
                UseButtonPanel
                WeaponButtonPanel
                TransportButtonPanel
                MapButtonPanel
                RestButtonPanel
            HotkeysPanel
                _ (where _ is the panel number, 0-9)
                    IconContainer
                        Icon
                        IconCutout
                        Animation
                        ItemCountLabel
                    CharLabel (This is the label that shows the key bound to the hotkey)
                    DescriptionLabel
            CompassPanel
                CompassPointerPanel
            TogglePanelButtonPanel
            PriorButtonPanel
            NextButtonPanel
            RightPanelPauseGameOverlay
        ErrorLogIcon
            ErrorLogCountLabel
        ViewPanel
            TopBorderPanel
            BottomBorderPanel
    ToolTipContainerPanel






