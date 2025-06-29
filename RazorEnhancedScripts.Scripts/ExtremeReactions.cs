using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class ExtremeReactions
    {
        private const int MessageColorInfo = 0x3C;
        private const int MessageColorDanger = 0x21;
        
        private Item _unlimitedTrappedBox;
        private Item _enchantedApples;
        private Item _rightHandItem;
        private Item _leftHandItem;

        private bool _disarmed = false;
        
        public void Run()
        {
            Player.HeadMessage(MessageColorInfo, "Initializing Extreme Reactions...");
            _unlimitedTrappedBox = Items.FindByID(0x0E7E, 0x0021, Player.Backpack.Serial);
            if (_unlimitedTrappedBox == null)
            {
                Player.HeadMessage(MessageColorDanger, "Look, you should really have an unlimited trapped box in your backpack!");
            }
            
            _enchantedApples = Items.FindByID(0x2FD8, 0x0488, Player.Backpack.Serial);
            if (_enchantedApples == null)
            {
                Player.HeadMessage(MessageColorDanger, "Enchanted Apples are highly recommended and you have none!");
            }
            
            Player.HeadMessage(MessageColorInfo, "Initialization complete! Head out!");
            
            while (true)
            {
                if (Player.Buffs.Contains("Paralyze"))
                {
                    Player.HeadMessage(MessageColorDanger, "You're paralized!");
                    if (_unlimitedTrappedBox != null)
                    {
                        Items.UseItem(_unlimitedTrappedBox);
                        Player.HeadMessage(MessageColorDanger, "Trap box triggered!");
                    }
                }
                
                if (_disarmed && !Player.Buffs.Contains("NoRearm"))
                {
                    var leftHandArmed = true;
                    var rightHandArmed = true;
                    
                    if (_leftHandItem != null)
                    {
                        if (!Player.CheckLayer("LeftHand"))
                        {
                            leftHandArmed = false;
                        }
                        Player.EquipItem(_leftHandItem);
                    }

                    if (_rightHandItem != null)
                    {
                        if (!Player.CheckLayer("RightHand"))
                        {
                            rightHandArmed = false;
                        }
                        Player.EquipItem(_rightHandItem);
                    }
                    
                    _disarmed = leftHandArmed && rightHandArmed;
                }
                
                if (Player.Buffs.Contains("NoRearm") && !_disarmed)
                {
                    Player.HeadMessage(MessageColorDanger, "You have been disarmed!");
                    _disarmed = true;
                }
                
                if (Player.Buffs.Contains("Curse"))
                {
                    /*
                    Player.HeadMessage(MessageColorDanger, "You have been cursed!");
                    if (_enchantedApples != null)
                    {
                        Items.UseItem(_enchantedApples);
                        Player.HeadMessage(MessageColorDanger, "Ate an Enchanted Apple!");
                    }
                    */
                }

                if (Player.CheckLayer("LeftHand")) _leftHandItem = Player.GetItemOnLayer("LeftHand");
                if (Player.CheckLayer("RightHand")) _rightHandItem = Player.GetItemOnLayer("RightHand");
                
                Misc.Pause(200);
            }
        }
    }
}