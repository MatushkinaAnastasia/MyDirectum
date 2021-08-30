using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.RecordManagement.Structures.ActionItemExecutionTask
{
  /// <summary>
  /// Модель контрола состояния задачи на исполнение поручения: кэш статусов, задачи, задания.
  /// </summary>
  [Public]
  partial class StateViewModel
  {
    
    public System.Collections.Generic.Dictionary<Sungero.Core.Enumeration?, string> StatusesCache { get; set; }
    
    public List<IActionItemExecutionTask> Tasks { get; set; }
    
    public List<Sungero.Workflow.IAssignment> Assignments { get; set; }
    
  }
}