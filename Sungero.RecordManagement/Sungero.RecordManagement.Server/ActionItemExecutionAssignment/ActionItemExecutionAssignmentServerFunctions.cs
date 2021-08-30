using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionAssignment;
using Sungero.RecordManagement.ActionItemExecutionTask;
using Sungero.Workflow;

namespace Sungero.RecordManagement.Server
{
  partial class ActionItemExecutionAssignmentFunctions
  {
    /// <summary>
    /// Построить модель состояния пояснения.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Модель состояния.</returns>
    [Remote(IsPure = true)]
    public static Sungero.Core.StateView GetActionItemExecutionAssignmentStateView(IActionItemExecutionAssignment assignment)
    {
      var stateView = Sungero.Core.StateView.Create();
      var block = stateView.AddBlock();
      var content = block.AddContent();
      
      content.AddLabel(GetDescription(assignment));
      
      block.ShowBorder = false;
      
      return stateView;
    }
    
    /// <summary>
    /// Получить пояснение к заданию.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Пояснение.</returns>
    private static string GetDescription(IActionItemExecutionAssignment assignment)
    {
      var description = string.Empty;
      
      var mainTask = ActionItemExecutionTasks.As(assignment.Task);
      
      if (mainTask == null)
        return description;
      
      var supervisor = mainTask.Supervisor;
      
      if (supervisor != null)
        description += (mainTask.ActionItemType == ActionItemType.Additional)
          ? RecordManagement.ActionItemExecutionTasks.Resources.OnControlWithResponsibleFormat(Sungero.Company.PublicFunctions.Employee.GetShortName(supervisor, false).TrimEnd('.'))
          : RecordManagement.ActionItemExecutionTasks.Resources.OnControlFormat(Sungero.Company.PublicFunctions.Employee.GetShortName(supervisor, false).TrimEnd('.'));
      
      if (mainTask.ActionItemType == ActionItemType.Additional)
      {
        description += RecordManagement.ActionItemExecutionTasks.Resources.YouAreAdditionalAssignee;
      }
      else
      {
        if (mainTask.ActionItemType == ActionItemType.Main && mainTask.CoAssignees.Any() && !mainTask.CoAssignees.Any(ca => Equals(ca.Assignee, assignment.Performer)))
          description += RecordManagement.ActionItemExecutionTasks.Resources.YouAreResponsibleAssignee;
        else
          description += RecordManagement.ActionItemExecutionTasks.Resources.YouAreAssignee;
      }
      
      return description;
    }
    
    /// <summary>
    /// Построить модель состояния.
    /// </summary>
    /// <returns>Модель состояния.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStateView()
    {
      var task = ActionItemExecutionTasks.As(_obj.Task);
      var additional = task.ActionItemType == ActionItemType.Additional;

      // Не выделять текущее, если задание прекращено.
      if (_obj.Status == Workflow.AssignmentBase.Status.Aborted && !additional)
      {
        var mainActionItemExecutionTask = Functions.ActionItemExecutionTask.GetMainActionItemExecutionTask(task);
        var stateViewModel = Structures.ActionItemExecutionTask.StateViewModel.Create();
        return Functions.ActionItemExecutionTask.GetActionItemStateView(mainActionItemExecutionTask, null, stateViewModel, null, string.Empty, null, null, false, true);
      }
      else
        return Functions.ActionItemExecutionTask.GetStateView(task);
    }
    
    /// <summary>
    /// Проверка, все ли задания соисполнителям созданы.
    /// </summary>
    /// <returns>True, если можно выполнять задание.</returns>
    [Remote(IsPure = true)]
    public bool IsCoAssigneeAssignamentCreated()
    {
      var task = ActionItemExecutionTasks.As(_obj.Task);
      var taskAssignees = task.CoAssignees.Select(c => c.Assignee).Distinct().ToList();
      var asgAssignees = ActionItemExecutionAssignments
        .GetAll(j => j.Task.ParentAssignment != null &&
                Equals(task, j.Task.ParentAssignment.Task) &&
                Equals(task.StartId, j.Task.ParentAssignment.TaskStartId) &&
                Equals(ActionItemExecutionTasks.As(j.Task).ActionItemType, ActionItemType.Additional))
        .Select(c => c.Performer).Distinct().ToList();
      return taskAssignees.Count == asgAssignees.Count;
    }
    
    /// <summary>
    /// Получить задания соисполнителей, не завершивших работу по поручению.
    /// </summary>
    /// <param name="entity">Поручение.</param>
    /// <returns>Задания соисполнителей, не завершивших работу.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<IActionItemExecutionAssignment> GetActionItems(IActionItemExecutionAssignment entity)
    {
      return ActionItemExecutionAssignments.GetAll(j => entity.Equals(j.Task.ParentAssignment) && j.Status == Workflow.AssignmentBase.Status.InProcess);
    }
    
    /// <summary>
    /// Получить соисполнителей, не завершивших работу по поручению.
    /// </summary>
    /// <param name="entity">Поручение.</param>
    /// <returns>Соисполнителей, не завершивших работу.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<IUser> GetActionItemsAssignees(IActionItemExecutionAssignment entity)
    {
      return GetActionItems(entity).Select(p => p.Performer);
    }
    
    /// <summary>
    /// Получить вложенные поручения соисполнителям.
    /// </summary>
    /// <param name="entity">Задание ответственного исполнителя.</param>
    /// <returns>Поручения.</returns>
    [Remote(IsPure = true)]
    public static List<IActionItemExecutionTask> GetSubActionItemExecution(IActionItemExecutionAssignment entity)
    {
      return ActionItemExecutionTasks
        .GetAll()
        .Where(j => j.Status.Value == Workflow.Task.Status.InProcess)
        .Where(j => j.ActionItemType == ActionItemType.Additional)
        .Where(j => j.ParentAssignment == entity)
        .ToList();
    }
  }
}