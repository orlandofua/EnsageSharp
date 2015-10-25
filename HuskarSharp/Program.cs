using System;
using System.Windows.Input;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using SharpDX;

namespace HuskarSharp
{
    class Program
    {
        private const Key ComboKey = Key.Space;

        private static Hero target;
        private static Hero me;
        private static ParticleEffect rangeDisplay;
        private static bool loaded;
        private static float lastRange;
        private static Ability spellQ;
        private static Ability spellR;

        #region Items

        private static Item Urn;
        private static Item Abyssal;
        private static Item Blademail;
        private static Item Mjollnir;
        private static Item Orchid;
        private static Item Halberd;
        private static Item Satanic;
        private static Item Hex;
        private static Item SolarCrest;
        private static Item Medallion;
        private static Item Blink;
        //private static Item Aghs;
        #endregion

        private static double turnTime;
        private const double LifebreakCastTime = 800;
        private const double InnervitalityCastTime = 830;

        


        public static void Main(string[] args)
        {
            Game.OnUpdate += Game_OnUpdate;
            Orbwalking.Load();
            if (rangeDisplay == null)
            {
                return;
            }
            rangeDisplay = null;

            Urn = null;
            Abyssal = null;
            Blademail = null;
            Mjollnir = null;
            Orchid = null;
            Halberd = null;
            Satanic = null;
            Hex = null;
            SolarCrest = null;
            Medallion = null;
            Blink = null;

            spellQ = null;
            spellR = null;
            //Aghs = null;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!loaded)
            {
                me = ObjectMgr.LocalHero;
                if (!Game.IsInGame || Game.IsWatchingGame || me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Huskar)
                {
                    return;
                }
                loaded = true;

                // Spells
                spellQ = me.Spellbook.SpellQ;
                spellR = me.Spellbook.SpellR;

                // Items
                if (Urn == null)
                    Urn = me.FindItem("item_urn_of_shadows");

                if (Abyssal == null)
                    Abyssal = me.FindItem("item_abyssal_blade");
                
                if (Blademail == null)
                    Blademail = me.FindItem("item_blade_mail");
                
                if (Mjollnir == null)
                    Mjollnir = me.FindItem("item_mjollnir");
                
                if (Orchid == null) 
                    Orchid = me.FindItem("item_orchid");
                
                if (Halberd == null)
                    Halberd = me.FindItem("item_heavens_halberd");
                
                if (Satanic == null)
                    Satanic = me.FindItem("item_satanic");
                
                if (Hex == null)
                    Hex = me.FindItem("item_sheepstick");

                if (Medallion == null)
                    Medallion = me.FindItem("item_medallion_of_courage");

                if (SolarCrest == null)
                    SolarCrest = me.FindItem("item_solar_crest");

                if (Blink == null)
                    Blink = me.FindItem("item_blink");
                //Aghs = me.FindItem("item_ultimate_scepter");
            }

            if (me == null || !me.IsValid)
            {
                loaded = false;
                me = ObjectMgr.LocalHero;
                if (rangeDisplay == null)
                {
                    return;
                }
                rangeDisplay = null;
                return;
            }

            if (Game.IsPaused)
            {
                return;
            }

            if (rangeDisplay == null)
            {
                rangeDisplay = me.AddParticleEffect(@"particles\ui_mouseactions\range_display.vpcf");
                lastRange = me.GetAttackRange() + me.HullRadius + 25;
                rangeDisplay.SetControlPoint(1, new Vector3(lastRange, 0, 0));
            }
            else
            {
                if (lastRange != (me.GetAttackRange() + me.HullRadius + 25))
                {
                    lastRange = me.GetAttackRange() + me.HullRadius + 25;
                    rangeDisplay.Dispose();
                    rangeDisplay = me.AddParticleEffect(@"particles\ui_mouseactions\range_display.vpcf");
                    rangeDisplay.SetControlPoint(1, new Vector3(lastRange, 0, 0));
                }
            }

            if (target != null && (!target.IsValid || !target.IsVisible || !target.IsAlive || target.Health <= 0))
            {
                target = null;
            }
            var canCancel = Orbwalking.CanCancelAnimation();
            if (canCancel)
            {
                if (target != null && !target.IsVisible && !Orbwalking.AttackOnCooldown(target))
                {
                    target = me.ClosestToMouseTarget();
                }
                else if (target == null || !Orbwalking.AttackOnCooldown(target))
                {
                    var bestAa = me.BestAATarget();
                    if (bestAa != null)
                    {
                        target = me.BestAATarget();
                    }
                }
            }

            if (Game.IsChatOpen)
            {
                return;
            }

            if (Game.IsKeyDown(ComboKey))
            {
                if (target != null)
                {
                    turnTime = me.GetTurnTime(target);

                    if (Blink.CanBeCasted() && me.Distance2D(target) <= Blink.CastRange && Utils.SleepCheck("Blink") &&
                        target != null)
                    {
                        Blink.UseAbility(target.Position);
                        Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "Blink");
                    }

                    if (spellR.CanBeCasted(target) && me.Distance2D(target) <= spellR.CastRange && Utils.SleepCheck("R") && target != null && target.Health > (target.MaximumHealth * 0.5))
                    {
                        spellR.UseAbility(target);
                        Utils.Sleep(LifebreakCastTime + Game.Ping, "R");
                    }

                    if (Abyssal.CanBeCasted(target) && me.Distance2D(target) <= Abyssal.CastRange && Utils.SleepCheck("abyssal") && target != null)
                    {
                        var canUse = Utils.ChainStun(target, turnTime + 0.1 + Game.Ping / 1000, null, false);
                        if (canUse)
                        {
                            Abyssal.UseAbility(target);
                            Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "abyssal");
                        }
                    }

                    if (Hex.CanBeCasted(target) && me.Distance2D(target) <= (Hex.CastRange) && Utils.SleepCheck("hex") && target != null)
                    {
                        var canUse = Utils.ChainStun(target, turnTime + 0.1 + Game.Ping / 1000, null, false);
                        if (canUse)
                        {
                            Hex.UseAbility(target);
                            Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "hex");
                        }
                    }

