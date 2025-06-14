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
        
        private enum State
        {
            Single = 0,
            Multi
        };
        private State _currentState = State.Single;
        
        private const string SpellNameConsecrateWeapon = "Consecrate Weapon";
        private const string SpellNameVampiricEmbrace = "Vampiric Embrace";
        
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
                        case 2:
                            Misc.SetSharedValue("ExtremeSampire:Targets","Single");
                            break;
                        case 1:
                            Misc.SetSharedValue("ExtremeSampire:Targets","Multi");
                            break;
                    }

                    var oldState = _currentState;
                    _currentState = IsMulti() ? State.Multi : State.Single;
                    if (_currentState != oldState)
                    {
                        UpdateGump();
                    }
                    
                    if (Player.WarMode && Player.Mana > 10)
                    {
                        if (!Player.Buffs.Contains(SpellNameConsecrateWeapon))
                        {
                            // Cast Consecrate Weapon
                            Spells.CastChivalry(SpellNameConsecrateWeapon); 
                            Misc.Pause(200);
                        }
                        else if (!Player.Buffs.Contains(SpellNameVampiricEmbrace))
                        {
                            // Cast Vampiric Embrace
                            Spells.CastNecro(SpellNameVampiricEmbrace); 
                            Misc.Pause(200);
                        }
                        else
                        {
                            if (_currentState == State.Multi)
                            {
                                if (!Player.HasSpecial)
                                {
                                    PrimeMulti();
                                    Misc.Pause(200);
                                }
                            }
                            else
                            {
                                Player.WeaponPrimarySA();
                            } 
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


        private void PrimeMulti()
        {
            Player.WeaponSecondarySA();
        }

        private bool IsMulti()
        {
            var val = Misc.ReadSharedValue("ExtremeSampire:Targets");
            var stance = (val is string) ? !string.IsNullOrEmpty(val.ToString()) ? val.ToString() : "Single" : "Single";
            if (string.IsNullOrEmpty(stance))
            {
                stance = "Single";
            }
            
            return stance == "Multi";
        }

        private void UpdateGump()
        {
            var gump = Gumps.CreateGump();
            gump.gumpId = GumpID;
            gump.serial = (uint)Player.Serial;
            Gumps.AddBackground(ref gump,0,0,55,55,1755);

            if (_currentState != State.Multi)
            {
                Gumps.AddButton(ref gump, 5,5, (int)Player.PrimarySpecial, (int)Player.PrimarySpecial,1,1,0);
                Gumps.AddTooltip(ref gump, "Single Target");
            }
            else
            {
                Gumps.AddButton(ref gump, 5,5,(int)Player.SecondarySpecial,(int)Player.SecondarySpecial,2,1,0);
                Gumps.AddTooltip(ref gump, "Multi Target");
            }

            Gumps.CloseGump(GumpID);
            Gumps.SendGump(gump,500,500);
        }
    }
}