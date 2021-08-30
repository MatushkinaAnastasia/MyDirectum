using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sungero.Commons;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.CoreEntities.RelationType;
using Sungero.Docflow;
using Sungero.Docflow.ApprovalStage;
using Sungero.Docflow.DocumentKind;
using Sungero.Docflow.OfficialDocument;
using Sungero.Domain;
using Sungero.Domain.Shared;
using Sungero.Workflow;
using Init = Sungero.RecordManagement.Constants.Module.Initialize;

namespace Sungero.RecordManagement.Server
{

  public class ModuleFunctions
  {
    
    #region Виджеты
    
    #region Виджет "Поручения"

    /// <summary>
    /// Выбрать поручения для виджета.
    /// </summary>
    /// <param name="onlyOverdue">Только просроченные.</param>
    /// <param name="substitution">Включать замещающих.</param>
    /// <returns>Список поручений.</returns>
    public IQueryable<Sungero.RecordManagement.IActionItemExecutionTask> GetActionItemsToWidgets(bool onlyOverdue, bool substitution)
    {
      var tasks = ActionItemExecutionTasks.GetAll()
        .Where(t => t.Status == Workflow.AssignmentBase.Status.InProcess);

      if (onlyOverdue)
        tasks = tasks.Where(t => t.Deadline.HasValue &&
                            (!t.Deadline.Value.HasTime() && t.Deadline.Value < Calendar.UserToday ||
                             t.Deadline.Value.HasTime() && t.Deadline.Value < Calendar.Now));

      var users = substitution ? Substitutions.ActiveSubstitutedUsersWithoutSystem.ToList() : new List<IUser>();
      users.Add(Users.Current);

      return tasks.Where(a => users.Contains(a.Supervisor));
    }

    #endregion

    #region "Динамика исполнения поручений в срок"

    /// <summary>
    /// Получить статистику по исполнению поручений.
    /// </summary>
    /// <param name="performer">Исполнитель, указанный в параметрах виджета.</param>
    /// <returns>Строка с результатом.</returns>
    public List<Structures.Module.ActionItemStatistic> GetActionItemCompletionStatisticForChart(Enumeration performer)
    {
      var periodBegin = Calendar.UserToday.AddMonths(-2).BeginningOfMonth();
      var periodEnd = Calendar.UserToday.EndOfMonth();
      
      var hasData = false;

      var author = Employees.Null;
      if (performer == RecordManagement.Widgets.ActionItemCompletionGraph.Performer.Author)
        author = Company.Employees.Current;

      var statistic = new List<Structures.Module.ActionItemStatistic>();

      var actionItems = Functions.Module.GetActionItemCompletionData(null, null, periodBegin, periodEnd, author, null, null, null, null, false, false);
      while (periodBegin <= Calendar.UserToday)
      {
        periodEnd = periodBegin.EndOfMonth();
        var currentStatistic = this.CalculateActionItemStatistic(actionItems, periodBegin, periodEnd);
        
        if (currentStatistic != null)
          hasData = true;
        
        statistic.Add(Structures.Module.ActionItemStatistic.Create(currentStatistic, periodBegin));
        
        periodBegin = periodBegin.AddMonths(1);
      }
      
      return hasData ? statistic : new List<Structures.Module.ActionItemStatistic>();
    }

    /// <summary>
    /// Получить статистику по исполнению поручений за месяц.
    /// </summary>
    /// <param name="actionItems">Список поручений.</param>
    /// <param name="beginDate">Начало периода.</param>
    /// <param name="endDate">Конец периода.</param>
    /// <returns>Статистика за период.</returns>
    private int? CalculateActionItemStatistic(List<Structures.Module.LightActiomItem> actionItems, DateTime beginDate, DateTime endDate)
    {
      var serverBeginDate = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(beginDate);
      var serverEndDate = endDate.EndOfDay().FromUserTime();
      actionItems = actionItems.Where(t => t.Status == Sungero.Workflow.Task.Status.Completed &&
                                      (Calendar.Between(t.ActualDate.Value.Date, beginDate.Date, endDate.Date) ||
                                       (t.Deadline.Value.Date == t.Deadline.Value ? t.Deadline.Between(beginDate.Date, endDate.Date) : t.Deadline.Between(serverBeginDate, serverEndDate)) ||
                                       t.ActualDate.Value.Date >= endDate && (t.Deadline.Value.Date == t.Deadline.Value ? t.Deadline <= beginDate.Date : t.Deadline <= serverBeginDate)) ||
                                      t.Status == Sungero.Workflow.Task.Status.InProcess &&
                                      (t.Deadline.Value.Date == t.Deadline.Value ? t.Deadline <= endDate.Date : t.Deadline <= serverEndDate)).ToList();

      var totalCount = actionItems.Count;
      if (totalCount == 0)
        return null;
      
      var completedInTime = actionItems
        .Where(j => j.Status == Workflow.Task.Status.Completed)
        .Where(j => Docflow.PublicFunctions.Module.CalculateDelay(j.Deadline, j.ActualDate.Value, j.Assignee) == 0).Count();
      
      var inProcess = actionItems.Where(j => j.Status == Workflow.Task.Status.InProcess).Count();
      var inProcessOverdue = actionItems
        .Where(j => j.Status == Workflow.Task.Status.InProcess)
        .Where(j => Docflow.PublicFunctions.Module.CalculateDelay(j.Deadline, Calendar.Now, j.Assignee) > 0).Count();

      int currentStatistic = 0;
      int.TryParse(Math.Round(totalCount == 0 ? 0 : ((completedInTime + inProcess - inProcessOverdue) * 100.00) / (double)totalCount).ToString(),
                   out currentStatistic);

      return currentStatistic;
    }
    
    /// <summary>
    /// Получить сокращенные ФИО соисполнителей.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <returns>Список сокращенных ФИО соисполнителей.</returns>
    private List<string> GetCoAssigneesShortNames(IActionItemExecutionTask task)
    {
      return task.CoAssignees.Select(ca => ca.Assignee.Person.ShortName).ToList();
    }
    
    /// <summary>
    /// Получить краткую информацию по исполнению поручений в срок за период.
    /// </summary>
    /// <param name="beginDate">Начало периода.</param>
    /// <param name="endDate">Конец периода.</param>
    /// <param name="author">Автор.</param>
    /// <returns>Краткая информация по исполнению поручений в срок за период.</returns>
    [Remote]
    public virtual List<Structures.Module.LightActiomItem> GetActionItemCompletionData(DateTime? beginDate,
                                                                                       DateTime? endDate,
                                                                                       IEmployee author)
    {
      return this.GetActionItemCompletionData(null, null, beginDate, endDate, author, null, null, null, null, false, false);
    }
    
