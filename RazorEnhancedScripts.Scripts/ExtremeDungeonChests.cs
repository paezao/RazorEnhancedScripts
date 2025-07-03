/*
 * ================================================
 * Extreme Dungeon Chests
 * ================================================
 *
 * Version: 1.0.0
 * Last Updated: 2025-07-03
 * Author: nkr
 *
 * ------------------------------------------------
 * Overview:
 * This script automates and assists with opening dungeon chests in dungeons.
 * No more clicking the lockpicks or running remove trap multiple times.
 * Just get 1 tile from the chest and the script will do its job.
 *
 * ------------------------------------------------
 * Features:
 * ✔️ Gump that shows an updated status on what it's doing
 * ✔️ Supports all kinds of dungeons chests and locked containers.
 *
 * ------------------------------------------------
 * Changelog:
 *
 * [1.0.0] - 2025-07-03
 *   - Initial release
 *
 * ================================================
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class ExtremeDungeonChests
    {
        private enum State
        {
            Searching = 0,
            Picking,
            RemovingTrap,
        };
        
        private static readonly List<int> DungeonChestItemIds = new List<int>{
            0x0E40, 0x0E42, 0x09AB, 0x0E42, // Chests
            0x0E7F, // Keg
            0x09A9, 0x0E3E, 0x0E3C, // Crates
            0x0E77, // Barrel
        };
        private const int LockpickItemId = 0x14FC;
        
        private const int GumpId = 543213422;
        private const int GumpWidth = 300;
        private const int GumpBackground = 1755;
        private const int GumpIcon = 0xE40;
        
        private State _state;
        private Item _currentChest = null;
        private readonly Journal _journal = new Journal();
        
        public void Run()
        {
            _state = State.Searching;
            UpdateGump();

            try
            {
                while (true)
                {
                    var gumpData = Gumps.GetGumpData(GumpId);

                    switch (gumpData.buttonid)
                    {
                        case 0:
                        {
                            Gumps.CloseGump(GumpId);
                            CleanTracking();
                            return;
                        }
                    }

                    if (!CheckLockpicks()) return;
                    if (!CheckChestDistance())
                    {
                        CleanTracking();
                        continue;
                    }

                    switch (_state)
                    {
                        case State.Searching:
                            HandleSearching();
                            break;
                        case State.Picking:
                            HandlePicking();
                            break;
                        case State.RemovingTrap:
                            HandleRemovingTrap();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    Misc.Pause(100);
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

        private void HandleSearching()
        {
            var itemsFilter = new Items.Filter
            {
                Enabled = true,
                RangeMax = 1,
                OnGround = 1,
                IsContainer = 1,
                CheckIgnoreObject = true,
            };
            var items = Items.ApplyFilter(itemsFilter);

            _currentChest = items.FirstOrDefault(item => DungeonChestItemIds.Contains(item.ItemID));
            if (_currentChest != null)
            {
                PickChest();
            }
        }

        private bool CheckChestDistance()
        {
            if (_currentChest == null) return true;
            var distanceToChest = Player.DistanceTo(_currentChest);
            return distanceToChest <= 1;
        }

        private void HandlePicking()
        {
            var lockpicks = Items.FindByID(LockpickItemId, -1, Player.Backpack.Serial, 1);
            if (lockpicks == null) return;
            
            _journal.Clear();
            Items.UseItem(lockpicks);
            Target.WaitForTarget(5000, true);
            Target.TargetExecute(_currentChest);
            Misc.Pause(2000);

            if (_journal.Search("The lock quickly yields to your skill.") || _journal.Search("This does not appear to be locked."))
            {
                _state = State.RemovingTrap;
                UpdateGump();
            }
        }

        private void HandleRemovingTrap()
        {
            Player.UseSkill("Remove Trap", _currentChest);
            Misc.Pause(2000);
            
            if (_journal.Search("You successfully render the trap harmless.") || _journal.Search("That doesn't appear to be trapped."))
            {
                Player.HeadMessage(0x43, "Chest is open! Enjoy!");
                Items.UseItem(_currentChest);
                Misc.IgnoreObject(_currentChest);
                CleanTracking();
                _state = State.Searching;
                UpdateGump();
            }
        }

        private bool CheckLockpicks()
        {
            if (Items.FindByID(LockpickItemId, -1, Player.Backpack.Serial, true) != null)
            {
                return true;
            }
            Player.HeadMessage(0x90, "Hey, you know you're out of picks, right? Closing for now...");
            return false;
        }

        private void PickChest()
        {
            _state = State.Picking;
        }

        private void CleanTracking()
        {
            _state = State.Searching;
            _currentChest = null;
            Player.TrackingArrow(0, 0, false);
            UpdateGump();
        }

        private void UpdateGump()
        {
            var gump = Gumps.CreateGump();
            gump.x = 300;
            gump.y = 300;
            Gumps.AddPage(ref gump, 0);
            
            const int leftPadding = 55;
            const int topPadding = 8;
            
            Gumps.AddBackground(ref gump, 0, 0, GumpWidth, 35, GumpBackground);
            Gumps.AddItem(ref gump, 0, -5, GumpIcon);

            var text = "";
            var color = 0;
            switch (_state)
            {
                case State.Searching:
                    text = "Searching for chest...";
                    color = 0x90;
                    break;
                case State.Picking:
                    text = "Picking...";
                    color = 0x90;
                    break;
                case State.RemovingTrap:
                    text = "Working on the trap...";
                    color = 0x90;
                    break;
            }
            Gumps.AddLabel(ref gump, leftPadding, topPadding, color, text);
            
            Gumps.CloseGump(GumpId);
            gump.serial = (uint)Player.Serial;
            gump.gumpId = GumpId;
            Gumps.SendGump(gump, 150, 150);
        }
    }
}