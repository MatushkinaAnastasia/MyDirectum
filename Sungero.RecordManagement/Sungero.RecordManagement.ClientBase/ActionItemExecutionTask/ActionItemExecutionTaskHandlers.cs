using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionTask;

namespace Sungero.RecordManagement
{

  partial class ActionItemExecutionTaskActionItemPartsClientHandlers
  {

    public virtual void ActionItemPartsAssigneeValueInput(Sungero.RecordManagement.Client.ActionItemExecutionTaskActionItemPartsAssigneeValueInputEventArgs e)
    {
      if (e.NewValue != null && !Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, _obj.Deadline, Calendar.Now))
        e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday, _obj.Info.Properties.Deadline);
    }
    
    public virtual void ActionItemPartsActionItemPartValueInput(Sungero.Presentation.TextValueInputEventArgs e)
    {
      if (!string.IsNullOrEmpty(e.NewValue))
        e.NewValue = e.NewValue.Trim();
      
      var resolutionPoint = e.NewValue;
      var allowableResolutionLength = ActionItemExecutionTasks.Info.Properties.ActionItem.Length;
      if (!string.IsNullOrEmpty(resolutionPoint) && resolutionPoint.Length > allowableResolutionLength)
        e.AddError(ActionItemExecutionTasks.Resources.AllowableLengthAssignmentsCharactersFormat(allowableResolutionLength));
    }

    public virtual void ActionItemPartsNumberValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      // Проверить число на положительность.
      if (e.NewValue < 1)
        e.AddError(ActionItemExecutionTasks.Resources.NumberIsNotPositive);
    }

    public virtual void ActionItemPartsDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var assignee = _obj.Assignee ?? Users.Current;
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(assignee, e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      // Проверить корректность срока.
      if (!Docflow.PublicFunctions.Module.CheckDeadline(assignee, e.NewValue, Calendar.Now))
        e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday);
    }
  }

  partial class ActionItemExecutionTaskClientHandlers
  {

    public virtual void FinalDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      this.CheckDeadline(e, Users.Current);

      if (!Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, Calendar.Now))
        e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday);

    }

    public virtual void SupervisorValueInput(Sungero.RecordManagement.Client.ActionItemExecutionTaskSupervisorValueInputEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }

    public virtual void AssigneeValueInput(Sungero.RecordManagement.Client.ActionItemExecutionTaskAssigneeValueInputEventArgs e)
    {
      if (e.NewValue != null && !Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, _obj.Deadline, Calendar.Now))
        e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday, _obj.Info.Properties.Deadline);
      _obj.State.Controls.Control.Refresh();
    }
    
    public virtual void DeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var assignee = _obj.Assignee ?? Users.Current;
      this.CheckDeadline(e, assignee);
      // Проверить корректность срока.
      if (!Docflow.PublicFunctions.Module.CheckDeadline(assignee, e.NewValue, Calendar.Now))
        e.AddError(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      if (_obj.IsDraftResolution == true)
      {
        if (_obj.State.IsInserted)
          e.AddWarning(ActionItemExecutionTasks.Resources.WillBeAddToDocumentReviewTask);
        else
          e.AddWarning(ActionItemExecutionTasks.Resources.WillBeSentAfterApprove);
      }
      var isComponentResolution = _obj.IsCompoundActionItem ?? false;
      var isTaskStateDraft = _obj.Status == Workflow.Task.Status.Draft;

      var properties = _obj.State.Properties;
      
      properties.ActionItemParts.IsVisible = isComponentResolution;
      
      properties.Assignee.IsVisible = !isComponentResolution;
      properties.Deadline.IsVisible = !isComponentResolution;
      properties.FinalDeadline.IsVisible = isComponentResolution;
      properties.CoAssignees.IsVisible = !isComponentResolution;
      
      properties.ActionItem.IsVisible = isTaskStateDraft;
      properties.AbortingReason.IsVisible = _obj.Status == Workflow.Task.Status.Aborted;
      
      _obj.State.Attachments.ResultGroup.IsVisible = _obj.ResultGroup.All.Any();
      _obj.State.Attachments.OtherGroup.IsVisible = isTaskStateDraft || _obj.OtherGroup.All.Any();

      Functions.ActionItemExecutionTask.SetRequiredProperties(_obj);

      properties.IsUnderControl.IsEnabled = isTaskStateDraft;
      properties.Supervisor.IsEnabled = (_obj.IsUnderControl ?? false) && isTaskStateDraft;
      
      e.Title = (_obj.Subject == Docflow.Resources.AutoformatTaskSubject) ? null : _obj.Subject;
      _obj.State.Controls.Control.Refresh();
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      e.Params.AddOrUpdate(RecordManagement.Constants.ActionItemExecutionTask.WorkingWithGUI, true);
    }
    
    private void CheckDeadline(Sungero.Presentation.DateTimeValueInputEventArgs e, IUser user)
    {
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(user, e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
      
      // Предупреждение на установку даты больше даты основного поручения.
      var parentAssignment = ActionItemExecutionAssignments.As(_obj.ParentAssignment);
      if (parentAssignment != null && Docflow.PublicFunctions.Module.CheckDeadline(e.NewValue, parentAssignment.Deadline))
        e.AddWarning(ActionItemExecutionTasks.Resources.DeadlineSubActionItemExecutionFormat(parentAssignment.Deadline.Value.ToUserTime().ToShortDateString()));
      _obj.State.Controls.Control.Refresh();
    }
  }
}