using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using SIL.Collections;
using SIL.Machine;

namespace SIL.Cog
{
	public class SegmentCollection : IKeyedReadOnlyCollection<string, Segment>, INotifyCollectionChanged
	{
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		private readonly Variety _variety;
		private readonly SimpleMonitor _reentrancyMonitor = new SimpleMonitor();
		private readonly Dictionary<string, Segment> _segments;

		internal SegmentCollection(Variety variety)
		{
			_variety = variety;
			_segments = new Dictionary<string, Segment>();
		}

		internal void WordAdded(Word word)
		{
			CheckReentrancy();
			var segmentsAdded = new List<Segment>();
			foreach (ShapeNode node in word.Shape.Where(n => n.Type().IsOneOf(CogFeatureSystem.VowelType, CogFeatureSystem.ConsonantType)))
			{
				Segment segment;
				if (!_segments.TryGetValue(node.StrRep(), out segment))
				{
					segment = new Segment(node.Annotation.FeatureStruct);
					_segments[node.StrRep()] = segment;
					segmentsAdded.Add(segment);
				}
				_variety.SegmentFrequencyDistribution.Increment(segment);
			}

			if (segmentsAdded.Count > 0)
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, segmentsAdded));
		}

		internal void WordRemoved(Word word)
		{
			CheckReentrancy();
			var segmentsRemoved = new List<Segment>();
			foreach (ShapeNode node in word.Shape.Where(n => n.Type().IsOneOf(CogFeatureSystem.VowelType, CogFeatureSystem.ConsonantType)))
			{
				Segment segment = _segments[node.StrRep()];
				_variety.SegmentFrequencyDistribution.Decrement(segment);
				if (_variety.SegmentFrequencyDistribution[segment] == 0)
				{
					_segments.Remove(node.StrRep());
					segmentsRemoved.Add(segment);
				}
			}

			if (segmentsRemoved.Count > 0)
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, segmentsRemoved));
		}

		internal void WordsCleared()
		{
			int count = _segments.Count;
			_segments.Clear();
			_variety.SegmentFrequencyDistribution.Reset();
			if (count > 0)
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		IEnumerator<Segment> IEnumerable<Segment>.GetEnumerator()
		{
			return _segments.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _segments.Values.GetEnumerator();
		}

		public bool Contains(Segment segment)
		{
			return _segments.ContainsKey(segment.StrRep);
		}

		public bool Contains(string strRep)
		{
			return _segments.ContainsKey(strRep);
		}

		public bool TryGetValue(string key, out Segment item)
		{
			return _segments.TryGetValue(key, out item);
		}

		public Segment this[string strRep]
		{
			get
			{
				if (strRep == "-")
					return Segment.Null;
				return _segments[strRep];
			}
		}

		public Segment this[ShapeNode node]
		{
			get
			{
				if (node.Type() == CogFeatureSystem.NullType)
					return Segment.Null;
				return _segments[node.StrRep()];
			}
		}

		public Ngram this[Annotation<ShapeNode> ann]
		{
			get
			{
				return new Ngram(ann.Span.Start.GetNodes(ann.Span.End).Select(n => this[n]));
			}
		}

		public int Count
		{
			get { return _segments.Count; }
		}

		protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (CollectionChanged != null)
			{
				using (_reentrancyMonitor.Enter())
					CollectionChanged(this, e);
			}
		}

		protected void CheckReentrancy()
		{
			if (_reentrancyMonitor.Busy)
				throw new InvalidOperationException("This collection cannot be changed during a CollectionChanged event.");
		}

		protected IDisposable BlockReentrancy()
		{
			return _reentrancyMonitor.Enter();
		}
	}
}