    /// <summary>
    /// Получить краткую информацию по исполнению поручений в срок за период.
    /// </summary>
    /// <param name="meeting">Совещание.</param>
    /// <param name="document">Документ.</param>
    /// <param name="beginDate">Начало периода.</param>
    /// <param name="endDate">Конец периода.</param>
    /// <param name="author">Автор.</param>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="department">Подразделение.</param>
    /// <param name="performer">Исполнитель.</param>
    /// <param name="documentType">Тип документов во вложениях поручений.</param>
    /// <param name="isMeetingsCoverContext">Признак контекста вызова с обложки совещаний.</param>
    /// <param name="getCoAssignees">Признак необходимости получения соисполнителей.</param>
    /// <returns>Краткая информация по исполнению поручений в срок за период.</returns>
    public virtual List<Structures.Module.LightActiomItem> GetActionItemCompletionData(Meetings.IMeeting meeting,
                                                                                       IOfficialDocument document,
                                                                                       DateTime? beginDate,
                                                                                       DateTime? endDate,
                                                                                       IEmployee author,
                                                                                       IBusinessUnit businessUnit,
                                                                                       IDepartment department,
                                                                                       IUser performer,
                                                                                       IDocumentType documentType,
                                                                                       bool? isMeetingsCoverContext,
                                                                                       bool getCoAssignees)
    {
      List<Structures.Module.LightActiomItem> tasks = null;
      
      var isAdministratorOrAdvisor = Sungero.Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor();
      var recipientsIds = Substitutions.ActiveSubstitutedUsers.Select(u => u.Id).ToList();
      recipientsIds.Add(Users.Current.Id);
      
      AccessRights.AllowRead(
        () =>
        {
          var query = ActionItemExecutionTasks.GetAll()
            .Where(t => isAdministratorOrAdvisor ||
                   recipientsIds.Contains(t.Author.Id) || recipientsIds.Contains(t.StartedBy.Id) ||
                   t.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Component &&
                   recipientsIds.Contains(t.MainTask.StartedBy.Id))
            .Where(t => t.Status == Sungero.Workflow.Task.Status.Completed || t.Status == Sungero.Workflow.Task.Status.InProcess)
            .Where(t => t.IsCompoundActionItem != true && t.ActionItemType != RecordManagement.ActionItemExecutionTask.ActionItemType.Additional);
          
          // Если отчёт вызывается не из документа (свойство Документ не заполнено), то даты заполнены и по ним нужно фильтровать.
          // Если же отчёт вызывается из документа, то поручения нужно фильтровать по этому документу во вложении.
          // Если отчет вызывается из Совещания, то поручения нужно фильтровать по протоколам этого совещания.
          
          // Guid группы вложений для документа в поручении.
          var documentsGroupGuid = Docflow.PublicConstants.Module.TaskMainGroup.ActionItemExecutionTask;
          
          if (documentType != null)
          {
            var documents = OfficialDocuments.GetAll(d => d.DocumentKind.DocumentType == documentType);
            
            // В Hibernate обращаться к группам вложений задачи можно только через метаданные.
            query = query.Where(t => t.AttachmentDetails.Any(g => g.GroupId == documentsGroupGuid && documents.Any(m => m.Id == g.AttachmentId)));
          }
          
          if (meeting != null && isMeetingsCoverContext != true)
          {
            var minutesList = Meetings.Minuteses.GetAll(d => Equals(d.Meeting, meeting));
            
            // В Hibernate обращаться к группам вложений задачи можно только через метаданные.
            query = query.Where(t => t.AttachmentDetails.Any(g => g.GroupId == documentsGroupGuid && minutesList.Any(m => m.Id == g.AttachmentId)));
          }
          else if (document != null)
          {
            // В Hibernate обращаться к группам вложений задачи можно только через метаданные.
            query = query.Where(t => t.AttachmentDetails.Any(g => g.GroupId == documentsGroupGuid && g.AttachmentId == document.Id));
          }
          else
          {
            var serverBeginDate = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(beginDate.Value);
            var serverEndDate = endDate.Value.EndOfDay().FromUserTime();
            query = query.Where(t => t.Status == Sungero.Workflow.Task.Status.Completed &&
                                (Calendar.Between(t.ActualDate.Value.Date, beginDate.Value.Date, endDate.Value.Date) ||
                                 (t.Deadline.Value.Date == t.Deadline.Value ? t.Deadline.Between(beginDate.Value.Date, endDate.Value.Date) : t.Deadline.Between(serverBeginDate, serverEndDate)) ||
                                 t.ActualDate.Value.Date >= endDate && (t.Deadline.Value.Date == t.Deadline.Value ? t.Deadline <= beginDate.Value.Date : t.Deadline <= serverBeginDate)) ||
                                t.Status == Sungero.Workflow.Task.Status.InProcess &&
                                (t.Deadline.Value.Date == t.Deadline.Value ? t.Deadline <= endDate.Value.Date : t.Deadline <= serverEndDate));
          }
          
          if (isMeetingsCoverContext == true)
          {
            var minutesList = meeting == null ?
              Meetings.Minuteses.GetAll(d => d.Meeting != null) :
              Meetings.Minuteses.GetAll(d => Equals(d.Meeting, meeting));
            
            query = query.Where(t => t.AttachmentDetails.Any(g => g.GroupId == documentsGroupGuid && minutesList.Any(m => m.Id == g.AttachmentId)));
          }
          
          // Dmitriev_IA: Проверка вынесена из Select для ускорения получения данных. Если занести проверку в Select, то проверка будет происходить для каждого t.
          if (getCoAssignees)
            tasks = query
              .Select(t => Structures.Module.LightActiomItem.Create(t.Id, t.Status, t.ActualDate, t.Deadline, t.Author, t.Assignee, t.ActionItem, t.ExecutionState, this.GetCoAssigneesShortNames(t)))
              .ToList();
          else
            tasks = query
              .Select(t => Structures.Module.LightActiomItem.Create(t.Id, t.Status, t.ActualDate, t.Deadline, t.Author, t.Assignee, t.ActionItem, t.ExecutionState, null))
              .ToList();
        });
      
      if (author != null)
        tasks = tasks.Where(t => Equals(t.Author, author))
          .ToList();
      
      if (businessUnit != null)
        tasks = tasks.Where(t => t.Assignee != null && t.Assignee.Department != null && t.Assignee.Department.BusinessUnit != null &&
                            Equals(t.Assignee.Department.BusinessUnit, businessUnit))
          .ToList();
      
      if (department != null)
        tasks = tasks.Where(t => t.Assignee != null && t.Assignee.Department != null &&
                            Equals(t.Assignee.Department, department))
          .ToList();
      
      if (performer != null)
        tasks = tasks.Where(t => Equals(t.Assignee, performer))
          .ToList();
      
      return tasks;
    }
    
