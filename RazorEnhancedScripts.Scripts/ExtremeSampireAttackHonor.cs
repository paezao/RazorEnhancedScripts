using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class ExtremeSampireAttackHonor
    {
        public void Run()
        {
            var filter = new Mobiles.Filter();
            filter.Notorieties.Add(6);
            filter.RangeMax = 18;
            
            var mobiles = Mobiles.ApplyFilter(filter);
            foreach (var mobile in mobiles.Where(mobile => mobile.Hits == mobile.HitsMax))
            {
                Player.InvokeVirtue("Honor");
                Target.WaitForTarget(200, true);
                Target.TargetExecute(mobile);
                Mobiles.Message(mobile, 0x9, "Honored!");
                Misc.Pause(80);
            }
        }
    }
}