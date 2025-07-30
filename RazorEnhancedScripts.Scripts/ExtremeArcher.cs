using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RazorEnhanced;

namespace RazorEnhancedScripts.Scripts
{
    public class ExtremeArcher
    {
        private const uint GumpId = 126542315;
        
        private enum Mode
        {
            Stationary = 0,
            Moving
        };
        private enum Stance
        {
            None = 0,
            Confidence,
            Evasion,
        };

        private enum Button
        {
            StationaryMode = 1,
            MovingMode,
            Confidence,
            Evasion,
            NoStance,
            ConsecrateWeaponOn,
            ConsecrateWeaponOff,
            EnemyOfOneOn,
            EnemyOfOneOff,
        };
        
        private Mode _currentMode = Mode.Stationary;
        private Stance _currentStance = Stance.None;
        
        private const string SpellNameBushidoConfidence = "Confidence";
        private const string SpellNameBushidoEvasion = "Evasion";
        private const string SpellNameChivalryConsecrateWeapon = "Consecrate Weapon";
        private const string SpellNameChivalryEnemyOfOne = "Enemy Of One";
        private const string SpellNameChivalryDivineFury = "Divine Fury";

        private const int IconEnemyOfOne = 0x5105;
        private const int IconConsecrateWeapon = 0x5102;
        
        private const int SpellManaChivalryConsecrateWeapon = 10;
        private const int SpellManaChivalryDivineFury = 10;
        private const int SpellManaChivalryEnemyOfOne = 20;

        private Journal _journal = new Journal();
        
