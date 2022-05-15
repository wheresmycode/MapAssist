/**
 *   Copyright (C) 2021 okaygo
 *
 *   https://github.com/misterokaygo/MapAssist/
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 **/

using GameOverlay.Drawing;
using GameOverlay.Windows;
using MapAssist.Helpers;
using MapAssist.Settings;
using MapAssist.Types;
using SendInputs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
//using WK.Libraries.HotkeyListenerNS;
using Graphics = GameOverlay.Drawing.Graphics;

namespace MapAssist
{
    public class FollowMe : IDisposable
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private readonly GraphicsWindow _window;
        private GameDataReader _gameDataReader;
        private GameData _gameData;
        private Compositor _compositor = new Compositor();
        private bool _show = true;
        private static readonly object _lock = new object();

        //public UnitPlayer _leader;

        public Point[] enemyPos;
        public Point leaderPos;

        private int inputDelay = MapAssistConfiguration.Loaded.FollowConfiguration.InputDelay;
        private int potTimerHealth = 0;
        private int potTimerMana = 0;

        private Point lastLeaderPos = new Point();


        public FollowMe()
        {
            _gameDataReader = new GameDataReader();

            GameOverlay.TimerService.EnableHighPrecisionTimers();

            var gfx = new Graphics() { MeasureFPS = true };
            gfx.PerPrimitiveAntiAliasing = true;
            gfx.TextAntiAliasing = true;

            _window = new GraphicsWindow(0, 0, 1, 1, gfx) { FPS = 40, IsVisible = true };

            _window.DrawGraphics += _window_DrawGraphics;
            _window.DestroyGraphics += _window_DestroyGraphics;

            
        }

        public void Run()
        {
            _window.Create();
            _window.Join();
        }

        

        private void MoveToLeader(Point WindowPos)
        {
            
            if (_compositor.Leader != null && lastLeaderPos != WindowPos && LeaderDistance() > MapAssistConfiguration.Loaded.FollowConfiguration.FollowRange)
            {
                //lastLeaderPos = _compositor.Leader.Position;
                WindowPos = Vector2.Transform(WindowPos.ToVector(), _compositor.areaTransformMatrix).ToPoint();
                
                if (LeaderDistance() > 1 && LeaderDistance() < 50)
                // LeaderDistance() > 1 -> Don't move if closer than 1
                // LeaderDistance() < 50 -> Don't move if Leader too far away
                {
                    _log.Info("LeaderDistance: " + LeaderDistance());

                    //_log.Info("D2R Window:" + rect);
                    var rect = WindowRect();
                    var X = (int)Math.Min(Math.Max(rect.Left + WindowPos.X, WindowPos.X),rect.Right);
                    var Y = (int)Math.Min(Math.Max(rect.Top + WindowPos.Y, WindowPos.Y), rect.Bottom);

                    //_log.Info("WindowRelPos: " + X + "," + X);

                    //InputSender.LeftClick(X,Y, inputDelay);

                    InputSender.SetCursorPosition(X,Y);
                    InputSender.ClickKey("E",100);
                    
                }

            }
            lastLeaderPos = _compositor.Leader.Position;
        }

        private void CacheLeaderPos() 
        { lastLeaderPos = _compositor.Leader.Position; }
        

