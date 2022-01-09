/****************************************************************\

 FamilyParameters and seleceted elements lists
 view model

\****************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace FamilyGuy.Forms
{
  internal enum ParametersSortType
  {
    Original,
    Alphabet,
    OriginalUntangled,
    AlphabetUntangled
  }

  internal sealed class ParametersBindingViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    private readonly FamilyParametersModel familyParametersModel;
    private readonly SelectedElementsParametersModel selectedElementsModel;

    private ParametersSortType familySortType = ParametersSortType.Original;

    private List<FamilyParam> unsortedFamilyParameters = null;
    private List<FamilyParam> mFamilyParameters = null;
    public List<FamilyParam> FamilyParameters
    {
      get => mFamilyParameters;
      private set
      {
        mFamilyParameters = value;
        OnPropertyChanged();
      }
    }

    public List<string> SelectedElements { get; private set; }

    /// <summary>
    ///   индекс семейства, индекс выделенного, все ли элементы выделения имеют эту связь
    /// </summary>
    private List<Tuple<int, int, bool>> mAssociationMap = null;
    public List<Tuple<int, int, bool>> AssociationMap
    {
      get => mAssociationMap;
      private set
      {
        mAssociationMap = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    ///   Selected elements parameters
    /// </summary>
    private List<CombinedParam> mElementParameters = null;
    public List<CombinedParam> ElementParameters
    {
      get => mElementParameters;
      private set
      {
        mElementParameters = value;
        OnPropertyChanged();
      }
    }

    public ParametersBindingViewModel(FamilyParametersModel familyParameters, SelectedElementsParametersModel selectedElements)
    {
      familyParametersModel = familyParameters;
      familyParametersModel.PropertyChanged += FamilyParametersChanged;

      selectedElementsModel = selectedElements;
      selectedElementsModel.PropertyChanged += SelectedElementsChanged;
    }

    private void FamilyParametersChanged(object sender, PropertyChangedEventArgs e)
    {
      unsortedFamilyParameters = familyParametersModel.GetFamilyParameters();
      SortFamilyParameters();
    }

    private void SelectedElementsChanged(object sender, PropertyChangedEventArgs e)
    {
      var familyElements = selectedElementsModel.GetSelectedElements();

      familyElements.Sort((x, y) => x.Parameters.Count - y.Parameters.Count);

      // объединенный список одноименных параметров выделенных элементов
      var combinedList = new List<CombinedParam>();
      foreach (var elem in familyElements)
        foreach (var param in elem.Parameters)
        {
          var existNameIdx = combinedList.FindIndex((x) => x.Name == param.Name);
          CombinedParam comboParam = null;
          if (existNameIdx < 0)
            combinedList.Add(comboParam = new CombinedParam { Name = param.Name, Links = new List<UnnamedLinkToFamilyParam>() });
          else
            comboParam = combinedList[existNameIdx];

          comboParam.Links.Add(new UnnamedLinkToFamilyParam { ElementId = elem.ElementId, FamilyParamName = param.LinkedFamilyParm });
        }
      //TODO если Links.Count < familyElements.Count значит этот параметр есть не во всех выделенных элементах - текст серый

      SelectedElements = familyElements.Select((x) => x.ElementName).ToList();
      ElementParameters = combinedList;

      SortFamilyParameters();
      UpdateAssociationMap();
    }

    // сортировка и распутывание связей
    private void SortFamilyParameters()
    {
      List<FamilyParam> sorted = unsortedFamilyParameters.ToList();

      // alphabetical sort
      if (familySortType == ParametersSortType.Alphabet || familySortType == ParametersSortType.AlphabetUntangled)
        sorted.Sort((x, y) => String.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase));

      // untangle
      if ((familySortType == ParametersSortType.OriginalUntangled || familySortType == ParametersSortType.AlphabetUntangled) && mElementParameters != null)
      {
        // список имен параметров семейства с индексом связанных параметров в другом списке
        var familyNamesIdxElems = new List<Tuple<FamilyParam, int>>();
        foreach (var fParam in sorted)
        {
          int linkedIdx = mElementParameters.FindLastIndex(x => x.Links.Any(y => y.FamilyParamName == fParam.Name));

          const int DummyUnassignedIndex = 9999;
          linkedIdx = linkedIdx == -1 ? DummyUnassignedIndex : linkedIdx;

          familyNamesIdxElems.Add(new Tuple<FamilyParam, int>(fParam, linkedIdx));
        }

        sorted = familyNamesIdxElems.OrderBy(x => x.Item2).Select(x => x.Item1).ToList();
      }

      FamilyParameters = sorted;
    }

    // построить карту связанности по индексам списков
    private void UpdateAssociationMap()
    {
      if (mElementParameters == null)
        return;

      var newAssociationMap = new List<Tuple<int, int, bool>>();

      int nameIdx = 0;
      foreach (var comboParam in mElementParameters)
      {
        //если один из Links[x].FamilyParamName == null значит не все элементы привязаны - линия серая
        bool isAllLinked = !comboParam.Links.Any((x) => x.FamilyParamName == null);

        HashSet<string> uniqueLinks = new HashSet<string>();
        foreach (var link in comboParam.Links)
          if (link.FamilyParamName != null)
            uniqueLinks.Add(link.FamilyParamName);

        foreach (var link in uniqueLinks)
        {
          var fpIdx = mFamilyParameters.FindIndex((x) => x.Name == link);
          if (fpIdx >= 0)
            newAssociationMap.Add(new Tuple<int, int, bool>(fpIdx, nameIdx, isAllLinked));
        }

        nameIdx++;
      }

      AssociationMap = newAssociationMap;
    }

    public void SetParametersSortType(ParametersSortType sort)
    {
      familySortType = sort;
      SortFamilyParameters();
      UpdateAssociationMap();
    }

    public void AssociateParameters(FamilyParam familyParam, CombinedParam elementsParam)
    {
      List<int> elemIds = elementsParam.Links.Select((x) => x.ElementId).ToList();
      selectedElementsModel.AssociateFamilyParameter(familyParam.Name, elementsParam.Name, elemIds);
    }

    public void DisassociateFamilyParameter(FamilyParam familyParam)
    {
      selectedElementsModel.DisassociateFamilyParameter(familyParam.Name);
    }

    public void DisassociateElementsParameter(CombinedParam elementsParam)
    {
      var ids = elementsParam.Links.Select((x) => x.ElementId).ToList();
      selectedElementsModel.DisassociateElementsParameter(elementsParam.Name, ids);
    }

    public void RefreshSelection()
    {
      selectedElementsModel.RefreshSelection();
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
      var handler = System.Threading.Volatile.Read(ref PropertyChanged);
      handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
