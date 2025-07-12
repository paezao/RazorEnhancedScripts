/*
 * ================================================
 * Extreme Dungeon Chests
 * ================================================
 *
 * Version: 1.1.1
 * Last Updated: 2025-07-12
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
 * ✔️ Supports all kinds of dungeons chests and locked containers
 * ✔️ Allows pausing and continuing by clicking the red/green button
 * ✔️ Marks locked containers with a color so you know where to go
 *
 * ------------------------------------------------
 * Changelog:
 *
 * [1.1.1] - 2025-07-12
 *   - Added Kotl City Ruins as a location for chests
 *   - Fixed handling of regal chests
 *   - Fixed bug related to leaving and re-joining dungeons
 * [1.1.0] - 2025-07-10
 *   - Fixed bug where gump stayed open after closing it
 *   - Added Regal Chests
 *   - Added color painting to mark current chest
 *   - Added color painting to locked chests
 *   - Added button to pause the script
 *   - Added extra checks so it doesn't try and open containers that aren't locked
 *   - Extra checks for when player is a ghost or isn't in a dungeon
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
            Paused = 0,
            Searching,
            Picking,
            RemovingTrap,
        };
        
        private static readonly List<int> DungeonChestItemIds = new List<int>{
            0x0E40, 0x0E42, 0x09AB, 0x0E42, // Chests
            0x0E7F, // Keg
            0x09A9, 0x0E3E, 0x0E3C, // Crates
            0x0E77, // Barrel
            0x4D0C, 0x4D0D, // Regal Chests
        };
        private const int LockpickItemId = 0x14FC;

        private const int CurrentChestColor = 0x3F;
        private const int FinishedChestColor = 0x2E;
        private const int RadarChestColor1 = 0x90;
        private const int RadarChestColor2 = 0x9C;
        private const int GumpId = 543213422;
        private const int GumpWidth = 300;
        private const int GumpBackground = 1755;
        private const int GumpIcon = 0xE40;
        private const int PauseButtonId = 0x2C88;
        private const int PauseButtonPressedId = 0x2C89;
        private const int ContinueButtonId = 0x2C92;
        private const int ContinueButtonPressedId = 0x2C93;
        
        private State _state;
        private Item _currentChest = null;
        private int _currentChestInitialColor = 0;
        private readonly Journal _journal = new Journal();
        private bool wasInDungeonOrGhost = false;
        
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
                            CleanTracking();
                            Gumps.CloseGump(GumpId);
                            return;
                        }
                        case 1: // Resuming...
                        {
                            CleanTracking();
                            break;
                        }
                        case 2: // Pausing...
                        {
                            CleanTracking();
                            _state = State.Paused;
                            UpdateGump();
                            break;
                        }
                    }

                    if (!CheckLockpicks()) return;
                    
                    if (!IsPlayerCloseToChest())
                    {
                        HandleFarFromChest();
                    }

                    switch (_state)
                    {
                        case State.Paused:
                            break;
                        case State.Searching:
                            HandleSearching();
                            Misc.Pause(300);
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

        private bool IsLockedContainer(Item container)
        {
            return (container.IsContainer &&
                    container.Properties.Count == 1) ||
                   ((container.ItemID == 0x4D0C || container.ItemID == 0x4D0D) &&
                    container.Properties.Count == 2);
        }

        private void HandleSearching()
        {
            if (Player.IsGhost || !IsPlayerInDungeon())
            {
                wasInDungeonOrGhost = true;
                UpdateGump();
                return;
            }

            if (wasInDungeonOrGhost)
            {
                wasInDungeonOrGhost = false;
                UpdateGump();
            }
            
            var itemsFilter = new Items.Filter
            {
                Enabled = true,
                RangeMax = 10,
                OnGround = 1,
                IsContainer = 1,
                CheckIgnoreObject = true,
            };
            var items = Items.ApplyFilter(itemsFilter);

            foreach (var chest in items.Where(item => DungeonChestItemIds.Contains(item.ItemID) && IsLockedContainer(item)))
            {
                if (Player.DistanceTo(chest) > 1)
                {
                    var blinkColor = RadarChestColor1;
                    if (chest.Color == RadarChestColor1)
                    {
                       blinkColor = RadarChestColor2; 
                    }
                    PaintChest(chest, blinkColor);
                    continue;
                }
                
                _currentChest = chest;
                _currentChestInitialColor = _currentChest.Color; 
                PaintCurrentChest();
                PickChest();
            }
        }

        private bool IsPlayerCloseToChest()
        {
            if (_currentChest == null) return false;
            var distanceToChest = Player.DistanceTo(_currentChest);
            return distanceToChest <= 2;
        }

        private void HandlePicking()
        {
            var lockpicks = Items.FindByID(LockpickItemId, -1, Player.Backpack.Serial, 1);
            if (lockpicks == null) return;
            
            _journal.Clear();
            Items.UseItem(lockpicks);
            Target.WaitForTarget(5000, true);
            Target.TargetExecute(_currentChest);
            Misc.Pause(1000);
            
            if (_journal.Search("You can't unlock that."))
            {
                var finishedChest = _currentChest;
                CleanTracking();
                PaintFinishedChest(finishedChest);
                return;
            }

            if (_journal.Search("The lock quickly yields to your skill.") || _journal.Search("This does not appear to be locked."))
            {
                _state = State.RemovingTrap;
                UpdateGump();
            }
        }

        private void HandleRemovingTrap()
        {
            Player.UseSkill("Remove Trap", _currentChest);
            Misc.Pause(1000);
            
            if (_journal.Search("You successfully render the trap harmless.") || _journal.Search("That doesn't appear to be trapped."))
            {
                Player.HeadMessage(0x43, "Chest is open! Enjoy!");
                Items.UseItem(_currentChest);
                Misc.IgnoreObject(_currentChest);
                var finishedChest = _currentChest;
                CleanTracking();
                PaintFinishedChest(finishedChest);
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
        
        private void PaintCurrentChest()
        {
            if (_currentChest == null) return;
            PaintChest(_currentChest, CurrentChestColor);
        }
        
        private void PaintFinishedChest(Item chest)
        {
            PaintChest(chest, FinishedChestColor);
        }
        
        private void PaintChest(Item chest, int color)
        {
            Items.SetColor(chest.Serial, color);
        }

        private void PickChest()
        {
            _state = State.Picking;
        }

        private void CleanTracking()
        {
            if (_currentChest != null)
            {
                Items.SetColor(_currentChest.Serial, _currentChestInitialColor);
                _currentChest = null;
            }
            _state = State.Searching;
            UpdateGump();
        }
        
        private void HandleFarFromChest()
        {
            if (_currentChest != null)
            {
                CleanTracking();
            }
        }

        private bool IsPlayerInDungeon()
        {
            return IsPlayerInKotlCityRuins() || string.Equals(Player.Zone(), "dungeons", StringComparison.InvariantCultureIgnoreCase);
        }

        private bool IsPlayerInKotlCityRuins()
        {
            return Player.Map == 5 &&
                   Player.Position.X > 433 && Player.Position.X < 676 &&
                   Player.Position.Y > 2270 && Player.Position.Y < 2500;
        }

        private void UpdateGump()
        {
            var gump = Gumps.CreateGump();
            gump.x = 300;
            gump.y = 300;
            Gumps.AddPage(ref gump, 0);
            
            var leftPadding = 55;
            const int topPadding = 8;
            
            Gumps.AddBackground(ref gump, 0, 0, GumpWidth, 35, GumpBackground);
            Gumps.AddItem(ref gump, 0, -5, GumpIcon);
            var pauseButtonPadding = 2;
            if (_state == State.Paused)
            {
                Gumps.AddButton(ref gump, leftPadding, topPadding + pauseButtonPadding, ContinueButtonId, ContinueButtonPressedId, 1, 1, 0);
            }
            else
            {
                Gumps.AddButton(ref gump, leftPadding, topPadding + pauseButtonPadding, PauseButtonId, PauseButtonPressedId, 2, 1, 0);
            }

            leftPadding += 22;

            var text = "";
            var color = 0;
            switch (_state)
            {
                case State.Paused:
                    text = "Paused!";
                    color = 0x90;
                    break;
                case State.Searching:
                    text = "Searching for chest...";
                    if (Player.IsGhost)
                    {
                        text = "I'm sorry that you're dead...";
                    } else if (!IsPlayerInDungeon())
                    {
                        text = "You won't find any chests here.";
                    }
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