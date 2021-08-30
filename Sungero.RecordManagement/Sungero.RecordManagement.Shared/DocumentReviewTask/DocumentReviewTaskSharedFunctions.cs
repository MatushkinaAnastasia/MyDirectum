using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.RecordManagement.DocumentReviewTask;
using Sungero.RecordManagement.Structures.DocumentReviewTask;

namespace Sungero.RecordManagement.Shared
{
  partial class DocumentReviewTaskFunctions
  {
    /// <summary>
    /// Получить сообщения валидации при старте.
    /// </summary>
    /// <returns>Сообщения валидации.</returns>
    public virtual List<StartValidationMessage> GetStartValidationMessages()
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      var errors = new List<StartValidationMessage>();
      
      var authorIsNonEmployeeMessage = Docflow.PublicFunctions.Module.ValidateTaskAuthor(_obj);
      if (!string.IsNullOrWhiteSpace(authorIsNonEmployeeMessage))
        errors.Add(StartValidationMessage.Create(authorIsNonEmployeeMessage, false, true));
      
      // Документ на исполнении нельзя отправлять на рассмотрение.
      if (document != null && document.ExecutionState == Docflow.OfficialDocument.ExecutionState.OnExecution)
        errors.Add(StartValidationMessage.Create(DocumentReviewTasks.Resources.DocumentOnExecution, false, false));
      
      // Проверить корректность срока.
      if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.Addressee, _obj.Deadline, Calendar.Now))
        errors.Add(StartValidationMessage.Create(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday, true, false));
      
      // Проверить, что входящий документ зарегистрирован.
      if (!Functions.DocumentReviewTask.IncomingDocumentRegistered(document))
        errors.Add(StartValidationMessage.Create(DocumentReviewTasks.Resources.IncomingDocumentMustBeRegistered, false, false));
      
      return errors;
    }
    
    /// <summary>
    /// Валидация старта задачи на рассмотрение.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateDocumentReviewTaskStart(Sungero.Core.IValidationArgs e)
    {
      var errorMessages = this.GetStartValidationMessages();
      if (errorMessages.Any())
      {
        foreach (var error in errorMessages)
        {
          if (error.IsCantSendTaskByNonEmployeeMessage)
            e.AddError(_obj.Info.Properties.Author, error.Message);
          else if (error.IsImpossibleSpecifyDeadlineLessThanTodayMessage)
            e.AddError(_obj.Info.Properties.Deadline, error.Message);
          else
            e.AddError(error.Message);
        }
        return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Проверка, зарегистрирован ли входящий документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если документ зарегистрирован, либо документ не входящий.</returns>
    public static bool IncomingDocumentRegistered(IOfficialDocument document)
    {
      if (document == null || document.DocumentKind == null)
        return true;
      
      var documentKind = document.DocumentKind;
      return documentKind.DocumentFlow != Docflow.DocumentKind.DocumentFlow.Incoming ||
        documentKind.NumberingType != Docflow.DocumentKind.NumberingType.Registrable ||
        document.RegistrationState == Docflow.OfficialDocument.RegistrationState.Registered;
    }
    
    /// <summary>
    /// Получить список просроченных задач на исполнение поручения в состоянии Черновик.
    /// </summary>
    /// <returns>Список просроченных задач на исполнение поручения в состоянии Черновик.</returns>
    public virtual List<IActionItemExecutionTask> GetDraftOverdueActionItemExecutionTasks()
    {
      var tasks = _obj.ResolutionGroup.ActionItemExecutionTasks.Where(t => t.Status == RecordManagement.ActionItemExecutionTask.Status.Draft);
      var overdueTasks = new List<IActionItemExecutionTask>();
      foreach (var task in tasks)
        if (Functions.ActionItemExecutionTask.CheckOverdueActionItemExecutionTask(task))
          overdueTasks.Add(task);
      
      return overdueTasks;
    }
  }
}