                    if (Urn.CanBeCasted(target) && me.Distance2D(target) <= Urn.CastRange && Urn.CurrentCharges >= 1 && Utils.SleepCheck("Urn") && target != null)
                    {
                        Urn.UseAbility(target);
                        Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "Urn");
                    }

                    if (Medallion.CanBeCasted(target) && me.Distance2D(target) <= Medallion.CastRange &&
                        Utils.SleepCheck("Medallion") && target != null)
                    {
                        Medallion.UseAbility(target);
                        Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "Medallion");
                    }

                    if (SolarCrest.CanBeCasted(target) && me.Distance2D(target) <= SolarCrest.CastRange &&
                        Utils.SleepCheck("SolarCrest") && target != null)
                    {
                        SolarCrest.UseAbility(target);
                        Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "SolarCrest");
                    }

                    if (Blademail.CanBeCasted() && me.Distance2D(target) < target.AttackRange && Utils.SleepCheck("Blademail") && target != null)
                    {
                        Blademail.UseAbility();
                        Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "Blademail");
                    }

                    if (Mjollnir.CanBeCasted(me) && target.IsValid && me.Distance2D(target) <= spellR.CastRange && Utils.SleepCheck("Mjollnir") && target != null)
                    {
                        Mjollnir.UseAbility(me);
                        Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "Mjollnir");
                    }

                    if (Orchid.CanBeCasted(target) && me.Distance2D(target) <= Orchid.CastRange && Utils.SleepCheck("Orchid") && target != null)
                    {
                        Orchid.UseAbility(target);
                        Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "Orchid");
                    }

                    if (Halberd.CanBeCasted(target) && me.Distance2D(target) <= Halberd.CastRange &&
                        (!target.IsHexed() && !target.IsStunned() && !target.IsDisarmed()) && Utils.SleepCheck("Halberd") && target != null)
                    {
                        Halberd.UseAbility(target);
                        Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "Halberd");
                    }

                    if (Satanic.CanBeCasted() && me.Health <= (me.MaximumHealth * 0.30) && me.Distance2D(target) <= lastRange)
                    {
                        Satanic.UseAbility();
                    }

                    if (spellQ.CanBeCasted() && me.Health <= (me.MaximumHealth * 0.6) && Utils.SleepCheck("Q"))
                    {
                        spellQ.UseAbility(me);
                        Utils.Sleep(InnervitalityCastTime + Game.Ping, "Q");
                    }
                }

                Orbwalking.Orbwalk(target, attackmodifiers: true);
            }
        }
    }
}