    /// <summary>
    /// Признак того, что для совещания и/или документа были поручения, выполненные в срок.
    /// </summary>
    /// <param name="meeting">Совещание.</param>
    /// <param name="document">Документ.</param>
    /// <returns>True, если были поручения, выполненные в срок, False в противном случае.</returns>
    [Public, Remote]
    public bool ActionItemCompletionDataIsPresent(Meetings.IMeeting meeting, IOfficialDocument document)
    {
      return this.GetActionItemCompletionData(meeting, document, null, null, null, null, null, null, null, false, true).Any();
    }
    
    #endregion

    #endregion

    #region Типы задач

    /// <summary>
    /// Создать задачу по процессу "Рассмотрение входящего".
    /// </summary>
    /// <param name="document">Документ на рассмотрение.</param>
    /// <returns>Задача по процессу "Рассмотрение входящего".</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public static ITask CreateDocumentReview(Sungero.Docflow.IOfficialDocument document)
    {
      var task = DocumentReviewTasks.Create();

      var incomingDocument = IncomingDocumentBases.As(document);
      if (incomingDocument != null && incomingDocument.Addressee != null)
        task.Addressee = incomingDocument.Addressee;

      task.DocumentForReviewGroup.All.Add(document);

      // Выдать права группе регистрации документа.
      if (document.DocumentRegister != null)
      {
        var registrationGroup = document.DocumentRegister.RegistrationGroup;

        if (registrationGroup != null)
          task.AccessRights.Grant(registrationGroup, DefaultAccessRightsTypes.Change);
      }

      return task;
    }
    
    /// <summary>
    /// Создать поручение по документу.
    /// </summary>
    /// <param name="document">Документ на рассмотрение.</param>
    /// <returns>Поручение по документу.</returns>
    /// <remarks>Только для создания самостоятельного поручения.
    /// Для создания подпоручения используется CreateActionItemExecutionTask(document, parentAssignment).</remarks>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IActionItemExecutionTask CreateActionItemExecution(IOfficialDocument document)
    {
      return CreateActionItemExecution(document, Assignments.Null);
    }

    /// <summary>
    /// Создать поручение по документу, с указанием задания-основания.
    /// </summary>
    /// <param name="document">Документ, на основании которого создается задача.</param>
    /// <param name="parentAssignmentId">Задание-основание.</param>
    /// <returns>Поручение по документу.</returns>
    /// <remarks>Для создания подпоручения, явно указывать MainTask.
    /// Необходимо для корректной работы вложений и текстов в задаче.
    /// TODO Lomagin: после исправления 24898 удалить.</remarks>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IActionItemExecutionTask CreateActionItemExecution(IOfficialDocument document, int parentAssignmentId)
    {
      return CreateActionItemExecution(document, Assignments.Get(parentAssignmentId));
    }

    /// <summary>
    /// Создать поручение по документу, с указанием задания-основания.
    /// </summary>
    /// <param name="document">Документ, на основании которого создается задача.</param>
    /// <param name="parentAssignmentId">Задание-основание.</param>
    /// <param name="resolution">Текст резолюции.</param>
    /// <param name="assignedBy">Пользователь - автор резолюции.</param>
    /// <returns>Поручение по документу.</returns>
    /// <remarks>Для создания подпоручения, явно указывать MainTask.
    /// Необходимо для корректной работы вложений и текстов в задаче.
    /// TODO Lomagin: после исправления 24898 удалить.</remarks>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IActionItemExecutionTask CreateActionItemExecutionWithResolution(IOfficialDocument document, int parentAssignmentId, string resolution, Sungero.Company.IEmployee assignedBy)
    {
      var newTask = CreateActionItemExecution(document, Assignments.Get(parentAssignmentId));
      newTask.ActionItem = resolution;
      newTask.Assignee = document.Assignee;
      newTask.AssignedBy = Docflow.PublicFunctions.Module.Remote.IsUsersCanBeResolutionAuthor(document, assignedBy) ? assignedBy : null;
      return newTask;
    }

    /// <summary>
    /// Создать поручение по документу с указанием задания-основания.
    /// </summary>
    /// <param name="document">Документ, на основании которого создается задача.</param>
    /// <param name="parentAssignment">Задание-основание.</param>
    /// <returns>Поручение по документу.</returns>
    /// <remarks>Для создания подпоручения явно указывать MainTask.
    /// Необходимо для корректной работы вложений и текстов в задаче.
    /// Метод !!ПАДАЕТ, если задание имеет в своих вложениях document. Баг 24899.</remarks>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IActionItemExecutionTask CreateActionItemExecution(IOfficialDocument document, IAssignment parentAssignment)
    {
      var task = parentAssignment == null ? ActionItemExecutionTasks.Create() : ActionItemExecutionTasks.CreateAsSubtask(parentAssignment);
      task.DocumentsGroup.OfficialDocuments.Add(document);
      task.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(task, ActionItemExecutionTasks.Resources.TaskSubject);
      // Выдать права на изменение группе регистрации. Группа регистрации будет взята из журнала документа.
      var documentRegister = document.DocumentRegister;

      if (documentRegister != null && documentRegister.RegistrationGroup != null)
        task.AccessRights.Grant(documentRegister.RegistrationGroup, DefaultAccessRightsTypes.Change);

      if (parentAssignment != null)
      {
        var resolutions = new List<IEntity>();
        if (DocumentReviewTasks.Is(parentAssignment.Task.MainTask))
          resolutions = DocumentReviewTasks.As(parentAssignment.Task.MainTask).ResolutionGroup.All.ToList();
        var addenda = task.AddendaGroup.All;
        foreach (var otherGroupAttachment in parentAssignment.AllAttachments.Where(x => !Equals(x, document) && !addenda.Contains(x) && !resolutions.Contains(x)))
          task.OtherGroup.All.Add(otherGroupAttachment);
      }
      
      return task;
    }
    
    /// <summary>
    /// Создать задачу на ознакомление с документом.
    /// </summary>
    /// <param name="document">Документ, который отправляется на ознакомление.</param>
    /// <returns>Задача на ознакомление по документу.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IAcquaintanceTask CreateAcquaintanceTask(IOfficialDocument document)
    {
      var newAcqTask = AcquaintanceTasks.Create();
      newAcqTask.DocumentGroup.OfficialDocuments.Add(document);
      return newAcqTask;
    }
    
