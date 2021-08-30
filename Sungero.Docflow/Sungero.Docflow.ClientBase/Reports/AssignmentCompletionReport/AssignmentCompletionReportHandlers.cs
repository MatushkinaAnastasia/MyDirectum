using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;

namespace Sungero.Docflow
{
  partial class AssignmentCompletionReportClientHandlers
  {
    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      if (AssignmentCompletionReport.PeriodBegin.HasValue && AssignmentCompletionReport.PeriodEnd.HasValue)
        return;
      
      // Запросить параметры.
      var dialog = Dialogs.CreateInputDialog(Resources.AssignmentCompletionReportDialog);
      dialog.HelpCode = Constants.AssignmentCompletionReport.HelpCode;
      dialog.Buttons.AddOkCancel();
      
      CommonLibrary.IDateDialogValue periodBegin = null;
      CommonLibrary.IDateDialogValue periodEnd = null;
      INavigationDialogValue<Company.IDepartment> department = null;
      INavigationDialogValue<Company.IEmployee> performer = null;
      INavigationDialogValue<Company.IBusinessUnit> businessUnit = null;
      CommonLibrary.IBooleanDialogValue loadOldAssignments = null;
      
      // Период.
      var today = Calendar.UserToday;
      if (!AssignmentCompletionReport.PeriodBegin.HasValue)
        periodBegin = dialog.AddDate(Resources.DocumentUsagePeriodBegin, true, today.AddDays(-30));
      if (!AssignmentCompletionReport.PeriodEnd.HasValue)
        periodEnd = dialog.AddDate(Resources.DocumentUsagePeriodEnd, true, today);
      
      // НОР.
      if (AssignmentCompletionReport.BusinessUnit == null)
        businessUnit = dialog.AddSelect(Resources.BusinessUnit, false, Company.BusinessUnits.Null);
      
      // Подразделение.
      if (AssignmentCompletionReport.Department == null)
        department = dialog.AddSelect(Resources.Department, false, Company.Departments.Null);
      
      // Сотрудник.
      if (AssignmentCompletionReport.Performer == null)
        performer = dialog.AddSelect(Resources.Employee, false, Company.Employees.Null);
      
      if (AssignmentCompletionReport.LoadOldAssignments == null)
        loadOldAssignments = dialog.AddBoolean(Resources.AddAssignmentsWithDeadlineBeforePeriodBegins, true);
      
      dialog.SetOnButtonClick((args) =>
                              {
                                Functions.Module.CheckReportDialogPeriod(args, periodBegin, periodEnd);
                              });
      
      if (dialog.Show() != DialogButtons.Ok)
      {
        e.Cancel = true;
        return;
      }
      
      if (!AssignmentCompletionReport.PeriodBegin.HasValue)
        AssignmentCompletionReport.PeriodBegin = periodBegin.Value;
      if (!AssignmentCompletionReport.PeriodEnd.HasValue)
      {
        AssignmentCompletionReport.ClientPeriodEnd = periodEnd.Value.Value;
        AssignmentCompletionReport.PeriodEnd = periodEnd.Value.Value.EndOfDay();
      }
      
      if (AssignmentCompletionReport.BusinessUnit == null)
        AssignmentCompletionReport.BusinessUnit = businessUnit.Value;
      
      if (AssignmentCompletionReport.Department == null)
        AssignmentCompletionReport.Department = department.Value;
      
      if (AssignmentCompletionReport.Performer == null)
        AssignmentCompletionReport.Performer = Users.As(performer.Value);
      
      if (AssignmentCompletionReport.LoadOldAssignments == null)
        AssignmentCompletionReport.LoadOldAssignments = loadOldAssignments.Value;
    }
  }
}