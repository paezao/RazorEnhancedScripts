using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class ExtremeBard
    {
        private const uint GumpID = 126542315;

        private enum Button
        {
        };
        
        private const string SkillNamePeacemaking = "Peacemaking";
        private const string SkillNameDiscordance = "Discordance";
        private const string SkillNameProvocation = "Provocation";

        private const int IconEnemyOfOne = 0x5105;
        private const int IconConsecrateWeapon = 0x5102;

        private Journal _journal = new Journal();
        
        public void Run()
        {
            try
            {
                UpdateGump();
                while (true)
                {
                    var gd = Gumps.GetGumpData(GumpID);
                    switch (gd.buttonid)
                    {
                        case 0: return;
                        //case (int)Button.MultiTarget: Misc.SetSharedValue("ExtremeSampire:Mode","Multi"); break;
                    }

                    if (gd.buttonid > 0)
                    {
                        UpdateGump();
                    }
                    
                    if (Player.WarMode)
                    {
                    }

                    Misc.Pause(500);
                }
            }
            catch (Exception e)
            {
                if (e.GetType() != typeof(ThreadAbortException))
                {
                    Misc.SendMessage(e.ToString());
                }
            }
        }

        private void UpdateGump()
        {
            var gump = Gumps.CreateGump();
            gump.gumpId = GumpID;
            gump.serial = (uint)Player.Serial;

            var gumpWidth = 205;
            Gumps.AddBackground(ref gump,0,0,gumpWidth,55,1755);

            Gumps.CloseGump(GumpID);
            Gumps.SendGump(gump,500,500);
        }
    }
}