using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.ExchangeCore.Shared
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Проставить статус соединения в абонентских ящиках подразделений.
    /// </summary>
    /// <param name="box">Абонентский ящик нашей организации.</param>
    public void SetDepartmentBoxConnectionStatus(IBusinessUnitBox box)
    {
      var departmentBoxes = Functions.BoxBase.Remote.GetActiveChildBoxes(box);
      var closedDepartmentBoxes = Functions.BoxBase.Remote.GetClosedChildBoxes(box);
      departmentBoxes.AddRange(closedDepartmentBoxes);
      departmentBoxes = departmentBoxes.Where(x => !Equals(x.ConnectionStatus, box.ConnectionStatus)).ToList();
      foreach (var departmentBox in departmentBoxes)
      {
        if (!Equals(departmentBox.ConnectionStatus, box.ConnectionStatus))
        {
          departmentBox.ConnectionStatus = box.ConnectionStatus;
          departmentBox.Save();
        }
      }
    }
  }
}