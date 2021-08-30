using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ConditionBase;

namespace Sungero.Docflow
{
  partial class ConditionBaseSharedHandlers
  {
    
    public virtual void ConditionTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      Functions.ConditionBase.ChangePropertiesAccess(_obj);
      
      Functions.ConditionBase.ClearHiddenProperties(_obj);
    }
  }
}