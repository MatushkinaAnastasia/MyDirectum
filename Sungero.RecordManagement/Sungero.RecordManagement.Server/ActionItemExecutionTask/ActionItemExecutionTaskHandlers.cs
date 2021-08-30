using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.Server;
using Sungero.RecordManagement.ActionItemExecutionTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement
{

  partial class ActionItemExecutionTaskActionItemObserversObserverPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ActionItemObserversObserverFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return (IQueryable<T>)PublicFunctions.Module.ObserversFiltering(query);
    }
  }

  partial class ActionItemExecutionTaskCreatingFromServerHandler
  {
    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.ExecutionState);
      e.Without(_info.Properties.Report);
      e.Without(_info.Properties.ActualDate);
      e.Without(_info.Properties.ReportNote);
      e.Without(_info.Properties.AbortingReason);
      e.Without(_info.Properties.ActionItemType);
    }
  }

  partial class ActionItemExecutionTaskAssignedByPropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> AssignedByFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      e.DisableUiFiltering = true;
      var resolutionAuthorsIds = Docflow.PublicFunctions.Module.Remote.UsersCanBeResolutionAuthor(_obj.DocumentsGroup.OfficialDocuments.SingleOrDefault()).Select(x => x.Id).ToList();
      return query.Where(x => resolutionAuthorsIds.Contains(x.Id));
    }
  }

  partial class ActionItemExecutionTaskFilteringServerHandler<T>
  {
    
    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      // Вернуть нефильтрованный список, если нет фильтра. Он будет использоваться во всех Get() и GetAll().
      var filter = _filter;
      if (filter == null)
        return query;
      
      e.DisableCheckRights = true;
      
      // Не показывать не стартованные поручения.
      query = query.Where(l => l.Status != Sungero.Workflow.Task.Status.Draft);
      
      // Не показывать составные поручения (только подзадачи).
      query = query.Where(j => j.IsCompoundActionItem == false);
      
      // Фильтр по статусу.
      var statuses = new List<Enumeration>();
      if (filter.OnExecution)
      {
        statuses.Add(ExecutionState.OnExecution);
        statuses.Add(ExecutionState.OnControl);
        statuses.Add(ExecutionState.OnRework);
      }
      
      if (filter.Executed)
      {
        statuses.Add(ExecutionState.Executed);
        statuses.Add(ExecutionState.Aborted);
      }
      
      if (statuses.Any())
        query = query.Where(q => q.ExecutionState != null && statuses.Contains(q.ExecutionState.Value));
      
      // Фильтры "Поручения где я", "По сотруднику".
      var currentUser = Users.Current;
      
      // Сформировать списки пользователей для фильтрации.
      var authors = new List<IUser>();
      var assignees = new List<IUser>();
      var supervisors = new List<IUser>();
      
      if (filter.Author != null)
        authors.Add(filter.Author);
      if (filter.Assignee != null)
        assignees.Add(filter.Assignee);
      if (filter.Supervisor != null)
        supervisors.Add(filter.Supervisor);
      
      // Наложить фильтр по всем замещениям, если не указаны фильтры по текущему или выбранному сотруднику.
      if (!Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor())
      {
        var allSubstitutes = Substitutions.ActiveSubstitutedUsers.ToList();
        allSubstitutes.Add(Users.Current);
        query = query.Where(j => allSubstitutes.Contains(j.AssignedBy) || allSubstitutes.Contains(j.Assignee) ||
                            j.CoAssignees.Any(p => allSubstitutes.Contains(p.Assignee)) ||
                            allSubstitutes.Contains(j.Supervisor) || allSubstitutes.Contains(j.StartedBy) ||
                            j.ActionItemObservers.Any(o => Recipients.AllRecipientIds.Contains(o.Observer.Id)));
      }
      
      query = query.Where(j => (!authors.Any() || authors.Contains(j.AssignedBy)) &&
                          (!assignees.Any() || assignees.Contains(j.Assignee) || j.CoAssignees.Any(p => assignees.Contains(p.Assignee))) &&
                          (!supervisors.Any() || supervisors.Contains(j.Supervisor)));
      
      // Фильтр по соблюдению сроков.
      var now = Calendar.Now;
      var today = Calendar.UserToday;
      var tomorrow = today.AddDays(1);
      if (filter.Overdue)
        query = query.Where(j => j.Status != Workflow.Task.Status.Aborted &&
                            ((j.ActualDate == null && j.Deadline < now && j.Deadline != today && j.Deadline != tomorrow) ||
                             (j.ActualDate != null && j.ActualDate > j.Deadline)));
      // Фильтр по плановому сроку.
      if (filter.LastMonth)
      {
        var lastMonthBeginDate = today.AddDays(-30);
        var lastMonthBeginDateNextDay = lastMonthBeginDate.AddDays(1);
        var lastMonthBeginDateWithTime = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(lastMonthBeginDate);

        query = query.Where(j => ((j.Deadline >= lastMonthBeginDateWithTime && j.Deadline < now) ||
                                  j.Deadline == lastMonthBeginDate || j.Deadline == lastMonthBeginDateNextDay || j.Deadline == today) &&
                            j.Deadline != tomorrow);
      }

      if (filter.ManualPeriod)
      {
        if (filter.DateRangeFrom != null)
        {
          var dateRangeFromNextDay = filter.DateRangeFrom.Value.AddDays(1);
          var dateFromWithTime = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(filter.DateRangeFrom.Value);
          query = query.Where(j => j.Deadline >= dateFromWithTime ||
                              j.Deadline == filter.DateRangeFrom.Value || j.Deadline == dateRangeFromNextDay);
        }
        if (filter.DateRangeTo != null)
        {
          var dateRangeNextDay = filter.DateRangeTo.Value.AddDays(1);
          var dateTo = filter.DateRangeTo.Value.EndOfDay().FromUserTime();
          query = query.Where(j => (j.Deadline < dateTo || j.Deadline == filter.DateRangeTo.Value) &&
                              j.Deadline != dateRangeNextDay);
        }
      }
      
      return query;
    }
  }

  partial class ActionItemExecutionTaskServerHandlers
  {
    public override void BeforeAbort(Sungero.Workflow.Server.BeforeAbortEventArgs e)
    {
      _obj.ExecutionState = ExecutionState.Aborted;
      
      // Если прекращён черновик, прикладную логику по прекращению выполнять не надо.
      if (_obj.State.Properties.Status.OriginalValue == Workflow.Task.Status.Draft)
        return;
      
      // Обновить статус исполнения документа - исполнен, статус контроля исполнения - снято с контроля.
      if (_obj.DocumentsGroup.OfficialDocuments.Any())
        Functions.ActionItemExecutionTask.SetDocumentStates(_obj);
      
      // При программном вызове не выполнять рекурсивную остановку подзадач.
      if (!e.Params.Contains(RecordManagement.Constants.ActionItemExecutionTask.WorkingWithGUI))
        return;
      
      // Рекурсивно прекратить подзадачи.
      Functions.Module.AbortSubtasksAndSendNotices(_obj);
      
      // Прекратить задачу на запрос отчёта, если она есть.
      var reportRequestTasks = StatusReportRequestTasks.GetAll(r => Equals(r.ParentTask, _obj) &&
                                                               r.Status == Workflow.Task.Status.InProcess);
      foreach (var reportRequestTask in reportRequestTasks)
        reportRequestTask.Abort();
    }

    public override void BeforeRestart(Sungero.Workflow.Server.BeforeRestartEventArgs e)
    {
      // Очистить причину прекращения и статус.
      _obj.AbortingReason = string.Empty;
      _obj.ExecutionState = null;
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault());
      
      // Очистить свойство созданных заданий у свойств-коллекций.
      if (_obj.CoAssignees != null && _obj.CoAssignees.Count > 0)
      {
        foreach (var assignee in _obj.CoAssignees)
        {
          assignee.AssignmentCreated = false;
        }
      }
      if (_obj.ActionItemParts != null && _obj.ActionItemParts.Count > 0)
      {
        foreach (var part in _obj.ActionItemParts)
        {
          part.AssignmentCreated = false;
        }
      }
    }

    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      // Если задача была стартована через UI, то проверяем корректность срока.
      bool startedFromUI;
      if (e.Params.TryGetValue(PublicConstants.ActionItemExecutionTask.CheckDeadline, out startedFromUI) && startedFromUI)
        e.Params.Remove(PublicConstants.ActionItemExecutionTask.CheckDeadline);
      
      if (!Functions.ActionItemExecutionTask.ValidateActionItemExecutionTaskStart(_obj, e, startedFromUI))
        return;

      // Задать текст в переписке.
      if (_obj.IsCompoundActionItem == true)
      {
        _obj.ActiveText = string.IsNullOrEmpty(_obj.ActionItem) ? Sungero.RecordManagement.ActionItemExecutionTasks.Resources.DefaultActionItem : _obj.ActionItem;
        _obj.ThreadSubject = Sungero.RecordManagement.ActionItemExecutionTasks.Resources.CompoundActionItemThreadSubject;
      }
      else if (_obj.ActionItemType != ActionItemType.Component)
        _obj.ThreadSubject = Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ActionItemThreadSubject;

      if (_obj.ActionItemType == ActionItemType.Component)
      {
        _obj.ActiveText = _obj.ActionItem;
        // При рестарте поручения обновляется текст, срок и исполнитель в табличной части составного поручения.
        var actionItem = ActionItemExecutionTasks.As(_obj.ParentTask).ActionItemParts.FirstOrDefault(s => Equals(s.ActionItemPartExecutionTask, _obj));
        // Обновить текст поручения, если изменен индивидуальный текст или указан общий текст вместо индивидуального.
        if (actionItem.ActionItemExecutionTask.ActionItem != _obj.ActionItem && actionItem.ActionItemPart != _obj.ActionItem ||
            actionItem.ActionItemExecutionTask.ActionItem == _obj.ActionItem && !string.IsNullOrEmpty(actionItem.ActionItemPart))
          actionItem.ActionItemPart = _obj.ActionItem;
        // Обновить срок поручения, если изменен индивидуальный срок или указан общий срок вместо индивидуального.
        if (actionItem.ActionItemExecutionTask.FinalDeadline != _obj.Deadline && actionItem.Deadline != _obj.Deadline ||
            actionItem.ActionItemExecutionTask.FinalDeadline == _obj.Deadline && actionItem.Deadline.HasValue)
          actionItem.Deadline = _obj.Deadline;
        // Обновить исполнителя, если он изменен при рестарте.
        if (actionItem.ActionItemExecutionTask.Assignee != _obj.Assignee && actionItem.Assignee != _obj.Assignee)
          actionItem.Assignee = _obj.Assignee;
      }
      
      if (_obj.ActionItemType == ActionItemType.Additional)
        _obj.ActiveText = ActionItemExecutionTasks.Resources.SentToCoAssignee;
      
      // Выдать права на изменение для возможности прекращения задачи.
      Functions.ActionItemExecutionTask.GrantAccessRightToTask(_obj, _obj);
      
      if (_obj.IsDraftResolution == true && !_obj.DocumentsGroup.OfficialDocuments.Any())
        if (ReviewDraftResolutionAssignments.Is(_obj.ParentAssignment))
          _obj.DocumentsGroup.OfficialDocuments.Add(ReviewDraftResolutionAssignments.As(_obj.ParentAssignment).DocumentForReviewGroup.OfficialDocuments.FirstOrDefault());
        else
          _obj.DocumentsGroup.OfficialDocuments.Add(PreparingDraftResolutionAssignments.As(_obj.ParentAssignment).DocumentForReviewGroup.OfficialDocuments.FirstOrDefault());
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.ActionItemType = ActionItemType.Main;
      
      if (!_obj.State.IsCopied)
      {
        _obj.NeedsReview = false;
        _obj.IsUnderControl = false;
        _obj.IsCompoundActionItem = false;
        _obj.Subject = Docflow.Resources.AutoformatTaskSubject;
        
        var employee = Employees.As(_obj.Author);
        if (employee != null)
        {
          var settings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(employee);
          if (settings != null)
          {
            _obj.IsUnderControl = settings.FollowUpActionItem;
            var resolutionAuthor = Docflow.PublicFunctions.PersonalSetting.GetResolutionAuthor(settings);
            _obj.AssignedBy = Docflow.PublicFunctions.Module.Remote.IsUsersCanBeResolutionAuthor(_obj.DocumentsGroup.OfficialDocuments.SingleOrDefault(), resolutionAuthor) ?
              resolutionAuthor :
              null;
          }
        }
      }
      else
      {
        if (_obj.Author != null && _obj.AssignedBy != null && !_obj.Author.Equals(_obj.AssignedBy))
          _obj.Author = Users.As(_obj.AssignedBy);
        
        // Сброс отметок о создании заданий соисполнителям.
        if (_obj.CoAssignees.Count > 0)
          foreach (var assignee in _obj.CoAssignees)
            assignee.AssignmentCreated = false;
        
        // Сброс отметок о создании заданий по частям составного поручения.
        if (_obj.IsCompoundActionItem == true)
          foreach (var part in _obj.ActionItemParts)
            part.AssignmentCreated = false;
      }
      
      var subjectTemplate = _obj.IsCompoundActionItem == true ?
        ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
        ActionItemExecutionTasks.Resources.TaskSubject;
      _obj.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, subjectTemplate);
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (!Functions.ActionItemExecutionTask.ValidateActionItemExecutionTaskSave(_obj, e))
        return;

      var isCompoundActionItem = _obj.IsCompoundActionItem ?? false;
      if (isCompoundActionItem)
      {
        if (string.IsNullOrEmpty(_obj.ActionItem) && !_obj.ActionItemParts.Any(i => string.IsNullOrEmpty(i.ActionItemPart)))
          _obj.ActionItem = ActionItemExecutionTasks.Resources.DefaultActionItem;
      }
      
      // Выдать права на документы для всех, кому выданы права на задачу.
      if (_obj.State.IsChanged)
        Docflow.PublicFunctions.Module.GrantManualReadRightForAttachments(_obj, _obj.AllAttachments.ToList());
      
      if (_obj.State.Properties.IsCompoundActionItem.IsChanged)
      {
        if (_obj.IsCompoundActionItem ?? false)
        {
          // Очистить ненужные свойства в составном поручение.
          _obj.Assignee = null;
          _obj.CoAssignees.Clear();
          _obj.Deadline = null;
          
          // Заменить первый символ на прописной.
          foreach (var job in _obj.ActionItemParts)
            job.ActionItemPart = Docflow.PublicFunctions.Module.ReplaceFirstSymbolToUpperCase(job.ActionItemPart);
        }
        else
        {
          // Очистить ненужные свойства в несоставном поручении.
          _obj.ActionItemParts.Clear();
        }
      }
      
      // Заполнить тему.
      var defaultSubject = _obj.IsCompoundActionItem == true ?
        ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
        ActionItemExecutionTasks.Resources.TaskSubject;
      var subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, defaultSubject);
      if (subject == Docflow.Resources.AutoformatTaskSubject)
        subject = defaultSubject;
      
      // Не перезаписывать тему, если не изменилась.
      if (subject != _obj.Subject)
        _obj.Subject = subject;
      
      // Задать текст в переписке.
      var threadSubject = string.Empty;
      if (isCompoundActionItem)
        threadSubject = Sungero.RecordManagement.ActionItemExecutionTasks.Resources.CompoundActionItemThreadSubject;
      else if (_obj.ActionItemType != ActionItemType.Component)
        threadSubject = Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ActionItemThreadSubject;
      
      // Не перезаписывать текст в переписке без необходимости, чтобы избежать блокировок.
      if (!string.IsNullOrEmpty(threadSubject) && threadSubject != _obj.ThreadSubject)
        _obj.ThreadSubject = threadSubject;
      
      // Текст задачи мог обновиться по действию "Изменить текст".
      if (_obj.ActionItemType == ActionItemType.Main &&
          _obj.ActionItem != _obj.ActiveText &&
          !string.IsNullOrEmpty(_obj.ActiveText))
      {
        _obj.ActionItem = _obj.ActiveText;
        _obj.State.Controls.Control.Refresh();
      }
    }
  }
}