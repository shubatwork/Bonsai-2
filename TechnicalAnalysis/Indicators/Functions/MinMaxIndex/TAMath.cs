﻿using TechnicalAnalysis.Common;

// ReSharper disable once CheckNamespace
namespace TechnicalAnalysis
{
    public static partial class TAMath
    {
        public static MinMaxIndexResult MinMaxIndex(int startIdx, int endIdx, double[] real, int timePeriod)
        {
            int outBegIdx = default;
            int outNBElement = default;
            int[] outMinIdx = new int[endIdx - startIdx + 1];
            int[] outMaxIdx = new int[endIdx - startIdx + 1];

            RetCode retCode = TACore.MinMaxIndex(
                startIdx,
                endIdx,
                real,
                timePeriod,
                ref outBegIdx,
                ref outNBElement,
                ref outMinIdx,
                ref outMaxIdx);
            
            return new(retCode, outBegIdx, outNBElement, outMinIdx, outMaxIdx);
        }
        
        public static MinMaxIndexResult MinMaxIndex(int startIdx, int endIdx, double[] real)
            => MinMaxIndex(startIdx, endIdx, real, 30);

        public static MinMaxIndexResult MinMaxIndex(int startIdx, int endIdx, float[] real, int timePeriod)
            => MinMaxIndex(startIdx, endIdx, real.ToDouble(), timePeriod);        

        public static MinMaxIndexResult MinMaxIndex(int startIdx, int endIdx, float[] real)
            => MinMaxIndex(startIdx, endIdx, real, 30);
    }
}
