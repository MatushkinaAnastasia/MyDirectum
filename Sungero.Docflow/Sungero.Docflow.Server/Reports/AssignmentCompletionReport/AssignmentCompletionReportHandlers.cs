using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Structures.AssignmentCompletionReport;
using Sungero.Workflow;

namespace Sungero.Docflow
{
  partial class AssignmentCompletionReportServerHandlers
  {
    
    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var reportSessionId = System.Guid.NewGuid().ToString();
      AssignmentCompletionReport.ReportSessionId = reportSessionId;
      AssignmentCompletionReport.ReportDate = Calendar.Now;
      AssignmentCompletionReport.DepartmentId = AssignmentCompletionReport.Department != null ? AssignmentCompletionReport.Department.Id : 0;
      AssignmentCompletionReport.PerformerId = AssignmentCompletionReport.Performer != null ? AssignmentCompletionReport.Performer.Id : 0;
      AssignmentCompletionReport.BusinessUnitId = AssignmentCompletionReport.BusinessUnit != null ? AssignmentCompletionReport.BusinessUnit.Id : 0;
      AssignmentCompletionReport.AssignmentViewTableName = Constants.AssignmentCompletionReport.SourceTableName;
      
      #region Описание параметров
      
      if (AssignmentCompletionReport.BusinessUnit != null)
        AssignmentCompletionReport.ParamsDescription += string.Format(Docflow.Resources.ReportBusinessUnit, AssignmentCompletionReport.BusinessUnit.Name, System.Environment.NewLine);
      
      if (AssignmentCompletionReport.Department != null)
        AssignmentCompletionReport.ParamsDescription += string.Format(Docflow.Resources.ReportDepartment, AssignmentCompletionReport.Department.Name, System.Environment.NewLine);
      
      AssignmentCompletionReport.ParamsDescription += string.Format(Docflow.Resources.ReportOldAssignments,
                                                                    AssignmentCompletionReport.LoadOldAssignments == true ? Docflow.Resources.ReportYes : Docflow.Resources.ReportNo);
      
      AssignmentCompletionReport.DoneAndNotOverdue = Resources.DoneAndNotOverdue;
      AssignmentCompletionReport.DoneAndOverdue = Resources.DoneAndOverdue;
      AssignmentCompletionReport.UndoneAndNotOverdue = Resources.UndoneAndNotOverdue;
      AssignmentCompletionReport.UndoneAndOverdue = Resources.UndoneAndOverdue;
      
      #endregion
      
      var tableData = Functions.Module.GetAssignmentCompletionReportData(reportSessionId, AssignmentCompletionReport.Department, 
        AssignmentCompletionReport.Performer, AssignmentCompletionReport.PeriodBegin, AssignmentCompletionReport.PeriodEnd, AssignmentCompletionReport.LoadOldAssignments);
      
      Functions.Module.WriteStructuresToTable(AssignmentCompletionReport.AssignmentViewTableName, tableData);
    }

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Functions.Module.DeleteReportData(AssignmentCompletionReport.AssignmentViewTableName, AssignmentCompletionReport.ReportSessionId);
    }
  }
}