        private void AttackMonster(List<UnitMonster> monsterList)
        {
            if (_gameData.PlayerUnit.Area.IsTown()) return;
            var attackRange = MapAssistConfiguration.Loaded.FollowConfiguration.AttackRange;
            var attackList = new List<(Point,int)>();
            var myMapPos = _gameData.PlayerUnit.Position.ToVector();
            var myOsdPos = Vector2.Transform(_gameData.PlayerUnit.Position.ToVector(), _compositor.areaTransformMatrix);
            var monstersToIgnore = MapAssistConfiguration.Loaded.FollowConfiguration.IgnoreMonsters; // new List<string> { "BaalTaunt", "Act5Combatant", "CatapultSpotterE", "CatapultSpotterSiege" };
            var ignoreImmunes = MapAssistConfiguration.Loaded.FollowConfiguration.IgnoreImmunities;
            var attackKey1 = MapAssistConfiguration.Loaded.FollowConfiguration.FastcastSkill1;
            var ignore = false;

            foreach (UnitMonster monster in monsterList)
            {
                if (attackList.Count > 6) continue; //dont populate attacklist with more than x monsters
                ignore = false;
                if (monster.DistanceTo(_gameData.PlayerUnit) <= attackRange && monster.IsMonster && !monster.IsPlayerOwned)
                {
                    //_log.Info("Monster Distance: " + monster.DistanceTo(_gameData.PlayerUnit));
                    if (monstersToIgnore.Contains(monster.Npc.Name())) continue;
                    if (monster.Immunities.Count > 0 && ignoreImmunes.Count > 0)
                    {
                        foreach (var immunity in ignoreImmunes) 
                        {
                            if (monster.Immunities.Contains(immunity))
                            {
                                //_log.Info($"Monster: {monster.Npc.Name()} has immunity {immunity} and is ignored");
                                ignore = true;
                                continue;
                            }
                            //_log.Info($"Monster: {monster.Npc.Name()} has immunity {monster.Immunities.First()}");
                        }
                    }
                    if (ignore == true) continue;
                    //_log.Info($"Monster: {monster.Npc.Name()}, Merc: {monster.IsMerc}, PlayerOwned: {monster.IsPlayerOwned}, UnitType: {monster.UnitType}, MonsterType: {monster.MonsterType}");
                    attackList.Add((monster.Position, (int)monster.DistanceTo(_gameData.PlayerUnit)));
                    //_log.Info($"Added Monster: {monster.Npc.Name()} to attacklist");
                }
            }
            if (attackList.Count>0)
            {
                //var nextTarget = attackList.Min(i => i.Item1);
                //var nextTarget1 = attackList.OrderBy(i => i.Item2).Last();
                //_log.Info("nextTarget1 Distance: " + nextTarget1.Item2);
                var nextTarget2 = attackList.OrderBy(i => i.Item2).First();
                //_log.Info("nextTarget2 Distance/Pos:  " + nextTarget2.Item2 +"" + nextTarget2.Item1);

                var monsterOsdPos = Vector2.Transform(nextTarget2.Item1.ToVector(), _compositor.areaTransformMatrix);
                var monsterPosFromMyPos = Vector2.Subtract(monsterOsdPos, myOsdPos);


                var rect = WindowRect();
                var rectMiddleX = rect.Width / 2;
                var rectMiddleY = rect.Height / 2;

                var resizeX = 8;
                var resizeY = 8;
                var paddingX = 1;
                var paddingY = 0;
                //if (leaderPosFromMyPos.X >= 0) { paddingX = padding; } else { paddingX = padding * -1; }
                //if (leaderPosFromMyPos.X >= 0) { paddingY = padding; } else { paddingY = padding * -1; }

                var X = (int)Math.Min(Math.Max(rect.Left + rectMiddleX + (monsterPosFromMyPos.X * resizeX + paddingX), rect.Left), rect.Right - 2);
                var Y = (int)Math.Min(Math.Max(rect.Top + rectMiddleY + (monsterPosFromMyPos.Y * resizeY + paddingY), rect.Top), rect.Bottom*0.85);

                //var X = (int)Math.Min(Math.Max(rect.Left + (monsterPosFromMyPos.X * resizeX), monsterPosFromMyPos.X * resizeX), rect.Right);
                //var Y = (int)Math.Min(Math.Max(rect.Top + (monsterPosFromMyPos.Y * resizeY), monsterPosFromMyPos.Y * resizeY), rect.Bottom);
                //_log.Info("nextTarget2 Distance/Pos:  " + nextTarget2.Item2 + " " + X + "," + Y);
                //_log.Info("Distance/Pos:  " + nextTarget2.Item2 + " " + monsterPosFromMyPos.X + "," + monsterPosFromMyPos.Y);

                // InputSender has too much options!!!

                ClickOnOsdPos(new Point(X,Y), attackKey1);
                //InputSender.ClickKey(attackKey1, 200);
                //InputSender.RightClick((int)X, (int)Y, inputDelay);
                //InputSender.SetCursorPosition(X, Y);
            }
        }

