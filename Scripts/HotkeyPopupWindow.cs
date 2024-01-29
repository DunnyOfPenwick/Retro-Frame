// Project:     Retro-Frame for Daggerfall Unity
// Author:      DunnyOfPenwick
// Origin Date: January 2024

using System.Linq;
using UnityEngine;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop;

namespace RetroFrame
{

    public class HotkeyPopupWindow : DaggerfallPopupWindow
    {
        readonly Panel mainPanel = new Panel();
        readonly MultiFormatTextLabel instructions = new MultiFormatTextLabel();
        readonly ListBox listBox = new ListBox();
        readonly VerticalScrollBar scrollBar = new VerticalScrollBar();
        readonly TextLabel cancelButton = new TextLabel();

        int hotkeyIndex;



        public HotkeyPopupWindow(IUserInterfaceManager uiManager) : base(uiManager)
        {
        }


        public void Show(int hotkeyIndex)
        {
            this.hotkeyIndex = hotkeyIndex;

            if (!(DaggerfallUI.Instance.UserInterfaceManager.TopWindow is HotkeyPopupWindow))
                DaggerfallUI.UIManager.PushWindow(this);
        }


        public override void OnPush()
        {
            listBox.SelectIndex(1);
            scrollBar.ScrollIndex = 0;
            base.OnPush();
        }


        protected override void Setup()
        {
            NativePanel.Components.Add(mainPanel);
            mainPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mainPanel.VerticalAlignment = VerticalAlignment.Middle;
            mainPanel.Size = new Vector2(240, 103);
            DaggerfallUI.Instance.SetDaggerfallPopupStyle(DaggerfallUI.PopupStyle.Parchment, mainPanel);

            //=================Instructions
            mainPanel.Components.Add(instructions);
            instructions.HorizontalAlignment = HorizontalAlignment.Left;
            instructions.VerticalAlignment = VerticalAlignment.Top;
            instructions.WrapText = true;
            instructions.WrapWords = true;
            instructions.MaxTextWidth = 220;
            instructions.AddTextLabel(Text.Hotkeys, null, new Color(0.8f, 0.6f, 0));
            instructions.NewLine();
            instructions.AddTextLabel(Text.Set, null, DaggerfallUI.DaggerfallDefaultTextColor);
            //To fix problem with MultiFormatTextLabel.  MFTL needs to move parent set immediately after new and
            //getRectangle() on TextLabel to force LocalScale to be set correctly.
            if (instructions.TextLabels.Last().LocalScale == Vector2.one) //not scaled correctly
            {
                float fwidth = DaggerfallUI.DefaultFont.CalculateTextWidth(Text.Set, new Vector2(3.5f, 4.2f));
                if (fwidth > instructions.MaxTextWidth)
                    instructions.NewLine(); //Word wrap not working correctly. See above.
            }
            instructions.NewLine();
            instructions.AddTextLabel(Text.Clear, null, DaggerfallUI.DaggerfallDefaultTextColor);
            instructions.NewLine();
            instructions.AddTextLabel(Text.Choose, null, DaggerfallUI.DaggerfallDefaultTextColor);

            //===================Listbox of Custom Callback
            Panel listBoxContainer = new Panel();
            mainPanel.Components.Add(listBoxContainer);
            listBoxContainer.Position = new Vector2(70, 28);
            listBoxContainer.Size = new Vector2(115, 55);

            listBoxContainer.Components.Add(listBox);
            listBox.VerticalAlignment = VerticalAlignment.Top;
            listBox.HorizontalAlignment = HorizontalAlignment.Center;
            listBox.Size = new Vector2(listBoxContainer.Size.x - 5, listBoxContainer.Size.y);
            listBox.BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            listBox.AlwaysAcceptKeyboardInput = true;
            listBox.RowsDisplayed = 7;
            listBox.OnScroll += () => scrollBar.ScrollIndex = listBox.ScrollIndex;
            listBox.OnUseSelectedItem += ListBox_OnUseSelectedItem;
            listBox.OnMouseScrollDown += ListBox_OnMouseScroll;
            listBox.OnMouseScrollUp += ListBox_OnMouseScroll;

            listBoxContainer.Components.Add(scrollBar);
            scrollBar.Size = new Vector2(5, listBoxContainer.Size.y);
            scrollBar.HorizontalAlignment = HorizontalAlignment.Right;
            scrollBar.VerticalAlignment = VerticalAlignment.Top;
            scrollBar.BackgroundColor = Color.grey;
            scrollBar.DisplayUnits = listBox.RowsDisplayed;
            scrollBar.ScrollIndex = 0;
            scrollBar.OnScroll += () => listBox.ScrollIndex = scrollBar.ScrollIndex;


            listBox.AddItem(Text.ClearHotkey, out ListBox.ListItem itemOut);
            itemOut.textColor = new Color(0, 0.5f, 1);
            itemOut.selectedTextColor = new Color(0.8f, 0, 0);

            foreach (RetroFrameMod.HotkeyCallbackInfo callback in RetroFrameMod.HotkeyCallbacks)
            {
                listBox.AddItem(callback.Description, out itemOut);
                itemOut.tag = callback;
                itemOut.selectedTextColor = new Color(0.8f, 0, 0);
            }

            scrollBar.TotalUnits = listBox.Count;
            listBox.SelectIndex(1);

            // Cancel button
            cancelButton.Text = Text.Cancel;
            cancelButton.BackgroundColor = new Color(0.4f, 0.3f, 0);
            cancelButton.HorizontalAlignment = HorizontalAlignment.Right;
            cancelButton.VerticalAlignment = VerticalAlignment.Bottom;
            cancelButton.OnMouseClick += CancelButton_OnMouseClick;
            cancelButton.OnMouseEnter += CancelButton_OnMouseEnter;
            cancelButton.OnMouseLeave += CancelButton_OnMouseLeave;
            mainPanel.Components.Add(cancelButton);

        }


        protected virtual void ListBox_OnMouseScroll(BaseScreenComponent sender)
        {
            scrollBar.ScrollIndex = listBox.ScrollIndex;
        }


        void CancelButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            CloseWindow();
        }


        void CancelButton_OnMouseEnter(BaseScreenComponent sender)
        {
            cancelButton.TextColor = Color.red;
        }

        void CancelButton_OnMouseLeave(BaseScreenComponent sender)
        {
            cancelButton.TextColor = DaggerfallUI.DaggerfallDefaultTextColor;
        }


        /// <summary>
        /// Called when listbox entry is 'used' (double-click or Enter) to set the hotkey value.
        /// </summary>
        void ListBox_OnUseSelectedItem()
        {
            if (listBox.SelectedIndex == 0) //The first item should be '<Clear Hotkey>'
                Hotkeys.Set(hotkeyIndex, null);
            else
                Hotkeys.Set(hotkeyIndex, listBox.SelectedValue.tag);

            DaggerfallUI.Instance.PlayOneShot(SoundClips.ButtonClick);
            CloseWindow();
        }




    } //class HotkeyPopupWindow



} //namespace
