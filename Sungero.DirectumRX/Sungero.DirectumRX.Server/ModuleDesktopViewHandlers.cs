using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.DirectumRX.Server
{
  internal static class DesktopViewHandlers
  {

    private static bool CanApplySecretary()
    {
      // Делопроизводитель.
      var isClerk = Docflow.PublicFunctions.Module.Remote.IncludedInClerksRole();
      if (isClerk)
        return true;
      
      // Помощник руководителя.
      return Company.ManagersAssistants.GetAll().Any(m => m.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
                                                  Equals(m.Assistant, Users.Current));
    }

    private static bool CanApplyManager()
    {
      return Docflow.PublicFunctions.Module.IncludedInDepartmentManagersRole();
    }

    private static bool CanApplyTopManager()
    {
      return Docflow.PublicFunctions.Module.IncludedInBusinessUnitHeadsRole();
    }
  }

}