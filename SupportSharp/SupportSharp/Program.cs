using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using SharpDX;

namespace SupportSharp
{
    internal class Program
    {
        private const Key toggleKey = Key.T;
        private const Key orbwalkKey = Key.X;
        private const Key saveSelfKey = Key.Y;
        private static Hero me;
        private static Entity fountain;
        private static bool loaded;

        private static Item Urn,
            Meka,
            Guardian,
            Arcane,
            LotusOrb,
            Medallion,
            SolarCrest,
            GlimmerCape,
            Pipe,
            CrimsonGuard;

        private static Hero needMana;
        private static Hero needMeka;
        private static Hero target;
        private static bool supportActive;
        private static bool includeSaveSelf;
        private static bool shouldCastLotusOrb;
        private static bool shouldCastGlimmerCape;

        private static void Main(string[] args)
        {
            Game.OnUpdate += Game_OnUpdate;
            Orbwalking.Load();
            Drawing.OnDraw += Drawing_OnDraw;

            /*Items*/
            Urn = null;
            Meka = null;
            Guardian = null;
            Arcane = null;
            LotusOrb = null;
            Medallion = null;
            SolarCrest = null;
            GlimmerCape = null;
            Pipe = null;
            CrimsonGuard = null;

            loaded = false;
            supportActive = true;
            includeSaveSelf = false;
            shouldCastLotusOrb = false;
            shouldCastGlimmerCape = false;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (loaded)
            {
                var mode = supportActive ? "ON" : "OFF";
                var orbwalkMode = Game.IsKeyDown(orbwalkKey) ? "ON" : "OFF";
                var includeSelfMode = includeSaveSelf ? "ON" : "OFF";
                Drawing.DrawText("Auto Support is: " + mode + ". Hotkey (Toggle): " + toggleKey + "",
                    new Vector2(Drawing.Width*5/100, Drawing.Height*4/100), Color.LightGreen, FontFlags.DropShadow);
                Drawing.DrawText("Orbwalk is: " + orbwalkMode + ". Hotkey (HOLD): " + orbwalkKey + "",
                    new Vector2(Drawing.Width*5/100, Drawing.Height*6/100), Color.LightGreen, FontFlags.DropShadow);
                Drawing.DrawText(
                    "Include Saving yourself?: " + includeSelfMode + ". Hotkey (TOGGLE): " + saveSelfKey + "",
                    new Vector2(Drawing.Width*5/100, Drawing.Height*8/100), Color.LightGreen, FontFlags.DropShadow);
            }
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
                supportActive = false;
                includeSaveSelf = false;
                shouldCastLotusOrb = false;
                shouldCastGlimmerCape = false;
                return;
            }

            if (Game.IsPaused)
            {
                return;
            }

            Urn = me.FindItem("item_urn_of_shadows");
            Meka = me.FindItem("item_mekansm");
            Guardian = me.FindItem("item_guardian_greaves");
            Arcane = me.FindItem("item_arcane_boots");
            LotusOrb = me.FindItem("item_lotus_orb");
            Medallion = me.FindItem("item_medallion_of_courage");
            SolarCrest = me.FindItem("item_solar_crest");
            GlimmerCape = me.FindItem("item_glimmer_cape");
            Pipe = me.FindItem("item_pipe");
            CrimsonGuard = me.FindItem("item_crimson_guard");

            needMana = null;
            needMeka = null;
            shouldCastLotusOrb = false;
            shouldCastGlimmerCape = false;

            if (!Game.IsChatOpen)
            {
                if (Game.IsKeyDown(toggleKey) && Utils.SleepCheck("togglingoption"))
                {
                    if (!supportActive)
                    {
                        supportActive = true;
                    }
                    else
                    {
                        supportActive = false;
                    }
                    Utils.Sleep(100 + Game.Ping, "togglingoption");
                }

                if (Game.IsKeyDown(saveSelfKey) && Utils.SleepCheck("togglingoption"))
                {
                    if (!includeSaveSelf)
                    {
                        includeSaveSelf = true;
                    }
                    else
                    {
                        includeSaveSelf = false;
                    }
                    Utils.Sleep(100 + Game.Ping, "togglingoption");
                }
            }

