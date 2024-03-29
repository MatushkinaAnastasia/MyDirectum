﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentTemplate;

namespace Sungero.Docflow
{
  partial class DocumentTemplateSharedHandlers
  {

    public override void DocumentTypeChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // Очистка видов актуальна только тогда, когда тип сменился на другой.
      if (e.NewValue == e.OldValue || e.NewValue == null)
        return;
      
      _obj.DocumentKinds.Clear();
    }

  }
}