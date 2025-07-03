using System.Linq;
using System.Runtime.CompilerServices;
using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class FishingTraining
    {
        private const int MessageColorInfo = 0x3C;
        private const int MessageColorDanger = 0x21;
        private const int OffsetTiles = 11;
        private const int FishingPoleItemId = 0x0DC0;
        private const int ButchersKnifeItemId = 0x13F6;

        private static readonly int[] FishItemIds = new[]
        {
            0x09CC,
            0x09CD,
            0x09CE,
            0x09CF,
            0x4303,
            0x4306,
            0x4307,
            0x44C4,
            0x44C6,
        };
        
        private static readonly int[] JunkItemIds = new[]
        {
            0x170B,
            0x170D,
            0x170F,
        };

        private static Item _fishingPole = null;
        
        public static void Run()
        {
            if (Player.IsGhost) return;
            if (Player.Mount != null) return;
            
            _fishingPole = Player.GetItemOnLayer("RightHand");
            if (_fishingPole?.ItemID != FishingPoleItemId)
            {
                Player.HeadMessage(MessageColorDanger, "Hey! Equip a fishing pole!");
                return;
            }

            FishNode();
            Player.HeadMessage(MessageColorDanger, "No more fish! Move!");
            CleanUp();
        }

        private static void FishNode()
        {
            var journal = new Journal();
            while (true)
            {
                journal.Clear();
                
                // Use fishing pole
                Items.UseItem(_fishingPole);
                Target.WaitForTarget(1500, true);
                ExecuteTargetFromPlayerOffset(OffsetTiles);
                
                Misc.Pause(1000);
                if (journal.SearchByType("The fish don't seem to be biting here.", "System"))
                {
                    return;
                }

                // Wait a bit to let fishing happen
                Misc.Pause(9000);
            }
        }

        private static void CleanUp()
        {
            Player.HeadMessage(MessageColorDanger, "Cleaning up...");
            foreach (var item in Player.Backpack.Contains.Where(item => FishItemIds.Contains(item.ItemID)))
            {
                Items.UseItemByID(ButchersKnifeItemId);
                Target.WaitForTarget(1000, true);
                Target.TargetExecute(item);
                Misc.Pause(500);
            }
            
            /*
            foreach (var item in Player.Backpack.Contains.Where(item => JunkItemIds.Contains(item.ItemID)))
            {
                Items.DropItemGroundSelf(item);
                Misc.Pause(500);
            }
            */
        }

        private static void ExecuteTargetFromPlayerOffset(int distance)
        {
            if (!Target.HasTarget()) return;

            var playerX = Player.Position.X;
            var playerY = Player.Position.Y;

            int xOffset = 0, yOffset = 0;
            
            switch (Player.Direction.ToLowerInvariant())
            {
                case "north":
                    yOffset = -distance;
                    break;
                case "south":
                    yOffset = distance;
                    break;
                case "east":
                    xOffset = distance;
                    break;
                case "west":
                    xOffset = -distance;
                    break;
                default:
                    Misc.SendMessage($"Unsupported direction: {Player.Direction}", 33);
                    return;
            }

            var targetX = playerX + xOffset;
            var targetY = playerY + yOffset;

            var statics = Statics.GetStaticsTileInfo(targetX, targetY, Player.Map);
            if (statics.Count > 0)
            {
                Target.TargetExecute(targetX, targetY, statics[0].StaticZ, statics[0].StaticID);
            }
            else
            {
                var landZ = Statics.GetLandZ(targetX, targetY, Player.Map);
                Target.TargetExecute(targetX, targetY, landZ);
            }
        }
    }
}