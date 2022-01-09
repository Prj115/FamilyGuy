/****************************************************************\

 Current editing family parameters

\****************************************************************/

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Autodesk.Revit.DB;

namespace FamilyGuy
{
  internal sealed class FamilyParametersModel : INotifyPropertyChanged
  {
    private Document doc = null;

    public event PropertyChangedEventHandler PropertyChanged;

    public void SetDocument(Document doc)
    {
      var newDoc = (doc != null && doc.IsFamilyDocument) ? doc : null;

      if (this.doc == newDoc)
        return;

      this.doc = newDoc;
      OnPropertyChanged("");
    }

    public List<FamilyParam> GetFamilyParameters()
    {
      var parameters = new List<FamilyParam>();

      if (doc != null)
      {
        var fParams = doc.FamilyManager.GetParameters();
        foreach (var param in fParams)
          parameters.Add(new FamilyParam { Name = param.Definition.Name, Formula = param.Formula });

        return parameters;
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
