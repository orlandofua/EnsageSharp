using System;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Windows.Input;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using SharpDX;

namespace HuskarSharp
{
    internal class Program
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
        private static Item Armlet;
        //private static Item Aghs;

        #endregion

        private static double turnTime;
        private const double LifebreakCastTime = 800;
        private const double InnervitalityCastTime = 830;
        private static bool armletState;


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
            Armlet = null;

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

            // Items
            Urn = me.FindItem("item_urn_of_shadows");

            Abyssal = me.FindItem("item_abyssal_blade");

            Blademail = me.FindItem("item_blade_mail");

            Mjollnir = me.FindItem("item_mjollnir");

            Orchid = me.FindItem("item_orchid");

            Halberd = me.FindItem("item_heavens_halberd");

            Satanic = me.FindItem("item_satanic");

            Hex = me.FindItem("item_sheepstick");

            Medallion = me.FindItem("item_medallion_of_courage");

            SolarCrest = me.FindItem("item_solar_crest");

            Blink = me.FindItem("item_blink");

            Armlet = me.FindItem("item_armlet");


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
                if (Armlet != null && me.CanUseItems())
                {
                    var enemiesInRange =
                        ObjectMgr.GetEntities<Hero>()
                            .Where(x => me.Distance2D(x) <= lastRange && x.IsAlive && x.Team != me.Team).ToList();
                    var projectiles =
                        ObjectMgr.Projectiles.Where(
                            x =>
                                x.Target != null && x.Target == me && (me.Distance2D(x.Position)/x.Speed)*1000 < 600/2.5)
                            .ToList();

                    if (me.Health > 250 && enemiesInRange.Count > 0)
                    {
                        if (!me.IsStunned() && me.IsAlive && !Armlet.IsToggled &&
                            !me.Modifiers.Any(x => x.Name == "modifier_item_armlet_unholy_strength") &&
                            Utils.SleepCheck("Armlet"))
                        {
//                            Console.WriteLine("Activating Armlet!");
                            Armlet.ToggleAbility();
                            Utils.Sleep(100 + Game.Ping, "Armlet");
                        }
                    }
                    else if (me.Health > 250 && enemiesInRange.Count < 1 && !me.IsStunned() && Armlet.IsToggled &&
                             Utils.SleepCheck("Armlet") &&
                             me.Modifiers.Any(x => x.Name == "modifier_item_armlet_unholy_strength"))
                    {
//                        Console.WriteLine("Deactivating Armlet!");
                        Armlet.ToggleAbility();
                        Utils.Sleep(100 + Game.Ping, "Armlet");
                    }
                    else if (me.Health < 250 && projectiles.Any() && Armlet.IsToggled && !me.IsStunned() &&
                             Utils.SleepCheck("Armlet"))
                    {
//                        Console.WriteLine("Toggling armlet!");
                        Armlet.ToggleAbility();
                        Armlet.ToggleAbility();
                        Utils.Sleep(100 + Game.Ping, "Armlet");
                    }
                }

                if (target != null)
                {
                    turnTime = me.GetTurnTime(target);

                    if (Blink != null && Blink.CanBeCasted() && me.Distance2D(target) <= Blink.CastRange &&
                        Utils.SleepCheck("Blink") &&
                        target != null)
                    {
                        Blink.UseAbility(target.Position);
                        Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "Blink");
                    }

                    if (spellR.CanBeCasted(target) && me.Distance2D(target) <= spellR.CastRange && Utils.SleepCheck("R") &&
                        target != null && target.Health > (target.MaximumHealth*0.5) && !target.IsMagicImmune())
                    {
                        spellR.UseAbility(target);
                        Utils.Sleep(LifebreakCastTime + Game.Ping, "R");
                    }

