using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Bank;

namespace Sungero.Parties
{
  partial class BankCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      e.Without(_info.Properties.IsSystem);
      
      // При копировании системного банка комментарий не переносится.
      if (_source.IsSystem == true)
        e.Without(_info.Properties.Note);
    }
  }

}