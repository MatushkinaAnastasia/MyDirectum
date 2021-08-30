using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PersonalSetting;

namespace Sungero.Docflow
{
  partial class PersonalSettingSharedHandlers
  {

    public virtual void IsAutoCalcResolutionAuthorChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      _obj.State.Properties.ResolutionAuthor.IsEnabled = !(e.NewValue ?? false);
      if (!_obj.State.Properties.ResolutionAuthor.IsEnabled)
        _obj.ResolutionAuthor = null;
    }

    public virtual void IsAutoCalcSupervisorChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      _obj.State.Properties.Supervisor.IsEnabled = !(e.NewValue ?? false);
      if (!_obj.State.Properties.Supervisor.IsEnabled)
        _obj.Supervisor = null;
    }

    public virtual void EmployeeChanged(Sungero.Docflow.Shared.PersonalSettingEmployeeChangedEventArgs e)
    {
      _obj.Name = e.NewValue != null ? e.NewValue.Name : null;
    }
  }
}