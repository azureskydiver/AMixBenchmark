using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace AMixBenchmark
{
    public static class ArrayMaker
    {
        static Random _random = new Random(123);

        public static double[] MakeRandomOneDimensionalArray(int n)
            => Enumerable.Range(0, n)
                         .Select(i => _random.NextDouble())
                         .ToArray();

        public static double[,] MakeRandomTwoDimensionalArray(int n)
        {
            var arr = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    arr[i, j] = _random.NextDouble();
            return arr;
        }
    }

    public class Bench
    {
        [Params(50, 100)]
        public int _n;
        double [] _xy;
        double [] _ai;
        double [,] _kijm;
        double [,] _aij;
        double [] _sumai;

        [GlobalSetup]
        public void Setup()
        {
            _xy = ArrayMaker.MakeRandomOneDimensionalArray(_n);
            _ai = ArrayMaker.MakeRandomOneDimensionalArray(_n);
            _kijm = ArrayMaker.MakeRandomTwoDimensionalArray(_n);
            _aij = new double[_n, _n];
            _sumai = new double[_n];
        }

        [Benchmark] public double CalcAMix() => CalcAmixInner(_n, _xy, _ai, _kijm, _aij, _sumai);
        [Benchmark(Baseline = true)] public double CalcAMixArrays() => CalcAmixArraysInner(_n, _xy, _ai, _kijm, _aij, _sumai);
        [Benchmark] public double CalcAMixArrays2() => CalcAmixArraysInner2(_n, _xy, _ai, _kijm, _aij, _sumai);
        [Benchmark] public double CalcAMixArrays3() => CalcAmixArraysInner3(_n, _xy, _ai, _kijm, _aij, _sumai);
        [Benchmark] public double CalcAMixSpans() => CalcAmixSpansInner(_n, _xy, _ai, _kijm, _aij, _sumai);
        [Benchmark] public double CalcAMixSpans2() => CalcAmixSpansInner2(_n, _xy, _ai, _kijm, _aij, _sumai);
        [Benchmark] public double CalcAMixSpans3() => CalcAmixSpansInner3(_n, _xy, _ai, _kijm, _aij, _sumai);
        [Benchmark] public double CalcAMixMirroredArrays() => CalcAmixMirroredArraysInner(_n, _xy, _ai, _kijm, _aij, _sumai);
        [Benchmark] public double CalcAMixMirroredArrays2() => CalcAmixMirroredArraysInner2(_n, _xy, _ai, _kijm, _aij, _sumai);
        [Benchmark] public double CalcAMixMirroredArrays3() => CalcAmixMirroredArraysInner3(_n, _xy, _ai, _kijm, _aij, _sumai);
        [Benchmark] public double CalcAMixMirroredSpans() => CalcAmixMirroredSpansInner(_n, _xy, _ai, _kijm, _aij, _sumai);
        [Benchmark] public double CalcAMixMirroredSpans2() => CalcAmixMirroredSpansInner2(_n, _xy, _ai, _kijm, _aij, _sumai);
        [Benchmark] public double CalcAMixMirroredSpans3() => CalcAmixMirroredSpansInner3(_n, _xy, _ai, _kijm, _aij, _sumai);
        [Benchmark] public double CalcAMixMirroredSpans3a() => CalcAmixMirroredSpansInner3a(_n, _xy, _ai, _kijm, _aij, _sumai);
        [Benchmark] public double CalcAMixMirroredPointers3a() => CalcAmixMirroredPointersInner3a(_n, _xy, _ai, _kijm, _aij, _sumai);

        static public unsafe double CalcAmixInner(int n, double[] XY, double[] ai, double[,] kijm, double[,] Aij, double[] sumAi)
        {
            double amix = 0;
            int UpperBound0 = Aij.GetUpperBound(0);
            int UpperBound1 = Aij.GetUpperBound(1);

            fixed (double* ptr = Aij, aiPtr = ai, kijmPtr = kijm)
            {
                double* element = ptr;
                double* kijelement = kijmPtr;
                double* aielement = aiPtr;
                for (int i = 0; i <= UpperBound0; i++)
                {
                    for (int j = 0; j <= UpperBound1; j++)
                    {
                        if (i == j)
                            *element = *aielement;
                        else
                            *element = Math.Sqrt(*(aiPtr + i) * *(aiPtr + j)) * (1 - *kijelement);

                        element++;
                        kijelement++;
                    }

                    aielement++;
                }
            }

            fixed (double* ptrAij = Aij, ptrXY = XY, ptrsumAI = sumAi)
            {
                double* elementAij = ptrAij;
                double* elementsumAI = ptrsumAI;
                double* elementXy = ptrXY;

                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        *elementsumAI += *elementXy * *elementAij;
                        elementXy++;
                        elementAij++;
                    }

                    amix += *elementsumAI * *(ptrXY + i);
                    elementXy = ptrXY;
                    elementsumAI++;
                }
            }
            return amix;
        }

        static public double CalcAmixArraysInner(int n, double[] XY, double[] ai, double[,] kijm, double[,] Aij, double[] sumAi)
        {
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                        Aij[i, j] = ai[i];
                    else
                        Aij[i, j] = Math.Sqrt(ai[i] * ai[j]) * (1 - kijm[i, j]);
                }
            }

            double amix = 0;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                    sumAi[i] += XY[j] * Aij[i, j];

                amix += sumAi[i] * XY[i];
            }
            return amix;
        }

        static public double CalcAmixArraysInner2(int n, double[] XY, double[] ai, double[,] kijm, double[,] Aij, double[] sumAi)
        {
            for (int i = 0; i < n; i++)
            {
                var aii = ai[i];
                for (int j = 0; j < n; j++)
                    Aij[i, j] = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);

                Aij[i, i] = aii;
            }

            double amix = 0;
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int j = 0; j < n; j++)
                    sum += XY[j] * Aij[i, j];

                sumAi[i] = sum;
                amix += sum * XY[i];
            }
            return amix;
        }

        static public double CalcAmixArraysInner3(int n, double[] XY, double[] ai, double[,] kijm, double[,] Aij, double[] sumAi)
        {
            double amix = 0;

            for (int i = 0; i < n; i++)
            {
                var aii = ai[i];
                double sum = XY[i] * aii;

                for (int j = 0; j < n; j++)
                {
                    var value = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);
                    Aij[i, j] = value;
                    sum += XY[j] * value;
                }

                sum -= XY[i] * Aij[i, i];
                Aij[i, i] = aii;

                sumAi[i] = sum;
                amix += sum * XY[i];
            }

            return amix;
        }

        static public double CalcAmixSpansInner(int n,
                                                ReadOnlySpan<double> XY,
                                                ReadOnlySpan<double> ai,
                                                double[,] kijm,
                                                double[,] Aij,
                                                Span<double> sumAi)
        {
            for (int i = 0; i < n; i++)
            {
                var aii = ai[i];
                for (int j = 0; j < n; j++)
                    Aij[i, j] = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);

                Aij[i, i] = aii;
            }

            double amix = 0;
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int j = 0; j < n; j++)
                    sum += XY[j] * Aij[i, j];

                sumAi[i] = sum;
                amix += sum * XY[i];
            }
            return amix;
        }

        static public unsafe double CalcAmixSpansInner2(int n,
                                                 ReadOnlySpan<double> XY,
                                                 ReadOnlySpan<double> ai,
                                                 double[,] kijm,
                                                 double[,] Aij,
                                                 Span<double> sumAi)
        {
            fixed (double* kijmPtr = kijm, AijPtr = Aij)
            {
                var kijmSpan = new ReadOnlySpan<double>(kijmPtr, kijm.Length);
                var kijmRowISpan = kijmSpan.Slice(0);
                var AijSpan = new Span<double>(AijPtr, Aij.Length);
                var AijRowISpan = AijSpan.Slice(0);

                for (int i = 0; i < n; i++)
                {
                    var aii = ai[i];
                    for (int j = 0; j < n; j++)
                        AijRowISpan[j] = Math.Sqrt(aii * ai[j]) * (1 - kijmRowISpan[j]);

                    AijRowISpan[i] = aii;
                    kijmRowISpan = kijmRowISpan.Slice(n);
                    AijRowISpan = AijRowISpan.Slice(n);
                }

                double amix = 0;
                AijRowISpan = AijSpan.Slice(0);
                for (int i = 0; i < n; i++)
                {
                    double sum = 0;
                    for (int j = 0; j < n; j++)
                        sum += XY[j] * AijRowISpan[j];

                    sumAi[i] = sum;
                    amix += sum * XY[i];
                    AijRowISpan = AijRowISpan.Slice(n);
                }
                return amix;
            }
        }

        static public unsafe double CalcAmixSpansInner3(int n,
                                                        ReadOnlySpan<double> XY,
                                                        ReadOnlySpan<double> ai,
                                                        double[,] kijm,
                                                        double[,] Aij,
                                                        Span<double> sumAi)
        {
            fixed (double* kijmPtr = kijm, AijPtr = Aij)
            {
                var kijmSpan = new ReadOnlySpan<double>(kijmPtr, kijm.Length);
                var kijmRowISpan = kijmSpan.Slice(0);
                var AijSpan = new Span<double>(AijPtr, Aij.Length);
                var AijRowISpan = AijSpan.Slice(0);
                double amix = 0;

                for (int i = 0; i < n; i++)
                {
                    var aii = ai[i];
                    double sum = XY[i] * aii;

                    for (int j = 0; j < n; j++)
                    {
                        var value = Math.Sqrt(aii * ai[j]) * (1 - kijmRowISpan[j]);
                        AijRowISpan[j] = value;
                        sum += XY[j] * value;
                    }

                    sum -= XY[i] * AijRowISpan[i];
                    AijRowISpan[i] = aii;

                    sumAi[i] = sum;
                    amix += sum * XY[i];

                    kijmRowISpan = kijmRowISpan.Slice(n);
                    AijRowISpan = AijRowISpan.Slice(n);
                }

                return amix;
            }
        }

        static public double CalcAmixMirroredArraysInner(int n, double[] XY, double[] ai, double[,] kijm, double[,] Aij, double[] sumAi)
        {
            for (int i = 0; i < n; i++)
            {
                Aij[i, i] = ai[i];
                for (int j = i + 1; j < n; j++)
                {
                    Aij[i, j] = Math.Sqrt(ai[i] * ai[j]) * (1 - kijm[i, j]);
                    Aij[j, i] = Aij[i, j];
                }
            }

            double amix = 0;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                    sumAi[i] += XY[j] * Aij[i, j];

                amix += sumAi[i] * XY[i];
            }
            return amix;
        }

        static public double CalcAmixMirroredArraysInner2(int n, double[] XY, double[] ai, double[,] kijm, double[,] Aij, double[] sumAi)
        {
            for (int i = 0; i < n; i++)
            {
                var aii = ai[i];
                Aij[i, i] = aii;
                for (int j = i + 1; j < n; j++)
                {
                    var value = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);
                    Aij[i, j] = value;
                    Aij[j, i] = value;
                }
            }

            double amix = 0;
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int j = 0; j < n; j++)
                    sum += XY[j] * Aij[i, j];

                sumAi[i] = sum;
                amix += sum * XY[i];
            }
            return amix;
        }

        static public double CalcAmixMirroredArraysInner3(int n, double[] XY, double[] ai, double[,] kijm, double[,] Aij, double[] sumAi)
        {
            double amix = 0;

            for (int i = 0; i < n; i++)
            {
                double sum = 0;

                for (int j = 0; j < i; j++)
                    sum += XY[j] * Aij[i, j];

                var aii = ai[i];
                Aij[i, i] = aii;
                sum += XY[i] * aii;

                for (int j = i + 1; j < n; j++)
                {
                    var value = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);
                    Aij[i, j] = value;
                    Aij[j, i] = value;
                    sum += XY[j] * value;
                }

                sumAi[i] = sum;
                amix += sum * XY[i];
            }

            return amix;
        }

        static public double CalcAmixMirroredSpansInner(int n,
                                                        ReadOnlySpan<double> XY,
                                                        ReadOnlySpan<double> ai,
                                                        double[,] kijm,
                                                        double[,] Aij,
                                                        Span<double> sumAi)
        {
            for (int i = 0; i < n; i++)
            {
                Aij[i, i] = ai[i];
                for (int j = i + 1; j < n; j++)
                {
                    Aij[i, j] = Math.Sqrt(ai[i] * ai[j]) * (1 - kijm[i, j]);
                    Aij[j, i] = Aij[i, j];
                }
            }

            double amix = 0;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                    sumAi[i] += XY[j] * Aij[i, j];

                amix += sumAi[i] * XY[i];
            }
            return amix;
        }

        static public double CalcAmixMirroredSpansInner2(int n,
                                                         ReadOnlySpan<double> XY,
                                                         ReadOnlySpan<double> ai,
                                                         double[,] kijm,
                                                         double[,] Aij,
                                                         Span<double> sumAi)
        {
            for (int i = 0; i < n; i++)
            {
                var aii = ai[i];
                Aij[i, i] = aii;
                for (int j = i + 1; j < n; j++)
                {
                    var value = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);
                    Aij[i, j] = value;
                    Aij[j, i] = value;
                }
            }

            double amix = 0;
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int j = 0; j < n; j++)
                    sum += XY[j] * Aij[i, j];

                sumAi[i] = sum;
                amix += sum * XY[i];
            }
            return amix;
        }

        static public double CalcAmixMirroredSpansInner3(int n,
                                                         ReadOnlySpan<double> XY,
                                                         ReadOnlySpan<double> ai,
                                                         double[,] kijm,
                                                         double[,] Aij,
                                                         Span<double> sumAi)
        {
            double amix = 0;

            for (int i = 0; i < n; i++)
            {
                double sum = 0;

                for (int j = 0; j < i; j++)
                    sum += XY[j] * Aij[i, j];

                var aii = ai[i];
                Aij[i, i] = aii;
                sum += XY[i] * aii;

                for (int j = i + 1; j < n; j++)
                {
                    var value = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);
                    Aij[i, j] = value;
                    Aij[j, i] = value;
                    sum += XY[j] * value;
                }

                sumAi[i] = sum;
                amix += sum * XY[i];
            }

            return amix;
        }

        static public unsafe double CalcAmixMirroredSpansInner3a(int n,
                                                                 ReadOnlySpan<double> XY,
                                                                 ReadOnlySpan<double> ai,
                                                                 double[,] kijm,
                                                                 double[,] Aij,
                                                                 Span<double> sumAi)
        {
            fixed (double* kijmPtr = kijm, AijPtr = Aij)
            {
                var kijmSpan = new ReadOnlySpan<double>(kijmPtr, kijm.Length);
                var kijmRowISpan = kijmSpan.Slice(0);
                var AijSpan = new Span<double>(AijPtr, Aij.Length);
                var AijRowISpan = AijSpan.Slice(0);
                double amix = 0;

                for (int i = 0; i < n; i++)
                {
                    double sum = 0;

                    for (int j = 0; j < i; j++)
                        sum += XY[j] * AijRowISpan[j];

                    var aii = ai[i];
                    AijRowISpan[i] = aii;
                    sum += XY[i] * aii;

                    if (i + 1 < n)
                    {
                        var AijColJSpan = AijSpan.Slice((i + 1) * n);
                        for (int j = i + 1; j < n; j++)
                        {
                            var value = Math.Sqrt(aii * ai[j]) * (1 - kijmRowISpan[j]);
                            AijRowISpan[j] = value;
                            AijColJSpan[i] = value;
                            sum += XY[j] * value;

                            AijColJSpan = AijColJSpan.Slice(n);
                        }
                    }

                    sumAi[i] = sum;
                    amix += sum * XY[i];

                    kijmRowISpan = kijmRowISpan.Slice(n);
                    AijRowISpan = AijRowISpan.Slice(n);
                }

                return amix;
            }
        }

        static public unsafe double CalcAmixMirroredPointersInner3a(int n, double[] XY, double[] ai, double[,] kijm, double[,] Aij, double[] sumAi)
        {
            fixed (double* XYPtr = XY, aiPtr = ai, kijmPtr = kijm, AijPtr = Aij, sumAiPtr = sumAi)
            {
                var kijmRowI = kijmPtr;
                var AijDst = AijPtr;
                var sumAiDst = sumAiPtr;
                double amix = 0;

                for (int i = 0; i < n; i++)
                {
                    var XYSrc = XYPtr;
                    double sum = 0;

                    for (int j = 0; j < i; j++)
                        sum += *XYSrc++ * *AijDst++;

                    var aii = aiPtr[i];
                    *AijDst++ = aii;
                    sum += *XYSrc++ * aii;

                    var AijColJSpan = &AijPtr[(i + 1) * n + i];
                    var kijmSrc = &kijmRowI[i + 1];
                    for (int j = i + 1; j < n; j++)
                    {
                        var value = Math.Sqrt(aii * aiPtr[j]) * (1 - *kijmSrc++);
                        *AijDst++ = value;
                        *AijColJSpan = value;
                        sum += *XYSrc++ * value;

                        AijColJSpan += n;
                    }

                    *sumAiDst++ = sum;
                    amix += sum * XYPtr[i];

                    kijmRowI += n;
                }

                return amix;
            }
        }
    }

    public class BenchArray
    {
        int n = 10000;
        double[] ai;
        double[,] kijm;
        double[,] Aij;

        public BenchArray()
        {
            ai = ArrayMaker.MakeRandomOneDimensionalArray(n);
            kijm = ArrayMaker.MakeRandomTwoDimensionalArray(n);
            Aij = new double[n, n];
        }

        [Benchmark]
        public void DoAllItemsDiagonalSpecialCase()
        {
            for (int i = 0; i < n; i++)
            {
                var aii = ai[i];
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                        Aij[i, j] = aii;
                    else
                        Aij[i, j] = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);
                }
            }
        }

        [Benchmark]
        public void DoAllItemsSegmented()
        {
            for (int i = 0; i < n; i++)
            {
                var aii = ai[i];
                for (int j = 0; j < i; j++)
                    Aij[i, j] = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);

                Aij[i, i] = aii;

                for (int j = i + 1; j < n; j++)
                    Aij[i, j] = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);
            }
        }

        [Benchmark]
        public void DoAllItemsDiagonalLast()
        {
            for (int i = 0; i < n; i++)
            {
                var aii = ai[i];
                for (int j = 0; j < n; j++)
                    Aij[i, j] = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);

                Aij[i, i] = aii;
            }
        }

        [Benchmark]
        public void MirrorItemsConditional()
        {
            for (int i = 0; i < n; i++)
            {
                var aii = ai[i];
                for (int j = 0; j < n; j++)
                {
                    if (i > j)
                        Aij[j, i] = Aij[i, j];
                    else
                        Aij[i, j] = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);
                }
                Aij[i, i] = aii;
            }
        }

        [Benchmark]
        public void MirrorItemsDiagonalFirst()
        {
            for (int i = 0; i < n; i++)
            {
                var aii = ai[i];
                Aij[i, i] = aii;
                for (int j = i + 1; j < n; j++)
                {
                    var value = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);
                    Aij[i, j] = value;
                    Aij[j, i] = value;
                }
            }
        }

        [Benchmark]
        public void MirrorItemsDiagonalLast()
        {
            for (int i = 0; i < n; i++)
            {
                var aii = ai[i];
                for (int j = i + 1; j < n; j++)
                {
                    var value = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);
                    Aij[i, j] = value;
                    Aij[j, i] = value;
                }
                Aij[i, i] = aii;
            }
        }

        [Benchmark]
        public void DoJustHalf()
        {
            for (int i = 0; i < n; i++)
            {
                var aii = ai[i];
                Aij[i, i] = aii;
                for (int j = i + 1; j < n; j++)
                    Aij[i, j] = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);
            }
        }

        [Benchmark]
        public void DoJustHalfThenMirrorByColumn()
        {
            for (int i = 0; i < n; i++)
            {
                var aii = ai[i];
                Aij[i, i] = aii;
                for (int j = i + 1; j < n; j++)
                    Aij[i, j] = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);
            }

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                    Aij[j, i] = Aij[i, j];
            }
        }

        [Benchmark]
        public void DoJustHalfThenMirrorByRow()
        {
            for (int i = 0; i < n; i++)
            {
                var aii = ai[i];
                Aij[i, i] = aii;
                for (int j = i + 1; j < n; j++)
                    Aij[i, j] = Math.Sqrt(aii * ai[j]) * (1 - kijm[i, j]);
            }

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < i; j++)
                    Aij[i, j] = Aij[j, i];
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Bench>();
        }
    }
}
