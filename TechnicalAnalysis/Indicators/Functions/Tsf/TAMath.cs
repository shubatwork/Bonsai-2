﻿using TechnicalAnalysis.Common;

// ReSharper disable once CheckNamespace
namespace TechnicalAnalysis
{
    public static partial class TAMath
    {
        public static TsfResult Tsf(int startIdx, int endIdx, double[] real, int timePeriod)
        {
            int outBegIdx = default;
            int outNBElement = default;
            double[] outReal = new double[endIdx - startIdx + 1];

            RetCode retCode = TACore.Tsf(startIdx, endIdx, real, timePeriod, ref outBegIdx, ref outNBElement, ref outReal);
            
            return new(retCode, outBegIdx, outNBElement, outReal);
        }

        public static TsfResult Tsf(int startIdx, int endIdx, double[] real)
            => Tsf(startIdx, endIdx, real, 14);

        public static TsfResult Tsf(int startIdx, int endIdx, float[] real, int timePeriod)
            => Tsf(startIdx, endIdx, real.ToDouble(), timePeriod);
            
        public static TsfResult Tsf(int startIdx, int endIdx, float[] real)
            => Tsf(startIdx, endIdx, real, 14);
    }
}
