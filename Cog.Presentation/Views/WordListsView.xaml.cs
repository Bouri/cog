﻿using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Threading;
using SIL.Cog.Applications.ViewModels;
using SIL.Cog.Presentation.Behaviors;
using SIL.Cog.Presentation.Converters;
using SIL.Collections;
using Xceed.Wpf.DataGrid;
using Xceed.Wpf.DataGrid.Views;

namespace SIL.Cog.Presentation.Views
{
	/// <summary>
	/// Interaction logic for WordListsView.xaml
	/// </summary>
	public partial class WordListsView
	{
		private readonly SimpleMonitor _monitor;
		private InputBinding _findBinding;

		public WordListsView()
		{
			InitializeComponent();
			WordListsGrid.ClipboardExporters.Clear();
			WordListsGrid.ClipboardExporters.Add(DataFormats.UnicodeText, new UnicodeCsvClipboardExporter {IncludeColumnHeaders = false, FormatSettings = {TextQualifier = '\0'}});
			_monitor = new SimpleMonitor();
			BusyCursor.DisplayUntilIdle();
		}

		private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var vm = DataContext as WordListsViewModel;
			if (vm == null)
				return;

			vm.PropertyChanged += ViewModel_PropertyChanged;
			vm.Senses.CollectionChanged += Senses_CollectionChanged;
			vm.Varieties.CollectionChanged += Varieties_CollectionChanged;
			_findBinding = new InputBinding(vm.FindCommand, new KeyGesture(Key.F, ModifierKeys.Control));
		}

