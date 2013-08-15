﻿using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using GalaSoft.MvvmLight.Threading;
using SIL.Cog.Applications.ViewModels;

namespace SIL.Cog.Presentation.Views
{
	/// <summary>
	/// Interaction logic for MultipleWordAlignmentView.xaml
	/// </summary>
	public partial class MultipleWordAlignmentView
	{
		public MultipleWordAlignmentView()
		{
			InitializeComponent();
			BusyCursor.DisplayUntilIdle();
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			LoadColumns();
			var vm = (MultipleWordAlignmentViewModel) DataContext;
			vm.WordsView = CollectionViewSource.GetDefaultView(vm.Words);
			if (vm.GroupByCognateSet)
			{
				Debug.Assert(vm.WordsView.GroupDescriptions != null);
				vm.WordsView.GroupDescriptions.Add(new PropertyGroupDescription("CognateSetIndex"));
			}
			vm.Words.CollectionChanged += WordsChanged;
			vm.SensesView = CollectionViewSource.GetDefaultView(vm.Senses);
			vm.PropertyChanged += vm_PropertyChanged;
		}

		private void vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var vm = (MultipleWordAlignmentViewModel) DataContext;
			switch (e.PropertyName)
			{
				case "Senses":
					DispatcherHelper.CheckBeginInvokeOnUI(() => vm.SensesView = CollectionViewSource.GetDefaultView(vm.Senses));
					break;

				case "GroupByCognateSet":
					if (vm.GroupByCognateSet)
						vm.WordsView.GroupDescriptions.Add(new PropertyGroupDescription("CognateSetIndex"));
					else
						vm.WordsView.GroupDescriptions.Clear();
					break;
			}
		}

		private void WordsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			LoadColumns();
		}

		private void LoadColumns()
		{
			var vm = (MultipleWordAlignmentViewModel) DataContext;
			AlignmentGrid.Columns.Clear();
			var prefixColumn = new DataGridTextColumn
				{
					Binding = new Binding("Prefix"),
					ClipboardContentBinding = new Binding("Prefix"),
					CellStyle = new Style(typeof(DataGridCell), AlignmentGrid.CellStyle) {Setters =
						{
							new Setter(BorderThicknessProperty, new Thickness(0, 0, 1, 0)),
							new Setter(BorderBrushProperty, new SolidColorBrush(Colors.LightGray)),
							new Setter(IsEnabledProperty, false),
						}},
					MinWidth = 0,
					Width = new DataGridLength(0, DataGridLengthUnitType.SizeToCells),
					ElementStyle = new Style(typeof(TextBlock)) {Setters =
						{
							new Setter(ForegroundProperty, new SolidColorBrush(Colors.Gray)),
							new Setter(MarginProperty, new Thickness(5, 0, 5, 0))
						}}
				};
			AlignmentGrid.Columns.Add(prefixColumn);
			for (int i = 0; i < vm.ColumnCount; i++)
			{
				var column = new DataGridTextColumn
					{
						Binding = new Binding(string.Format("Columns[{0}]", i)),
						ClipboardContentBinding = new Binding(string.Format("Columns[{0}]", i)),
						Width = new DataGridLength(0, DataGridLengthUnitType.SizeToCells)
					};
				AlignmentGrid.Columns.Add(column);
			}
			var suffixColumn = new DataGridTextColumn
				{
					Binding = new Binding("Suffix"),
					ClipboardContentBinding = new Binding("Suffix"),
					CellStyle = new Style(typeof(DataGridCell), AlignmentGrid.CellStyle) {Setters =
						{
							new Setter(BorderThicknessProperty, new Thickness(1, 0, 0, 0)),
							new Setter(BorderBrushProperty, new SolidColorBrush(Colors.LightGray)),
							new Setter(IsEnabledProperty, false),
						}},
					MinWidth = 0,
					Width = new DataGridLength(0, DataGridLengthUnitType.SizeToCells),
					ElementStyle = new Style(typeof(TextBlock)) {Setters =
						{
							new Setter(ForegroundProperty, new SolidColorBrush(Colors.Gray)),
							new Setter(MarginProperty, new Thickness(5, 0, 5, 0))
						}}
				};
			AlignmentGrid.Columns.Add(suffixColumn);
		}

		private void AlignmentGrid_OnSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
		{
			var vm = (MultipleWordAlignmentViewModel) DataContext;
			if (e.AddedCells.Count == 1)
			{
				DataGridCellInfo ci = e.AddedCells[0];
				vm.CurrentColumn = ci.Column.DisplayIndex - 1;
				vm.CurrentWord = (MultipleWordAlignmentWordViewModel) ci.Item;
			}
			else
			{
				vm.CurrentColumn = -1;
				vm.CurrentWord = null;
			}
		}
	}
}
