using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalExecutionAssignment;

namespace Sungero.Docflow.Server
{
  partial class ApprovalExecutionAssignmentFunctions
  {
    #region Контроль состояния
   
    /// <summary>
    /// Построить регламент.
    /// </summary>
    /// <returns>Регламент.</returns>
    [Remote(IsPure = true)]
    public Sungero.Core.StateView GetStagesStateView()
    {
      return PublicFunctions.ApprovalRuleBase.GetStagesStateView(_obj);
    }
    
    #endregion
    
    /// <summary>
    /// Определить необходимость задания на создание поручений.
    /// </summary>
    /// <param name="task">Согласование.</param>
    /// <returns>True, если нужно, иначе - false.</returns>
    public static bool NeedExecutionAssignment(IApprovalTask task)
    {
      var reviewAssignments = ApprovalReviewAssignments.GetAll()
        .Where(a => Equals(a.Task, task))
        .Where(a => a.Status == Workflow.AssignmentBase.Status.Completed);
      var hasReviewAssignments = reviewAssignments.Any();
      var hasReviewAssignmentsWithResolution = reviewAssignments
        .Any(a => a.Result == Docflow.ApprovalReviewAssignment.Result.AddResolution);
      
      // Если вынесена резолюция, то создание поручений нужно.
      if (hasReviewAssignmentsWithResolution)
        return true;
      
      // Если рассмотрение есть (без резолюции), то создание поручений не нужно.
      if (hasReviewAssignments)
        return false;
      
      // Если нет рассмотрения, но есть подписание, то создание поручений нужно.
      var hasSignAssignments = ApprovalSigningAssignments.GetAll().Any(a => Equals(a.Task, task));
      if (hasSignAssignments)
        return true;
      
      return true;
    }
    
  }
}