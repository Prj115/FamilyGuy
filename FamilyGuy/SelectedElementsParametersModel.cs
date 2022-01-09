/****************************************************************\

 Parameters of selected family elements

\****************************************************************/

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace FamilyGuy
{
  /// <summary>
  ///   представление параметров элементов
  /// </summary>
  internal sealed class SelectedElementsParametersModel : INotifyPropertyChanged
  {
    private List<Element> elements = null;

    public event PropertyChangedEventHandler PropertyChanged;

    public void SetSelection(List<Element> elems)
    {
      elements = elems;
      OnPropertyChanged("");
    }

    public List<ElementParams> GetSelectedElements()
    {
      var elems = new List<ElementParams>();

      if (RevitBinding.Instance.IsFamilyDocument && elements != null)
      {
        foreach (var elem in elements)
          elems.Add(new ElementParams { ElementId = elem.Id.IntegerValue, ElementName = elem.Name, Parameters = GetElementParameters(elem) });
      }

      return elems;
    }

    public void AssociateFamilyParameter(string familyParam, string elemParam, List<int> elementIds)
    {
      RevitBinding.Instance.Context.Call("Associate parameters event handler", delegate (UIApplication app)
      {
        var doc = app.ActiveUIDocument.Document;

        if (!doc.IsFamilyDocument)
          return;

        var fm = doc.FamilyManager;

        var fParam = fm.get_Parameter(familyParam);
        if (fParam == null)
          return;

        using (var transaction = new Transaction(doc, RevitBinding.TransactionNameAssociate))
        {
          transaction.Start();

          bool success = true;
          try
          {
            foreach (var id in elementIds)
            {
              var elem = doc.GetElement(new ElementId(id));
              if (elem != null)
              {
                var eParam = elem.LookupParameter(elemParam);
                if (eParam != null)
                  fm.AssociateElementParameterToFamilyParameter(eParam, fParam);
              }
            }
          }
          catch (Autodesk.Revit.Exceptions.InvalidOperationException)
          {
            System.Windows.MessageBox.Show($"Types of '{familyParam}' and '{elemParam}' are different, parameters can't be binded.", RevitBinding.AddinName, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.None);
            success = false;
          }
          catch (Autodesk.Revit.Exceptions.ApplicationException e)
          {
            System.Windows.MessageBox.Show(e.ToString(), RevitBinding.AddinName, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Hand);
            success = false;
          }

          if (success)
            transaction.Commit();
          else
            transaction.RollBack();
        }
      });
    }

    public void DisassociateFamilyParameter(string familyParam)
    {
      RevitBinding.Instance.Context.Call("Disassociate family parameter event handler", delegate (UIApplication app)
      {
        var doc = app.ActiveUIDocument.Document;

        if (!doc.IsFamilyDocument)
          return;

        var fm = doc.FamilyManager;

        var fParam = fm.get_Parameter(familyParam);
        if (fParam == null)
          return;

        var eParams = fParam.AssociatedParameters;
        if (eParams == null)
          return;

        using (var transaction = new Transaction(doc, RevitBinding.TransactionNameDisassociate))
        {
          transaction.Start();

          bool success = true;
          try
          {
            foreach (var param in eParams)
            {
              if (param is Parameter eParam)
                fm.AssociateElementParameterToFamilyParameter(eParam, null);
            }
          }
          catch (Autodesk.Revit.Exceptions.ApplicationException e)
          {
            System.Windows.MessageBox.Show(e.ToString(), RevitBinding.AddinName, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Hand);
            success = false;
          }

          if (success)
            transaction.Commit();
          else
            transaction.RollBack();
        }
      });
    }

    public void DisassociateElementsParameter(string elemParam, List<int> elementIds)
    {
      RevitBinding.Instance.Context.Call("Disassociate elements parameter event handler", delegate (UIApplication app)
      {
        var doc = app.ActiveUIDocument.Document;

        if (!doc.IsFamilyDocument)
          return;

        var fm = doc.FamilyManager;

        using (var transaction = new Transaction(doc, RevitBinding.TransactionNameDisassociate))
        {
          transaction.Start();

          bool success = true;
          try
          {
            foreach (var id in elementIds)
            {
              var elem = doc.GetElement(new ElementId(id));
              if (elem != null)
              {
                var eParam = elem.LookupParameter(elemParam);
                if (eParam != null)
                  fm.AssociateElementParameterToFamilyParameter(eParam, null);
              }
            }
          }
          catch (Autodesk.Revit.Exceptions.ApplicationException e)
          {
            System.Windows.MessageBox.Show(e.ToString(), RevitBinding.AddinName, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Hand);
            success = false;
          }

          if (success)
            transaction.Commit();
          else
            transaction.RollBack();
        }
      });
    }

    public void RefreshSelection()
    {
      RevitBinding.Instance.Context.Call("Refresh selected elements event handler", delegate (UIApplication app)
      {
        RevitBinding.Instance.ScanSelection(app);
      });
    }

    /// <summary>
    ///   параметры элемента
    /// </summary>
    private List<LinkedParam> GetElementParameters(Element elem)
    {
      var parameters = new List<LinkedParam>();

      Document doc = elem.Document;
      FamilyManager fm = doc.IsFamilyDocument ? doc.FamilyManager : null;

      var eParams = elem.GetOrderedParameters();
      foreach (var param in eParams)
        if (fm != null)
          if (fm.CanElementParameterBeAssociated(param))
          {
            var linkedFamilyParam = fm.GetAssociatedFamilyParameter(param);
            var linkedParamName = linkedFamilyParam?.Definition.Name;

            parameters.Add(new LinkedParam { Name = param.Definition.Name, LinkedFamilyParm = linkedParamName });
          }

      return parameters;
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
      var handler = System.Threading.Volatile.Read(ref PropertyChanged);
      handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
