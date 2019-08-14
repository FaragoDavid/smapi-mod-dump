﻿using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;
using System;
using StardewValley.Tools;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using BetterPanning.Data;
using Harmony;
using BetterPanning.GamePatch;
using BetterPanning.Config;
using System.Linq;

namespace BetterPanning
{
    class PanningMod : Mod
    {
        public static PanningMod Instance { get; private set; }
        private bool startup = true;
        private bool hasPanningSpot = false;
        private Dictionary<GameLocation, bool> modCreatedPanningSpot = new Dictionary<GameLocation, bool>();
        private bool playerPannedSpot = false;
        private bool foundAllArtifacts = false;
        private int numberOfPanningSpotsGathered = 0;
        private bool updatedNumberOfTimesGathered = false;        
        private string farmFile = "Farm.json";  //Default farm

        private Dictionary<string, List<Point>> openWaterTiles = new Dictionary<string, List<Point>>();
        internal ModConfig config; 

        internal Dictionary<TREASURE_GROUP, TreasureGroup> treasureGroups;

        internal HarmonyInstance harmony { get; private set; }

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            helper.Events.Player.Warped += Player_Warped;                        
            helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            helper.Events.Display.RenderedHud += Display_RenderedHud;
            helper.Events.Input.ButtonReleased += Input_ButtonReleased;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;

            try
            {
                config = this.Helper.Data.ReadJsonFile<ModConfig>("config.json") ?? ModConfigDefaultConfig.CreateDefaultConfig("config.json");
                config = ModConfigDefaultConfig.UpdateConfigToLatest(config, "config.json"); 
            }
            catch  //Really the only time this is going to error is when going from old version to new version of the config file or there is a bad config file
            {               
                config = ModConfigDefaultConfig.UpdateConfigToLatest(config, "config.json") ?? ModConfigDefaultConfig.CreateDefaultConfig("config.json"); 
            }

            if (config.useCustomPanningTreasure)
            {
                string treasureFile = Path.Combine("DataFiles", "Treasure.json");
                treasureGroups = this.Helper.Data.ReadJsonFile<Dictionary<TREASURE_GROUP, TreasureGroup>>(treasureFile) ?? TreasureGroupDefaultConfig.CreateTreasureGroup(treasureFile);

                harmony = HarmonyInstance.Create("com.aairthegreat.mod.panning");
                harmony.Patch(typeof(Pan).GetMethod("getPanItems"), null, new HarmonyMethod(typeof(PanOverrider).GetMethod("postfix_getPanItems")));
            }                        
        }

        // Used for if the player goes to the start menu and selects a different saved game.
        private void VerifyInitValues()
        {
            openWaterTiles.Clear();
            modCreatedPanningSpot.Clear();

            numberOfPanningSpotsGathered = 0;
            startup = true;
            hasPanningSpot = false;
            playerPannedSpot = false;
            foundAllArtifacts = false;
            updatedNumberOfTimesGathered = false;
        }

        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {            
            if (config.useCustomPanningTreasure)
            {
                VerifyInitValues();

                farmFile = Farm.getMapNameFromTypeInt(Game1.whichFarm) + ".json";
                if (config.useCustomFarmMaps)
                {
                    if (config.customMaps.ContainsKey(Game1.whichFarm))
                    {
                        farmFile = config.customMaps[Game1.whichFarm];
                    }
                }
                
                this.Monitor.Log($"Mod will use the {farmFile} farm data file.");

                if (config.enableArtifactTreasures)
                {
                    if (config.enableAllArtifactsAfterFoundThemAll)
                    {
                        foundAllArtifacts = HasFoundAllArtifacts();
                        this.Monitor.Log($"Player has found all artifacts: {foundAllArtifacts}");
                    }
                }
                else
                {
                    treasureGroups[TREASURE_GROUP.Artifacts].SetEnableFlagOnAllTreasures(false);
                }

                if (config.enableGeodeMineralsTreasure)
                {
                    //Disables geode minerals which are not found yet
                    treasureGroups[TREASURE_GROUP.GeodeMinerals].CheckGroupTreasuresStatus();                      
                }
                else
                {
                    treasureGroups[TREASURE_GROUP.GeodeMinerals].SetEnableFlagOnAllTreasures(false);                    
                }

                CheckSeeds();  //Initial settings
            }
        }