            if (supportActive)
            {
                var allies =
                    ObjectMgr.GetEntities<Hero>()
                        .Where(
                            ally =>
                                ally.Team == me.Team && ally.IsAlive && !ally.IsIllusion && me.Distance2D(ally) <= 1500)
                        .ToList();
                fountain =
                    ObjectMgr.GetEntities<Entity>()
                        .First(entity => entity.ClassID == ClassID.CDOTA_Unit_Fountain && entity.Team == me.Team);


                if (allies.Any())
                {
                    foreach (var ally in allies)
                    {
                        if (!ally.IsIllusion() && ally.IsAlive && ally.Health > 0 && me.IsAlive && !me.IsChanneling() &&
                            me.Distance2D(fountain) > 2000 &&
                            !me.IsInvisible())
                        {
                            if ((ally.MaximumHealth - ally.Health) > (450 + ally.HealthRegeneration*10) &&
                                me.Distance2D(ally) <= 2000 &&
                                (me.Mana >= 225 || Guardian != null))
                            {
                                if (needMeka == null || (needMeka != null && me.Distance2D(needMeka) <= 750))
                                {
                                    needMeka = ally;
                                }
                            }

                            var enemyTowers =
                                ObjectMgr.GetEntities<Entity>()
                                    .Any(
                                        x =>
                                            x.ClassID == ClassID.CDOTA_BaseNPC_Tower && x.Team != me.Team &&
                                            x.IsAlive && ally.Distance2D(x) <= 750);

                            if (me.CanUseItems())
                            {
                                if (Urn != null && Urn.CanBeCasted() && Urn.CurrentCharges > 0 &&
                                    !ally.Modifiers.Any(x => x.Name == "modifier_item_urn_heal") && !enemyTowers)
                                {
                                    if (me.Distance2D(ally) <= 950 && !IsInDanger(ally) && Utils.SleepCheck("Urn") &&
                                        ally.Health <= (ally.MaximumHealth*0.7))
                                    {
                                        Urn.UseAbility(ally);
                                        Utils.Sleep(100 + Game.Ping, "Urn");
                                    }
                                    if (ally.Modifiers.Any(x => x.Name == "modifier_wisp_tether") &&
                                        (ally.MaximumHealth - ally.Health) >= 600 && Utils.SleepCheck("Urn"))
                                    {
                                        Urn.UseAbility(me);
                                        Utils.Sleep(100 + Game.Ping, "Urn");
                                    }
                                }

                                if (Arcane != null && Arcane.Cooldown == 0)
                                {
                                    if ((ally.MaximumMana - ally.Mana) >= 135 && me.Distance2D(ally) < 2000 &&
                                        me.Mana >= 35)
                                    {
                                        if (needMana == null || (needMana != null && me.Distance2D(needMana) <= 600))
                                        {
                                            needMana = ally;
                                        }
                                    }
                                }

                                /*Pipe and Crimson Guard*/
                                if (((Pipe != null && Pipe.CanBeCasted()) ||
                                     (CrimsonGuard != null && CrimsonGuard.CanBeCasted())) && me.CanUseItems())
                                {
                                    var enemiesInRadius =
                                        ObjectMgr.GetEntities<Hero>()
                                            .Where(
                                                x =>
                                                    x.Team != me.Team && x.IsAlive && me.Distance2D(x) <= 1500 &&
                                                    !x.IsIllusion).ToList();
                                    var alliesInRadius =
                                        ObjectMgr.GetEntities<Hero>()
                                            .Where(
                                                x =>
                                                    x.Team == me.Team && x.IsAlive && me.Distance2D(x) <= 900 &&
                                                    !x.IsIllusion).ToList();

                                    if (enemiesInRadius.Any() && alliesInRadius.Any())
                                    {
                                        if (enemiesInRadius.Count >= 2 && alliesInRadius.Count >= 2)
                                        {
                                            if (Pipe != null && Pipe.CanBeCasted() && Utils.SleepCheck("Pipe"))
                                            {
                                                Pipe.UseAbility();
                                                Utils.Sleep(100 + Game.Ping, "Pipe");
                                            }

                                            if (CrimsonGuard != null && CrimsonGuard.CanBeCasted() &&
                                                Utils.SleepCheck("CrimsonGuard"))
                                            {
                                                CrimsonGuard.UseAbility();
                                                Utils.Sleep(100 + Game.Ping, "CrimsonGuard");
                                            }
                                        }
                                    }
                                }

                                var enemyList =
                                    ObjectMgr.GetEntities<Hero>()
                                        .Where(
                                            x =>
                                                x.Team != me.Team && !x.IsIllusion && x.IsAlive && x.CanCast() &&
                                                ally.Distance2D(x) <= 1000)
                                        .ToList();

                                if (enemyList.Any())
                                {
                                    foreach (var enemy in enemyList)
                                    {
                                        var targettedSpell =
                                            enemy.Spellbook.Spells.Any(
                                                x =>
                                                    x.TargetTeamType == TargetTeamType.Enemy &&
                                                    x.AbilityState == AbilityState.Ready &&
                                                    ally.Distance2D(enemy) <= x.CastRange + 50 &&
                                                    x.AbilityBehavior == AbilityBehavior.UnitTarget);

                                        var targettedItem =
                                            enemy.Inventory.Items.Any(
                                                x =>
                                                    x.TargetTeamType == TargetTeamType.Enemy &&
                                                    x.AbilityState == AbilityState.Ready &&
                                                    x.AbilityBehavior == AbilityBehavior.UnitTarget &&
                                                    ally.Distance2D(enemy) <= x.CastRange + 50);

                                        var enemySkill =
                                            enemy.Spellbook.Spells.Any(
                                                x =>
                                                    x.DamageType == DamageType.Magical &&
                                                    x.TargetTeamType == TargetTeamType.Enemy &&
                                                    x.AbilityState == AbilityState.Ready &&
                                                    ally.Distance2D(enemy) <= x.CastRange + 50);

                                        if (enemySkill)
                                        {
                                            shouldCastGlimmerCape = true;
                                        }

                                        if (targettedSpell || targettedItem)
                                        {
                                            shouldCastLotusOrb = true;
                                        }
                                    }
                                }

                                if (LotusOrb != null && LotusOrb.Cooldown == 0 && Utils.SleepCheck("LotusOrb") &&
                                    me.Distance2D(ally) <= LotusOrb.CastRange + 50 &&
                                    (shouldCastLotusOrb || IsInDanger(ally)))
                                {
                                    LotusOrb.UseAbility(ally);
                                    Utils.Sleep(100 + Game.Ping, "LotusOrb");
                                }

                                if (Medallion != null && Medallion.Cooldown == 0 &&
                                    me.Distance2D(ally) <= Medallion.CastRange + 50 && Utils.SleepCheck("Medallion") &&
                                    ally != me && IsInDanger(ally))
                                {
                                    Medallion.UseAbility(ally);
                                    Utils.Sleep(100 + Game.Ping, "Medallion");
                                }

                                if (SolarCrest != null && SolarCrest.Cooldown == 0 &&
                                    me.Distance2D(ally) <= SolarCrest.CastRange + 50 && Utils.SleepCheck("SolarCrest") &&
                                    ally != me && IsInDanger(ally))
                                {
                                    SolarCrest.UseAbility(ally);
                                    Utils.Sleep(100 + Game.Ping, "SolarCrest");
                                }

                                if (GlimmerCape != null && GlimmerCape.Cooldown == 0 &&
                                    me.Distance2D(ally) <= GlimmerCape.CastRange + 50 && Utils.SleepCheck("GlimmerCape") &&
                                    (shouldCastGlimmerCape || IsInDanger(ally)))
                                {
                                    GlimmerCape.UseAbility(ally);
                                    Utils.Sleep(100 + Game.Ping, "GlimmerCape");
                                }
                            }
                        }
                    }
                }

                if (needMeka != null &&
                    ((Guardian != null && Guardian.CanBeCasted()) || (Meka != null && Meka.CanBeCasted())) &&
                    me.Distance2D(needMeka) <= 750)
                {
                    if (Meka != null)
                    {
                        Meka.UseAbility();
                    }
                    else
                    {
                        Guardian.UseAbility();
                    }
                }
                if (needMana != null && Arcane != null && Arcane.CanBeCasted() && me.Distance2D(needMana) <= 600)
                {
                    Arcane.UseAbility();
                }


                if (Support(me.ClassID))
                {
                    switch (me.ClassID)
                    {
                        case ClassID.CDOTA_Unit_Hero_Abaddon:
                            Save(me, me.Spellbook.SpellW, 1000, me.Spellbook.SpellW.CastRange);
                            Heal(me, me.Spellbook.SpellQ, new float[] {100, 150, 200, 250},
                                800,
                                1);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Chen:
                            Save(me, me.Spellbook.SpellE, 1000, me.Spellbook.SpellE.CastRange);
                            Heal(me, me.Spellbook.SpellR, new float[] {200, 300, 400},
                                me.Spellbook.SpellR.CastRange, 2);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Dazzle:
                            Save(me, me.Spellbook.SpellW, 300, me.Spellbook.SpellW.CastRange);
                            Heal(me, me.Spellbook.SpellE, new float[] {80, 100, 120, 140},
                                750,
                                1);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Enchantress:
                            Heal(me, me.Spellbook.SpellE, new float[] {400, 600, 800, 1000},
                                275, 2);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Legion_Commander:
                            Heal(me, me.Spellbook.SpellW, new float[] {150, 200, 250, 300},
                                800,
                                1);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Necrolyte:
                            Heal(me, me.Spellbook.SpellQ, new float[] {70, 90, 110, 130},
                                475,
                                2);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Omniknight:
                            Heal(me, me.Spellbook.SpellQ, new float[] {90, 180, 270, 360},
                                950,
                                1);
                            Save(me, me.Spellbook.SpellW, 1570, me.Spellbook.SpellW.CastRange);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Oracle:
                            Save(me, me.Spellbook.SpellR, 1270, me.Spellbook.SpellR.CastRange);
                            Heal(me, me.Spellbook.SpellE, new float[] {99, 198, 297, 396},
                                750,
                                1);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Shadow_Demon:
                            Save(me, me.Spellbook.SpellQ, 900,
                                me.Spellbook.SpellQ.CastRange);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Treant:
                            Heal(me, me.Spellbook.SpellE, new float[] {60, 105, 150, 195},
                                me.Spellbook.SpellE.CastRange, 1);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Undying:
                            var unitsAround =
                                ObjectMgr.GetEntities<Entity>()
                                    .Where(entity => entity.IsAlive && me.Distance2D(entity) <= 1300).ToList();

                            if (unitsAround.Any())
                            {
                                var unitCount = unitsAround.Count;
                                var healperUnit = new[] {18, 22, 36, 30};

                                Heal(me, me.Spellbook.SpellW,
                                    new float[]
                                    {
                                        unitCount*healperUnit[me.Spellbook.SpellW.Level - 1],
                                        unitCount*healperUnit[me.Spellbook.SpellW.Level - 1],
                                        unitCount*healperUnit[me.Spellbook.SpellW.Level - 1],
                                        unitCount*healperUnit[me.Spellbook.SpellW.Level - 1]
                                    },
                                    750, 1);
                            }
                            break;
                        case ClassID.CDOTA_Unit_Hero_Warlock:
                            Heal(me, me.Spellbook.SpellW, new float[] {165, 275, 385, 495},
                                me.Spellbook.SpellW.CastRange, 1);
                            break;
                        case ClassID.CDOTA_Unit_Hero_Winter_Wyvern:
                            Save(me, me.Spellbook.SpellE, 930, me.Spellbook.SpellE.CastRange);
                            break;
                        case ClassID.CDOTA_Unit_Hero_WitchDoctor:
                            Heal(me, me.Spellbook.SpellW, new float[] {16, 24, 32, 40}, 500,
                                3);
                            break;
                    }
                }
            }