        public void Run()
        {
            try
            {
                UpdateGump();
                while (true)
                {
                    var gd = Gumps.GetGumpData(GumpId);
                    switch (gd.buttonid)
                    {
                        case 0: return;
                        case (int)Button.StationaryMode: Misc.SetSharedValue("ExtremeArcher:Mode","Stationary"); break;
                        case (int)Button.MovingMode: Misc.SetSharedValue("ExtremeArcher:Mode","Moving"); break;
                        case (int)Button.Confidence: Misc.SetSharedValue("ExtremeArcher:Stance", SpellNameBushidoConfidence); break;
                        case (int)Button.Evasion: Misc.SetSharedValue("ExtremeArcher:Stance", SpellNameBushidoEvasion); break;
                        case (int)Button.NoStance: Misc.SetSharedValue("ExtremeArcher:Stance", "None"); break;
                        case (int)Button.EnemyOfOneOn: Misc.SetSharedValue("ExtremeArcher:EnemyOfOne", "on"); break;
                        case (int)Button.EnemyOfOneOff: Misc.SetSharedValue("ExtremeArcher:EnemyOfOne", "off"); break;
                    }

                    if (gd.buttonid > 0)
                    {
                        UpdateGump();
                    }

                    var oldMode = _currentMode;
                    var oldStance = _currentStance;
                    
                    _currentMode = IsStationary() ? Mode.Stationary : Mode.Moving;
                    _currentStance = GetBushidoStance();
                    
                    if (_currentMode != oldMode || _currentStance != oldStance)
                    {
                        UpdateGump();
                    }
                    
                    if (Player.WarMode)
                    {
                        MaintainStance();
                        if (IsPlayerPaladin())
                        {
                            MaintainEnemyOfOne();
                            MaintainDivineFury();
                            MaintainConsecrateWeapon();
                        }

                        if (_currentMode == Mode.Stationary)
                        {
                            ExecuteStationaryRotation();
                        }
                        else
                        {
                            ExecuteMovingRotation();
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

        private bool IsPlayerPaladin()
        {
            return Player.GetSkillValue("Chivalry") >= 60;
        }

        private int GetStationaryIcon()
        {
            return (int)Player.PrimarySpecial;
        }
        
        private int GetMovingIcon()
        {
            return (int)Player.SecondarySpecial;
        }

        private void ReadyStationaryAbility()
        {
            if (!Player.HasSpecial) Player.WeaponPrimarySA();
        }
        
        private void ReadyMovingAbility()
        {
            
            if (!Player.HasSpecial) Player.WeaponSecondarySA();
        }

        private void MaintainStance()
        {
            var stance = GetBushidoSpellByBushidoStance(_currentStance);
            if (stance == "None" || Player.Buffs.Contains(stance)) return;
            
            Spells.CastBushido(stance);
            Misc.Pause(300);
        }

        private void MaintainConsecrateWeapon()
        {
            if (!IsConsecrateWeaponEnabled()) return;
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
        
        private void MaintainDivineFury()
        {
            if (Player.Buffs.Contains(SpellNameChivalryDivineFury)) return;
            if (Player.Mana < SpellManaChivalryDivineFury) return;
            
            Spells.CastChivalry(SpellNameChivalryDivineFury);
            Misc.Pause(300);
        }
        
        private void MaintainEnemyOfOne()
        {
            if (!IsEnemyOfOneEnabled())
            {
                DisableEnemyOfOne();
                return;
            }
            if (Player.Buffs.Contains(SpellNameChivalryEnemyOfOne)) return;
            if (Player.Mana < SpellManaChivalryEnemyOfOne) return;
            
            Spells.CastChivalry(SpellNameChivalryEnemyOfOne);
            Misc.Pause(300);
        }

        private void ExecuteMovingRotation()
        {
            if (!Player.HasSpecial)
            {
                ReadyMovingAbility();
            }
        }
        
        private void ExecuteStationaryRotation()
        {
            if (Player.HasSpecial) return;
            
            ReadyStationaryAbility();
            Misc.Pause(100);
        }

        private bool IsConsecrateWeaponEnabled()
        {
            var val = Misc.ReadSharedValue("ExtremeArcher:ConsecrateWeapon");
            switch (val)
            {
                case "on": return true;
                case "off": return false;
                default: return true;
            }
        }
        
        private bool IsEnemyOfOneEnabled()
        {
            var val = Misc.ReadSharedValue("ExtremeArcher:EnemyOfOne");
            switch (val)
            {
                case "on": return true;
                default: return false;
            }
        }
        
        private Stance GetBushidoStance()
        {
            var val = Misc.ReadSharedValue("ExtremeArcher:Stance");
            switch (val)
            {
                case "None": return Stance.None;
                case "Confidence": return Stance.Confidence;
                case "Evasion": return Stance.Evasion;
                default: return Stance.None;
            }
        }

        private string GetBushidoSpellByBushidoStance(Stance stance)
        {
            switch (stance)
            {
                case Stance.None: return "None";
                case Stance.Confidence: return SpellNameBushidoConfidence;
                case Stance.Evasion: return SpellNameBushidoEvasion;
                default: return "None";
            }
        }

        private bool IsStationary()
        {
            var val = Misc.ReadSharedValue("ExtremeArcher:Mode");
            var mode = (val is string) ? !string.IsNullOrEmpty(val.ToString()) ? val.ToString() : "Stationary" : "Stationary";
            if (string.IsNullOrEmpty(mode))
            {
                mode = "Stationary";
            }
            
            return mode == "Moving";
        }

        private void UpdateGump()
        {
            var gump = Gumps.CreateGump();
            gump.gumpId = GumpId;
            gump.serial = (uint)Player.Serial;
            
            var gumpWidth = IsPlayerPaladin() ? 205 : 105;
            Gumps.AddBackground(ref gump,0,0,gumpWidth,55,1755);

            if (_currentMode != Mode.Moving)
            {
                Gumps.AddButton(ref gump, 5,5, GetStationaryIcon(), GetStationaryIcon(),(int)Button.StationaryMode,1,0);
                Gumps.AddTooltip(ref gump, "Stationary");
            }
            else
            {
                Gumps.AddButton(ref gump, 5,5,GetMovingIcon(),GetMovingIcon(),(int)Button.MovingMode,1,0);
                Gumps.AddTooltip(ref gump, "Moving");
            }

            switch (_currentStance)
            {
                case Stance.Confidence:
                {
                    Gumps.AddButton(ref gump, 55,5,21537,21537,(int)Button.Confidence,1,0);
                    Gumps.AddTooltip(ref gump, "Confidence");
                    break;
                }
                case Stance.Evasion:
                {
                    Gumps.AddButton(ref gump, 55,5,21538,21538,(int)Button.Evasion,1,0);
                    Gumps.AddTooltip(ref gump, "Evasion");
                    break;
                }
                case Stance.None:
                {
                    Gumps.AddButton(ref gump, 55,5,21539,21539,(int)Button.NoStance,1,0);
                    Gumps.AddTooltip(ref gump, "No Stance");
                    break;
                }
            }

            if (IsPlayerPaladin())
            {
                // Consecrate Weapon
                var consecrateButtonId = IsConsecrateWeaponEnabled() ? Button.ConsecrateWeaponOff : Button.ConsecrateWeaponOn;
                Gumps.AddButton(ref gump, 105, 5, IconConsecrateWeapon, IconConsecrateWeapon, (int)consecrateButtonId, 1, 0);
                Gumps.AddTooltip(ref gump, "Consecrate Weapon");
                if (!IsConsecrateWeaponEnabled())
                {
                    Gumps.AddImage(ref gump, 115, 30, 1150);
                }
                
                // Enemy Of One
                var enemyOfOneButtonId = IsEnemyOfOneEnabled() ? Button.EnemyOfOneOff : Button.EnemyOfOneOn;
                Gumps.AddButton(ref gump, 155, 5, IconEnemyOfOne, IconEnemyOfOne, (int)enemyOfOneButtonId, 1, 0);
                Gumps.AddTooltip(ref gump, "Enemy of One");
                if (!IsEnemyOfOneEnabled())
                {
                    Gumps.AddImage(ref gump, 165, 30, 1150);
                }
            }

            Gumps.CloseGump(GumpId);
            Gumps.SendGump(gump,500,500);
        }
    }
}