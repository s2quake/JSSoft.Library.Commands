﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ntreev.Library.Commands
{
    public interface ICommand
    {
        string Name { get; }

        bool IsEnabled { get; }
    }
}
