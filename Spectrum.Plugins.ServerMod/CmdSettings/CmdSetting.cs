﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spectrum.Plugins.ServerMod.CmdSettings
{
    abstract class CmdSetting
    {
        public abstract string FileId { get; }  // for settings.json
        public virtual string SettingsId { get { return FileId; } }  // for !settings
        
        public abstract string DisplayName { get; }  // for automated help messages
        public abstract string HelpShort { get; }
        public abstract string HelpLong { get; }
        public virtual string HelpMarkdown { get { return HelpLong; } }
        public virtual string UsageParameters { get; } = "<option>";
        public virtual string UpdatedOnVersion { get; } = "C.7.3.1";

        public object value;
        public object Value
        {
            get { return this.value == null ? Default : this.value;  }
            set { this.value = value; }
        }
        public abstract object Default { get; }

        public abstract UpdateResult UpdateFromString(string input);
        public abstract UpdateResult UpdateFromObject(object input);
    }
}
