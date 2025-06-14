using System;
using System.Threading;
using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class RuneGump
    {
        private const int GumpId = 54321543;
        private const int GumpBackground = 1755;
        private const int RecallIcon = 2271;
        private const int GateTravelIcon = 2291;

        private enum Buttons
        {
            Recall = 0,
            GateTravel,
        };
        
        public void Run()
        {
            try
            {
                UpdateGump();

                while (true)
                {

                }
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(ThreadAbortException))
                {
                    Misc.SendMessage(ex.ToString());
                }
            }
        }

        private void UpdateGump()
        {
            var gump = Gumps.CreateGump();
            gump.x = 300;
            gump.y = 300;
            Gumps.AddPage(ref gump, 0);
            Gumps.AddBackground(ref gump, 0, 0, 187, 200, GumpBackground);
            Gumps.AddLabel(ref gump, 8, 58, 0x90, "SEA");
            Gumps.AddButton(ref gump, 100, 58, RecallIcon, RecallIcon, (int)Buttons.Recall, 1, 0);
            Gumps.AddButton(ref gump, 100, 58, RecallIcon, RecallIcon, (int)Buttons.Recall, 1, 0);
            Gumps.AddLabel(ref gump, 8, 12, 0x90, "House");
            //Gumps.AddButton(ref gump, 150, 55, 2152, 2151, 500, 1);
            gump.serial = (uint)Player.Serial;
            gump.gumpId = GumpId;
            Gumps.SendGump(gump, 150, 150);
        }
    }
}