using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class ExtremeBlaster
    {
        private const uint GumpID = 126542315;
        
        private enum Button
        {
            ArcaneEmpowermentOn = 1,
            ArcaneEmpowermentOff,
        };
        
        private const int IconArcaneEmpowerment = 0x59E7;
        private const string SpellNameArcaneEmpowerment = "Arcane Empowerment";
        private const string BuffNameArcaneEmpowerment = "Arcane Enpowerment";
        private const int SpellManaArcaneEmpowerment = 50;
        
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
                        case (int)Button.ArcaneEmpowermentOn: Misc.SetSharedValue("ExtremeBlaster:ArcaneEmpowerment", "on"); break;
                        case (int)Button.ArcaneEmpowermentOff: Misc.SetSharedValue("ExtremeBlaster:ArcaneEmpowerment", "off"); break;
                    }

                    if (gd.buttonid > 0)
                    {
                        UpdateGump();
                    }

                    if (Player.WarMode)
                    {
                        MaintainArcaneEmpowerment();
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

        private void MaintainArcaneEmpowerment()
        {
            if (!IsArcaneEmpowermentEnabled()) return;
            if (Player.Buffs.Contains(BuffNameArcaneEmpowerment)) return;
            if (Player.Hits < Player.HitsMax) return;
            if (Player.Mana < SpellManaArcaneEmpowerment) return;
            
            Spells.CastSpellweaving(SpellNameArcaneEmpowerment);
            Misc.Pause(2000);
        }

        private bool IsArcaneEmpowermentEnabled()
        {
            var val = Misc.ReadSharedValue("ExtremeBlaster:ArcaneEmpowerment");
            switch (val)
            {
                case "on": return true;
                case "off": return false;
                default: return true;
            }
        }

        private void UpdateGump()
        {
            var gump = Gumps.CreateGump();
            gump.gumpId = GumpID;
            gump.serial = (uint)Player.Serial;
            
            var gumpWidth = 55;
            Gumps.AddBackground(ref gump,0,0,gumpWidth,55,1755);

            var arcaneEmpowermentButtonIdButtonId = IsArcaneEmpowermentEnabled() ? Button.ArcaneEmpowermentOff : Button.ArcaneEmpowermentOn;
            Gumps.AddButton(ref gump, 5, 5, IconArcaneEmpowerment, IconArcaneEmpowerment, (int)arcaneEmpowermentButtonIdButtonId, 1, 0);
            Gumps.AddTooltip(ref gump, "Arcane Empowerment");
            if (!IsArcaneEmpowermentEnabled())
            {
                Gumps.AddImage(ref gump, 165, 30, 1150);
            }

            Gumps.CloseGump(GumpID);
            Gumps.SendGump(gump,500,500);
        }
    }
}