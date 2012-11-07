﻿using System;

namespace SIL.Cog.Statistics
{
	public class WittenBellProbabilityDistribution<TSample> : IProbabilityDistribution<TSample>
	{
		private readonly FrequencyDistribution<TSample> _freqDist;
		private readonly double _probZero;

		public WittenBellProbabilityDistribution(FrequencyDistribution<TSample> freqDist, int binCount)
		{
			if (binCount <= freqDist.ObservedSamples.Count)
				throw new ArgumentOutOfRangeException("binCount");

			_freqDist = freqDist;
			int z = binCount - _freqDist.ObservedSamples.Count;
			_probZero = (double) _freqDist.ObservedSamples.Count / (z * (_freqDist.SampleOutcomeCount + _freqDist.ObservedSamples.Count));
		}

		public double GetProbability(TSample sample)
		{
			int count = _freqDist[sample];
			if (count == 0)
				return _probZero;
			return (double) count / (_freqDist.SampleOutcomeCount + _freqDist.ObservedSamples.Count);
		}
	}
}