            if (Game.IsKeyDown(orbwalkKey))
            {
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

                Orbwalking.Orbwalk(target, Game.Ping, attackmodifiers: true);
            }
        }

        private static void Save(Hero self, Ability saveSpell, float castTime = 800, uint castRange = 0,
            int targettingType = 1)
        {
            if (saveSpell != null && saveSpell.CanBeCasted())
            {
                if (self.IsAlive && !self.IsChanneling() &&
                    (!self.IsInvisible() || !me.Modifiers.Any(x => x.Name == "modifier_treant_natures_guise")))
                {
                    var alliesExcludeMe =
                        ObjectMgr.GetEntities<Hero>()
                            .Where(
                                x =>
                                    x.Team == self.Team && self.Distance2D(x) <= castRange && IsInDanger(x) &&
                                    !Equals(x, self) && !x.IsIllusion && x.IsAlive)
                            .ToList();
                    var alliesIncludeMe =
                        ObjectMgr.GetEntities<Hero>()
                            .Where(
                                x =>
                                    x.Team == self.Team && self.Distance2D(x) <= castRange && IsInDanger(x) && x.IsAlive &&
                                    !x.IsIllusion && x.IsAlive)
                            .ToList();

                    if (includeSaveSelf)
                    {
                        if (alliesIncludeMe.Any())
                        {
                            foreach (var ally in alliesIncludeMe)
                            {
                                if (ally.Health <= (ally.MaximumHealth*0.3))
                                {
                                    if (targettingType == 1)
                                    {
                                        if (Utils.SleepCheck("saveduration"))
                                        {
                                            saveSpell.UseAbility(ally);
                                            Utils.Sleep(castTime + Game.Ping, "saveduration");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (alliesExcludeMe.Any())
                        {
                            foreach (var ally in alliesExcludeMe)
                            {
                                if (ally.Health <= (ally.MaximumHealth*0.3))
                                {
                                    if (targettingType == 1)
                                    {
                                        if (Utils.SleepCheck("saveduration"))
                                        {
                                            saveSpell.UseAbility(ally);
                                            Utils.Sleep(castTime + Game.Ping, "saveduration");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void Heal(Hero self, Ability healSpell, float[] amount, uint range, int targettingType)
        {
            if (healSpell != null && healSpell.CanBeCasted() && !self.IsChanneling())
            {
                if (self.IsAlive && !self.IsChanneling() &&
                    (!self.IsInvisible() || !me.Modifiers.Any(x => x.Name == "modifier_treant_natures_guise")) &&
                    self.Distance2D(fountain) > 2000)
                {
                    var heroes =
                        ObjectMgr.GetEntities<Hero>()
                            .Where(
                                entity =>
                                    entity.Team == self.Team && self.Distance2D(entity) <= range && !entity.IsIllusion &&
                                    entity.IsAlive)
                            .ToList();

                    if (heroes.Any())
                    {
                        foreach (var ally in heroes)
                        {
                            if (ally.Health <= (ally.MaximumHealth*0.7) && healSpell.CanBeCasted() &&
                                self.Distance2D(fountain) > 2000 && IsInDanger(ally) &&
                                ally.Health + amount[healSpell.Level - 1] <= ally.MaximumHealth)
                            {
                                if (targettingType == 1)
                                    CastHeal(healSpell, ally);
                                else if (targettingType == 2)
                                    CastHeal(healSpell);
                                else if (targettingType == 3)
                                {
                                    /*if (healSpell.CanBeCasted() && !healSpell.IsChanneling)
                                    {
                                        Console.Write("Casting Heal");
                                        CastHeal(healSpell);
                                    }
                                    else
                                    {
                                        if (ally.Health > (ally.MaximumHealth*0.7) && healSpell.IsChanneling)
                                        {
                                            Console.Write("Casting Heal");
                                            CastHeal(healSpell);
                                        }
                                    }*/

                                    if (healSpell.CanBeCasted() && Utils.SleepCheck("ToggleHeal"))
                                    {
                                        if (!healSpell.IsToggled)
                                        {
                                            CastHeal(healSpell);
                                            Utils.Sleep(100 + Game.Ping, "ToggleHeal");
                                        }
                                    }
                                }
                            }
                            else if (targettingType == 3 && ally.Health > (ally.MaximumHealth*0.7) && healSpell.IsToggled &&
                                     Utils.SleepCheck("ToggleHeal"))
                            {
                                CastHeal(healSpell);
                                Utils.Sleep(100 + Game.Ping, "ToggleHeal");
                            }
                        }
                    }
                }
            }
        }

        private static void CastHeal(Ability healSpell, Hero destination = null)
        {
            if (destination != null)
            {
                if (healSpell.CanBeCasted() && Utils.SleepCheck("Casting Heal"))
                {
                    healSpell.UseAbility(destination);
                    Utils.Sleep(100 + Game.Ping, "Casting Heal");
                }
            }
            else
            {
                if (Utils.SleepCheck("Casting Heal"))
                {
                    healSpell.UseAbility();
                    Utils.Sleep(healSpell.ChannelTime + Game.Ping, "Casting Heal");
                }
            }
        }

        private static bool IsInDanger(Hero ally)
        {
            if (ally != null && ally.IsAlive)
            {
                var percHealth = ally.Health <= (ally.MaximumHealth*0.7);
                var enemies =
                    ObjectMgr.GetEntities<Hero>()
                        .Where(
                            entity =>
                                entity.Team != ally.Team && entity.IsAlive && entity.IsVisible && !entity.IsIllusion)
                        .ToList();
                if (enemies.Any() && percHealth)
                {
                    foreach (var enemy in enemies)
                    {
                        if (ally.Distance2D(enemy) < enemy.AttackRange + 50)
                        {
                            return true;
                        }
                        if (enemy.Spellbook.Spells.Any(abilities => ally.Distance2D(enemy) < abilities.CastRange + 50))
                        {
                            return true;
                        }
                    }
                }

                var buffs = new[]
                {
                    "modifier_item_urn_damage", "modifier_doom_bringer_doom", "modifier_axe_battle_hunger",
                    "modifier_queenofpain_shadow_strike", "modifier_phoenix_fire_spirit_burn",
                    "modifier_venomancer_poison_nova", "modifier_venomancer_venomous_gale",
                    "modifier_silencer_curse_of_the_silent", "modifier_silencer_last_word", "modifier_bane_fiends_grip",
                    "modifier_earth_spirit_magnetize", "modifier_jakiro_macropyre", "modifier_nerolyte_reapers_scythe",
                    "modifier_batrider_flaming_lasso", "modifier_sniper_assassinate", "modifier_pudge_dismember",
                    "modifier_enigma_black_hole_pull", "modifier_disruptor_static_storm", "modifier_sand_king_epicenter",
                    "modifier_bloodseeker_rupture", "modifier_dual_breath_burn", "modifier_jakiro_liquid_fire_burn",
                    "modifier_axe_battle_hunger", "modifier_viper_corrosive_skin", "modifier_viper_poison_attack",
                    "modifier_viper_viper_strike"
                };

                foreach (var buff in buffs)
                {
                    return ally.Modifiers.Any(x => x.Name == buff);
                }

                if ((ally.IsStunned() ||
                     (ally.IsSilenced() &&
                      ((ally.FindItem("item_manta_style") == null || ally.FindItem("item_manta_style").Cooldown > 0) ||
                       (ally.FindItem("item_black_king_bar") == null ||
                        ally.FindItem("item_black_king_bar").Cooldown > 0))) ||
                     ally.IsHexed() ||
                     ally.IsRooted()) && !ally.IsInvul()
                    )
                {
                    return true;
                }

                return false;
            }
            return false;
        }

        private static bool Support(ClassID hero)
        {
            if ((hero == ClassID.CDOTA_Unit_Hero_Oracle || hero == ClassID.CDOTA_Unit_Hero_Winter_Wyvern ||
                 hero == ClassID.CDOTA_Unit_Hero_KeeperOfTheLight || hero == ClassID.CDOTA_Unit_Hero_Dazzle ||
                 hero == ClassID.CDOTA_Unit_Hero_Chen || hero == ClassID.CDOTA_Unit_Hero_Enchantress ||
                 hero == ClassID.CDOTA_Unit_Hero_Legion_Commander || hero == ClassID.CDOTA_Unit_Hero_Abaddon ||
                 hero == ClassID.CDOTA_Unit_Hero_Omniknight || hero == ClassID.CDOTA_Unit_Hero_Treant ||
                 hero == ClassID.CDOTA_Unit_Hero_Wisp || hero == ClassID.CDOTA_Unit_Hero_Centaur ||
                 hero == ClassID.CDOTA_Unit_Hero_Undying || hero == ClassID.CDOTA_Unit_Hero_WitchDoctor ||
                 hero == ClassID.CDOTA_Unit_Hero_Necrolyte || hero == ClassID.CDOTA_Unit_Hero_Warlock ||
                 hero == ClassID.CDOTA_Unit_Hero_Rubick || hero == ClassID.CDOTA_Unit_Hero_Huskar ||
                 hero == ClassID.CDOTA_Unit_Hero_Shadow_Demon) && Utils.SleepCheck("checkIfSupport"))
            {
                return true;
            }
            return false;
        }
    }
}
