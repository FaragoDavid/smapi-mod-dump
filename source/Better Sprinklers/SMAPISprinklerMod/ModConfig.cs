﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace BetterSprinklers
{
    public class SprinklerModConfig
    {
        /*********
        ** Accessors
        *********/
        public SButton ConfigKey { get; set; } = SButton.K;
        public SButton HighlightKey { get; set; } = SButton.F3;
        public Color GridColour { get; set; } = Color.PowderBlue;
        public Dictionary<int, int[,]> SprinklerShapes { get; set; } = new Dictionary<int, int[,]>
        {
            [599] = new[,]
            {
                { 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 1, 0, 0, 0 },
                { 0, 0, 0, 2, 0, 0, 0 },
                { 0, 1, 2, 2, 2, 1, 0 },
                { 0, 0, 0, 2, 0, 0, 0 },
                { 0, 0, 0, 1, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0 }
            },
            [621] = new[,]
            {
                { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 2, 2, 2, 0, 0, 0, 0 },
                { 0, 0, 1, 1, 2, 2, 2, 1, 1, 0, 0 },
                { 0, 0, 0, 0, 2, 2, 2, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
            },
            [645] = new[,]
            {
                { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0 },
                { 0, 1, 1, 1, 1, 2, 2, 2, 2, 2, 1, 1, 1, 1, 0 },
                { 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
            }
        };
        public Dictionary<int, int> SprinklerPrices { get; set; } = new Dictionary<int, int>
        {
            [599] = 1,
            [621] = 1,
            [645] = 1
        };
    }
}
