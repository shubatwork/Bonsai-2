﻿using TechnicalAnalysis.Common;

// ReSharper disable once CheckNamespace
namespace TechnicalAnalysis
{
    public static partial class TAMath
    {
        public static PpoResult Ppo(int startIdx, int endIdx, double[] real, int fastPeriod, int slowPeriod, MAType maType)
        {
            int outBegIdx = default;
            int outNBElement = default;
            double[] outReal = new double[endIdx - startIdx + 1];

            RetCode retCode = TACore.Ppo(
                startIdx,
                endIdx,
                real,
                fastPeriod,
                slowPeriod,
                maType,
                ref outBegIdx,
                ref outNBElement,
                ref outReal);
            
            return new(retCode, outBegIdx, outNBElement, outReal);
        }

        public static PpoResult Ppo(int startIdx, int endIdx, double[] real)
            => Ppo(startIdx, endIdx, real, 12, 26, MAType.Sma);

        public static PpoResult Ppo(int startIdx, int endIdx, float[] real, int fastPeriod, int slowPeriod, MAType maType)
            => Ppo(startIdx, endIdx, real.ToDouble(), fastPeriod, slowPeriod, maType);
        
        public static PpoResult Ppo(int startIdx, int endIdx, float[] real)
            => Ppo(startIdx, endIdx, real, 12, 26, MAType.Sma);
    }
}
