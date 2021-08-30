using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionTask;
using Sungero.RecordManagement.Structures.ActionItemExecutionTask;

namespace Sungero.RecordManagement.Shared
{
  partial class ActionItemExecutionTaskFunctions
  {

    /// <summary>
    /// Установить обязательность свойств в зависимости от заполненных данных.
    /// </summary>
    public virtual void SetRequiredProperties()
    {
      var isComponentResolution = _obj.IsCompoundActionItem ?? false;
      
      _obj.State.Properties.Deadline.IsRequired = _obj.Info.Properties.Deadline.IsRequired || !isComponentResolution;
      _obj.State.Properties.Assignee.IsRequired = _obj.Info.Properties.Assignee.IsRequired || !isComponentResolution;
      _obj.State.Properties.ActionItem.IsRequired = _obj.Info.Properties.ActionItem.IsRequired || !isComponentResolution;
      
      // Проверить заполненность контролера, если поручение на контроле.
      _obj.State.Properties.Supervisor.IsRequired = _obj.Info.Properties.Supervisor.IsRequired || _obj.IsUnderControl == true;
    }
    
    /// <summary>
    /// Форматирует резолюцию в формате, необходимом для темы задачи/задания.
    /// </summary>
    /// <param name="actionItem">Резолюция.</param>
    /// <param name="hasDocument">Будет ли документ в теме задачи (т.е. нужно ли обрезать резолюцию).</param>
    /// <returns>Отформатированная разолюция.</returns>
    [Public]
    public static string FormatActionItemForSubject(string actionItem, bool hasDocument)
    {
      if (string.IsNullOrEmpty(actionItem))
        return string.Empty;
      
      // Убрать переносы.
      var formattedActionItem = actionItem.Replace(Environment.NewLine, " ").Replace("\n", " ");
      // Убрать двойные пробелы.
      formattedActionItem = formattedActionItem.Replace("   ", " ").Replace("  ", " ");
      
      // Обрезать размер резолюции для повышения информативности темы.
      if (formattedActionItem.Length > 50 && hasDocument)
        formattedActionItem = actionItem.Substring(0, 50) + "…";
      
      if (hasDocument)
        formattedActionItem = ActionItemExecutionTasks.Resources.SubtaskSubjectPostfixFormat(formattedActionItem);
      
      return formattedActionItem;
    }

    /// <summary>
    /// Получить тему поручения.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="beginningSubject">Изначальная тема.</param>
    /// <returns>Сформированная тема поручения.</returns>
    public static string GetActionItemExecutionSubject(IActionItemExecutionTask task, CommonLibrary.LocalizedString beginningSubject)
    {
      var autoSubject = Docflow.Resources.AutoformatTaskSubject.ToString();
      
      using (TenantInfo.Culture.SwitchTo())
      {
        var subject = beginningSubject.ToString();
        var actionItem = task.ActionItem;
        
        // Добавить резолюцию в тему.
        if (!string.IsNullOrWhiteSpace(actionItem))
        {
          var hasDocument = task.DocumentsGroup.OfficialDocuments.Any();
          var formattedResolution = Functions.ActionItemExecutionTask.FormatActionItemForSubject(actionItem, hasDocument);

          // Конкретно у уведомления о старте составного поручения - всегда рисуем с кавычками.
          if (!hasDocument && subject == ActionItemExecutionTasks.Resources.WorkFromActionItemIsCreatedCompound.ToString())
            formattedResolution = string.Format("\"{0}\"", formattedResolution);

          subject += string.Format(" {0}", formattedResolution);
        }
        
        // Добавить ">> " для тем подзадач.
        var isNotMainTask = task.ActionItemType != Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Main;
        if (isNotMainTask)
          subject = string.Format(">> {0}", subject);
        
        // Добавить имя документа, если поручение с документом.
        var document = task.DocumentsGroup.OfficialDocuments.FirstOrDefault();
        if (document != null)
          subject += ActionItemExecutionTasks.Resources.SubjectWithDocumentFormat(document.Name);
        
        subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
        
        if (subject != beginningSubject)
          return subject;
      }
      
      return autoSubject;
    }
    
