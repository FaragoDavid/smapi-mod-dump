using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

using TrendyHaley.Framework;


namespace TrendyHaley {
    public class ModEntry : Mod, IAssetEditor {
        private bool hasColdWeatherHaley_;
        private ModConfig config_;
        private Color actualHairColor_;

        public override void Entry(IModHelper helper) {
            this.Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            this.Helper.Events.GameLoop.SaveLoaded   += OnSaveLoaded;
            this.Helper.Events.GameLoop.DayStarted   += OnDayStarted;
        }

        /// <summary>Implements <see cref="IAssetEditor.CanEdit"/>.</summary>
        public bool CanEdit<T>(IAssetInfo asset) {
            return asset.AssetNameEquals("Characters/Haley") || asset.AssetNameEquals("Portraits/Haley");
        }

        /// <summary>Implements <see cref="IAssetEditor.Edit"/>.</summary>
        public void Edit<T>(IAssetData asset) {
            if (asset.AssetNameEquals("Characters/Haley") || asset.AssetNameEquals("Portraits/Haley")) {
                this.Monitor.Log($"Edit asset {asset.AssetName}");

                IAssetDataForImage baseImage = asset.AsImage();
                // Support for Cold Wether Haley.
                Texture2D overlay = hasColdWeatherHaley_ && Game1.IsWinter
                    ? this.Helper.Content.Load<Texture2D>($"assets/{asset.AssetName}_winter_overlay_hair_gray.png")
                    : this.Helper.Content.Load<Texture2D>($"assets/{asset.AssetName}_overlay_hair_gray.png");

                // Workaround for the missing sleeping sprite of Cold Weather Haley.
                if (hasColdWeatherHaley_ && Game1.IsWinter && asset.AssetNameEquals("Characters/Haley")) {
                    Texture2D sleepingHaley = this.Helper.Content.Load<Texture2D>($"assets/{asset.AssetName}_sleeping.png");
                    baseImage.PatchImage(sleepingHaley, patchMode: PatchMode.Overlay);
                }

                baseImage.PatchImage(ColorBlend(overlay, actualHairColor_), patchMode: PatchMode.Overlay);
            }
            else {
                throw new ArgumentException($"Invalid asset {asset.AssetName}");
            }
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e) {
            // Check for ColdWeatherHaley CP mod.
            hasColdWeatherHaley_ = this.Helper.ModRegistry.IsLoaded("NanoGamer7.ColdWeatherHaley");
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e) {
            // Read persisted config.
            config_ = this.Helper.ReadConfig<ModConfig>();
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e) {
            // Create a config entry for this save game if necessary.
            string saveGameName = $"{Game1.GetSaveGameName()}_{Game1.uniqueIDForThisGame}";
            if (!config_.SaveGame.ContainsKey(saveGameName)) {
                config_.SaveGame.Add(saveGameName, new ConfigEntry());
            }

            // First day of season or color unset.
            if (Game1.dayOfMonth == 1 || config_.SaveGame[saveGameName].HairColor == Color.Transparent) {
                // Get a new hair color for Haley.
                config_.SaveGame[saveGameName].HairColor = RandomColor();
                // Save config.
                this.Helper.WriteConfig(config_);

                SetHairColor(config_.SaveGame[saveGameName].HairColor);
                this.Monitor.Log($"Haley chose a new hair color for this season: {config_.SaveGame[saveGameName].HairColor}");

                return;
            }

            if (config_.SaveGame[saveGameName].ColorIsFading) {
                // The color gets brighter day by day so at season's end the color multiplier is white.
                Color baseColor = config_.SaveGame[saveGameName].HairColor;
                Color fadedColor
                    = new Color((byte) (baseColor.R + (255 - baseColor.R) * (float) (Game1.dayOfMonth - 1) / 27.0f),
                                (byte) (baseColor.G + (255 - baseColor.G) * (float) (Game1.dayOfMonth - 1) / 27.0f),
                                (byte) (baseColor.B + (255 - baseColor.B) * (float) (Game1.dayOfMonth - 1) / 27.0f));

                SetHairColor(fadedColor);
                this.Monitor.Log($"Haley's hair color faded: {fadedColor}");
            }
        }

        /// <summary>Sets color and triggers sprite reload.</summary>
        private void SetHairColor(Color hairColor) {
            actualHairColor_ = hairColor;

            this.Helper.Content.InvalidateCache("Characters/Haley");
            this.Helper.Content.InvalidateCache("Portraits/Haley");
        }

        /// <summary>Random color (always full opaque).</summary>
        private static Color RandomColor() {
            int R = Game1.random.Next(0, 255);
            int G = Game1.random.Next(0, 255);
            int B = Game1.random.Next(0, 255);

            return new Color(R, G, B);
        }

        /// <summary>Color blending (multiplication).</summary>
        private Texture2D ColorBlend(Texture2D source, Color blendColor) {
            Color[] sourcePixels = new Color[source.Width * source.Height];
            source.GetData(sourcePixels);
            for (int i = 0; i < sourcePixels.Length; i++) {
                sourcePixels[i]
                    = new Color((byte) (sourcePixels[i].R * blendColor.R / 255),
                                (byte) (sourcePixels[i].G * blendColor.G / 255),
                                (byte) (sourcePixels[i].B * blendColor.B / 255),
                                (byte) (sourcePixels[i].A * blendColor.A / 255));
            }

            Texture2D blended = new Texture2D(Game1.graphics.GraphicsDevice, source.Width, source.Height);
            blended.SetData(sourcePixels);

            return blended;
        }
    }
}