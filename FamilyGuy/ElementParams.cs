/****************************************************************\

 Element and his parameters
 for parameters - bound FamilyParameters

\****************************************************************/

using System.Collections.Generic;


namespace FamilyGuy
{
  internal sealed class LinkedParam
  {
    public string Name { get; set; }
    public string LinkedFamilyParm { get; set; }
  }

  internal sealed class ElementParams
  {
    public int ElementId;
    public string ElementName;
    public List<LinkedParam> Parameters;
  }
}
