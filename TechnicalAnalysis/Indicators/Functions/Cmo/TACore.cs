using System;
using TechnicalAnalysis.Common;

namespace TechnicalAnalysis
{
    internal static partial class TACore
    {
        public static RetCode Cmo(
            int startIdx,
            int endIdx,
            in double[] inReal,
            in int optInTimePeriod,
            ref int outBegIdx,
            ref int outNBElement,
            ref double[] outReal)
        {
            if (startIdx < 0)
            {
                return RetCode.OutOfRangeStartIndex;
            }

            if (endIdx < 0 || endIdx < startIdx)
            {
                return RetCode.OutOfRangeEndIndex;
            }

            if (inReal == null)
            {
                return RetCode.BadParam;
            }

            if (optInTimePeriod is < 2 or > 100000)
            {
                return RetCode.BadParam;
            }

            if (outReal == null)
            {
                return RetCode.BadParam;
            }

            outBegIdx = 0;
            outNBElement = 0;
            int lookbackTotal = CmoLookback(optInTimePeriod);
            if (startIdx < lookbackTotal)
            {
                startIdx = lookbackTotal;
            }

            if (startIdx <= endIdx)
            {
                double prevLoss;
                double prevGain;
                double tempValue1;
                double tempValue2;
                int i;
                int outIdx = 0;
                if (optInTimePeriod == 1)
                {
                    outBegIdx = startIdx;
                    i = endIdx - startIdx + 1;
                    outNBElement = i;
                    Array.Copy(inReal, startIdx, outReal, 0, i);
                    return RetCode.Success;
                }

                int today = startIdx - lookbackTotal;
                double prevValue = inReal[today];
                if (Globals.unstablePeriod[3] == 0 && Globals.compatibility == Compatibility.Metastock)
                {
                    double savePrevValue = prevValue;
                    prevGain = 0.0;
                    prevLoss = 0.0;
                    for (i = optInTimePeriod; i > 0; i--)
                    {
                        tempValue1 = inReal[today];
                        today++;
                        tempValue2 = tempValue1 - prevValue;
                        prevValue = tempValue1;
                        if (tempValue2 < 0.0)
                        {
                            prevLoss -= tempValue2;
                        }
                        else
                        {
                            prevGain += tempValue2;
                        }
                    }

                    tempValue1 = prevLoss / optInTimePeriod;
                    tempValue2 = prevGain / optInTimePeriod;
                    double tempValue3 = tempValue2 - tempValue1;
                    double tempValue4 = tempValue1 + tempValue2;
                    outReal[outIdx] = 100.0 * (tempValue3 / tempValue4);
                    outIdx++;

                    if (today > endIdx)
                    {
                        outBegIdx = startIdx;
                        outNBElement = outIdx;
                        return RetCode.Success;
                    }

                    today -= optInTimePeriod;
                    prevValue = savePrevValue;
                }

                prevGain = 0.0;
                prevLoss = 0.0;
                today++;
                for (i = optInTimePeriod; i > 0; i--)
                {
                    tempValue1 = inReal[today];
                    today++;
                    tempValue2 = tempValue1 - prevValue;
                    prevValue = tempValue1;
                    if (tempValue2 < 0.0)
                    {
                        prevLoss -= tempValue2;
                    }
                    else
                    {
                        prevGain += tempValue2;
                    }
                }

                prevLoss /= optInTimePeriod;
                prevGain /= optInTimePeriod;
                if (today > startIdx)
                {
                    tempValue1 = prevGain + prevLoss;
                    outReal[outIdx] = 100.0 * ((prevGain - prevLoss) / tempValue1);
                    outIdx++;
                }
                else
                {
                    while (today < startIdx)
                    {
                        tempValue1 = inReal[today];
                        tempValue2 = tempValue1 - prevValue;
                        prevValue = tempValue1;
                        prevLoss *= optInTimePeriod - 1;
                        prevGain *= optInTimePeriod - 1;
                        if (tempValue2 < 0.0)
                        {
                            prevLoss -= tempValue2;
                        }
                        else
                        {
                            prevGain += tempValue2;
                        }

                        prevLoss /= optInTimePeriod;
                        prevGain /= optInTimePeriod;
                        today++;
                    }
                }

                while (today <= endIdx)
                {
                    tempValue1 = inReal[today];
                    today++;
                    tempValue2 = tempValue1 - prevValue;
                    prevValue = tempValue1;
                    prevLoss *= optInTimePeriod - 1;
                    prevGain *= optInTimePeriod - 1;
                    if (tempValue2 < 0.0)
                    {
                        prevLoss -= tempValue2;
                    }
                    else
                    {
                        prevGain += tempValue2;
                    }

                    prevLoss /= optInTimePeriod;
                    prevGain /= optInTimePeriod;
                    tempValue1 = prevGain + prevLoss;
                    outReal[outIdx] = 100.0 * ((prevGain - prevLoss) / tempValue1);
                    outIdx++;
                }

                outBegIdx = startIdx;
                outNBElement = outIdx;
            }

            return RetCode.Success;
        }

        public static int CmoLookback(int optInTimePeriod)
        {
            if (optInTimePeriod is < 2 or > 100000)
            {
                return -1;
            }

            int retValue = optInTimePeriod + (int)Globals.unstablePeriod[3];
            if (Globals.compatibility == Compatibility.Metastock)
            {
                retValue--;
            }

            return retValue;
        }
    }
}
