using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalStage;

namespace Sungero.Docflow.Client
{
  partial class ApprovalStageActions
  {
    public virtual void GetApprovalRulesWithImpossibleRoles(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.ApprovalStage.Remote.GetRulesWithImpossibleRoles(_obj).Show();
    }

    public virtual bool CanGetApprovalRulesWithImpossibleRoles(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void GetApprovalRules(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var rules = Functions.ApprovalStage.Remote.GetApprovalRules(_obj);
      rules.Show();      
    }

    public virtual void ChangeRequisites(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Открыть поля для изменения этапа согласования, если он используется в правиле.
      if (Functions.ApprovalStage.Remote.HasRules(_obj))
      {
        foreach (var property in _obj.State.Properties)
        {
          property.IsEnabled = true;
        }
        // Тип этапа и признак доп. согласующих оставить недоступными для редактирования.
        _obj.State.Properties.StageType.IsEnabled = false;
        _obj.State.Properties.AllowAdditionalApprovers.IsEnabled = false;
        
        // Очистить признак используемости в правилах.
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.ApprovalStage.HasRules, false);
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.ApprovalStage.ChangeRequisites, true);
        // HACK, BUG 28505
        ((Domain.Shared.Validation.IValidationObject)_obj).ValidationResult.Clear();
      }
    }

    public virtual bool CanChangeRequisites(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual bool CanGetApprovalRules(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }
  }
}