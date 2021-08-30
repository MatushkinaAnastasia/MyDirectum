using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.DocumentReviewTask;

namespace Sungero.RecordManagement
{
  partial class DocumentReviewTaskClientHandlers
  {

    public virtual void AddresseeValueInput(Sungero.RecordManagement.Client.DocumentReviewTaskAddresseeValueInputEventArgs e)
    {
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(e.NewValue, _obj.Deadline);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var canPrepareResolution = this.CanPrepareResolution();
      _obj.State.Attachments.ResolutionGroup.IsVisible = canPrepareResolution;
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      var canPrepareResolution = this.CanPrepareResolution();
      if (!canPrepareResolution)
      {
        e.HideAction(_obj.Info.Actions.AddResolution);
      }
    }
    
    private bool CanPrepareResolution()
    {
      return Company.ManagersAssistants.GetAllCached()
        .Any(x => x.Status == CoreEntities.DatabookEntry.Status.Active &&
             x.Assistant.Status == CoreEntities.DatabookEntry.Status.Active &&
             Equals(x.Assistant, Company.Employees.Current) && x.PreparesResolution == true);
    }
    
    public virtual void DeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      var warnMessage = Docflow.PublicFunctions.Module.CheckDeadlineByWorkCalendar(_obj.Addressee, e.NewValue);
      if (!string.IsNullOrEmpty(warnMessage))
        e.AddWarning(warnMessage);
    }
  }
}