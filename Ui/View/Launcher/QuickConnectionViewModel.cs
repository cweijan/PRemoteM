﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using _1RM.Controls.NoteDisplay;
using _1RM.Model;
using _1RM.Model.Protocol;
using _1RM.Model.Protocol.Base;
using _1RM.Service;
using _1RM.Utils;
using _1RM.View.Editor;
using Shawn.Utils;
using Shawn.Utils.Interface;
using Shawn.Utils.Wpf;
using Shawn.Utils.Wpf.PageHost;
using Stylet;

namespace _1RM.View.Launcher
{
    public class QuickConnectionViewModel : NotifyPropertyChangedBaseScreen
    {
        private LauncherWindowViewModel? _launcherWindowViewModel;
        public readonly QuickConnectionItem OpenConnectActionItem;
        public TextBox TbKeyWord { get; private set; } = new TextBox();
        //private LauncherWindowViewModel _launcherWindowViewModel;
        public List<ProtocolBaseWithAddressPort> Protocols { get; }
        public QuickConnectionViewModel()
        {
            Protocols = new List<ProtocolBaseWithAddressPort>();
            var protocols = ProtocolBase.GetAllSubInstance().Select(x => x as ProtocolBaseWithAddressPort).Where(x => x != null).ToList();
            foreach (var protocol in protocols)
            {
                if (protocol != null
                    && protocol.Protocol != RdpApp.ProtocolName)
                {
                    Protocols.Add(protocol);
                }
            }
            _selectedProtocol = Protocols.First(x => x.Protocol == RDP.ProtocolName);


            OpenConnectActionItem = new QuickConnectionItem()
            {
                Host = IoC.Get<ILanguageService>().Translate("Connect"),
            };
        }


        public void Init(LauncherWindowViewModel launcherWindowViewModel)
        {
            _launcherWindowViewModel = launcherWindowViewModel;
        }

        protected override void OnViewLoaded()
        {
            if (this.View is QuickConnectionView window)
            {
                TbKeyWord = window.TbKeyWord;
                TbKeyWord.Focus();
                RebuildConnectionHistory();
            }
        }

        public void Show()
        {
            if (_launcherWindowViewModel == null) return;
            Filter = "";
            RebuildConnectionHistory();
            Execute.OnUIThread(() =>
            {
                _launcherWindowViewModel.ServerSelectionsViewVisibility = Visibility.Collapsed;
                TbKeyWord.Focus();
            });
        }


        private readonly DebounceDispatcher _debounceDispatcher = new();
        private string _filter = "";
        public string Filter
        {
            get => _filter;
            set
            {
                if (SetAndNotifyIfChanged(ref _filter, value))
                {
                    _debounceDispatcher.Debounce(150, (obj) =>
                    {
                        if (value == _filter)
                        {
                            SimpleLogHelper.Warning("CalcVisibleByFilter");
                            CalcVisibleByFilter();
                        }
                    });
                }
            }
        }

        private ProtocolBaseWithAddressPort _selectedProtocol;
        public ProtocolBaseWithAddressPort SelectedProtocol
        {
            get => _selectedProtocol;
            set => SetAndNotifyIfChanged(ref _selectedProtocol, value);
        }


