﻿using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;

namespace SIL.Cog.Statistics
{
	public class ConditionalProbabilityDistribution<TCondition, TSample> : IConditionalProbabilityDistribution<TCondition, TSample>
	{
		private readonly Dictionary<TCondition, IProbabilityDistribution<TSample>> _probDists; 

		public ConditionalProbabilityDistribution(ConditionalFrequencyDistribution<TCondition, TSample> cfd,
			Func<FrequencyDistribution<TSample>, IProbabilityDistribution<TSample>> getProbDist)
		{
			_probDists = cfd.Conditions.ToDictionary(cond => cond, cond => getProbDist(cfd[cond]));
		}

		public IReadOnlyCollection<TCondition> Conditions
		{
			get { return _probDists.Keys.ToReadOnlyCollection(); }
		}

		public IProbabilityDistribution<TSample> this[TCondition condition]
		{
			get { return _probDists[condition]; }
		}

		public bool TryGetProbabilityDistribution(TCondition condition, out IProbabilityDistribution<TSample> probDist)
		{
			return _probDists.TryGetValue(condition, out probDist);
		}
	}
}
