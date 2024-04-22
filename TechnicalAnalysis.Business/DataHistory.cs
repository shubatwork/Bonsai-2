using System;
using System.Collections.Generic;
using System.Linq;
using TechnicalAnalysis.Common;

namespace TechnicalAnalysis.Business
{
    public class DataHistory
    {
        private readonly List<Candle> _candles;

        public DataHistory(List<Candle> candles)
        {
            if (candles == null)
            {
                throw new ArgumentNullException(nameof(candles));
            }

            if (candles.Count == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(candles));
            }

            _candles = candles;

            Count = candles.Count;
            Open = candles.Select(x => x.Open).ToArray();
            High = candles.Select(x => x.High).ToArray();
            Low = candles.Select(x => x.Low).ToArray();
            Close = candles.Select(x => x.Close).ToArray();
            Volume = candles.Select(x => x.Volumefrom).ToArray();

            Average = candles
                .Select(x => (x.Open + x.High + x.Low + x.Close) / 4)
                .ToArray();

            Indicators = new Dictionary<Indicator, IndicatorBase>();
        }

        public int Count { get; }

        public double[] Open { get; }

        public double[] High { get; }

        public double[] Low { get; }

        public double[] Close { get; }

        public double[] Volume { get; }

        public double[] Average { get; }

        public Dictionary<Indicator, IndicatorBase> Indicators { get; }

        public Dictionary<string, double[]> CalculateBollingerBands(int period, double standardDeviationMultiplier)
        {
            if (period <= 0)
            {
                throw new ArgumentException("Period must be greater than zero.", nameof(period));
            }

            if (standardDeviationMultiplier <= 0)
            {
                throw new ArgumentException("Standard deviation multiplier must be greater than zero.", nameof(standardDeviationMultiplier));
            }

            Dictionary<string, double[]> bollingerBands = new Dictionary<string, double[]>();

            int dataSize = _candles.Count;

            double[] middleBand = new double[dataSize];
            double[] upperBand = new double[dataSize];
            double[] lowerBand = new double[dataSize];

            for (int i = period - 1; i < dataSize; i++)
            {
                double[] prices = Close.Skip(i - period + 1).Take(period).ToArray();
                double mean = prices.Average();
                double stdDev = CalculateStandardDeviation(prices, mean);

                middleBand[i] = mean;
                upperBand[i] = mean + stdDev * standardDeviationMultiplier;
                lowerBand[i] = mean - stdDev * standardDeviationMultiplier;
            }

            bollingerBands.Add("MiddleBand", middleBand);
            bollingerBands.Add("UpperBand", upperBand);
            bollingerBands.Add("LowerBand", lowerBand);

            return bollingerBands;
        }

        // Helper method to calculate standard deviation
        private double CalculateStandardDeviation(double[] values, double mean)
        {
            double sumOfSquares = values.Sum(v => Math.Pow(v - mean, 2));
            return Math.Sqrt(sumOfSquares / values.Length);
        }
    }
}
