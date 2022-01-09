/****************************************************************\

 Add-in to Revit connection interface

\****************************************************************/

using System;
using System.Reflection;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI.Events;

namespace FamilyGuy
{
  internal sealed class RevitBinding : IExternalApplication
  {
    public const string AddinName = "Family Guy";
    public const string AddinButtonName = "Bind parameters";
    public const string TransactionNameAssociate = "Bind parameters";
    public const string TransactionNameDisassociate = "Unbind parameters";

    private const string IsectorButtonImage16Uri = "pack://application:,,,/FamilyGuy;component/Resources/icon16.png";
    private const string IsectorButtonImage32Uri = "pack://application:,,,/FamilyGuy;component/Resources/icon32.png";

    private const string DockablePaneGuid = "21bbdb14-4f4e-4391-955f-230f70c31888";

    public readonly DockablePaneId ParametersAssociationPaneId;
    private readonly SelectionManager selectionManager;

    private readonly Forms.ParametersBindingViewModel parametersBindingVM;

    private readonly FamilyParametersModel familyParameters;
    private readonly SelectedElementsParametersModel selectedElementsParameters;

    public bool IsFamilyDocument { get; private set; }
    public RevitContext Context { get; private set; }
    public static RevitBinding Instance { get; private set; }

    public RevitBinding()
    {
      Instance = this;

      selectionManager = new SelectionManager();

      familyParameters = new FamilyParametersModel();
      selectedElementsParameters = new SelectedElementsParametersModel();

      parametersBindingVM = new Forms.ParametersBindingViewModel(familyParameters, selectedElementsParameters);
      ParametersAssociationPaneId = new DockablePaneId(new Guid(DockablePaneGuid));
    }

    private void AddRibbonPanel(UIControlledApplication application)
    {
      RibbonPanel ribbonPanel = application.CreateRibbonPanel(AddinName);

      string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;

      // open/close parameters association window
      PushButtonData markButtonData = new PushButtonData("cmdShowAssociations", AddinButtonName, thisAssemblyPath, typeof(AssociationsPanelShow).FullName);
      PushButton markButton = ribbonPanel.AddItem(markButtonData) as PushButton;
      markButton.Image = new System.Windows.Media.Imaging.BitmapImage(new Uri(IsectorButtonImage16Uri));
      markButton.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(IsectorButtonImage32Uri));
      var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
      markButton.ToolTip = "Binding family parameters to elements (in family editor).\nVersion " + version;
    }

    public Result OnStartup(UIControlledApplication application)
    {
      if (application == null)
        return Result.Failed;

      // WPF debug
#if DEBUG
      System.Diagnostics.PresentationTraceSources.Refresh();
      System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Warning | System.Diagnostics.SourceLevels.Error;
#endif

      Context = new RevitContext();
      AddRibbonPanel(application);
      RegisterBindingView(application);

      application.Idling += IdlingHandler;
      application.ViewActivated += ViewActivatedHandler;
      application.ControlledApplication.DocumentChanged += DocumentChangedHandler;

      return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
      if (application == null)
        return Result.Failed;

      application.ControlledApplication.DocumentChanged -= DocumentChangedHandler;
      application.ViewActivated -= ViewActivatedHandler;
      application.Idling -= IdlingHandler;

      return Result.Succeeded;
    }

    private void DocumentChangedHandler(object sender, Autodesk.Revit.DB.Events.DocumentChangedEventArgs e)
    {
      if (!IsFamilyDocument)
        return;

      var doc = e.GetDocument();

      // full refresh
      if (e.Operation == Autodesk.Revit.DB.Events.UndoOperation.TransactionGroupRolledBack)
      {
        familyParameters.SetDocument(doc);
        selectedElementsParameters.SetSelection(selectionManager.Selection);
        return;
      }

      var modifiedIds = e.GetModifiedElementIds();

      bool selectionModified = false;
      bool familyModified = false;
      foreach (var elemId in modifiedIds)
      {
        var elem = doc.GetElement(elemId);
        if (elem is Autodesk.Revit.DB.Family)
        {
          familyModified = true;
          break;
        }

        if (selectionManager.IsSelected(elemId.IntegerValue))
        {
          selectionModified = true;
        }
      }

      if (familyModified)
        familyParameters.SetDocument(doc);

      if (familyModified || selectionModified)
        selectedElementsParameters.SetSelection(selectionManager.Selection);
    }

    private void RegisterBindingView(UIControlledApplication application)
    {
      var parametersBindingWindow = new Forms.ParametersBindingWindow { DataContext = parametersBindingVM };
      application.RegisterDockablePane(ParametersAssociationPaneId, AddinButtonName, parametersBindingWindow);
    }

    private void ViewActivatedHandler(object sender, Autodesk.Revit.UI.Events.ViewActivatedEventArgs e)
    {
      IsFamilyDocument = e.CurrentActiveView.Document.IsFamilyDocument;
      familyParameters.SetDocument(e.CurrentActiveView.Document);
    }

    private void IdlingHandler(object sender, IdlingEventArgs e)
    {
      ScanSelection(sender as UIApplication);
    }

    public void ScanSelection(UIApplication uiApp)
    {
      if (!IsFamilyDocument)
      {
        selectionManager.ResetSelection();
        selectedElementsParameters.SetSelection(null);
        return;
      }

      if (uiApp.ActiveUIDocument != null)
      {
        var doc = uiApp.ActiveUIDocument.Document;
        var selected = uiApp.ActiveUIDocument.Selection.GetElementIds();

        if (selectionManager.IsSelectionChanged(doc, selected))
          selectedElementsParameters.SetSelection(selectionManager.Selection);
      }
    }
  }

  [TransactionAttribute(TransactionMode.Manual)]
  [RegenerationAttribute(RegenerationOption.Manual)]
  public sealed class AssociationsPanelShow : IExternalCommand
  {
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      if (commandData == null)
        return Result.Failed;

      var pane = commandData.Application.GetDockablePane(RevitBinding.Instance.ParametersAssociationPaneId);

      if (pane != null)
      {
        if (pane.IsShown())
          pane.Hide();
        else
          pane.Show();
      }

      return Result.Succeeded;
    }
  }
}