    /// <summary>
    /// Создать задачу на ознакомление с документом.
    /// </summary>
    /// <param name="document">Документ, который отправляется на ознакомление.</param>
    /// <param name="parentAssignment">Задание, из которого создается подзадача.</param>
    /// <returns>Задача на ознакомление по документу.</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IAcquaintanceTask CreateAcquaintanceTaskAsSubTask(IOfficialDocument document, IAssignment parentAssignment)
    {
      var newAcqTask = AcquaintanceTasks.CreateAsSubtask(parentAssignment);
      newAcqTask.DocumentGroup.OfficialDocuments.Add(document);
      return newAcqTask;
    }

    /// <summary>
    /// Создать поручение.
    /// </summary>
    /// <returns>Поручение.</returns>
    [Remote, Public]
    public static IActionItemExecutionTask CreateActionItemExecution()
    {
      return ActionItemExecutionTasks.Create();
    }

    #region AbortSubtasksAndSendNotices

    /// <summary>
    /// Рекурсивно завершить все подзадачи, выслать уведомления.
    /// </summary>
    /// <param name="actionItem">Поручение, подзадачи которого следует завершить.</param>
    public static void AbortSubtasksAndSendNotices(IActionItemExecutionTask actionItem)
    {
      AbortSubtasksAndSendNotices(actionItem, null, string.Empty);
    }

    /// <summary>
    /// Рекурсивно завершить все подзадачи, выслать уведомления.
    /// </summary>
    /// <param name="actionItem">Поручение, подзадачи которого следует завершить.</param>
    /// <param name="performer">Исполнитель, которого не нужно уведомлять.</param>
    /// <param name="abortingReason">Причина прекращения.</param>
    public static void AbortSubtasksAndSendNotices(IActionItemExecutionTask actionItem, IUser performer = null, string abortingReason = "")
    {
      var recipients = AssignmentBases.GetAll(a => Equals(a.Task, actionItem)).Select(u => u.Performer).ToList();
      var subTasks = Functions.Module.GetSubtasksForTaskRecursive(actionItem);
      foreach (var subTask in subTasks
               .Where(t => ActionItemExecutionTasks.Is(t) || DeadlineExtensionTasks.Is(t) || StatusReportRequestTasks.Is(t)))
      {
        var job = ActionItemExecutionTasks.As(subTask);
        if (job != null)
          job.AbortingReason = string.IsNullOrEmpty(abortingReason) ? job.AbortingReason : abortingReason;

        subTask.Abort();
        recipients.AddRange(AssignmentBases.GetAll(a => Equals(a.Task, subTask)).Select(u => u.Performer).ToList());
      }

      recipients = recipients.Distinct().ToList();
      if (performer != null)
        recipients.Remove(performer);
      else
        recipients.Remove(Users.Current);

      if (recipients.Any())
      {
        var threadSubject = ActionItemExecutionTasks.Resources.NoticeSubjectWithoutDoc;
        var noticesSubject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(actionItem, Sungero.RecordManagement.Resources.TwoSpotTemplateFormat(threadSubject));
        Docflow.PublicFunctions.Module.Remote.SendNoticesAsSubtask(noticesSubject, recipients, actionItem, actionItem.AbortingReason, performer, threadSubject);
      }
    }

    #endregion

    /// <summary>
    /// Рекурсивно получить все незавершенные подзадачи.
    /// </summary>
    /// <param name="task">Задача, для которой необходимо получить незавершенные подзадачи.</param>
    /// <returns>Список незавершенных подзадач.</returns>
    public static List<ITask> GetSubtasksForTaskRecursive(ITask task)
    {
      // TODO: переписать на тернарный оператор после исправления бага 18327
      var subTasks = Tasks.GetAll(p => ((p.ParentAssignment != null && task.Equals(p.ParentAssignment.Task)) ||
                                        (p.ParentAssignment == null && task.Equals(p.ParentTask))) &&
                                  p.Status == Workflow.Task.Status.InProcess).ToList();

      var result = new List<ITask>();
      result.AddRange(subTasks);

      foreach (var subTask in subTasks)
        result.AddRange(GetSubtasksForTaskRecursive(subTask));

      return result;
    }

    #endregion

    #region Работа с документами

    /// <summary>
    /// Получить виды документов по документопотоку.
    /// </summary>
    /// <param name="direction">Документопоток вида документа.</param>
    /// <returns>Виды документов.</returns>
    [Remote(IsPure = true)]
    public static List<IDocumentKind> GetFilteredDocumentKinds(Enumeration direction)
    {
      if (direction == Docflow.DocumentKind.DocumentFlow.Incoming)
        return DocumentKinds.GetAll(d => d.DocumentFlow.Value == Docflow.DocumentKind.DocumentFlow.Incoming).ToList();
      else if (direction == Docflow.DocumentKind.DocumentFlow.Outgoing)
        return DocumentKinds.GetAll(d => d.DocumentFlow.Value == Docflow.DocumentKind.DocumentFlow.Outgoing).ToList();
      else if (direction == Docflow.DocumentKind.DocumentFlow.Inner)
        return DocumentKinds.GetAll(d => d.DocumentFlow.Value == Docflow.DocumentKind.DocumentFlow.Inner).ToList();
      else if (direction == Docflow.DocumentKind.DocumentFlow.Contracts)
        return DocumentKinds.GetAll(d => d.DocumentFlow.Value == Docflow.DocumentKind.DocumentFlow.Contracts).ToList();
      else
        return null;
    }

    /// <summary>
    /// Получить входящее письмо по ИД.
    /// </summary>
    /// <param name="letterId">ИД письма.</param>
    /// <returns>Если письмо не существует возвращает null.</returns>
    [Remote(IsPure = true)]
    public static IOutgoingDocumentBase GetIncomingLetterById(int letterId)
    {
      return Sungero.Docflow.OutgoingDocumentBases.GetAll().FirstOrDefault(l => l.Id == letterId);
    }
    
    /// <summary>
    /// Провалидировать подписи документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="onlyLastSignature">Проверить только последнюю подпись.</param>
    /// <returns>Если подписи валидны, возвращает пустой список, иначе список ошибок.</returns>
    [Public]
    public static List<string> GetDocumentSignatureValidationErrors(IEntity document, bool onlyLastSignature)
    {
      var validationMessages = new List<string>();
      if (document == null)
        return validationMessages;
      
      var signatures = Signatures.Get(document).Where(s => s.SignatureType == SignatureType.Approval && s.IsExternal != true);
      if (onlyLastSignature)
        signatures = signatures.OrderByDescending(x => x.Id).Take(1);
      
      foreach (var signature in signatures)
      {
        var error = Functions.Module.GetSignatureValidationErrors(signature);
        if (!string.IsNullOrWhiteSpace(error))
          validationMessages.Add(error);
      }
      
      return validationMessages;
    }
    
