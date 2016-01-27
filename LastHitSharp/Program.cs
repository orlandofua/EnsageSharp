using System;
using System.Linq;
using Ensage;
using Ensage.Common;
using Ensage.Common.Menu;
using Ensage.Common.Extensions;

namespace LastHitSharp
{
    internal class Program
    {
        private static bool Jinada;
        private static bool TideBringer;
        private static Hero me;
        private static bool loaded;
        private static long attackRange;
        private static double aPoint;
        private static uint aRange;
        private static int bonus;
        private static int buffer;
        private static string toggleText;
        private static Entity target;
        private static double damage;
        private static Item qblade;
        private static Item bfury;

        private static Menu lasthitMenu;
        private static bool active;

        private static void CreateMenu()
        {
            lasthitMenu = new Menu("LastHitSharp!", "lastHitSharp", true);
            lasthitMenu.AddItem(new MenuItem("toggleKey", "Enabled").SetValue(new KeyBind('T', KeyBindType.Toggle, true)));
            active = lasthitMenu.Item("toggleKey").GetValue<bool>();
        }

        private static void Main(string[] args)
        {
            Game.OnUpdate += Game_OnUpdate;
            Player.OnExecuteOrder += Player_OnExecuteOrder;

            CreateMenu();
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!loaded)
            {
                me = ObjectMgr.LocalHero;

                if (!Game.IsInGame || Game.IsWatchingGame || me == null || Game.IsChatOpen)
                {
                    return;
                }

                loaded = true;
            }

            if (me == null || !me.IsValid)
            {
                loaded = false;
                me = ObjectMgr.LocalHero;
            }

            if (Game.IsPaused) return;

            aPoint = ((UnitDatabase.GetAttackPoint(me)*100)/(1 + me.AttackSpeedValue))*1000;
            aRange = me.AttackRange;
            bonus = 0;
            buffer = 0;

            if (me.ClassID == ClassID.CDOTA_Unit_Hero_Sniper)
            {
                var takeAim = me.Spellbook.SpellE;
                var aimRange = new[] {100, 200, 300, 400};

                if (takeAim.AbilityState != AbilityState.NotLearned && takeAim.Level > 0)
                {
                    bonus = aimRange[takeAim.Level - 1];
                }
            }

            if (me.ClassID == ClassID.CDOTA_Unit_Hero_TemplarAssassin)
            {
                var psyBlade = me.Spellbook.SpellE;
                var psyRange = new[] {60, 120, 180, 240};

                if (psyBlade.AbilityState != AbilityState.NotLearned && psyBlade.Level > 0)
                {
                    bonus = psyRange[psyBlade.Level - 1];
                }
            }

            if (me.ClassID == ClassID.CDOTA_Unit_Hero_Kunkka)
            {
                var Tide = me.Spellbook.SpellW;

                if (Tide.AbilityState != AbilityState.NotLearned && Tide.Level > 0)
                {
                    if (Tide.Cooldown == 0)
                    {
                        TideBringer = true;
                    }
                    else
                    {
                        TideBringer = false;
                    }
                }
            }

            attackRange = aRange + bonus;

