using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.IncomingDocumentBase;

namespace Sungero.Docflow
{
  partial class IncomingDocumentBaseClientHandlers
  {

    public virtual void DatedValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (e.NewValue != null && e.NewValue < Calendar.SqlMinValue)
        e.AddError(_obj.Info.Properties.Dated, Sungero.Docflow.OfficialDocuments.Resources.SetCorrectDate);
    }

  }
}