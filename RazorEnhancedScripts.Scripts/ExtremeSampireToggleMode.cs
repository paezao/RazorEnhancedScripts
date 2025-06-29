using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class ExtremeSampireToggleMode
    {
        public void Run()
        {
            var val = Misc.ReadSharedValue("ExtremeSampire:Mode");
            var stance = (val is string) ? !string.IsNullOrEmpty(val.ToString()) ? val.ToString() : "Single" : "Single";
            if (string.IsNullOrEmpty(stance))
            {
                stance = "Single";
            }

            Misc.SetSharedValue("ExtremeSampire:Mode", stance == "Single" ? "Multi" : "Single");
        }
    }
}