            if (active && target != null)
            {
                if ((target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane ||
                     target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege ||
                     target.ClassID == ClassID.CDOTA_BaseNPC_Tower) &&
                    (target.IsAlive && target.IsVisible && target != null))
                {
                    if (qblade != null &&
                        (!(bfury != null || TideBringer || target.ClassID == ClassID.CDOTA_BaseNPC_Tower ||
                           target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege)))
                    {
                        if (attackRange > 195)
                        {
                            damage = (me.MinimumDamage*1.15 + me.BonusDamage);
                        }
                        else
                        {
                            damage = (me.MinimumDamage*1.40 + me.BonusDamage);
                        }
                    }

                    if (bfury != null &&
                        !(TideBringer || target.ClassID == ClassID.CDOTA_BaseNPC_Tower ||
                          target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege))
                    {
                        if (attackRange > 195)
                        {
                            damage = (me.MinimumDamage*1.25 + me.BonusDamage);
                        }
                        else
                        {
                            damage = (me.MinimumDamage*1.60 + me.BonusDamage);
                        }
                    }

                    if (me.ClassID == ClassID.CDOTA_Unit_Hero_AntiMage)
                    {
                        var Manabreak = me.Spellbook.SpellQ;
                        var Manaburn = new[] {28, 40, 52, 64};

                        if (Manabreak.AbilityState != AbilityState.NotLearned && Manabreak.Level > 0 &&
                            ((Unit) target).MaximumMana > 0 && ((Unit) target).Mana > 0 &&
                            target.Team != me.Team)
                        {
                            damage = (damage + Manaburn[Manabreak.Level - 1]*0.60);
                        }
                    }


                    if (me.ClassID == ClassID.CDOTA_Unit_Hero_Viper)
                    {
                        var Nethertoxin = me.Spellbook.SpellW;
                        var ToxinDamage = new[] {2.2, 4.7, 7.2, 9.7};

                        if (Nethertoxin.AbilityState != AbilityState.NotLearned && Nethertoxin.Level > 0 &&
                            target.Team != me.Team)
                        {
                            var HPcent = (target.Health/target.MaximumHealth)*100;
                            double Netherdamage = 0;
                            if (HPcent > 80 && HPcent <= 100)
                            {
                                Netherdamage = ToxinDamage[Nethertoxin.Level - 1]*0.5;
                            }
                            else if (HPcent > 60 && HPcent <= 80)
                            {
                                Netherdamage = ToxinDamage[Nethertoxin.Level - 1];
                            }
                            else if (HPcent > 40 && HPcent <= 60)
                            {
                                Netherdamage = ToxinDamage[Nethertoxin.Level - 1]*2;
                            }
                            else if (HPcent > 20 && HPcent <= 40)
                            {
                                Netherdamage = ToxinDamage[Nethertoxin.Level - 1]*4;
                            }
                            else if (HPcent > 0 && HPcent <= 20)
                            {
                                Netherdamage = ToxinDamage[Nethertoxin.Level - 1]*8;
                            }
                            damage = (float) (damage + Netherdamage);
                        }
                    }

                    if (me.ClassID == ClassID.CDOTA_Unit_Hero_Ursa &&
                        !(target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege ||
                          target.ClassID == ClassID.CDOTA_BaseNPC_Tower))
                    {
                        var Furyswipes = me.Spellbook.SpellE;
                        var Furybuff =
                            ((Unit) target).Modifiers.Where(
                                x => x.Name == "modifier_ursa_fury_swipes_damage_increase").ToList();
                        var Furydamage = new[] {15, 20, 25, 30};

                        if (Furyswipes.Level > 0 && target.Team != me.Team)
                        {
                            if (Furybuff.Any())
                            {
                                damage = damage + Furydamage[Furyswipes.Level - 1]*(Furybuff.Count);
                            }
                            else
                            {
                                damage = damage + Furydamage[Furyswipes.Level - 1];
                            }
                        }
                    }

                    if (me.ClassID == ClassID.CDOTA_Unit_Hero_BountyHunter &&
                        !(target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege ||
                          target.ClassID == ClassID.CDOTA_BaseNPC_Tower))
                    {
                        var jinada = me.Spellbook.SpellW;
                        var jinadaDamage = new[] {1.5, 1.75, 2, 2.25};

                        if (jinada.AbilityState != AbilityState.NotLearned && jinada.Level > 0 &&
                            jinada.Cooldown == 0 && target.Team != me.Team)
                        {
                            damage = (float) (damage*(jinadaDamage[jinada.Level - 1]));
                        }
                    }

                    if (me.ClassID == ClassID.CDOTA_Unit_Hero_Weaver)
                    {
                        var gem = me.Spellbook.SpellE;

                        if (gem.AbilityState != AbilityState.NotLearned && gem.Level > 0 && gem.Cooldown == 0)
                        {
                            damage = (float) (damage*1.8);
                        }
                    }

                    if ((me.ClassID == ClassID.CDOTA_Unit_Hero_SkeletonKing ||
                         me.ClassID == ClassID.CDOTA_Unit_Hero_ChaosKnight) &&
                        me.NetworkActivity == NetworkActivity.Crit &&
                        target.ClassID != ClassID.CDOTA_BaseNPC_Tower)
                    {
                        var critabil = me.Spellbook.SpellE;
                        var critmult = new[] {1.5, 2, 2.5, 3};

                        if (critabil.AbilityState != AbilityState.NotLearned && critabil.Level > 0 &&
                            target.Team != me.Team)
                        {
                            damage = (float) (damage*(critmult[critabil.Level - 1]));
                        }
                    }

                    if ((me.ClassID == ClassID.CDOTA_Unit_Hero_Juggernaut ||
                         me.ClassID == ClassID.CDOTA_Unit_Hero_Brewmaster) &&
                        me.NetworkActivity == NetworkActivity.Crit &&
                        target.ClassID != ClassID.CDOTA_BaseNPC_Tower)
                    {
                        var jugcrit = me.Spellbook.SpellE;

                        if (jugcrit.AbilityState != AbilityState.NotLearned && jugcrit.Level > 0 &&
                            target.Team != me.Team)
                        {
                            damage = damage*2;
                        }
                    }

                    if (me.ClassID == ClassID.CDOTA_Unit_Hero_PhantomAssassin &&
                        me.NetworkActivity == NetworkActivity.Crit &&
                        target.ClassID != ClassID.CDOTA_BaseNPC_Tower)
                    {
                        var pacrit = me.Spellbook.SpellR;
                        var pamod = new[] {2.3, 3.4, 4.5};

                        if (pacrit.AbilityState != AbilityState.NotLearned && pacrit.Level > 0 &&
                            target.Team != me.Team)
                        {
                            damage = (float) (damage*(pamod[pacrit.Level - 1]));
                        }
                    }

                    if (me.ClassID == ClassID.CDOTA_Unit_Hero_Riki &&
                        (target.ClassID != ClassID.CDOTA_BaseNPC_Creep_Siege ||
                         target.ClassID != ClassID.CDOTA_BaseNPC_Tower))
                    {
                        if ((me.Rotation + 180 > target.Rotation + 180 - (220/2) &&
                             me.Rotation + 180 < target.Rotation + 180 + (220/2)) &&
                            me.Spellbook.SpellE.Level > 0)
                        {
                            damage = (float) (damage + me.TotalAgility*(me.Spellbook.SpellE.Level*0.25 + 0.25));
                        }
                    }

                    if (target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege ||
                        target.ClassID == ClassID.CDOTA_BaseNPC_Tower)
                    {
                        damage = damage/2;
                    }

                    if (target.Team == me.Team && qblade != null &&
                        target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane)
                    {
                        damage = me.MinimumDamage + me.BonusDamage;
                    }

                    if ((((target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane ||
                           target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege) &&
                          me.Distance2D(target) <= attackRange + 100) ||
                         (target.ClassID == ClassID.CDOTA_BaseNPC_Tower &&
                          me.Distance2D(target) <= attackRange + 300)) &&
                        (target.Health >
                         ((Unit) target).DamageTaken((float) damage, DamageType.Physical, me)))
                    {
                        if (Utils.SleepCheck("stop"))
                        {
                            if (me.ClassID == ClassID.CDOTA_Unit_Hero_Bristleback)
                            {
                                Utils.Sleep(aPoint*0.80 + Game.Ping, "stop");
                            }
                            else
                            {
                                Utils.Sleep(aPoint + Game.Ping, "stop");
                            }
                            me.Hold();
                            me.Attack((Unit) target);
                        }
                    }
                    else if (target.Health <
                             ((Unit) target).DamageTaken((float) damage, DamageType.Physical, me) &&
                             Utils.SleepCheck("StopIt"))
                    {
                        me.Attack((Unit) target);
                        Utils.Sleep(250, "StopIt");
                    }
                }
            }
        }

        private static void Player_OnExecuteOrder(Player sender, ExecuteOrderEventArgs args)
        {
            if (active && (args.Order == Order.MoveLocation || args.Order == Order.Stop || args.Target == null))
            {
                target = null;
                return;
            }

            if (active && !Game.IsPaused && !Game.IsChatOpen && Equals(sender, me.Player))
            {
                damage = me.MinimumDamage + me.BonusDamage;
                qblade = me.FindItem("item_quelling_blade");
                bfury = me.FindItem("item_bfury");

                if (args.Order == Order.MoveLocation || (target != null && !target.IsAlive))
                {
                    target = null;
                    return;
                }

                if (args.Order == Order.AttackTarget && me.IsAlive && args.Target != null)
                {
                    target = args.Target;
                }
            }
        }
    }
}