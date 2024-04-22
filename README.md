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
See the ***'DunnyOfPenwick/Retro-Frame-Custom'*** repository for a pre-built mod you can use for customization.

Use the "getOverlay" message.  It will return the overlay Panel which you can then modify.
The overlay Panel contains numerous nested child panels, the structure is shown below the example code.
Each panel and label has a Tag containing a string identifying it.
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
            //Note: If you want to change what the button actually does on-click, disable the original
            //button and create another with the same Size and Position and add your own event handler.
            //Then add your new button to the ActionsPanel Components list.
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

### Structure of Overlay Panel Content (current as of v1.1.1)
- OverlayPanel (default BackgroundTexture is 'Frame')
    - MainPanel
        - LeftPanel
            - CharacterPanel (default BackgroundTexture is 'HeadFrame')
                - HeadPanel (BackgroundTexture gets periodically refreshed from the standard big HUD character panel when needed)
                   - HeadShadePanel (to be used by future mod)
                       - HeadTintPanel (normally transparent, can flash colors to indicate character status)
                - NameLabel
            - InventoryButtonPanel
            - InteractionModeButtonPanel
            - VitalsPanel (default BackgroundTexture is 'VitalsFrame')
                - HUDVitalsBars
            - ActiveEffectsPanel
                - ActiveEffectsRowPanel_ (where _ is 0 through 5)
                    - DescriptionLabel
                    - Icon_ (where _ is 0 to 2, so 3 icons per row)
                        - IconCutout (default BackgroundTexture 'IconCutout')
            - LeftPanelPauseGameOverlay (Darkens left panel when game paused)
        - InstSpellIconContainer (this is the icon for any instantaneous spell that is briefly shown at the bottom of the screen)
            - InstSpellIcon
            - InstSpellIconOverlay
        - InstSpellLabel (where the name of the instantaneous spell is shown)
        - RightPanel
            - ActionsPanel
                - SpellsButtonPanel (The default BackgroundTexture for the buttons is clipped from internal storage)
                - UseButtonPanel
                - WeaponButtonPanel
                - TransportButtonPanel
                - MapButtonPanel
                - RestButtonPanel
            - HotkeysPanel
                - _ (where _ is the panel number, 0-9) (default BackgroundTexture is 'HotkeyButton')
                    - IconContainer
                        - Icon
                        - IconCutout (default BackgroundTexture is 'HotkeyIconCutout')
                        - Animation (for the animated swirl around magic items)
                        - ItemCountLabel
                    - CharLabel (This is the label that shows the key bound to the hotkey)
                    - DescriptionLabel
            - CompassPanel (default BackgroundTexture clipped from internal storage)
                - CompassPointerPanel (default BackgroundTexture is from an array of 32 textures from internal storage, swapped on Update)
            - TogglePanelButtonPanel (default BackgroundTexture is 'Switch')
            - PriorButtonPanel (default BackgroundTexture is 'Prior')
            - NextButtonPanel (default BackgroundTexture is 'Next')
            - RightPanelPauseGameOverlay (Darkens right panel when game paused)
        - ErrorLogIcon (default BackgroundTexture is 'ErrorLogIcon')
            - ErrorLogCountLabel
        - ViewPanel
            - TopBorderPanel (default BackgroundTexture is 'TopBorder')
            - BottomBorderPanel (default BackgroundTexture is 'BottomBorder')
    - ToolTipContainerPanel (needed so tooltips scale correctly)






