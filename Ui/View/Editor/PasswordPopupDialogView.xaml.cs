﻿using System.Collections.Generic;
using System.Windows;
using _1RM.Model;
using _1RM.Model.Protocol;
using _1RM.Model.Protocol.Base;
using Shawn.Utils.Wpf;
using Shawn.Utils.WpfResources.Theme.Styles;

namespace _1RM.View.Editor
{
    /// <summary>
    /// PasswordPopupDialogView.xaml 的交互逻辑
    /// </summary>
    public partial class PasswordPopupDialogView : WindowChromeBase
    {
        public List<ProtocolBaseViewModel> ProtocolList { get; }
        public ProtocolBaseWithAddressPortUserPwd Result { get; } = new FTP();
        public PasswordPopupDialogView(List<ProtocolBaseViewModel> protocolList)
        {
            ProtocolList = protocolList;

            InitializeComponent();

            BtnClose.Click += (sender, args) =>
            {
                this.DialogResult = false;
                this.Close();
            };

            DataContext = this;
        }


        /// <summary>
        /// validate whether all fields are correct to save
        /// </summary>
        /// <returns></returns>
        public virtual bool CanSave()
        {
            if (!string.IsNullOrEmpty(Result.UserName))
                return true;
            return false;
        }


        private RelayCommand? _cmdSave;
        public RelayCommand CmdSave
        {
            get
            {
                if (_cmdSave != null) return _cmdSave;
                _cmdSave = new RelayCommand((o) =>
                {
                    this.DialogResult = true;
                    this.Close();
                }, o => CanSave());
                return _cmdSave;
            }
        }
    }
}