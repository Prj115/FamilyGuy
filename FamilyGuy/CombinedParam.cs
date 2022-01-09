/****************************************************************\

 Объединение нескольких одноименных параметров принадлежащих разным элементам
 также информация о том, с какими параметрами семейства у них есть связь

\****************************************************************/

using System.Collections.Generic;

namespace FamilyGuy
{
  internal sealed class UnnamedLinkToFamilyParam
  {
    public int ElementId;
    public string FamilyParamName { get; set; }
  }

  internal sealed class CombinedParam
  {
    public string Name { get; set; }
    public List<UnnamedLinkToFamilyParam> Links;
  }
}
