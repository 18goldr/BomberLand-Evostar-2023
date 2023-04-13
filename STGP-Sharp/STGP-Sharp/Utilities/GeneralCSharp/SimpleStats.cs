using System;
using System.Collections.Generic;
using System.Linq;

namespace STGP_Sharp.Utilities.GeneralCSharp
{
    public class SimpleStats
    {
        private readonly List<double> _samples;

        public SimpleStats(IEnumerable<double> samples)
        {
            this._samples = samples.ToList();
        }

        public SimpleStats()
        {
            this._samples = new List<double>();
        }

        public bool HasSamples => this._samples.Count > 0;
        public int NumSamples => this._samples.Count;

        public double Min => this.HasSamples ? this._samples.Min() : 0;
        public double Max => this.HasSamples ? this._samples.Max() : 0;
        public double StandardDeviation => Math.Sqrt(this.Variance);

        public double Mean
        {
            get
            {
                if (!this.HasSamples) return 0;

                double sum = this._samples.Sum();
                return sum / this._samples.Count;
            }
        }

        public double Variance
        {
            get
            {
                if (this._samples.Count < 2) return 0;

                double mean = this.Mean;
                double sum = this._samples.Sum(sample => (sample - mean) * (sample - mean));
                return sum / (this._samples.Count - 1);
            }
        }

        public void Add(double newSample)
        {
            this._samples.Add(newSample);
        }

        public void Add(IEnumerable<double> newSamples)
        {
            this._samples.AddRange(newSamples);
        }

        public Summary GetSummary()
        {
            return new Summary(
                this._samples.Count, this.Min,
                this.Mean, this.Max, this.Variance, this.StandardDeviation);
        }

        [Serializable]
        public struct Summary
        {
            public int numSamples;
            public double min;
            public double mean;
            public double max;
            public double variance;
            public double standardDeviation;

            public Summary(int numSamples, double min, double mean,
                double max, double variance, double standardDeviation)
            {
                this.numSamples = numSamples;
                this.min = min;
                this.mean = mean;
                this.max = max;
                this.variance = variance;
                this.standardDeviation = standardDeviation;
            }

            public string ToString(int indent = 0) // TODO what if you don't define something?
            {
                return GeneralCSharpUtilities.Indent(indent) +
                       $"N: {this.numSamples}\n" +
                       GeneralCSharpUtilities.Indent(indent) + $"Min: {this.min}\n" +
                       GeneralCSharpUtilities.Indent(indent) + $"Mean: {this.mean}\n" +
                       GeneralCSharpUtilities.Indent(indent) + $"Max: {this.max}\n" +
                       GeneralCSharpUtilities.Indent(indent) + $"Variance: {this.variance}\n" +
                       GeneralCSharpUtilities.Indent(indent) + $"STD: {this.standardDeviation}";
            }
        }
    }
}