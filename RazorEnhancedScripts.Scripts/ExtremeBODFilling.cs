using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class ExtremeBODFilling
    {
        private const int BodId = 0x2258;
        //private const uint BodGumpId = 0x7a6efab2;
        private const uint BodGumpId = 0x2267ab21;
        private const int BodGumpCombineContainerButtonId = 4;
        
        private const int TailoringBodColor = 0x0483;
        private const int BlacksmithingBodColor = 0x044e;

        private const int SewingKitId = 0x0F9D;
        private const int SalvageBagId = 0x0E76;
        
        private const int MessageColorInfo = 0x90;
        private const int MessageColorSuccess = 0x3C;
        private const int MessageColorError = 0x21;

        private readonly Target _target = new Target();
        private readonly Journal _journal = new Journal();

        private Item _salvageBag = null;

        public void Run()
        {
            Player.HeadMessage(MessageColorInfo, "Target your salvage bag!");
            var salvageBagSerial = _target.PromptTarget("Target your salvage bag!");
            _salvageBag = Items.FindBySerial(salvageBagSerial);
            if (_salvageBag == null || _salvageBag.ItemID != SalvageBagId || _salvageBag.Name != "Salvage Bag")
            {
                Player.HeadMessage(MessageColorError, "Hey! This is not a Salvage Bag!");
                return;
            }
            
            Player.HeadMessage(MessageColorInfo, "Target the BOD you want to fill.");
            var itemSerial = _target.PromptTarget("Target the BOD you want to fill.");
            var item = Items.FindBySerial(itemSerial);
            
            if (item.ItemID != BodId)
            {
                Player.HeadMessage(MessageColorError, "Hey! This is not a BOD!");
                return;
            }
            
            if (Items.GetPropStringList(item)[3].StartsWith("large"))
            {
                Player.HeadMessage(MessageColorError, "This only works with Small Bods!");
                return;
            }

            switch (item.Color)
            {
                case TailoringBodColor:
                    FillTailoringBod(item);
                    break;
                default:
                    Player.HeadMessage(MessageColorError, "We only support Tailoring and Blacksmithing bods for now!");
                    return;
            }
        }

        private void FillTailoringBod(Item bod)
        {
            var mustBeExceptional = bod.Properties.Count > 6;
            var amount = int.Parse(bod.Properties[bod.Properties.Count-2].ToString().Split(' ').Last());
            var currentAmount = int.Parse(bod.Properties.Last().ToString().Split(':').Last().Trim());
            var itemName = bod.Properties.Last().ToString().Split(':').First();

            var exceptionalText = mustBeExceptional ? "exceptional" : "normal";
            
            Player.HeadMessage(MessageColorInfo, $"Bod: {amount}x ({currentAmount}) {exceptionalText} {itemName}");
            
            Items.UseItemByID(SewingKitId);
            Misc.Pause(500);
            var craftingGump = new CraftingGump(Gumps.CurrentGump());
            craftingGump.ScanGump();

            var craftedOnce = false;
            var amountMade = currentAmount;
            while (amountMade < amount)
            {
                Items.UseItemByID(SewingKitId);
                Misc.Pause(200);
                if (!craftedOnce)
                {
                    craftingGump.MakeItemByName(itemName);
                    craftedOnce = true;
                }
                else
                {
                    craftingGump.MakeLast();
                }
                
                Misc.Pause(2000);
                var createdItem = Items.FindByName(itemName, -1, Player.Backpack.Serial, -1);
                if (createdItem == null)
                {
                    if (craftingGump.GetNotice().Contains("You do not have sufficient"))
                    {
                        Player.HeadMessage(MessageColorError, "Hey, you're out of mats!!");
                        break;
                    }

                    continue;
                }

                var exceptionalProp = Items.GetPropStringByIndex(createdItem.Serial, 2);
                if ((mustBeExceptional && exceptionalProp == "exceptional") || !mustBeExceptional)
                {
                    amountMade++;
                    var amountToGo = amount - amountMade;
                    if (amountToGo > 0)
                    {
                        Player.HeadMessage(MessageColorSuccess, $"{amountMade} made, {amountToGo} to go!");
                    }
                    else
                    {
                        Player.HeadMessage(MessageColorSuccess, $"{amountMade} made, DONE!");
                    }
                }
                else
                {
                    Player.HeadMessage(MessageColorError, $"Oops! Not exceptional, still {amount - amountMade} to go!");
                    Items.Move(createdItem, _salvageBag, 1);
                    Misc.Pause(200);
                }
                
                Misc.IgnoreObject(createdItem.Serial);
            }
            
            Items.UseItem(bod.Serial);
            Misc.Pause(200);
            Gumps.SendAction(BodGumpId, BodGumpCombineContainerButtonId);
            Misc.Pause(200);
            Target.TargetExecute(Player.Backpack.Serial);
        }

        private class CraftingGump
        {
            private struct MakeItemPath
            {
                public readonly int CategoryButtonId;
                public readonly int CraftButtonId;

                public MakeItemPath(int categoryButtonId, int craftButtonId)
                {
                    CategoryButtonId = categoryButtonId;
                    CraftButtonId = craftButtonId;
                }
            }

            private const int MakeLastButtonId = 47;
            private readonly uint _gumpId;
            
            private readonly Dictionary<string, MakeItemPath> _itemPaths = new Dictionary<string, MakeItemPath>();

            public CraftingGump(uint gumpId)
            {
                _gumpId = gumpId;
            }
            
            public void MakeLast()
            {
                Gumps.SendAction(_gumpId, MakeLastButtonId);
            }

            public bool MakeItemByName(string itemName)
            {
                var itemPath = _itemPaths[itemName];
                if (Gumps.HasGump(_gumpId))
                {
                    Gumps.SendAction(_gumpId, itemPath.CategoryButtonId);
                    Misc.Pause(300);
                    Gumps.SendAction(_gumpId, itemPath.CraftButtonId);
                    Misc.Pause(3000);
                }
                return true;
            }

            public string GetNotice()
            {
                return Gumps.GetLineList(_gumpId)[13];
            }

            public void ScanGump()
            {
                var gumpStrings = Gumps.GetLineList(_gumpId);

                var categories = new Dictionary<string, int>();
                
                var scanningCategories = false;
                var categoryButtonId = 1;
                
                // Read all Categories
                //Player.HeadMessage(MessageColorInfo, "Scanning Categories");
                for (var i = 0; i < gumpStrings.Count; i++)
                {
                    var gumpString = gumpStrings[i];
                    //Player.HeadMessage(MessageColorInfo, $"String: {gumpString}");
                    
                    if (gumpString == "LAST TEN")
                    {
                        scanningCategories = true;
                        continue;
                    }

                    if (!scanningCategories) continue;
                    
                    if (char.IsLower(gumpString.First()))
                    {
                        break;
                    }

                    categories.Add(gumpString, categoryButtonId);
                    categoryButtonId += 20;
                }

                foreach (var category in categories)
                {
                    //Player.HeadMessage(MessageColorInfo, $"Scanning Category: {category.Key}");
                    Gumps.SendAction(_gumpId, category.Value);
                    Misc.Pause(300);
                    
                    gumpStrings = Gumps.GetLineList(_gumpId);
                    var itemButtonId = 2;
                    foreach (var gumpString in gumpStrings)
                    {
                        var gumpStringFirstChar = gumpString.First();
                        if (!char.IsLetter(gumpStringFirstChar) || char.IsUpper(gumpStringFirstChar)) continue;
                        
                        _itemPaths[gumpString] = new MakeItemPath(category.Value, itemButtonId);
                        itemButtonId += 20;
                    }
                }
            }
        }
    }
}