        private void Input_ButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if(Game1.player.CurrentTool is Pan &&  Game1.player.UsingTool)
            {
                if (!updatedNumberOfTimesGathered)
                {
                    playerPannedSpot = true;
                    hasPanningSpot = false;
                    modCreatedPanningSpot[Game1.currentLocation] = false;
                    numberOfPanningSpotsGathered++;
                    updatedNumberOfTimesGathered = true;
                }
            }
        }
 
        private void Display_RenderedHud(object sender, RenderedHudEventArgs e)
        {            
            if (!config.showHudData || Game1.eventUp || !(Game1.player.CurrentTool is Pan))
                return;

            Color textColor = Color.White;
            SpriteFont font = Game1.smallFont;

            // Draw the panning info GUI to the screen
            float boxWidth = 0;            
            float lineHeight = font.LineSpacing;
            Vector2 boxTopLeft = new Vector2(config.hudXPostion, config.hudYPostion);
            Vector2 boxBottomLeft = boxTopLeft;

            // Setup the sprite batch
            SpriteBatch batch = Game1.spriteBatch;
            batch.End();
            batch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend);

            Point orePoint = Game1.player.currentLocation.orePanPoint.Value;
            string hudTextLine1 = "Found a panning spot!"; 
            string hudTextLine2 = "";
            string hudTextLine3 = "";            

            if (!orePoint.Equals(Point.Zero))
            {                
                string oreRelativePostion = GetOreRelativePostion(orePoint);
                long distance = GetDistanceToOre(orePoint);
                if (config.showDistance)
                {
                    hudTextLine2 = $"In the {oreRelativePostion} direction.";
                    hudTextLine3 = $"Roughly { distance} tiles away.";
                }
            }
            else
            {
                if (hasPanningSpot && !playerPannedSpot)
                {
                    hudTextLine1 = "The panning spot is GONE!";
                    hudTextLine2 = "A fish must have ate the treasure.";
                }
                else if (playerPannedSpot)
                {
                    hudTextLine1 = "Good job! You got the spot!";
                    hudTextLine2 = "Try this area again later!";
                    updatedNumberOfTimesGathered = false;
                }
                else
                {
                    hudTextLine1 = "No panning spot found!";                    
                }
            }

            batch.DrawStringWithShadow(font, hudTextLine1, boxBottomLeft, textColor, 1.0f);
            boxWidth = Math.Max(boxWidth, font.MeasureString(hudTextLine1).X + 5);
            boxBottomLeft += new Vector2(0, lineHeight);
            
            batch.DrawStringWithShadow(font, hudTextLine2, boxBottomLeft, textColor, 1.0f);
            boxWidth = Math.Max(boxWidth, font.MeasureString(hudTextLine2).X + 5);
            boxBottomLeft += new Vector2(0, lineHeight);

            batch.DrawStringWithShadow(font, hudTextLine3, boxBottomLeft, textColor, 1.0f);
            boxWidth = Math.Max(boxWidth, font.MeasureString(hudTextLine3).X + 5);
            boxBottomLeft += new Vector2(0, lineHeight);

            Texture2D box =  Game1.staminaRect;
            // Draw the background rectangle DrawHelpers.WhitePixel
            batch.Draw(box, new Rectangle((int)boxTopLeft.X, (int)boxTopLeft.Y, (int)boxWidth, (int)(boxBottomLeft.Y - boxTopLeft.Y)), null,
                new Color(0, 0, 0, 0.25F), 0f, Vector2.Zero, SpriteEffects.None, 0.85F);
            
            batch.End();
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
        }

        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            if (startup) // Only need to initalize once.
            {
                UpdatePossibleTiles(Game1.player.currentLocation);
                if (!modCreatedPanningSpot.ContainsKey(Game1.player.currentLocation))
                {
                    modCreatedPanningSpot.Add(Game1.player.currentLocation, false);
                }
                startup = false;
            }

            //this.Monitor.Log($"Setting: numberOfPanningSpotsGathered ({numberOfPanningSpotsGathered}) to 0");
            numberOfPanningSpotsGathered = 0; //new day, new chances!
            updatedNumberOfTimesGathered = false;

            if (config.useCustomPanningTreasure)
            {
                if (config.enableArtifactTreasures)
                {
                    if (!foundAllArtifacts)
                    {
                        foundAllArtifacts = HasFoundAllArtifacts();  // Need to figure out if they have found all the artifacts in the previous day

                        if (!foundAllArtifacts || (foundAllArtifacts && !config.enableAllArtifactsAfterFoundThemAll))
                        {
                            treasureGroups[TREASURE_GROUP.Artifacts].CheckGroupTreasuresStatus(); //In case they found new ones the previous day.                                                
                        }
                        else if (config.enableAllArtifactsAfterFoundThemAll)
                        {
                            treasureGroups[TREASURE_GROUP.Artifacts].SetEnableFlagOnAllTreasures(true);
                        }
                    }
                }

                if (config.enableGeodeMineralsTreasure)
                {
                    treasureGroups[TREASURE_GROUP.GeodeMinerals].CheckGroupTreasuresStatus();  //In case they found new ones the previous day.                    
                }

                if (Game1.dayOfMonth == 1)  //Should mean it's in new season, different seeds should now be available.
                {
                    CheckSeeds();
                    this.Monitor.Log($"New Season: {Game1.currentSeason}, changing seeds.");
                }
            }
        }

        private void Player_Warped(object sender, WarpedEventArgs e)
        {
            GameLocation location = e.NewLocation;
            hasPanningSpot = false;
            playerPannedSpot = false;
            updatedNumberOfTimesGathered = false;

            if (!modCreatedPanningSpot.ContainsKey(location))
            {
                modCreatedPanningSpot.Add(location, false);
            }
            
            if (Game1.MasterPlayer.mailReceived.Contains("ccFishTank")) //Original code excludes beach... not this code!
            {
                if (numberOfPanningSpotsGathered <= config.maxNumberOfOrePointsGathered)
                {
                    UpdatePossibleTiles(location);

                    Point orePoint = location.orePanPoint.Value;                    
                    if (!orePoint.Equals(Point.Zero) && !modCreatedPanningSpot[location])
                    {                        
                        if ((config.sp_alwaysCreatePanningSpots && Game1.getOnlineFarmers().Count == 1)
                            || (config.mp_alwaysCreatePanningSpots && Context.IsMultiplayer && Game1.getOnlineFarmers().Count > 1))
                        {
                            CreatePanningSpot(location);
                        }
                        else
                        {
                            hasPanningSpot = true;
                            playerPannedSpot = false;
                        }
                    }
                    else if(orePoint.Equals(Point.Zero))
                    {
                        CreatePanningSpot(location);
                    }
                }
                else
                {
                    this.Monitor.Log($"Ores Gathered {numberOfPanningSpotsGathered} : max {config.maxNumberOfOrePointsGathered}");
                }
            }
        }

        private void CheckSeeds()
        {
            if (!config.enableSeedPanning)
            {
                EnableSeasonSeeds(false, false, false, false);
            }
            else
            {
                if (config.enableAllSeedsEverySeason)
                {
                    EnableSeasonSeeds(true, true, true, true);
                }
                else
                {                   
                    switch (Game1.currentSeason)
                    {
                        case "spring":
                            EnableSeasonSeeds(false, true, false, false);
                            break;
                        case "summer":
                            EnableSeasonSeeds(false, false, true, false);
                            break;
                        case "fall":
                            EnableSeasonSeeds(true, false, false, false);
                            break;
                        case "winter":
                            EnableSeasonSeeds(true, true, true, true);
                            break;
                        default:
                            EnableSeasonSeeds(false, false, false, false);
                            this.Monitor.Log($"Unknown Season: {Game1.currentSeason}, disabled seeds.");
                            break;
                    }

                    if (Game1.year == 1 && !config.enableAllSecondYearSeedsOnFirstYear)
                    {
                        EnableSecondYearSeeds(false);
                    }
                    else
                    {
                        EnableSecondYearSeeds(true);
                        this.Monitor.Log($"Enabled Second Year Seeds, Game Year: {Game1.year}");
                    }
                }
            }
        }

        private void EnableSecondYearSeeds(bool enable)
        {
            if (treasureGroups[TREASURE_GROUP.SpringSeeds].Enabled)
            {
                treasureGroups[TREASURE_GROUP.SpringSeeds].SetEnableTreasure(476, enable); //Garlic Seeds
            }

            if (treasureGroups[TREASURE_GROUP.SummerSeeds].Enabled)
            {
                treasureGroups[TREASURE_GROUP.SummerSeeds].SetEnableTreasure(485, enable); //Red Cabbage Seeds
            }

            if (treasureGroups[TREASURE_GROUP.FallSeeds].Enabled)
            {
                treasureGroups[TREASURE_GROUP.FallSeeds].SetEnableTreasure(489, enable); //Artichoke Seeds
            }
        }

        private void EnableSeasonSeeds(bool enableSpring, bool enableSummer, bool enableFall, bool enableWinter)
        {
            double count = Convert.ToDouble(enableSpring) + Convert.ToDouble(enableSummer) + Convert.ToDouble(enableFall) + Convert.ToDouble(enableWinter);

            treasureGroups[TREASURE_GROUP.SpringSeeds].SetEnableFlagOnAllTreasures(enableSpring);
            treasureGroups[TREASURE_GROUP.SummerSeeds].SetEnableFlagOnAllTreasures(enableSummer);
            treasureGroups[TREASURE_GROUP.FallSeeds].SetEnableFlagOnAllTreasures(enableFall);
            treasureGroups[TREASURE_GROUP.WinterSeeds].SetEnableFlagOnAllTreasures(enableWinter);

            if( enableSpring)
            {
                SetSeedGroupChance(TREASURE_GROUP.SpringSeeds,  count);
            }
            if (enableSummer)
            {
                SetSeedGroupChance(TREASURE_GROUP.SummerSeeds, count);
            }
            if (enableFall)
            {
                SetSeedGroupChance(TREASURE_GROUP.FallSeeds, count);
            }
            if (enableWinter)
            {
                SetSeedGroupChance(TREASURE_GROUP.WinterSeeds, count);
            }
        }

        private void SetSeedGroupChance(TREASURE_GROUP group, double count)
        {
            if (!treasureGroups[group].ManualOverride && count > 0)
            {
                treasureGroups[group].GroupChance = .05 / count;
            }           
        }

        private void CreatePanningSpot(GameLocation location)
        {
            Random random = new Random(Game1.timeOfDay + (int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed);
            List<Point> possibleTiles = openWaterTiles[location.Name];
            if (possibleTiles.Count != 0)
            {
                for (int i = 0; i < 4; i++) // should only ever need to try once, but just in case...
                {
                    int indx = random.Next(0, possibleTiles.Count);
                    Point newOrePoint = possibleTiles[indx];

                    // Double check to make sure it's a valid point/tile
                    if (location.isOpenWater(newOrePoint.X, newOrePoint.Y) && FishingRod.distanceToLand(newOrePoint.X, newOrePoint.Y, location) <= 0)
                    {

                        if (Game1.player.currentLocation.Equals(location) && config.enableSplashSounds)
                        {
                            location.playSound("slosh");
                        }

                        location.orePanPoint.Value = newOrePoint;
                        hasPanningSpot = true;
                        playerPannedSpot = false;
                        modCreatedPanningSpot[location] = true;

                        if (i > 0)
                        {
                            this.Monitor.Log($"Had to loop... check data file {location}.json");
                        }
                        break;
                    }
                }
            }
        }

        private bool HasFoundAllArtifacts()
        {
            for (int i = 96; i < 128; i++)
            {
                if (!Game1.player.archaeologyFound.ContainsKey(i))
                {
                    return false;
                }
            }

            for (int i = 579; i < 590; i++)
            {
                if (!Game1.player.archaeologyFound.ContainsKey(i))
                {
                    return false;
                }
            }
            return true;
        }

        private void UpdatePossibleTiles(GameLocation currentLocation)
        {
            if (!openWaterTiles.ContainsKey(currentLocation.Name))
            {
                string file = Path.Combine("DataFiles", $"{currentLocation.Name}.json");
                if ( currentLocation.Name == "Farm")  // Special Case
                {
                    file = Path.Combine("DataFiles", farmFile);
                }
                List<Point> possibleTiles = this.Helper.Data.ReadJsonFile<List<Point>>(file);

                if (possibleTiles == null) //No file was found...
                {
                    possibleTiles = new List<Point>();

                    int maxWidth = currentLocation.Map.GetLayer("Back").LayerWidth;
                    int maxHeight = currentLocation.Map.GetLayer("Back").LayerHeight;
                    for (int width = 0; width < maxWidth; width++)
                    {
                        for (int height = 0; height < maxHeight; height++)
                        {
                            Point possibleOrePoint = new Point(width, height);
                            if (currentLocation.isOpenWater(width, height) && FishingRod.distanceToLand(width, height, currentLocation) <= 0)
                            {
                                possibleTiles.Add(possibleOrePoint);
                            }
                        }
                    }
                    if (!currentLocation.Name.Contains("UndergroundMine"))
                    {
                        this.Helper.Data.WriteJsonFile(file, possibleTiles); // Write out new file since we had to try and find spawn points.
                    }
                    else if (currentLocation.Name == "UndergroundMine20" 
                        || currentLocation.Name == "UndergroundMine60" 
                        || currentLocation.Name == "UndergroundMine100")
                    {
                        this.Helper.Data.WriteJsonFile(file, possibleTiles); // The only mine levels with water
                    }

                    
                }
                openWaterTiles.Add(currentLocation.Name, possibleTiles);
            }
        }

        private string GetOreRelativePostion(Point orePoint)
        {
            if (orePoint.X == Game1.player.getTileX())
            {
                return (orePoint.Y < Game1.player.getTileY()) ? "N" : "S";
            }

            if (orePoint.Y == Game1.player.getTileY())
            {
                return (orePoint.X < Game1.player.getTileX()) ? "W" : "E";
            }

            if (orePoint.X < Game1.player.getTileX())
            {
                return (orePoint.Y < Game1.player.getTileY()) ? "NW" : "SW";
            }
            else
            {
                return (orePoint.Y < Game1.player.getTileY()) ? "NE" : "SE";
            }
        }

        private long GetDistanceToOre(Point orePoint)
        {
            return (long) Math.Round(Math.Sqrt(Math.Pow(orePoint.X - Game1.player.getTileX(), 2) + Math.Pow(orePoint.Y - Game1.player.getTileY(), 2)));
        }
    }
}