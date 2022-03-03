﻿using Shawn.Utils;

namespace PRM.Protocol.Base
{
    public abstract class ProtocolServerWithAddrPortUserPwdBase : ProtocolServerWithAddrPortBase
    {
        protected ProtocolServerWithAddrPortUserPwdBase(string protocol, string classVersion, string protocolDisplayName, string protocolDisplayNameInShort = "") : base(protocol, classVersion, protocolDisplayName, protocolDisplayNameInShort)
        {
        }

        #region Conn

        private string _userName = "Administrator";
        [OtherName(Name = "PRM_USERNAME")]
        public string UserName
        {
            get => _userName;
            set => SetAndNotifyIfChanged(ref _userName, value);
        }

        private string _password = "";
        [OtherName(Name = "PRM_PASSWORD")]
        public string Password
        {
            get => _password;
            set => SetAndNotifyIfChanged(ref _password, value);
        }

        protected override string GetSubTitle()
        {
            return $"{Address}:{Port} ({UserName})";
        }

        #endregion
    }
}