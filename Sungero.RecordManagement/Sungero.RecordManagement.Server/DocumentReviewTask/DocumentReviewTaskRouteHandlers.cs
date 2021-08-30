using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OfficialDocument;
using Sungero.RecordManagement.DocumentReviewTask;
using Sungero.RecordManagement.ReviewManagerAssignment;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server
{
  partial class DocumentReviewTaskRouteHandlers
  {
    public virtual bool Decision17Result()
    {
      var addressee = Employees.As(_obj.Addressee);
      var lastAssignment = Assignments.GetAll(x => Equals(x.MainTask, _obj)).ToList().LastOrDefault();
      var isForwarded = lastAssignment != null && lastAssignment.Result == Sungero.RecordManagement.ReviewManagerAssignment.Result.Forward;
      return Docflow.PublicFunctions.Module.GetSecretaries(addressee).Any(x => x.PreparesResolution == true && Equals(x.Assistant, _obj.StartedBy)) && !isForwarded;
    }

    public virtual bool Decision15Result()
    {
      return _obj.ResolutionGroup.ActionItemExecutionTasks.Any();
    }
    
    public virtual bool Decision10Result()
    {
      var addressee = Employees.As(_obj.Addressee);
      return Docflow.PublicFunctions.Module.GetSecretaries(addressee).Any(x => x.PreparesResolution == true);
    }

    #region 9. Уведомление наблюдателям

    public virtual void StartBlock9(Sungero.RecordManagement.Server.ReviewObserversNotificationArguments e)
    {
      var canPrepareResolution = Docflow.PublicFunctions.Module.GetSecretaries(_obj.Addressee).Any(x => Equals(x.Assistant, _obj.StartedBy) && x.PreparesResolution == true);
      _obj.NeedDeleteActionItems = !canPrepareResolution && _obj.ResolutionGroup.ActionItemExecutionTasks.Any();
      _obj.Save();
      
      if (_obj.NeedDeleteActionItems == true)
        Functions.Module.DeleteActionItemExecutionTasks(_obj.ResolutionGroup.ActionItemExecutionTasks.ToList());
      
      // Добавить наблюдателей задачи в качестве исполнителей уведомления.
      foreach (var observer in _obj.ResolutionObservers)
        e.Block.Performers.Add(observer.Observer);
      
      // Получить вложенный для рассмотрения документ.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      
      // Задать тему.
      var subject = DocumentReviewTasks.Resources.DocumentConsiderationStartedFormat(document.Name);
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);

      // Выдать наблюдателям права на вложения.
      Docflow.PublicFunctions.Module.GrantReadRightsForAttachments(_obj.AddendaGroup.All.ToList(), e.Block.Performers);
    }

    public virtual void StartNotice9(Sungero.RecordManagement.IReviewObserversNotification notice, Sungero.RecordManagement.Server.ReviewObserversNotificationArguments e)
    {
      notice.ThreadSubject = DocumentReviewTasks.Resources.ReviewBeginingNoticeThreadSubject;
    }

    public virtual void EndBlock9(Sungero.RecordManagement.Server.ReviewObserversNotificationEndBlockEventArguments e)
    {
      
    }
    
    #endregion

    #region 2. Рассмотрение руководителем
    
    public virtual void StartBlock2(Sungero.RecordManagement.Server.ReviewManagerAssignmentArguments e)
    {
      // Добавить адресата в качестве исполнителя.
      e.Block.Performers.Add(_obj.Addressee);
      
      // Установить срок и тему.
      if (_obj.Deadline.HasValue && _obj.Started.HasValue)
      {
        var deadline = Docflow.PublicFunctions.Module.GetDateWithTime(_obj.Deadline.Value, _obj.Addressee);
        var deadlineInHour = WorkingTime.GetDurationInWorkingHours(_obj.Started.Value, deadline, _obj.Addressee);
        e.Block.RelativeDeadlineHours = deadlineInHour > 0 ? deadlineInHour : 1;
      }
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.ReviewDocument, document.Name);
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);

      // Выдать исполнителю права на вложения.
      Functions.DocumentReviewTask.GrantRightForAttachmentsToAssignees(_obj, e.Block.Performers.ToList());
    }

    public virtual void StartAssignment2(Sungero.RecordManagement.IReviewManagerAssignment assignment, Sungero.RecordManagement.Server.ReviewManagerAssignmentArguments e)
    {
      // Обновить статус исполнения - на рассмотрении.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      document.ExecutionState = ExecutionState.OnReview;
    }

    public virtual void CompleteAssignment2(Sungero.RecordManagement.IReviewManagerAssignment assignment, Sungero.RecordManagement.Server.ReviewManagerAssignmentArguments e)
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      
      // Заполнить текст резолюции из задания руководителя в задачу.
      if (assignment.Result == Result.AddResolution)
        _obj.ResolutionText = assignment.ActiveText;
      
      // Обновить статус исполнения - не требует исполнения.
      if (assignment.Result == Result.Explored)
        document.ExecutionState = ExecutionState.WithoutExecut;
      
      // Обновить статус исполнения - на исполнении.
      if (assignment.Result == Result.AddAssignment)
        document.ExecutionState = ExecutionState.OnExecution;
      
      // Заполнить нового адресата в задаче.
      if (assignment.Result == Result.Forward)
        Functions.DocumentReviewTask.UpdateReviewTaskAfterForward(_obj, assignment.Addressee);
    }

    public virtual void EndBlock2(Sungero.RecordManagement.Server.ReviewManagerAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region 3. Уведомление наблюдателям

    public virtual void StartBlock3(Sungero.RecordManagement.Server.ReviewObserverNotificationArguments e)
    {
      // Добавить наблюдателей задачи в качестве исполнителей уведомления.
      foreach (var observer in _obj.ResolutionObservers)
        e.Block.Performers.Add(observer.Observer);
      
      // Получить вложенный для рассмотрения документ.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      
      // Задать тему.
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.AcquaintanceWithDocumentComplete, document.Name);
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);

      // Выдать наблюдателям права на вложения.
      Docflow.PublicFunctions.Module.GrantReadRightsForAttachments(_obj.AddendaGroup.All.ToList(), e.Block.Performers);
    }

    public virtual void StartNotice3(Sungero.RecordManagement.IReviewObserverNotification notice, Sungero.RecordManagement.Server.ReviewObserverNotificationArguments e)
    {
      // Установить "От" как исполнителя рассмотрения.
      notice.Author = _obj.Addressee;
      
      notice.ThreadSubject = DocumentReviewTasks.Resources.ReviewCompletionNoticeThreadSubject;
    }

    public virtual void EndBlock3(Sungero.RecordManagement.Server.ReviewObserverNotificationEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region 4. Уведомление делопроизводителю

    public virtual void StartBlock4(Sungero.RecordManagement.Server.ReviewClerkNotificationArguments e)
    {
      // Отправляется только в случае, если руководитель выполнил задание с результатом "Ознакомлен", "Отправлено на исполнение".
      var result = Functions.DocumentReviewTask.GetLastAssignmentResult(_obj);
      if (result != RecordManagement.ReviewManagerAssignment.Result.AddResolution && result != RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution)
      {
        e.Block.Performers.Add(_obj.Author);
        
        var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
        e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.AcquaintanceWithDocumentComplete, document.Name);
        
        Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);

        // Выдать наблюдателям права на вложения.
        Docflow.PublicFunctions.Module.GrantReadRightsForAttachments(_obj.AddendaGroup.All.ToList(), e.Block.Performers);
      }
    }

    public virtual void StartNotice4(Sungero.RecordManagement.IReviewClerkNotification notice, Sungero.RecordManagement.Server.ReviewClerkNotificationArguments e)
    {
      // Установить "От" как исполнителя рассмотрения.
      notice.Author = _obj.Addressee;
      
      notice.ThreadSubject = DocumentReviewTasks.Resources.ReviewCompletionNoticeThreadSubject;
    }

    public virtual void EndBlock4(Sungero.RecordManagement.Server.ReviewClerkNotificationEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region 4. Уведомление инициатору
    
    public virtual void StartBlock19(Sungero.Workflow.Server.NoticeArguments e)
    {
      // Отправляется только в случае, если руководитель выполнил задание с результатом "Вынесена резолюция".
      // И поручение создает не инициатор.
      var result = Functions.DocumentReviewTask.GetLastAssignmentResult(_obj);
      
      if ((result == RecordManagement.ReviewManagerAssignment.Result.AddResolution || result == RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution) &&
          Functions.DocumentReviewTask.GetClerkToSendActionItem(_obj) != _obj.Author)
      {
        e.Block.Performers.Add(_obj.Author);
        
        var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
        e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.AcquaintanceWithDocumentComplete, document.Name);
        
        Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);

        // Выдать наблюдателям права на вложения.
        Docflow.PublicFunctions.Module.GrantReadRightsForAttachments(_obj.AddendaGroup.All.ToList(), e.Block.Performers);
      }
      
    }
    
    public virtual void StartNotice19(Sungero.Workflow.INotice notice, Sungero.Workflow.Server.NoticeArguments e)
    {
      // Установить "От" как исполнителя рассмотрения.
      notice.Author = _obj.Addressee;
      
      notice.ThreadSubject = DocumentReviewTasks.Resources.ReviewCompletionNoticeThreadSubject;
    }
    
    public virtual void EndBlock19(Sungero.Workflow.Server.NoticeEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region 5. Делопроизводителю требуется создать поручения?

    public virtual bool Decision5Result()
    {
      var result = Functions.DocumentReviewTask.GetLastAssignmentResult(_obj);
      return result == RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution || result == RecordManagement.ReviewManagerAssignment.Result.AddResolution;
    }
    
    #endregion

    #region 6. Создание поручения делопроизводителем
    
    public virtual void StartBlock6(Sungero.RecordManagement.Server.ReviewResolutionAssignmentArguments e)
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      e.Block.Performers.Add(Functions.DocumentReviewTask.GetClerkToSendActionItem(_obj));
      
      // Тема.
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.CreateAssignment, document.Name);
      
      // Установить срок на оформление поручений 4 часа.
      e.Block.RelativeDeadlineHours = 4;
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);

      // Выдать исполнителю права на вложения.
      Functions.DocumentReviewTask.GrantRightForAttachmentsToAssignees(_obj, e.Block.Performers.ToList());
    }

    public virtual void StartAssignment6(Sungero.RecordManagement.IReviewResolutionAssignment assignment, Sungero.RecordManagement.Server.ReviewResolutionAssignmentArguments e)
    {
      assignment.ResolutionText = _obj.ResolutionText;
      
      // Установить "От" как исполнителя рассмотрения.
      assignment.Author = _obj.Addressee;
      
      // Обновить статус исполнения - отправка на исполнение.
      _obj.DocumentForReviewGroup.OfficialDocuments.First().ExecutionState = ExecutionState.Sending;
    }

    public virtual void CompleteAssignment6(Sungero.RecordManagement.IReviewResolutionAssignment assignment, Sungero.RecordManagement.Server.ReviewResolutionAssignmentArguments e)
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      
      // Если поручения не созданы, то изменить статус исполнения - не требует исполнения.
      if (!ActionItemExecutionTasks.GetAll(t => t.Status == Workflow.Task.Status.InProcess && Equals(t.ParentAssignment, assignment)).Any())
        document.ExecutionState = ExecutionState.WithoutExecut;
      else
        document.ExecutionState = ExecutionState.OnExecution;
    }

    public virtual void EndBlock6(Sungero.RecordManagement.Server.ReviewResolutionAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region 11. Создание и доработка проектов резолюций
    
    public virtual void StartBlock11(Sungero.RecordManagement.Server.PreparingDraftResolutionAssignmentArguments e)
    {
      var addressee = Employees.As(_obj.Addressee);
      var assistant = Docflow.PublicFunctions.Module.GetSecretary(addressee);
      // Добавить адресата в качестве исполнителя.
      e.Block.Performers.Add(assistant);
      
      // Вычислить дедлайн задания.
      // На подготовку проекта резолюции 4 часа.
      e.Block.RelativeDeadlineHours = 4;
      
      // Проставляем признак того, что задание для доработки.
      var lastReview = Assignments
        .GetAll(a => Equals(a.Task, _obj) && Equals(a.TaskStartId, _obj.StartId))
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
      if (lastReview != null && ReviewDraftResolutionAssignments.Is(lastReview) &&
          lastReview.Result == RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution)
        e.Block.IsRework = true;
      
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      
      var result = Functions.DocumentReviewTask.GetLastAssignmentResult(_obj);
      if (result != RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution)
        e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.PrepareDraftResolution, document.Name);
      else
        e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.ReworkPrepareDraftResolution, document.Name);
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);

      // Выдать исполнителю права на вложения.
      Functions.DocumentReviewTask.GrantRightForAttachmentsToAssignees(_obj, e.Block.Performers.ToList());
    }

    public virtual void StartAssignment11(Sungero.RecordManagement.IPreparingDraftResolutionAssignment assignment, Sungero.RecordManagement.Server.PreparingDraftResolutionAssignmentArguments e)
    {
      // Обновить статус исполнения - на рассмотрении.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      document.ExecutionState = ExecutionState.OnReview;
      var result = Functions.DocumentReviewTask.GetLastAssignmentResult(_obj);
      if (result == RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution)
        assignment.ThreadSubject = Sungero.RecordManagement.DocumentReviewTasks.Resources.ReworkDraftResolutionThreadSubject;
    }

    public virtual void CompleteAssignment11(Sungero.RecordManagement.IPreparingDraftResolutionAssignment assignment, Sungero.RecordManagement.Server.PreparingDraftResolutionAssignmentArguments e)
    {
      // Заполнить нового адресата в задаче.
      if (assignment.Result == Sungero.RecordManagement.PreparingDraftResolutionAssignment.Result.Forward)
        Functions.DocumentReviewTask.UpdateReviewTaskAfterForward(_obj, assignment.Addressee);
      if (assignment.NeedDeleteActionItems == true)
        Functions.Module.DeleteActionItemExecutionTasks(_obj.ResolutionGroup.ActionItemExecutionTasks.ToList());
      
      // Обновить статус исполнения - не требует исполнения.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      if (assignment.Result == Sungero.RecordManagement.PreparingDraftResolutionAssignment.Result.Explored)
        document.ExecutionState = ExecutionState.WithoutExecut;
    }

    public virtual void EndBlock11(Sungero.RecordManagement.Server.PreparingDraftResolutionAssignmentEndBlockEventArguments e)
    {

    }
    
    #endregion
    
    #region 12. Рассмотрение руководителем проектов резолюций
    
    public virtual void StartBlock12(Sungero.RecordManagement.Server.ReviewDraftResolutionAssignmentArguments e)
    {
      // Добавить адресата в качестве исполнителя.
      e.Block.Performers.Add(_obj.Addressee);
      
      // Установить срок и тему.
      if (_obj.Deadline.HasValue && _obj.Started.HasValue)
      {
        var deadline = Sungero.Docflow.PublicFunctions.Module.GetDateWithTime(_obj.Deadline.Value, _obj.Addressee);
        var deadlineInHour = WorkingTime.GetDurationInWorkingHours(_obj.Started.Value, deadline, _obj.Addressee);
        e.Block.RelativeDeadlineHours = deadlineInHour > 0 ? deadlineInHour : 1;
      }
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.ReviewDocument, document.Name);
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);

      // Выдать исполнителю права на вложения.
      Functions.DocumentReviewTask.GrantRightForAttachmentsToAssignees(_obj, e.Block.Performers.ToList());
    }
    
    public virtual void StartAssignment12(Sungero.RecordManagement.IReviewDraftResolutionAssignment assignment, Sungero.RecordManagement.Server.ReviewDraftResolutionAssignmentArguments e)
    {
      // Обновить статус исполнения - на рассмотрении.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      document.ExecutionState = ExecutionState.OnReview;
    }
    
    public virtual void CompleteAssignment12(Sungero.RecordManagement.IReviewDraftResolutionAssignment assignment, Sungero.RecordManagement.Server.ReviewDraftResolutionAssignmentArguments e)
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      
      // Заполнить текст резолюции из задания руководителя в задачу.
      if (assignment.Result == Sungero.RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution)
        _obj.ResolutionText = assignment.ActiveText;
      
      // Обновить статус исполнения - на исполнении.
      if (assignment.Result == Sungero.RecordManagement.ReviewDraftResolutionAssignment.Result.ForExecution)
        document.ExecutionState = ExecutionState.OnExecution;
      
      // Обновить статус исполнения - не требует исполнения.
      if (assignment.Result == Sungero.RecordManagement.ReviewDraftResolutionAssignment.Result.Informed)
        document.ExecutionState = ExecutionState.WithoutExecut;
      
      // Заполнить нового адресата в задаче.
      if (assignment.Result == Sungero.RecordManagement.ReviewDraftResolutionAssignment.Result.Forward)
        Functions.DocumentReviewTask.UpdateReviewTaskAfterForward(_obj, assignment.Addressee);
      if (assignment.NeedDeleteActionItems == true)
      {
        var actionItems = _obj.ResolutionGroup.ActionItemExecutionTasks.ToList();
        _obj.ResolutionGroup.ActionItemExecutionTasks.Clear();
        Functions.Module.DeleteActionItemExecutionTasks(actionItems);
      }
    }
    
    public virtual void EndBlock12(Sungero.RecordManagement.Server.ReviewDraftResolutionAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region 13. Уведомление помощнику руководителя
    
    public virtual void StartBlock13(Sungero.RecordManagement.Server.ReviewObserversNotificationArguments e)
    {
      var addressee = Employees.As(_obj.Addressee);
      var assistant = Docflow.PublicFunctions.Module.GetSecretary(addressee);
      // Добавить помощника в качестве исполнителя, если он не делопроизводитель.
      if (!Equals(assistant, _obj.Author))
        e.Block.Performers.Add(assistant);
      
      // Получить вложенный для рассмотрения документ.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      
      // Задать тему.
      if (document.ExecutionState == ExecutionState.OnExecution)
        e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.AcquaintanceWithDocumentComplete, document.Name);
      else if (document.ExecutionState == ExecutionState.WithoutExecut)
        e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.ManagerIsInformed, document.Name);
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);

      // Выдать помощнику права на вложения.
      Docflow.PublicFunctions.Module.GrantReadRightsForAttachments(_obj.AddendaGroup.All.ToList(), e.Block.Performers);
    }
    
    public virtual void StartNotice13(Sungero.RecordManagement.IReviewObserversNotification notice, Sungero.RecordManagement.Server.ReviewObserversNotificationArguments e)
    {
      // Установить "От" как исполнителя рассмотрения.
      notice.Author = _obj.Addressee;
      
      notice.ThreadSubject = DocumentReviewTasks.Resources.ReviewCompletionNoticeThreadSubject;
    }
    
    public virtual void EndBlock13(Sungero.RecordManagement.Server.ReviewObserversNotificationEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region 8. Конец

    public virtual void StartReviewAssignment8(Sungero.Workflow.IReviewAssignment reviewAssignment)
    {
      
    }
    
    #endregion
    
  }
}