        private void ClickOnLeader() //this tries to click on the leader instead of its icon on the map
        {
            if (_compositor.Leader != null)
            {
                var myMapPos = _gameData.PlayerUnit.Position.ToVector();
                var myOsdPos = Vector2.Transform(_gameData.PlayerUnit.Position.ToVector(), _compositor.areaTransformMatrix);
                var leaderOsdPos = Vector2.Transform(_compositor.Leader.Position.ToVector(), _compositor.areaTransformMatrix);

                var leaderPosFromMyPos = Vector2.Subtract(leaderOsdPos, myOsdPos);
                var leaderMapDist = Vector2.Distance(_compositor.Leader.Position.ToVector(), myMapPos);

                var rect = WindowRect();
                var rectMiddleX = rect.Width / 2;
                var rectMiddleY = rect.Height / 2;
                
                var resizeX = 10;
                var resizeY = 10;
                
                var paddingX = 0;
                var paddingY = 0;
                //if (leaderPosFromMyPos.X >= 0) { paddingX = padding; } else { paddingX = padding * -1; }
                //if (leaderPosFromMyPos.X >= 0) { paddingY = padding; } else { paddingY = padding * -1; }

                var X = (int)Math.Min(Math.Max(rect.Left + rectMiddleX + (leaderPosFromMyPos.X * resizeX + paddingX), rect.Left), rect.Right);
                var Y = (int)Math.Min(Math.Max(rect.Top + rectMiddleY + (leaderPosFromMyPos.Y * resizeY + paddingY), rect.Top), rect.Bottom);

                //var X = (int)Math.Min(Math.Max(rect.Left + (monsterPosFromMyPos.X * resizeX), monsterPosFromMyPos.X * resizeX), rect.Right);
                //var Y = (int)Math.Min(Math.Max(rect.Top + (monsterPosFromMyPos.Y * resizeY), monsterPosFromMyPos.Y * resizeY), rect.Bottom);
                //_log.Info(rect);
                //_log.Info("Leader Click Pos: " + X + "," + Y);
                //_log.Info("Leader Distance/Pos:  " + leaderMapDist + " " + leaderPosFromMyPos.X + "," + leaderPosFromMyPos.Y);

                InputSender.RightClick((int)X, (int)Y, 400);
                
                //InputSender.SetCursorPosition(X, Y);
                
                

            }
        }

        private void ClickOnOsdPos(Point pos,string input) //
        {
            switch (input)
            {
                case "right":
                    InputSender.RightClick((int)pos.X, (int)pos.Y, 300);
                    break;
                case "left":
                    InputSender.LeftClick((int)pos.X, (int)pos.Y, 300);
                    break;
                case string i when (i.Length == 1):
                    InputSender.SetCursorPosition((int)pos.X, (int)pos.Y);
                    InputSender.ClickKey(input, 300);
                    Console.WriteLine($"Click Input was: {i}");
                    break;
            }

            //InputSender.SetCursorPosition(X, Y);

        }


