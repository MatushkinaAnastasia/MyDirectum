using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.PersonalSetting;

namespace Sungero.Docflow
{
  partial class PersonalSettingClientHandlers
  {
    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)  
    {
      _obj.State.Properties.Supervisor.IsEnabled = !(_obj.IsAutoCalcSupervisor ?? false);
      _obj.State.Properties.ResolutionAuthor.IsEnabled = !(_obj.IsAutoCalcResolutionAuthor ?? false);
    }
  }
}