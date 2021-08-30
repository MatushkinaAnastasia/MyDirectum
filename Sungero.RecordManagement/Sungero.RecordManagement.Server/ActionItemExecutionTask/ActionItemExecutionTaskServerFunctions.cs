using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.RecordManagement.ActionItemExecutionTask;
using Sungero.Security;
using Sungero.Workflow;
using Sungero.Workflow.Task;
using DeclensionCase = Sungero.Core.DeclensionCase;

namespace Sungero.RecordManagement.Server
{
  partial class ActionItemExecutionTaskFunctions
  {

    #region Предметное отображение
    
    /// <summary>
    /// Построить модель состояния главного поручения.
    /// </summary>
    /// <returns>Схема модели состояния.</returns>
    [Public, Remote(IsPure = true)]
    public string GetStateViewXml()
    {
      return this.GetStateView().ToString();
    }
    
    /// <summary>
    /// Построить модель состояния главного поручения.
    /// </summary>
    /// <returns>Контрол состояния.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      // Определить главное поручение и построить его состояние.
      var mainActionItemExecutionTask = this.GetMainActionItemExecutionTask();

      var stateViewModel = Structures.ActionItemExecutionTask.StateViewModel.Create();
      stateViewModel.Tasks = new List<IActionItemExecutionTask>() { mainActionItemExecutionTask };
      stateViewModel = GetAllActionItems(stateViewModel);
      return GetActionItemStateView(mainActionItemExecutionTask, _obj, stateViewModel, null, null);
    }

    /// <summary>
    /// Найти самое верхнее поручение.
    /// </summary>
    /// <returns>Самое верхнее поручение.</returns>
    public IActionItemExecutionTask GetMainActionItemExecutionTask()
    {
      var mainActionItemExecutionTask = _obj;
      ITask currentTask = _obj;
      while (currentTask.ParentTask != null || currentTask.ParentAssignment != null)
      {
        currentTask = currentTask.ParentTask ?? currentTask.ParentAssignment.Task;
        if (ActionItemExecutionTasks.Is(currentTask))
          mainActionItemExecutionTask = ActionItemExecutionTasks.As(currentTask);
      }
      return mainActionItemExecutionTask;
    }
    
    /// <summary>
    /// Построить модель состояния главного поручения.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Контрол состояния.</returns>
    public Sungero.Core.StateView GetStateView(Sungero.Docflow.IOfficialDocument document)
    {
      if (!_obj.DocumentsGroup.OfficialDocuments.Any(d => Equals(document, d)))
        return StateView.Create();
      
      return GetActionItemStateView(_obj, null, null, null, null);
    }
    
    /// <summary>
    /// Построить модель состояния поручения.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="openedTask">Новое подпоручение.</param>
    /// <param name="stateViewModel">Модель предметного отображения.</param>
    /// <param name="draftAssignee">Исполнитель в черновике.</param>
    /// <param name="draftActionItem">Поручение в черновике.</param>
    /// <param name="draftDeadline">Срок в черновике.</param>
    /// <param name="draftNumber">Номер поручения в черновике составного.</param>
    /// <param name="skipResolutionBlock">Пропустить блок резолюции.</param>
    /// <param name="withHighlight">Выделять цветом основной блок.</param>
    /// <returns>Модель состояния.</returns>
    [Public]
    public static Sungero.Core.StateView GetActionItemStateView(IActionItemExecutionTask task,
                                                                IActionItemExecutionTask openedTask,
                                                                Structures.ActionItemExecutionTask.IStateViewModel stateViewModel,
                                                                IEmployee draftAssignee = null,
                                                                string draftActionItem = "",
                                                                DateTime? draftDeadline = null,
                                                                int? draftNumber = null,
                                                                bool skipResolutionBlock = false,
                                                                bool withHighlight = true)
    {
      var stateView = StateView.Create();
      
      if (task == null && openedTask == null)
        return stateView;
      
      if (stateViewModel == null)
        stateViewModel = Structures.ActionItemExecutionTask.StateViewModel.Create();

      if (stateViewModel.Tasks == null || stateViewModel.Tasks.Count == 0)
      {
        stateViewModel.Tasks = new List<IActionItemExecutionTask>() { task };
        stateViewModel = GetAllActionItems(stateViewModel);
      }
      
      var isDraft = true;
      if (task != null)
        isDraft = task.Status == Workflow.Task.Status.Draft;
      
      // Стили.
      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle(isDraft);
      var performerDeadlineStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle(isDraft);
      var boldStyle = Docflow.PublicFunctions.Module.CreateStyle(true, isDraft, false);
      var grayStyle = Docflow.PublicFunctions.Module.CreateStyle(false, isDraft, true);
      var labelStyle = Docflow.PublicFunctions.Module.CreateStyle(false, isDraft, false);
      var separatorStyle = Docflow.PublicFunctions.Module.CreateSeparatorStyle();

      if (stateViewModel.StatusesCache == null)
        stateViewModel.StatusesCache = new Dictionary<Enumeration?, string>();

      // Добавить блок по резолюции, если поручение в рамках рассмотрения.
      // Блок добавить только для самого верхнего поручения.
      if (task != null && task.MainTask != null && !skipResolutionBlock &&
          Equals(task, Functions.ActionItemExecutionTask.GetMainActionItemExecutionTask(task)) && DocumentReviewTasks.Is(task.MainTask))
      {
        StateView reviewState;
        if (DocumentReviewTasks.Is(task.MainTask))
        {
          reviewState = Functions.DocumentReviewTask.GetStateView(DocumentReviewTasks.As(task.MainTask));
          foreach (var block in reviewState.Blocks)
            stateView.AddBlock(block);
        }
        else if (Docflow.ApprovalTasks.Is(task.MainTask))
        {
          Functions.ActionItemExecutionTask.AddReviewBlock(task, stateView, stateViewModel.Tasks, stateViewModel.Assignments);
        }
      }
      
      var main = false;
      var additional = false;
      var component = false;
      var underControl = false;
      var hasCoAssigness = false;
      var isCompound = false;
      if (task != null)
      {
        main = task.ActionItemType == ActionItemType.Main;
        additional = task.ActionItemType == ActionItemType.Additional;
        component = task.ActionItemType == ActionItemType.Component;
        underControl = task.IsUnderControl == true;
        hasCoAssigness = task.CoAssignees.Any();
        isCompound = task.IsCompoundActionItem == true;
      }
      else
      {
        var isCompoundDraftActionItem = openedTask.IsCompoundActionItem == true;
        additional = !isCompoundDraftActionItem;
        component = isCompoundDraftActionItem;
        underControl = !isCompoundDraftActionItem || openedTask.IsUnderControl == true;
        isDraft = true;
      }

      // Не выводить задачу, если она была стартована до последнего рестарта главной, если это не черновик.
      if (!isDraft)
      {
        var parentTask = Tasks.Null;
        if (task.ActionItemType == ActionItemType.Component)
          parentTask = task.ParentTask;
        else if (task.ActionItemType == ActionItemType.Additional)
          parentTask = task.ParentAssignment.Task;
        
        if (parentTask != null && parentTask.Started.HasValue && task.Started.HasValue && parentTask.Started > task.Started)
          return StateView.Create();
      }

      // Добавить заголовок с информацией по отправителю поручения.
      if (main && task != null)
      {
        var text = ActionItemExecutionTasks.Resources.StateViewActionItemOnExecution;
        if (task.ParentAssignment != null && ActionItemExecutionAssignments.Is(task.ParentAssignment))
          text = ActionItemExecutionTasks.Resources.StateViewSubordinateActionItemSent;
        var comment = Docflow.PublicFunctions.Module.GetFormatedUserText(text);
        
        if (task.Started.HasValue)
          Docflow.PublicFunctions.OfficialDocument
            .AddUserActionBlock(stateView, task.Author, comment, task.Started.Value, task, string.Empty, task.StartedBy);
        else
          Docflow.PublicFunctions.OfficialDocument
            .AddUserActionBlock(stateView, task.Author, Docflow.ApprovalTasks.Resources.StateViewTaskDrawCreated, task.Created.Value, task, string.Empty, task.Author);
      }
      
      var taskBlock = stateView.AddBlock();
      
      if (task != null && !task.State.IsInserted)
        taskBlock.Entity = task;
      
      if (Equals(task, openedTask) && withHighlight)
        Docflow.PublicFunctions.Module.MarkBlock(taskBlock);
      
      // Для поручения соисполнителю сменить иконку.
      if (additional)
        taskBlock.AssignIcon(ActionItemExecutionAssignments.Info.Actions.CreateChildActionItem, StateBlockIconSize.Large);
      else if (isCompound)
        taskBlock.AssignIcon(ActionItemExecutionTasks.Resources.CompoundActionItem, StateBlockIconSize.Large);
      else if (taskBlock.Entity != null)
        taskBlock.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Large);
      else
        taskBlock.AssignIcon(Docflow.ApprovalRuleBases.Resources.ActionItemTask, StateBlockIconSize.Large);