        private int _selectedIndex = 0;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (SetAndNotifyIfChanged(ref _selectedIndex, value))
                {
                    if (this.View is QuickConnectionView view)
                    {
                        Execute.OnUIThread(() =>
                        {
                            view.ListBoxHistory.ScrollIntoView(view.ListBoxHistory.SelectedItem);
                        });
                    }
                }
            }
        }

        private ObservableCollection<QuickConnectionItem> _connectHistory = new ObservableCollection<QuickConnectionItem>();
        public ObservableCollection<QuickConnectionItem> ConnectHistory
        {
            get => _connectHistory;
            set
            {
                if (SetAndNotifyIfChanged(ref _connectHistory, value))
                {
                    SelectedIndex = 0;
                }
            }
        }

        public void RebuildConnectionHistory()
        {
            var list = IoC.Get<LocalityService>().QuickConnectionHistory.ToList();
            list.Insert(0, OpenConnectActionItem);
            ConnectHistory = new ObservableCollection<QuickConnectionItem>(list);
            _launcherWindowViewModel?.ReSetWindowHeight();
        }


        public double ReCalcGridMainHeight()
        {
            var tmp = LauncherWindowViewModel.LAUNCHER_SERVER_LIST_ITEM_HEIGHT * ConnectHistory.Count;
            var ret = LauncherWindowViewModel.LAUNCHER_GRID_KEYWORD_HEIGHT + tmp;
            return ret;
        }


        private string _lastKeyword = string.Empty;
        public void CalcVisibleByFilter()
        {
            if (string.IsNullOrEmpty(_filter) == false && _lastKeyword == _filter) return;
            _lastKeyword = _filter;

            var keyword = _filter.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                RebuildConnectionHistory();
                return;
            }


            var newList = IoC.Get<LocalityService>().QuickConnectionHistory.Where(x => x.Host.StartsWith(keyword, StringComparison.OrdinalIgnoreCase));
            var list = newList?.ToList() ?? new List<QuickConnectionItem>();
            list.Insert(0, OpenConnectActionItem);
            ConnectHistory = new ObservableCollection<QuickConnectionItem>(list);
            _launcherWindowViewModel?.ReSetWindowHeight();
        }


        public void OpenConnection()
        {
            if (ConnectHistory.Count > 0
                && SelectedIndex >= 0
                && SelectedIndex < ConnectHistory.Count)
            {
                var host = Filter;
                var protocol = SelectedProtocol.Protocol;
                var item = ConnectHistory[SelectedIndex];
                if (item == OpenConnectActionItem
                    && string.IsNullOrWhiteSpace(host))
                    return;


                // if open current input
                if (item == OpenConnectActionItem)
                {
                    //host = Filter;
                    //protocol = SelectedProtocol.Protocol;
                }
                // if open from history
                else
                {
                    host = item.Host;
                    protocol = item.Protocol;
                }

                // Hide Ui
                Filter = "";
                _launcherWindowViewModel?.HideMe();

                // create protocol
                var server = (Protocols.FirstOrDefault(x => x.Protocol == protocol) ?? SelectedProtocol).Clone();
                server.DisplayName = host;
                if (server is ProtocolBaseWithAddressPort protocolBaseWithAddressPort)
                {
                    protocolBaseWithAddressPort.Address = host;
                    var i = host.LastIndexOf(":", StringComparison.Ordinal);
                    if (i > 0)
                    {
                        var portStr = host.Substring(i + 1);
                        if (int.TryParse(portStr, out var port))
                        {
                            protocolBaseWithAddressPort.Port = port.ToString();
                            protocolBaseWithAddressPort.Address = host.Substring(0, i);
                        }
                    }
                }

                // pop password window if needed
                if (server is ProtocolBaseWithAddressPortUserPwd protocolBaseWithAddressPortUserPwd)
                {
                    var pwdDlg = IoC.Get<PasswordPopupDialogViewModel>();
                    pwdDlg.Title = $"[{server.ProtocolDisplayName}]{host}";
                    if (string.IsNullOrEmpty(pwdDlg.UserName))
                    {
                        pwdDlg.UserName = protocolBaseWithAddressPortUserPwd.UserName;
                    }
                    if (IoC.Get<IWindowManager>().ShowDialog(pwdDlg) == true)
                    {
                        protocolBaseWithAddressPortUserPwd.UserName = pwdDlg.UserName;
                        protocolBaseWithAddressPortUserPwd.Password = pwdDlg.Password;
                    }
                    else
                    {
                        return;
                    }
                }

                // save history
                IoC.Get<LocalityService>().QuickConnectionHistoryAdd(new QuickConnectionItem() { Host = host, Protocol = protocol });
                GlobalEventHelper.OnRequestQuickConnect?.Invoke(server);
            }
        }
    }
}
