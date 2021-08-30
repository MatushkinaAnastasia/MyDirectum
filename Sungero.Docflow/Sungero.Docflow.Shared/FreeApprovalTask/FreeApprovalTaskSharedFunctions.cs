using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalTask;
using Sungero.Docflow.Structures.FreeApprovalTask;

namespace Sungero.Docflow.Shared
{
  partial class FreeApprovalTaskFunctions
  {
    /// <summary>
    /// Получить сообщения валидации при старте.
    /// </summary>
    /// <returns>Сообщения валидации.</returns>
    public virtual List<StartValidationMessage> GetStartValidationMessages()
    {
      var errors = new List<StartValidationMessage>();
      
      // Задачу может отправить только сотрудник.
      var authorIsNonEmployeeMessage = Docflow.PublicFunctions.Module.ValidateTaskAuthor(_obj);
      if (!string.IsNullOrWhiteSpace(authorIsNonEmployeeMessage))
        errors.Add(StartValidationMessage.Create(authorIsNonEmployeeMessage, false, true));
      
      // Проверить корректность срока.
      if (!Functions.Module.CheckDeadline(_obj.MaxDeadline, Calendar.Now))
        errors.Add(StartValidationMessage.Create(FreeApprovalTasks.Resources.ImpossibleSpecifyDeadlineLessThanToday, true, false));
      
      // Проверить права на изменение документа.
      if (!_obj.ForApprovalGroup.ElectronicDocuments.First().AccessRights.CanUpdate())
        errors.Add(StartValidationMessage.Create(FreeApprovalTasks.Resources.CantSendDocumentsWithoutUpdateRights, false, false));
      
      return errors;
    }
    
    /// <summary>
    /// Валидация старта задачи на свободное согласование.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateFreeApprovalTaskStart(Sungero.Core.IValidationArgs e)
    {
      var errorMessages = this.GetStartValidationMessages();
      if (errorMessages.Any())
      {
        foreach (var error in errorMessages)
        {
          if (error.IsCantSendTaskByNonEmployeeMessage)
            e.AddError(_obj.Info.Properties.Author, error.Message);
          else if (error.IsImpossibleSpecifyDeadlineLessThanTodayMessage)
            e.AddError(_obj.Info.Properties.MaxDeadline, error.Message);
          else
            e.AddError(error.Message);
        }
        return false;
      }
      
      return true;
    }

  }
}