using System;
using System.Collections.Generic;
using System.Linq;
using LudoApi.Models;

namespace LudoApi.Services
{
    public static class ColorPositions
    {
        public const int BoardSize = 40;

        private const int WinningSpots = 4;

        public static int StartPosition(Color color)
        {
            var colorCount = Enum.GetValues(typeof(Color)).Length;

            if ((int) color >= colorCount)
            {
                throw new IndexOutOfRangeException("Unknown color");
            }

            return (int) color * (BoardSize / colorCount);
        }

        public static bool OutsideWinningPosition(Color color, int pieceLocation)
        {
            var maxWinPos = WinPositions(color).Max(); // get the highest winning position
            return pieceLocation > maxWinPos; 
        }

        public static IEnumerable<int> WinPositions(Color color)
        {
            return Enumerable.Range(StartPosition(color) + BoardSize, WinningSpots);
        }

        // --- New Method ---
        public static int HomeEntry(Color color)
        {
            // the last square before entering the finish
            return (StartPosition(color) + BoardSize - 1) % BoardSize;
        }

                // First index in the finish line
        public static int FinishStart(Color color)
        {
            return StartPosition(color) + BoardSize;
        }

        public static bool IsInFinishLine(Color color, int from) {
            int finishStart = FinishStart(color);
            int finishEnd = finishStart + WinningSpots - 1;

            if (from >= finishStart && from <= finishEnd)
                return true;
            return false;
        }

        public static bool IsEnteringFinish(Color color, int from, int to)
        {
            int entry = HomeEntry(color);

            // not entering, already there
            if (IsInFinishLine(color, from))
                return false;
            // Normal case: move crosses the entry
            if (from <= entry && to > entry)
                return true;

            // Wrap-around case
           // if (from > entry && (to % BoardSize) <= entry)
             //   return true;

            return false;
        }

    }
}