    /// <summary>
    /// Провалидировать подпись.
    /// </summary>
    /// <param name="signature">Подпись.</param>
    /// <returns>Если подпись валидна, возвращает пустую строку, иначе строку с ошибкой.</returns>
    [Public]
    public static string GetSignatureValidationErrors(Sungero.Domain.Shared.ISignature signature)
    {
      if (signature == null)
        return string.Empty;

      var separator = ". ";
      var signatureErrors = Docflow.PublicFunctions.Module.GetSignatureValidationErrorsAsString(signature, separator);
      if (string.IsNullOrWhiteSpace(signatureErrors))
        return string.Empty;
      
      var signatory = string.IsNullOrWhiteSpace(signature.SubstitutedUserFullName)
        ? signature.SignatoryFullName
        : RecordManagement.Resources.SignatorySubstituteFormat(signature.SignatoryFullName, signature.SubstitutedUserFullName);
      
      return RecordManagement.Resources.SignatureValidationMessageFormat(signatory,
                                                                         signature.SigningDate,
                                                                         signatureErrors);
    }
    
    #endregion

    #region Работа с SQL

    /// <summary>
    /// Выполнить SQL-запрос.
    /// </summary>
    /// <param name="format">Формат запроса.</param>
    /// <param name="args">Аргументы запроса, подставляемые в формат.</param>
    /// <remarks>Функция дублируется из Docflow, т.к. нельзя исп. params в public-функциях.</remarks>
    public static void ExecuteSQLCommandFormat(string format, params object[] args)
    {
      var command = string.Format(format, args);
      Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
    }

    #endregion

    #region Подпапки входящих
    
    /// <summary>
    /// Применить к списку заданий стандартные фильтры: по длинному периоду (180 дней) и по статусу "Завершено".
    /// </summary>
    /// <param name="query">Список заданий.</param>
    /// <returns>Отфильтрованный список заданий.</returns>
    [Public]
    public IQueryable<Sungero.Workflow.IAssignmentBase> ApplyCommonSubfolderFilters(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      return this.ApplyCommonSubfolderFilters(query, false, false, false, false, true);
    }
    
