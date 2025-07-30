using System;
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

        private enum Button
        {
            MultiTarget = 1,
            SingleTarget,
            Confidence,
            Evasion,
            CounterAttack,
            ConsecrateWeaponOn,
            ConsecrateWeaponOff,
            EnemyOfOneOn,
            EnemyOfOneOff,
            AutoAttackOn,
            AutoAttackOff,
        };
        
        private Mode _currentMode = Mode.Single;
        private Stance _currentStance = Stance.CounterAttack;
        
        private const string SpellNameMasteryOnslaught = "Onslaught";
        private const string SpellNameBushidoConfidence = "Confidence";
        private const string SpellNameBushidoEvasion = "Evasion";
        private const string SpellNameBushidoCounterAttack = "Counter Attack";
        private const string SpellNameChivalryConsecrateWeapon = "Consecrate Weapon";
        private const string SpellNameChivalryEnemyOfOne = "Enemy Of One";

        private const int IconEnemyOfOne = 0x5105;
        private const int IconAutoAttack = 0x5105;
        private const int IconConsecrateWeapon = 0x5102;
        
        private const int RadiantScimitarItemId = 0x2D33;

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
                        case (int)Button.MultiTarget: Misc.SetSharedValue("ExtremeSampire:Mode","Multi"); break;
                        case (int)Button.SingleTarget: Misc.SetSharedValue("ExtremeSampire:Mode","Single"); break;
                        case (int)Button.Confidence: Misc.SetSharedValue("ExtremeSampire:Stance", SpellNameBushidoConfidence); break;
                        case (int)Button.Evasion: Misc.SetSharedValue("ExtremeSampire:Stance", SpellNameBushidoEvasion); break;
                        case (int)Button.CounterAttack: Misc.SetSharedValue("ExtremeSampire:Stance", SpellNameBushidoCounterAttack); break;
                        case (int)Button.ConsecrateWeaponOn: Misc.SetSharedValue("ExtremeSampire:ConsecrateWeapon", "on"); break;
                        case (int)Button.ConsecrateWeaponOff: Misc.SetSharedValue("ExtremeSampire:ConsecrateWeapon", "off"); break;
                        case (int)Button.EnemyOfOneOn: Misc.SetSharedValue("ExtremeSampire:EnemyOfOne", "on"); break;
                        case (int)Button.EnemyOfOneOff: Misc.SetSharedValue("ExtremeSampire:EnemyOfOne", "off"); break;
                        case (int)Button.AutoAttackOn: Misc.SetSharedValue("ExtremeSampire:AutoAttack", "on"); break;
                        case (int)Button.AutoAttackOff: Misc.SetSharedValue("ExtremeSampire:AutoAttack", "off"); break;
                    }

                    if (gd.buttonid > 0)
                    {
                        UpdateGump();
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
                        if (IsPlayerPaladin()) MaintainEnemyOfOne();
                        if (IsPlayerPaladin()) MaintainConsecrateWeapon();

                        if (_currentMode == Mode.Multi)
                        {
                            ExecuteMultiTargetRotation();
                        }
                        else
                        {
                            ExecuteSingleTargetRotation();
                        }

                        if (IsAutoAttackEnabled()) AttackNearest();
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

        private void AttackNearest()
        {
            var filter = new Mobiles.Filter();
            filter.Notorieties.Add(6);
            filter.CheckLineOfSight = true;
            filter.RangeMax = 5;
            filter.IgnorePets = true;
            var mobiles = Mobiles.ApplyFilter(filter);
            if (mobiles.Count > 0)
            {
                Player.Attack(Mobiles.Select(mobiles, "Nearest"));
            }
        }

        private bool IsPlayerPaladin()
        {
            return Player.GetSkillValue("Chivalry") >= 60;
        }

        private int GetSingleTargetIcon()
        {
            var currentWeapon = Player.GetItemOnLayer("RightHand");
            if (currentWeapon != null && currentWeapon.ItemID == RadiantScimitarItemId)
            {
                return (int)Player.SecondarySpecial;
            }
            else
            {
                return (int)Player.PrimarySpecial;
            }
        }
        
        private int GetMultiTargetIcon()
        {
            var currentWeapon = Player.GetItemOnLayer("RightHand");
            if (currentWeapon != null && currentWeapon.ItemID == RadiantScimitarItemId)
            {
                return (int)Player.PrimarySpecial;
            }
            else
            {
                return (int)Player.SecondarySpecial;
            }
        }

        private void ReadySingleTargetAbility()
        {
            var currentWeapon = Player.GetItemOnLayer("RightHand");
            if (currentWeapon != null && currentWeapon.ItemID == RadiantScimitarItemId)
            {
                if (!Player.HasSpecial) Player.WeaponSecondarySA();
            }
            else
            {
                if (!Player.HasSpecial) Player.WeaponPrimarySA();
            }
        }
        
        private void ReadyMultiTargetAbility()
        {
            
            var currentWeapon = Player.GetItemOnLayer("RightHand");
            if (currentWeapon != null && currentWeapon.ItemID == RadiantScimitarItemId)
            {
                if (!Player.HasSpecial) Player.WeaponPrimarySA();
            }
            else
            {
                if (!Player.HasSpecial) Player.WeaponSecondarySA();
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

        private void ExecuteMultiTargetRotation()
        {
            if (!Player.HasSpecial)
            {
                ReadyMultiTargetAbility();
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
            
            ReadySingleTargetAbility();
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

        private bool IsConsecrateWeaponEnabled()
        {
            var val = Misc.ReadSharedValue("ExtremeSampire:ConsecrateWeapon");
            switch (val)
            {
                case "on": return true;
                case "off": return false;
                default: return true;
            }
        }
        
        private bool IsEnemyOfOneEnabled()
        {
            var val = Misc.ReadSharedValue("ExtremeSampire:EnemyOfOne");
            switch (val)
            {
                case "on": return true;
                default: return false;
            }
        }
        
        private bool IsAutoAttackEnabled()
        {
            var val = Misc.ReadSharedValue("ExtremeSampire:AutoAttack");
            switch (val)
            {
                case "on": return true;
                default: return false;
            }
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
            
            var gumpWidth = IsPlayerPaladin() ? 255 : 155;
            Gumps.AddBackground(ref gump,0,0,gumpWidth,55,1755);

            var xCursor = 5;

            if (_currentMode != Mode.Multi)
            {
                Gumps.AddButton(ref gump, xCursor,5, GetSingleTargetIcon(), GetSingleTargetIcon(),(int)Button.SingleTarget,1,0);
                Gumps.AddTooltip(ref gump, "Single Target");
            }
            else
            {
                Gumps.AddButton(ref gump, xCursor,5,GetMultiTargetIcon(),GetMultiTargetIcon(),(int)Button.MultiTarget,1,0);
                Gumps.AddTooltip(ref gump, "Multi Target");
            }

            xCursor += 50;
            
            // Auto Attack
            var autoAttackButtonId = IsAutoAttackEnabled() ? Button.AutoAttackOff : Button.AutoAttackOn;
            Gumps.AddButton(ref gump, xCursor, 5, IconAutoAttack, IconAutoAttack, (int)autoAttackButtonId, 1, 0);
            Gumps.AddTooltip(ref gump, "Auto Attack");
            if (!IsAutoAttackEnabled())
            {
                Gumps.AddImage(ref gump, xCursor + 10, 30, 1150);
            }

            xCursor += 50;

            switch (_currentStance)
            {
                case Stance.Confidence:
                {
                    Gumps.AddButton(ref gump, xCursor,5,21537,21537,(int)Button.Confidence,1,0);
                    Gumps.AddTooltip(ref gump, "Confidence");
                    break;
                }
                case Stance.Evasion:
                {
                    Gumps.AddButton(ref gump, xCursor,5,21538,21538,(int)Button.Evasion,1,0);
                    Gumps.AddTooltip(ref gump, "Evasion");
                    break;
                }
                case Stance.CounterAttack:
                {
                    Gumps.AddButton(ref gump, xCursor,5,21539,21539,(int)Button.CounterAttack,1,0);
                    Gumps.AddTooltip(ref gump, "Counter Attack");
                    break;
                }
            }

            if (IsPlayerPaladin())
            {
                xCursor += 50;
                // Consecrate Weapon
                var consecrateButtonId = IsConsecrateWeaponEnabled() ? Button.ConsecrateWeaponOff : Button.ConsecrateWeaponOn;
                Gumps.AddButton(ref gump, xCursor, 5, IconConsecrateWeapon, IconConsecrateWeapon, (int)consecrateButtonId, 1, 0);
                Gumps.AddTooltip(ref gump, "Consecrate Weapon");
                if (!IsConsecrateWeaponEnabled())
                {
                    Gumps.AddImage(ref gump, xCursor + 10, 30, 1150);
                }
                
                xCursor += 50;
                // Enemy Of One
                var enemyOfOneButtonId = IsEnemyOfOneEnabled() ? Button.EnemyOfOneOff : Button.EnemyOfOneOn;
                Gumps.AddButton(ref gump, xCursor, 5, IconEnemyOfOne, IconEnemyOfOne, (int)enemyOfOneButtonId, 1, 0);
                Gumps.AddTooltip(ref gump, "Enemy of One");
                if (!IsEnemyOfOneEnabled())
                {
                    Gumps.AddImage(ref gump, xCursor + 10, 30, 1150);
                }
            }

            Gumps.CloseGump(GumpID);
            Gumps.SendGump(gump,500,500);
        }
    }
}