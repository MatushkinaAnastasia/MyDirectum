using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OfficialDocument;
using Sungero.Domain.Shared;
using Sungero.Workflow;

namespace Sungero.Docflow.Server
{
  partial class ApprovalTaskRouteHandlers
  {
    #region Уведомление инициатору о старте доработки(блок 42)
    public virtual void StartBlock42(Sungero.Docflow.Server.ApprovalNotificationArguments e)
    {
      var reworkPerformer = Functions.ApprovalTask.GetReworkPerformer(_obj, null);
      if (Equals(reworkPerformer, _obj.Author))
        return;
      
      // Задать тему.
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var lastAssignment = Functions.ApprovalTask.GetLastTaskAssigment(_obj, null);
      var subject = Functions.Module.TrimSpecialSymbols(FreeApprovalTasks.Resources.ReworkNoticeSubject, document.Name);
      
      if (ApprovalSigningAssignments.Is(lastAssignment) && lastAssignment.Result == Docflow.ApprovalSigningAssignment.Result.Abort ||
          ApprovalReviewAssignments.Is(lastAssignment) && lastAssignment.Result == Docflow.ApprovalReviewAssignment.Result.Abort)
        subject = Functions.Module.TrimSpecialSymbols(Sungero.Docflow.ApprovalTasks.Resources.RejectNoticeSubject,
                                                      document.Name);
      
      e.Block.Subject = subject;
      e.Block.Performers.Add(_obj.Author);
      // Выдать права на документы.
      Functions.ApprovalTask.GrantRightForAttachmentsToPerformers(_obj, new List<IRecipient>() { _obj.Author });
    }

    public virtual void StartNotice42(Sungero.Docflow.IApprovalNotification notice, Sungero.Docflow.Server.ApprovalNotificationArguments e)
    {
      notice.Author = Functions.ApprovalTask.GetLastAssignmentPerformer(_obj);
    }
    
    #endregion
    
    #region Согласовано?(блок 41)
    public virtual bool Decision41Result()
    {
      var currentTaskStartId = _obj.StartId;
      var minAssignmentDate = Assignments.GetAll(a => Equals(a.Task, _obj) && a.TaskStartId == currentTaskStartId).Min(a => a.Created);
      var reworkAssignments = ApprovalReworkAssignments.GetAll(a => Equals(a.Task, _obj) && a.TaskStartId == currentTaskStartId);
      
      DateTime lastIterationDate;
      if (reworkAssignments.Any())
      {
        var maxCreated = reworkAssignments.Max(a => a.Created);
        lastIterationDate = maxCreated > minAssignmentDate ? maxCreated.Value : minAssignmentDate.Value;
      }
      else
      {
        lastIterationDate = minAssignmentDate.Value;
      }
      
      var approved = true;
      var approvalAssignments = ApprovalAssignments.GetAll()
        .Where(a => Equals(a.Task, _obj) && a.Created >= lastIterationDate)
        .ToList();
      foreach (var assignment in approvalAssignments.Where(a => a.Result == Docflow.ApprovalAssignment.Result.ForRevision))
      {
        var hasApprovedAssignment = approvalAssignments.Any(a => Equals(a.Performer, assignment.Performer) &&
                                                            a.Modified > assignment.Modified &&
                                                            Equals(a.Result, Docflow.ApprovalAssignment.Result.Approved));
        if (!hasApprovedAssignment)
        {
          approved = false;
          break;
        }
      }
      return approved;
    }
    #endregion
    
    #region Начало (блок 26)

    public virtual void Script26Execute()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (_obj.StageNumber == null && document != null)
        Sungero.Docflow.Functions.OfficialDocument.SetLifeCycleStateDraft(document);
      
      _obj.StageNumber = Functions.ApprovalTask.GetNextStageNumber(_obj);
      if (_obj.StageNumber != null)
        Functions.ApprovalTask.UpdateApprovalState(_obj, InternalApprovalState.OnApproval);

      Functions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);
      Functions.OfficialDocument.AddRelatedDocumentsToAttachmentGroup(document, _obj.OtherGroup);
      
