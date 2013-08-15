using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using SIL.Cog.Applications.Services;
using SIL.Cog.Domain;
using SIL.Cog.Domain.Statistics;
using SIL.Collections;

namespace SIL.Cog.Applications.ViewModels
{
	public class VarietiesVarietyViewModel : VarietyViewModel
	{
		public delegate VarietiesVarietyViewModel Factory(Variety variety);

		private readonly IProjectService _projectService;
		private readonly IDialogService _dialogService;
		private readonly BindableList<VarietySegmentViewModel> _segments;
		private readonly ReadOnlyObservableList<VarietySegmentViewModel> _readOnlySegments;
		private double _maxSegProb;
		private readonly ReadOnlyMirroredList<Affix, AffixViewModel> _affixes;
		private VarietySegmentViewModel _currentSegment;
		private AffixViewModel _currentAffix;
		private readonly WordsViewModel _words;
		private readonly ICommand _newAffixCommand;
		private readonly ICommand _editAffixCommand;
		private readonly ICommand _removeAffixCommand;
 
		public VarietiesVarietyViewModel(IProjectService projectService, IDialogService dialogService, WordsViewModel.Factory wordsFactory, Variety variety)
			: base(variety)
		{
			_projectService = projectService;
			_dialogService = dialogService;

			IEnumerable<Segment> segments;
			FrequencyDistribution<Segment> freqDist;
			if (variety.SegmentFrequencyDistributions.TryGetValue(SyllablePosition.Anywhere, out freqDist))
				segments = freqDist.ObservedSamples;
			else
				segments = Enumerable.Empty<Segment>();

			_segments = new BindableList<VarietySegmentViewModel>(segments.Select(seg => new VarietySegmentViewModel(variety, seg, SyllablePosition.Anywhere)));
			_maxSegProb = _segments.Select(seg => seg.Probability).Concat(0).Max();
			_readOnlySegments = new ReadOnlyObservableList<VarietySegmentViewModel>(_segments);
			variety.SegmentFrequencyDistributions.CollectionChanged += SegmentFrequencyDistributionsChanged;
			_affixes = new ReadOnlyMirroredList<Affix, AffixViewModel>(DomainVariety.Affixes, affix => new AffixViewModel(affix), vm => vm.DomainAffix);
			_words = wordsFactory(variety);
			_newAffixCommand = new RelayCommand(NewAffix);
			_editAffixCommand = new RelayCommand(EditAffix, CanEditAffix);
			_removeAffixCommand = new RelayCommand(RemoveAffix, CanRemoveAffix);
		}

		private void SegmentFrequencyDistributionsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				KeyValuePair<SyllablePosition, FrequencyDistribution<Segment>> kvp = e.NewItems.Cast<KeyValuePair<SyllablePosition, FrequencyDistribution<Segment>>>().Single();
				if (kvp.Key == SyllablePosition.Anywhere)
				{
					Segment curSeg = null;
					if (CurrentSegment != null)
						curSeg = CurrentSegment.DomainSegment;
					_segments.ReplaceAll(kvp.Value.ObservedSamples.Select(seg => new VarietySegmentViewModel(DomainVariety, seg, SyllablePosition.Anywhere)));
					MaxSegmentProbability = _segments.Select(seg => seg.Probability).Concat(0).Max();
					if (curSeg != null)
						CurrentSegment = _segments.FirstOrDefault(vm => vm.DomainSegment.Equals(curSeg));
				}
			}
		}

		private void NewAffix()
		{
			var vm = new EditAffixViewModel(_projectService.Project.Segmenter);
			if (_dialogService.ShowModalDialog(this, vm) == true)
			{
				var affix = new Affix(vm.StrRep, vm.Type == AffixViewModelType.Prefix ? AffixType.Prefix : AffixType.Suffix, vm.Category);
				_projectService.Project.Segmenter.Segment(affix);
				Messenger.Default.Send(new DomainModelChangingMessage());
				DomainVariety.Affixes.Add(affix);
				CurrentAffix = _affixes.Single(a => a.DomainAffix == affix);
			}
		}

		private bool CanEditAffix()
		{
			return CurrentAffix != null;
		}

		private void EditAffix()
		{
			var vm = new EditAffixViewModel(_projectService.Project.Segmenter, _currentAffix.DomainAffix);
			if (_dialogService.ShowModalDialog(this, vm) == true)
			{
				var affix = new Affix(vm.StrRep, vm.Type == AffixViewModelType.Prefix ? AffixType.Prefix : AffixType.Suffix, vm.Category);
				int index = DomainVariety.Affixes.IndexOf(_currentAffix.DomainAffix);
				Messenger.Default.Send(new DomainModelChangingMessage());
				DomainVariety.Affixes[index] = affix;
				_projectService.Project.Segmenter.Segment(affix);
				CurrentAffix = _affixes.Single(a => a.DomainAffix == affix);
			}
		}

		private void RemoveAffix()
		{
			Messenger.Default.Send(new DomainModelChangingMessage());
			DomainVariety.Affixes.Remove(CurrentAffix.DomainAffix);
		}

		private bool CanRemoveAffix()
		{
			return CurrentAffix != null;
		}

		public double MaxSegmentProbability
		{
			get { return _maxSegProb; }
			private set { Set(() => MaxSegmentProbability, ref _maxSegProb, value); }
		}

		public ReadOnlyObservableList<VarietySegmentViewModel> Segments
		{
			get { return _readOnlySegments; }
		}

		public WordsViewModel Words
		{
			get { return _words; }
		}

		public ReadOnlyObservableList<AffixViewModel> Affixes
		{
			get { return _affixes; }
		}

		public AffixViewModel CurrentAffix
		{
			get { return _currentAffix; }
			set { Set(() => CurrentAffix, ref _currentAffix, value); }
		}

		public VarietySegmentViewModel CurrentSegment
		{
			get { return _currentSegment; }
			set
			{
				if (Set(() => CurrentSegment, ref _currentSegment, value))
				{
					_words.SelectedSegmentWords.Clear();
					foreach (WordViewModel word in _words.Words)
					{
						bool selected = false;
						foreach (WordSegmentViewModel segment in word.Segments.Where(s => !s.IsBoundary))
						{
							segment.IsSelected = _currentSegment != null && segment.DomainNode.StrRep() == _currentSegment.StrRep;
							if (segment.IsSelected)
								selected = true;
						}

						if (selected)
							_words.SelectedSegmentWords.Add(word);
					}
				}
			}
		}

		public ICommand NewAffixCommand
		{
			get { return _newAffixCommand; }
		}

		public ICommand EditAffixCommand
		{
			get { return _editAffixCommand; }
		}

		public ICommand RemoveAffixCommand
		{
			get { return _removeAffixCommand; }
		}
	}
}