      // Статус.
      var status = GetStatusInfo(task, stateViewModel.StatusesCache);
      
      // Для непрочитанных заданий указать это.
      if (task != null && task.Status == Workflow.Task.Status.InProcess)
      {
        var actionItemExecution = stateViewModel.Assignments
          .Where(a => Equals(a.Task.Id, task.Id) && ActionItemExecutionAssignments.Is(a))
          .OrderByDescending(a => a.Created)
          .FirstOrDefault();
        if (actionItemExecution != null && actionItemExecution.IsRead == false)
          status = Docflow.ApprovalTasks.Resources.StateViewUnRead.ToString();
      }
      if (!string.IsNullOrWhiteSpace(status))
        Docflow.PublicFunctions.Module.AddInfoToRightContent(taskBlock, status, labelStyle);
      
      // Заголовок.
      var header = GetHeader(task, additional, component, hasCoAssigness, isCompound, openedTask, draftNumber);
      taskBlock.AddLabel(header, headerStyle);
      taskBlock.AddLineBreak();
      
      // Задержка исполнения.
      var deadline = task != null ? GetDeadline(task, isCompound) : draftDeadline;
      var deadlineLabel = deadline.HasValue ? Docflow.PublicFunctions.Module.ToShortDateShortTime(deadline.Value.ToUserTime()) : ActionItemExecutionTasks.Resources.StateViewNotSpecified;
      if (deadline.HasValue && task != null &&
          (task.ExecutionState == ExecutionState.OnExecution ||
           task.ExecutionState == ExecutionState.OnControl ||
           task.ExecutionState == ExecutionState.OnRework))
        Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(taskBlock, deadline.Value, task.Assignee ?? Users.Current);
      
      // Добавить информацию по главному поручению, поручению соисполнителю и подпоручений составного.
      if (!isCompound)
      {
        // Кому.
        var assignee = task != null ? task.Assignee : draftAssignee;
        var assigneName = assignee != null ? Company.PublicFunctions.Employee.GetShortName(assignee, false) : ActionItemExecutionTasks.Resources.StateViewNotSpecified;
        var performerInfo = string.Format("{0}: {1}", Docflow.OfficialDocuments.Resources.StateViewTo, assigneName);
        taskBlock.AddLabel(performerInfo, performerDeadlineStyle);
        
        // Срок.
        var deadlineInfo = string.Format(" {0}: {1} ", Docflow.OfficialDocuments.Resources.StateViewDeadline, deadlineLabel);
        taskBlock.AddLabel(deadlineInfo, performerDeadlineStyle);
        
        // Контролёр.
        var supervisor = task != null ? task.Supervisor : openedTask.Assignee;
        if (underControl && !component && supervisor != null)
        {
          var supervisorInfo = string.Format(" {0}: {1}", Docflow.OfficialDocuments.Resources.StateViewSupervisor, Company.PublicFunctions.Employee.GetShortName(supervisor, false));
          taskBlock.AddLabel(supervisorInfo.Trim(), performerDeadlineStyle);
        }
        
        // Разделитель.
        taskBlock.AddLineBreak();
        taskBlock.AddLabel(Docflow.Constants.Module.SeparatorText, separatorStyle);
        taskBlock.AddLineBreak();
        taskBlock.AddEmptyLine(Docflow.Constants.Module.EmptyLineMargin);
        
        var actionItem = task != null ? task.ActionItem : draftActionItem;
        
        // Отчет по исполнению поручения и текст поручения.
        var report = GetReportInfo(task, stateViewModel.Assignments);
        if (!string.IsNullOrWhiteSpace(report))
        {
          taskBlock.AddLabel(Docflow.PublicFunctions.Module.GetFormatedUserText(actionItem), grayStyle);
          
          taskBlock.AddLineBreak();
          taskBlock.AddLabel(report, labelStyle);
        }
        else
        {
          taskBlock.AddLabel(Docflow.PublicFunctions.Module.GetFormatedUserText(actionItem), labelStyle);
        }
        
        // Добавить подпоручения.
        AddAssignmentTasks(taskBlock, task, openedTask, stateViewModel);
      }
      else
      {
        // Добавить информацию по главному поручению составного.
        // Общий срок.
        if (task.FinalDeadline.HasValue)
          deadline = task.FinalDeadline;
        
        if (deadline.HasValue)
          taskBlock.AddLabel(string.Format("{0}: {1}",
                                           Docflow.OfficialDocuments.Resources.StateViewFinalDeadline,
                                           Docflow.PublicFunctions.Module.ToShortDateShortTime(deadline.Value.ToUserTime())),
                             performerDeadlineStyle);
        
        // Контролёр.
        var supervisor = task != null ? task.Supervisor : openedTask.Supervisor;
        if (underControl && !component && supervisor != null)
        {
          var supervisorInfo = string.Format(" {0}: {1}", Docflow.OfficialDocuments.Resources.StateViewSupervisor, Company.PublicFunctions.Employee.GetShortName(supervisor, false));
          taskBlock.AddLabel(supervisorInfo.Trim(), performerDeadlineStyle);
        }
        
        // Разделитель.
        taskBlock.AddLineBreak();
        taskBlock.AddLabel(Docflow.Constants.Module.SeparatorText, separatorStyle);
        taskBlock.AddLineBreak();
        
        // Общий текст составного поручения.
        var actionItem = task != null ? task.ActionItem : draftActionItem;
        
        if (task != null && task.Status != Sungero.Workflow.Task.Status.Draft && task.Status != Sungero.Workflow.Task.Status.InProcess)
        {
          taskBlock.AddLabel(Docflow.PublicFunctions.Module.GetFormatedUserText(actionItem), grayStyle);
        }
        else
        {
          taskBlock.AddLabel(Docflow.PublicFunctions.Module.GetFormatedUserText(actionItem), labelStyle);
        }
        
        // Добавить подпоручения составного поручения и подпоручения к ним.
        AddComponentSubTasks(taskBlock, task, openedTask, stateViewModel);
        taskBlock.NeedGroupChildren = true;
      }
      
