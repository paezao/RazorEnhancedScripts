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
        private Journal _journal = new Journal();

        private const int RunebookId = 0x22C5;
        private const int BodBookId = 0x2259;
        private const int BodId = 0x2258;
        private const int TailoringBodColor = 0x0483;
        private const int BlacksmithingBodColor = 0x044e;
        private const int RunebookGumpId = 0x59;
        private const int MessageColorInfo = 0x90;
        private const int MessageColorSuccess = 0x3C;
        private const int MessageColorError = 0x21;

        private enum BodType
        {
            Tailoring = 0,
            Blacksmithing,
        };
        
        public void Run()
        {
            if (!FindRunebook()) return;
            if (!FindBodBooks()) return;

            if (Player.GetSkillValue("Tailoring") >= 100)
            {
                if (!RecallToTailor()) return;
                CollectBods(BodType.Tailoring);
            }
            
            if (Player.GetSkillValue("Blacksmithing") >= 100)
            {
                if (!RecallToBlacksmith()) return;
                CollectBods(BodType.Blacksmithing);
            }
            
            StoreBods();

            Misc.Pause(2000);
            RecallHome();
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

        private void RecallHome()
        {
            Player.HeadMessage(MessageColorInfo, "Recalling home...");
            RecallToRune(_runebook, 1);
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

        private void CollectBods(BodType bodType)
        {
            var npcSuffix = "";
            
            switch (bodType)
            {
                case BodType.Tailoring:
                    npcSuffix = "tailor";
                    break;
                case BodType.Blacksmithing:
                    npcSuffix = "blacksmith";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(bodType), bodType, null);
            }

            var mobileFilter = new Mobiles.Filter();
            mobileFilter.Enabled = true;
            mobileFilter.RangeMax = 8;
            mobileFilter.CheckLineOfSight = true;
            var mobiles = Mobiles.ApplyFilter(mobileFilter);

            foreach (var mobile in mobiles)
            {
                _journal.Clear();
                
                if (!Mobiles.GetPropStringByIndex(mobile, 0).EndsWith(npcSuffix)) continue;
                for (var i = 0; i < 3; i++)
                {
                    Misc.UseContextMenu(mobile.Serial, "Bulk Order Info", 1000);
                    Misc.Pause(500);

                    if (_journal.SearchByType("An offer may be available in about", "Regular"))
                    {
                        Player.HeadMessage(MessageColorInfo, "No Bods available at this time.");
                        return;
                    }

                    var gump = Gumps.CurrentGump();
                    Gumps.SendAction(Gumps.CurrentGump(), 1);
                    
                    Misc.Pause(500);
                }
            }
            
            Player.HeadMessage(MessageColorSuccess, "Bods collected!");
        }

        private void StoreBods()
        {
            var bods = Player.Backpack.Contains.Where(i => i.ItemID == BodId).ToList();
            foreach (var bod in bods)
            {
                Item destinationBodBook = null;
                
                if (bod.Color == TailoringBodColor)
                {
                    destinationBodBook = _tailoringBodBook;
                }
                else if (bod.Color == BlacksmithingBodColor)
                {
                    destinationBodBook = _blacksmithBodBook;
                }

                if (destinationBodBook == null) continue;
                
                Items.Move(bod, destinationBodBook, -1);
                Misc.Pause(300);
            }
        }
    }
}