      // Обновить сроки задачи.
      _obj.MaxDeadline = Functions.ApprovalTask.GetExpectedDate(_obj);
    }

    #endregion

    #region Условия и исполнители вычислимы?

    public virtual bool Decision38Result()
    {
      // Если исполнитель не вычислен, то текстовка уже заполнена, уходим на доработку.
      if (_obj.IsStageAssigneeNotFound == true)
        return false;
      
      // Проверка вычислимости условий.
      var stages = Functions.ApprovalTask.GetStages(_obj);
      _obj.ReworkReason = stages.IsConditionsDefined ? string.Empty : stages.ErrorMessage;
      return stages.IsConditionsDefined;
    }

    #endregion

    #region Согласование с руководителем (блок 3)
    
    public virtual void StartBlock3(Sungero.Docflow.Server.ApprovalManagerAssignmentArguments e)
    {
      var stage = Functions.ApprovalTask.GetStage(_obj, Docflow.ApprovalStage.StageType.Manager);
      if (stage == null)
        return;
      
      e.Block.Subject = Functions.Module.TrimSpecialSymbols(ApprovalTasks.Resources.ApproversAsgSubject,
                                                            _obj.DocumentGroup.OfficialDocuments.First().Name);
      
      e.Block.ApprovalRule = _obj.ApprovalRule;
      
      e.Block.Stage = stage.Stage;
      
      e.Block.DeliveryMethod = _obj.DeliveryMethod;
      e.Block.ExchangeService = _obj.ExchangeService;
      
      var manager = Functions.ApprovalStage.GetStagePerformer(_obj, stage.Stage, null, null);
      var reworkAssignment = Functions.ApprovalTask.GetLastReworkAssignment(_obj);
      var isReworkPerformer = reworkAssignment != null && Equals(reworkAssignment.CompletedBy, manager);
      // Отправлять задание руководителю, если:
      // 1. Руководитель вычислился;
      // 2. Руководитель не инициатор и нет доработки;
      // 3. Есть доработка и руководитель не отв. за доработку.
      if (manager != null && ((!Equals(manager, _obj.Author) && reworkAssignment == null) ||
                              (!isReworkPerformer && reworkAssignment != null)))
      {
        e.Block.Performers.Add(manager);
        
        e.Block.RelativeDeadlineDays = stage.Stage.DeadlineInDays;
        e.Block.RelativeDeadlineHours = stage.Stage.DeadlineInHours;
        
        e.Block.Signatory = _obj.Signatory;
        e.Block.Addressee = _obj.Addressee;
        
        #region Сформировать список согласующих
        
        Functions.ApprovalTask.UpdateReglamentApprovers(_obj, _obj.ApprovalRule);
        
        foreach (var approver in _obj.AddApproversExpanded)
          e.Block.AddApprovers.AddNew().Approver = approver.Approver;
        
        #endregion
      }
      
      // Выдать права на документы
      Functions.ApprovalTask.GrantRightForAttachmentsToPerformers(_obj, e.Block.Performers.ToList());
    }

    public virtual void StartAssignment3(Sungero.Docflow.IApprovalManagerAssignment assignment, Sungero.Docflow.Server.ApprovalManagerAssignmentArguments e)
    {
      assignment.StageNumber = _obj.StageNumber;
      assignment.ThreadSubject = ApprovalTasks.Resources.ApprovalManagerAssignmentThreadSubject;
      var stage = Functions.ApprovalTask.GetStage(_obj, Docflow.ApprovalStage.StageType.Manager);
      assignment.ReworkPerformer = Functions.ApprovalTask.GetReworkPerformer(ApprovalTasks.As(assignment.Task), stage.Stage);
    }

    public virtual void CompleteAssignment3(Sungero.Docflow.IApprovalManagerAssignment assignment, Sungero.Docflow.Server.ApprovalManagerAssignmentArguments e)
    {
      // Пробросить исполнителя доработки в задачу.
      if (assignment.Result == Sungero.Docflow.ApprovalManagerAssignment.Result.ForRevision)
        _obj.ReworkPerformer = assignment.ReworkPerformer;
      
      var approvers = assignment.AddApprovers.Select(a => a.Approver).ToList();
      Docflow.Functions.ApprovalTask.UpdateAdditionalApprovers(_obj, approvers);
      
      if (_obj.Signatory != assignment.Signatory)
        _obj.Signatory = assignment.Signatory;
      
      if (_obj.Addressee != assignment.Addressee)
        _obj.Addressee = assignment.Addressee;
      
      if (_obj.DeliveryMethod != assignment.DeliveryMethod)
        Functions.ApprovalTask.RefreshDeliveryMethod(_obj, assignment.DeliveryMethod);
      
      if (_obj.ExchangeService != assignment.ExchangeService)
        _obj.ExchangeService = assignment.ExchangeService;
    }

    public virtual void EndBlock3(Sungero.Docflow.Server.ApprovalManagerAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion

    #region Доработка (блок 5)
    
    public virtual void StartBlock5(Sungero.Docflow.Server.ApprovalReworkAssignmentArguments e)
    {
      // Сбросить номер очередного этапа.
      _obj.StageNumber = null;
      
      // Апнуть Iteration.
      _obj.Iteration++;
      
      // Для схемы версии V2 будет заполняться Author.
      var reworkPerformer = Functions.ApprovalTask.GetReworkPerformer(_obj, null);
      e.Block.Performers.Add(reworkPerformer);
      if (_obj.ReworkPerformer != null)
        _obj.ReworkPerformer = null;
      
      // Тема.
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var subject = ApprovalTasks.Resources.NeedToRework;
      if (Functions.ApprovalTask.IsSignatoryAbortTask(_obj, null))
        subject = ApprovalTasks.Resources.RejectedToSign;
      else if (Functions.ApprovalTask.IsExternalSignatoryAbortTask(_obj, null))
      {
        subject = Functions.ApprovalTask.IsInvoiceAmendmentRequest(document) ? ApprovalTasks.Resources.InvoiceAmendmentRequest : ApprovalTasks.Resources.RejectedToExternalSing;
        if (Functions.ApprovalTask.IsAttachmentObsolete(_obj))
          subject = ApprovalTasks.Resources.RevokedByUs;
      }
      else if (Functions.ApprovalTask.IsAddresseeAbortTask(_obj, null))
        subject = ApprovalTasks.Resources.RejectedToReview;
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject, document != null ? document.Name : string.Empty);
      
      // Срок.
      e.Block.RelativeDeadlineDays = _obj.ApprovalRule.ReworkDeadline;
      
      e.Block.ApprovalRule = _obj.ApprovalRule;
      e.Block.Signatory = _obj.Signatory;
      e.Block.Addressee = _obj.Addressee;
      Functions.ApprovalTask.UpdateReglamentApprovers(_obj, _obj.ApprovalRule);

      // Сформировать список согласующих.
      // TODO Котегов: похоже на устаревший код - надо проверить и выпилить.
      var reqApprovers = Functions.Module.GetEmployeesFromRecipients(_obj.ReqApprovers.Select(r => r.Approver).ToList());
      foreach (var approver in reqApprovers)
        e.Block.RegApprovers.AddNew().Approver = approver;
      var additionalApprovers = Functions.Module.GetEmployeesFromRecipients(_obj.AddApproversExpanded.Select(a => a.Approver).ToList());
      foreach (var approver in additionalApprovers)
        e.Block.AddApprovers.AddNew().Approver = approver;
      
      // Исключить непосредственного руководителя из списка обязательных согласующих.
      var stages = Functions.ApprovalTask.GetStages(_obj);
      var managerStage = stages.Stages.FirstOrDefault(s => s.StageType == Docflow.ApprovalStage.StageType.Manager);
      if (managerStage != null)
      {
        var manager = Functions.ApprovalStage.GetStagePerformer(_obj, managerStage.Stage, null, null);
        reqApprovers = reqApprovers.Where(a => !Equals(a, manager)).ToList();
      }
      
      // Заполнить свойства для обмена.
      e.Block.DeliveryMethod = _obj.DeliveryMethod;
      e.Block.ExchangeService = _obj.ExchangeService;
      
      // Заполнить список согласующих.
      Functions.ApprovalTask.FillApproversList(_obj, e.Block, reqApprovers, true);
      Functions.ApprovalTask.FillApproversList(_obj, e.Block, additionalApprovers, false);
    }

    public virtual void StartAssignment5(Sungero.Docflow.IApprovalReworkAssignment assignment, Sungero.Docflow.Server.ApprovalReworkAssignmentArguments e)
    {
      ((Domain.Shared.IExtendedEntity)assignment).Params[Constants.ApprovalTask.CreateFromSchema] = true;
      assignment.StageNumber = _obj.StageNumber;
      
      // Когда у задачи есть "причина доработки", это задание от системы.
      if (!string.IsNullOrWhiteSpace(_obj.ReworkReason))
      {
        assignment.ActiveText = ApprovalTasks.Resources.ReworkReasonSubjectFormat(_obj.ReworkReason);
        assignment.Author = Users.Current;
      }
      else
        assignment.Author = Functions.ApprovalTask.GetLastAssignmentPerformer(_obj);
      
      // Обновить статус согласования - на доработке.
      Functions.ApprovalTask.UpdateApprovalState(_obj, InternalApprovalState.OnRework);
      
      // Сбросить статус согласования с КА, если он был помечен как подписанный.
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetIncomingExDocumentInfo(document) == null &&
          document.ExternalApprovalState == ExternalApprovalState.Signed)
      {
        document.ExternalApprovalState = null;
        _obj.DocumentExternalApprovalState = Docflow.ApprovalTask.DocumentExternalApprovalState.Empty;
      }
      
      Functions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);
      Functions.OfficialDocument.AddRelatedDocumentsToAttachmentGroup(document, _obj.OtherGroup);
      
      // Выдать права на документы
      Functions.ApprovalTask.GrantRightForAttachmentsToPerformers(_obj, new List<IRecipient>() { assignment.Performer });
      
      // Выдать права на задачу исполнителю доработки
      if (!Equals(_obj.Author, assignment.Performer) &&
          !_obj.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, assignment.Performer) &&
          !_obj.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, assignment.Performer))
        _obj.AccessRights.Grant(assignment.Performer, DefaultAccessRightsTypes.Change);
      
      // Обновить сроки задачи.
      _obj.MaxDeadline = Functions.ApprovalTask.GetExpectedDate(_obj);
    }

    public virtual void CompleteAssignment5(Sungero.Docflow.IApprovalReworkAssignment assignment, Sungero.Docflow.Server.ApprovalReworkAssignmentArguments e)
    {
      if (assignment.Result == Sungero.Docflow.ApprovalAssignment.Result.Forward)
      {
        assignment.Forward(assignment.ForwardPerformer, ForwardingLocation.Next);
      }
      else
      {
        var approvers = assignment.AddApprovers.Select(a => Sungero.CoreEntities.Recipients.As(a.Approver)).ToList();
        Docflow.Functions.ApprovalTask.UpdateAdditionalApprovers(_obj, approvers);
        
        if (_obj.Signatory != assignment.Signatory)
          _obj.Signatory = assignment.Signatory;
        
        if (_obj.Addressee != assignment.Addressee)
          _obj.Addressee = assignment.Addressee;
        
        if (_obj.DeliveryMethod != assignment.DeliveryMethod)
          Functions.ApprovalTask.RefreshDeliveryMethod(_obj, assignment.DeliveryMethod);
        
        if (_obj.ExchangeService != assignment.ExchangeService)
          _obj.ExchangeService = assignment.ExchangeService;
      }
    }

    public virtual void EndBlock5(Sungero.Docflow.Server.ApprovalReworkAssignmentEndBlockEventArguments e)
    {
      _obj.IsStageAssigneeNotFound = false;
    }
    
    #endregion
    
    #region Уведомления согласующим (блок 39)
    
    public virtual void StartBlock39(Sungero.Docflow.Server.ApprovalNotificationArguments e)
    {
      // Задать тему.
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      e.Block.Subject = Functions.Module.TrimSpecialSymbols(FreeApprovalTasks.Resources.NewApprovalLapSubject,
                                                            document.Name);
      
      var lastReworkAssignment = Functions.ApprovalTask.GetLastReworkAssignment(_obj);
      if (lastReworkAssignment == null)
        return;
      
      // Признак, что уведомления уже были отправлены.
      var hasNotices = ApprovalNotifications.GetAll().Where(n => Equals(n.Task, _obj) && n.Created >= lastReworkAssignment.Completed).Any();
      if (hasNotices)
        return;
      
      // Собрать исполнителей из заданий на доработку и руководителю, которым нужно отправить уведомления.
      var approvers = lastReworkAssignment.Approvers.Where(a => a.Action == Sungero.Docflow.ApprovalReworkAssignmentApprovers.Action.SendNotice).Select(a => a.Approver);
      foreach (var approver in approvers)
        e.Block.Performers.Add(approver);
    }

    public virtual void StartNotice39(Sungero.Docflow.IApprovalNotification notice, Sungero.Docflow.Server.ApprovalNotificationArguments e)
    {
      
    }

    public virtual void EndBlock39(Sungero.Docflow.Server.ApprovalNotificationEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region Согласование с согласующими (блок 6)

    public virtual void StartBlock6(Sungero.Docflow.Server.ApprovalAssignmentArguments e)
    {
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Approvers);
      if (stage == null)
        return;
      
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(ApprovalTasks.Resources.ApproversAsgSubject,
                                                                          _obj.DocumentGroup.OfficialDocuments.First().Name);
      e.Block.Stage = stage.Stage;
      
      var author = _obj.Author;
      
      #region Сформировать список согласующих

      Functions.ApprovalTask.UpdateReglamentApprovers(_obj, _obj.ApprovalRule);
      
      #endregion
      
      var approvers = Functions.ApprovalAssignment.GetCurrentIterationEmployees(_obj, stage.Stage);
      foreach (var approver in approvers)
        e.Block.Performers.Add(approver);
      
      e.Block.IsParallel = stage.Stage.Sequence == Docflow.ApprovalStage.Sequence.Parallel;
      
      e.Block.RelativeDeadlineDays = stage.Stage.DeadlineInDays;
      e.Block.RelativeDeadlineHours = stage.Stage.DeadlineInHours;
      
      // Задать результат выполнения, при котором остановятся все задания по блоку.
      if (stage.Stage.ReworkType == Sungero.Docflow.ApprovalStage.ReworkType.AfterEach)
        e.Block.StopResult = Docflow.ApprovalAssignment.Result.ForRevision;
    }

    public virtual void StartAssignment6(Sungero.Docflow.IApprovalAssignment assignment, Sungero.Docflow.Server.ApprovalAssignmentArguments e)
    {
      // WARN Zamerov: в этом событии не надо менять задачу, см 76408.
      assignment.StageNumber = _obj.StageNumber;
      
      // Выдать права на документы.
      Functions.ApprovalTask.GrantRightForAttachmentsToPerformers(_obj, e.Block.Performers.ToList());
      
      // Создание нового задания может изменить срок задачи.
      var approvalTask = ApprovalTasks.As(assignment.Task);
      var stages = Functions.ApprovalTask.GetStages(approvalTask).Stages;
      assignment.Task.MaxDeadline = Functions.ApprovalTask.GetExpectedDate(approvalTask, assignment, stages);
      var stage = Functions.ApprovalTask.GetStage(_obj, Docflow.ApprovalStage.StageType.Approvers);
      assignment.ReworkPerformer = Functions.ApprovalTask.GetReworkPerformer(ApprovalTasks.As(assignment.Task), stage.Stage);
      Logger.DebugFormat("Start new approval assignment id {0}, performer {1}, rework performer {2}.", assignment.Id, assignment.Performer.Id,
                         assignment.ReworkPerformer != null ? assignment.ReworkPerformer.Id : 0);
    }

    public virtual void CompleteAssignment6(Sungero.Docflow.IApprovalAssignment assignment, Sungero.Docflow.Server.ApprovalAssignmentArguments e)
    {
      // Пробросить исполнителя доработки в задачу.
      if (assignment.Result == Sungero.Docflow.ApprovalAssignment.Result.ForRevision)
        _obj.ReworkPerformer = assignment.ReworkPerformer;
      
      // Синхронизировать приложения документа.
      Functions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentGroup.OfficialDocuments.FirstOrDefault());
      Functions.OfficialDocument.AddRelatedDocumentsToAttachmentGroup(_obj.DocumentGroup.OfficialDocuments.FirstOrDefault(), _obj.OtherGroup);
      
      var recipientsToGrantRights = Functions.Module.GetTaskAssignees(_obj);
      Functions.ApprovalTask.GrantRightForAttachmentsToPerformers(_obj, recipientsToGrantRights);

      if (assignment.Result == Sungero.Docflow.ApprovalAssignment.Result.Forward)
      {
        assignment.Forward(assignment.Addressee, ForwardingLocation.Next);
        
        var requiredApprovers = Functions.ApprovalTask.GetAllRequiredApprovers(_obj);
        if (assignment.Stage.StageType == Docflow.ApprovalStage.StageType.Approvers &&
            assignment.Stage.AllowAdditionalApprovers == true &&
            !requiredApprovers.Contains(assignment.Addressee) &&
            !_obj.AddApproversExpanded.Select(x => x.Approver).Contains(assignment.Addressee))
        {
          var newApprover = _obj.AddApproversExpanded.AddNew();
          newApprover.Approver = assignment.Addressee;
        }
      }
      
      if (assignment.Stage.StageType == Docflow.ApprovalStage.StageType.Approvers && assignment.Stage.AllowAdditionalApprovers == true)
      {
        var requiredApprovers = Functions.ApprovalTask.GetAllRequiredApprovers(_obj);
        var forwarders = assignment.ForwardedTo;
        
        foreach (var forwarder in forwarders)
        {
          if (requiredApprovers.Contains(forwarder))
            continue;
          
          if (!_obj.AddApproversExpanded.Select(x => x.Approver).Contains(forwarder))
          {
            var approverExpandedForwarded = _obj.AddApproversExpanded.AddNew();
            approverExpandedForwarded.Approver = forwarder;
          }
          if (!_obj.AddApprovers.Select(x => x.Approver).Contains(forwarder))
          {
            var approverAdd = _obj.AddApprovers.AddNew();
            approverAdd.Approver = forwarder;
          }
        }
      }
    }

    public virtual void EndBlock6(Sungero.Docflow.Server.ApprovalAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region Регистрация (блок 23)
    
    public virtual void StartBlock23(Sungero.Docflow.Server.ApprovalRegistrationAssignmentArguments e)
    {
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Register);
      if (stage == null)
        return;
      
      // Схлопнуть, если нужно.
      if (Functions.ApprovalTask.NeedCollapse(_obj, stage))
        return;
      
      // Задать исполнителей.
      var performer = Docflow.PublicFunctions.ApprovalStage.GetStagePerformer(_obj, stage.Stage, null, null);
      if (performer == null)
      {
        Functions.ApprovalTask.FillReworkReasonWhenAssigneeNotFound(_obj, stage.Stage);
        return;
      }
      e.Block.Performers.Add(performer);
      
      // Задать тему.
      e.Block.Subject = Functions.ApprovalTask.GetCollapsedSubject(_obj, stage);
      
      // Задать срок.
      e.Block.RelativeDeadlineDays = Functions.ApprovalTask.CollapsedDeadlineInDays(_obj, stage);
      e.Block.RelativeDeadlineHours = Functions.ApprovalTask.CollapsedDeadlineInHours(_obj, stage);
      
      // Заполнить список типов схлопнутых этапов.
      var collapsedStageTypes = Functions.ApprovalTask.GetCollapsedStagesTypes(_obj, stage);
      foreach (var stageType in collapsedStageTypes)
        e.Block.CollapsedStagesTypesReg.AddNew().StageType = stageType;
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var stages = Functions.ApprovalTask.GetStages(_obj).Stages;
      
      var hasSignStage = Functions.ApprovalRuleBase.HasApprovalStage(_obj.ApprovalRule, Docflow.ApprovalStage.StageType.Sign, document, stages);
      var hasReviewStage = Functions.ApprovalRuleBase.HasApprovalStage(_obj.ApprovalRule, Sungero.Docflow.ApprovalStage.StageType.Review, document, stages);
      
      // Если схлопнуто с печатью, то задать подписывающего и адресата в карточке.
      if (collapsedStageTypes.Any(t => t == Sungero.Docflow.ApprovalStage.StageType.Print))
      {
        if (hasSignStage)
          e.Block.Signatory = _obj.Signatory;
        if (hasReviewStage)
          e.Block.Addressee = _obj.Addressee;
      }
      
      // Если схлопнуто с отправкой, заполнить способ доставки и сервис обмена.
      if (collapsedStageTypes.Any(t => t == Sungero.Docflow.ApprovalStage.StageType.Sending))
      {
        e.Block.DeliveryMethod = _obj.DeliveryMethod;
        e.Block.ExchangeService = _obj.ExchangeService;
      }
      
      // Выдать права на документы.
      Functions.ApprovalTask.GrantRightForAttachmentsToPerformers(_obj, e.Block.Performers.ToList());
    }

    public virtual void StartAssignment23(Sungero.Docflow.IApprovalRegistrationAssignment assignment, Sungero.Docflow.Server.ApprovalRegistrationAssignmentArguments e)
    {
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Register);
      assignment.StageNumber = _obj.StageNumber;
      assignment.ThreadSubject = Functions.ApprovalTask.GetCollapsedThreadSubject(_obj, stage);
      assignment.ReworkPerformer = Functions.ApprovalTask.GetReworkPerformer(ApprovalTasks.As(assignment.Task), stage.Stage);
    }

    public virtual void CompleteAssignment23(Sungero.Docflow.IApprovalRegistrationAssignment assignment, Sungero.Docflow.Server.ApprovalRegistrationAssignmentArguments e)
    {
      if (assignment.Result == Sungero.Docflow.ApprovalRegistrationAssignment.Result.ForRevision)
        // Пробросить исполнителя доработки в задачу.
        _obj.ReworkPerformer = assignment.ReworkPerformer;
      
      if (assignment.Result == Sungero.Docflow.ApprovalRegistrationAssignment.Result.Execute)
        // Добавить запись о выдаче документа.
        Functions.ApprovalTask.IssueDocument(_obj, assignment.Performer.Id);
    }

    public virtual void EndBlock23(Sungero.Docflow.Server.ApprovalRegistrationAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region Печать (блок 20)
    
    public virtual void StartBlock20(Sungero.Docflow.Server.ApprovalPrintingAssignmentArguments e)
    {
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Print);
      if (stage == null)
        return;
      
      // Схлопнуть, если нужно.
      if (Functions.ApprovalTask.NeedCollapse(_obj, stage))
        return;

      // Задать исполнителя.
      var performer = Docflow.PublicFunctions.ApprovalStage.GetStagePerformer(_obj, stage.Stage, null, null);
      if (performer == null)
      {
        Functions.ApprovalTask.FillReworkReasonWhenAssigneeNotFound(_obj, stage.Stage);
        return;
      }
      e.Block.Performers.Add(performer);
      
      e.Block.Subject = Functions.ApprovalTask.GetCollapsedSubject(_obj, stage);
      
      e.Block.RelativeDeadlineDays = Functions.ApprovalTask.CollapsedDeadlineInDays(_obj, stage);
      e.Block.RelativeDeadlineHours = Functions.ApprovalTask.CollapsedDeadlineInHours(_obj, stage);
      e.Block.Signatory = _obj.Signatory;
      e.Block.Addressee = _obj.Addressee;
      
      // Заполнить список типов схлопнутых этапов.
      foreach (var stageType in Functions.ApprovalTask.GetCollapsedStagesTypes(_obj, stage))
        e.Block.CollapsedStagesTypesPr.AddNew().StageType = stageType;
      
      if (e.Block.CollapsedStagesTypesPr.Any(x => x.StageType == Sungero.Docflow.ApprovalStage.StageType.Sending))
      {
        e.Block.DeliveryMethod = _obj.DeliveryMethod;
        e.Block.ExchangeService = _obj.ExchangeService;
      }
    }

    public virtual void StartAssignment20(Sungero.Docflow.IApprovalPrintingAssignment assignment, Sungero.Docflow.Server.ApprovalPrintingAssignmentArguments e)
    {
      // Добавить вложения в группу "Документы на печать".
      foreach (var entity in _obj.DocumentGroup.All)
        if (!assignment.ForPrinting.All.Any(a => Equals(a, entity)))
          assignment.ForPrinting.All.Add(entity);
      
      foreach (var entity in _obj.AddendaGroup.All)
        if (!assignment.ForPrinting.All.Any(a => Equals(a, entity)))
          assignment.ForPrinting.All.Add(entity);

      // Выдать права на просмотр всех документов.
      foreach (var document in assignment.ForPrinting.ElectronicDocuments.ToList())
      {
        document.AccessRights.Grant(assignment.Performer, DefaultAccessRightsTypes.Read);
      }
      assignment.StageNumber = _obj.StageNumber;
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Print);
      assignment.ThreadSubject = Functions.ApprovalTask.GetCollapsedThreadSubject(_obj, stage);
      assignment.ReworkPerformer = Functions.ApprovalTask.GetReworkPerformer(ApprovalTasks.As(assignment.Task), stage.Stage);
    }

    public virtual void CompleteAssignment20(Sungero.Docflow.IApprovalPrintingAssignment assignment, Sungero.Docflow.Server.ApprovalPrintingAssignmentArguments e)
    {
      if (assignment.Result == Sungero.Docflow.ApprovalPrintingAssignment.Result.ForRevision)
        // Пробросить исполнителя доработки в задачу.
        _obj.ReworkPerformer = assignment.ReworkPerformer;
      
      if (assignment.Result == Sungero.Docflow.ApprovalPrintingAssignment.Result.Execute)
        // Добавить запись о выдаче документа.
        Functions.ApprovalTask.IssueDocument(_obj, assignment.Performer.Id);
    }

    public virtual void EndBlock20(Sungero.Docflow.Server.ApprovalPrintingAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region Подписание (блок 9)
    
    public virtual void StartBlock9(Sungero.Docflow.Server.ApprovalSigningAssignmentArguments e)
    {
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Sign);
      if (stage == null)
        return;
      
      // Схлопнуть, если нужно.
      if (Functions.ApprovalTask.NeedCollapse(_obj, stage))
        return;
      
      // Пропустить, если следующим этапом идет рассмотрение и исполнитель совпадает.
      if (Functions.ApprovalTask.NeedSkipSignStage(_obj, stage, _obj.Signatory, _obj.Addressee))
        return;
      
      // Задать исполнителя.
      var signatory = Functions.ApprovalStage.GetStagePerformer(_obj, stage.Stage) ?? _obj.Signatory;
      
      e.Block.Performers.Add(signatory);
      
      // Задать тему.
      e.Block.Subject = Functions.ApprovalTask.GetCollapsedSubject(_obj, stage);
      
      e.Block.Stage = stage.Stage;
      e.Block.RelativeDeadlineDays = Functions.ApprovalTask.CollapsedDeadlineInDays(_obj, stage);
      e.Block.RelativeDeadlineHours = Functions.ApprovalTask.CollapsedDeadlineInHours(_obj, stage);
      
      // Заполнить список типов схлопнутых этапов.
      foreach (var stageType in Functions.ApprovalTask.GetCollapsedStagesTypes(_obj, stage))
        e.Block.CollapsedStagesTypesSig.AddNew().StageType = stageType;
      
      if (e.Block.CollapsedStagesTypesSig.Any(x => x.StageType == Sungero.Docflow.ApprovalStage.StageType.Sending))
      {
        e.Block.DeliveryMethod = _obj.DeliveryMethod;
        e.Block.ExchangeService = _obj.ExchangeService;
      }
      
      // Выдать права на документы, не выше чем права инициатора.
      Functions.ApprovalTask.GrantRightForAttachmentsToPerformers(_obj, e.Block.Performers.ToList());
    }

    public virtual void StartAssignment9(Sungero.Docflow.IApprovalSigningAssignment assignment, Sungero.Docflow.Server.ApprovalSigningAssignmentArguments e)
    {
      var document = assignment.DocumentGroup.OfficialDocuments.First();
      
      // Установить признак Подтверждения подписания для контроля состояния.
      var confirmBy = Functions.ApprovalStage.GetConfirmByForSignatory(assignment.Stage, _obj.Signatory, _obj);
      assignment.IsConfirmSigning = confirmBy != null;
      
      // Обновить статус согласования - на подписании.
      Functions.ApprovalTask.UpdateApprovalState(_obj, InternalApprovalState.PendingSign);
      
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Sign);
      assignment.IsCollapsed = Functions.ApprovalTask.GetCollapsedStages(_obj, stage).Any(s => !Equals(s, stage));
      assignment.StageNumber = _obj.StageNumber;
      assignment.ThreadSubject = Functions.ApprovalTask.GetCollapsedThreadSubject(_obj, stage);
      assignment.ReworkPerformer = Functions.ApprovalTask.GetReworkPerformer(ApprovalTasks.As(assignment.Task), stage.Stage);
    }

    public virtual void CompleteAssignment9(Sungero.Docflow.IApprovalSigningAssignment assignment, Sungero.Docflow.Server.ApprovalSigningAssignmentArguments e)
    {
      // Пробросить исполнителя доработки в задачу.
      if (assignment.Result == Sungero.Docflow.ApprovalSigningAssignment.Result.ForRevision ||
          assignment.Result == Sungero.Docflow.ApprovalSigningAssignment.Result.Abort)
        _obj.ReworkPerformer = assignment.ReworkPerformer;
      
      if (assignment.Result == Docflow.ApprovalSigningAssignment.Result.Sign || assignment.Result == Docflow.ApprovalSigningAssignment.Result.ConfirmSign)
      {
        // Обновить статус согласования - подписан.
        Functions.ApprovalTask.UpdateApprovalState(_obj, InternalApprovalState.Signed);
        
        // Добавить запись о выдаче документа.
        Functions.ApprovalTask.IssueDocument(_obj, assignment.Performer.Id);
      }
    }

    public virtual void EndBlock9(Sungero.Docflow.Server.ApprovalSigningAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region Рассмотрение (блок 36)
    
    public virtual void StartBlock36(Sungero.Docflow.Server.ApprovalReviewAssignmentArguments e)
    {
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Review);
      if (stage == null)
        return;
      
      // Схлопнуть, если нужно.
      if (Functions.ApprovalTask.NeedCollapse(_obj, stage))
        return;
      
      // Задать исполнителя.
      var addressee = Functions.ApprovalStage.GetStagePerformer(_obj, stage.Stage) ?? _obj.Addressee;
      e.Block.Performers.Add(addressee);
      
      // Задать тему.
      e.Block.Subject = Functions.ApprovalTask.GetCollapsedSubject(_obj, stage);
      
      e.Block.Stage = stage.Stage;
      e.Block.RelativeDeadlineDays = Functions.ApprovalTask.CollapsedDeadlineInDays(_obj, stage);
      e.Block.RelativeDeadlineHours = Functions.ApprovalTask.CollapsedDeadlineInHours(_obj, stage);
      
      // Заполнить список типов схлопнутых этапов.
      var collapsedStageTypes = Functions.ApprovalTask.GetCollapsedStagesTypes(_obj, stage);
      foreach (var stageType in collapsedStageTypes)
        e.Block.CollapsedStagesTypesRe.AddNew().StageType = stageType;
      
      // Выдать права на документы, не выше чем права инициатора.
      Functions.ApprovalTask.GrantRightForAttachmentsToPerformers(_obj, e.Block.Performers.ToList());
      
      // Если адресат не является исполнителем и схлопнуто с печатью, то задать адресата в карточке.
      if (!Equals(_obj.Addressee, addressee) && collapsedStageTypes.Any(t => t == Sungero.Docflow.ApprovalStage.StageType.Print))
      {
        e.Block.Addressee = _obj.Addressee;
      }
      
      if (e.Block.CollapsedStagesTypesRe.Any(x => x.StageType == Sungero.Docflow.ApprovalStage.StageType.Sending))
      {
        e.Block.DeliveryMethod = _obj.DeliveryMethod;
        e.Block.ExchangeService = _obj.ExchangeService;
      }
      
      // Заполнить, схлопнулось ли задание с чем-либо.
      e.Block.IsCollapsed = Functions.ApprovalTask.GetCollapsedStages(_obj, stage).Any(s => !Equals(s, stage));
    }

    public virtual void StartAssignment36(Sungero.Docflow.IApprovalReviewAssignment assignment, Sungero.Docflow.Server.ApprovalReviewAssignmentArguments e)
    {
      // Установить признак Внесения результата рассмотрения адресатом для контроля состояния.
      var assistant = Functions.ApprovalStage.GetAddresseeAssistantForResultSubmission(assignment.Stage, _obj.Addressee, _obj);
      assignment.IsResultSubmission = assistant != null;
      
      // Предвычислить ответственного за группу регистрации документа.
      var document = _obj.DocumentGroup.OfficialDocuments.First();

      // Обновить статус согласования - на рассмотрении.
      if (Memos.Is(document))
        Memos.As(document).InternalApprovalState = Docflow.Memo.InternalApprovalState.PendingReview;
      
      // Запомнить признак показа "Вынести резолюцию".
      // TODO Котегов: перенести на нормальный клиентский код с предвычислением при открытии карточки.
      assignment.NeedHideAddResolutionAction = Functions.ApprovalReviewAssignment.NeedHideAddResolutionAction(assignment);
      
      assignment.StageNumber = _obj.StageNumber;
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Review);
      assignment.ThreadSubject = Functions.ApprovalTask.GetCollapsedThreadSubject(_obj, stage);
      assignment.ReworkPerformer = Functions.ApprovalTask.GetReworkPerformer(ApprovalTasks.As(assignment.Task), stage.Stage);
    }

    public virtual void CompleteAssignment36(Sungero.Docflow.IApprovalReviewAssignment assignment, Sungero.Docflow.Server.ApprovalReviewAssignmentArguments e)
    {
      if (assignment.Result == Sungero.Docflow.ApprovalReviewAssignment.Result.ForRework ||
          assignment.Result == Sungero.Docflow.ApprovalReviewAssignment.Result.Abort)
      {
        // Пробросить исполнителя доработки в задачу.
        _obj.ReworkPerformer = assignment.ReworkPerformer;
      }
      
      // Обновить статус согласования служебной записки - Рассмотрен, если она не отправлена на доработку.
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      if (Memos.Is(document))
      {
        if (Equals(assignment.Result, Docflow.ApprovalReviewAssignment.Result.ForRework))
          document.InternalApprovalState = InternalApprovalState.OnRework;
        else
          Memos.As(document).InternalApprovalState = Docflow.Memo.InternalApprovalState.Reviewed;
      }
      
      // Заполнить текст резолюции из задания руководителя в задачу.
      if (assignment.Result == Docflow.ApprovalReviewAssignment.Result.AddResolution)
        _obj.ResolutionText = assignment.ActiveText;
      
      // Обновить статус исполнения - не требует исполнения.
      if (assignment.Result == Docflow.ApprovalReviewAssignment.Result.Informed)
        document.ExecutionState = ExecutionState.WithoutExecut;
    }

    public virtual void EndBlock36(Sungero.Docflow.Server.ApprovalReviewAssignmentEndBlockEventArguments e)
    {
      
    }

    #endregion

    #region Создание поручений (блок 37)
    
    public virtual void StartBlock37(Sungero.Docflow.Server.ApprovalExecutionAssignmentArguments e)
    {
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Execution);
      if (stage == null)
        return;
      
      // Создание поручений по документу нужно только в том случае, если на этапе рассмотрения вынесена резолюция, или если есть этап подписания и нет этапа рассмотрения.
      var isExecutionNeeded = Functions.ApprovalExecutionAssignment.NeedExecutionAssignment(_obj);
      if (!isExecutionNeeded)
        return;
      
      // Схлопнуть, если нужно.
      if (Functions.ApprovalTask.NeedCollapse(_obj, stage))
        return;
      
      // Задать исполнителя.
      var secretary = Functions.ApprovalStage.GetStagePerformer(_obj, stage.Stage);
      if (secretary == null)
      {
        Functions.ApprovalTask.FillReworkReasonWhenAssigneeNotFound(_obj, stage.Stage);
        return;
      }
      e.Block.Performers.Add(secretary);
      
      // Задать тему.
      e.Block.Subject = Functions.ApprovalTask.GetCollapsedSubject(_obj, stage);
      
      e.Block.Stage = stage.Stage;
      e.Block.RelativeDeadlineDays = Functions.ApprovalTask.CollapsedDeadlineInDays(_obj, stage);
      e.Block.RelativeDeadlineHours = Functions.ApprovalTask.CollapsedDeadlineInHours(_obj, stage);
      
      // Заполнить список типов схлопнутых этапов.
      foreach (var stageType in Functions.ApprovalTask.GetCollapsedStagesTypes(_obj, stage))
        e.Block.CollapsedStagesTypesExe.AddNew().StageType = stageType;
      
      if (e.Block.CollapsedStagesTypesExe.Any(x => x.StageType == Sungero.Docflow.ApprovalStage.StageType.Sending))
      {
        e.Block.DeliveryMethod = _obj.DeliveryMethod;
        e.Block.ExchangeService = _obj.ExchangeService;
      }
      
      // Выдать права на документы, не выше чем права инициатора.
      Functions.ApprovalTask.GrantRightForAttachmentsToPerformers(_obj, e.Block.Performers.ToList());
    }

    public virtual void StartAssignment37(Sungero.Docflow.IApprovalExecutionAssignment assignment, Sungero.Docflow.Server.ApprovalExecutionAssignmentArguments e)
    {
      assignment.ResolutionText = _obj.ResolutionText;
      assignment.StageNumber = _obj.StageNumber;
      assignment.Author = _obj.Addressee ?? _obj.Signatory ?? assignment.Author;
      
      // Обновить статус исполнения - Отправка на исполнение.
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      document.ExecutionState = ExecutionState.Sending;
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Execution);
      assignment.ThreadSubject = Functions.ApprovalTask.GetCollapsedThreadSubject(_obj, stage);
      assignment.ReworkPerformer = Functions.ApprovalTask.GetReworkPerformer(ApprovalTasks.As(assignment.Task), stage.Stage);
    }

    public virtual void CompleteAssignment37(Sungero.Docflow.IApprovalExecutionAssignment assignment, Sungero.Docflow.Server.ApprovalExecutionAssignmentArguments e)
    {
      if (assignment.Result == Sungero.Docflow.ApprovalExecutionAssignment.Result.ForRevision)
        // Пробросить исполнителя доработки в задачу.
        _obj.ReworkPerformer = assignment.ReworkPerformer;
      
      // Обновить статус исполнения документа - поставить "Не требует исполнения", если от задачи не создано ни одного поручения.
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      if (!Functions.Module.HasSubActionItems(_obj))
        document.ExecutionState = ExecutionState.WithoutExecut;
    }

    public virtual void EndBlock37(Sungero.Docflow.Server.ApprovalExecutionAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion

    #region Отправка контрагенту (блок 28)

    public virtual void StartBlock28(Sungero.Docflow.Server.ApprovalSendingAssignmentArguments e)
    {
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Sending);
      if (stage == null)
        return;
      
      // Схлопнуть, если нужно.
      if (Functions.ApprovalTask.NeedCollapse(_obj, stage))
        return;
      
      // Если документ подписан КА, но не подписан нами, не отправлять КА.
      if (Functions.ApprovalTask.NeedSkipSendingStage(_obj, false))
        return;
      
      // Задать исполнителей.
      var performer = Docflow.PublicFunctions.ApprovalStage.GetStagePerformer(_obj, stage.Stage, null, null);
      if (performer == null)
      {
        Functions.ApprovalTask.FillReworkReasonWhenAssigneeNotFound(_obj, stage.Stage);
        return;
      }
      e.Block.Performers.Add(performer);
      e.Block.IsParallel = stage.Stage.Sequence == Docflow.ApprovalStage.Sequence.Parallel;

      // Задать тему.
      e.Block.Subject = Functions.ApprovalTask.GetCollapsedSubject(_obj, stage);
      
      // Задать срок.
      e.Block.RelativeDeadlineDays = Functions.ApprovalTask.CollapsedDeadlineInDays(_obj, stage);
      e.Block.RelativeDeadlineHours = Functions.ApprovalTask.CollapsedDeadlineInHours(_obj, stage);
      
      e.Block.Stage = stage.Stage;
      
      e.Block.DeliveryMethod = _obj.DeliveryMethod;
      e.Block.ExchangeService = _obj.ExchangeService;
      
      // Заполнить список типов схлопнутых этапов.
      foreach (var stageType in Functions.ApprovalTask.GetCollapsedStagesTypes(_obj, stage))
        e.Block.CollapsedStagesTypesSen.AddNew().StageType = stageType;
      
      // Выдать права на документы.
      Functions.ApprovalTask.GrantRightForAttachmentsToPerformers(_obj, e.Block.Performers.ToList());
    }

    public virtual void StartAssignment28(Sungero.Docflow.IApprovalSendingAssignment assignment, Sungero.Docflow.Server.ApprovalSendingAssignmentArguments e)
    {
      assignment.StageNumber = _obj.StageNumber;
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Sending);
      assignment.ThreadSubject = Functions.ApprovalTask.GetCollapsedThreadSubject(_obj, stage);
      assignment.ReworkPerformer = Functions.ApprovalTask.GetReworkPerformer(ApprovalTasks.As(assignment.Task), stage.Stage);
    }

    public virtual void CompleteAssignment28(Sungero.Docflow.IApprovalSendingAssignment assignment, Sungero.Docflow.Server.ApprovalSendingAssignmentArguments e)
    {
      if (assignment.Result == Sungero.Docflow.ApprovalSendingAssignment.Result.ForRevision)
      {
        // Пробросить исполнителя доработки в задачу.
        _obj.ReworkPerformer = assignment.ReworkPerformer;
      }
      
      if (assignment.Result != Sungero.Docflow.ApprovalSendingAssignment.Result.ForRevision)
        // Добавить запись о выдаче документа.
        Functions.ApprovalTask.IssueDocument(_obj, assignment.Performer.Id);
    }

    public virtual void EndBlock28(Sungero.Docflow.Server.ApprovalSendingAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region Задание (блок 30)
    
    public virtual void StartBlock30(Sungero.Docflow.Server.ApprovalSimpleAssignmentArguments e)
    {
      var ruleStage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.SimpleAgr);
      if (ruleStage == null || ruleStage.Stage.AllowSendToRework == true)
        return;
      
      var stage = ruleStage.Stage;
      
      // Задать тему.
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      e.Block.Subject = Docflow.PublicFunctions.ApprovalRuleBase.FormatStageSubject(stage, document);
      
      // Задать исполнителей.
      e.Block.IsParallel = stage.Sequence == Docflow.ApprovalStage.Sequence.Parallel;
      var performers = Docflow.PublicFunctions.ApprovalStage.Remote.GetStagePerformers(_obj, stage);
      if (!performers.Any())
      {
        Functions.ApprovalTask.FillReworkReasonWhenAssigneeNotFound(_obj, stage);
        return;
      }
      foreach (var performer in performers)
        e.Block.Performers.Add(performer);

      // Срок.
      e.Block.RelativeDeadlineDays = stage.DeadlineInDays;
      e.Block.RelativeDeadlineHours = stage.DeadlineInHours;
      
      // Тема из этапа.
      e.Block.StageSubject = stage.Subject;
      
      // Выдать права на документы.
      var recipients = Docflow.PublicFunctions.ApprovalStage.Remote.GetStageRecipients(stage, _obj);
      Functions.ApprovalTask.GrantRightForAttachmentsToPerformers(_obj, recipients);
    }

    public virtual void StartAssignment30(Sungero.Docflow.IApprovalSimpleAssignment assignment, Sungero.Docflow.Server.ApprovalSimpleAssignmentArguments e)
    {
      assignment.StageNumber = _obj.StageNumber;
      assignment.ThreadSubject = e.Block.StageSubject;
    }

    public virtual void CompleteAssignment30(Sungero.Docflow.IApprovalSimpleAssignment assignment, Sungero.Docflow.Server.ApprovalSimpleAssignmentArguments e)
    {

    }

    public virtual void EndBlock30(Sungero.Docflow.Server.ApprovalSimpleAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region Задание c отправкой на доработку (блок 31)
    
    public virtual void StartBlock31(Sungero.Docflow.Server.ApprovalCheckingAssignmentArguments e)
    {
      var ruleStage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.SimpleAgr);
      if (ruleStage == null || ruleStage.Stage.AllowSendToRework != true)
        return;
      
      var stage = ruleStage.Stage;
      
      // Задать тему.
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      e.Block.Subject = Docflow.PublicFunctions.ApprovalRuleBase.FormatStageSubject(stage, document);
      
      // Задать исполнителей.
      e.Block.IsParallel = stage.Sequence == Docflow.ApprovalStage.Sequence.Parallel;
      var performers = Docflow.PublicFunctions.ApprovalStage.Remote.GetStagePerformers(_obj, stage);
      if (!performers.Any())
      {
        Functions.ApprovalTask.FillReworkReasonWhenAssigneeNotFound(_obj, stage);
        return;
      }
      foreach (var performer in performers)
        e.Block.Performers.Add(performer);
      
      // Задать результат выполнения, при котором остановятся все задания по блоку.
      if (stage.ReworkType == Sungero.Docflow.ApprovalStage.ReworkType.AfterEach)
        e.Block.StopResult = Docflow.ApprovalCheckingAssignment.Result.ForRework;

      // Срок.
      e.Block.RelativeDeadlineDays = stage.DeadlineInDays;
      e.Block.RelativeDeadlineHours = stage.DeadlineInHours;
      
      // Тема из этапа.
      e.Block.StageSubject = stage.Subject;
      
      // Выдать права на документы.
      var recipients = Docflow.PublicFunctions.ApprovalStage.Remote.GetStageRecipients(stage, _obj);
      Functions.ApprovalTask.GrantRightForAttachmentsToPerformers(_obj, recipients);
    }

    public virtual void StartAssignment31(Sungero.Docflow.IApprovalCheckingAssignment assignment, Sungero.Docflow.Server.ApprovalCheckingAssignmentArguments e)
    {
      assignment.StageNumber = _obj.StageNumber;
      assignment.ThreadSubject = e.Block.StageSubject;
      var stage = Functions.ApprovalTask.GetStage(_obj, Docflow.ApprovalStage.StageType.SimpleAgr);
      assignment.ReworkPerformer = Functions.ApprovalTask.GetReworkPerformer(ApprovalTasks.As(assignment.Task), stage.Stage);
    }

    public virtual void CompleteAssignment31(Sungero.Docflow.IApprovalCheckingAssignment assignment, Sungero.Docflow.Server.ApprovalCheckingAssignmentArguments e)
    {
      // Пробросить исполнителя доработки в задачу.
      if (assignment.Result == Sungero.Docflow.ApprovalCheckingAssignment.Result.ForRework)
        _obj.ReworkPerformer = assignment.ReworkPerformer;
    }

    public virtual void EndBlock31(Sungero.Docflow.Server.ApprovalCheckingAssignmentEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region Уведомление (блок 33)
    
    public virtual void StartBlock33(Sungero.Docflow.Server.ApprovalSimpleNotificationArguments e)
    {
      var ruleStage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Notice);
      if (ruleStage == null)
        return;
      
      var stage = ruleStage.Stage;
      
      // Задать тему.
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      e.Block.Subject = Docflow.PublicFunctions.ApprovalRuleBase.FormatStageSubject(stage, document);
      
      // Задать исполнителей.
      var performers = Docflow.PublicFunctions.ApprovalStage.Remote.GetStagePerformers(_obj, stage);
      
      // Если исполнитель один и предыдущее задание было ему же, то уведомление не посылать.
      if (performers.Count == 1)
      {
        var lastAssignment = Assignments.GetAll()
          .Where(a => Equals(a.Task, _obj))
          .OrderByDescending(a => a.Completed)
          .FirstOrDefault(a => a.Status == Workflow.AssignmentBase.Status.Completed);
        
        if (lastAssignment != null && Equals(lastAssignment.Performer, performers.First()))
          performers.Clear();
      }
      
      foreach (var performer in performers)
        e.Block.Performers.Add(performer);
      
      // Выдать права на документы.
      var recipients = Docflow.PublicFunctions.ApprovalStage.Remote.GetStageRecipients(stage, _obj);
      Functions.ApprovalTask.GrantRightForAttachmentsToPerformers(_obj, recipients);
    }

    public virtual void StartNotice33(Sungero.Docflow.IApprovalSimpleNotification notice, Sungero.Docflow.Server.ApprovalSimpleNotificationArguments e)
    {
      var ruleStage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Notice);
      if (ruleStage != null)
      {
        var stage = ruleStage.Stage;
        notice.ThreadSubject = stage.Subject;
      }
    }

    public virtual void EndBlock33(Sungero.Docflow.Server.ApprovalSimpleNotificationEndBlockEventArguments e)
    {
      
    }
    
    #endregion
    
    #region Установка статуса "На подписании у контрагента" (блок 34)
    
    public virtual void Script34Execute()
    {
      // TODO Котегов: Убрать копипасту по проверке наличия этапа согласования.
      var ruleStage = Functions.ApprovalTask.GetStage(_obj, Docflow.ApprovalStage.StageType.CheckReturn);
      if (ruleStage == null)
        return;
      
      if (!Functions.ApprovalTask.NeedControlReturn(_obj))
        return;
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      document.ExternalApprovalState = ExternalApprovalState.OnApproval;
    }
    
    #endregion
    
    #region Отсрочка отправки задания на контроль возврата (блок 29)

    public virtual void StartBlock29(Sungero.Workflow.Server.Route.MonitoringStartBlockEventArguments e)
    {
      // TODO Котегов: Убрать копипасту по проверке наличия этапа согласования.
      var ruleStage = Functions.ApprovalTask.GetStage(_obj, Docflow.ApprovalStage.StageType.CheckReturn);
      if (ruleStage == null)
        return;
      
      if (!Functions.ApprovalTask.NeedControlReturn(_obj))
        return;

      var startDelayInDays = ruleStage.Stage.StartDelayDays;
      if (startDelayInDays.HasValue && startDelayInDays != 0)
        _obj.ControlReturnStartDate = Calendar.Today.AddWorkingDays(startDelayInDays.Value);
    }
    
    public virtual bool Monitoring29Result()
    {
      var stages = Functions.ApprovalTask.GetStages(_obj).Stages;
      var stageRow = stages.Where(s => s.Stage.StageType == Sungero.Docflow.ApprovalStage.StageType.CheckReturn).FirstOrDefault();
      if (stageRow == null || stageRow.Number != _obj.StageNumber)
        return true;
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      
      // Если есть выдача от текущей задачи или выдачи, созданные ручной отправкой документа КА через сервис обмена.
      var needReturn = document.Tracking.Where(r => (Equals(r.ReturnTask, _obj) || (r.Action == Docflow.OfficialDocumentTracking.Action.Endorsement && r.ReturnTask == null))
                                               && r.ReturnDeadline != null && r.ReturnDate == null).Any();
      var returned = document.ExternalApprovalState == ExternalApprovalState.Signed ||
        document.ExternalApprovalState == ExternalApprovalState.Unsigned;

      if (!_obj.ControlReturnStartDate.HasValue || _obj.ControlReturnStartDate.Value <= Calendar.Today || !needReturn || returned)
        return true;

      return false;
    }
    
    #endregion
    
    #region Контроль возврата (блок 27)
    
    public virtual void StartBlock27(Sungero.Docflow.Server.ApprovalCheckReturnAssignmentArguments e)
    {
      // TODO Котегов: Убрать копипасту по проверке наличия этапа согласования.
      var ruleStage = Functions.ApprovalTask.GetStage(_obj, Docflow.ApprovalStage.StageType.CheckReturn);
      if (ruleStage == null)
        return;
      
      if (!Functions.ApprovalTask.NeedControlReturn(_obj))
        return;
      
      var stage = ruleStage.Stage;

      // Задать тему.
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      e.Block.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(ApprovalTasks.Resources.ControlReturnAsgSubject, document.Name);
      
      // Задать исполнителей.
      e.Block.IsParallel = true;
      var performers = Docflow.PublicFunctions.ApprovalStage.Remote.GetStagePerformers(_obj, stage);
      if (!performers.Any())
      {
        Functions.ApprovalTask.FillReworkReasonWhenAssigneeNotFound(_obj, stage);
        return;
      }
      foreach (var performer in performers)
        e.Block.Performers.Add(performer);
      
      // Срок.
      e.Block.RelativeDeadlineDays = (stage.StartDelayDays.HasValue && stage.StartDelayDays != 0) ?
        stage.DeadlineInDays - stage.StartDelayDays :
        stage.DeadlineInDays;
      
      // Выдать права на документы.
      Functions.ApprovalTask.GrantRightForAttachmentsToPerformers(_obj, e.Block.Performers.ToList());
      
      // Обновить сроки задачи.
      _obj.MaxDeadline = Functions.ApprovalTask.GetExpectedDate(_obj);
    }

    public virtual void StartAssignment27(Sungero.Docflow.IApprovalCheckReturnAssignment assignment, Sungero.Docflow.Server.ApprovalCheckReturnAssignmentArguments e)
    {
      assignment.StageNumber = _obj.StageNumber;
      var document = _obj.DocumentGroup.OfficialDocuments.First();

      if (document.ExternalApprovalState == ExternalApprovalState.Signed)
        assignment.Complete(Docflow.ApprovalCheckReturnAssignment.Result.Signed);
      if (document.ExternalApprovalState == ExternalApprovalState.Unsigned)
        assignment.Complete(Docflow.ApprovalCheckReturnAssignment.Result.NotSigned);

      // Выполнить задание, если документ уже вернули.
      if (document.Tracking.Where(r => Equals(r.ReturnTask, _obj) && Equals(r.Iteration, _obj.Iteration) &&
                                  r.ReturnDate == null && r.ReturnResult == null).Any())
      {
        var tracking = document.Tracking.Where(r => Equals(r.ReturnTask, _obj) && Equals(r.Iteration, _obj.Iteration) &&
                                               r.ReturnDate != null && r.ReturnResult != null).OrderByDescending(l => l.Id).FirstOrDefault();
        if (tracking != null)
        {
          if (Equals(tracking.ReturnResult, Docflow.OfficialDocumentTracking.ReturnResult.Signed))
            assignment.Complete(Docflow.ApprovalCheckReturnAssignment.Result.Signed);
          else if (Equals(tracking.ReturnResult, Docflow.OfficialDocumentTracking.ReturnResult.NotSigned))
            assignment.Complete(Docflow.ApprovalCheckReturnAssignment.Result.NotSigned);
        }
      }
    }

    public virtual void CompleteAssignment27(Sungero.Docflow.IApprovalCheckReturnAssignment assignment, Sungero.Docflow.Server.ApprovalCheckReturnAssignmentArguments e)
    {
      // TODO Котегов переделать через e.Block.StopResult после реализации 20912.
      var parallelAssignments = ApprovalCheckReturnAssignments.GetAll()
        .Where(a => Equals(a.Task, _obj))
        .Where(a => Equals(a.BlockUid, assignment.BlockUid))
        .Where(a => a.Status == Sungero.Workflow.AssignmentBase.Status.InProcess)
        .Where(a => !Equals(a, assignment))
        .Where(a => a.AutoReturned != true);
      foreach (var parallelAssignment in parallelAssignments)
      {
        if (!string.IsNullOrEmpty(parallelAssignment.ActiveText))
          parallelAssignment.ActiveText += Environment.NewLine;
        
        if (assignment.CompletedBy != null && !assignment.CompletedBy.Equals(assignment.Performer))
          parallelAssignment.ActiveText += ApprovalTasks.Resources.ControlReturnExecutedAnotherUserWithOnBehalfOfFormat(assignment.CompletedBy.Name, assignment.Performer.Name);
        else
          parallelAssignment.ActiveText += ApprovalTasks.Resources.ControlReturnExecutedAnotherUserFormat(assignment.Performer.Name);

        parallelAssignment.Abort();
      }
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      
      if (document.ExternalApprovalState == ExternalApprovalState.Signed ||
          document.ExternalApprovalState == ExternalApprovalState.Unsigned)
      {
        var items = Sungero.Workflow.SpecialFolders.GetInbox(assignment.Performer).Items;
        if (items.Contains(assignment))
          items.Remove(assignment);
      }
      
      #region Сменить статус "Согласование с КА" и обновить грид выдачи для документов, возвращенных вручную

      var isSigned = assignment.Result == Docflow.ApprovalCheckReturnAssignment.Result.Signed;
      var isNotSigned = assignment.Result == Docflow.ApprovalCheckReturnAssignment.Result.NotSigned;
      
      if (assignment.AutoReturned != true)
      {
        if (isSigned || isNotSigned)
        {
          document.ExternalApprovalState = isSigned ? ExternalApprovalState.Signed : ExternalApprovalState.Unsigned;
          var tracking = document.Tracking.Where(r => Equals(r.ReturnTask, _obj) && r.ReturnDeadline != null && r.ReturnDate == null);
          
          foreach (var row in tracking)
          {
            // Если комментарий - системный "на согласовании у контрагента", то очистить его, т.к. документ вернулся.
            if (row.Note == ApprovalTasks.Resources.CommentOnEndorsement)
              row.Note = null;
            
            row.ReturnResult = isSigned ?
              Docflow.OfficialDocumentTracking.ReturnResult.Signed :
              Docflow.OfficialDocumentTracking.ReturnResult.NotSigned;
            row.ReturnDate = Calendar.GetUserToday(assignment.Performer);
          }
        }
      }
      
      #endregion
      
      #region Уведомление о не подписании документа КА
      
      var author = _obj.Author;
      
      if (isNotSigned && assignment.AutoReturned != true)
      {
        // Получить всех, кто участвовал в согласовании и подписании.
        var performers = PublicFunctions.ApprovalTask.GetAllApproversAndSignatories(_obj);

        var threadSubject = ApprovalTasks.Resources.AdditionalTaskToAuthorSubject;
        var subject = string.Format(Sungero.Exchange.Resources.TaskSubjectTemplate, threadSubject, document.Name);

        // Отправка уведомлений.
        performers.Remove(assignment.Performer);
        if (performers.Any())
        {
          var notice = SimpleTasks.CreateAsSubtask(assignment);
          notice.Subject = subject;
          notice.ThreadSubject = threadSubject;
          
          // TODO: удалить код после исправления бага 17930 (сейчас этот баг в TFS недоступен, он про автоматическое обрезание темы).
          if (notice.Subject.Length > notice.Info.Properties.Subject.Length)
            notice.Subject = notice.Subject.Substring(0, notice.Info.Properties.Subject.Length);
          
          foreach (var recipient in performers)
          {
            var routeStep = notice.RouteSteps.AddNew();
            routeStep.AssignmentType = Workflow.SimpleTaskRouteSteps.AssignmentType.Notice;
            routeStep.Performer = recipient;
            routeStep.Deadline = null;
          }
          notice.Author = assignment.Performer;
          notice.Start();
        }
      }
      
      #endregion
    }

    public virtual void EndBlock27(Sungero.Docflow.Server.ApprovalCheckReturnAssignmentEndBlockEventArguments e)
    {
      
    }

    #endregion

    #region Подписан контрагентом? (блок 35)
    
    public virtual bool Decision35Result()
    {
      var assignment = ApprovalCheckReturnAssignments.GetAll(a => Equals(a.Task, _obj) && a.Created >= _obj.Started && a.Result != null)
        .OrderByDescending(a => a.Completed)
        .FirstOrDefault();
      return assignment.Result == Docflow.ApprovalCheckReturnAssignment.Result.Signed;
    }
    
    #endregion
    
    #region Действие после утверждения (блок 11)

    public virtual void Script11Execute()
    {
      // Обновить сроки задачи.
      _obj.MaxDeadline = Calendar.Now;
    }
    
    #endregion

    #region Согласование завершено? (блок 32)
    
    public virtual bool Decision32Result()
    {
      return _obj.StageNumber == null;
    }
    
    #endregion
    
    #region Контроль задачи (блок 2)
    
    public virtual void StartReviewAssignment2(Sungero.Workflow.IReviewAssignment reviewAssignment)
    {
      
    }
    
    public virtual void StartReviewAssignment2(Sungero.Docflow.IApprovalCheckingAssignment reviewAssignment)
    {
      
    }
    
    #endregion
    
  }
}
