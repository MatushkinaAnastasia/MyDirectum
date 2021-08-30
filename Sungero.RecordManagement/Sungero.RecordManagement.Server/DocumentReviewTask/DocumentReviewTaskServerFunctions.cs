using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DocumentReviewTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server
{
  partial class DocumentReviewTaskFunctions
  {
    #region Контрол "Состояние"

    /// <summary>
    /// Построить модель состояния рассмотрения.
    /// </summary>
    /// <returns>Схема модели состояния.</returns>
    [Public, Remote(IsPure = true)]
    public string GetStateViewXml()
    {
      return this.GetStateView().ToString();
    }
    
    /// <summary>
    /// Построить модель состояния задачи на рассмотрение документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Контрол состояния.</returns>    
    public Sungero.Core.StateView GetStateView(Sungero.Docflow.IOfficialDocument document)
    {
      if (_obj.DocumentForReviewGroup.OfficialDocuments.Any(d => Equals(document, d)))
        return this.GetStateView();
      else
        return StateView.Create();
    }
    
    /// <summary>
    /// Построить модель состояния рассмотрения.
    /// </summary>
    /// <returns>Схема модели состояния.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      var stateView = StateView.Create();

      var comment = Docflow.PublicFunctions.Module.GetTaskUserComment(_obj, Resources.ConsiderationText);
      
      if (_obj.Status != Workflow.Task.Status.Aborted)
        if (_obj.Started.HasValue)
          Docflow.PublicFunctions.OfficialDocument
            .AddUserActionBlock(stateView, _obj.Author, DocumentReviewTasks.Resources.StateViewDocumentSent, _obj.Started.Value, _obj, comment, _obj.StartedBy);
        else
          Docflow.PublicFunctions.OfficialDocument
            .AddUserActionBlock(stateView, _obj.Author, Docflow.ApprovalTasks.Resources.StateViewTaskDrawCreated, _obj.Created.Value, _obj, comment, _obj.Author);
      
      var startDate = this.GetIterationStartDate();
      var managerBlock = this.AddReviewManagerBlock(stateView, startDate);
      if (managerBlock != null)
      {
        this.AddPreraringDraftResolutionBlock(managerBlock, startDate);
        this.AddReviewResolutionBlock(managerBlock, startDate);
      }
      return stateView;
    }
    
    /// <summary>
    /// Добавить статус выполнения задания.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="style">Стиль.</param>
    /// <param name="assignment">Задание.</param>
    private void AddAssignmentStatusInfoToRight(StateBlock block, Sungero.Core.StateBlockLabelStyle style, IAssignment assignment)
    {
      // Добавить колонку справа, если всего одна колонка (main).
      var rightContent = block.Contents.LastOrDefault();
      if (block.Contents.Count() <= 1)
        rightContent = block.AddContent();
      else
        rightContent.AddLineBreak();

      rightContent.AddLabel(Assignments.Info.Properties.Status.GetLocalizedValue(assignment.Status), style);
    }
    
    /// <summary>
    /// Добавить блок информации о рассмотрении документа руководителем.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <param name="startDate">Дата начала текущей итерации рассмотрения.</param>
    /// <returns>Полученный блок.</returns>
    private StateBlock AddReviewManagerBlock(StateView stateView, DateTime startDate)
    {
      var managerAssignment = this.GetManagerAssignment(startDate);
      var resolutionAssignment = this.GetPreparingDraftResolutionAssignment(startDate);
      
      var author = managerAssignment != null ?
        Docflow.PublicFunctions.OfficialDocument.GetAuthor(managerAssignment.Performer, managerAssignment.CompletedBy) :
        Docflow.PublicFunctions.OfficialDocument.GetAuthor(_obj.Addressee, _obj.Addressee);
      var actionItems = ActionItemExecutionTasks.GetAll()
        .Where(t => (t.ParentAssignment != null && (Equals(t.ParentAssignment.Task, _obj) || Equals(t.ParentAssignment, managerAssignment))) &&
               t.Status != Workflow.Task.Status.Draft &&
               Equals(t.AssignedBy, DocumentReviewTasks.As(_obj).Addressee))
        .OrderBy(t => t.Started);
      var isCompleted = (managerAssignment != null && managerAssignment.Status == Workflow.AssignmentBase.Status.Completed) ||
        (resolutionAssignment != null && resolutionAssignment.Result == RecordManagement.PreparingDraftResolutionAssignment.Result.AddAssignment);
      var isReworkresolution = managerAssignment != null && ReviewDraftResolutionAssignments.Is(managerAssignment) &&
        managerAssignment.Result == RecordManagement.ReviewDraftResolutionAssignment.Result.AddResolution &&
        !(resolutionAssignment != null && resolutionAssignment.Result == RecordManagement.PreparingDraftResolutionAssignment.Result.AddAssignment);
      var isDraft = _obj.Status == Workflow.Task.Status.Draft;
      
      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle(isDraft);
      var performerStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle(isDraft);
      var labelStyle = Docflow.PublicFunctions.Module.CreateStyle(false, isDraft, false);
      var separatorStyle = Docflow.PublicFunctions.Module.CreateSeparatorStyle();
      
      // Добавить блок. Установить иконку и сущность.
      var block = stateView.AddBlock();
      block.Entity = _obj;
      if (isCompleted && !isReworkresolution)
        block.AssignIcon(ReviewManagerAssignments.Info.Actions.AddResolution, StateBlockIconSize.Large);
      else
        block.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Large);

      // Рассмотрение руководителем ещё в работе.
      if (!isCompleted || isReworkresolution)
      {
        // Добавить заголовок.
        block.AddLabel(Docflow.Resources.StateViewDocumentReview, headerStyle);
        block.AddLineBreak();
        if (managerAssignment != null && !isReworkresolution)
        {
          if (managerAssignment.Status == Workflow.AssignmentBase.Status.Aborted)
            Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Resources.StateViewAborted);
          else if (managerAssignment.IsRead == false)
            Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Resources.StateViewUnRead);
          else
            this.AddAssignmentStatusInfoToRight(block, labelStyle, managerAssignment);
        }
        else if (_obj.Status == Workflow.Task.Status.Completed)
        {
          Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Resources.StateViewCompleted);
        }
        else if (_obj.Status == Workflow.Task.Status.Aborted)
        {
          Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Resources.StateViewAborted);
        }
        
        // Адресат.
        block.AddLabel(string.Format("{0}: {1}",
                                     Docflow.Resources.StateViewAddressee,
                                     Company.PublicFunctions.Employee.GetShortName(_obj.Addressee, false)), performerStyle);

        var deadline = managerAssignment != null && !isReworkresolution ?
          managerAssignment.Deadline : _obj.MaxDeadline;
        var deadlineString = deadline.HasValue ?
          Docflow.PublicFunctions.Module.ToShortDateShortTime(deadline.Value.ToUserTime()) :
          Docflow.OfficialDocuments.Resources.StateViewWithoutTerm;

        block.AddLabel(string.Format("{0}: {1}", Docflow.OfficialDocuments.Resources.StateViewDeadline, deadlineString),
                       performerStyle);
        
        if (!isReworkresolution && managerAssignment != null && managerAssignment.Deadline.HasValue)
          Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(block, managerAssignment.Deadline.Value, managerAssignment.Performer);
        else if (resolutionAssignment != null && resolutionAssignment.Deadline.HasValue)
          Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(block, resolutionAssignment.Deadline.Value, resolutionAssignment.Performer);
      }
      else if (managerAssignment != null || resolutionAssignment != null)
      {
        // Рассмотрение завершено.
        // Добавить заголовок.
        var completionDate = managerAssignment == null ? resolutionAssignment.Completed.Value.ToUserTime() : managerAssignment.Completed.Value.ToUserTime();
        var resolutionDate = Docflow.PublicFunctions.Module.ToShortDateShortTime(completionDate);
        block.AddLabel(Docflow.Resources.StateViewResolution, headerStyle);
        block.AddLineBreak();
        block.AddLabel(string.Format("{0}: {1} {2}: {3}",
                                     DocumentReviewTasks.Resources.StateViewAuthor,
                                     author,
                                     Docflow.OfficialDocuments.Resources.StateViewDate,
                                     resolutionDate), performerStyle);
        block.AddLineBreak();
        block.AddLabel(Docflow.Constants.Module.SeparatorText, separatorStyle);
        block.AddLineBreak();
        block.AddEmptyLine(Docflow.Constants.Module.EmptyLineMargin);
        
        // Если поручения не созданы, или рассмотрение выполнено с результатом "Вынести резолюцию" или "Принято к сведению" и помощник сам не отправлял поручения в работу.
        // В старых задачах поручение и рассмотрение не связаны, поэтому обрабатываем такие случаи как резолюцию.
        if (!actionItems.Any() || (managerAssignment != null && managerAssignment.Result != RecordManagement.ReviewManagerAssignment.Result.AddAssignment &&
                                   managerAssignment.Result != RecordManagement.ReviewDraftResolutionAssignment.Result.ForExecution &&
                                   !(resolutionAssignment != null && resolutionAssignment.Result == RecordManagement.PreparingDraftResolutionAssignment.Result.AddAssignment)))
        {
          var comment = resolutionAssignment != null && resolutionAssignment.Result == RecordManagement.PreparingDraftResolutionAssignment.Result.AddAssignment ?
            Docflow.PublicFunctions.Module.GetFormatedUserText(resolutionAssignment.Texts.Last().Body) :
            Docflow.PublicFunctions.Module.GetFormatedUserText(managerAssignment.Texts.Last().Body);
          block.AddLabel(comment);
          block.AddLineBreak();
        }
        else
        {
          // Добавить информацию по каждому поручению.
          foreach (var actionItem in actionItems)
          {
            if (actionItem.IsCompoundActionItem == true)
            {
              foreach (var item in actionItem.ActionItemParts)
              {
                if (item.ActionItemPartExecutionTask != null)
                  Functions.ActionItemExecutionTask.AddActionItemInfo(block, item.ActionItemPartExecutionTask, author);
              }
            }
            else
            {
              Functions.ActionItemExecutionTask.AddActionItemInfo(block, actionItem, author);
            }
          }
        }
      }
      return block;
    }
    
    /// <summary>
    /// Добавить блок информации о создании поручения по резолюции.
    /// </summary>
    /// <param name="parentBlock">Основной блок.</param>
    /// <param name="startDate">Дата начала текущей итерации рассмотрения.</param>
    private void AddReviewResolutionBlock(StateBlock parentBlock, DateTime startDate)
    {
      var resolutionAssignment = ReviewResolutionAssignments.GetAll()
        .Where(a => Equals(a.Task, _obj) && a.Created >= startDate)
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
      
      this.AddAssignmentBlock(parentBlock, resolutionAssignment, DocumentReviewTasks.Resources.StateViewSendActionItemOnResolution, string.Empty);
    }
    
    /// <summary>
    /// Добавить блок информации о подготовке проекта резолюции.
    /// </summary>
    /// <param name="parentBlock">Основной блок.</param>
    /// <param name="startDate">Дата начала текущей итерации рассмотрения.</param>
    private void AddPreraringDraftResolutionBlock(StateBlock parentBlock, DateTime startDate)
    {
      var resolutionAssignment = this.GetPreparingDraftResolutionAssignment(startDate);
      
      var result = string.Empty;
      if (this.GetManagerAssignment(startDate) == null && resolutionAssignment != null &&
          resolutionAssignment.Status == Workflow.AssignmentBase.Status.Completed &&
          resolutionAssignment.Result != RecordManagement.PreparingDraftResolutionAssignment.Result.AddAssignment)
        result = Docflow.PublicFunctions.Module.GetFormatedUserText(resolutionAssignment.Texts.Last().Body);
      
      this.AddAssignmentBlock(parentBlock, resolutionAssignment, DocumentReviewTasks.Resources.PreparingDraftResolution, result);
    }
    
    /// <summary>
    /// Получить дату начала текущей итерации рассмотрения.
    /// </summary>
    /// <returns>Дата начала текущей итерации рассмотрения.</returns>
    private DateTime GetIterationStartDate()
    {
      if (!_obj.Started.HasValue)
        return _obj.Created.Value;
      
      var startDate = _obj.Started.Value;
      var lastForwardedAsg = Assignments.GetAll()
        .Where(a => Equals(a.Task, _obj))
        .Where(a => a.Created >= startDate)
        .Where(a => a.Status == Workflow.AssignmentBase.Status.Completed)
        .Where(a => a.Result == RecordManagement.ReviewManagerAssignment.Result.Forward)
        .OrderByDescending(a => a.Completed.Value)
        .FirstOrDefault();
      
      if (lastForwardedAsg != null)
        startDate = lastForwardedAsg.Completed.Value;
      
      return startDate;
    }
    
    /// <summary>
    /// Получить задание руководителю.
    /// </summary>
    /// <param name="startDate">Дата начала текущей итерации рассмотрения.</param>
    /// <returns>Задание руководителю.</returns>
    private IAssignment GetManagerAssignment(DateTime startDate)
    {
      return Assignments.GetAll()
        .Where(a => Equals(a.Task, _obj))
        .Where(a => a.Created >= startDate)
        .Where(a => ReviewManagerAssignments.Is(a) || ReviewDraftResolutionAssignments.Is(a))
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Получить задание на подготовку проекта резолюции.
    /// </summary>
    /// <param name="startDate">Дата начала текущей итерации рассмотрения.</param>
    /// <returns>Задание на подготовку проекта резолюции.</returns>
    private IAssignment GetPreparingDraftResolutionAssignment(DateTime startDate)
    {
      return Assignments.GetAll()
        .Where(a => Equals(a.Task, _obj))
        .Where(a => a.Created >= startDate)
        .Where(a => PreparingDraftResolutionAssignments.Is(a))
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Добавить блок с заданием.
    /// </summary>
    /// <param name="parentBlock">Основной блок.</param>
    /// <param name="assignment">Задание.</param>
    /// <param name="header">Заголовок блока.</param>
    /// <param name="result">Результат выполнения задания.</param>
    private void AddAssignmentBlock(StateBlock parentBlock, IAssignment assignment, string header, string result)
    {
      if (assignment != null && (assignment.Status != Workflow.AssignmentBase.Status.Completed || !string.IsNullOrEmpty(result)))
      {
        var isDraft = _obj.Status == Workflow.Task.Status.Draft;
        var labelStyle = Docflow.PublicFunctions.Module.CreateStyle(false, isDraft, false);
        
        parentBlock.IsExpanded = true;
        var block = parentBlock.AddChildBlock();
        
        block.Entity = assignment;
        block.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Large);
        
        if (assignment.Status == Workflow.AssignmentBase.Status.InProcess && assignment.IsRead == false)
          Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Resources.StateViewUnRead);
        else
          this.AddAssignmentStatusInfoToRight(block, labelStyle, assignment);

        var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle(isDraft);
        var performerStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle(isDraft);
        
        block.AddLabel(header, headerStyle);
        block.AddLineBreak();

        var resolutionPerformerName = Employees.Is(assignment.Performer) ?
          Company.PublicFunctions.Employee.GetShortName(Employees.As(assignment.Performer), false) :
          assignment.Performer.Name;
        block.AddLabel(string.Format("{0}: {1} {2}: {3}",
                                     Docflow.OfficialDocuments.Resources.StateViewTo,
                                     resolutionPerformerName,
                                     Docflow.OfficialDocuments.Resources.StateViewDeadline,
                                     Docflow.PublicFunctions.Module.ToShortDateShortTime(assignment.Deadline.Value.ToUserTime())), performerStyle);
        
        if (!string.IsNullOrEmpty(result))
        {
          var separatorStyle = Docflow.PublicFunctions.Module.CreateSeparatorStyle();
          
          block.AddLineBreak();
          block.AddLabel(Docflow.Constants.Module.SeparatorText, separatorStyle);
          block.AddLineBreak();
          block.AddEmptyLine(Docflow.Constants.Module.EmptyLineMargin);
          block.AddLabel(result);
        }
        else
        {
          Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(block, assignment.Deadline.Value, assignment.Performer);
        }
      }
    }
    
    #endregion
    
    /// <summary>
    /// Получить результат выполнения задания руководителю с последней итерации.
    /// </summary>
    /// <param name="task">Задача "рассмотрение входящего".</param>
    /// <returns>Результат задания руководителю.</returns>
    public static Enumeration? GetLastAssignmentResult(IDocumentReviewTask task)
    {
      var lastAssignments = Assignments.GetAll(c => Equals(c.Task, task) && c.Status == Sungero.Workflow.Assignment.Status.Completed)
        .OrderByDescending(c => c.Completed);
      if (!lastAssignments.Any())
        return null;
      else
        return lastAssignments.First().Result.Value;
    }
    
    /// <summary>
    /// Выдать права на вложения, не выше прав инициатора задачи.
    /// </summary>
    /// <param name="assignees">Исполнители.</param>
    public void GrantRightForAttachmentsToAssignees(List<IRecipient> assignees)
    {
      foreach (var assignee in assignees)
      {
        // На основной документ - на изменение.
        _obj.DocumentForReviewGroup.OfficialDocuments.First().AccessRights.Grant(assignee, DefaultAccessRightsTypes.Change);
        
        // На приложения - на изменение, но не выше, чем у инициатора.
        foreach (var document in _obj.AddendaGroup.All)
        {
          var rightType = document.AccessRights.CanUpdate(_obj.Author) ? DefaultAccessRightsTypes.Change : DefaultAccessRightsTypes.Read;
          document.AccessRights.Grant(assignee, rightType);
        }
      }

      // Дополнительно обновляем права наблюдателей.
      Docflow.PublicFunctions.Module.GrantReadRightsForAttachments(_obj.AddendaGroup.All.ToList(), _obj.ResolutionObservers.Select(o => o.Observer));
    }
    
    /// <summary>
    /// Получить нестандартных исполнителей задачи.
    /// </summary>
    /// <returns>Исполнители.</returns>
    public virtual List<IRecipient> GetTaskAdditionalAssignees()
    {
      var assignees = new List<IRecipient>();

      var documentReview = DocumentReviewTasks.As(_obj);
      if (documentReview == null)
        return assignees;
      
      var recipient = documentReview.Addressee;
      if (recipient != null)
      {
        assignees.Add(recipient);
        var secretary = Docflow.PublicFunctions.Module.GetSecretary(recipient);
        var document = documentReview.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
        secretary = secretary ?? Docflow.PublicFunctions.Module.Remote.GetClerk(document);
        assignees.Add(secretary ?? documentReview.Author);
      }
      
      assignees.AddRange(documentReview.ResolutionObservers.Where(o => o.Observer != null).Select(o => o.Observer));
      
      return assignees.Distinct().ToList();
    }
    
    /// <summary>
    /// Обновить адресата после переадресации.
    /// </summary>
    /// <param name="newAddressee">Новый адресат.</param>
    public void UpdateReviewTaskAfterForward(IEmployee newAddressee)
    {
      _obj.Addressee = newAddressee;
    }
    
    /// <summary>
    /// Получить делопроизводителя для отправки поручений.
    /// </summary>
    /// <returns>Исполнитель задания по отправке поручения.</returns>
    public IUser GetClerkToSendActionItem()
    {
      var author = _obj.Author;
      var addressee = Employees.As(_obj.Addressee);
      var secretary = Employees.Null;
      
      // Личный секретарь адресата (руководителя).
      if (addressee != null)
        secretary = Docflow.PublicFunctions.Module.GetSecretary(addressee);
      
      // Ответственный за группу регистрации, либо инициатор.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      if (secretary == null && document.DocumentKind.NumberingType == Docflow.DocumentKind.NumberingType.Registrable)
        secretary = Docflow.PublicFunctions.Module.Remote.GetClerk(document);
      
      return secretary ?? author;
    }

    /// <summary>
    /// Отправить проект резолюции на исполнение.
    /// </summary>
    /// <param name="parentAssignment">Задание на рассмотрение.</param>
    [Remote, Public]
    public void StartActionItemsForDraftResolution(IAssignment parentAssignment)
    {
      parentAssignment.Save();
      // TODO Shklyaev: переделать метод, когда сделают 65004.
      foreach (var draftResolution in _obj.ResolutionGroup.ActionItemExecutionTasks.Where(t => t.Status == RecordManagement.ActionItemExecutionTask.Status.Draft))
      {
        // Очистить все вложения и заполнить заново, чтобы корректно отработала синхронизация вновь добавленных документов.
        var officialDocuments = draftResolution.DocumentsGroup.OfficialDocuments.ToList();
        draftResolution.DocumentsGroup.OfficialDocuments.Clear();
        
        var addendaDocuments = draftResolution.AddendaGroup.OfficialDocuments.ToList();
        draftResolution.AddendaGroup.OfficialDocuments.Clear();
        
        var othersGroup = draftResolution.OtherGroup.All.ToList();
        draftResolution.OtherGroup.All.Clear();
        
        ((Sungero.Workflow.IInternalTask)draftResolution).ParentAssignment = parentAssignment;
        ((Sungero.Workflow.IInternalTask)draftResolution).MainTask = parentAssignment.MainTask;
        draftResolution.Save();
        
        foreach (var attachment in othersGroup)
        {
          draftResolution.OtherGroup.All.Add(attachment);
          
          var participants = Sungero.Docflow.PublicFunctions.Module.Remote.GetTaskAssignees(draftResolution).ToList();
          foreach (var participant in participants)
            attachment.AccessRights.Grant(participant, DefaultAccessRightsTypes.Read);
          attachment.AccessRights.Save();
        }
        
        foreach (var attachment in officialDocuments)
          draftResolution.DocumentsGroup.OfficialDocuments.Add(attachment);
        
        foreach (var attachment in addendaDocuments)
          draftResolution.AddendaGroup.OfficialDocuments.Add(attachment);
        
        draftResolution.Save();
        ((Domain.Shared.IExtendedEntity)draftResolution).Params[PublicConstants.ActionItemExecutionTask.CheckDeadline] = true;
        draftResolution.Start();
      }
    }
  }
}