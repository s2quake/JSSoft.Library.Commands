﻿using Ntreev.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleApplication
{
    [Export(typeof(CommandContext))]
    class GitCommandContext : CommandContext
    {
        [ImportingConstructor]
        public GitCommandContext([ImportMany]IEnumerable<ICommand> commands)
            : base(commands)
        {

        }

        protected override CommandLineParser CreateInstance(ICommand command)
        {
            return new GitCommandLineParser(command);
        }
    }
}