      taskBlock.IsExpanded = false;
      
      // Раскрыть поручение, если оно в работе, на приёмке, это черновик или это открытое поручение.
      if (isDraft || task.Status == Workflow.Task.Status.InProcess ||
          task.Status == Workflow.Task.Status.UnderReview || Equals(task, openedTask))
        taskBlock.IsExpanded = true;
      
      // Если есть развернутые подчиненные поручения, то развернуть и это.
      if (taskBlock.ChildBlocks.Where(c => c.IsExpanded == true).Any())
        taskBlock.IsExpanded = true;

      return stateView;
    }
    
    /// <summary>
    /// Заполнение модели контрола состояния задачи на исполнение поручения.
    /// </summary>
    /// <param name="model">Модель контрола состояния.</param>
    /// <returns>Заполненная (полностью или частично) модель контрола состояния.</returns>
    public static Structures.ActionItemExecutionTask.IStateViewModel GetAllActionItems(Structures.ActionItemExecutionTask.IStateViewModel model)
    {
      if (model.Tasks == null)
        model.Tasks = new List<IActionItemExecutionTask>();
      if (model.Assignments == null)
        model.Assignments = new List<IAssignment>();
      
      var tasksIds = model.Tasks.Select(p => p.Id).ToList();
      var assignmentsIds = model.Assignments.Select(p => p.Id).ToList();
      
      // Подзадачи - пунты составного поручения.
      var subtasks = ActionItemExecutionTasks.GetAll(t => t.ParentTask != null && tasksIds.Contains(t.ParentTask.Id) && !tasksIds.Contains(t.Id)).ToList();
      model.Tasks.AddRange(subtasks);

      // Подзадачи - подчиненные поручения и поручения соисполнителям.
      var assignments = Assignments.GetAll(a => tasksIds.Contains(a.Task.Id) && !assignmentsIds.Contains(a.Id)).ToList();
      model.Assignments.AddRange(assignments);
      assignmentsIds = assignments.Select(a => a.Id).ToList();
      var assignmentSubtasks = ActionItemExecutionTasks.GetAll(t => t.ParentAssignment != null &&
                                                               assignmentsIds.Contains(t.ParentAssignment.Id)).ToList();
      model.Tasks.AddRange(assignmentSubtasks);
      
      if (subtasks.Any() || assignmentSubtasks.Any())
        GetAllActionItems(model);

      Logger.DebugFormat("ActionItemsView: tasks count: {0}", model.Tasks.Count.ToString());
      return model;
    }

    /// <summary>
    /// Получить статус выполнения поручения.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="statusesCache">Кэш статусов.</param>
    /// <returns>Статус.</returns>
    private static string GetStatusInfo(IActionItemExecutionTask task, Dictionary<Enumeration?, string> statusesCache)
    {
      Enumeration? status = null;
      if (task == null || task.Status == Workflow.Task.Status.Draft)
      {
        status = Workflow.Task.Status.Draft;
      }
      else if (task.ExecutionState != null && task.IsCompoundActionItem != true)
      {
        status = task.ExecutionState == ExecutionState.OnRework ? ExecutionState.OnExecution : task.ExecutionState;
      }
      else if (task.Status == Workflow.Task.Status.InProcess)
      {
        status = ExecutionState.OnExecution;
      }
      else if (task.Status == Workflow.Task.Status.Aborted)
      {
        status = ExecutionState.Aborted;
      }
      else if (task.Status == Workflow.Task.Status.Suspended)
      {
        status = Workflow.AssignmentBase.Status.Suspended;
      }
      else if (task.Status == Workflow.Task.Status.Completed)
      {
        status = ExecutionState.Executed;
      }
      
      return GetLocalizedValue(status, statusesCache);
    }

    private static string GetLocalizedValue(Enumeration? value, Dictionary<Enumeration?, string> statusesCache)
    {
      string localizedStatus = string.Empty;
      if (!statusesCache.TryGetValue(value.Value, out localizedStatus))
      {
        localizedStatus = value == Workflow.Task.Status.Draft ?
          Workflow.Tasks.Info.Properties.Status.GetLocalizedValue(value) :
          ActionItemExecutionTasks.Info.Properties.ExecutionState.GetLocalizedValue(value);
        statusesCache.Add(value.Value, localizedStatus);
      }

      return localizedStatus;
    }
    
    /// <summary>
    /// Получить заголовок блока поручения.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="additional">Задача соисполнителю.</param>
    /// <param name="component">Задача составного поручения.</param>
    /// <param name="hasCoAssigness">Есть соисполнители.</param>
    /// <param name="isCompound">Составное поручение.</param>
    /// <param name="openedTask">Черновик.</param>
    /// <param name="number">Номер подпункта поручения.</param>
    /// <returns>Заголовок.</returns>
    private static string GetHeader(IActionItemExecutionTask task, bool additional, bool component, bool hasCoAssigness, bool isCompound,
                                    IActionItemExecutionTask openedTask, int? number)
    {
      var header = ActionItemExecutionTasks.Resources.StateViewActionItem;
      if (additional)
        header = ActionItemExecutionTasks.Resources.StateViewActionItemForCoAssignee;
      
      if (isCompound)
        header = ActionItemExecutionTasks.Resources.StateViewCompoundActionItem;
      
      if (hasCoAssigness && !additional)
        header = ActionItemExecutionTasks.Resources.StateViewActionItemForResponsible;
      
      if (component)
      {
        if (number != null)
          return string.Format("{0}{1}", ActionItemExecutionTasks.Resources.StateViewActionItemPart, number);
        else
          header = ActionItemExecutionTasks.Resources.StateViewActionItemPart;
      }
      
      return header;
    }

    /// <summary>
    /// Получить срок поручения.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="isCompound">Составное.</param>
    /// <returns>Срок.</returns>
    private static DateTime? GetDeadline(IActionItemExecutionTask task, bool isCompound)
    {
      // Срок обычного поручения.
      if (task.MaxDeadline.HasValue)
        return task.MaxDeadline.Value;
      
      // Срок составного поручения.
      if (isCompound)
        return task.ActionItemParts.Select(p => p.Deadline ?? task.FinalDeadline).Max();
      
      return null;
    }
    
    /// <summary>
    /// Получить отчет по поручению.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="assignments">Задания.</param>
    /// <returns>Отчет.</returns>
    private static string GetReportInfo(IActionItemExecutionTask task, List<IAssignment> assignments)
    {
      if (task == null)
        return string.Empty;
      
      var actionItemExecution = assignments.Where(a => ActionItemExecutionAssignments.Is(a))
        .Where(a => Equals(a.Task.Id, task.Id))
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
      
      if (actionItemExecution != null && actionItemExecution.Status == Workflow.AssignmentBase.Status.Completed)
        return string.Format("{0}: {1}", ActionItemExecutionTasks.Resources.StateViewReport,
                             Docflow.PublicFunctions.Module.GetFormatedUserText(actionItemExecution.ActiveText));
      
      return string.Empty;
    }
    
    /// <summary>
    /// Добавить блоки подпоручений.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="task">Задача.</param>
    /// <param name="openedTask">Новое подпоручение.</param>
    /// <param name="stateViewModel">Модель предметного отображения.</param>
    private static void AddAssignmentTasks(StateBlock block, IActionItemExecutionTask task, IActionItemExecutionTask openedTask,
                                           Structures.ActionItemExecutionTask.IStateViewModel stateViewModel)
    {
      if (task == null)
        return;
      
      // Добавить ещё не созданные подзадачи черновика.
      if (Equals(task, openedTask) && openedTask.Status == Workflow.Task.Status.Draft)
      {
        var childBlocks = openedTask.CoAssignees
          .SelectMany(a => Functions.ActionItemExecutionTask
                      .GetActionItemStateView(null, openedTask, stateViewModel, a.Assignee, openedTask.ActionItem, openedTask.MaxDeadline, null, false, true)
                      .Blocks);
        foreach (var childBlock in childBlocks)
          block.AddChildBlock(childBlock);
        block.IsExpanded = true;
        return;
      }
      
      var subTasks = stateViewModel.Tasks
        .Where(t => t.ParentAssignment != null && Equals(t.ParentAssignment.Task.Id, task.Id))
        .Where(t => t.Started >= task.Started)
        .OrderBy(t => t.Started)
        .ToList();
      
      // Добавить вывод черновика подпоручения.
      if (openedTask != null && openedTask.ParentAssignment != null &&
          Equals(task, openedTask.ParentAssignment.Task) &&
          !subTasks.Any(st => Equals(openedTask, st)))
        subTasks.Add(openedTask);
      
      var blocks = subTasks.SelectMany(t => GetActionItemStateView(t, openedTask, stateViewModel).Blocks);
      foreach (var childBlock in blocks)
        block.AddChildBlock(childBlock);
      block.IsExpanded = subTasks.Any(t => t.Status == Workflow.Task.Status.InProcess || t.Status == Workflow.Task.Status.Draft) ||
        block.ChildBlocks.Any(b => b.IsExpanded);
    }
    
    /// <summary>
    /// Добавить блоки подпоручений составного поручения.
    /// </summary>
    /// <param name="stateBlock">Схема.</param>
    /// <param name="task">Задача.</param>
    /// <param name="openedTask">Черновик.</param>
    /// <param name="stateViewModel">Модель предметного отображения.</param>
    private static void AddComponentSubTasks(StateBlock stateBlock, IActionItemExecutionTask task, IActionItemExecutionTask openedTask,
                                             Structures.ActionItemExecutionTask.IStateViewModel stateViewModel)
    {
      if (task == null)
        return;
      
      // Добавить ещё не созданные подзадачи черновика.
      if (Equals(task, openedTask) && openedTask.Status == Workflow.Task.Status.Draft)
      {
        var draftTaskParts = openedTask.ActionItemParts;
        var childBlocks = draftTaskParts.OrderBy(a => a.Number)
          .SelectMany(a => Functions.ActionItemExecutionTask.GetActionItemStateView(
            null, openedTask, stateViewModel, a.Assignee,
            string.IsNullOrEmpty(a.ActionItemPart) ? openedTask.ActionItem : a.ActionItemPart,
            a.Deadline ?? openedTask.FinalDeadline,
            a.Number, false, true)
                      .Blocks);
        foreach (var block in childBlocks)
          stateBlock.AddChildBlock(block);
        stateBlock.IsExpanded = true;
        return;
      }
      
      foreach (var partTask in task.ActionItemParts.OrderBy(pt => pt.Number))
      {
        var currentPartTask = partTask.ActionItemPartExecutionTask;
        if (currentPartTask == null || stateBlock.ChildBlocks.Any(b => b.HasEntity(currentPartTask)))
          continue;
        
        var childBlocks = GetActionItemStateView(currentPartTask, openedTask, stateViewModel, currentPartTask.Assignee, draftNumber: partTask.Number).Blocks;
        foreach (var block in childBlocks)
          stateBlock.AddChildBlock(block);
      }
    }
    
    /// <summary>
    /// Добавить блок информации о рассмотрении документа руководителем.
    /// </summary>
    /// <param name="stateView">Схема представления.</param>
    /// <param name="tasks">Задачи.</param>
    /// <param name="assignments">Задания.</param>
    /// <returns>Полученный блок.</returns>
    public Sungero.Core.StateBlock AddReviewBlock(Sungero.Core.StateView stateView, List<IActionItemExecutionTask> tasks, List<IAssignment> assignments)
    {
      var reviewAssignmentBase = assignments.Where(a => Docflow.ApprovalReviewAssignments.Is(a))
        .Where(a => Equals(a.Task.Id, _obj.MainTask.Id))
        .OrderByDescending(a => a.Created)
        .FirstOrDefault();
      
      if (reviewAssignmentBase == null)
        return null;

      var reviewAssignment = Docflow.ApprovalReviewAssignments.As(reviewAssignmentBase);
      
      // Добавить блок информации по отправителю.
      var text = Docflow.ApprovalTasks.Resources.StateViewDocumentSentForApproval;
      var task = reviewAssignment.Task;
      Docflow.PublicFunctions.OfficialDocument
        .AddUserActionBlock(stateView, task.Author, text, task.Started.Value, task, string.Empty, task.StartedBy);
      
      var author = Docflow.PublicFunctions.OfficialDocument.GetAuthor(reviewAssignment.Performer, reviewAssignment.CompletedBy);
      var actionItems = tasks
        .Where(t => t.ParentAssignment != null && Equals(t.ParentAssignment.Task.Id, reviewAssignment.Task.Id) && t.Status != Workflow.Task.Status.Draft)
        .OrderBy(t => t.Started);
      var isCompleted = reviewAssignment.Status == Workflow.AssignmentBase.Status.Completed;

      var headerStyle = Docflow.PublicFunctions.Module.CreateHeaderStyle();
      var performerStyle = Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle();
      var separatorStyle = Docflow.PublicFunctions.Module.CreateSeparatorStyle();
      
      // Добавить блок. Установить иконку и сущность.
      var block = stateView.AddBlock();
      block.Entity = reviewAssignment;
      if (isCompleted)
        block.AssignIcon(reviewAssignment.Info.Actions.AddResolution, StateBlockIconSize.Large);
      else
        block.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Large);

      // Рассмотрение руководителем ещё в работе.
      if (!isCompleted)
      {
        // Добавить заголовок.
        block.AddLabel(Docflow.Resources.StateViewDocumentReview, headerStyle);
        block.AddLineBreak();
        Docflow.PublicFunctions.Module.AddInfoToRightContent(block, Docflow.ApprovalTasks.Info.Properties.Status.GetLocalizedValue(reviewAssignment.Status));
        var employeeName = Employees.Is(reviewAssignment.Performer) ?
          Company.PublicFunctions.Employee.GetShortName(Employees.As(reviewAssignment.Performer), false) :
          reviewAssignment.Performer.Name;
        var headerText = string.Format("{0}: {1} ",
                                       Docflow.Resources.StateViewAddressee,
                                       employeeName);
        
        if (reviewAssignment.Deadline != null)
        {
          var deadlineText = string.Format(" {0}: {1}",
                                           Docflow.OfficialDocuments.Resources.StateViewDeadline,
                                           Docflow.PublicFunctions.Module.ToShortDateShortTime(reviewAssignment.Deadline.Value.ToUserTime()));
          headerText = headerText + deadlineText;
        }
        
        block.AddLabel(headerText, performerStyle);
        
        Docflow.PublicFunctions.OfficialDocument.AddDeadlineHeaderToRight(block, reviewAssignment.Deadline.Value, reviewAssignment.Performer);
      }
      else
      {
        // Рассмотрение завершено.
        // Добавить заголовок.
        var resolutionDate = Docflow.PublicFunctions.Module.ToShortDateShortTime(reviewAssignment.Completed.Value.ToUserTime());
        block.AddLabel(Docflow.Resources.StateViewResolution, headerStyle);
        block.AddLineBreak();
        block.AddLabel(string.Format("{0}: {1} {2}: {3}",
                                     RecordManagement.DocumentReviewTasks.Resources.StateViewAuthor,
                                     author,
                                     Docflow.OfficialDocuments.Resources.StateViewDate,
                                     resolutionDate), performerStyle);
        block.AddLineBreak();
        block.AddLabel(Docflow.Constants.Module.SeparatorText, separatorStyle);
        block.AddLineBreak();
        block.AddEmptyLine(Docflow.Constants.Module.EmptyLineMargin);
        
        // Если поручения не созданы, значит, рассмотрение выполнено с результатом "Вынести резолюцию" или "Принято к сведению".
        // В старых задачах поручение и рассмотрение не связаны, поэтому обрабатываем такие случаи как резолюцию.
        if (!actionItems.Any())
        {
          var comment = Docflow.PublicFunctions.Module.GetFormatedUserText(reviewAssignment.Texts.Last().Body);
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
                AddActionItemInfo(block, item.ActionItemPartExecutionTask, author);
              }
            }
            else
            {
              AddActionItemInfo(block, actionItem, author);
            }
          }
        }
      }
      return block;
    }
    
    /// <summary>
    /// Добавить информацию о созданном поручении в резолюцию.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="actionItem">Поручение.</param>
    /// <param name="author">Автор.</param>
    public static void AddActionItemInfo(Sungero.Core.StateBlock block, IActionItemExecutionTask actionItem, string author)
    {
      block.AddEmptyLine(Docflow.Constants.Module.EmptyLineMargin);
      
      block.AddLabel(Docflow.PublicFunctions.Module.GetFormatedUserText(actionItem.ActiveText));
      block.AddLineBreak();
      
      // Исполнители.
      var performerStyle = Sungero.Docflow.PublicFunctions.Module.CreatePerformerDeadlineStyle();
      var info = string.Empty;
      if (actionItem.CoAssignees.Any())
        info += string.Format("{0}: {1}, {2}: {3}",
                              Docflow.Resources.StateViewResponsible,
                              Company.PublicFunctions.Employee.GetShortName(actionItem.Assignee, false),
                              Docflow.Resources.StateViewCoAssignees,
                              string.Join(", ", actionItem.CoAssignees.Select(c => Company.PublicFunctions.Employee.GetShortName(c.Assignee, false))));
      else
        info += string.Format("{0}: {1}", Docflow.Resources.StateViewAssignee, Company.PublicFunctions.Employee.GetShortName(actionItem.Assignee, false));
      
      // Срок.
      if (actionItem.MaxDeadline.HasValue)
        info += string.Format(" {0}: {1}", Docflow.OfficialDocuments.Resources.StateViewDeadline, Docflow.PublicFunctions.Module.ToShortDateShortTime(actionItem.MaxDeadline.Value.ToUserTime()));
      
      // Контролер.
      if (actionItem.IsUnderControl == true)
      {
        info += string.Format(" {0}: {1}", Docflow.OfficialDocuments.Resources.StateViewSupervisor, Company.PublicFunctions.Employee.GetShortName(actionItem.Supervisor, false));
      }
      
      block.AddLabel(info, performerStyle);
      block.AddLineBreak();
      block.AddLineBreak();
    }
    
    #endregion
    
    /// <summary>
    /// Установить статусы в документе из поручения.
    /// </summary>
    public virtual void SetDocumentStates()
    {
      var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document == null || !document.AccessRights.CanUpdate())
        return;
      
      var documentsGroupGuid = Docflow.PublicConstants.Module.TaskMainGroup.ActionItemExecutionTask;
      var tasksWithDocument = ActionItemExecutionTasks
        .GetAll(t => t.AttachmentDetails.Any(a => a.GroupId == documentsGroupGuid && a.AttachmentId == document.Id))
        .ToList();

      // Нужны поручения, которые созданы от заданий и задач других типов (согласование или рассмотрение).
      var firstLevelTasks = tasksWithDocument.Where(t => t.ParentTask != null ?
                                                    !ActionItemExecutionTasks.Is(t.ParentTask) :
                                                    !ActionItemExecutionAssignments.Is(t.ParentAssignment))
        .ToList();
      
      // А ещё нужны поручения, которые просто не от заданий и задач.
      firstLevelTasks.AddRange(tasksWithDocument.Where(t => t.ParentAssignment == null &&
                                                       t.ParentTask == null));

      Enumeration? executionState = Docflow.OfficialDocument.ExecutionState.WithoutExecut;
      Enumeration? controlExecutionState = Docflow.OfficialDocument.ControlExecutionState.WithoutControl;
      
      var inProcess = firstLevelTasks.Where(t => t.ExecutionState == ExecutionState.OnExecution ||
                                            t.ExecutionState == ExecutionState.OnRework ||
                                            t.ExecutionState == ExecutionState.OnControl)
        .ToList();
      
      // Добавить составные поручения, если хотя бы один пункт поручения в процессе исполнения.
      var compoundTasks = firstLevelTasks.Where(i => i.IsCompoundActionItem.Value == true);
      inProcess.AddRange(compoundTasks.Where(t => t.ActionItemParts.Any(i => i.ActionItemPartExecutionTask == null ||
                                                                        i.ActionItemPartExecutionTask.ExecutionState == ExecutionState.OnExecution ||
                                                                        i.ActionItemPartExecutionTask.ExecutionState == ExecutionState.OnRework ||
                                                                        i.ActionItemPartExecutionTask.ExecutionState == ExecutionState.OnControl)));
      
      if (inProcess.Any())
      {
        executionState = Docflow.OfficialDocument.ExecutionState.OnExecution;

        if (inProcess.Any(t => t.IsUnderControl == true))
        {
          controlExecutionState = inProcess.Any(t => t.IsUnderControl == true &&
                                                t.Importance == Sungero.RecordManagement.ActionItemExecutionTask.Importance.High)
            ? Docflow.OfficialDocument.ControlExecutionState.SpecialControl
            : Docflow.OfficialDocument.ControlExecutionState.OnControl;
        }
      }
      else
      {
        var executeTasks = firstLevelTasks.Where(t => t.ExecutionState == ExecutionState.Executed).ToList();
        executeTasks.AddRange(tasksWithDocument.Where(t => compoundTasks.Contains(t.ParentTask) && t.ExecutionState == ExecutionState.Executed));
        
        if (executeTasks.Any())
        {
          executionState = Docflow.OfficialDocument.ExecutionState.Executed;
          if (executeTasks.Any(t => t.IsUnderControl == true))
            controlExecutionState = Docflow.OfficialDocument.ControlExecutionState.ControlRemoved;
        }
      }
      
      if (firstLevelTasks.All(t => t.ExecutionState == ExecutionState.Aborted) && firstLevelTasks.Count > 0)
      {
        executionState = Docflow.OfficialDocument.ExecutionState.Aborted;
        controlExecutionState = null;
      }
      
      // Изменение статусов документа только при их реальном отличии, тогда не будет блокировок документа на одинаковых запущенных поручениях соисполнителям.
      if (document.ExecutionState != executionState)
      {
        Logger.DebugFormat("ExecutionState for document {0}. Current state: {1}, new state: {2}.", document.Id, document.ExecutionState, executionState);
        document.ExecutionState = executionState;
      }
      if (document.ControlExecutionState != controlExecutionState)
      {
        Logger.DebugFormat("ControlExecutionState for document {0}. Current state: {1} new state: {2}.", document.Id, document.ControlExecutionState, controlExecutionState);
        document.ControlExecutionState = controlExecutionState;
      }
    }
    
    /// <summary>
    /// Получить незавершенные подчиненные поручения.
    /// </summary>
    /// <param name="entity"> Поручение, для которого требуется получить незавершенные.</param>
    /// <returns>Список незавершенных подчиненных поручений.</returns>
    [Remote(IsPure = true)]
    public static List<IActionItemExecutionTask> GetSubActionItemExecutions(Sungero.RecordManagement.IActionItemExecutionAssignment entity)
    {
      // TODO Котегов: есть бага платформы 19850.
      return ActionItemExecutionTasks.GetAll()
        .Where(t => t.ParentAssignment == entity)
        .Where(t => t.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Additional ||
               t.ActionItemType == RecordManagement.ActionItemExecutionTask.ActionItemType.Main)
        .Where(t => t.Status.Value == Workflow.Task.Status.InProcess)
        .ToList();
    }
    
    /// <summary>
    /// Проверить, созданы ли поручения из задания.
    /// </summary>
    /// <param name="assignment">Задание, для которого проверить.</param>
    /// <returns>True, если поручения созданы, иначе false.</returns>
    [Remote(IsPure = true), Public]
    public static bool HasSubActionItems(IAssignment assignment)
    {
      var subActionItemExecutions = ActionItemExecutionTasks.GetAll()
        .Where(ai => Equals(ai.ParentAssignment, assignment));
      if (!subActionItemExecutions.Any())
        return true;
      
      return false;
    }
    
    /// <summary>
    /// Проверить, созданы ли поручения из задачи.
    /// </summary>
    /// <param name="task">Задача, для которой проверить.</param>
    /// <returns>True, если поручения созданы, иначе false.</returns>
    [Remote(IsPure = true), Public]
    public static bool HasSubActionItems(ITask task)
    {
      if (task == null)
        return false;
      
      var hasSubActionItem = ActionItemExecutionTasks.GetAll()
        .Where(a => a.ParentAssignment != null && Equals(a.ParentAssignment.Task, task))
        .Any();
      
      return hasSubActionItem;
    }
    
    /// <summary>
    /// Проверить, созданы ли поручения из задачи, с определенным заничением жизненного цикла.
    /// </summary>
    /// <param name="task">Задача, для которой проверить.</param>
    /// <param name="status">Статус поручений.</param>
    /// <returns>True, если поручения созданы, иначе false.</returns>
    [Remote(IsPure = true), Public]
    public static bool HasSubActionItems(ITask task, Enumeration status)
    {
      if (task == null)
        return false;
      
      var hasSubActionItem = ActionItemExecutionTasks.GetAll()
        .Where(a => a.ParentAssignment != null && Equals(a.ParentAssignment.Task, task))
        .Any(a => a.Status == status);
      
      return hasSubActionItem;
    }
    
    /// <summary>
    /// Проверить, созданы ли поручения из задачи, с определенным значением жизненного цикла, с учетом, что "Выдал" адресат.
    /// </summary>
    /// <param name="task">Задача, для которой проверить.</param>
    /// <param name="status">Статус поручений.</param>
    /// <param name="addressee">Адресат.</param>
    /// <returns>True, если поручения созданы, иначе false.</returns>
    [Remote(IsPure = true), Public]
    public static bool HasSubActionItems(ITask task, Enumeration status, Sungero.Company.IEmployee addressee)
    {
      if (task == null)
        return false;
      
      var hasSubActionItem = ActionItemExecutionTasks.GetAll()
        .Where(a => a.ParentAssignment != null && Equals(a.ParentAssignment.Task, task))
        .Where(a => Equals(addressee, a.AssignedBy))
        .Any(a => a.Status == status);
      
      return hasSubActionItem;
    }
    
    /// <summary>
    /// Получить задания исполнителей, не завершивших работу по поручению.
    /// </summary>
    /// <param name="entity"> Поручение, для которого требуется получить исполнителей.</param>
    /// <returns>Список исполнителей, не завершивших работу по поручению.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<IActionItemExecutionAssignment> GetActionItems(Sungero.RecordManagement.IActionItemExecutionTask entity)
    {
      return ActionItemExecutionAssignments.GetAll(j => ((entity.IsCompoundActionItem ?? false) ?
                                                         entity.Equals(j.Task.ParentTask) :
                                                         entity.Equals(j.Task)) &&
                                                   j.Status == Workflow.AssignmentBase.Status.InProcess);
    }
    
    /// <summary>
    /// Получить список поручений для формирования блока резолюции задачи на согласование.
    /// </summary>
    /// <param name="task">Задача согласования.</param>
    /// <param name="status">Статус поручений (исключаемый).</param>
    /// <param name="addressee">Адресат.</param>
    /// <returns>Список поручений.</returns>
    [Remote(IsPure = true), Public]
    public static List<ITask> GetActionItemsForResolution(ITask task, Enumeration status, IEmployee addressee)
    {
      var actionItems = RecordManagement.ActionItemExecutionTasks.GetAll()
        .Where(t => Equals(t.ParentAssignment.Task, task) && t.Status != status && Equals(t.AssignedBy, addressee))
        .OrderBy(t => t.Started);
      
      var actionItemList = new List<ITask>();
      
      foreach (var actionItem in actionItems)
      {
        if (actionItem.IsCompoundActionItem == true)
        {
          foreach (var item in actionItem.ActionItemParts)
          {
            actionItemList.Add(item.ActionItemPartExecutionTask);
          }
        }
        else
        {
          actionItemList.Add(actionItem);
        }
      }
      
      return actionItemList;
    }

    /// <summary>
    /// Сформировать вспомогательную информацию по поручению для задачи на согласование.
    /// </summary>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Вспомогательная информация по поручению для задачи на согласование.</returns>
    [Remote(IsPure = true), Public]
    public static List<string> ActionItemInfoProvider(ITask task)
    {
      var result = new string[4];
      var actionItem = ActionItemExecutionTasks.As(task);
      if (task != null)
      {
        // Отчет пользователя. result[0]
        result[0] += actionItem.ActiveText;
        
        // Исполнители. result[1]
        if (actionItem.CoAssignees.Any())
          result[1] += string.Format("{0}: {1}, {2}: {3}",
                                     Docflow.Resources.StateViewResponsible,
                                     Company.PublicFunctions.Employee.GetShortName(actionItem.Assignee, false),
                                     Docflow.Resources.StateViewCoAssignees,
                                     string.Join(", ", actionItem.CoAssignees.Select(c => Company.PublicFunctions.Employee.GetShortName(c.Assignee, false))));
        else
          result[1] += string.Format("{0}: {1}", Docflow.Resources.StateViewAssignee, Company.PublicFunctions.Employee.GetShortName(actionItem.Assignee, false));
        
        // Срок. result[2]
        if (actionItem.MaxDeadline.HasValue)
          result[2] += string.Format(" {0}: {1}", Docflow.OfficialDocuments.Resources.StateViewDeadline, Docflow.PublicFunctions.Module.ToShortDateShortTime(actionItem.MaxDeadline.Value.ToUserTime()));
        
        // Контролер. result[3]
        if (actionItem.IsUnderControl == true)
        {
          result[3] += string.Format(" {0}: {1}", Docflow.OfficialDocuments.Resources.StateViewSupervisor, Company.PublicFunctions.Employee.GetShortName(actionItem.Supervisor, false));
        }
      }
      return result.ToList();
    }
    
    /// <summary>
    /// Получить исполнителей, не завершивших работу по поручению.
    /// </summary>
    /// <param name="entity"> Поручение, для которого требуется получить исполнителей.</param>
    /// <returns>Список исполнителей, не завершивших работу по поручению.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<IUser> GetActionItemsPerformers(Sungero.RecordManagement.IActionItemExecutionTask entity)
    {
      return GetActionItems(entity).Select(p => p.Performer);
    }
    
    /// <summary>
    /// Выдать права на вложения поручения.
    /// </summary>
    /// <param name="attachmentGroup"> Группа вложения.</param>
    /// <param name="needGrantRightToPerformer"> Нужно ли выдать права исполнителю.</param>
    public virtual void GrantRightsToAttachments(List<IEntity> attachmentGroup, bool needGrantRightToPerformer)
    {
      foreach (var item in attachmentGroup)
      {
        if (ElectronicDocuments.Is(item))
        {
          var accessRights = item.AccessRights;
          
          if (_obj.Author != null && !accessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Read, _obj.Author))
            accessRights.Grant(_obj.Author, DefaultAccessRightsTypes.Read);
          
          if (_obj.AssignedBy != null && !accessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Read, _obj.AssignedBy))
            accessRights.Grant(_obj.AssignedBy, DefaultAccessRightsTypes.Read);
          
          if (_obj.Supervisor != null)
          {
            var accessRightType = item.AccessRights.CanUpdate(_obj.Author) ? DefaultAccessRightsTypes.Change : DefaultAccessRightsTypes.Read;
            if (!accessRights.IsGrantedDirectly(accessRightType, _obj.Supervisor))
              accessRights.Grant(_obj.Supervisor, accessRightType);
          }
          
          if (_obj.Assignee != null && needGrantRightToPerformer && !accessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Read, _obj.Assignee))
            accessRights.Grant(_obj.Assignee, DefaultAccessRightsTypes.Read);
          
          foreach (var observer in _obj.ActionItemObservers)
          {
            if (!accessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Read, observer.Observer))
              accessRights.Grant(observer.Observer, DefaultAccessRightsTypes.Read);
          }
        }
      }
    }
    
    /// <summary>
    /// Создать поручение из открытого задания.
    /// </summary>
    /// <param name="actionItemAssignment">Задание.</param>
    /// <returns>Поручение.</returns>
    [Remote(PackResultEntityEagerly = true)]
    public static IActionItemExecutionTask CreateActionItemExecutionFromExecution(Sungero.RecordManagement.IActionItemExecutionAssignment actionItemAssignment)
    {
      IActionItemExecutionTask task;
      var document = actionItemAssignment.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      var otherDocuments = actionItemAssignment.OtherGroup.All;
      
      // MainTask должен быть изменен до создания вложений и текстов задачи.
      if (document != null)
        task = Functions.Module.CreateActionItemExecution(document, actionItemAssignment);
      else
        task = ActionItemExecutionTasks.CreateAsSubtask(actionItemAssignment);

      foreach (var otherDocument in otherDocuments)
        if (!task.OtherGroup.All.Contains(otherDocument))
          task.OtherGroup.All.Add(otherDocument);
      
      task.Assignee = null;
      if (actionItemAssignment.Deadline.HasValue &&
          (actionItemAssignment.Deadline.Value.HasTime() && actionItemAssignment.Deadline >= Calendar.Now ||
           !actionItemAssignment.Deadline.Value.HasTime() && actionItemAssignment.Deadline >= Calendar.Today))
        task.Deadline = actionItemAssignment.Deadline;
      task.AssignedBy = Employees.Current;
      
      return task;
    }
    
    /// <summary>
    /// Выдать права на задачу контролеру, инициатору и группе регистрации инициатора ведущей задачи (включая ведущие ведущего).
    /// </summary>
    /// <param name="targetTask">Текущая задача.</param>
    /// <param name="sourceTask">Ведущая задача.</param>
    /// <returns>Текущую задачу с правами.</returns>
    public static IEntity GrantAccessRightToTask(IEntity targetTask, ITask sourceTask)
    {
      if (targetTask == null || sourceTask == null)
        return null;
      
      if (!ActionItemExecutionTasks.Is(sourceTask))
        sourceTask = GetLeadTaskToTask(sourceTask);
      
      var leadPerformers = Functions.ActionItemExecutionTask.GetLeadActionItemExecutionPerformers(ActionItemExecutionTasks.As(sourceTask));
      foreach (var performer in leadPerformers)
        targetTask.AccessRights.Grant(performer, DefaultAccessRightsTypes.Change);
      
      return targetTask;
    }
    
    /// <summary>
    /// Выдать права на задачу контролеру, инициатору и группе регистрации инициатора ведущей задачи (включая ведущие ведущего).
    /// </summary>
    /// <param name="targetAssignment">Текущее задание.</param>
    /// <param name="sourceTask">Ведущая задача.</param>
    /// <returns>Текущее задание с правами.</returns>
    [Remote, Public]
    public static IAssignment GrantAccessRightToAssignment(IAssignment targetAssignment, ITask sourceTask)
    {
      GrantAccessRightToTask(targetAssignment, sourceTask);
      targetAssignment.AccessRights.Save();
      return targetAssignment;
    }
    
    /// <summary>
    /// Получить всех контролеров, инициаторов (включая группу регистрации) ведущих задач.
    /// </summary>
    /// <param name="actionItemExecution">Поручение.</param>
    /// <returns>Список контроллеров, инициаторов.</returns>
    public static List<IRecipient> GetLeadActionItemExecutionPerformers(Sungero.RecordManagement.IActionItemExecutionTask actionItemExecution)
    {
      var leadPerformers = new List<IRecipient>();
      var taskAuthors = new List<IRecipient>();
      ITask parentTask = actionItemExecution;
      
      while (true)
      {
        if (parentTask.StartedBy != null)
          taskAuthors.Add(parentTask.StartedBy);
        
        if (ActionItemExecutionTasks.Is(parentTask))
        {
          var parentActionItemExecution = ActionItemExecutionTasks.As(parentTask);
          taskAuthors.Add(parentActionItemExecution.Author);
          if (parentActionItemExecution.Supervisor != null)
            leadPerformers.Add(parentActionItemExecution.Supervisor);
          if (parentActionItemExecution.AssignedBy != null)
            leadPerformers.Add(parentActionItemExecution.AssignedBy);
        }
        else if (DocumentReviewTasks.Is(parentTask))
        {
          var parentDocumentReview = DocumentReviewTasks.As(parentTask);
          taskAuthors.Add(parentDocumentReview.Author);
        }
        else if (Sungero.Docflow.ApprovalTasks.Is(parentTask))
        {
          // TODO Добавить исполнителей соласования.
          var parentApprovalTask = Sungero.Docflow.ApprovalTasks.As(parentTask);
          taskAuthors.Add(parentApprovalTask.Author);
        }
        
        if (Equals(parentTask.MainTask, parentTask))
          break;
        parentTask = GetLeadTaskToTask(parentTask);
      }
      
      // В список также включить: группы регистрации, в которых находится инициатор задачи, группу регистрации для документа.
      var registrationGroups = GetActionItemRegistrationGroups(taskAuthors, actionItemExecution.DocumentsGroup.OfficialDocuments.FirstOrDefault());
      
      leadPerformers.AddRange(taskAuthors);
      leadPerformers.AddRange(registrationGroups);
      return leadPerformers.Distinct().ToList();
    }
    
    /// <summary>
    /// Получить ведущую задачу задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Ведущая задача.</returns>
    public static ITask GetLeadTaskToTask(ITask task)
    {
      if (task.ParentAssignment != null)
        return task.ParentAssignment.Task;
      else
        return task.ParentTask ?? task.MainTask;
    }
    
    /// <summary>
    /// Получить нестандартных исполнителей задачи.
    /// </summary>
    /// <returns>Исполнители.</returns>
    public virtual List<IRecipient> GetTaskAdditionalAssignees()
    {
      var assignees = new List<IRecipient>();

      var actionItem = ActionItemExecutionTasks.As(_obj);
      if (actionItem == null)
        return assignees;
      
      // В список также включить: группы регистрации, в которых находится инициатор задачи, группу регистрации для документа.
      var authors = new List<IRecipient>() { _obj.Author };
      if (_obj.StartedBy != null)
        authors.Add(_obj.StartedBy);
      
      assignees.AddRange(GetActionItemRegistrationGroups(authors, actionItem.DocumentsGroup.OfficialDocuments.FirstOrDefault()));
      
      if (actionItem.Assignee != null)
        assignees.Add(actionItem.Assignee);
      
      if (actionItem.Supervisor != null)
        assignees.Add(actionItem.Supervisor);
      
      if (actionItem.AssignedBy != null)
        assignees.Add(actionItem.AssignedBy);
      
      assignees.AddRange(actionItem.CoAssignees.Where(o => o.Assignee != null).Select(o => o.Assignee));
      assignees.AddRange(actionItem.ActionItemParts.Where(o => o.Assignee != null).Select(o => o.Assignee));
      assignees.AddRange(actionItem.ActionItemObservers.Where(o => o.Observer != null).Select(o => o.Observer));
      
      return assignees.Distinct().ToList();
    }
    
    /// <summary>
    /// Проверить документ на вхождение в обязательную группу вложений.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если документ обязателен.</returns>
    public virtual bool DocumentInRequredGroup(Docflow.IOfficialDocument document)
    {
      return _obj.DocumentsGroup.OfficialDocuments.Any(d => Equals(d, document));
    }
    
    /// <summary>
    /// Получить группы регистрации для поручения.
    /// </summary>
    /// <param name="users">Список пользователей.</param>
    /// <param name="document">Вложенный документ.</param>
    /// <returns>Список групп регистрации.</returns>
    private static List<Docflow.IRegistrationGroup> GetActionItemRegistrationGroups(IList<IRecipient> users, Docflow.IOfficialDocument document)
    {
      var groups = new List<Docflow.IRegistrationGroup>();
      if (document != null && document.DocumentRegister != null &&
          document.DocumentRegister.RegistrationGroup != null &&
          document.DocumentRegister.RegistrationGroup.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
        groups.Add(document.DocumentRegister.RegistrationGroup);
      
      return groups;
    }
    
    /// <summary>
    /// Добавить получателей в группу исполнителей поручения, исключая дублирующиеся записи.
    /// </summary>
    /// <param name="recipient">Реципиент.</param>
    /// <returns>Если возникили ошибки/хинты, возвращает текст ошибки, иначе - пустая строка.</returns>
    [Public, Remote]
    public string SetRecipientsToAssignees(IRecipient recipient)
    {
      var error = string.Empty;
      var performers = new List<IRecipient> { recipient };
      var employees = Docflow.PublicFunctions.Module.Remote.GetEmployeesFromRecipientsRemote(performers);
      if (employees.Count > Constants.ActionItemExecutionTask.MaxCompoundGroup)
        return ActionItemExecutionTasks.Resources.BigGroupWarningFormat(Sungero.RecordManagement.PublicConstants.ActionItemExecutionTask.MaxCompoundGroup);
      
      var currentPerformers = _obj.ActionItemParts.Select(x => x.Assignee);
      employees = employees.Except(currentPerformers).ToList();
      
      foreach (var employee in employees)
        _obj.ActionItemParts.AddNew().Assignee = employee;
      
      return error;
    }
  }
}