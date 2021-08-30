using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Bank;

namespace Sungero.Parties.Client
{
  partial class BankActions
  {
    public override void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var bikDuplicates = Functions.Bank.Remote.GetBanksWithSameBic(_obj, true).Where(x => !Equals(x, _obj));
      if (bikDuplicates.Any())
      {
        bikDuplicates.Show();
        return;
      }
      
      base.ShowDuplicates(e);
    }

    public override bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanShowDuplicates(e);
    }
  }
}