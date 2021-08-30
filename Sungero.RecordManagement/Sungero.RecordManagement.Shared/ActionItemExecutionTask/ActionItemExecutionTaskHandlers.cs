using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionTask;

namespace Sungero.RecordManagement
{

  partial class ActionItemExecutionTaskActionItemPartsSharedCollectionHandlers
  {
    public virtual void ActionItemPartsAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      // Задать порядковый номер для пункта поручения.
      var lastNumber = _obj.ActionItemParts.OrderBy(j => j.Number).LastOrDefault();
      if (lastNumber.Number.HasValue)
        _added.Number = lastNumber.Number + 1;
      else
        _added.Number = 1;
    }
  }

  partial class ActionItemExecutionTaskSharedHandlers
  {

    public virtual void ActionItemPartsChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void CoAssigneesChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void IsUnderControlChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      _obj.State.Properties.Supervisor.IsEnabled = e.NewValue ?? false;
      _obj.Supervisor = e.NewValue.Value ?
        Docflow.PublicFunctions.PersonalSetting.GetSupervisor(Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(Employees.Current)) :
        null;
      Functions.ActionItemExecutionTask.SetRequiredProperties(_obj);
    }

    public virtual void AssignedByChanged(Sungero.RecordManagement.Shared.ActionItemExecutionTaskAssignedByChangedEventArgs e)
    {
      if (e.NewValue != null)
        _obj.Author = e.NewValue;
    }
    
    public virtual void DocumentsGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      var subjectTemplate = _obj.IsCompoundActionItem == true ?
        ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
        ActionItemExecutionTasks.Resources.TaskSubject;
      _obj.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, subjectTemplate);
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault());
    }

    public virtual void DocumentsGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      var document = Docflow.OfficialDocuments.As(e.Attachment);
      // Заполнить исполнителя из письма.
      if (_obj.IsCompoundActionItem == true ? !_obj.ActionItemParts.Any() : _obj.Assignee == null)
      {
        if (document != null && document.Assignee != null)
        {
          if (_obj.IsCompoundActionItem == true)
            _obj.ActionItemParts.AddNew().Assignee = document.Assignee;
          else
            _obj.Assignee = document.Assignee;
        }
      }

      var subjectTemplate = _obj.IsCompoundActionItem == true ?
        ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
        ActionItemExecutionTasks.Resources.TaskSubject;
      _obj.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, subjectTemplate);
      
      if (!_obj.State.IsCopied)
        Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);
      
      Docflow.PublicFunctions.OfficialDocument.DocumentAttachedInMainGroup(document, _obj);
    }

    public virtual void ActionItemChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (!Equals(e.NewValue, e.OldValue))
      {
        // Установить тему.
        var subjectTemplate = _obj.IsCompoundActionItem == true ?
          ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
          ActionItemExecutionTasks.Resources.TaskSubject;
        _obj.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, subjectTemplate);
        
        // Заменить первый символ на прописной.
        _obj.ActionItem = _obj.ActionItem != null ? _obj.ActionItem.Trim() : string.Empty;
        _obj.ActionItem = Docflow.PublicFunctions.Module.ReplaceFirstSymbolToUpperCase(_obj.ActionItem);
        
        if (_obj.IsCompoundActionItem != true)
        {

          if (_obj.ActionItemType == ActionItemType.Main)
            _obj.ActiveText = _obj.ActionItem;
        }
      }
      _obj.State.Controls.Control.Refresh();
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // TODO: удалить код после исправления бага 17930 (сейчас этот баг в TFS недоступен, он про автоматическое обрезание темы).
      if (e.NewValue != null && e.NewValue.Length > ActionItemExecutionTasks.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, ActionItemExecutionTasks.Info.Properties.Subject.Length);
    }

    public virtual void DeadlineChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      _obj.MaxDeadline = e.NewValue;
    }

    public virtual void IsCompoundActionItemChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.OldValue != e.NewValue)
      {
        // Заполнить данные из составного поручения в обычное и наоборот.
        if (e.NewValue.Value)
        {
          // Составное поручение.
          _obj.ActionItemParts.Clear();
          _obj.FinalDeadline = _obj.Deadline;
          
          if (_obj.Assignee != null)
          {
            var newJob = _obj.ActionItemParts.AddNew();
            newJob.Assignee = _obj.Assignee;
          }
          
          foreach (var job in _obj.CoAssignees)
          {
            var newJob = _obj.ActionItemParts.AddNew();
            newJob.Assignee = job.Assignee;
          }
        }
        else
        {
          // Не составное поручение.
          var actionItemPart = _obj.ActionItemParts.OrderBy(x => x.Number).FirstOrDefault();
          if (_obj.FinalDeadline != null)
            _obj.Deadline = _obj.FinalDeadline;
          else if (actionItemPart != null)
            _obj.Deadline = actionItemPart.Deadline;
          else
            _obj.Deadline = null;
          
          if (actionItemPart != null)
            _obj.Assignee = actionItemPart.Assignee;
          else
            _obj.Assignee = null;
          
          _obj.CoAssignees.Clear();
          
          foreach (var job in _obj.ActionItemParts.OrderBy(x => x.Number).Skip(1))
          {
            if (job.Assignee != null && !_obj.CoAssignees.Select(z => z.Assignee).Contains(job.Assignee))
              _obj.CoAssignees.AddNew().Assignee = job.Assignee;
          }
          
          if (string.IsNullOrEmpty(_obj.ActionItem) && actionItemPart != null)
          {
            _obj.ActionItem = actionItemPart.ActionItemPart;
          }

          // Чистим грид в составном, чтобы не мешать валидации.
          _obj.ActionItemParts.Clear();
        }
        
        // Установить тему.
        var subjectTemplate = _obj.IsCompoundActionItem == true ?
          ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
          ActionItemExecutionTasks.Resources.TaskSubject;
        _obj.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, subjectTemplate);
      }
      Functions.ActionItemExecutionTask.SetRequiredProperties(_obj);
      _obj.State.Controls.Control.Refresh();
    }
  }
}