using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class ExtremeSampireAttackHonor
    {
        private readonly Journal _journal = new Journal();
        
        public void Run()
        {
            try
            {
                var filter = new Mobiles.Filter();
                filter.Notorieties.Add(6);
                filter.CheckLineOfSight = true;
                filter.RangeMax = 18;

                Mobile closestMob = null;
                var mobiles = Mobiles.ApplyFilter(filter);
                foreach (var mobile in mobiles)
                {
                    _journal.Clear();
                    if (mobile.Hits == mobile.HitsMax)
                    {
                        Player.InvokeVirtue("Honor");
                        Target.WaitForTarget(300, true);
                        Target.TargetExecute(mobile);
                        Misc.Pause(50);
                        if (!_journal.Search("You don't need to declare again. You are already under Honorable Combat with this target."))
                        {
                            Mobiles.Message(mobile, 0x9, "Honored!");
                        }
                    }

                    if (closestMob == null || Player.DistanceTo(mobile) < Player.DistanceTo(closestMob))
                    {
                        closestMob = mobile;
                    }
                }

                if (closestMob == null) return;
                Player.Attack(closestMob);
                Mobiles.Message(closestMob, 0x3C, $"Attacking a '{closestMob.Name}'!");
            }
            catch (Exception e)
            {
                Player.HeadMessage(0x90, $"Error: {e.Message}");
                Console.WriteLine(e);
                throw;
            }
        }
    }
}