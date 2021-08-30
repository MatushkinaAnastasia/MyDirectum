using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemSupervisorAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class ActionItemSupervisorAssignmentActions
  {
    public virtual bool CanAgree(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void ForRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!RecordManagement.Functions.ActionItemSupervisorAssignment.ValidateActionItemSupervisorAssignment(_obj, e))
        return;
      
      var dialogID = Constants.ActionItemExecutionTask.ActionItemSupervisorAssignmentConfirmDialogID.ForRework;
      if (Docflow.PublicFunctions.Module.CheckDeadline(ActionItemExecutionTasks.As(_obj.Task).Assignee, _obj.NewDeadline, Calendar.Now))
      {
        if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(), e.Action, dialogID))
          e.Cancel();
      }
      else
      {
        // Если срок вышел, рисуем диалог с дополнительным описанием.
        if (Docflow.PublicFunctions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList()) != false)
        {
          var description = ActionItemSupervisorAssignments.Resources.NewDeadlineLessThenTodayDescription;
          if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, description, null, dialogID))
            e.Cancel();
        }
      }
    }

    public virtual bool CanForRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void Agree(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, _obj.OtherGroup.All.ToList(), e.Action,
                                                                                            Constants.ActionItemExecutionTask.ActionItemSupervisorAssignmentConfirmDialogID.Agree))
        e.Cancel();
    }

    public virtual bool CanAgree(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }
}