    /// <summary>
    /// Проверить поручение на просроченность.
    /// </summary>
    /// <returns>True, если поручение просрочено.</returns>
    [Public]
    public virtual bool CheckOverdueActionItemExecutionTask()
    {
      if (_obj.IsCompoundActionItem != true)
      {
        // Проверить корректность срока.
        if (!Docflow.PublicFunctions.Module.CheckDeadline(_obj.Assignee, _obj.Deadline, Calendar.Now))
          return true;
      }
      else
      {
        // Проверить корректность срока.
        if (_obj.ActionItemParts.Any(j => !Docflow.PublicFunctions.Module.CheckDeadline(j.Assignee, j.Deadline, Calendar.Now)))
          return true;

        // Проверить корректность Общего срока.
        if (_obj.FinalDeadline != null && _obj.ActionItemParts.Any(p => p.Deadline == null) &&
            !Docflow.PublicFunctions.Module.CheckDeadline(_obj.FinalDeadline, Calendar.Now))
          return true;
      }
      
      return false;
    }

    /// <summary>
    /// Валидация старта задачи на исполнение поручения.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <param name="startedFromUI">Признак того, что задача была стартована через UI.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateActionItemExecutionTaskStart(Sungero.Core.IValidationArgs e, bool startedFromUI)
    {
      var isValid = Docflow.PublicFunctions.Module.ValidateTaskAuthor(_obj, e);
      
      // Проверить корректность заполнения свойства Выдал.
      if (!(Sungero.Company.Employees.Current == null && Users.Current.IncludedIn(Roles.Administrators)) &&
          !Docflow.PublicFunctions.Module.Remote.IsUsersCanBeResolutionAuthor(_obj.DocumentsGroup.OfficialDocuments.SingleOrDefault(), _obj.AssignedBy))
      {
        e.AddError(_obj.Info.Properties.AssignedBy, ActionItemExecutionTasks.Resources.ActionItemCanNotAssignedByUser);
        isValid = false;
      }
      
      // Проверить количество исполнителей по поручению.
      if (_obj.ActionItemParts.Count() + _obj.CoAssignees.Count() > Constants.ActionItemExecutionTask.MaxActionItemAssignee)
      {
        e.AddError(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ActionItemAsigneeTooMatchFormat(Constants.ActionItemExecutionTask.MaxActionItemAssignee));
        isValid = false;
      }
      
      // Проверить корректность срока (только при старте через UI).
      if (startedFromUI && Functions.ActionItemExecutionTask.CheckOverdueActionItemExecutionTask(_obj))
      {
        e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday);
        isValid = false;
      }
      
      return isValid;
    }
    
    /// <summary>
    /// Валидация сохранения задачи на исполнение поручения.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateActionItemExecutionTaskSave(Sungero.Core.IValidationArgs e)
    {
      var isValid = true;
      
      // Проверить заполненость Общего срока (а также корректность), исполнителей, текста поручения у составного поручения.
      var isCompoundActionItem = _obj.IsCompoundActionItem ?? false;
      if (isCompoundActionItem)
      {
        if (_obj.ActionItemParts.Count == 0)
        {
          e.AddError(_obj.Info.Properties.ActionItemParts, ActionItemExecutionTasks.Resources.ActionItemsNotFilled);
          isValid = false;
        }
        
        if (_obj.FinalDeadline == null && _obj.ActionItemParts.Any(i => i.Deadline == null))
        {
          e.AddError(_obj.Info.Properties.FinalDeadline, ActionItemExecutionTasks.Resources.EmptyFinalDeadline);
          isValid = false;
        }
        
        if (string.IsNullOrEmpty(_obj.ActionItem) && _obj.ActionItemParts.Any(i => string.IsNullOrEmpty(i.ActionItemPart)))
        {
          e.AddError(_obj.Info.Properties.ActionItem, ActionItemExecutionTasks.Resources.EmptyActionItem);
          isValid = false;
        }
      }
      
      return isValid;
    }
  }
}