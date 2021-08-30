using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Bank;

namespace Sungero.Parties
{
  partial class BankClientHandlers
  {

    public virtual void CorrespondentAccountValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      var result = Functions.Bank.CheckCorrLength(e.NewValue);
      if (!string.IsNullOrEmpty(result))
        e.AddError(result);
    }

    public virtual void BICValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      var result = Functions.Bank.CheckBicLength(e.NewValue);
      if (!string.IsNullOrEmpty(result))
        e.AddError(result);
    }

  }
}