using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionTask;

namespace Sungero.RecordManagement.Client
{
  internal static class ActionItemExecutionTaskStaticActions
  {
    public static void FollowUpExecution(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Reports.GetActionItemsExecutionReport().Open();
    }

    public static bool CanFollowUpExecution(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Reports.GetActionItemsExecutionReport().CanExecute();
    }
  }

  partial class ActionItemExecutionTaskActions
  {
    public virtual void ChangeCompoundMode(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.IsCompoundActionItem == true)
      {
        if (_obj.ActionItemParts.Count(a => a.Assignee != null) > 1 || _obj.ActionItemParts.Any(a => a.Deadline != null || !string.IsNullOrEmpty(a.ActionItemPart)))
        {
          var dialog = Dialogs.CreateTaskDialog(ActionItemExecutionTasks.Resources.ChangeCompoundModeQuestion,
                                                ActionItemExecutionTasks.Resources.ChangeCompoundModeDescription,
                                                MessageType.Question);
          dialog.Buttons.AddYesNo();
          dialog.Buttons.Default = DialogButtons.No;
          var yesResult = dialog.Show() == DialogButtons.Yes;
          if (yesResult)
            _obj.IsCompoundActionItem = false;
        }
        else
          _obj.IsCompoundActionItem = false;
      }
      else
        _obj.IsCompoundActionItem = true;
    }

    public virtual bool CanChangeCompoundMode(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.Task.Status.Draft;
    }

    public override void Restart(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ActionItemExecutionTask.DisablePropertiesRequirement(_obj);
      base.Restart(e);
    }

    public override bool CanRestart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanRestart(e);
    }

    public virtual void AddPerformer(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var recipients = Company.PublicFunctions.Module.GetAllActiveNoSystemGroups();
      var performer = recipients.ShowSelect(ActionItemExecutionTasks.Resources.SelectDepartmentOrRole);
      if (performer != null)
      {
        var error = Sungero.RecordManagement.PublicFunctions.ActionItemExecutionTask.Remote.SetRecipientsToAssignees(_obj, performer);
        if (error == ActionItemExecutionTasks.Resources.BigGroupWarningFormat(Constants.ActionItemExecutionTask.MaxCompoundGroup))
          Dialogs.NotifyMessage(error);
      }
    }

    public virtual bool CanAddPerformer(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.IsCompoundActionItem == true && _obj.Status == Workflow.Task.Status.Draft;
    }

    public override void CopyEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CopyEntity(e);
    }

    public override bool CanCopyEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCopyEntity(e) && _obj.IsDraftResolution != true;
    }

    public override void Start(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Functions.ActionItemExecutionTask.ValidateActionItemExecutionTaskSave(_obj, e))
        return;
      
      if (!Functions.ActionItemExecutionTask.ValidateActionItemExecutionTaskStart(_obj, e, true))
        return;
      
      if (!Docflow.PublicFunctions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj,
                                                                                            _obj.OtherGroup.All.ToList(),
                                                                                            e.Action,
                                                                                            Constants.ActionItemExecutionTask.ActionItemExecutionTaskConfirmDialogID))
        return;
      e.Params.AddOrUpdate(PublicConstants.ActionItemExecutionTask.CheckDeadline, true);
      
      base.Start(e);
    }

    public override bool CanStart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.IsDraftResolution == true ? false : base.CanStart(e);
    }

    public override void Abort(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var error = Docflow.PublicFunctions.Module.Remote.GetTaskAbortingError(_obj, Docflow.Constants.Module.TaskMainGroup.ActionItemExecutionTask.ToString());
      if (!string.IsNullOrWhiteSpace(error))
      {
        e.AddError(error);
        return;
      }
      
      var dialog = Dialogs.CreateInputDialog(ActionItemExecutionTasks.Resources.Confirmation);
      var abortingReason = dialog.AddMultilineString(_obj.Info.Properties.AbortingReason.LocalizedName, true);
      
      dialog.SetOnButtonClick(args =>
                              {
                                if (string.IsNullOrWhiteSpace(abortingReason.Value))
                                  args.AddError(ActionItemExecutionTasks.Resources.EmptyAbortingReason, abortingReason);
                              });
      
      if (dialog.Show() == DialogButtons.Ok)
      {
        _obj.AbortingReason = abortingReason.Value;
        Functions.ActionItemExecutionTask.DisablePropertiesRequirement(_obj);
        base.Abort(e);
      }
    }

    public override bool CanAbort(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() && base.CanAbort(e) && _obj.IsDraftResolution != true;
    }

    public virtual void RequireReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var task = Functions.StatusReportRequestTask.Remote.CreateStatusReportRequest(_obj);
      if (task == null)
        e.AddWarning(ActionItemExecutionTasks.Resources.NoActiveChildActionItems);
      else
        task.Show();
    }

    public virtual bool CanRequireReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Workflow.Task.Status.InProcess &&
        _obj.ExecutionState != RecordManagement.ActionItemExecutionTask.ExecutionState.Executed &&
        _obj.AccessRights.CanUpdate();
    }

  }
}