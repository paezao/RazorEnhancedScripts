using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class ExtremeSampire
    {
        private const uint GumpID = 126542315;
        
        private enum Mode
        {
            Single = 0,
            Multi
        };
        private enum Stance
        {
            Confidence = 0,
            Evasion,
            CounterAttack,
        };
        
        private Mode _currentMode = Mode.Single;
        private Stance _currentStance = Stance.CounterAttack;
        
        private const string SpellNameMasteryOnslaught = "Onslaught";
        private const string SpellNameBushidoConfidence = "Confidence";
        private const string SpellNameBushidoEvasion = "Evasion";
        private const string SpellNameBushidoCounterAttack = "Counter Attack";
        private const string SpellNameChivalryConsecrateWeapon = "Consecrate Weapon";
        private const string SpellNameChivalryEnemyOfOne = "Enemy Of One";

        private const int SpellManaChivalryConsecrateWeapon = 10;
        private const int SpellManaChivalryEnemyOfOne = 20;

        private DateTime _lastOnslaughtTime = DateTime.MinValue;
        private const int OnslaughtDurationMs = 8000;

        private Journal _journal = new Journal();
        
        public void Run()
        {
            try
            {
                UpdateGump();
                while (true)
                {
                    var gd = Gumps.GetGumpData(GumpID);
                    switch (gd.buttonid)
                    {
                        case 0: return;
                        case 1: Misc.SetSharedValue("ExtremeSampire:Mode","Multi"); break;
                        case 2: Misc.SetSharedValue("ExtremeSampire:Mode","Single"); break;
                        case 3: Misc.SetSharedValue("ExtremeSampire:Stance", SpellNameBushidoConfidence); break;
                        case 4: Misc.SetSharedValue("ExtremeSampire:Stance", SpellNameBushidoEvasion); break;
                        case 5: Misc.SetSharedValue("ExtremeSampire:Stance", SpellNameBushidoCounterAttack); break;
                    }

                    var oldMode = _currentMode;
                    var oldStance = _currentStance;
                    
                    _currentMode = IsMulti() ? Mode.Multi : Mode.Single;
                    _currentStance = GetBushidoStance();
                    
                    if (_currentMode != oldMode || _currentStance != oldStance)
                    {
                        UpdateGump();
                    }
                    
                    if (Player.WarMode)
                    {
                        MaintainStance();
                        MaintainConsecrateWeapon();

                        if (_currentMode == Mode.Multi)
                        {
                            DisableEnemyOfOne();
                            ExecuteMultiTargetRotation();
                        }
                        else
                        {
                            MaintainEnemyOfOne();
                            ExecuteSingleTargetRotation();
                        }
                    }

                    Misc.Pause(500);
                }
            }
            catch (Exception e)
            {
                if (e.GetType() != typeof(ThreadAbortException))
                {
                    Misc.SendMessage(e.ToString());
                }
            }
        }

        private void MaintainStance()
        {
            var stance = GetBushidoSpellByBushidoStance(_currentStance);
            if (Player.Buffs.Contains(stance)) return;
            
            Spells.CastBushido(stance);
            Misc.Pause(300);
        }

        private void MaintainConsecrateWeapon()
        {
            if (Player.Buffs.Contains(SpellNameChivalryConsecrateWeapon)) return;
            if (Player.Mana < SpellManaChivalryConsecrateWeapon) return;
            
            Spells.CastChivalry(SpellNameChivalryConsecrateWeapon);
            Misc.Pause(300);
        }
        
        private void DisableEnemyOfOne()
        {
            if (!Player.Buffs.Contains(SpellNameChivalryEnemyOfOne)) return;
            if (Player.Mana < SpellManaChivalryEnemyOfOne) return;
            
            Spells.CastChivalry(SpellNameChivalryEnemyOfOne);
            Misc.Pause(300);
        }
        
        private void MaintainEnemyOfOne()
        {
            if (Player.Buffs.Contains(SpellNameChivalryEnemyOfOne)) return;
            if (Player.Mana < SpellManaChivalryEnemyOfOne) return;
            
            Spells.CastChivalry(SpellNameChivalryEnemyOfOne);
            Misc.Pause(300);
        }

        private void ExecuteMultiTargetRotation()
        {
            if (!Player.HasSpecial)
            {
                Player.WeaponSecondarySA();
            }
        }
        
        private void ExecuteSingleTargetRotation()
        {
            var onslaughtActive = (DateTime.Now - _lastOnslaughtTime).TotalMilliseconds < OnslaughtDurationMs;

            if (!onslaughtActive)
            {
                Spells.CastMastery(SpellNameMasteryOnslaught);
                Misc.Pause(100);

                if (WaitForOnslaughtHit())
                {
                    Player.HeadMessage(0x90, "Onslaught");
                    _lastOnslaughtTime = DateTime.Now;
                }
                return;
            }

            if (Player.HasSpecial) return;
            
            Player.WeaponPrimarySA();
            Misc.Pause(100);
        }

        private bool WaitForOnslaughtHit(int timeoutMs = 2500)
        {
            _journal.Clear();
            DateTime start = DateTime.Now;

            while ((DateTime.Now - start).TotalMilliseconds < timeoutMs)
            {
                if (_journal.Search("You deliver an onslaught of sword strikes!"))
                {
                    return true;
                }

                Misc.Pause(100);
            }

            return false;
        }
        
        private Stance GetBushidoStance()
        {
            var val = Misc.ReadSharedValue("ExtremeSampire:Stance");
            switch (val)
            {
                case "Counter Attack": return Stance.CounterAttack;
                case "Confidence": return Stance.Confidence;
                case "Evasion": return Stance.Evasion;
                default: return Stance.CounterAttack;
            }
        }

        private string GetBushidoSpellByBushidoStance(Stance stance)
        {
            switch (stance)
            {
                case Stance.CounterAttack: return SpellNameBushidoCounterAttack;
                case Stance.Confidence: return SpellNameBushidoConfidence;
                case Stance.Evasion: return SpellNameBushidoEvasion;
                default: return SpellNameBushidoCounterAttack;
            }
        }

        private bool IsMulti()
        {
            var val = Misc.ReadSharedValue("ExtremeSampire:Mode");
            var mode = (val is string) ? !string.IsNullOrEmpty(val.ToString()) ? val.ToString() : "Single" : "Single";
            if (string.IsNullOrEmpty(mode))
            {
                mode = "Single";
            }
            
            return mode == "Multi";
        }

        private void UpdateGump()
        {
            var gump = Gumps.CreateGump();
            gump.gumpId = GumpID;
            gump.serial = (uint)Player.Serial;
            Gumps.AddBackground(ref gump,0,0,105,55,1755);

            if (_currentMode != Mode.Multi)
            {
                Gumps.AddButton(ref gump, 5,5, (int)Player.PrimarySpecial, (int)Player.PrimarySpecial,1,1,0);
                Gumps.AddTooltip(ref gump, "Single Target");
            }
            else
            {
                Gumps.AddButton(ref gump, 5,5,(int)Player.SecondarySpecial,(int)Player.SecondarySpecial,2,1,0);
                Gumps.AddTooltip(ref gump, "Multi Target");
            }

            switch (_currentStance)
            {
                case Stance.Confidence:
                {
                    Gumps.AddButton(ref gump, 55,5,21537,21537,3,1,0);
                    Gumps.AddTooltip(ref gump, "Confidence");
                    break;
                }
                case Stance.Evasion:
                {
                    Gumps.AddButton(ref gump, 55,5,21538,21538,4,1,0);
                    Gumps.AddTooltip(ref gump, "Evasion");
                    break;
                }
                case Stance.CounterAttack:
                {
                    Gumps.AddButton(ref gump, 55,5,21539,21539,5,1,0);
                    Gumps.AddTooltip(ref gump, "Counter Attack");
                    break;
                }
            }

            Gumps.CloseGump(GumpID);
            Gumps.SendGump(gump,500,500);
        }
    }
}