using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.ActionItemExecutionTask;

namespace Sungero.RecordManagement.Client
{
  partial class ActionItemExecutionTaskFunctions
  {
    /// <summary>
    /// Отключение обязательности свойств для прекращения и рестарта поручения.
    /// </summary>
    public void DisablePropertiesRequirement()
    {
      if (_obj.Assignee == null)
        _obj.State.Properties.Assignee.IsRequired = false;
      if (_obj.Deadline == null)
        _obj.State.Properties.Deadline.IsRequired = false;
    }
  }
}