		private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var window = this.FindVisualAncestor<Window>();
			if (IsVisible)
				window.InputBindings.Add(_findBinding);
			else
				window.InputBindings.Remove(_findBinding);
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			LoadColumns();
			LoadCollectionView();
			SizeRowSelectorPaneToFit();
			SelectFirstCell();
		}

		private void LoadColumns()
		{
			var vm = (WordListsViewModel) DataContext;

			WordListsGrid.Columns.Clear();
			for (int i = 0; i < vm.Senses.Count; i++)
			{
				var pen = new Pen(new SolidColorBrush(Colors.Red), 2) {DashStyle = DashStyles.Dash};
				var textDecoration = new TextDecoration {Location = TextDecorationLocation.Underline, Pen = pen, PenOffset = 1};
				var textDecorations = new TextDecorationCollection {textDecoration};

				var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
				var textBinding = new Binding(string.Format("Senses[{0}].Words", i)) {Converter = new WordsToInlinesConverter(), ConverterParameter = textDecorations};
				textBlockFactory.SetBinding(TextBlockBehaviors.InlinesListProperty, textBinding);
				textBlockFactory.SetValue(TextBlock.PaddingProperty, new Thickness(3, 1, 3, 1));
				textBlockFactory.SetValue(TextBlock.FontSizeProperty, 16.0);
				textBlockFactory.SetBinding(TagProperty, new Binding(string.Format("Senses[{0}].StrRep", i)));
				var menuItem = new MenuItem {Header = "Show in varieties"};
				menuItem.SetBinding(MenuItem.CommandProperty, string.Format("Senses[{0}].ShowInVarietiesCommand", i));
				textBlockFactory.SetValue(ContextMenuProperty, new ContextMenu {Items = {menuItem}});
				textBlockFactory.Name = "textBlock";
				var cellTemplate = new DataTemplate
					{
						VisualTree = textBlockFactory,
						Triggers =
							{
								new Trigger {SourceName = "textBlock", Property = TextBlockBehaviors.IsTextTrimmedProperty, Value = true, Setters =
									{
										new Setter(ToolTipProperty, new Binding(string.Format("Senses[{0}].StrRep", i)), "textBlock")
									}}
							}
					};

				var textBoxFactory = new FrameworkElementFactory(typeof(TextBox));
				textBoxFactory.SetBinding(TextBox.TextProperty, new Binding(string.Format("Senses[{0}].StrRep", i)));
				textBoxFactory.SetValue(BorderThicknessProperty, new Thickness(0));
				textBoxFactory.SetValue(FontSizeProperty, 16.0);
				textBoxFactory.Name = "textBox";
				var cellEditTemplate = new DataTemplate {VisualTree = textBoxFactory};

				var c = new Column
					{
						FieldName = vm.Senses[i].Gloss,
						Title = vm.Senses[i].Gloss,
						DisplayMemberBindingInfo = new DataGridBindingInfo { Path = new PropertyPath(".") },
						CellContentTemplate = cellTemplate,
						CellEditor = new CellEditor { EditTemplate = cellEditTemplate },
						Width = new ColumnWidth(100)
					};

				WordListsGrid.Columns.Add(c);
			}
		}

		private void SizeRowSelectorPaneToFit()
		{
			var vm = (WordListsViewModel) DataContext;
			if (vm == null)
				return;

			var textBrush = (Brush) Application.Current.FindResource("HeaderTextBrush");
			double maxWidth = 0;
			foreach (WordListsVarietyViewModel variety in vm.Varieties)
			{
				var formattedText = new FormattedText(variety.Name, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
					new Typeface(WordListsGrid.FontFamily, WordListsGrid.FontStyle, WordListsGrid.FontWeight, WordListsGrid.FontStretch), WordListsGrid.FontSize, textBrush);
				if (formattedText.Width > maxWidth)
					maxWidth = formattedText.Width;
				variety.PropertyChanged -= variety_PropertyChanged;
				variety.PropertyChanged += variety_PropertyChanged;
			}

			var tableView = (TableView) WordListsGrid.View;
			tableView.RowSelectorPaneWidth = maxWidth + 18;
		}

		private void variety_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "Name":
					DispatcherHelper.CheckBeginInvokeOnUI(SizeRowSelectorPaneToFit);
					break;
			}
		}

		private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var vm = (WordListsViewModel) sender;
			switch (e.PropertyName)
			{
				case "Senses":
					DispatcherHelper.CheckBeginInvokeOnUI(() =>
						{
							LoadColumns();
							vm.Senses.CollectionChanged += Senses_CollectionChanged;
						});
					break;

				case "Varieties":
					DispatcherHelper.CheckBeginInvokeOnUI(() =>
						{
							LoadCollectionView();
							SizeRowSelectorPaneToFit();
							vm.Varieties.CollectionChanged += Varieties_CollectionChanged;
						});
					WordListsGrid.Dispatcher.BeginInvoke(new Action(SelectFirstCell));
					break;

				case "CurrentVarietySense":
					DispatcherHelper.CheckBeginInvokeOnUI(() =>
						{
							if (_monitor.Busy)
								return;

							using (_monitor.Enter())
							{
								WordListsGrid.SelectedCellRanges.Clear();
								if (vm.CurrentVarietySense != null)
								{
									WordListsVarietyViewModel variety = vm.CurrentVarietySense.Variety;
									int itemIndex = vm.Varieties.IndexOf(variety);
									int columnIndex = variety.Senses.IndexOf(vm.CurrentVarietySense);
									WordListsGrid.SelectedCellRanges.Add(new SelectionCellRange(itemIndex, columnIndex));
									WordListsGrid.BringItemIntoView(variety);
									WordListsGrid.Dispatcher.BeginInvoke(new Action(() =>
									    {
									        var row = (DataRow) WordListsGrid.GetContainerFromIndex(itemIndex);
										    if (row != null)
										    {
											    Cell cell = row.Cells[columnIndex];
											    cell.BringIntoView();
										    }
									    }), DispatcherPriority.ApplicationIdle);
								}
							}
						});
					break;
			}
		}

		private void SelectFirstCell()
		{
			if (WordListsGrid.Items.Count > 0)
				WordListsGrid.SelectedCellRanges.Add(new SelectionCellRange(0, 0));
			WordListsGrid.Focus();
		}

		private void LoadCollectionView()
		{
			var vm = (WordListsViewModel) DataContext;
			if (vm == null)
				return;

			var source = new DataGridCollectionView(vm.Varieties, typeof(WordListsVarietyViewModel), false, false);
			for (int i = 0; i < vm.Senses.Count; i++)
				source.ItemProperties.Add(new DataGridItemProperty(vm.Senses[i].Gloss, string.Format("Senses[{0}].StrRep", i), typeof(string)));
			WordListsGrid.ItemsSource = source;
		}

		private void Senses_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			LoadColumns();
		}

		private void Varieties_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			WordListsGrid.Items.Refresh();
			SizeRowSelectorPaneToFit();
		}

		private void WordListsGrid_OnSelectionChanged(object sender, DataGridSelectionChangedEventArgs e)
		{
			var vm = (WordListsViewModel) DataContext;
			if (_monitor.Busy)
				return;

			using (_monitor.Enter())
			{
				if (WordListsGrid.SelectedCellRanges.Count == 1)
				{
					SelectionCellRange cellRange = WordListsGrid.SelectedCellRanges[0];
					int itemIndex = cellRange.ItemRange.StartIndex;
					WordListsVarietyViewModel variety = vm.Varieties[itemIndex];
					int columnIndex = cellRange.ColumnRange.StartIndex;
					vm.CurrentVarietySense = variety.Senses[columnIndex];
				}
				else
				{
					vm.CurrentVarietySense = null;
				}
			}
		}

		private void Cell_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			var cell = (DataCell) sender;
			WordListsGrid.SelectedCellRanges.Clear();
			int itemIndex = WordListsGrid.Items.IndexOf(cell.ParentRow.DataContext);
			WordListsGrid.SelectedCellRanges.Add(new SelectionCellRange(itemIndex, cell.ParentColumn.Index));
		}
	}
}