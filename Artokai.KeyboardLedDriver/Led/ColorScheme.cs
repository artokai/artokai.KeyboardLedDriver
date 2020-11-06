using System;
using System.Collections.Generic;
using System.Text;

namespace Artokai.KeyboardLedDriver.Led
{
    public class ColorScheme
    {
        public static readonly ColorScheme Default = new ColorScheme() { R = 0, G = 167, B = 224 };
        public static readonly ColorScheme Vpn = new ColorScheme() { R = 150, G = 70, B = 0 };
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }

    }

}
