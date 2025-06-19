using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class ExtremeCraftingTrainer
    {
        private const int GumpId = 111333444;
        
        public void Run()
        {
        }

        public void UpdateGump()
        {
            var gump = Gumps.CreateGump();
            gump.x = 300;
            gump.y = 300;
            Gumps.AddPage(ref gump, 0);
        }
    }
}