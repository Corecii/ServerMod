﻿using Spectrum.Plugins.ServerMod.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spectrum.Plugins.ServerMod.Cmds
{
    class DateCmd : Cmd
    {
        public override string name { get { return "date"; } }
        public override PermType perm { get { return PermType.ALL; } }
        public override bool canUseLocal { get { return true; } }

        public override bool showChatPublic(ClientPlayerInfo p)
        {
            return true;
        }

        public override void help(ClientPlayerInfo p)
        {
            MessageUtilities.sendMessage(p, GeneralUtilities.formatCmd("!date") + ": Write the time and date.");
        }

        public override void use(ClientPlayerInfo p, string message)
        {
            MessageUtilities.sendMessage("Current date: [FFFFFF]" + DateTime.Now.ToString() + "[-]");
        }
    }
}
