using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.AssignmentCompletionReport
{
  /// <summary>
  /// Строчка отчета.
  /// </summary>
  partial class TableLine
  {
    public string ReportSessionId { get; set; }
    
    public int AssignmentId { get; set; }
    
    public int Performer { get; set; }
    
    public string EmployeeName { get; set; }
    
    public string JobTitle { get; set; }
    
    public string Department { get; set; }
    
    public int Task { get; set; }
    
    public string Subject { get; set; }
    
    public int Author { get; set; }
    
    public string AuthorName { get; set; }
    
    public DateTime Created { get; set; }
    
    public DateTime? Deadline { get; set; }
    
    public DateTime? Completed { get; set; }
    
    public bool IsRead { get; set; }
    
    public string Status { get; set; }
    
    public string CreatedTime { get; set; }
    
    public string DeadlineTime { get; set; }
    
    public string CompletedTime { get; set; }
    
    public string Delay { get; set; }
    
    public int DelaySort { get; set; }
    
    public string AssignmentStatus { get; set; }
    
    public string HyperLink { get; set; }
  }
}