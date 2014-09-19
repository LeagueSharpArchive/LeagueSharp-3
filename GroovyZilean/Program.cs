﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace GroovyZilean
{ 
    class Program
    {
        // Generic
        public static readonly string champName = "Zilean";
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        // Spells
        private static readonly List<Spell> spellList = new List<Spell>();
        private static Spell Q, W, E, R;
        
        // Menu
        public static Menu menu;
        
        private static Orbwalking.Orbwalker OW;

        public static void Main(string[] args)
        {
            // Register events
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            //Champ validation
            if (player.ChampionName != champName) return;

            //Define spells
            Q = new Spell(SpellSlot.Q, 700);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 700);
            R = new Spell(SpellSlot.R, 900);
            spellList.AddRange(new []{Q, E, R});

            // Create menu
            createMenu();

            // Register events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

            // Print
            Game.PrintChat(String.Format("<font color='#08F5F8'>blacky -</font> <font color='#FFFFFF'>{0} Loaded!</font>", champName));
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            // Spell ranges
            foreach (var spell in spellList)
            {
                // Regular spell ranges
                var circleEntry = menu.Item("drawRange" + spell.Slot).GetValue<Circle>();
                if (circleEntry.Active)
                    Utility.DrawCircle(player.Position, spell.Range, circleEntry.Color);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Combo
            if (menu.SubMenu("combo").Item("comboActive").GetValue<KeyBind>().Active)
                OnCombo();

            // Harass
            if (menu.SubMenu("harass").Item("harassActive").GetValue<KeyBind>().Active && 
               (ObjectManager.Player.Mana / ObjectManager.Player.MaxMana * 100) >
                menu.Item("harassMana").GetValue<Slider>().Value)
                OnHarass();

            // AutoUlt
            if (menu.SubMenu("ult").Item("ultUseR").GetValue<bool>())
                AutoUlt();

        }

        private static void OnCombo()
        {
            Menu comboMenu = menu.SubMenu("combo");
            bool useQ = comboMenu.Item("comboUseQ").GetValue<bool>() && Q.IsReady();
            bool useW = comboMenu.Item("comboUseW").GetValue<bool>() && W.IsReady();
            bool useE = comboMenu.Item("comboUseE").GetValue<bool>() && E.IsReady();

            if (useQ)
            {
                var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
                if (target != null)
                    Q.Cast(target);
            }

            if (useW && !useQ)
            {
                W.Cast();
            }

            if (useE)
            {
                var target = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
                if (target != null)
                    E.Cast(target);
            }
        }

        private static void OnHarass()
        {
            Menu harassMenu = menu.SubMenu("harass");
            bool useQ = harassMenu.Item("harassUseQ").GetValue<bool>() && Q.IsReady();
            bool useW = harassMenu.Item("harassUseW").GetValue<bool>() && W.IsReady();

            if (useQ)
            {
                var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
                if (target != null)
                    Q.Cast(target);
            }

            if (useW && !useQ)
            {
                W.Cast();
            }
        }

        private static void AutoUlt()
        {
            if (menu.Item("ultUseR").GetValue<bool>())
            {
                foreach (Obj_AI_Hero AChamp in ObjectManager.Get<Obj_AI_Hero>())
                    if ((AChamp.IsAlly) && (ObjectManager.Player.ServerPosition.Distance(AChamp.Position) < R.Range))
                    if (menu.Item("Ult" + AChamp.BaseSkinName).GetValue<bool>() && R.IsReady())
                    if (AChamp.Health < (AChamp.MaxHealth * (menu.Item("ultPercent").GetValue<Slider>().Value * 0.01)))
                    if ((!AChamp.IsDead) && (!AChamp.IsInvulnerable))
                                {
                                    R.CastOnUnit(AChamp, false);
                                }
            }
        }

        private static void createMenu()
        {
            menu = new Menu("Groovy" + champName, "groovy" + champName, true);

            // Target selector
            Menu ts = new Menu("Target Selector", "ts");
            menu.AddSubMenu(ts);
            SimpleTs.AddToMenu(ts);

            // Orbwalker
            Menu orbwalk = new Menu("Orbwalking", "orbwalk");
            menu.AddSubMenu(orbwalk);
            OW = new Orbwalking.Orbwalker(orbwalk);

            // Combo
            Menu combo = new Menu("Combo", "combo");
            menu.AddSubMenu(combo);
            combo.AddItem(new MenuItem("comboUseQ",         "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("comboUseW",         "Use W").SetValue(true));
            combo.AddItem(new MenuItem("comboUseE",         "Use E").SetValue(true));
            combo.AddItem(new MenuItem("comboActive",       "Combo active!").SetValue(new KeyBind(32, KeyBindType.Press)));

            // Harass
            Menu harass = new Menu("Harass", "harass");
            menu.AddSubMenu(harass);
            harass.AddItem(new MenuItem("harassUseQ",   "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("harassUseW",   "Use W").SetValue(false));
            harass.AddItem(new MenuItem("harassMana", "Mana To Harass").SetValue(new Slider(40, 100, 0)));
            harass.AddItem(new MenuItem("harassActive", "Harass active!").SetValue(new KeyBind('C', KeyBindType.Press)));

            // Ult
            Menu ult = new Menu("Ult", "ult");
            menu.AddSubMenu(ult);
            ult.AddItem(new MenuItem("ultUseR", "Use R")).SetValue(true);
            foreach (Obj_AI_Hero Champ in ObjectManager.Get<Obj_AI_Hero>())
                if (Champ.IsAlly)
            ult.AddItem(new MenuItem("Ult" + Champ.BaseSkinName, string.Format("Ult {0}", Champ.BaseSkinName)).SetValue(true));
            ult.AddItem(new MenuItem("ultPercent", "R at % HP")).SetValue(new Slider(25, 1, 100));

            // Drawings
            Menu drawings = new Menu("Drawings", "drawings");
            menu.AddSubMenu(drawings);
            drawings.AddItem(new MenuItem("drawRangeQ",     "Q range").SetValue(new Circle(true, Color.FromArgb(150, Color.DarkRed))));

            // Finalizing
            menu.AddToMainMenu();
        }
    }
}

