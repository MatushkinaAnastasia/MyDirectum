using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Shared
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Получить список guid для всех системных реципиентов за исключением "Все пользователи".
    /// </summary>
    /// <param name="fullSystemRecipientList">Включать в список системные роли: Администраторы, Аудиторы, Пользователи Solo, Менеджеры системы. </param>
    /// <returns>Список guid для системных реципиентов за исключением "Все пользователи".</returns>
    [Public]
    public virtual List<Guid> GetSystemRecipientsSidWithoutAllUsers(bool fullSystemRecipientList)
    {
      var systemRecipientsSid = new List<Guid>();
      if (fullSystemRecipientList)
      {
        systemRecipientsSid.Add(Sungero.Domain.Shared.SystemRoleSid.Administrators);
        systemRecipientsSid.Add(Sungero.Domain.Shared.SystemRoleSid.Auditors);
        systemRecipientsSid.Add(Sungero.Domain.Shared.SystemRoleSid.SoloUsers);
        systemRecipientsSid.Add(Sungero.Domain.Shared.SystemRoleSid.DeliveryUsersSid);
      }
      
      systemRecipientsSid.Add(Sungero.Domain.Shared.SystemRoleSid.ConfigurationManagers);
      systemRecipientsSid.Add(Sungero.Domain.Shared.SystemRoleSid.ServiceUsers);
      systemRecipientsSid.Add(Projects.PublicConstants.Module.RoleGuid.ParentProjectTeam);
      systemRecipientsSid.Add(Docflow.PublicConstants.Module.CollaborationService);
      systemRecipientsSid.Add(Docflow.PublicConstants.Module.DefaultGroup);
      systemRecipientsSid.Add(Docflow.PublicConstants.Module.DefaultUser);
      
      return systemRecipientsSid;
    }
  }
}