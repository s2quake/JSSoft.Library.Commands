﻿using Ntreev.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleApplication
{
    [Export(typeof(ICommand))]
    [GitSummary("HelpSummary")]
    [GitDescription("HelpDescription")]
    class HelpCommand : ICommand
    {
        [Import]
        private Lazy<CommandContext> commandContext = null;

        public bool HasSubCommand
        {
            get { return false; }
        }

        public string Name
        {
            get { return "help"; }
        }

        public void Execute()
        {
            var commandContext = this.commandContext.Value;
            var parser = commandContext.Parsers[this.Name];
            parser.PrintUsage();
        }

        [CommandSwitch(ShortName = 'a')]
        [Description("Prints all the available commands on the standard output. This option overrides any given command or guide name.")]
        public bool All
        {
            get; set;
        }

        [CommandSwitch(ShortName = 'g')]
        [Description("Prints a list of useful guides on the standard output. This option overrides any given command or guide name.")]
        public bool Guides
        {
            get; set;
        }

        [CommandSwitch(ShortName = 'i')]
        [Description("Display manual page for the command in the info format. The info program will be used for that purpose.")]
        public bool Info
        {
            get; set;
        }

        [CommandSwitch(ShortName = 'm')]
        [Description("Display manual page for the command in the man format. This option may be used to override a value set in the help.format configuration variable. \r\nBy default the man program will be used to display the manual page, but the man.viewer configuration variable may be used to choose other display programs(see below).")]
        public bool Man
        {
            get; set;
        }
    }
}
