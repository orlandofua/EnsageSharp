using System;
using System.Linq;
using System.Windows.Input;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using SharpDX;

namespace LastHitSharp
{
    internal class Program
    {
        private const Key toggleKey = Key.G;
        private static bool active = true;
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

        private static void Main(string[] args)
        {
            Game.OnUpdate += Game_OnUpdate;
            Player.OnExecuteOrder += Player_OnExecuteOrder;
            Drawing.OnDraw += Drawing_OnDraw;
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
                active = false;
            }

            if (Game.IsPaused) return;

            if (Game.IsKeyDown(toggleKey) && Utils.SleepCheck("toggle"))
            {
                if (!active)
                {
                    active = true;
                    toggleText = "(" + toggleKey + ") Last Hit: On";
                }
                else
                {
                    active = false;
                    toggleText = "(" + toggleKey + ") Last Hit: Off";
                }

                Utils.Sleep(200, "toggle");
            }

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
        }

        private static void Player_OnExecuteOrder(Player sender, ExecuteOrderEventArgs args)
        {
            if (active && !Game.IsChatOpen && sender == me.Player)
            {
                double damage = me.MinimumDamage + me.BonusDamage;
                var megaplayer = me.Player;
                var qblade = me.FindItem("item_quelling_blade");
                var bfury = me.FindItem("item_bfury");

                if (args.Order == Order.AttackTarget && megaplayer.IsAlive)
                {
                    if (args.Target == null) return;

                    if (args.Target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane ||
                        args.Target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege ||
                        args.Target.ClassID == ClassID.CDOTA_BaseNPC_Tower &&
                        (args.Target.IsAlive && args.Target.IsVisible && args.Target != null))
                    {
                        if (qblade != null &&
                            !(bfury != null || TideBringer || args.Target.ClassID == ClassID.CDOTA_BaseNPC_Tower ||
                              args.Target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege))
                        {
                            if (attackRange > 195)
                            {
                                damage = me.MinimumDamage*1.15 + me.BonusDamage;
                            }
                            else
                            {
                                damage = me.MinimumDamage*1.40 + me.BonusDamage;
                            }
                        }

                        if (bfury != null &&
                            !(TideBringer || args.Target.ClassID == ClassID.CDOTA_BaseNPC_Tower ||
                              args.Target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege))
                        {
                            if (attackRange > 195)
                            {
                                damage = me.MinimumDamage*1.25 + me.BonusDamage;
                            }
                            else
                            {
                                damage = me.MinimumDamage*1.60 + me.BonusDamage;
                            }
                        }

                        if (me.ClassID == ClassID.CDOTA_Unit_Hero_AntiMage)
                        {
                            var Manabreak = me.Spellbook.SpellQ;
                            var Manaburn = new[] {28, 40, 52, 64};

                            if (Manabreak.AbilityState != AbilityState.NotLearned && Manabreak.Level > 0 &&
                                ((Unit) args.Target).MaximumMana > 0 && ((Unit) args.Target).Mana > 0 &&
                                args.Target.Team != me.Team)
                            {
                                damage = damage + Manaburn[Manabreak.Level - 1]*0.60;
                            }
                        }

                        if (me.ClassID == ClassID.CDOTA_Unit_Hero_Viper)
                        {
                            var Nethertoxin = me.Spellbook.SpellW;
                            var ToxinDamage = new[] {2.2, 4.7, 7.2, 9.7};

                            if (Nethertoxin.AbilityState != AbilityState.NotLearned && Nethertoxin.Level > 0 &&
                                args.Target.Team != me.Team)
                            {
                                var HPcent = (args.Target.Health/args.Target.MaximumHealth)*100;
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
                                if (Netherdamage != null)
                                {
                                    damage = damage + Netherdamage;
                                }
                            }
                        }

                        if (me.ClassID == ClassID.CDOTA_Unit_Hero_Ursa &&
                            !(args.Target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege ||
                              args.Target.ClassID == ClassID.CDOTA_BaseNPC_Tower))
                        {
                            var Furyswipes = me.Spellbook.SpellE;
                            var Furybuff =
                                ((Unit) args.Target).Modifiers.Where(
                                    x => x.Name == "modifier_ursa_fury_swipes_damage_increase").ToList();
                            var Furydamage = new[] {15, 20, 25, 30};

                            if (Furyswipes.Level > 0 && args.Target.Team != me.Team)
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
                            !(args.Target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege ||
                              args.Target.ClassID == ClassID.CDOTA_BaseNPC_Tower))
                        {
                            var jinada = me.Spellbook.SpellW;
                            var jinadaDamage = new[] {1.5, 1.75, 2, 2.25};

                            if (jinada.AbilityState != AbilityState.NotLearned && jinada.Level > 0 &&
                                jinada.Cooldown == 0 && args.Target.Team != me.Team)
                            {
                                damage = damage*(jinadaDamage[jinada.Level - 1]);
                            }
                        }

                        if (me.ClassID == ClassID.CDOTA_Unit_Hero_Weaver)
                        {
                            var gem = me.Spellbook.SpellE;

                            if (gem.AbilityState != AbilityState.NotLearned && gem.Level > 0 && gem.Cooldown == 0)
                            {
                                damage = damage*1.8;
                            }
                        }

                        if ((me.ClassID == ClassID.CDOTA_Unit_Hero_SkeletonKing ||
                             me.ClassID == ClassID.CDOTA_Unit_Hero_ChaosKnight) &&
                            me.NetworkActivity == NetworkActivity.Crit &&
                            args.Target.ClassID != ClassID.CDOTA_BaseNPC_Tower)
                        {
                            var critabil = me.Spellbook.SpellE;
                            var critmult = new[] {1.5, 2, 2.5, 3};

                            if (critabil.AbilityState != AbilityState.NotLearned && critabil.Level > 0 &&
                                args.Target.Team != me.Team)
                            {
                                damage = damage*(critmult[critabil.Level - 1]);
                            }
                        }

                        if ((me.ClassID == ClassID.CDOTA_Unit_Hero_Juggernaut ||
                             me.ClassID == ClassID.CDOTA_Unit_Hero_Brewmaster) &&
                            me.NetworkActivity == NetworkActivity.Crit &&
                            args.Target.ClassID != ClassID.CDOTA_BaseNPC_Tower)
                        {
                            var jugcrit = me.Spellbook.SpellE;

                            if (jugcrit.AbilityState != AbilityState.NotLearned && jugcrit.Level > 0 &&
                                args.Target.Team != me.Team)
                            {
                                damage = damage*2;
                            }
                        }

                        if (me.ClassID == ClassID.CDOTA_Unit_Hero_PhantomAssassin &&
                            me.NetworkActivity == NetworkActivity.Crit &&
                            args.Target.ClassID != ClassID.CDOTA_BaseNPC_Tower)
                        {
                            var pacrit = me.Spellbook.SpellR;
                            var pamod = new[] {2.3, 3.4, 4.5};

                            if (pacrit.AbilityState != AbilityState.NotLearned && pacrit.Level > 0 &&
                                args.Target.Team != me.Team)
                            {
                                damage = damage*(pamod[pacrit.Level - 1]);
                            }
                        }

                        if (me.ClassID == ClassID.CDOTA_Unit_Hero_Riki &&
                            !(args.Target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege ||
                              args.Target.ClassID == ClassID.CDOTA_BaseNPC_Tower))
                        {
                            if ((me.Rotation + 180 > args.Target.Rotation + 180 - (220/2) &&
                                 me.Rotation + 180 < args.Target.Rotation + 180 + (220/2)) &&
                                me.Spellbook.SpellE.Level > 0)
                            {
                                damage = damage + me.TotalAgility*(me.Spellbook.SpellE.Level*0.25 + 0.25);
                            }
                        }

                        if (args.Target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege ||
                            args.Target.ClassID == ClassID.CDOTA_BaseNPC_Tower)
                        {
                            damage = damage*0.5;
                        }

                        if (args.Target.Team == me.Team && qblade != null &&
                            args.Target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane)
                        {
                            damage = me.MinimumDamage + me.BonusDamage;
                        }

                        toggleText = "(" + toggleKey + ") Last Hit: ON | Target HP = " + args.Target.Health +
                                     "| Damage = " +
                                     ((Unit) args.Target).DamageTaken((float) damage, DamageType.Physical, me) + "";

                        if ((((args.Target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane ||
                               args.Target.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege) &&
                              me.Distance2D(args.Target) <= attackRange + 100) ||
                             (args.Target.ClassID == ClassID.CDOTA_BaseNPC_Tower &&
                              me.Distance2D(args.Target) <= attackRange + 300)) &&
                            (args.Target.Health >
                             ((Unit) args.Target).DamageTaken((float) damage, DamageType.Physical, me)))
                        {
                            if (Utils.SleepCheck("stop"))
                            {
                                if (me.ClassID == ClassID.CDOTA_Unit_Hero_Bristleback)
                                {
                                    Utils.Sleep(aPoint*0.80, "stop");
                                }
                                else
                                {
                                    Utils.Sleep(aPoint, "stop");
                                }
                                me.Hold();
                                me.Attack((Unit) args.Target);
                            }
                            else if (args.Target.Health <
                                     ((Unit) args.Target).DamageTaken((float) damage, DamageType.Physical, me) &&
                                     Utils.SleepCheck("StopIt"))
                            {
                                me.Attack((Unit) args.Target);
                                Utils.Sleep(250, "StopIt");
                            }
                        }
                    }
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            Drawing.DrawText(toggleText,
                new Vector2(Drawing.Width*5/100, Drawing.Height*20/100), Color.LightGreen, FontFlags.DropShadow);
        }
    }
}