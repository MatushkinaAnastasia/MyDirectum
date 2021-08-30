using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace Sungero.Shell.Server
{

  partial class MyTodayAssignmentsWidgetHandlers
  {

    public virtual IQueryable<Sungero.Workflow.IAssignment> MyTodayAssignmentsChartFiltering(IQueryable<Sungero.Workflow.IAssignment> query, Sungero.Domain.WidgetPieChartFilteringEventArgs e)
    {
      return Functions.Module.GetMyAssignments(query, _parameters.Substitution, e.ValueId);
    }
    
    public virtual void GetMyTodayAssignmentsChartValue(Sungero.Domain.GetWidgetPieChartValueEventArgs e)
    {
      AccessRights.AllowRead(
        () =>
        {
          var assignments = Workflow.Assignments.GetAll();

          var overdue = Functions.Module.GetMyAssignments(assignments, _parameters.Substitution, Constants.Module.TodayAssignments.OverdueToday).Count();
          if (overdue != 0)
            e.Chart.AddValue(Constants.Module.TodayAssignments.OverdueToday, Resources.WidgetMTAOverdue, overdue, Colors.Charts.Red);

          var deadline = Functions.Module.GetMyAssignments(assignments, _parameters.Substitution, Constants.Module.TodayAssignments.DeadlineToday).Count();
          if (deadline != 0)
            e.Chart.AddValue(Constants.Module.TodayAssignments.DeadlineToday, Resources.WidgetMTADeadline, deadline, Colors.Charts.Color1);

          var after = Functions.Module.GetMyFutureAssignments(assignments, _parameters.Substitution);
          if (after != null && after.Count != 0)
            e.Chart.AddValue(after.ConstantName, after.Resource, after.Count, Colors.Charts.Color6);

          var completed = Functions.Module.GetMyAssignments(assignments, _parameters.Substitution, Constants.Module.TodayAssignments.CompletedToday).Count();
          if (completed != 0)
            e.Chart.AddValue(Constants.Module.TodayAssignments.CompletedToday, Resources.WidgetMTACompleted, completed, Colors.Charts.Green);
        });
    }
  }

  partial class AssignmentCompletionEmployeeGraphWidgetHandlers
  {

    public virtual void GetAssignmentCompletionEmployeeGraphChartValue(Sungero.Domain.GetWidgetBarChartValueEventArgs e)
    {
      e.Chart.IsLegendVisible = false;
      var employeeDisciplineList = Functions.Module.GetEmployeeDisciplineForChart(_parameters.Performer, _parameters.Period);
      var uniqueNamesEmployeeDiscipline = Functions.Module.SetUniqueEmployeeNames(employeeDisciplineList).Take(5);
      
      foreach (var employeeDiscipline in uniqueNamesEmployeeDiscipline)
      {
        var serie = e.Chart.AddNewSeries(employeeDiscipline.EmployeeDiscipline.Employee.Id.ToString(), employeeDiscipline.UniqueName);
        serie.DisplayValueFormat = "{0}%";
        serie.AddValue(employeeDiscipline.EmployeeDiscipline.Employee.Id.ToString(), string.Empty, employeeDiscipline.EmployeeDiscipline.Discipline ?? 0);
      }
    }
  }

  partial class AssignmentCompletionDepartmentGraphWidgetHandlers
  {

    public virtual void GetAssignmentCompletionDepartmentGraphChartValue(Sungero.Domain.GetWidgetBarChartValueEventArgs e)
    {
      e.Chart.IsLegendVisible = false;
      var seriesList = Functions.Module.GetDepartmentDisciplineForChart(_parameters.Period);
      var uniqueSeriesList = Functions.Module.SetUniqueDepartmentDisciplineNames(seriesList);
      foreach (var series in uniqueSeriesList)
      {
        var departmentSeries = e.Chart.AddNewSeries(series.DepartmentDiscipline.Department.Id.ToString(), series.UniqueName);
        departmentSeries.DisplayValueFormat = "{0}%";
        departmentSeries.AddValue(series.DepartmentDiscipline.Department.Id.ToString(), RecordManagement.Resources.AssignmentCompletion, series.DepartmentDiscipline.Discipline.Value);
      }
    }
  }

  partial class AssignmentCompletionGraphWidgetHandlers
  {

    public virtual void GetAssignmentCompletionGraphAllAssignmentChartValue(Sungero.Domain.GetWidgetGaugeChartValueEventArgs e)
    {
      var value = Functions.Module.GetAssignmentCompletionStatistic(_parameters.Period, _parameters.Performer);
      
      if (value == null)
        return;
      
      var color = Functions.Module.GetAssignmentCompletionWidgetValueColor(value.Value);
      
      e.Chart.AddValue(string.Format("{0}", Sungero.RecordManagement.Resources.AssignmentCompletion), value.Value, color);
    }
  }

  partial class TasksCreatingDynamicWidgetHandlers
  {

    public virtual void GetTasksCreatingDynamicTasksDynamicChartValue(Sungero.Domain.GetWidgetPlotChartValueEventArgs e)
    {
      var period = _parameters.Period == Widgets.TasksCreatingDynamic.Period.Last90Days ? -90 :
        (_parameters.Period == Widgets.TasksCreatingDynamic.Period.Last180Days ? -180 : -30);
      var periodEnd = Sungero.Core.Calendar.Today.EndOfDay();
      var periodBegin = Sungero.Core.Calendar.Today.AddDays(period).BeginningOfDay();
      var palette = Functions.Module.GetPlotColorPalette();
      var allEmployees = Employees.GetAll();
      
      var topSeriesCount = 4;
      
      Structures.Module.ObjectCreateDynamicCache cachedTasks;
      
      if (Equals(_parameters.CarriedObjects, Widgets.TasksCreatingDynamic.CarriedObjects.All) && Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor())
      {
        // Общий кэш для аудиторов и администраторов по всем сотрудникам.
        cachedTasks = Functions.Module.GetCachedTasks(null, allEmployees.Select(x => x.Id).ToList(), _parameters.CarriedObjects);
      }
      else
      {
        // Получение списка сотрудников департамента текущего сотрудника.
        var employees = Functions.Module.FilterWidgetRecipientsBySubstitution(allEmployees,
                                                                              _parameters.CarriedObjects == Widgets.TasksCreatingDynamic.CarriedObjects.My,
                                                                              _parameters.CarriedObjects == Widgets.TasksCreatingDynamic.CarriedObjects.MyDepartment)
          .Select(x => x.Id).ToList();
        
        cachedTasks = Functions.Module.GetCachedTasks(Users.Current, employees, _parameters.CarriedObjects);
      }
      
      var tasks = cachedTasks.Points.Where(x => x.Date >= periodBegin).ToList();
      
      if (!tasks.Any())
        return;
      
      // Упорядоченный список типов. Т.к. в точках хранится накопительная сумма, значение на последний день будет максимальным, по нему и определяем топ.
      var maxDate = tasks.Max(x => x.Date);
      var topTypeList = tasks.Where(x => x.Date == maxDate).OrderByDescending(x => x.Count).Select(x => x.TypeDiscriminator).ToList();
      
      e.Chart.Axis.X.AxisType = AxisType.DateTime;
      e.Chart.Axis.Y.Title = Resources.WidgetTasksCreatingDynamicYAxisTitle;
      var maxValue = 0;
      var minValue = int.MaxValue;
      
      // Все типы.
      if (topTypeList.Any())
      {
        var iteratedPeriodBegin = periodBegin;
        var iteratedPeriodEnd = periodBegin.EndOfDay();
        var serieColor = palette.FirstOrDefault();
        var serie = e.Chart.AddNewSeries(Resources.WidgetTasksCreatingDynamicSeriesAllTasks, serieColor);

        while (iteratedPeriodEnd <= periodEnd)
        {
          var count = tasks.Where(x => x.Date == iteratedPeriodEnd.Date).Sum(x => x.Count);
          if (count > maxValue)
            maxValue = count;
          
          serie.AddValue(iteratedPeriodEnd.Date, count);
          
          iteratedPeriodBegin = iteratedPeriodBegin.AddDays(1);
          iteratedPeriodEnd = iteratedPeriodEnd.AddDays(1);
        }
      }
      
      // Топовые типы.
      var topTypes = topTypeList.Take(topSeriesCount).ToList();
      foreach (var taskType in topTypes)
      {
        var points = tasks.Where(x => Equals(x.TypeDiscriminator, taskType)).ToList();
        
        var startPoint = points.OrderBy(x => x.Date).First();
        var startCount = startPoint.Count;
        var startDate = startPoint.Date;
        
        // Dmitriev_IA: важно знать позицию, а не сам индекс. Позиция типа задачи в списке всегда на 1 больше, чем индекс.
        var taskTypeIndex = topTypes.FindIndex(t => Equals(t, taskType)) + 1;
        var serieColor = palette.Skip(taskTypeIndex).FirstOrDefault();
        
        var iteratedPeriodBegin = periodBegin;
        var iteratedPeriodEnd = periodBegin.EndOfDay();

        var typeName = string.Empty;
        
        AccessRights.AllowRead(
          () =>
          {
            var task = Workflow.Tasks.GetAll().Where(t => t.TypeDiscriminator.ToString() == taskType).First();
            typeName = Workflow.SimpleTasks.Is(task) ? Docflow.Resources.SimpleTask : task.Info.LocalizedName;
          });
        
        var serie = e.Chart.AddNewSeries(typeName, serieColor);
        
        while (iteratedPeriodEnd <= periodEnd)
        {
          var count = iteratedPeriodBegin < startDate ? 0 : startCount;
          var point = points.Where(x => x.Date == iteratedPeriodEnd.Date).FirstOrDefault();
          
          if (point != null)
            count = point.Count;
          
          serie.AddValue(iteratedPeriodEnd.Date, count);
          
          if (count < minValue)
            minValue = count;
          
          iteratedPeriodBegin = iteratedPeriodBegin.AddDays(1);
          iteratedPeriodEnd = iteratedPeriodEnd.AddDays(1);
        }
      }
      
      // Прочие типы.
      var top3Types = topTypeList.Skip(topSeriesCount).ToList();
      var otherTasks = tasks.Where(x => top3Types.Contains(x.TypeDiscriminator)).ToList();
      
      if (otherTasks.Any())
      {
        var iteratedPeriodBegin = periodBegin;
        var iteratedPeriodEnd = periodBegin.EndOfDay();
        var serieColor = palette.Skip(topSeriesCount + 1).FirstOrDefault();
        var serie = e.Chart.AddNewSeries(Resources.WidgetTasksCreatingDynamicSeriesOtherDocuments, serieColor);

        while (iteratedPeriodEnd <= periodEnd)
        {
          var count = otherTasks.Where(x => x.Date == iteratedPeriodEnd.Date).Sum(x => x.Count);

          serie.AddValue(iteratedPeriodEnd.Date, count);
          
          iteratedPeriodBegin = iteratedPeriodBegin.AddDays(1);
          iteratedPeriodEnd = iteratedPeriodEnd.AddDays(1);
        }
      }
      
      // Dmitriev_IA: Ограничение оси Oy графика
      if (minValue > Math.Round(maxValue * 0.1))
        e.Chart.Axis.Y.MinValue = Math.Round(minValue * 0.95);
      else
        e.Chart.Axis.Y.MinValue = 0;
      
      e.Chart.Axis.Y.MaxValue = Math.Round(maxValue * 1.05);
    }
  }

  partial class DocumentsCreatingDynamicWidgetHandlers
  {

    public virtual void GetDocumentsCreatingDynamicDocumentsDynamicChartValue(Sungero.Domain.GetWidgetPlotChartValueEventArgs e)
    {
      var period = _parameters.Period == Widgets.DocumentsCreatingDynamic.Period.Last90Days ? -90 :
        (_parameters.Period == Widgets.DocumentsCreatingDynamic.Period.Last180Days ? -180 : -30);
      var periodEnd = Sungero.Core.Calendar.Today.EndOfDay();
      var periodBegin = Sungero.Core.Calendar.Today.AddDays(period).BeginningOfDay();
      var palette = Functions.Module.GetPlotColorPalette();
      var allEmployees = Employees.GetAll();
      
      var topSeriesCount = 4;
      
      Structures.Module.ObjectCreateDynamicCache cachedDocuments;
      
      if (Equals(_parameters.CarriedObjects, Widgets.DocumentsCreatingDynamic.CarriedObjects.All) && Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor())
      {
        // Общий кэш для аудиторов и администраторов по всем сотрудникам.
        cachedDocuments = Functions.Module.GetCachedDocuments(null, allEmployees.Select(x => x.Id).ToList(), _parameters.CarriedObjects);
      }
      else
      {
        // Получение списка сотрудников департамента текущего сотрудника.
        var employees = Functions.Module.FilterWidgetRecipientsBySubstitution(allEmployees,
                                                                              _parameters.CarriedObjects == Widgets.DocumentsCreatingDynamic.CarriedObjects.My,
                                                                              _parameters.CarriedObjects == Widgets.DocumentsCreatingDynamic.CarriedObjects.MyDepartment)
          .Select(x => x.Id).ToList();
        
        cachedDocuments = Functions.Module.GetCachedDocuments(Users.Current, employees, _parameters.CarriedObjects);
      }
      
      var documents = cachedDocuments.Points.Where(x => x.Date >= periodBegin).ToList();
      
      if (!documents.Any())
        return;
      
      // Упорядоченный список типов. Т.к. в точках хранится накопительная сумма, значение на последний день будет максимальным, по нему и определяем топ.
      var maxDate = documents.Max(x => x.Date);
      var topTypeList = documents.Where(x => x.Date == maxDate).OrderByDescending(x => x.Count).Select(x => x.TypeDiscriminator).ToList();
      
      e.Chart.Axis.X.AxisType = AxisType.DateTime;
      e.Chart.Axis.Y.Title = Resources.WidgetDocumentsCreatingDynamicYAxisTitle;
      var maxValue = 0;
      var minValue = int.MaxValue;
      
      // Все типы.
      if (topTypeList.Any())
      {
        var iteratedPeriodBegin = periodBegin;
        var iteratedPeriodEnd = periodBegin.EndOfDay();
        var serieColor = palette.FirstOrDefault();
        var serie = e.Chart.AddNewSeries(Resources.WidgetDocumentsCreatingDynamicSeriesAllDocuments, serieColor);

        while (iteratedPeriodEnd <= periodEnd)
        {
          var count = documents.Where(x => x.Date == iteratedPeriodEnd.Date).Sum(x => x.Count);
          
          if (count > maxValue)
            maxValue = count;
          
          serie.AddValue(iteratedPeriodEnd.Date, count);
          
          iteratedPeriodBegin = iteratedPeriodBegin.AddDays(1);
          iteratedPeriodEnd = iteratedPeriodEnd.AddDays(1);
        }
      }
      
      // Топовые типы.
      var topTypes = topTypeList.Take(topSeriesCount).ToList();
      foreach (var docType in topTypes)
      {
        // Dmitriev_IA: важно знать позицию, а не сам индекс. Позиция типа документа в списке всегда на 1 больше, чем индекс.
        var docTypeIndex = topTypes.FindIndex(t => Equals(t, docType)) + 1;
        var serieColor = palette.Skip(docTypeIndex).FirstOrDefault();
        
        var points = documents.Where(x => Equals(x.TypeDiscriminator, docType)).ToList();
        
        var startPoint = points.OrderBy(x => x.Date).First();
        var startCount = startPoint.Count;
        var startDate = startPoint.Date;
        
        var iteratedPeriodBegin = periodBegin;
        var iteratedPeriodEnd = periodBegin.EndOfDay();

        var typeName = string.Empty;
        
        // Поиск локализованного названия документа в метаданных. Учитывает слои.
        Guid docTypeGuid;
        if (Guid.TryParse(docType, out docTypeGuid))
        {
          var docTypeType = Sungero.Domain.Shared.TypeExtension.GetFinalTypeGuid(docTypeGuid).GetTypeByGuid();
          if (docTypeType != null)
            typeName = docTypeType.GetEntityMetadata().GetSingularDisplayName();
        }
        
        var serie = e.Chart.AddNewSeries(typeName, serieColor);
        
        while (iteratedPeriodEnd <= periodEnd)
        {
          var count = iteratedPeriodEnd < startDate ? 0 : startCount;
          var point = points.Where(x => x.Date == iteratedPeriodEnd.Date).FirstOrDefault();
          
          if (point != null)
            count = point.Count;
          
          serie.AddValue(iteratedPeriodEnd.Date, count);
          
          if (count < minValue)
            minValue = count;
          
          iteratedPeriodBegin = iteratedPeriodBegin.AddDays(1);
          iteratedPeriodEnd = iteratedPeriodEnd.AddDays(1);
        }
      }
      
      // Прочие типы.
      var top3Types = topTypeList.Skip(topSeriesCount).ToList();
      var otherDocs = documents.Where(x => top3Types.Contains(x.TypeDiscriminator)).ToList();
      
      if (otherDocs.Any())
      {
        var iteratedPeriodBegin = periodBegin;
        var iteratedPeriodEnd = periodBegin.EndOfDay();
        var serieColor = palette.Skip(topSeriesCount + 1).FirstOrDefault();
        var serie = e.Chart.AddNewSeries(Resources.WidgetDocumentsCreatingDynamicSeriesOtherDocuments, serieColor);

        while (iteratedPeriodEnd <= periodEnd)
        {
          var count = otherDocs.Select(x => x).Where(x => x.Date == iteratedPeriodEnd.Date).Sum(x => x.Count);

          serie.AddValue(iteratedPeriodEnd.Date, count);
          
          iteratedPeriodBegin = iteratedPeriodBegin.AddDays(1);
          iteratedPeriodEnd = iteratedPeriodEnd.AddDays(1);
        }
      }
      
      // Dmitriev_IA: Ограничение оси Oy графика
      if (minValue > Math.Round(maxValue * 0.1))
        e.Chart.Axis.Y.MinValue = Math.Round(minValue * 0.95);
      else
        e.Chart.Axis.Y.MinValue = 0;
      
      e.Chart.Axis.Y.MaxValue = Math.Round(maxValue * 1.05);
    }
  }

  partial class ActiveAssignmentsDynamicWidgetHandlers
  {

    public virtual void GetActiveAssignmentsDynamicAssignmentsDynamicChartValue(Sungero.Domain.GetWidgetPlotChartValueEventArgs e)
    {
      var period = _parameters.Period == Widgets.ActiveAssignmentsDynamic.Period.Last90Days ? -90 :
        (_parameters.Period == Widgets.ActiveAssignmentsDynamic.Period.Last180Days ? -180 : -30);
      var periodEnd = Sungero.Core.Calendar.Today.EndOfDay();
      var periodBegin = Sungero.Core.Calendar.Today.AddDays(period).BeginningOfDay();
      var allEmployees = Employees.GetAll();
      
      Structures.Module.ObjectCreateDynamicCache cache;
      
      if (Equals(_parameters.CarriedObjects, Widgets.DocumentsCreatingDynamic.CarriedObjects.All) && Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor())
      {
        // Общий кэш для аудиторов и администраторов по всем сотрудникам.
        cache = Functions.Module.GetCachedActiveAssignmentDynamic(null, allEmployees.Select(x => x.Id).ToList(), _parameters.CarriedObjects);
      }
      else
      {
        // Получение списка сотрудников департамента текущего сотрудника.
        var employees = Functions.Module.FilterWidgetRecipientsBySubstitution(allEmployees,
                                                                              _parameters.CarriedObjects == Widgets.DocumentsCreatingDynamic.CarriedObjects.My,
                                                                              _parameters.CarriedObjects == Widgets.DocumentsCreatingDynamic.CarriedObjects.MyDepartment)
          .Select(x => x.Id).ToList();
        
        cache = Functions.Module.GetCachedActiveAssignmentDynamic(Users.Current, employees, _parameters.CarriedObjects);
      }
      
      var cachedPoint = cache.Points.Where(x => x.Date >= periodBegin);
      
      if (!cachedPoint.Any())
        return;
      
      e.Chart.Axis.X.AxisType = AxisType.DateTime;
      e.Chart.Axis.Y.Title = Resources.WidgetActiveAssignmentsDynamicYAxisTitle;
      
      var assignmentsInWork = e.Chart.AddNewSeries(Resources.WidgetActiveAssignmentsDynamicSeriesAllTitle, Colors.Charts.Color1);
      
      var activeAssignments = cachedPoint.Where(x => x.TypeDiscriminator == Constants.Module.ActiveAssignments).ToList();
      
      var maxValue = activeAssignments.Select(aa => aa.Count).Max();
      
      foreach (var point in activeAssignments)
        assignmentsInWork.AddValue(point.Date, point.Count);
      
      var overduedAssignmentsInWork = e.Chart.AddNewSeries(Resources.WidgetActiveAssignmentsDynamicSeriesOverduedTitle, Colors.Charts.Red);
      
      var activeOverduedAssignments = cachedPoint.Where(x => x.TypeDiscriminator == Constants.Module.OverduedAssignments).ToList();
      
      var minValue = activeOverduedAssignments.Select(aoa => aoa.Count).Min();
      
      foreach (var point in activeOverduedAssignments)
        overduedAssignmentsInWork.AddValue(point.Date, point.Count);
      
      // Dmitriev_IA: Ограничение оси Oy графика
      if (minValue > Math.Round(maxValue * 0.1))
        e.Chart.Axis.Y.MinValue = Math.Round(minValue * 0.95);
      else
        e.Chart.Axis.Y.MinValue = 0;
      
      e.Chart.Axis.Y.MaxValue = Math.Round(maxValue * 1.05);
    }
  }

  partial class TopLoadedDepartmentsGraphWidgetHandlers
  {

    public virtual void GetTopLoadedDepartmentsGraphTopLoadedDepartmentsValue(Sungero.Domain.GetWidgetBarChartValueEventArgs e)
    {
      var departmentLoads = Functions.Module.GetTopLoadedDepartaments(_parameters.Period).Take(5);
      var uniqueNameDepartmentLoads = Functions.Module.SetUniqueDepartmentNames(departmentLoads.ToList());
      
      foreach (var departmentLoad in uniqueNameDepartmentLoads)
      {
        var title = departmentLoad.UniqueName;
        var departmentSeries = e.Chart.AddNewSeries(departmentLoad.DepartmentLoad.Department.Id.ToString(), title);
        departmentSeries.AddValue(Constants.Module.OverduedAssignments,
                                  Resources.WithOverdue,
                                  departmentLoad.DepartmentLoad.OverduedAssignment, Colors.Charts.Red);
        departmentSeries.AddValue(Constants.Module.NotOverduedAssignments,
                                  Resources.WithoutOverdue,
                                  departmentLoad.DepartmentLoad.AllAssignment - departmentLoad.DepartmentLoad.OverduedAssignment, Colors.Charts.Green);
      }
    }
  }

  partial class TopLoadedPerformersGraphWidgetHandlers
  {

    public virtual void GetTopLoadedPerformersGraphTopLoadedPerformersValue(Sungero.Domain.GetWidgetBarChartValueEventArgs e)
    {
      var performerLoads = Functions.Module.GetTopLoaded(_parameters.CarriedObjects, _parameters.Period).Take(5);
      var uniqueNamedPerformerLoads = Functions.Module.SetUniquePerformerNames(performerLoads.ToList());
      
      foreach (var performerLoad in uniqueNamedPerformerLoads)
      {
        var performerSeries = e.Chart.AddNewSeries(performerLoad.PerformerLoad.Employee.Id.ToString(), performerLoad.UniqueName);
        performerSeries.AddValue(Constants.Module.OverduedAssignments,
                                 Resources.WithOverdue,
                                 performerLoad.PerformerLoad.OverduedAssignment, Colors.Charts.Red);
        performerSeries.AddValue(Constants.Module.NotOverduedAssignments,
                                 Resources.WithoutOverdue,
                                 performerLoad.PerformerLoad.AllAssignment - performerLoad.PerformerLoad.OverduedAssignment, Colors.Charts.Green);
      }
    }
  }
}