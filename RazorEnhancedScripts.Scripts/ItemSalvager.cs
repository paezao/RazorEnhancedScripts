using System;
using System.Collections.Generic;
using System.Linq;
using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class ItemSalvager
    {
        private const int MessageColorInfo = 0x90;
        private const int MessageColorSuccess = 0x3C;
        private const int MessageColorError = 0x21;
        
        private const int SalvageBagId = 0x0E76;
        
        private readonly Target _target = new Target();
        private Item _salvageBag = null;

        private const string ItemName = "Studded Sleeves";

        private static readonly List<ItemRule> Rules = new List<ItemRule>
        {
            new ItemRule
            {
                Type = RuleType.Exceptional
            },
            new ItemRule
            {
                Type = RuleType.PropertyMatch,
                PropertyName = "physical resist",
                AllowedValues = new[] { "2%", "3%" }
            },
        };
        
        public void Run()
        {
            try
            {
                var storedSalvageBagSerial = Misc.ReadSharedValue("ItemSalvager:SalvageBagSerial");
                _salvageBag = Items.FindBySerial((int)storedSalvageBagSerial);
                if (_salvageBag == null)
                {
                    Player.HeadMessage(MessageColorInfo, "Target your salvage bag!");
                    var salvageBagSerial = _target.PromptTarget("Target your salvage bag!");
                    _salvageBag = Items.FindBySerial(salvageBagSerial);
                    if (_salvageBag == null || _salvageBag.ItemID != SalvageBagId || _salvageBag.Name != "Salvage Bag")
                    {
                        Player.HeadMessage(MessageColorError, "Hey! This is not a Salvage Bag!");
                        return;
                    }
                    Misc.SetSharedValue("ItemSalvager:SalvageBagSerial", salvageBagSerial);
                }
                
                Items.WaitForContents(_salvageBag, 1000);
                Misc.Pause(2000);
            
                foreach (var item in Player.Backpack.Contains.Where(item => string.Equals(item.Name, ItemName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    if (IsItemValid(item)) continue;
                    
                    Items.Move(item, _salvageBag, 1);
                    Misc.Pause(700);
                }
            }
            catch (Exception ex)
            {
                Player.HeadMessage(MessageColorError, $"Error: {ex.Message} Stack: {ex.StackTrace}");
            }
        }
        
        private static bool IsItemValid(Item item)
        {
            return Rules.All(rule => ItemMatchesRule(item, rule));
        }

        
        private static bool ItemMatchesRule(Item item, ItemRule rule)
        {
            switch (rule.Type)
            {
                case RuleType.Exceptional:
                    return item.Name.ToLowerInvariant().Contains("exceptional") 
                           || item.Properties.Any(p => p.ToString().ToLowerInvariant().Contains("exceptional"));

                case RuleType.PropertyMatch:
                    foreach (var prop in item.Properties)
                    {
                        var line = prop.ToString().ToLowerInvariant();
                        if (!line.StartsWith(rule.PropertyName.ToLowerInvariant())) continue;
                        if (rule.AllowedValues.Any(val => line.EndsWith(val.ToLowerInvariant())))
                        {
                            return true;
                        }
                    }
                    return false;

                default:
                    return false;
            }
        }

        private enum RuleType
        {
            PropertyMatch,
            Exceptional,
        }

        private class ItemRule
        {
            public RuleType Type;
            public string PropertyName;
            public string[] AllowedValues;
        }
    }
}