        private void DrinkPots()
        {
            if (_gameData.PlayerUnit.Area.IsTown() | _gameData.PlayerUnit.IsCorpse) return; //dont drink if in town or dead
            var health = _gameData.PlayerUnit.LifePercentage;
            var mana = _gameData.PlayerUnit.ManaPercentage;
            var drinkHPRJ = MapAssistConfiguration.Loaded.FollowConfiguration.HealthRejuv;
            var drinkMPRJ = MapAssistConfiguration.Loaded.FollowConfiguration.ManaRejuv;
            var drinkHP = MapAssistConfiguration.Loaded.FollowConfiguration.HealthPot;
            var drinkMP = MapAssistConfiguration.Loaded.FollowConfiguration.ManaPot;

            var pots = _compositor.potsInBelt; //health=0,mana=1,rejuv=2,else=3

            var drankMana = false;
            var drankHealth = false;

            if (health > drinkHP && mana > drinkMP) 
            {
                if(potTimerHealth > 0) potTimerHealth--;
                if(potTimerMana > 0) potTimerMana--;
                //_log.Info($"Health/Drink {health}/{drinkHP} potTimerHealth@{potTimerHealth}");
                //_log.Info($"Mana/Drink {mana}/{drinkMP} potTimerMana@{potTimerMana}");
                return; 
            }


            if ((health <= drinkHPRJ | mana <= drinkMPRJ) && (potTimerHealth <= 120 | potTimerMana <= 100))
            {
                var rejuv = 2;
                var potKeyInt = 0;
                for (var i = 0; i < pots.Length; i++)
                {
                    if (pots[i] == rejuv)
                    {
                        potKeyInt = (i+1);
                    }
                    //else potKeyInt = 0;
                }
                
                if (potKeyInt > 0)
                {
                    InputSender.ClickKey(potKeyInt.ToString(), 200);
                    drankHealth = true;
                    drankMana = true;
                }
                else 
                {
                    _log.Info($"Missing Rejuvs");
                    potTimerHealth--;
                    potTimerMana--;
                }
                
            }

            if (mana <= drinkMP && potTimerMana <= 10)
            {
                var manaPot = 1;
                var potKeyInt = 0;
                for (var i = 0; i < pots.Length; i++)
                {
                    if (pots[i] == manaPot)
                    {
                        potKeyInt = (i + 1);
                    }
                    //else potKeyInt = 0;
                }

                if (potKeyInt > 0)
                {
                    InputSender.ClickKey(potKeyInt.ToString(), 200);
                    drankMana = true;
                    //_log.Info($"Clicked {potKeyInt} for Mana");
                }
                else
                {
                    _log.Info($"Missing Manapots");
                    potTimerMana--;
                }
            }else potTimerMana--;

            if (health <= drinkHP && potTimerHealth <= 10)
            {
                var healthPot = 0;
                var potKeyInt = 0;
                for (var i = 0; i < pots.Length; i++)
                {
                    if (pots[i] == healthPot)
                    {
                        potKeyInt = (i + 1);
                    }
                    //else potKeyInt = 0;
                }

                if (potKeyInt > 0)
                {
                    InputSender.ClickKey(potKeyInt.ToString(), 200);
                    drankHealth = true;
                    //_log.Info($"Clicked {potKeyInt} for Health");
                }
                else
                {
                    _log.Info($"Missing Healthpots");
                    potTimerHealth--;
                }
            }else potTimerHealth--;


            if (drankHealth == true)
            {
                potTimerHealth = 150;
            }
            if (drankMana == true)
            {
                potTimerMana = 150;
            }
        }

        private int LeaderDistance()
        {
            if (_gameData.PlayerUnit.Area == _compositor.Leader.Area)
            {
                return Convert.ToInt32(Vector2.Distance(PointExt.ToVector2(_gameData.PlayerUnit.Position), PointExt.ToVector2(_compositor.Leader.Position)));
            }
            _log.Info("Error in LeaderDistance() "+ _compositor.Leader.Name + " is not in the same area!");
            return 500;
        }


