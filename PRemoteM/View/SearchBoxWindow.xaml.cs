﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PRM.Core.Model;
using PRM.ViewModel;
using Shawn.Ulits;

namespace PRM.View
{
    /// <summary>
    /// SearchBoxWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SearchBoxWindow : Window
    {
        private readonly VmSearchBox _vmSearchBox = null;


        public SearchBoxWindow()
        {
            InitializeComponent();
            ShowInTaskbar = false;
            _vmSearchBox = new VmSearchBox();
            DataContext = _vmSearchBox;
            Loaded += (sender, args) =>
            {
                HideMe();
                Deactivated += (sender1, args1) => { Dispatcher.Invoke(HideMe); };
                KeyDown += (sender1, args1) =>
                {
                    if (args1.Key == Key.Escape) HideMe();
                };
            };
            Show();

            SystemConfig.Instance.QuickConnect.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(SystemConfigQuickConnect.HotKeyKey) ||
                    args.PropertyName == nameof(SystemConfigQuickConnect.HotKeyModifiers))
                {
                    SetHotKey();
                }
            };
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            // make popup control moves with parent by https://stackoverflow.com/questions/5736359/popup-control-moves-with-parent
            PopupSelections.HorizontalOffset += 1;
            PopupSelections.HorizontalOffset -= 1;
            base.OnLocationChanged(e);
        }

        private readonly object _closeLocker = new object();
        private bool _isHidden = false;
        private void HideMe()
        {
            if (_isHidden == false)
                lock (_closeLocker)
                {
                    if (_isHidden == false)
                    {
                        this.Visibility = Visibility.Hidden;
                        _vmSearchBox.DispNameFilter = "";
                        _vmSearchBox.PopupIsOpen = false;
                        _isHidden = true;
                        this.Hide();
                    }
                }
        }




        public void ShowMe()
        {
            SimpleLogHelper.Info("Call shortcut to invoke quick window.");
            if (SystemConfig.Instance.QuickConnect.Enable)
                if (_isHidden == true)
                    lock (_closeLocker)
                    {
                        if (_isHidden == true)
                        {
                            var p = ScreenInfoEx.GetMouseScreenPosition();
                            var screenEx = ScreenInfoEx.GetCurrentScreen(new System.Drawing.Point((int)p.X,(int)p.Y));
                            this.Top = screenEx.VirtualWorkingAreaCenter.Y - this.Height / 2;
                            this.Left = screenEx.VirtualWorkingAreaCenter.X - this.Width / 2;
                            this.Show();
                            this.Visibility = Visibility.Visible;
                            this.Activate();
                            this.Topmost = true;  // important
                            this.Topmost = false; // important
                            this.Focus();         // important
                            TbKeyWord.Focus();
                            _isHidden = false;
                        }
                    }
        }










        private void WindowHeader_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }




        private readonly object _keyDownLocker = new object();
        private void TbKeyWord_OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    HideMe();
                    break;

                case Key.Enter:
                    {
                        lock (_closeLocker)
                        {
                            var i = _vmSearchBox.SelectedServerIndex;
                            var j = _vmSearchBox.Servers.Count;
                            if (i < j && i >= 0)
                            {
                                var s = _vmSearchBox.Servers[i];
                                s.Server.Conn();
                            }
                        }
                        HideMe();
                        break;
                    }

                case Key.Down:
                    {
                        lock (_keyDownLocker)
                        {
                            if (_vmSearchBox.SelectedServerIndex < _vmSearchBox.Servers.Count - 1)
                            {
                                ++_vmSearchBox.SelectedServerIndex;
                                ListBoxSelections.ScrollIntoView(ListBoxSelections.SelectedItem);
                            }
                        }
                        break;
                    }

                case Key.Up:
                    {
                        lock (_keyDownLocker)
                        {
                            if (_vmSearchBox.SelectedServerIndex > 0)
                            {
                                --_vmSearchBox.SelectedServerIndex;
                                ListBoxSelections.ScrollIntoView(ListBoxSelections.SelectedItem);
                            }
                        }
                        break;
                    }

                case Key.PageUp:
                    {
                        lock (_keyDownLocker)
                        {
                            var i = _vmSearchBox.SelectedServerIndex - 5;
                            if (i < 0)
                                i = 0;
                            _vmSearchBox.SelectedServerIndex = i;
                            ListBoxSelections.ScrollIntoView(ListBoxSelections.SelectedItem);
                        }
                        break;
                    }

                case Key.PageDown:
                    {
                        lock (_keyDownLocker)
                        {
                            var i = _vmSearchBox.SelectedServerIndex + 5;
                            if (i > _vmSearchBox.Servers.Count - 1)
                                i = _vmSearchBox.Servers.Count - 1;
                            _vmSearchBox.SelectedServerIndex = i;
                            ListBoxSelections.ScrollIntoView(ListBoxSelections.SelectedItem);
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// use it after Show() has been called
        /// </summary>
        public void SetHotKey()
        {
            GlobalHotkeyHooker.Instance.Unregist(this);
            var r = GlobalHotkeyHooker.Instance.Regist(this, SystemConfig.Instance.QuickConnect.HotKeyModifiers, SystemConfig.Instance.QuickConnect.HotKeyKey, this.ShowMe);
            var title = SystemConfig.Instance.Language.GetText("messagebox_title_warning");
            switch (r.Item1)
            {
                case GlobalHotkeyHooker.RetCode.Success:
                    break;
                case GlobalHotkeyHooker.RetCode.ERROR_HOTKEY_NOT_REGISTERED:
                {
                    var msg = $"{SystemConfig.Instance.Language.GetText("info_hotkey_registered_fail")}: {r.Item2}";
                    SimpleLogHelper.Warning(msg);
                    MessageBox.Show(msg, title);
                    break;
                }
                case GlobalHotkeyHooker.RetCode.ERROR_HOTKEY_ALREADY_REGISTERED:
                {
                    var msg = $"{SystemConfig.Instance.Language.GetText("info_hotkey_already_registered")}: {r.Item2}";
                    SimpleLogHelper.Warning(msg);
                    MessageBox.Show(msg, title);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
