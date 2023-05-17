﻿using TechnicalAnalysis.Common;

// ReSharper disable once CheckNamespace
namespace TechnicalAnalysis
{
    public static partial class TAMath
    {
        public static EmaResult Ema(int startIdx, int endIdx, double[] real, int timePeriod)
        {
            int outBegIdx = default;
            int outNBElement = default;
            double[] outReal = new double[endIdx - startIdx + 1];

            RetCode retCode = TACore.Ema(startIdx, endIdx, real, timePeriod, ref outBegIdx, ref outNBElement, ref outReal);
            
            return new(retCode, outBegIdx, outNBElement, outReal);
        }

        public static EmaResult Ema(int startIdx, int endIdx, double[] real)
            => Ema(startIdx, endIdx, real, 30);

        public static EmaResult Ema(int startIdx, int endIdx, float[] real, int timePeriod)
            => Ema(startIdx, endIdx, real.ToDouble(), timePeriod);
        
        public static EmaResult Ema(int startIdx, int endIdx, float[] real)
            => Ema(startIdx, endIdx, real, 30);
    }
}
