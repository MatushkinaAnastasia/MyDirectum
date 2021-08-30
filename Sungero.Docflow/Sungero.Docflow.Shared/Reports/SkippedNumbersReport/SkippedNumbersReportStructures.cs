using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.SkippedNumbersReport
{

  /// <summary>
  /// Пропущенный номер.
  /// </summary>
  partial class SkippedNumber
  {
    public string RegistrationNumber { get; set; }
    
    public string OrdinalNumber { get; set; }
    
    public int Index { get; set; }
    
    public string ReportSessionId { get; set; }
  }
  
  /// <summary>
  /// Доступный по правам документ.
  /// </summary>
  partial class AvailableDocument
  {
    public int Id { get; set; }
    
    public bool NumberOnFormat { get; set; }
    
    public bool CanRead { get; set; }
    
    public bool InCorrectOrder { get; set; }
    
    public string ReportSessionId { get; set; }
  }

}