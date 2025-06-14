using System;
using System.Linq;
using System.Threading;
using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class TreasureMapChestOrganizer
    {
        private Item _mapContainer;
        private readonly Journal _journal = new Journal();

        public void Run()
        {
            Player.HeadMessage(0x90, "Select your Treasure Map Container");

            var selectContainerTarget = new Target();
            var containerSerial = selectContainerTarget.PromptTarget("Select your Treasure Map Container");
            
            var containerItem = Items.FindBySerial(containerSerial);
            if (!containerItem.IsContainer)
            {
                Player.HeadMessage(0x90, "You must select a container!");
                return;
            }

            Items.WaitForContents(containerItem, 5000);

            try
            {
                foreach (var item in containerItem.Contains)
                {
                    Items.WaitForContents(item, 5000);
                    Player.HeadMessage(0x90, $"Item: {item.Name}");

                    if (Items.ContextExist(item, "Decode Map") < 0) continue;

                    var decodedMap = false;
                    do
                    {
                        _journal.Clear();
                        Misc.ContextReply(item, "Decode Map");
                        Misc.Pause(1000);

                        if (_journal.GetJournalEntry(-1D).Any(entry => entry.Name == Player.Name && entry.Text == "You successfully decode a treasure map!"))
                        {
                            decodedMap = true;
                        }
                    } while (!decodedMap);
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
    }
}
