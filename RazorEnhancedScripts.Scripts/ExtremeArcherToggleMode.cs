using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class ExtremeArcherToggleMode
    {
        public void Run()
        {
            var val = Misc.ReadSharedValue("ExtremeArcher:Mode");
            var stance = (val is string) ? !string.IsNullOrEmpty(val.ToString()) ? val.ToString() : "Stationary" : "Stationary";
            if (string.IsNullOrEmpty(stance))
            {
                stance = "Stationary";
            }

            Misc.SetSharedValue("ExtremeArcher:Mode", stance == "Stationary" ? "Moving" : "Stationary");
        }
    }
}