                    if (Abyssal != null && Abyssal.CanBeCasted(target) && me.Distance2D(target) <= Abyssal.CastRange &&
                        Utils.SleepCheck("abyssal") && target != null)
                    {
                        var canUse = Utils.ChainStun(target, turnTime + 0.1 + Game.Ping/1000, null, false);
                        if (canUse)
                        {
                            Abyssal.UseAbility(target);
                            Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "abyssal");
                        }
                    }

                    if (Hex != null && Hex.CanBeCasted(target) && me.Distance2D(target) <= (Hex.CastRange) &&
                        Utils.SleepCheck("hex") &&
                        target != null)
                    {
                        var canUse = Utils.ChainStun(target, turnTime + 0.1 + Game.Ping/1000, null, false);
                        if (canUse)
                        {
                            Hex.UseAbility(target);
                            Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "hex");
                        }
                    }

                    if (Urn != null && Urn.CanBeCasted(target) && me.Distance2D(target) <= Urn.CastRange &&
                        Urn.CurrentCharges >= 1 &&
                        Utils.SleepCheck("Urn") && target != null &&
                        target.Modifiers.All(x => x.Name != "modifier_item_urn_heal"))
                    {
                        Urn.UseAbility(target);
                        Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "Urn");
                    }

                    if (Medallion != null && Medallion.CanBeCasted(target) &&
                        me.Distance2D(target) <= Medallion.CastRange &&
                        Utils.SleepCheck("Medallion") && target != null)
                    {
                        Medallion.UseAbility(target);
                        Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "Medallion");
                    }

                    if (SolarCrest != null && SolarCrest.CanBeCasted(target) &&
                        me.Distance2D(target) <= SolarCrest.CastRange &&
                        Utils.SleepCheck("SolarCrest") && target != null)
                    {
                        SolarCrest.UseAbility(target);
                        Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "SolarCrest");
                    }

                    if (Blademail != null && Blademail.CanBeCasted() && me.Distance2D(target) < target.AttackRange &&
                        Utils.SleepCheck("Blademail") && target != null)
                    {
                        Blademail.UseAbility();
                        Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "Blademail");
                    }

                    if (Mjollnir != null && Mjollnir.CanBeCasted(me) && target.IsValid &&
                        me.Distance2D(target) <= spellR.CastRange &&
                        Utils.SleepCheck("Mjollnir") && target != null)
                    {
                        Mjollnir.UseAbility(me);
                        Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "Mjollnir");
                    }

                    if (Orchid != null && Orchid.CanBeCasted(target) && me.Distance2D(target) <= Orchid.CastRange &&
                        Utils.SleepCheck("Orchid") && target != null)
                    {
                        Orchid.UseAbility(target);
                        Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "Orchid");
                    }

                    if (Halberd != null && Halberd.CanBeCasted(target) && me.Distance2D(target) <= Halberd.CastRange &&
                        (!target.IsHexed() && !target.IsStunned() && !target.IsDisarmed()) &&
                        Utils.SleepCheck("Halberd") && target != null)
                    {
                        Halberd.UseAbility(target);
                        Utils.Sleep(turnTime*1000 + 100 + Game.Ping, "Halberd");
                    }

                    if (Satanic != null && Satanic.CanBeCasted() && me.Health <= (me.MaximumHealth*0.20) &&
                        me.Distance2D(target) <= lastRange)
                    {
                        Satanic.UseAbility();
                    }

                    if (spellQ.CanBeCasted() && me.Health <= (me.MaximumHealth*0.4) && Utils.SleepCheck("Q"))
                    {
                        spellQ.UseAbility(me);
                        Utils.Sleep(InnervitalityCastTime + Game.Ping, "Q");
                    }
                }
                if (target != null && target.IsMagicImmune())
                {
                    Orbwalking.Orbwalk(target, Game.Ping);
                }
                else if (target == null || !target.IsMagicImmune())
                {
                    Orbwalking.Orbwalk(target, Game.Ping, attackmodifiers: true);
                }
            }
        }
    }
}