using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class ExtremeBODCollecting
    {
        private Item _runebook = null;
        private Item _tailoringBodBook = null;
        private Item _blacksmithBodBook = null;

        private const int RunebookId = 0x22C5;
        private const int BodBookId = 0x2259;
        private const int RunebookGumpId = 0x59;
        private const int MessageColorInfo = 0x90;
        private const int MessageColorSuccess = 0x3C;
        private const int MessageColorError = 0x21;
        
        public void Run()
        {
            if (!FindRunebook()) return;
            if (!FindBodBooks()) return;

            if (!RecallToTailor()) return;

            CollectTailorBods();
        }

        private bool FindRunebook()
        {
            _runebook = Items.FindByID(RunebookId, -1, Player.Backpack.Serial);
            if (_runebook == null)
            {
                Player.HeadMessage(MessageColorError, "Can't find runebook!");
            }
            return _runebook != null;
        }

        private bool FindBodBooks()
        {
            _tailoringBodBook = FindBodBookByBookName("Tailoring");
            _blacksmithBodBook = FindBodBookByBookName("Blacksmith");
            
            return _tailoringBodBook != null && _blacksmithBodBook != null;
        }

        private Item FindBodBookByBookName(string name)
        {
            var bodBooks = Player.Backpack.Contains.Where(i => i.ItemID == BodBookId).ToList();
            foreach (var bodBook in bodBooks)
            {
                var bookName = Items.GetPropValueString(bodBook.Serial, "Book Name");
                if (bookName == name) return bodBook;
            }
            
            Player.HeadMessage(MessageColorError, $"Can't find a BOD Book named {name}!");
            return null;
        }

        private bool RecallHome()
        {
            Player.HeadMessage(MessageColorInfo, "Recalling home...");
            return RecallToRune(_runebook, 1);
        }

        private bool RecallToTailor()
        {
            Player.HeadMessage(MessageColorInfo, "Recalling to tailor...");
            return RecallToRune(_runebook, 2);
        }

        private bool RecallToBlacksmith()
        {
            Player.HeadMessage(MessageColorInfo, "Recalling to blacksmith...");
            return RecallToRune(_runebook, 3);
        }

        private bool RecallToRune(Item runebook, uint runeNumber)
        {
            Items.UseItem(runebook);
            Misc.Pause(200);
            Gumps.SendAction(RunebookGumpId, 49 + (int)runeNumber);
            Misc.Pause(3000);
            return true;
        }

        private bool CollectTailorBods()
        {
        }

        private bool CollectBods()
        {
            
        }
    }
}