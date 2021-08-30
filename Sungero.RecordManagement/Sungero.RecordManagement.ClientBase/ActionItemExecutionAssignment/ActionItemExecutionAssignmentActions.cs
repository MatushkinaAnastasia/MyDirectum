using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionAssignment;

namespace Sungero.RecordManagement.Client
{
  partial class ActionItemExecutionAssignmentActions
  {
    public virtual void PrintActionItem(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var report = Reports.GetActionItemPrintReport();
      report.Task = ActionItemExecutionTasks.As(_obj.Task);
      report.Assignment = ActionItemExecutionAssignments.As(_obj);
      report.Open();
    }

    public virtual bool CanPrintActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.Task.Status.InProcess;
    }
    
    public virtual void CreateReplyLetter(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
      {
        // TODO Reshetnikov_MA переполучаем документ для обновления связей в десктоп, #71595.
        var officialDocument = Docflow.PublicFunctions.OfficialDocument.Remote.GetOfficialDocument(document.Id);
        if (officialDocument != null)
        {
          var outgoingLetter = Docflow.PublicFunctions.OfficialDocument.Remote.CreateReplyDocument(officialDocument);
          outgoingLetter.ShowModal();
          
          if (!outgoingLetter.State.IsInserted)
            _obj.ResultGroup.OfficialDocuments.Add(outgoingLetter);
        }
      }
    }

    public virtual bool CanCreateReplyLetter(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.Task.Status.InProcess;
    }

    public virtual void Done(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(ActionItemExecutionAssignments.Resources.ReportIsNotFilled);
        return;
      }
      
      if (!Functions.ActionItemExecutionAssignment.Remote.IsCoAssigneeAssignamentCreated(_obj))
      {
        Dialogs.NotifyMessage(ActionItemExecutionAssignments.Resources.AssignmentsNotCreated);
        e.Cancel();
      }
      
      var giveRights = Docflow.PublicFunctions.Module.ShowDialogGrantAccessRights(_obj, _obj.ResultGroup.All.Concat(_obj.OtherGroup.All).ToList());
      if (giveRights == false)
        e.Cancel();
      
      // Проверить наличие подчиненных поручений.
      var subActionItemExecutions = Functions.ActionItemExecutionTask.Remote.GetSubActionItemExecutions(_obj);
      if (!subActionItemExecutions.Any())
      {
        if (giveRights == null)
        {
          // Замена стандартного диалога подтверждения выполнения действия.
          if (!Docflow.PublicFunctions.Module.ShowConfirmationDialog(e.Action.ConfirmationMessage, null, null,
                                                                     Constants.ActionItemExecutionTask.ActionItemExecutionAssignmentConfirmDialogID))
            e.Cancel();
        }
      }
      else
      {
        var confirmationDialog = Dialogs.CreateTaskDialog(ActionItemExecutionTasks.Resources.StopAdditionalActionItemExecutions,
                                                          ActionItemExecutionTasks.Resources.StopAdditionalActionItemExecutionsDescription,
                                                          MessageType.Question);
        var abort = confirmationDialog.Buttons.AddCustom(ActionItemExecutionAssignments.Resources.Abort);
        confirmationDialog.Buttons.Default = abort;
        var notAbort = confirmationDialog.Buttons.AddCustom(ActionItemExecutionAssignments.Resources.NotAbort);
        confirmationDialog.Buttons.AddCancel();
        var result = confirmationDialog.Show();
        
        // Необходимость прекращения подчиненных поручений.
        if (result == abort)
          _obj.NeedAbortChildActionItems = true;
        
        // Отменить выполнения задания.
        if (result == DialogButtons.Cancel)
          e.Cancel();
      }
    }

    public virtual bool CanDone(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void RequireReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ActionItemExecutionAssignment.Remote.IsCoAssigneeAssignamentCreated(_obj))
      {
        Dialogs.NotifyMessage(ActionItemExecutionAssignments.Resources.AssignmentsNotCreated);
        return;
      }
      
      var task = Functions.StatusReportRequestTask.Remote.CreateStatusReportRequest(_obj);
      if (task == null)
        e.AddWarning(ActionItemExecutionTasks.Resources.NoActiveChildActionItems);
      else
        task.Show();
    }

    public virtual bool CanRequireReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate();
    }

    public virtual void ExtendDeadline(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var task = Sungero.Docflow.PublicFunctions.DeadlineExtensionTask.Remote.GetDeadlineExtension(_obj);
      task.Show();
    }

    public virtual bool CanExtendDeadline(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.AssignmentBase.Status.InProcess && _obj.AccessRights.CanUpdate();
    }

    public virtual void CreateChildActionItem(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var subTask = Functions.ActionItemExecutionTask.Remote.CreateActionItemExecutionFromExecution(_obj);
      subTask.Show();
    }

    public virtual bool CanCreateChildActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate();
    }

  }
}