        private void _window_DrawGraphics(object sender, DrawGraphicsEventArgs e)
        {
            if (disposed) return;

            var gfx = e.Graphics;

            try
            {
                lock (_lock)
                {
                    var (gameData, areaData, pointsOfInterest, changed) = _gameDataReader.Get();
                    _gameData = gameData;
                    enemyPos = null;

                    if (changed)
                    {
                        _compositor.SetArea(areaData, pointsOfInterest);
                    }

                    gfx.ClearScene();

                    //if (_compositor != null && InGame() && _compositor != null && _gameData != null)
                    if (_compositor != null && InGame() && _gameData != null)
                    {
                        UpdateLocation();
                        //WindowBounds rect;
                        //WindowHelper.GetWindowClientBounds(_window.Handle, out rect);

                        var errorLoadingAreaData = _compositor._areaData == null;

                        var overlayHidden = !_show ||
                            errorLoadingAreaData ||
                            _gameData.Area == Area.None ||
                            gfx.Width == 1 ||
                            gfx.Height == 1;

                        var size = MapAssistConfiguration.Loaded.RenderingConfiguration.Size;

                        var drawBounds = new Rectangle(0, 0, gfx.Width, gfx.Height * 0.78f);
                        switch (MapAssistConfiguration.Loaded.RenderingConfiguration.Position)
                        {
                            case MapPosition.TopLeft:
                                drawBounds = new Rectangle(PlayerIconWidth() + 40, PlayerIconWidth() + 100, 0, PlayerIconWidth() + 100 + size);
                                break;

                            case MapPosition.TopRight:
                                drawBounds = new Rectangle(0, 100, gfx.Width, 100 + size);
                                break;
                        }

                        _compositor.Init(gfx, _gameData, drawBounds);

                        if (!overlayHidden)
                        {
                            _compositor.DrawGamemap(gfx);
                            _compositor.DrawOverlay(gfx);
                            _compositor.DrawBuffs(gfx);
                            _compositor.DrawMonsterBar(gfx);
                        }

                        _compositor.DrawPlayerInfo(gfx);
                        //getLeader();


                        DrinkPots();
                        if (_compositor.Leader != null) //this only runs if the leader is in the roster
                        {
                            //_log.Info("Leader is NOT null");
                            //_log.Info($"Leader Position Changed:{lastLeaderPos} {_compositor.Leader.Position} {lastLeaderPos.DistanceTo(_compositor.Leader.Position)}");
                            
                            if (_compositor.MonsterList.Count > 0) 
                            { 
                                if (MapAssistConfiguration.Loaded.FollowConfiguration.Melee && !_gameData.PlayerUnit.Area.IsTown() && LeaderDistance() <= MapAssistConfiguration.Loaded.FollowConfiguration.AttackRange && lastLeaderPos == _compositor.Leader.Position)
                                {    // allows follower to freeroam in attackrange if leader does not change position more than 10
                                    AttackMonster(_compositor.MonsterList);
                                    _log.Info("Look at me mom, im wildly attacking monsters!");
                                }
                                else if (LeaderDistance() <= MapAssistConfiguration.Loaded.FollowConfiguration.FollowRange)
                                {
                                AttackMonster(_compositor.MonsterList);
                                //_log.Info("Monsterlist: "+ _compositor.MonsterList.Count);
                                }
                            }

                            if (MapAssistConfiguration.Loaded.FollowConfiguration.Melee && LeaderDistance() >= MapAssistConfiguration.Loaded.FollowConfiguration.AttackRange) 
                            {
                                MoveToLeader(_compositor.Leader.Position);
                            }
                            else
                            {
                                MoveToLeader(_compositor.Leader.Position);
                            }
                            //CacheLeaderPos();
                        }
                        else if (_compositor.leaderDistance > 1 && _compositor.leaderDistance < 50 && _compositor.leaderOsdPos.Y > 0) 
                        {
                            ClickOnOsdPos(_compositor.leaderOsdPos, "E"); //try to click on leader if not in same map
                            _log.Info($"Leader distance: {_compositor.leaderDistance} Clicked Leader");
                        }

                        //_log.Info("_compositor.leaderDistance" + _compositor.leaderDistance);

                        //var leaderClickPos = Vector2.Transform(_compositor.leaderWindowPos, _compositor.areaTransformMatrix);
                        //_log.Info("leaderClickPos: "+ leaderClickPos);

                        var gameInfoAnchor = GameInfoAnchor(MapAssistConfiguration.Loaded.GameInfo.Position);
                        var nextAnchor = _compositor.DrawGameInfo(gfx, gameInfoAnchor, e, errorLoadingAreaData);

                        var itemLogAnchor = (MapAssistConfiguration.Loaded.ItemLog.Position == MapAssistConfiguration.Loaded.GameInfo.Position)
                            ? nextAnchor.Add(0, GameInfoPadding())
                            : GameInfoAnchor(MapAssistConfiguration.Loaded.ItemLog.Position);
                        _compositor.DrawItemLog(gfx, itemLogAnchor);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public void oldRun()
        {
            _window.Create();
            _window.Join();
        }

        private bool InGame()
        {
            return _gameData != null && _gameData.MainWindowHandle != IntPtr.Zero;
        }

        public void KeyDownHandler(object sender, KeyEventArgs args)
        {
            if (InGame() && GameManager.IsGameInForeground)
            {
                var keys = new Hotkey(args.Modifiers, args.KeyCode);

                if (keys == new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.ToggleKey))
                {
                    _show = !_show;
                }

                if (keys == new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.HideMapKey))
                {
                    _show = false;
                }

                if (keys == new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.AreaLevelKey))
                {
                    MapAssistConfiguration.Loaded.GameInfo.ShowAreaLevel = !MapAssistConfiguration.Loaded.GameInfo.ShowAreaLevel;
                }

                if (keys == new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.ZoomInKey))
                {
                    var zoomLevel = MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel;

                    if (MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel > 0.1f)
                    {
                        MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel -= zoomLevel <= 1 ? 0.1 : 0.2;
                        MapAssistConfiguration.Loaded.RenderingConfiguration.Size +=
                          (int)(MapAssistConfiguration.Loaded.RenderingConfiguration.InitialSize * 0.05f);
                    }
                }

                if (keys == new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.ZoomOutKey))
                {
                    var zoomLevel = MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel;

                    if (MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel < 4f)
                    {
                        MapAssistConfiguration.Loaded.RenderingConfiguration.ZoomLevel += zoomLevel >= 1 ? 0.2 : 0.1;
                        MapAssistConfiguration.Loaded.RenderingConfiguration.Size -=
                          (int)(MapAssistConfiguration.Loaded.RenderingConfiguration.InitialSize * 0.05f);
                    }
                }

                if (keys == new Hotkey(MapAssistConfiguration.Loaded.HotkeyConfiguration.ExportItemsKey))
                {
                    if (InGame())
                    {
                        ItemExport.ExportPlayerInventory(_gameData.PlayerUnit, _gameData.AllItems);
                    }
                }
            }
        }

        /// <summary>
        /// Resize overlay to currently active screen
        /// </summary>
        private void UpdateLocation()
        {
            var rect = WindowRect();
            var ultraWideMargin = UltraWideMargin();

            _window.Resize((int)(rect.Left + ultraWideMargin), (int)rect.Top, (int)(rect.Right - rect.Left - ultraWideMargin * 2), (int)(rect.Bottom - rect.Top));
            _window.PlaceAbove(_gameData.MainWindowHandle);
        }

        private Rectangle WindowRect() //Returns rectangle of D2R window
        {
            WindowBounds rect;
            WindowHelper.GetWindowClientBounds(_gameData.MainWindowHandle, out rect);

            return new Rectangle(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        private float UltraWideMargin()
        {
            var rect = WindowRect();
            return (float)Math.Max(Math.Round(((rect.Width + 2) - (rect.Height + 4) * 2.1f) / 2f), 0);
        }

        private float PlayerIconWidth()
        {
            var rect = WindowRect();
            return rect.Height / 20f;
        }

        private float GameInfoPadding()
        {
            var rect = WindowRect();
            return rect.Height / 100f;
        }

        private Point GameInfoAnchor(GameInfoPosition position)
        {
            switch (position)
            {
                case GameInfoPosition.TopLeft:
                    var margin = _window.Height / 18f;
                    return new Point(PlayerIconWidth() + margin, PlayerIconWidth() + margin);

                case GameInfoPosition.TopRight:
                    var rightMargin = _window.Width / 60f;
                    var topMargin = _window.Height / 35f;
                    return new Point(_window.Width - rightMargin, topMargin);
            }
            return new Point();
        }

        private void _window_DestroyGraphics(object sender, DestroyGraphicsEventArgs e)
        {
            if (_compositor != null) _compositor.Dispose();
            _compositor = null;
        }

        ~FollowMe() => Dispose();

        private bool disposed = false;

        public void Dispose()
        {
            lock (_lock)
            {
                if (!disposed)
                {
                    disposed = true; // This first to let GraphicsWindow.DrawGraphics know to return instantly
                    _window.Dispose(); // This second to dispose of GraphicsWindow
                    if (_compositor != null) _compositor.Dispose(); // This last so it's disposed after GraphicsWindow stops using it
                }
            }
        }
    }

    public static class PointExt
    {
        public static Vector2 ToVector2(this Point point)
        {
            return new Vector2(point.X, point.Y);
        }
    }
}