    /// <summary>
    /// Применить к списку заданий фильтры по статусу и периоду.
    /// </summary>
    /// <param name="query">Список заданий.</param>
    /// <param name="inProcess">Признак показа заданий "В работе".</param>
    /// <param name="shortPeriod">Фильтр по короткому периоду (30 дней).</param>
    /// <param name="middlePeriod">Фильтр по среднему периоду (90 дней).</param>
    /// <param name="longPeriod">Фильтр по длинному периоду (180 дней).</param>
    /// <param name="longPeriodToCompleted">Фильтр по длинному периоду (180 дней) для завершённых заданий.</param>
    /// <returns>Отфильтрованный список заданий.</returns>
    [Public]
    public IQueryable<Sungero.Workflow.IAssignmentBase> ApplyCommonSubfolderFilters(IQueryable<Sungero.Workflow.IAssignmentBase> query,
                                                                                    bool inProcess,
                                                                                    bool shortPeriod,
                                                                                    bool middlePeriod,
                                                                                    bool longPeriod,
                                                                                    bool longPeriodToCompleted)
    {
      // Фильтр по статусу.
      if (inProcess)
        return query.Where(a => a.Status == Workflow.AssignmentBase.Status.InProcess);
      
      // Фильтр по периоду.
      DateTime? periodBegin = null;
      if (shortPeriod)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-30));
      else if (middlePeriod)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-90));
      else if (longPeriod || longPeriodToCompleted)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-180));
      
      if (shortPeriod || middlePeriod || longPeriod)
        query = query.Where(a => a.Created >= periodBegin);
      else if (longPeriodToCompleted)
        query = query.Where(a => a.Created >= periodBegin || a.Status == Workflow.AssignmentBase.Status.InProcess);

      return query;
    }
    
    /// <summary>
    /// Применить к списку задач стандартные фильтры: по длинному периоду (180 дней) и по статусу "Завершено".
    /// </summary>
    /// <param name="query">Список задач.</param>
    /// <returns>Отфильтрованный список задач.</returns>
    [Public]
    public IQueryable<Sungero.Workflow.ITask> ApplyCommonSubfolderFilters(IQueryable<Sungero.Workflow.ITask> query)
    {
      return this.ApplyCommonSubfolderFilters(query, false, false, false, false, true);
    }
    
    /// <summary>
    /// Применить к списку задач фильтры по статусу и периоду.
    /// </summary>
    /// <param name="query">Список задач.</param>
    /// <param name="inProcess">Признак показа задач "В работе".</param>
    /// <param name="shortPeriod">Фильтр по короткому периоду (30 дней).</param>
    /// <param name="middlePeriod">Фильтр по среднему периоду (90 дней).</param>
    /// <param name="longPeriod">Фильтр по длинному периоду (180 дней).</param>
    /// <param name="longPeriodToCompleted">Фильтр по длинному периоду (180 дней) для завершённых задач.</param>
    /// <returns>Отфильтрованный список задач.</returns>
    [Public]
    public IQueryable<Sungero.Workflow.ITask> ApplyCommonSubfolderFilters(IQueryable<Sungero.Workflow.ITask> query,
                                                                          bool inProcess,
                                                                          bool shortPeriod,
                                                                          bool middlePeriod,
                                                                          bool longPeriod,
                                                                          bool longPeriodToCompleted)
    {
      // Фильтр по статусу.
      if (inProcess)
        return query.Where(t => t.Status == Workflow.Task.Status.InProcess || t.Status == Workflow.Task.Status.Draft);

      // Фильтр по периоду.
      DateTime? periodBegin = null;
      if (shortPeriod)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-30));
      else if (middlePeriod)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.Today.AddDays(-90));
      else if (longPeriod || longPeriodToCompleted)
        periodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.Today.AddDays(-180));
      
      if (shortPeriod || middlePeriod || longPeriod)
        query = query.Where(t => t.Created >= periodBegin);
      else if (longPeriodToCompleted)
        query = query.Where(t => t.Created >= periodBegin || t.Status == Workflow.AssignmentBase.Status.InProcess);
      
      return query;
    }

    #endregion
    
    /// <summary>
    /// Получить информацию о контроле поручения.
    /// </summary>
    /// <param name="actionItemTask">Поручение.</param>
    /// <returns>Информация о контролере.</returns>
    [Public]
    public virtual string GetSupervisorInfoForActionItem(IActionItemExecutionTask actionItemTask)
    {
      var supervisor = actionItemTask.Supervisor;
      var isOnControl = actionItemTask.IsUnderControl == true;
      var supervisorLabel = string.Empty;
      if (isOnControl && supervisor != null)
        supervisorLabel = Company.PublicFunctions.Employee.GetShortName(supervisor, false);
      return supervisorLabel;
    }
    
    /// <summary>
    /// Данные для печати проекта резолюции.
    /// </summary>
    /// <param name="resolution">Список поручений.</param>
    /// <param name="reportSessionId">ИД сессии.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public static List<Structures.DraftResolutionReport.DraftResolutionReportParameters> GetDraftResolutionReportData(List<IActionItemExecutionTask> resolution,
                                                                                                                      string reportSessionId)
    {
      var result = new List<Structures.DraftResolutionReport.DraftResolutionReportParameters>();
      foreach (var actionItemTask in resolution)
      {
        // Контролер.
        var supervisor = actionItemTask.Supervisor;
        var isOnControl = actionItemTask.IsUnderControl == true;
        var supervisorLabel = string.Empty;
        if (isOnControl && supervisor != null)
          supervisorLabel = Company.PublicFunctions.Employee.GetShortName(supervisor, false);
        
        // Равноправное поручение.
        if (actionItemTask.IsCompoundActionItem == true)
        {
          foreach (var part in actionItemTask.ActionItemParts)
          {
            var deadline = part.Deadline ?? actionItemTask.FinalDeadline ?? Calendar.Now;
            var resolutionLabel = string.Join("\r\n", actionItemTask.ActionItem, part.ActionItemPart);
            var data = GetActionItemDraftResolutionReportData(part.Assignee,
                                                              null,
                                                              deadline,
                                                              resolutionLabel,
                                                              supervisorLabel,
                                                              reportSessionId);
            result.Add(data);
          }
        }
        else
        {
          // Поручение с соисполнителями.
          var deadline = actionItemTask.Deadline ?? Calendar.Now;
          var subAssignees = actionItemTask.CoAssignees.Select(a => a.Assignee).ToList();
          var data = GetActionItemDraftResolutionReportData(actionItemTask.Assignee,
                                                            subAssignees,
                                                            deadline,
                                                            actionItemTask.ActionItem,
                                                            supervisorLabel,
                                                            reportSessionId);
          result.Add(data);
        }
      }
      return result;
    }
    
    /// <summary>
    /// Получение данных поручения для отчета Проект резолюции.
    /// </summary>
    /// <param name="assignee">Исполнитель.</param>
    /// <param name="subAssignees">Соисполнители.</param>
    /// <param name="deadline">Срок исполнения.</param>
    /// <param name="actionItem">Текст поручения.</param>
    /// <param name="supervisorLabel">Контролёр.</param>
    /// <param name="reportSessionId">Ид сессии.</param>
    /// <returns>Данные поручения.</returns>
    public static Structures.DraftResolutionReport.DraftResolutionReportParameters GetActionItemDraftResolutionReportData(IEmployee assignee,
                                                                                                                          List<IEmployee> subAssignees,
                                                                                                                          DateTime deadline,
                                                                                                                          string actionItem,
                                                                                                                          string supervisorLabel,
                                                                                                                          string reportSessionId)
    {
      var data = new Structures.DraftResolutionReport.DraftResolutionReportParameters();
      data.ReportSessionId = reportSessionId;
      
      // Исполнители и срок.
      var assigneeShortName = Company.PublicFunctions.Employee.GetShortName(assignee, false);
      if (subAssignees != null && subAssignees.Any())
        assigneeShortName = string.Format("<u>{0}</u>{1}{2}", assigneeShortName, Environment.NewLine,
                                          string.Join(", ", subAssignees.Select(p => Company.PublicFunctions.Employee.GetShortName(p, false))));
      
      data.PerformersLabel = assigneeShortName;
      data.Deadline = deadline.HasTime() ? deadline.ToUserTime().ToString("g") : deadline.ToString("d");
      
      // Поручение.
      data.ResolutionLabel = actionItem;
      
      // Контролёр.
      data.SupervisorLabel = supervisorLabel;
      
      return data;
    }
    
    /// <summary>
    /// Исключить из наблюдателей системных пользователей.
    /// </summary>
    /// <param name="query">Запрос.</param>
    /// <returns>Отфильтрованный результат запроса.</returns>
    [Public]
    public IQueryable<Sungero.CoreEntities.IRecipient> ObserversFiltering(IQueryable<Sungero.CoreEntities.IRecipient> query)
    {
      var systemRecipientsSid = Company.PublicFunctions.Module.GetSystemRecipientsSidWithoutAllUsers(true);
      return query.Where(x => !systemRecipientsSid.Contains(x.Sid.Value));
    }

    /// <summary>
    /// Получить константу срока рассмотрения документа по умолчанию в днях.
    /// </summary>
    /// <returns>Константу срока рассмотрения документа по умолчанию в днях.</returns>
    [RemoteAttribute]
    public virtual int GetDocumentReviewDefaultDays()
    {
      return Constants.Module.DocumentReviewDefaultDays;
    }
    
    /// <summary>
    /// Получить отфильтрованные журналы регистрации для отчета.
    /// </summary>
    /// <param name="direction">Документопоток.</param>
    /// <returns>Журналы регистрации.</returns>
    [Remote(IsPure = true)]
    public static List<IDocumentRegister> GetFilteredDocumentRegistersForReport(Enumeration direction)
    {
      var needFilterDocumentRegisters = !Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor();
      return Docflow.PublicFunctions.DocumentRegister.Remote.GetFilteredDocumentRegisters(direction, null, needFilterDocumentRegisters).ToList();
    }
    
    /// <summary>
    /// Удалить поручения.
    /// </summary>
    /// <param name="actionItems">Список поручений.</param>
    [Remote]
    public static void DeleteActionItemExecutionTasks(List<IActionItemExecutionTask> actionItems)
    {
      foreach (var draftResolution in actionItems)
        ActionItemExecutionTasks.Delete(draftResolution);
    }
    
    /// <summary>
    /// Получить списки ознакомления.
    /// </summary>
    /// <returns>Списки ознакомления.</returns>
    [Remote(IsPure = true)]
    public IQueryable<IAcquaintanceList> GetAcquaintanceLists()
    {
      return AcquaintanceLists.GetAll()
        .Where(a => a.Status == Sungero.RecordManagement.AcquaintanceList.Status.Active);
    }
    
    /// <summary>
    /// Создать список ознакомления.
    /// </summary>
    /// <returns>Список ознакомления.</returns>
    [Remote]
    public IAcquaintanceList CreateAcquaintanceList()
    {
      return AcquaintanceLists.Create();
    }
    
    /// <summary>
    /// Получить поручение по ИД.
    /// </summary>
    /// <param name="id">ИД задачи.</param>
    /// <returns>Поручение.</returns>
    [Remote]
    public IActionItemExecutionTask GetActionitemById(int id)
    {
      return ActionItemExecutionTasks.GetAll(t => Equals(t.Id, id)).FirstOrDefault();
    }
    
    /// <summary>
    /// Получить исполнителей задачи на ознакомление.
    /// </summary>
    /// <param name="recipients">Участники.</param>
    /// <returns>Исполнители.</returns>
    public virtual List<IEmployee> GetAcquaintanceTaskPerformers(List<IRecipient> recipients)
    {
      return Docflow.PublicFunctions.Module.GetEmployeesFromRecipients(recipients);
    }
    
    /// <summary>
    /// Получить статус выполнения задания на ознакомление.
    /// </summary>
    /// <param name="assignment">Задание на ознакомление.</param>
    /// <param name="isElectronicAcquaintance">Признак "Электронное ознакомление".</param>
    /// <param name="isCompleted">Признак завершённости задачи.</param>
    /// <returns>Статус выполнения задания на ознакомление.</returns>
    public virtual string GetAcquaintanceAssignmentState(IAcquaintanceAssignment assignment,
                                                         bool isElectronicAcquaintance,
                                                         bool isCompleted)
    {
      if (!isCompleted)
        return string.Empty;
      
      if (Equals(assignment.CompletedBy, assignment.Performer) || !isElectronicAcquaintance)
        return Reports.Resources.AcquaintanceReport.AcquaintedState;

      return Reports.Resources.AcquaintanceReport.CompletedState;
    }
    
    /// <summary>
    /// Получить все приложения по задачам ознакомления с документом.
    /// </summary>
    /// <param name="tasks">Задачи.</param>
    /// <returns>Список приложений.</returns>
    [Remote(IsPure = true)]
    public List<IElectronicDocument> GetAcquintanceTaskAddendas(List<IAcquaintanceTask> tasks)
    {
      var addenda = new List<IElectronicDocument>();
      var addendaIds = tasks.SelectMany(x => x.AcquaintanceVersions)
        .Where(x => x.IsMainDocument != true)
        .Select(x => x.DocumentId);
      
      var documentAddenda = tasks.SelectMany(x => x.AddendaGroup.OfficialDocuments)
        .Where(x => addendaIds.Contains(x.Id))
        .Distinct()
        .ToList();
      addenda.AddRange(documentAddenda);

      return addenda;
    }
    
    /// <summary>
    /// Получить все приложения по задаче ознакомления с документом.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Список приложений.</returns>
    [Remote(IsPure = true)]
    public List<IElectronicDocument> GetAcquintanceTaskAddendas(IAcquaintanceTask task)
    {
      return this.GetAcquintanceTaskAddendas(new List<IAcquaintanceTask> { task });
    }
    
    /// <summary>
    /// Получить список всех получателей.
    /// </summary>
    /// <returns>Список всех получателей.</returns>
    [Obsolete, Public, Remote(IsPure = true)]
    public IQueryable<IRecipient> GetAllRecipients()
    {
      return Sungero.CoreEntities.Recipients.GetAll();
    }
    
    /// <summary>
    /// Получить значение поля Адресат в отчете Журнал исходящих документов.
    /// </summary>
    /// <param name="letterId">ИД исходящего письма.</param>
    /// <returns>Значение поля Адресат.</returns>
    [Public]
    public string GetOutgoingDocumentReportAddressee(int letterId)
    {
      var outgoingLetter = Sungero.Docflow.OutgoingDocumentBases.Get(letterId);
      if (outgoingLetter == null)
        return string.Empty;
      if (outgoingLetter.Addressees.Count < 5)
      {
        var addresseeList = new List<string>();
        foreach (var addressee in outgoingLetter.Addressees.OrderBy(a => a.Number))
        {
          var addresseeString = addressee.Addressee == null
            ? addressee.Correspondent.Name
            : string.Concat(addressee.Correspondent.Name, "\n", addressee.Addressee.Name);

          addresseeList.Add(addresseeString);
        }
        return string.Join("\n\n", addresseeList);
      }
      else
        return Docflow.PublicFunctions.Module.ReplaceFirstSymbolToUpperCase(
          OutgoingLetters.Resources.CorrespondentToManyAddressees.ToString().Trim());
    }
    
    /// <summary>
    /// Получить данные для отчета DraftResolutionReport.
    /// </summary>
    /// <param name="actionItems">Поручения.</param>
    /// <param name="reportSessionId">Ид отчета.</param>
    /// <param name="textResolution">Текстовая резолюция.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public virtual List<Structures.DraftResolutionReport.DraftResolutionReportParameters> GetDraftResolutionReportData(List<IActionItemExecutionTask> actionItems, string reportSessionId, string textResolution)
    {
      // Получить данные для отчета.
      var reportData = new List<Structures.DraftResolutionReport.DraftResolutionReportParameters>();
      if (actionItems.Any())
      {
        reportData = PublicFunctions.Module.GetDraftResolutionReportData(actionItems, reportSessionId);
      }
      else
      {
        // Если нет поручений, то берём текстовую резолюцию.
        reportData = new List<Structures.DraftResolutionReport.DraftResolutionReportParameters>();
        var data = new Structures.DraftResolutionReport.DraftResolutionReportParameters();
        data.ReportSessionId = reportSessionId;
        data.PerformersLabel = textResolution;
        reportData.Add(data);
      }
      return reportData;
    }
    
    /// <summary>
    /// Получить представление документа для отчета DraftResolutionReport.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns> Представление.</returns>
    [Public]
    public virtual string GetDraftResolutionReportDocumentShortName(Docflow.IOfficialDocument document)
    {
      // Номер и дата документа.
      var documentShortName = string.Empty;
      if (document != null)
      {
        if (!string.IsNullOrWhiteSpace(document.RegistrationNumber))
          documentShortName += string.Format("{0} {1}", Docflow.OfficialDocuments.Resources.Number, document.RegistrationNumber);
        
        if (document.RegistrationDate.HasValue)
          documentShortName += Docflow.OfficialDocuments.Resources.DateFrom + document.RegistrationDate.Value.ToString("d");
        
        if (!string.IsNullOrWhiteSpace(document.RegistrationNumber))
          documentShortName += string.Format(" ({0} {1})", Reports.Resources.DraftResolutionReport.IDPrefix, document.Id.ToString());
        else
          documentShortName += string.Format(" {0} {1}", Reports.Resources.DraftResolutionReport.IDPrefix, document.Id.ToString());
      }
      return documentShortName;
    }
    
    /// <summary>
    /// Получить представление документа для отчета ActionItemPrintReport.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="actionItem">Поручение.</param>
    /// <returns>Представление.</returns>
    [Public]
    public virtual string GetActionItemPrintReportDocumentShortName(Docflow.IOfficialDocument document, Sungero.Workflow.IAssignment actionItem)
    {
      // Номер и дата документа.
      var documentShortName = string.Empty;
      if (document != null)
      {
        // "К документу".
        documentShortName += Reports.Resources.ActionItemPrintReport.ToDocument;
        
        // Номер.
        if (!string.IsNullOrWhiteSpace(document.RegistrationNumber))
          documentShortName += string.Format("{0} {1}", Docflow.OfficialDocuments.Resources.Number, document.RegistrationNumber);
        
        // Дата.
        if (document.RegistrationDate.HasValue)
          documentShortName += string.Format("{0}{1}", Docflow.OfficialDocuments.Resources.DateFrom, document.RegistrationDate.Value.ToString("d"));
        
        // ИД и разделитель /.
        documentShortName += string.Format(" ({0} {1}) / ", Reports.Resources.ActionItemPrintReport.DocumentID, document.Id.ToString());
      }
      
      // ИД поручения.
      documentShortName += string.Format("{0} {1}", Reports.Resources.ActionItemPrintReport.ActionItemID, actionItem.Id.ToString());
      
      return documentShortName;
    }
    
    /// <summary>
    /// Получить данные для отчета ActionItemPrintReport.
    /// </summary>
    /// <param name="actionItemTask">Поручение.</param>
    /// <param name="reportId">Ид отчета.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public virtual List<Structures.ActionItemPrintReport.ActionItemPrintReportParameters> GetActionItemPrintReportData(IActionItemExecutionTask actionItemTask, string reportId)
    {
      // Получить данные для отчета.
      var reportData = new List<Structures.ActionItemPrintReport.ActionItemPrintReportParameters>();
      
      // Контролёр.
      var supervisor = this.GetSupervisorInfoForActionItem(actionItemTask);
      // От кого.
      var fromAuthor = this.GetAuthorLineInfoForActionItem(actionItemTask);
      
      // Равноправное поручение.
      if (actionItemTask.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Component)
      {
        var task = ActionItemExecutionTasks.As(actionItemTask.ParentTask);
        var part = task.ActionItemParts.Where(n => n.Assignee.Equals(actionItemTask.Assignee)).FirstOrDefault();
        var actionItemText = string.Join("\r\n", task.ActionItem, part.ActionItemPart);
        var assigneeShortName = Company.PublicFunctions.Employee.GetShortName(actionItemTask.Assignee, false);
        
        var deadline = part.Deadline ?? task.FinalDeadline ?? Calendar.Now;
        var formattedDeadline = deadline.HasTime() ? deadline.ToUserTime().ToString("g") : deadline.ToString("d");
        
        var data = this.GetActionItemPrintReportData(assigneeShortName, formattedDeadline, fromAuthor, supervisor, actionItemText, reportId);
        reportData.Add(data);
      }
      else
      {
        var task = actionItemTask.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Additional ? ActionItemExecutionTasks.As(actionItemTask.ParentAssignment.Task) : actionItemTask;
        // Поручение с соисполнителями.
        var actionItemText = task.ActionItem;
        var subAssignees = task.CoAssignees.Select(a => a.Assignee).ToList();
        var assigneeShortName = Company.PublicFunctions.Employee.GetShortName(task.Assignee, false);
        if (subAssignees != null && subAssignees.Any())
          assigneeShortName = string.Format("<u>{0}</u>{1}{2}",
                                            assigneeShortName,
                                            Environment.NewLine,
                                            string.Join(", ", subAssignees.Select(p => Company.PublicFunctions.Employee.GetShortName(p, false))));
        
        var deadline = task.Deadline ?? Calendar.Now;
        var formattedDeadline = deadline.HasTime() ? deadline.ToUserTime().ToString("g") : deadline.ToString("d");
        
        var data = this.GetActionItemPrintReportData(assigneeShortName, formattedDeadline, fromAuthor, supervisor, actionItemText, reportId);
        reportData.Add(data);
      }

      return reportData;
    }
    
    /// <summary>
    /// Получить данные для отчета ActionItemPrintReport.
    /// </summary>
    /// <param name="assigneeShortName">Кому.</param>
    /// <param name="deadline">Срок.</param>
    /// <param name="fromAuthor">От кого.</param>
    /// <param name="supervisor">Контролер.</param>
    /// <param name="actionItemText">Текст поручения.</param>
    /// <param name="reportId">Ид отчета.</param>
    /// <returns>Структура для отчета.</returns>
    [Public]
    public virtual Structures.ActionItemPrintReport.ActionItemPrintReportParameters GetActionItemPrintReportData(string assigneeShortName, string deadline, string fromAuthor, string supervisor,
                                                                                                                 string actionItemText, string reportId)
    {
      var data = new Structures.ActionItemPrintReport.ActionItemPrintReportParameters();
      data.ReportSessionId = reportId;
      data.Performer = assigneeShortName;
      data.Deadline = deadline;
      data.ActionItemText = actionItemText;
      data.Supervisor = supervisor;
      data.FromAuthor = fromAuthor;
      
      return data;
    }
    
    /// <summary>
    /// Получить цепочку сотрудников, выдавших поручение.
    /// </summary>
    /// <param name="actionItemTask">Поручение.</param>
    /// <returns>Информация о выдавших поручение.</returns>
    [Public]
    public virtual string GetAuthorLineInfoForActionItem(IActionItemExecutionTask actionItemTask)
    {
      var authorInfo = Company.PublicFunctions.Employee.GetShortName(Employees.As(actionItemTask.AssignedBy), false);
      var currentTask = Workflow.Tasks.As(actionItemTask);
      var parentTask = currentTask.ParentTask != null ? currentTask.ParentTask : currentTask.ParentAssignment != null ? currentTask.ParentAssignment.Task : currentTask.MainTask;
      while (ActionItemExecutionTasks.As(parentTask) != null && currentTask != parentTask)
      {
        if (ActionItemExecutionTasks.As(currentTask).ActionItemType != RecordManagement.ActionItemExecutionTask.ActionItemType.Component)
          authorInfo = string.Format("{0} -> {1}", Company.PublicFunctions.Employee.GetShortName(Employees.As(ActionItemExecutionTasks.As(parentTask).AssignedBy), false), authorInfo);
        
        currentTask = parentTask;
        parentTask = parentTask.ParentTask != null ? parentTask.ParentTask : currentTask.ParentAssignment != null ? currentTask.ParentAssignment.Task : currentTask.MainTask;
      }
      return authorInfo;
    }

  }
  
}