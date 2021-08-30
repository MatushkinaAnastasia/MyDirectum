using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Shell.Client
{
  
  partial class AssignmentCompletionEmployeeGraphWidgetHandlers
  {

    public virtual void ExecuteAssignmentCompletionEmployeeGraphChartAction(Sungero.Domain.Client.ExecuteWidgetBarChartActionEventArgs e)
    {
      int employeeId;
      if (ClientApplication.ApplicationType == ApplicationType.Desktop && int.TryParse(e.ValueId, out employeeId))
      {
        var employee = Sungero.Company.PublicFunctions.Module.Remote.GetEmployeeById(employeeId);
        if (employee != null)
          Sungero.Docflow.PublicFunctions.Module.EmployeeDiscipline(employee, _parameters.Period, _parameters.Performer == Widgets.AssignmentCompletionEmployeeGraph.Performer.MyDepartment);
      }
    }
  }

  partial class AssignmentCompletionDepartmentGraphWidgetHandlers
  {

    public virtual void ExecuteAssignmentCompletionDepartmentGraphChartAction(Sungero.Domain.Client.ExecuteWidgetBarChartActionEventArgs e)
    {
      int departmentId;
      if (ClientApplication.ApplicationType == ApplicationType.Desktop && int.TryParse(e.SeriesId, out departmentId))
      {
        var department = Sungero.Company.PublicFunctions.Module.Remote.GetDepartmentById(departmentId);
        if (department != null)
          Sungero.Docflow.PublicFunctions.Module.EmployeeDiscipline(_parameters.Period, department);
      }
    }
  }

  partial class AssignmentCompletionGraphWidgetHandlers
  {

    public virtual void ExecuteAssignmentCompletionGraphAllAssignmentChartAction()
    {
      if (ClientApplication.ApplicationType == ApplicationType.Desktop)
        Docflow.PublicFunctions.Module.EmployeeDiscipline(_parameters.Performer, _parameters.Period);
    }
  }

  partial class TopLoadedDepartmentsGraphWidgetHandlers
  {

    public virtual void ExecuteTopLoadedDepartmentsGraphTopLoadedDepartmentsAction(Sungero.Domain.Client.ExecuteWidgetBarChartActionEventArgs e)
    {
      if (ClientApplication.ApplicationType == ApplicationType.Desktop)
      {
        var seriesId = e.SeriesId;
        var department = Sungero.Company.PublicFunctions.Module.Remote.GetDepartmentById(Convert.ToInt32(seriesId));
        var overdue = e.ValueId == Constants.Module.OverduedAssignments;
        
        if (department != null)
          Docflow.PublicFunctions.Module.EmployeeAssignmentPage(department, overdue, _parameters.Period);
      }
    }
  }

  partial class TopLoadedPerformersGraphWidgetHandlers
  {

    public virtual void ExecuteTopLoadedPerformersGraphTopLoadedPerformersAction(Sungero.Domain.Client.ExecuteWidgetBarChartActionEventArgs e)
    {
      if (ClientApplication.ApplicationType == ApplicationType.Desktop)
      {
        var seriesId = e.SeriesId;
        var employee = Sungero.Company.PublicFunctions.Module.Remote.GetEmployeeById(Convert.ToInt32(seriesId));
        var overdue = e.ValueId == Constants.Module.OverduedAssignments;
        
        if (employee != null)
          Docflow.PublicFunctions.Module.EmployeeAssignmentPage(employee, overdue, _parameters.Period, _parameters.CarriedObjects == Widgets.TopLoadedPerformersGraph.CarriedObjects.MyDepartment);
      }
    }
  }
}