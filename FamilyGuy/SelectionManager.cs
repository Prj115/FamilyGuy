/****************************************************************\

 Watch selected elements changes

\****************************************************************/

using System.Collections.Generic;

using Autodesk.Revit.DB;


namespace FamilyGuy
{
  internal sealed class SelectionManager
  {
    public List<Element> Selection { get; } = new List<Element>();

    private int[] prevSelection = new int[0];

    public void ResetSelection()
    {
      Selection.Clear();
      prevSelection = new int[0];
    }

    public bool IsSelected(int id)
    {
      foreach (var selId in prevSelection)
        if (selId == id)
          return true;

      return false;
    }

    public bool IsSelectionChanged(Document doc, ICollection<ElementId> currentSelection)
    {
      if (currentSelection.Count == prevSelection.Length)
      {
        bool isTheSame = true;
        int i = 0;
        foreach (var elemId in currentSelection)
          if (elemId.IntegerValue != prevSelection[i++])
          {
            isTheSame = false;
            break;
          }

        if (isTheSame)
          return false;
      }

      UpdateSelection(doc, currentSelection);

      return true;
    }

    private void UpdateSelection(Document doc, ICollection<ElementId> currentSelection)
    {
      prevSelection = new int[currentSelection.Count];

      int i = 0;
      Selection.Clear();
      foreach (var elemId in currentSelection)
      {
        prevSelection[i++] = elemId.IntegerValue;

        Element elem = doc.GetElement(elemId);
        if (elem != null)
          Selection.Add(elem);
      }
    }
  }
}
