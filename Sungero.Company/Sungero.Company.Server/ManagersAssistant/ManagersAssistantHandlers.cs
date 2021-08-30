using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company.ManagersAssistant;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company
{
  partial class ManagersAssistantServerHandlers
  {
    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.PreparesResolution = false;
    }
    
    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.Status == CoreEntities.DatabookEntry.Status.Closed)
        return;
      
      if (_obj.Manager == null || _obj.Assistant == null)
        return;
      
      // Руководитель не мб помощником сам у себя.
      if (Equals(_obj.Manager, _obj.Assistant))
        e.AddError(ManagersAssistants.Resources.ManagerCanNotBeAssistantForHimself);
      
      // Найти дубли.
      var equalsManagersAssistants = ManagersAssistants
        .GetAll(m => m.Status == CoreEntities.DatabookEntry.Status.Active)
        .Where(m => !Equals(m, _obj))
        .Where(m => Equals(m.Manager, _obj.Manager));
      if (equalsManagersAssistants.Any())
        e.AddError(ManagersAssistants.Resources.ExecutiveSecretaryIsAppointedFormat(_obj.Manager.Name));
    }
  }
}