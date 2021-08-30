using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.CompanyBase;

namespace Sungero.Parties
{
  partial class CompanyBaseClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      if (_obj.IsCardReadOnly == true)
        foreach (var property in _obj.State.Properties)
          property.IsEnabled = false;
    }
  }
}