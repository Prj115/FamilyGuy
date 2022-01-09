/****************************************************************\

 FamilyParameters and seleceted elements lists window

\****************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using Autodesk.Revit.UI;

namespace FamilyGuy.Forms
{
  /// <summary>
  /// Interaction logic for BindingWindow.xaml
  /// </summary>
  public partial class ParametersBindingWindow : Page, IDockablePaneProvider
  {
    private double familyScrollPos = 0;
    private double elemScrollPos = 0;
    private int selectedFamily = -1;
    private int selectedSelected = -1;

    private bool alphabet = false;
    private bool untangled = false;

    public ParametersBindingWindow()
    {
      InitializeComponent();

      DataContextChanged += OnDataContextChanged;
      ((INotifyCollectionChanged)SelectedParametersList.Items).CollectionChanged += SelectedParametersList_CollectionChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      (DataContext as ParametersBindingViewModel).PropertyChanged += ViewModedl_PropertyChanged;
    }

    private void ViewModedl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      if (e.PropertyName == "AssociationMap")
        DrawAssociationMap();
    }

    public void DrawAssociationMap()
    {
      const double itemHeght = 20;
      const double centerShift = 12;

      var dataVM = DataContext as ParametersBindingViewModel;
      var map = dataVM.AssociationMap;
      if (map == null)
        return;

      AssociationCanvas.Children.Clear();

      foreach (var link in map)
      {
        var leftIdx = link.Item1;
        var rightIdx = link.Item2;
        var isFull = link.Item3;

        bool highlightLine = (leftIdx == selectedFamily) || (rightIdx == selectedSelected);

        var line = DrawBezier(
          new Point(0, (leftIdx - familyScrollPos) * itemHeght + centerShift),
          new Point(AssociationCanvas.ActualWidth, (rightIdx - elemScrollPos) * itemHeght + centerShift)
        );

        line.Stroke = highlightLine ? Brushes.LightSkyBlue : Brushes.Black;
        line.Opacity = isFull ? 1 : 0.3;
        line.StrokeThickness = 2;
        Panel.SetZIndex(line, highlightLine ? 1 : 0);

        AssociationCanvas.Children.Add(line);
      }
    }

    private Path DrawBezier(Point p1, Point p2)
    {
      double width = p2.X - p1.X;
      var controlShift = new Vector(width / 3, 0);

      var pbs = new PolyBezierSegment(new List<Point> { p1 + controlShift, p2 - controlShift, p2 }, true);
      var psc = new PathSegmentCollection { pbs };
      var pf = new PathFigure(p1, psc, false);
      var pfc = new PathFigureCollection { pf };
      var pg = new PathGeometry { Figures = pfc };
      var path = new Path { Data = pg };

      return path;
    }

    public void SetupDockablePane(DockablePaneProviderData data)
    {
      if (data == null)
        return;

      data.FrameworkElement = this;
      data.InitialState.DockPosition = DockPosition.Floating;
      data.InitialState.SetFloatingRectangle(new Autodesk.Revit.DB.Rectangle(100, 100, 400, 400));
    }

    private void UpdateSort()
    {
      var dataVM = this.DataContext as ParametersBindingViewModel;

      if (alphabet)
      {
        if (untangled)
          dataVM.SetParametersSortType(ParametersSortType.AlphabetUntangled);
        else
          dataVM.SetParametersSortType(ParametersSortType.Alphabet);
      }
      else
      {
        if (untangled)
          dataVM.SetParametersSortType(ParametersSortType.OriginalUntangled);
        else
          dataVM.SetParametersSortType(ParametersSortType.Original);
      }
    }

    private void FamilyParametersList_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
      familyScrollPos = e.VerticalOffset;
      DrawAssociationMap();
    }

    private void SelectedParameters_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
      elemScrollPos = e.VerticalOffset;
      DrawAssociationMap();
    }

    private void FamilyParametersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      selectedFamily = FamilyParametersList.SelectedIndex;
      selectedSelected = -1;
      DrawAssociationMap();
    }

    private void SelectedParametersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      selectedFamily = -1;
      selectedSelected = SelectedParametersList.SelectedIndex;
      DrawAssociationMap();
    }

    private void SelectedParametersList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      var dataVM = this.DataContext as ParametersBindingViewModel;
      var selectedNames = dataVM.SelectedElements;

      var namesCount = new Dictionary<string, int>();
      foreach (var name in selectedNames)
        if (namesCount.ContainsKey(name))
          namesCount[name]++;
        else
          namesCount[name] = 1;

      var namesCompacted = namesCount.Select((x) => (x.Value > 1) ? $"{x.Key} ({x.Value})" : x.Key).ToList();
      SelectedNames.Text = String.Join(", ", namesCompacted);
    }

    private void FamilyList_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
      if (sender is ListBoxItem listBoxItem)
        if (listBoxItem.DataContext is FamilyParam fp)
          if (fp.Formula != null)
            System.Windows.Clipboard.SetText(fp.Formula);
    }

    private void FamilyList_PreviewMouseMove(object sender, MouseEventArgs e) // завершение в ParametersList_Drop
    {
      if (sender is ListBoxItem listBoxItem && e.LeftButton == MouseButtonState.Pressed)
      {
        SelectedParametersList.AllowDrop = true;
        DragDrop.DoDragDrop(listBoxItem, listBoxItem.DataContext, DragDropEffects.Link);
        SelectedParametersList.AllowDrop = false;
      }
    }

    private void ParametersList_Drop(object sender, DragEventArgs e)
    {
      FamilyParam familyParam = e.Data.GetData(typeof(FamilyParam)) as FamilyParam;

      var listBoxItem = sender as ListBoxItem;
      if (familyParam != null && listBoxItem.DataContext is CombinedParam combinedParam)
      {
        (DataContext as ParametersBindingViewModel).AssociateParameters(familyParam, combinedParam);
        e.Effects = DragDropEffects.Link;
      }
    }

    private void ParametersList_PreviewMouseMove(object sender, MouseEventArgs e) // завершение в FamilyList_Drop
    {
      if (sender is ListBoxItem listBoxItem && e.LeftButton == MouseButtonState.Pressed)
      {
        FamilyParametersList.AllowDrop = true;
        DragDrop.DoDragDrop(listBoxItem, listBoxItem.DataContext, DragDropEffects.Link);
        FamilyParametersList.AllowDrop = false;
      }
    }

    private void FamilyList_Drop(object sender, DragEventArgs e)
    {
      CombinedParam combinedParam = e.Data.GetData(typeof(CombinedParam)) as CombinedParam;

      var listBoxItem = sender as ListBoxItem;
      if (combinedParam != null && listBoxItem.DataContext is FamilyParam familyParam)
      {
        (DataContext as ParametersBindingViewModel).AssociateParameters(familyParam, combinedParam);
        e.Effects = DragDropEffects.Link;
      }
    }

    private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      var listBoxItem = sender as ListBoxItem;

      if (listBoxItem.DataContext is FamilyParam fp)
        (DataContext as ParametersBindingViewModel).DisassociateFamilyParameter(fp);

      if (listBoxItem.DataContext is CombinedParam cp)
        (DataContext as ParametersBindingViewModel).DisassociateElementsParameter(cp);
    }

    private void ListBoxItem_DragEnter(object sender, DragEventArgs e)
    {
      var listBoxItem = sender as ListBoxItem;
      listBoxItem.IsSelected = true;
    }

    private void ListBoxItem_DragLeave(object sender, DragEventArgs e)
    {
      var listBoxItem = sender as ListBoxItem;
      listBoxItem.IsSelected = false;
    }

    private void Alpabet_Click(object sender, RoutedEventArgs e)
    {
      alphabet = !alphabet;
      UpdateSort();
    }

    private void Untangled_Click(object sender, RoutedEventArgs e)
    {
      untangled = !untangled;
      UpdateSort();
    }

    private void RefreshSelection_Click(object sender, RoutedEventArgs e)
    {
      (DataContext as ParametersBindingViewModel).RefreshSelection();
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
      MessageBox.Show(
        @"Drag - bind parameters
Double click - unbind parameter
Right click - copy formula of a family parameter to clipboard",
        RevitBinding.AddinName, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information
      );
    }
  }
}
