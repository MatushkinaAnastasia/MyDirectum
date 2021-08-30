using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalStage;

namespace Sungero.Docflow
{
  partial class ApprovalStageClientHandlers
  {
    public virtual IEnumerable<Enumeration> ReworkTypeFiltering(IEnumerable<Enumeration> query)
    {
      if (_obj.StageType == StageType.Approvers || _obj.StageType == StageType.Manager ||
          _obj.StageType == StageType.Sign || _obj.StageType == StageType.SimpleAgr)
        return query.Where(e => !e.Equals(ReworkType.AfterComplete));
      
      return query;
    }
    
    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      if (_obj.State.IsInserted)
      {
        _obj.Name = string.Empty;
        _obj.Status = CoreEntities.DatabookEntry.Status.Closed;
      }
    }

    public virtual void DeadlineInHoursValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(ApprovalStages.Resources.IncorrectDayDeadline);
    }

    public virtual void DeadlineInDaysValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(ApprovalStages.Resources.IncorrectDayDeadline);
    }

    public virtual void StartDelayDaysValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(ApprovalStages.Resources.IncorrectStartDelayDays);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.ApprovalStage.SetPropertiesVisibility(_obj);
      Functions.ApprovalStage.SetPropertiesAvailability(_obj);
      
      if (!(_obj.State.IsInserted || _obj.State.IsCopied) && _obj.AccessRights.CanUpdate())
      {
        bool hasRules;
        if (!e.Params.Contains(Sungero.Docflow.Constants.ApprovalStage.HasRules))
          e.Params.Add(Sungero.Docflow.Constants.ApprovalStage.HasRules, Functions.ApprovalStage.Remote.HasRules(_obj));

        if (e.Params.TryGetValue(Sungero.Docflow.Constants.ApprovalStage.HasRules, out hasRules) && hasRules)
        {
          foreach (var property in _obj.State.Properties)
          {
            property.IsEnabled = false;
          }
          e.AddInformation(ApprovalStages.Resources.DisableStageProperties, _obj.Info.Actions.ChangeRequisites);
        }
        
        bool changeRequisites;
        if (e.Params.TryGetValue(Sungero.Docflow.Constants.ApprovalStage.ChangeRequisites, out changeRequisites) && changeRequisites)
          e.AddInformation(ApprovalStages.Resources.StageHasRules, _obj.Info.Actions.GetApprovalRules);
      }

      Functions.ApprovalStage.SetRequiredProperties(_obj);
      
      if (CallContext.CalledFrom(ApprovalRuleBases.Info))
        _obj.State.Properties.StageType.IsEnabled = false;
      
      var сanRegister = true;
      if (!e.Params.TryGetValue(Constants.ApprovalStage.CanRegister, out сanRegister))
      {
        сanRegister = Functions.ApprovalStage.Remote.ClerkCanRegister(_obj);
        e.Params.Add(Constants.ApprovalStage.CanRegister, сanRegister);
      }
      
      // Проверка прав регистратора.
      if (!сanRegister)
        e.AddWarning(ApprovalStages.Resources.CantRegister);
    }
  }
}