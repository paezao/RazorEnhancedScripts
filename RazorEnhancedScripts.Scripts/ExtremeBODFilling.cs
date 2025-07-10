using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;
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
            try
            {
                Player.HeadMessage(MessageColorInfo, "Target your salvage bag!");
                var salvageBagSerial = _target.PromptTarget("Target your salvage bag!");
                _salvageBag = Items.FindBySerial(salvageBagSerial);
                if (_salvageBag == null || _salvageBag.ItemID != SalvageBagId || _salvageBag.Name != "Salvage Bag")
                {
                    Player.HeadMessage(MessageColorError, "Hey! This is not a Salvage Bag!");
                    return;
                }

                Items.WaitForContents(_salvageBag, 1000);
                
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
                
                var bod = ParseBod(item);

                switch (bod.Skill)
                {
                    case CraftingSkill.Tailoring:
                        FillTailoringBod(bod);
                        break;
                    default:
                        Player.HeadMessage(MessageColorError, "We only support Tailoring bods for now!");
                        return;
                }
            }
            catch (Exception e)
            {
                Player.HeadMessage(MessageColorError, $"Error: {e.Message} Trace: {e.StackTrace}");
                throw;
            }
        }

        private Bod ParseBod(Item bodItem)
        {
            var bod = new Bod();
            
            bod.Serial = bodItem.Serial;
            
            switch (bodItem.Color)
            {
                case TailoringBodColor: bod.Skill = CraftingSkill.Tailoring; break;
            }

            foreach (var property in bodItem.Properties)
            {
                var prop = property.ToString();
                
                if (prop.StartsWith("All Items Must Be Made With", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(prop, @"All Items Must Be Made With (?<material>[\w\s]+)", RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        bod.Material = ParseMaterial(match.Groups[1].Value);
                    }
                }
                else if (prop.StartsWith("All Items Must Be Exceptional", StringComparison.OrdinalIgnoreCase))
                {
                    bod.Exceptional = true;
                }
                else if (prop.StartsWith("Amount To Make:", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(prop, @"\d+");
                    if (match.Success)
                    {
                        bod.Amount = int.Parse(match.Value);
                    }
                }
                else
                {
                    // Try to match "Item Name: FilledAmount"
                    var match = Regex.Match(prop, @"^(?<name>.+?):\s*(?<count>\d+)$");
                    if (match.Success)
                    {
                        bod.Item = match.Groups["name"].Value.Trim();
                        bod.FilledAmount = int.Parse(match.Groups["count"].Value);
                    }
                }
            }

            var materialPrefix = GetMaterialItemPrefix(bod.Material);
            bod.ItemFullName = materialPrefix.Length > 0 ? $"{materialPrefix} {bod.Item}" : bod.Item;

            return bod;
        }

        private Material ParseMaterial(string materialString)
        {
            var normalized = materialString.ToLowerInvariant().Replace(" ", "");

            switch (normalized)
            {
                case "leather": return Material.RegularLeather;
                case "spinedleather": return Material.SpinedLeather;
                case "hornedleather": return Material.HornedLeather;
                case "barbedleather": return Material.BarbedLeather;
            }
            
            throw new Exception("Unknown material: " + materialString);
        }

        private void FillTailoringBod(Bod bod)
        {
            var exceptionalText = bod.Exceptional ? "exceptional" : "normal";
            
            Player.HeadMessage(MessageColorInfo, $"Bod: {bod.Amount}x ({bod.FilledAmount}) {exceptionalText} {bod.Item}");
            
            Items.UseItemByID(SewingKitId);
            Misc.Pause(500);
            var craftingGump = new CraftingGump(Gumps.CurrentGump());
            craftingGump.ScanGump();

            craftingGump.SelectMaterial(bod.Material);

            var craftedOnce = false;
            var amountMade = bod.FilledAmount;
            while (amountMade < bod.Amount)
            {
                Items.UseItemByID(SewingKitId);
                Misc.Pause(200);
                if (!craftedOnce)
                {
                    craftingGump.MakeItemByName(bod.Item);
                    craftedOnce = true;
                }
                else
                {
                    craftingGump.MakeLast();
                }
                
                Misc.Pause(2000);
                var createdItem = Items.FindByName(bod.ItemFullName, -1, Player.Backpack.Serial, -1);
                if (createdItem == null)
                {
                    craftingGump.WaitForGump();
                    if (craftingGump.GetNotice().Contains("You do not have"))
                    {
                        Player.HeadMessage(MessageColorError, "Hey, you're out of mats!!");
                        break;
                    }

                    continue;
                }

                var exceptionalProp = Items.GetPropStringByIndex(createdItem.Serial, 2);
                if ((bod.Exceptional && exceptionalProp == "exceptional") || !bod.Exceptional)
                {
                    amountMade++;
                    var amountToGo = bod.Amount - amountMade;
                    Player.HeadMessage(MessageColorSuccess,
                        amountToGo > 0 ? $"{amountMade} made, {amountToGo} to go!" : $"{amountMade} made, DONE!");
                }
                else
                {
                    Player.HeadMessage(MessageColorError, $"Oops! Not exceptional, still {bod.Amount - amountMade} to go!");
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
        
        private enum CraftingSkill
        {
            Tailoring,
            Blacksmithing,
        };

        [AttributeUsage(AttributeTargets.Field)]
        public class ItemPrefixAttribute : Attribute
        {
            public string Prefix { get; }

            public ItemPrefixAttribute(string prefix)
            {
                Prefix = prefix.ToLowerInvariant();
            }
        }
        
        private enum Material
        {
            // Tailoring leathers
            RegularLeather,
            
            [ItemPrefix("spined")]
            SpinedLeather,
            
            [ItemPrefix("horned")]
            HornedLeather,
            
            [ItemPrefix("barbed")]
            BarbedLeather,

            // Blacksmithing metals
            IronIngot,
            DullCopperIngot,
            ShadowIronIngot,
            CopperIngot,
            BronzeIngot,
            GoldIngot,
            AgapiteIngot,
            VeriteIngot,
            ValoriteIngot
        }

        private static string GetMaterialItemPrefix(Material material)
        {
            var type = typeof(Material);
            var memInfo = type.GetMember(material.ToString());

            if (memInfo.Length <= 0) return "";
            
            var attrs = memInfo[0].GetCustomAttributes(typeof(ItemPrefixAttribute), false);
            return attrs.Length > 0 ? ((ItemPrefixAttribute)attrs[0]).Prefix : "";
        }

        private struct Bod
        {
            public CraftingSkill Skill;
            public int Amount;
            public int FilledAmount;
            public bool Exceptional;
            public Material Material;
            public string Item;
            public string ItemFullName;
            public int Serial;

            public Bod(CraftingSkill skill, int amount, int filledAmount, bool exceptional, Material material, string item, string itemFullName, int serial)
            {
                Skill = skill;
                Amount = amount;
                FilledAmount = filledAmount;
                Exceptional = exceptional;
                Material = material;
                Item = item;
                ItemFullName = itemFullName;
                Serial = serial;
            }
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
            private const int SelectMaterialButtonId = 7;
            private readonly uint _gumpId;
            
            private readonly Dictionary<string, MakeItemPath> _itemPaths = new Dictionary<string, MakeItemPath>();
            private readonly Dictionary<Material, int> _materialPaths = new Dictionary<Material, int>();

            public CraftingGump(uint gumpId)
            {
                _gumpId = gumpId;
            }

            public bool SelectMaterial(Material material)
            {
                var materialPath = _materialPaths[material];
                if (Gumps.HasGump(_gumpId))
                {
                    Gumps.SendAction(_gumpId, SelectMaterialButtonId);
                    Misc.Pause(1000);
                    Gumps.SendAction(_gumpId, materialPath);
                }
                return true;
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

            public void WaitForGump()
            {
                for (var i = 0; i < 3; i++)
                {
                    if (Gumps.HasGump(_gumpId)) return;
                    Misc.Pause(200);
                }
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
                foreach (var gumpString in gumpStrings)
                {
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
                
                // Read Materials
                Gumps.SendAction(_gumpId, SelectMaterialButtonId);
                Misc.Pause(300);
                gumpStrings = Gumps.GetLineList(_gumpId);
                var materialsStarted = false;
                var materialButtonId = 6;
                foreach (var gumpString in gumpStrings)
                {
                    if (gumpString == "DO NOT COLOR")
                    {
                        materialsStarted = true;
                        continue;
                    }

                    if (!materialsStarted) continue;
                    
                    var match = Regex.Match(gumpString, @"^(.*?)\s*\(~", RegexOptions.IgnoreCase);
                    if (!match.Success) continue;
                    
                    var rawMaterial = match.Groups[1].Value.Trim();
                    var mat = MapToMaterial(rawMaterial);
                    _materialPaths[mat] = materialButtonId;
                    materialButtonId += 20;
                }
            }

            private Material MapToMaterial(string rawMaterial)
            {
                var normalized = rawMaterial.ToLower().Replace(" ", "");
                switch (normalized)
                {
                    case "leather/hides":
                    case "leatherhides": return Material.RegularLeather;
                    case "spinedhides": return Material.SpinedLeather;
                    case "hornedhides": return Material.HornedLeather;
                    case "barbedhides": return Material.BarbedLeather;
                }
                
                throw new Exception("Unknown material from BOD gump: " + rawMaterial);
            }
        }
    }
}