﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Nutadore
{
    class Note : Sign
    {
        public void Paint()
        {
        }

        override public double Paint(Canvas canvas, double left, double top, double maginification)
        {
            return left;
        }
    }
}
