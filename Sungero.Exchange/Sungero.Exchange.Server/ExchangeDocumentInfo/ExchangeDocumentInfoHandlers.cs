using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ExchangeDocumentInfo;

namespace Sungero.Exchange
{
  partial class ExchangeDocumentInfoServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.RevocationStatus = RevocationStatus.None;
    }
  }

}