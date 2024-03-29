﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionTask;
using Sungero.RecordManagement.ActionItemSupervisorAssignment;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server
{
  partial class ActionItemExecutionTaskRouteHandlers
  {
    
    #region - 2 - Уведомление контролеру

    public virtual void StartBlock2(Sungero.RecordManagement.Server.ActionItemSupervisorNotificationArguments e)
    {
      // Задать тему.
      e.Block.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.ControlNoticeSubject);
      
      // Задать исполнителя.
      if (_obj.Supervisor != null && _obj.Author != _obj.Supervisor && _obj.StartedBy != _obj.Supervisor && (_obj.ActionItemType == ActionItemType.Main))
        e.Block.Performers.Add(_obj.Supervisor);

      Functions.ActionItemExecutionTask.GrantRightsToAttachments(_obj, _obj.DocumentsGroup.All.ToList(), true);
      Functions.ActionItemExecutionTask.GrantRightsToAttachments(_obj, _obj.AddendaGroup.All.ToList(), true);
      
      var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
      {
        var documentsGroupGuid = Docflow.PublicConstants.Module.TaskMainGroup.ActionItemExecutionTask;
        var nonStartedActiveTasks = ActionItemExecutionTasks
          .GetAll(t => t.AttachmentDetails.Any(a => a.GroupId == documentsGroupGuid && a.AttachmentId == document.Id) && t.Status == Workflow.Task.Status.InProcess && t.Id != _obj.Id)
          .ToList();

        foreach (var task in nonStartedActiveTasks)
        {
          var hasAssignments = Workflow.Assignments.GetAll(s => Equals(task, s.Task)).Any();
          var hasSubTasks = Workflow.Tasks.GetAll(s => Equals(task, s.ParentTask)).Any();
          if (!hasAssignments && !hasSubTasks)
          {
            Logger.DebugFormat("Granting rights for task {0}. Current Task: {1}", task.Id, _obj.Id);
            Functions.ActionItemExecutionTask.GrantRightsToAttachments(task, task.DocumentsGroup.All.ToList(), true);
            Functions.ActionItemExecutionTask.GrantRightsToAttachments(task, task.AddendaGroup.All.ToList(), true);
          }
        }
      }
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault());
      
      // Выдача прав соисполнителям и составным, чтобы Script10Execute в цикле не зависал при блокировках документа
      if (document != null)
      {
        var assignees = _obj.CoAssignees.Select(a => a.Assignee);
        var partAssignees = _obj.ActionItemParts.Select(p => p.Assignee);
        foreach (var assignee in assignees.Concat(partAssignees))
        {
          if (!document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Read, assignee))
            document.AccessRights.Grant(assignee, DefaultAccessRightsTypes.Read);
        }
      }
      
      // Задать состояние поручения.
      if (_obj.ExecutionState != ExecutionState.OnRework && _obj.Assignee != null)
        _obj.ExecutionState = ExecutionState.OnExecution;
      
      // Заполнить исполнителя, если это первое поручение по документу.
      if (document != null && document.Assignee == null && document.State.Properties.Assignee.IsVisible)
      {
        document.Assignee = _obj.Assignee;
      }
      
      // Обновить статус исполнения документа
      Functions.ActionItemExecutionTask.SetDocumentStates(_obj);
    }
    
    public virtual void StartNotice2(Sungero.RecordManagement.IActionItemSupervisorNotification notice, Sungero.RecordManagement.Server.ActionItemSupervisorNotificationArguments e)
    {
      notice.Importance = _obj.Importance;
    }
    
    public virtual void EndBlock2(Sungero.RecordManagement.Server.ActionItemSupervisorNotificationEndBlockEventArguments e)
    {
    }

    #endregion
    
    #region - 3 - Есть исполнитель?

    public virtual bool Decision3Result()
    {
      return _obj.Assignee != null;
    }
    
    #endregion
    
    #region - 4 - Исполнение поручения

    public virtual void StartBlock4(Sungero.RecordManagement.Server.ActionItemExecutionAssignmentArguments e)
    {
      // Задать тему, исполнителей и срок.
      if (_obj.ExecutionState == ExecutionState.OnControl)
        e.Block.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.ActionItemReworkSubject);
      else
        e.Block.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.ActionItemExecutionSubject);
      
      e.Block.Performers.Add(_obj.Assignee);
      if (_obj.Deadline.HasValue)
      {
        e.Block.AbsoluteDeadline = _obj.Deadline.Value;
        e.Block.ScheduledDate = _obj.Deadline.Value;
      }
      
      // Выдать права на документ.
      var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
        Docflow.PublicFunctions.OfficialDocument.GrantAccessRightsToActionItemAttachment(document, _obj.Assignee);
      
      // Для подзадач соисполнителям заполнять "Выдал" из основной задачи.
      IActionItemExecutionTask actionItemTask = null;
      if (_obj.ActionItemType != ActionItemType.Main)
      {
        var mainActionItemExecution = ActionItemExecutionTasks.As(_obj.MainTask);
        if (mainActionItemExecution != null && !(mainActionItemExecution.IsCompoundActionItem ?? false))
          actionItemTask = mainActionItemExecution;
      }
      if (actionItemTask == null)
        actionItemTask = _obj;
      e.Block.AssignedBy = actionItemTask.AssignedBy;
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);
    }
    
    public virtual void StartAssignment4(Sungero.RecordManagement.IActionItemExecutionAssignment assignment, Sungero.RecordManagement.Server.ActionItemExecutionAssignmentArguments e)
    {
      assignment.ActionItem = _obj.ActionItem;
      assignment.Importance = _obj.Importance;
      if (_obj.ActionItemType == ActionItemType.Additional)
        assignment.Author = Sungero.RecordManagement.ActionItemExecutionTasks.As(_obj.ParentAssignment.Task).AssignedBy;
      
      // Выдать права на изменение для возможности прекращения подзадач.
      Functions.ActionItemExecutionTask.GrantAccessRightToAssignment(assignment, _obj);
    }

    public virtual void CompleteAssignment4(Sungero.RecordManagement.IActionItemExecutionAssignment assignment, Sungero.RecordManagement.Server.ActionItemExecutionAssignmentArguments e)
    {
      // Переписка.
      _obj.Report = assignment.ActiveText;
      
      // Завершить задание на продление срока, если оно есть.
      var extendDeadlineTasks = DeadlineExtensionTasks.GetAll(j => Equals(j.ParentAssignment, assignment) &&
                                                              j.Status == Workflow.Task.Status.InProcess);
      foreach (var extendDeadlineTask in extendDeadlineTasks)
        extendDeadlineTask.Abort();
      
      // Завершить задание на продление срока, если оно есть.
      var newExtendDeadlineTasks = Docflow.DeadlineExtensionTasks.GetAll(j => Equals(j.ParentAssignment, assignment) &&
                                                                         j.Status == Workflow.Task.Status.InProcess);
      foreach (var newExtendDeadlineTask in newExtendDeadlineTasks)
        newExtendDeadlineTask.Abort();
      
      // Завершить задание на запрос отчёта, если оно есть.
      var reportRequestTasks = StatusReportRequestTasks.GetAll(r => Equals(r.ParentTask, assignment.Task) &&
                                                               r.Status == Workflow.Task.Status.InProcess);
      foreach (var reportRequestTask in reportRequestTasks)
        reportRequestTask.Abort();

      // Рекурсивно прекратить подзадачи.
      if (assignment.NeedAbortChildActionItems ?? false)
        Functions.Module.AbortSubtasksAndSendNotices(_obj, assignment.Performer, ActionItemExecutionTasks.Resources.AutoAbortingReason);
      
      // Добавить документы из группы "Результаты исполнения" соисполнителя в группу основновного поручения.
      if (_obj.ActionItemType == ActionItemType.Additional)
      {
        var parentActionItem = ActionItemExecutionTasks.As(_obj.ParentAssignment.Task);
        if (parentActionItem != null)
        {
          var documentGroup = parentActionItem.ResultGroup.OfficialDocuments;
          foreach (var document in _obj.ResultGroup.OfficialDocuments)
          {
            if (!documentGroup.Contains(document))
              documentGroup.Add(document);
          }
          
          // Выдать права на вложенные документы.
          Functions.ActionItemExecutionTask.GrantRightsToAttachments(parentActionItem, parentActionItem.ResultGroup.All.ToList(), false);
        }
      }
      
      // Добавить документы из группы "Результаты исполнения" из подчиненего поручения в задание на исполнение.
      if (_obj.ActionItemType == ActionItemType.Main && _obj.ParentAssignment != null)
      {
        var parentAssignment = ActionItemExecutionAssignments.As(_obj.ParentAssignment);
        if (parentAssignment != null)
        {
          var parentActionItem = ActionItemExecutionTasks.As(parentAssignment.Task);
          var documentGroup = parentAssignment.ResultGroup.OfficialDocuments;
          foreach (var document in _obj.ResultGroup.OfficialDocuments)
          {
            if (!documentGroup.Contains(document))
              documentGroup.Add(document);
          }
          
          // Выдать права на вложенные документы.
          Functions.ActionItemExecutionTask.GrantRightsToAttachments(parentActionItem, parentActionItem.ResultGroup.All.ToList(), false);
        }
      }

      // Выдать права на вложенные документы.
      Functions.ActionItemExecutionTask.GrantRightsToAttachments(_obj, _obj.ResultGroup.All.ToList(), false);
      
      // Связать документы из группы "Результаты исполнения" с основным документом.
      var mainDocument = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (mainDocument != null)
      {
        foreach (var document in _obj.ResultGroup.OfficialDocuments.Where(d => !Equals(d, mainDocument)))
        {
          document.Relations.AddFrom(Constants.Module.SimpleRelationRelationName, mainDocument);
          document.Save();
        }
      }
    }
    
    public virtual void EndBlock4(Sungero.RecordManagement.Server.ActionItemExecutionAssignmentEndBlockEventArguments e)
    {
      // Заполнить фактическую дату завершения исполнения поручения.
      var completed = e.CreatedAssignments.Select(a => a.Completed).OrderByDescending(a => a).FirstOrDefault();
      if (completed != null)
      {
        _obj.ActualDate = e.Block.AbsoluteDeadline.HasTime()
          ? completed
          : Calendar.GetUserToday(e.Block.Performers.FirstOrDefault());
      }
    }

    #endregion
    
    #region - 5 - Нужен контроль?

    public virtual bool Decision5Result()
    {
      return _obj.Supervisor != null && _obj.Supervisor != _obj.Assignee;
    }
    
    #endregion
    
    #region - 6 - Контроль
    
    public virtual void StartBlock6(Sungero.RecordManagement.Server.ActionItemSupervisorAssignmentArguments e)
    {
      // Задать тему, исполнителей и срок.
      e.Block.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.ControlWorkFromJob);
      var controller = _obj.Supervisor;
      if (controller != null)
        e.Block.Performers.Add(controller);
      
      var assignmentsDeadLine = 1;
      e.Block.RelativeDeadlineDays = assignmentsDeadLine;
      
      // Задать состояние поручения.
      _obj.ExecutionState = ExecutionState.OnControl;
      
      // Заполнить даты поручения.
      e.Block.ScheduledDate = _obj.Deadline;
      
      // Для подзадач соисполнителям заполнять данными из основной задачи.
      if (_obj.ActionItemType != ActionItemType.Main)
      {
        var mainActionItemExecution = ActionItemExecutionTasks.As(_obj.MainTask);
        if (mainActionItemExecution != null && !(mainActionItemExecution.IsCompoundActionItem ?? false))
        {
          // Задать автора.
          e.Block.AssignedBy = mainActionItemExecution.AssignedBy;
        }
      }
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault());
    }
    
    public virtual void StartAssignment6(Sungero.RecordManagement.IActionItemSupervisorAssignment assignment, Sungero.RecordManagement.Server.ActionItemSupervisorAssignmentArguments e)
    {
      assignment.Author = _obj.Assignee;
      assignment.ActionItem = _obj.ActionItem;
      assignment.Importance = _obj.Importance;
      assignment.NewDeadline = _obj.Deadline;
      assignment.AssignedBy = _obj.AssignedBy;
      
      // Выдать права на изменение для возможности прекращения подзадач.
      Functions.ActionItemExecutionTask.GrantAccessRightToAssignment(assignment, _obj);
    }

    public virtual void CompleteAssignment6(Sungero.RecordManagement.IActionItemSupervisorAssignment assignment, Sungero.RecordManagement.Server.ActionItemSupervisorAssignmentArguments e)
    {
      // Переписка.
      _obj.ReportNote = assignment.ActiveText;
    }
    
    public virtual void EndBlock6(Sungero.RecordManagement.Server.ActionItemSupervisorAssignmentEndBlockEventArguments e)
    {
      var assignment = e.CreatedAssignments.FirstOrDefault();
      if (assignment != null && assignment.Result == Result.ForRework)
      {
        _obj.ExecutionState = ExecutionState.OnRework;
        var newDeadline = ActionItemSupervisorAssignments.As(assignment).NewDeadline;
        _obj.Deadline = newDeadline;
        if (_obj.ActionItemType == ActionItemType.Component && ActionItemExecutionTasks.Is(_obj.ParentTask))
        {
          var rootTask = ActionItemExecutionTasks.As(_obj.ParentTask);
          var actionItem = rootTask.ActionItemParts.Where(i => Equals(i.ActionItemPartExecutionTask, _obj)).FirstOrDefault();
          if (actionItem != null && (actionItem.Deadline != null || rootTask.FinalDeadline != newDeadline))
            actionItem.Deadline = newDeadline;
        }
      }
    }
    
    #endregion
    
    #region - 8 - Уведомления о приемке поручения
    
    public virtual void StartBlock8(Sungero.RecordManagement.Server.ActionItemExecutionNotificationArguments e)
    {
      if (_obj.Supervisor != null)
        e.Block.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.WorkFromActionItemIsAccepted);
      else
      {
        var employee = Company.PublicFunctions.Employee.GetShortName(_obj.Assignee, false);
        var begin = ActionItemExecutionTasks.Resources.WorkFromActionItemIsCompletedFormat(employee);
        e.Block.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, begin);
      }
      
      if (_obj.Supervisor != null && _obj.Assignee != _obj.Supervisor)
        e.Block.Performers.Add(_obj.Assignee);
      
      Sungero.CoreEntities.IUser initiator;
      if (_obj.ActionItemType == ActionItemType.Component && _obj.ParentTask != null)
        initiator = _obj.ParentTask.StartedBy ?? _obj.ParentTask.Author;
      else if (_obj.ActionItemType == ActionItemType.Additional)
        initiator = _obj.Author;
      else
        initiator = _obj.StartedBy ?? _obj.Author;

      if (initiator != _obj.Supervisor && !e.Block.Performers.Contains(initiator))
        e.Block.Performers.Add(initiator);
      
      // Задать состояние поручения.
      _obj.ExecutionState = ExecutionState.Executed;
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault());
    }
    
    public virtual void StartNotice8(Sungero.RecordManagement.IActionItemExecutionNotification notice, Sungero.RecordManagement.Server.ActionItemExecutionNotificationArguments e)
    {
      if (_obj.Supervisor != null)
        notice.Author = _obj.Supervisor;
      else
      {
        notice.Author = _obj.Assignee;
        notice.ThreadSubject = ActionItemExecutionTasks.Resources.ActionItemExecutionNotificationThreadSubject;
      }
      notice.Importance = _obj.Importance;
      
      // Обновить статус исполнения документа - исполнен, статус контроля исполнения - снято с контроля.
      Functions.ActionItemExecutionTask.SetDocumentStates(_obj);
    }
    
    public virtual void EndBlock8(Sungero.RecordManagement.Server.ActionItemExecutionNotificationEndBlockEventArguments e)
    {
      
    }
    
    #endregion

    #region - 9 - Мониторинг создания задания исполнителю

    public virtual bool Monitoring9Result()
    {
      return (_obj.IsCompoundActionItem ?? false) ||
        _obj.CoAssignees.Count == 0 ||
        ActionItemExecutionAssignments.GetAll()
        .Where(j => j.Task == _obj)
        .Where(j => j.Status == Workflow.AssignmentBase.Status.InProcess)
        .Where(j => j.Performer == _obj.Assignee)
        .Where(j => j.TaskStartId == _obj.StartId)
        .Any();
    }

    public virtual void StartBlock9(Sungero.Workflow.Server.Route.MonitoringStartBlockEventArguments e)
    {
      e.Block.Period = TimeSpan.FromSeconds(10);
    }

    #endregion
    
    #region - 10 - Подзадачи на исполнение
    
    public virtual void Script10Execute()
    {
      var subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.TaskSubject);
      var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);
      
      // Задания соисполнителям.
      if (_obj.CoAssignees != null && _obj.CoAssignees.Count > 0)
      {
        var performer = _obj.CoAssignees.FirstOrDefault(ca => ca.AssignmentCreated != true);
        
        var parentAssignment = ActionItemExecutionAssignments.GetAll()
          .Where(j => Equals(j.Task, _obj))
          .Where(j => j.Status == Workflow.AssignmentBase.Status.InProcess)
          .Where(j => Equals(j.Performer, _obj.Assignee))
          .Where(j => Equals(_obj.StartId, j.TaskStartId))
          .FirstOrDefault();
        var actionItemExecution = ActionItemExecutionTasks.CreateAsSubtask(parentAssignment);
        actionItemExecution.Importance = _obj.Importance;
        actionItemExecution.ActionItemType = ActionItemType.Additional;
        
        // Синхронизировать вложения и выдать права.
        if (document != null)
          actionItemExecution.DocumentsGroup.OfficialDocuments.Add(document);
        
        foreach (var addInformation in _obj.OtherGroup.All)
          actionItemExecution.OtherGroup.All.Add(addInformation);
        
        // Задать текст.
        actionItemExecution.Texts.Last().IsAutoGenerated = true;
        
        // Задать поручение.
        actionItemExecution.ActionItem = _obj.ActionItem;
        
        // Задать тему.
        actionItemExecution.Subject = subject;
        
        // Задать исполнителя, ответственного, контролера и инициатора.
        actionItemExecution.Assignee = performer.Assignee;
        actionItemExecution.IsUnderControl = true;
        actionItemExecution.Supervisor = _obj.Assignee;
        actionItemExecution.AssignedBy = _obj.Assignee;
        
        // Задать срок.
        actionItemExecution.Deadline = _obj.Deadline;
        actionItemExecution.MaxDeadline = _obj.Deadline;
        
        actionItemExecution.Start();
        
        performer.AssignmentCreated = true;
      }
      
      // Составное поручение.
      if (_obj.ActionItemParts != null && _obj.ActionItemParts.Count > 0)
      {
        var job = _obj.ActionItemParts.FirstOrDefault(aip => aip.AssignmentCreated != true);
        
        var actionItemExecution = ActionItemExecutionTasks.CreateAsSubtask(_obj);
        actionItemExecution.Importance = _obj.Importance;
        actionItemExecution.ActionItemType = ActionItemType.Component;
        
        // Синхронизировать вложения и выдать права.
        if (document != null)
          actionItemExecution.DocumentsGroup.OfficialDocuments.Add(document);
        
        foreach (var addInformation in _obj.OtherGroup.All)
          actionItemExecution.OtherGroup.All.Add(addInformation);
        
        // Задать поручение и текст.
        actionItemExecution.ActionItem = string.IsNullOrWhiteSpace(job.ActionItemPart) ? _obj.ActionItem : job.ActionItemPart;
        
        // Задать тему.
        actionItemExecution.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(actionItemExecution, ActionItemExecutionTasks.Resources.TaskSubject);
        actionItemExecution.ThreadSubject = Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ActionItemWithNumberThreadSubject;
        
        // Задать исполнителя, ответственного, контролера и инициатора.
        actionItemExecution.Assignee = job.Assignee;
        actionItemExecution.IsUnderControl = _obj.IsUnderControl;
        actionItemExecution.Supervisor = _obj.Supervisor;
        actionItemExecution.Author = _obj.Author;
        actionItemExecution.AssignedBy = _obj.AssignedBy;
        
        // Задать срок.
        var actionItemDeadline = job.Deadline.HasValue ? job.Deadline : _obj.FinalDeadline;
        actionItemExecution.Deadline = actionItemDeadline;
        actionItemExecution.MaxDeadline = actionItemDeadline;
        
        actionItemExecution.Start();
        
        // Добавить составные подзадачи в исходящее.
        if (actionItemExecution.Status == Sungero.Workflow.Task.Status.InProcess)
          Sungero.Workflow.SpecialFolders.GetOutbox(_obj.StartedBy).Items.Add(actionItemExecution);
        
        // Записать ссылку на поручение в составное поручение.
        job.ActionItemPartExecutionTask = actionItemExecution;
        
        job.AssignmentCreated = true;
      }
    }

    #endregion
    
    #region - 11 - Мониторинг завершения поручений
    
    public virtual bool Monitoring11Result()
    {
      if (_obj.IsCompoundActionItem != true)
        return true;
      
      return !ActionItemExecutionTasks.GetAll(j => Equals(j.ParentTask, _obj) &&
                                              j.ActionItemType != ActionItemType.Main &&
                                              j.Status.Value == Workflow.Task.Status.InProcess &&
                                              j.ParentStartId == _obj.StartId).Any();
    }

    public virtual void StartBlock11(Sungero.Workflow.Server.Route.MonitoringStartBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region - 12 - Уведомление о старте поручения
    
    public virtual void StartBlock12(Sungero.RecordManagement.Server.ActionItemObserversNotificationArguments e)
    {
      if (_obj.IsDraftResolution == true)
      {
        // Скопировать собственные права поручения в MainTask, чтобы корректно определялись права на задания.
        foreach (var accessRight in _obj.AccessRights.Current)
          if (!_obj.MainTask.AccessRights.IsGranted(accessRight.AccessRightsType, accessRight.Recipient))
            _obj.MainTask.AccessRights.Grant(accessRight.Recipient, accessRight.AccessRightsType);
        
        _obj.IsDraftResolution = null;
      }
      
      if (_obj.IsCompoundActionItem == true)
        e.Block.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.WorkFromActionItemIsCreatedCompound);
      else
        e.Block.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.WorkFromActionItemIsCreated);
      
      foreach (var observer in _obj.ActionItemObservers)
        e.Block.Performers.Add(observer.Observer);
      
      var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
      {
        var assignees = _obj.ActionItemParts.Select(x => x.Assignee).ToList();
        assignees.AddRange(_obj.CoAssignees.Select(x => x.Assignee).ToList());
        foreach (var assignee in assignees)
          if (!document.AccessRights.CanRead(assignee))
            document.AccessRights.Grant(assignee, DefaultAccessRightsTypes.Read);
      }
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);
    }

    public virtual void StartNotice12(Sungero.RecordManagement.IActionItemObserversNotification notice, Sungero.RecordManagement.Server.ActionItemObserversNotificationArguments e)
    {
      notice.Importance = _obj.Importance;
    }

    public virtual void EndBlock12(Sungero.RecordManagement.Server.ActionItemObserversNotificationEndBlockEventArguments e)
    {
      
    }

    #endregion
    
    #region - 13 - Задания соисполнителям созданы?
    
    public virtual bool Decision13Result()
    {
      // Если есть хотя бы один соисполнитель, по которому не создано задание (AssignmentCreated == false или == null),
      // то возвращаем False.
      // Если задания созданы по всем соисполнителям,
      // то возвращаем True.
      if (_obj.CoAssignees == null && _obj.ActionItemParts == null)
        return true;
      
      return !_obj.CoAssignees.Any(ca => ca.AssignmentCreated != true) &&
        !_obj.ActionItemParts.Any(aip => aip.AssignmentCreated != true);
    }
    
    #endregion
    
    #region - 99 - Finish
    
    public virtual void StartReviewAssignment99(Sungero.Workflow.IReviewAssignment reviewAssignment)
    {
      
    }
    
    #endregion
  }
}