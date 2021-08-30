using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Contracts;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.ApprovalStage;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using Sungero.RecordManagement;
using Sungero.Shell.Structures.Module;
using Sungero.Workflow;

namespace Sungero.Shell.Server
{
  public class ModuleFunctions
  {
    #region Виджеты
    
    #region Кэш
    
    /// <summary>
    /// Получить задания подразделения из кеша.
    /// </summary>
    /// <param name="department">Подразделение.</param>
    /// <returns>Задания подразделения.</returns>
    public static Shell.Structures.Module.EmployeeAssignmentsCache GetCachedAssignment(IDepartment department)
    {
      var key = string.Format(Shell.Constants.Module.DepartmentAssignmentsCacheKey, department.Id);
      Shell.Structures.Module.EmployeeAssignmentsCache cachedAssignmentCompletion;
      var usersId = Employees.GetAll(e => Equals(e.Department, department)).Select(e => e.Id).ToList();
      
      if (Cache.TryGetValue(key, out cachedAssignmentCompletion) && cachedAssignmentCompletion.Users.Select(u => u.UserId).OrderBy(x => x)
          .SequenceEqual(usersId.OrderBy(x => x)))
        cachedAssignmentCompletion = UpdateEmployeeAssignmentsCache(cachedAssignmentCompletion, Sungero.Shell.Constants.Module.DepartmentCache);
      else
      {
        var lightUsers = new List<LightUser>();
        foreach (var userId in usersId)
        {
          var lightUser = Shell.Structures.Module.LightUser.Create(userId, GetTimeZoneByUserId(userId));
          lightUsers.Add(lightUser);
        }
        var newCache = Shell.Structures.Module.EmployeeAssignmentsCache.Create(null, department.Id, lightUsers, null, new List<Shell.Structures.Module.CachedAssignment>(), 0, 0, 0, 0, 0, 0, false);
        cachedAssignmentCompletion = UpdateEmployeeAssignmentsCache(newCache, Sungero.Shell.Constants.Module.DepartmentCache);
      }

      if (cachedAssignmentCompletion.IsChanged)
        Cache.AddOrUpdate(key, cachedAssignmentCompletion, Calendar.Today.AddDays(90));

      return cachedAssignmentCompletion;
    }

    /// <summary>
    /// Получить задания сотрудника из кеша.
    /// </summary>
    /// <param name="recipient">Сотрудник.</param>
    /// <returns>Задания сотрудника.</returns>
    public static Shell.Structures.Module.EmployeeAssignmentsCache GetCachedAssignment(IRecipient recipient)
    {
      var key = string.Format(Shell.Constants.Module.EmployeeAssignmentsCacheKey, recipient.Id);
      Shell.Structures.Module.EmployeeAssignmentsCache cachedAssignmentCompletion;

      if (Cache.TryGetValue(key, out cachedAssignmentCompletion))
        cachedAssignmentCompletion = UpdateEmployeeAssignmentsCache(cachedAssignmentCompletion, Sungero.Shell.Constants.Module.UserCache);
      else
      {
        var lightUser = Shell.Structures.Module.LightUser.Create(recipient.Id, GetTimeZoneByUserId(recipient.Id));
        var newCache = Shell.Structures.Module.EmployeeAssignmentsCache.Create(lightUser, null, new List<LightUser>(), null, new List<Shell.Structures.Module.CachedAssignment>(), 0, 0, 0, 0, 0, 0, false);
        cachedAssignmentCompletion = UpdateEmployeeAssignmentsCache(newCache, Sungero.Shell.Constants.Module.UserCache);
      }

      if (cachedAssignmentCompletion.IsChanged)
        Cache.AddOrUpdate(key, cachedAssignmentCompletion, Calendar.Today.AddDays(90));

      return cachedAssignmentCompletion;
    }
    
    /// <summary>
    /// Получить задания из кеша.
    /// </summary>
    /// <returns>Задания.</returns>
    public static Shell.Structures.Module.EmployeeAssignmentsCache GetCachedAssignment()
    {
      var key = Shell.Constants.Module.AdviserAssignmentCacheKey;
      Shell.Structures.Module.EmployeeAssignmentsCache cachedAssignmentCompletion;
      var usersId = Employees.GetAll().Select(x => x.Id).ToList();

      if (Cache.TryGetValue(key, out cachedAssignmentCompletion) && cachedAssignmentCompletion.Users.Select(u => u.UserId).OrderBy(x => x).SequenceEqual(usersId.OrderBy(x => x)))
        cachedAssignmentCompletion = UpdateEmployeeAssignmentsCache(cachedAssignmentCompletion, Sungero.Shell.Constants.Module.AllCache);
      else
      {
        var lightUsers = new List<LightUser>();
        foreach (var userId in usersId)
        {
          var lightUser = Shell.Structures.Module.LightUser.Create(userId, GetTimeZoneByUserId(userId));
          lightUsers.Add(lightUser);
        }
        var newCache = Shell.Structures.Module.EmployeeAssignmentsCache.Create(null, null, lightUsers, null, new List<Shell.Structures.Module.CachedAssignment>(), 0, 0, 0, 0, 0, 0, false);
        cachedAssignmentCompletion = UpdateEmployeeAssignmentsCache(newCache, Sungero.Shell.Constants.Module.AllCache);
      }

      if (cachedAssignmentCompletion.IsChanged)
        Cache.AddOrUpdate(key, cachedAssignmentCompletion, Calendar.Today.AddDays(90));

      return cachedAssignmentCompletion;
    }

    private static double? GetTimeZoneByUserId(int userId)
    {
      return TimeZones.GetUtcOffsetByRecipient(Users.Get(userId));
    }

    /// <summary>
    /// Обновить кеш заданий.
    /// </summary>
    /// <param name="cache">Кеш заданий.</param>
    /// <param name="cacheType">Тип кэша.</param>
    /// <returns>Обновленный кеш заданий.</returns>
    public static Shell.Structures.Module.EmployeeAssignmentsCache UpdateEmployeeAssignmentsCache(Shell.Structures.Module.EmployeeAssignmentsCache cache, string cacheType)
    {
      Logger.DebugFormat("Start UpdateEmployeeAssignmentsCache");
      if (cache.User != null && cache.User.TimeZone != GetTimeZoneByUserId(cache.User.UserId) ||
          cache.Users.Any(x => x.TimeZone != GetTimeZoneByUserId(x.UserId)))
      {
        var newUser = cache.User != null ? Shell.Structures.Module.LightUser.Create(cache.User.UserId, GetTimeZoneByUserId(cache.User.UserId)) : null;
        var newUsersList = new List<LightUser>();
        var departmentId = cache.DepartmentId;
        foreach (var cacheUser in cache.Users)
        {
          var newCacheUser = Shell.Structures.Module.LightUser.Create(cacheUser.UserId, GetTimeZoneByUserId(cacheUser.UserId));
          newUsersList.Add(newCacheUser);
        }
        cache = Shell.Structures.Module.EmployeeAssignmentsCache.Create(newUser, departmentId,
                                                                        newUsersList, null, new List<Shell.Structures.Module.CachedAssignment>(),
                                                                        0, 0, 0, 0, 0, 0, false);
        Logger.DebugFormat("UpdateEmployeeAssignmentsCache Create New Cache");
      }
      
      var isChanged = false;
      var lastUpdate = cache.Modified ?? Calendar.Today.AddDays(-90);
      var currentUpdate = Calendar.Now;
      var oldAssignmentDate = currentUpdate.Date.AddDays(-90);

      var period30begin = currentUpdate.Date.AddDays(-30);
      
      var assignments = new List<Structures.Module.LightAssignment>();
      var createdAssignments = new List<Structures.Module.LightAssignment>();
      var modifiedAssignments = new List<Structures.Module.LightAssignment>();

      var userCache = new List<IUser>();
      
      if (lastUpdate.Date != currentUpdate.Date)
        isChanged = true;
      
      #region Получение заданий
      
      Logger.DebugFormat("UpdateEmployeeAssignmentsCache Start Get Assignments");
      
      AccessRights.AllowRead(
        () =>
        {
          var allAssignments = Workflow.Assignments.GetAll();
          Logger.DebugFormat("UpdateEmployeeAssignmentsCache AllAssignments {0}", allAssignments.Any());
          Logger.DebugFormat("UpdateEmployeeAssignmentsCache CacheType {0}", cacheType);
          if (cacheType == Sungero.Shell.Constants.Module.UserCache)
            allAssignments = allAssignments.Where(x => x.Performer.Id == cache.User.UserId);

          if (cacheType == Sungero.Shell.Constants.Module.DepartmentCache)
          {
            var userIds = cache.Users.Select(x => x.UserId).ToList();
            allAssignments = allAssignments.Where(a => userIds.Contains(a.Performer.Id));
          }
          
          if (cache.Modified == null)
          {
            assignments = Docflow.PublicFunctions.Module.FilterAssignments(allAssignments, lastUpdate, currentUpdate, Constants.Module.FilterAssignmentsMode.Default)
              .Select(x => Structures.Module.LightAssignment.Create(x.Id, x.Status, x.Deadline, x.Modified, x.Created, x.Performer.Id, null, x.Completed))
              .ToList();

            createdAssignments = assignments;
            modifiedAssignments = new List<Structures.Module.LightAssignment>();
            var performersIds = assignments.Select(a => a.PerformerId).Distinct().ToList();
            userCache = Users.GetAll(u => performersIds.Contains(u.Id)).ToList();
          }
          else
          {
            assignments = Docflow.PublicFunctions.Module.FilterAssignments(allAssignments, lastUpdate, currentUpdate, Constants.Module.FilterAssignmentsMode.Modified)
              .Select(x => Structures.Module.LightAssignment.Create(x.Id, x.Status, x.Deadline, x.Modified, x.Created, x.Performer.Id, null, x.Completed))
              .ToList();
            
            createdAssignments = assignments
              .Where(x => x.Created >= lastUpdate)
              .Where(a => (a.Status == Workflow.AssignmentBase.Status.Completed || a.Status == Workflow.AssignmentBase.Status.InProcess))
              .ToList();
            modifiedAssignments = assignments.Where(x => x.Modified >= lastUpdate && x.Created < lastUpdate).ToList();
            var performersIds = assignments.Select(a => a.PerformerId).ToList();
            userCache = Users.GetAll(u => performersIds.Contains(u.Id)).ToList();
          }
          
        });
      
      Logger.DebugFormat("UpdateEmployeeAssignmentsCache User Cache = {0}", userCache.Any());
      Logger.DebugFormat("UpdateEmployeeAssignmentsCache End Get Assignments");
      
      #endregion

      #region Добавление новых заданий
      
      Logger.DebugFormat("UpdateEmployeeAssignmentsCache Start Add Assignments");
      
      foreach (var assignment in createdAssignments)
      {
        DateTime? completed = null;
        bool? hasOverdue = null;

        if (Equals(assignment.Status, Workflow.AssignmentBase.Status.Completed))
        {
          completed = assignment.Completed;
          if (assignment.Deadline > completed || !completed.HasValue)
            hasOverdue = false;
          else
          {
            hasOverdue = Docflow.PublicFunctions.Module.CalculateDelay(assignment.Deadline, completed.Value,
                                                                       userCache.First(u => u.Id == assignment.PerformerId)) != 0;
          }
        }
        else
        {
          if (assignment.Deadline < currentUpdate)
            hasOverdue = (Docflow.PublicFunctions.Module.CalculateDelay(assignment.Deadline, currentUpdate,
                                                                        userCache.First(u => u.Id == assignment.PerformerId)) == 0) ? hasOverdue : true;
        }
        
        var asg = Shell.Structures.Module.CachedAssignment.Create(assignment.Id, assignment.Created, assignment.Deadline, completed, hasOverdue, currentUpdate, assignment.PerformerId);
        
        cache.Assignments.Add(asg);
        isChanged = true;
      }
      
      Logger.DebugFormat("UpdateEmployeeAssignmentsCache End Add Assignments");
      
      #endregion

      #region Удаление старых заданий
      
      Logger.DebugFormat("UpdateEmployeeAssignmentsCache Start Delete Old Assignments");

      var oldAssignments = cache.Assignments.Where(x => x.Completed != null && x.Completed < oldAssignmentDate).ToList();

      foreach (var assignment in oldAssignments)
      {
        cache.Assignments.Remove(assignment);
        isChanged = true;
      }
      
      Logger.DebugFormat("UpdateEmployeeAssignmentsCache End Delete Old Assignments");

      #endregion

      #region Обновление измененных заданий
      
      Logger.DebugFormat("UpdateEmployeeAssignmentsCache Start Update Modified Assignments");
      
      foreach (var assignment in modifiedAssignments)
      {
        var cachedAssignment = cache.Assignments.Where(x => x.Id == assignment.Id).SingleOrDefault();
        
        if (cachedAssignment == null)
          continue;
        
        // Если задание выполнено, считаем просрочку. Если нет - проверяем, не изменился ли срок или задание было прекращено.
        if (Equals(assignment.Status, Workflow.AssignmentBase.Status.Completed))
        {
          cachedAssignment.Completed = assignment.Completed;
          if (assignment.Deadline > assignment.Completed || !assignment.Completed.HasValue)
            cachedAssignment.HasOverdue = false;
          else
          {
            cachedAssignment.HasOverdue = Docflow.PublicFunctions.Module.CalculateDelay(assignment.Deadline, assignment.Completed.Value,
                                                                                        userCache.First(u => u.Id == assignment.PerformerId)) != 0;
          }
          
          cachedAssignment.LastUpdate = currentUpdate;
          
          isChanged = true;
        }
        else if (Equals(assignment.Status, Workflow.AssignmentBase.Status.Aborted))
        {
          cache.Assignments.Remove(cachedAssignment);
          
          isChanged = true;
        }
        else
        {
          if (cachedAssignment.Deadline != assignment.Deadline)
          {
            cachedAssignment.Deadline = assignment.Deadline;
            cachedAssignment.HasOverdue = null;
            
            cachedAssignment.LastUpdate = currentUpdate;
            
            isChanged = true;
          }
        }
      }
      
      Logger.DebugFormat("UpdateEmployeeAssignmentsCache End Update Modified Assignments");

      #endregion
      
      #region Проверка просроченности

      Logger.DebugFormat("UpdateEmployeeAssignmentsCache Start Check Overdue");
      
      var checkedCachedAssignments = cache.Assignments.Where(x => x.Deadline < currentUpdate && x.HasOverdue == null).ToList();
      var checkedPerformersIds = checkedCachedAssignments.Select(a => a.PerformerId).ToList();
      userCache = Users.GetAll(u => checkedPerformersIds.Contains(u.Id)).ToList();

      foreach (var assignment in checkedCachedAssignments)
      {
        var hasOverdue = Docflow.PublicFunctions.Module.CalculateDelay(assignment.Deadline, assignment.Completed ?? currentUpdate,
                                                                       userCache.First(u => u.Id == assignment.PerformerId)) != 0;
        
        if (!hasOverdue && assignment.Completed != null)
        {
          assignment.HasOverdue = hasOverdue;
          isChanged = true;
        }
        
        if (hasOverdue)
        {
          assignment.HasOverdue = hasOverdue;
          isChanged = true;
        }
      }
      Logger.DebugFormat("UpdateEmployeeAssignmentsCache End Check Overdue");

      #endregion
      
      #region Расчет количества заданий

      Logger.DebugFormat("UpdateEmployeeAssignmentsCache Start Add AsgCount");
      if (isChanged)
      {
        var asg30 = cache.Assignments.Where(x => x.Completed == null || x.Completed >= period30begin);

        cache.AsgCount30 = asg30.Count();
        cache.AsgInTimeCount30 = asg30.Where(x => x.Completed.HasValue && x.HasOverdue != true).Count();
        cache.AsgOverdueCount30 = asg30.Where(x => x.HasOverdue == true).Count();
        
        cache.AsgCount90 = cache.Assignments.Count();
        cache.AsgInTimeCount90 = cache.Assignments.Where(x => x.Completed.HasValue && x.HasOverdue != true).Count();
        cache.AsgOverdueCount90 = cache.Assignments.Where(x => x.HasOverdue == true).Count();
        
        cache.Modified = currentUpdate;
      }
      Logger.DebugFormat("UpdateEmployeeAssignmentsCache End Add AsgCount");

      #endregion

      #region Логирование
      
      Logger.DebugFormat("AsgCount30: {0}, AsgInTimeCount30: {1}, AsgOverdueCount30: {2}", cache.AsgCount30, cache.AsgInTimeCount30, cache.AsgOverdueCount30);
      Logger.DebugFormat("AsgCount90: {0}, AsgInTimeCount90: {1}, AsgOverdueCount90: {2}", cache.AsgCount90, cache.AsgInTimeCount90, cache.AsgOverdueCount90);
      
      #endregion
      
      cache.IsChanged = isChanged;
      
      return cache;
    }
    
    /// <summary>
    /// Получить кэш динамики активных заданий.
    /// </summary>
    /// <param name="user">Текущий пользователь.</param>
    /// <param name="usersId">Список замещаемых пользователей или сотрудников департамента.</param>
    /// <param name="authorParam">Параметр фильтрации виджета.</param>
    /// <returns>Кэш динамики создания документов.</returns>
    public static Sungero.Shell.Structures.Module.ObjectCreateDynamicCache GetCachedActiveAssignmentDynamic(IRecipient user, List<int> usersId, Enumeration authorParam)
    {
      var key = string.Empty;
      var userId = user != null ? user.Id : 0;
      
      if (user == null)
      {
        // Ключ кэша аудиторов по всем сотрудникам.
        key = Constants.Module.AsgDynamicAllCacheKeyFormat;
      }
      else
      {
        if (Equals(authorParam, Widgets.ActiveAssignmentsDynamic.CarriedObjects.MyDepartment))
        {
          // Ключ кэша департамента текущего сотрудника.
          key = string.Format(Constants.Module.AsgDynamicDepartmentCacheKeyFormat, user.Id);
        }
        else if (Equals(authorParam, Widgets.ActiveAssignmentsDynamic.CarriedObjects.My))
        {
          // Ключ кэша текущего сотрудника.
          key = string.Format(Constants.Module.AsgDynamicMyCacheKeyFormat, user.Id);
        }
        else
        {
          // Ключ кэша всех заданий текущего сотрудника.
          key = string.Format(Constants.Module.AsgDynamicEmployeeAllCacheKeyFormat, user.Id);
        }
      }
      
      Shell.Structures.Module.ObjectCreateDynamicCache cache;
      
      if (Cache.TryGetValue(key, out cache))
        cache = UpdateActiveAssignmentDynamicCache(cache, usersId, authorParam);
      else
      {
        var newCache = Shell.Structures.Module.ObjectCreateDynamicCache.Create(userId, usersId, new List<Structures.Module.PlotDatePoint>(), null, false);
        cache = UpdateActiveAssignmentDynamicCache(newCache, usersId, authorParam);
      }
      
      if (cache.IsChanged)
        Cache.AddOrUpdate(key, cache, cache.LastUpdate.Value.AddDays(180));
      
      return cache;
    }
    
    /// <summary>
    /// Обновить кэш динамики активных заданий.
    /// </summary>
    /// <param name="cache">Обновляемый кэш.</param>
    /// <param name="usersId">Список пользователей для фильтрации.</param>
    /// <param name="authorParam">Параметр фильтрации виджета.</param>
    /// <returns>Обновленный кэш.</returns>
    public static Sungero.Shell.Structures.Module.ObjectCreateDynamicCache UpdateActiveAssignmentDynamicCache(Sungero.Shell.Structures.Module.ObjectCreateDynamicCache cache, List<int> usersId, Enumeration authorParam)
    {
      cache.IsChanged = false;
      var periodEnd = Calendar.Now;
      var periodBegin = (cache.LastUpdate ?? periodEnd.AddDays(-180)).Date;
      var oldRecordDate = periodEnd.AddDays(-180).Date;
      
      // Аудитор и фильтр виджета "Все".
      var adviserAllParam = cache.EmployeeId == 0 && Equals(authorParam, Widgets.ActiveAssignmentsDynamic.CarriedObjects.All);
      
      if (!cache.UsersId.OrderBy(x => x).SequenceEqual(usersId.OrderBy(x => x)))
      {
        cache.Points.Clear();
        periodBegin = periodEnd.AddDays(-180).Date;
        cache.LastUpdate = null;
      }
      
      var allAssignments = new List<Structures.Module.LightAssignment>();
      
      AccessRights.AllowRead(
        () =>
        {
          var assignments = Workflow.Assignments.GetAll();

          // Если это не аудитор и фильтр виджета отличен от "Все" - фильтруем задания по исполнителю из списка возможных.
          if (!adviserAllParam)
            assignments = assignments.Where(d => usersId.Contains(d.Performer.Id));
          
          allAssignments = Docflow.PublicFunctions.Module.FilterAssignments(assignments, periodBegin, periodEnd, Constants.Module.FilterAssignmentsMode.Default)
            .Select(x => Structures.Module.LightAssignment.Create(x.Id, x.Status, x.Deadline, x.Modified, x.Created, x.Performer.Id, null, x.Completed))
            .ToList();
          
        });
      
      #region Добавление новых записей
      
      // Расчет фактической даты просрочки с учетом 4-х часов.
      var performersIds = allAssignments.Select(a => a.PerformerId).Distinct().ToList();
      var userCache = Users.GetAll(u => performersIds.Contains(u.Id)).ToList();

      foreach (var assignment in allAssignments)
      {
        if (!assignment.Deadline.HasValue)
        {
          assignment.FactDeadline = Calendar.SqlMaxValue;
          continue;
        }
        
        // Не рассчитывать фактическую дату просрочки для заданий, срок которых точно не наступил.
        if (assignment.Deadline.Value > periodEnd.AddDays(2))
        {
          assignment.FactDeadline = assignment.Deadline;
          continue;
        }
        // Не рассчитывать фактическую дату просрочки для заданий, которые выполнены вовремя.
        var defaultCompleted = assignment.Completed ?? Calendar.Now;
        if (defaultCompleted.AddDays(2) < assignment.Deadline.Value)
        {
          assignment.FactDeadline = assignment.Deadline;
          continue;
        }
        
        var performer = userCache.First(u => u.Id == assignment.PerformerId);
        if (assignment.Deadline.Value.HasTime())
          assignment.FactDeadline = assignment.Deadline.Value.AddWorkingHours(performer, 4);
        else
          assignment.FactDeadline = assignment.Deadline.Value.EndOfDay().FromUserTime(performer).AddWorkingHours(performer, 4);
      }
      
      var iteratedPeriodBegin = periodBegin;
      var iteratedPeriodEnd = periodBegin.EndOfDay();
      while (iteratedPeriodEnd <= periodEnd.EndOfDay())
      {
        var activeAssignments = allAssignments.Where(x => x.Created <= iteratedPeriodEnd && (Equals(x.Status, Workflow.AssignmentBase.Status.InProcess) || x.Completed >= iteratedPeriodBegin));
        
        #region Все задания

        {
          var count = activeAssignments.Count();
          
          var existPoint = cache.Points.Where(x => x.Date == iteratedPeriodEnd.Date && x.TypeDiscriminator == Constants.Module.ActiveAssignments).FirstOrDefault();
          
          if (existPoint != null)
          {
            if (existPoint.Count != count)
            {
              existPoint.Count = count;
              cache.IsChanged = true;
            }
          }
          else
          {
            cache.IsChanged = true;
            var point = new Structures.Module.PlotDatePoint();
            point.Count = count;
            point.TypeDiscriminator = Constants.Module.ActiveAssignments;
            point.Date = iteratedPeriodEnd.Date;
            
            cache.Points.Add(point);
          }
        }
        
        #endregion
        
        #region Просроченные задания
        {
          // Dmitriev_IA: Задания со сроком сегодня должны учитывать просрочку относительно текущего времени.
          if (iteratedPeriodEnd.Date == Sungero.Core.Calendar.Today)
            iteratedPeriodEnd = Sungero.Core.Calendar.Now;

          var count = activeAssignments.Where(a => Equals(a.Status, Sungero.Workflow.AssignmentBase.Status.InProcess) && a.FactDeadline < iteratedPeriodEnd ||
                                              Equals(a.Status, Sungero.Workflow.AssignmentBase.Status.Completed) && a.FactDeadline < iteratedPeriodEnd && a.FactDeadline < a.Completed.Value).Count();
          
          var existPoint = cache.Points.Where(x => x.Date == iteratedPeriodEnd.Date && x.TypeDiscriminator == Constants.Module.OverduedAssignments).FirstOrDefault();
          
          if (existPoint != null)
          {
            if (existPoint.Count != count)
            {
              existPoint.Count = count;
              cache.IsChanged = true;
            }
          }
          else
          {
            cache.IsChanged = true;
            var point = new Structures.Module.PlotDatePoint();
            point.Count = count;
            point.TypeDiscriminator = Constants.Module.OverduedAssignments;
            point.Date = iteratedPeriodEnd.Date;
            
            cache.Points.Add(point);
          }
        }
        
        #endregion
        
        iteratedPeriodBegin = iteratedPeriodBegin.AddDays(1);
        iteratedPeriodEnd = iteratedPeriodEnd.AddDays(1);
      }
      
      #endregion
      
      #region Удаление записей
      
      var deletedPoints = cache.Points.Where(x => x.Date < oldRecordDate).ToList();

      foreach (var point in deletedPoints)
      {
        cache.Points.Remove(point);
        cache.IsChanged = true;
      }
      
      #endregion
      
      cache.LastUpdate = periodEnd.Date;
      
      return cache;
    }
    
    /// <summary>
    /// Получить кэш динамики создания документов.
    /// </summary>
    /// <param name="user">Текущий пользователь.</param>
    /// <param name="usersId">Список замещаемых пользователей или сотрудников департамента.</param>
    /// <param name="authorParam">Параметр фильтрации виджета.</param>
    /// <returns>Кэш динамики создания документов.</returns>
    public static Sungero.Shell.Structures.Module.ObjectCreateDynamicCache GetCachedDocuments(IRecipient user, List<int> usersId, Enumeration authorParam)
    {
      var key = string.Empty;
      var userId = user != null ? user.Id : 0;
      
      if (user == null)
      {
        // Ключ кэша аудиторов по всем сотрудникам.
        key = Constants.Module.DocumentDynamicAllCacheKeyFormat;
      }
      else
      {
        if (Equals(authorParam, Widgets.DocumentsCreatingDynamic.CarriedObjects.MyDepartment))
        {
          // Ключ кэша департамента текущего сотрудника.
          key = string.Format(Constants.Module.DocumentDynamicDepartmentCacheKeyFormat, user.Id);
        }
        else if (Equals(authorParam, Widgets.DocumentsCreatingDynamic.CarriedObjects.My))
        {
          // Ключ кэша текущего сотрудника.
          key = string.Format(Constants.Module.DocumentDynamicMyCacheKeyFormat, user.Id);
        }
        else
        {
          // Ключ кэша всех документов текущего сотрудника.
          key = string.Format(Constants.Module.DocumentDynamicEmployeeAllCacheKeyFormat, user.Id);
        }
      }
      
      Shell.Structures.Module.ObjectCreateDynamicCache cache;
      
      if (Cache.TryGetValue(key, out cache))
        cache = UpdateObjectCreateDynamicCache(true, cache, usersId, authorParam);
      else
      {
        var newCache = Shell.Structures.Module.ObjectCreateDynamicCache.Create(userId, usersId, new List<Structures.Module.PlotDatePoint>(), null, false);
        cache = UpdateObjectCreateDynamicCache(true, newCache, usersId, authorParam);
      }
      
      if (cache.IsChanged)
        Cache.AddOrUpdate(key, cache, cache.LastUpdate.Value.AddDays(180));
      
      return cache;
    }
    
    /// <summary>
    /// Получить кэш динамики создания задач.
    /// </summary>
    /// <param name="user">Текущий пользователь.</param>
    /// <param name="usersId">Список замещаемых пользователей или сотрудников департамента.</param>
    /// <param name="authorParam">Параметр фильтрации виджета.</param>
    /// <returns>Кэш динамики создания задач.</returns>
    public static Sungero.Shell.Structures.Module.ObjectCreateDynamicCache GetCachedTasks(IRecipient user, List<int> usersId, Enumeration authorParam)
    {
      var key = string.Empty;
      var userId = user != null ? user.Id : 0;
      
      if (user == null)
      {
        // Ключ кэша аудиторов по всем сотрудникам.
        key = Constants.Module.TaskDynamicAllCacheKeyFormat;
      }
      else
      {
        if (Equals(authorParam, Widgets.TasksCreatingDynamic.CarriedObjects.MyDepartment))
        {
          // Ключ кэша департамента текущего сотрудника.
          key = string.Format(Constants.Module.TaskDynamicDepartmentCacheKeyFormat, user.Id);
        }
        else if (Equals(authorParam, Widgets.TasksCreatingDynamic.CarriedObjects.My))
        {
          // Ключ кэша текущего сотрудника.
          key = string.Format(Constants.Module.TaskDynamicMyCacheKeyFormat, user.Id);
        }
        else
        {
          // Ключ кэша всех задач текущего сотрудника.
          key = string.Format(Constants.Module.TaskDynamicEmployeeAllCacheKeyFormat, user.Id);
        }
      }
      
      Shell.Structures.Module.ObjectCreateDynamicCache cache;
      
      if (Cache.TryGetValue(key, out cache))
        cache = UpdateObjectCreateDynamicCache(false, cache, usersId, authorParam);
      else
      {
        var newCache = Shell.Structures.Module.ObjectCreateDynamicCache.Create(userId, usersId, new List<Structures.Module.PlotDatePoint>(), null, false);
        cache = UpdateObjectCreateDynamicCache(false, newCache, usersId, authorParam);
      }
      
      if (cache.IsChanged)
        Cache.AddOrUpdate(key, cache, cache.LastUpdate.Value.AddDays(180));
      
      return cache;
    }
    
    /// <summary>
    /// Обновить кэш динамики создания документов.
    /// </summary>
    /// <param name="isDocuments">Кеш по документам. Иначе по задачам.</param>
    /// <param name="cache">Обновляемый кэш.</param>
    /// <param name="usersId">Список пользователей для фильтрации.</param>
    /// <param name="authorParam">Параметр фильтрации виджета.</param>
    /// <returns>Обновленный кэш.</returns>
    public static Sungero.Shell.Structures.Module.ObjectCreateDynamicCache UpdateObjectCreateDynamicCache(bool isDocuments, Sungero.Shell.Structures.Module.ObjectCreateDynamicCache cache, List<int> usersId, Enumeration authorParam)
    {
      cache.IsChanged = false;
      var periodEnd = Calendar.Now;
      var periodBegin = cache.LastUpdate ?? periodEnd.AddDays(-180).Date;
      var oldRecordDate = periodEnd.AddDays(-180).Date;
      
      // Аудитор и фильтр виджета "Все".
      var adviserAllParam = cache.EmployeeId == 0 && Equals(authorParam, Widgets.DocumentsCreatingDynamic.CarriedObjects.All);
      
      if (!cache.UsersId.OrderBy(x => x).SequenceEqual(usersId.OrderBy(x => x)))
      {
        cache.Points.Clear();
        periodBegin = periodEnd.AddDays(-180).Date;
        cache.LastUpdate = null;
      }
      
      var allPoints = new List<Structures.Module.PlotDatePoint>();
      
      // Если кэш документов, иначе кэш задач.
      if (isDocuments)
      {
        AccessRights.AllowRead(
          () =>
          {
            var allDocuments = Content.ElectronicDocuments.GetAll().Where(t => t.Created.Between(periodBegin, periodEnd));
            
            // Если это не аудитор и фильтр виджета отличен от "Все" - фильтруем объекты по автору из списка возможных.
            if (!adviserAllParam)
              allDocuments = allDocuments.Where(d => usersId.Contains(d.Author.Id));
            
            allPoints = allDocuments.GroupBy(t => new { TypeDiscriminator = t.TypeDiscriminator.ToString(), t.Created.Value.Date })
              .Select(t => Structures.Module.PlotDatePoint.Create(t.Key.TypeDiscriminator, t.Key.Date, t.Count())).ToList();
          });
      }
      else
      {
        AccessRights.AllowRead(
          () =>
          {
            var allTasks = Workflow.Tasks.GetAll().Where(t => t.Created.Between(periodBegin, periodEnd));
            
            // Если это не аудитор и фильтр виджета отличен от "Все" - фильтруем объекты по автору из списка возможных.
            if (!adviserAllParam)
              allTasks = allTasks.Where(d => usersId.Contains(d.Author.Id));

            allPoints = allTasks.GroupBy(t => new { TypeDiscriminator = t.TypeDiscriminator.ToString(), t.Created.Value.Date })
              .Select(t => Structures.Module.PlotDatePoint.Create(t.Key.TypeDiscriminator, t.Key.Date, t.Count())).ToList();
          });
      }
      
      // Cписок типов.
      var typeList = allPoints.Select(x => x.TypeDiscriminator).Distinct().ToList();
      
      #region Добавление новых записей
      
      foreach (var type in typeList)
      {
        var startCount = 0;
        
        var objects = allPoints.Where(x => x.TypeDiscriminator == type).ToList();
        
        // Если кэш новый, берем стартовое значение из базы, если в кэше есть данные - берем значение из последней точки по данному типу.
        if (cache.LastUpdate == null)
        {
          if (isDocuments)
          {
            AccessRights.AllowRead(
              () =>
              {
                var docs = Content.ElectronicDocuments.GetAll().Where(t => t.Created < periodBegin && t.TypeDiscriminator.ToString() == type);
                
                if (!adviserAllParam)
                  docs = docs.Where(d => usersId.Contains(d.Author.Id));
                
                startCount = docs.Count();
              });
          }
          else
          {
            AccessRights.AllowRead(
              () =>
              {
                var tasks = Workflow.Tasks.GetAll().Where(t => t.Created < periodBegin && t.TypeDiscriminator.ToString() == type);
                
                if (!adviserAllParam)
                  tasks = tasks.Where(d => usersId.Contains(d.Author.Id));
                
                startCount = tasks.Count();
              });
          }
        }
        else
        {
          var lastCount = cache.Points.Where(x => x.TypeDiscriminator == type).OrderBy(x => x.Date).LastOrDefault();
          if (lastCount != null)
            startCount = lastCount.Count;
        }
        
        var iteratedPeriodBegin = periodBegin;
        var iteratedPeriodEnd = periodBegin.EndOfDay();
        
        while (iteratedPeriodEnd <= periodEnd.EndOfDay())
        {
          var datePoint = objects.Where(x => x.Date == iteratedPeriodEnd.Date).FirstOrDefault();
          
          if (datePoint != null)
            startCount += datePoint.Count;
          
          var point = cache.Points.Where(x => x.Date == iteratedPeriodEnd.Date && x.TypeDiscriminator == type).FirstOrDefault();
          
          // Если на текущий день уже существует точка.
          if (point != null)
            point.Count = startCount;
          else
          {
            point = Sungero.Shell.Structures.Module.PlotDatePoint.Create(type, iteratedPeriodEnd.Date, startCount);
            cache.Points.Add(point);
          }
          
          iteratedPeriodBegin = iteratedPeriodBegin.AddDays(1);
          iteratedPeriodEnd = iteratedPeriodEnd.AddDays(1);
        }
        cache.IsChanged = true;
      }
      
      #endregion
      
      #region Добавление точек при отсутствии данных

      // Все точки с последнего обновления.
      var lastPoints = cache.Points.Where(x => x.Date == periodBegin.Date).ToList();
      
      foreach (var lastPoint in lastPoints)
      {
        // Если в списке созданных нет этого типа - дублируем данные.
        if (!typeList.Contains(lastPoint.TypeDiscriminator))
        {
          var iteratedPeriodBegin = periodBegin.AddDays(1);
          var iteratedPeriodEnd = iteratedPeriodBegin.EndOfDay();

          while (iteratedPeriodEnd <= periodEnd.EndOfDay())
          {
            var point = Sungero.Shell.Structures.Module.PlotDatePoint.Create(lastPoint.TypeDiscriminator, iteratedPeriodEnd.Date, lastPoint.Count);
            cache.Points.Add(point);
            
            iteratedPeriodBegin = iteratedPeriodBegin.AddDays(1);
            iteratedPeriodEnd = iteratedPeriodEnd.AddDays(1);
            
            cache.IsChanged = true;
          }
        }
      }

      #endregion
      
      #region Удаление записей
      
      var deletedPoints = cache.Points.Where(x => x.Date < oldRecordDate).ToList();

      foreach (var point in deletedPoints)
      {
        cache.Points.Remove(point);
        cache.IsChanged = true;
      }
      
      #endregion
      
      cache.LastUpdate = periodEnd;
      
      return cache;
    }
    
    #endregion
    
    #region Фильтрация

    /// <summary>
    /// Отфильтровать список заданий по правам на сотрудников.
    /// </summary>
    /// <param name="assignments">Задания.</param>
    /// <param name="me">Только я.</param>
    /// <param name="department">Сотрудники моего подразделения.</param>
    /// <returns>Отфильтрованные задания.</returns>
    /// <remarks>Для виджетов тип фильтрации может быть только один.</remarks>
    [Public]
    public static IQueryable<IAssignment> FilterWidgetAssignments(IQueryable<IAssignment> assignments, bool me, bool department)
    {
      var employee = Employees.GetAll(e => Equals(Users.Current, e)).SingleOrDefault();
      var employeeDepartment = employee != null ? employee.Department : null;
      var admin = Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor();
      
      if (!admin)
      {
        var ids = Recipients.AllRecipientIds;
        assignments = assignments.Where(a => ids.Contains(a.Performer.Id));
      }
      
      if (department)
      {
        var departmentUsers = Employees.GetAll(e => Equals(e.Department, employeeDepartment));
        assignments = assignments.Where(a => departmentUsers.Contains(a.Performer));
      }
      else if (me)
      {
        assignments = assignments.Where(a => Equals(a.Performer, employee));
      }
      
      return assignments;
    }
    
    /// <summary>
    /// Отфильтровать список сотрудников по правам.
    /// </summary>
    /// <param name="employees">Сотрудники.</param>
    /// <param name="me">Только я.</param>
    /// <param name="department">Сотрудники моего подразделения.</param>
    /// <returns>Отфильтрованные сотрудники.</returns>
    /// <remarks>Для виджетов тип фильтрации может быть только один.</remarks>
    [Public]
    public static List<IEmployee> FilterWidgetRecipients(IQueryable<IEmployee> employees, bool me, bool department)
    {
      var employee = Employees.GetAll(e => Equals(Users.Current, e)).SingleOrDefault();
      var employeeDepartment = employee != null ? employee.Department : null;
      var admin = Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor();
      
      if (department)
      {
        employees = employees.Where(p => Equals(p.Department, employeeDepartment));
      }
      else if (me)
      {
        employees = employees.Where(p => Equals(p, employee));
      }
      
      return employees.ToList();
    }
    
    /// <summary>
    /// Отфильтровать список сотрудников по замещениям.
    /// </summary>
    /// <param name="employees">Сотрудники.</param>
    /// <param name="me">Только я.</param>
    /// <param name="department">Сотрудники моего подразделения.</param>
    /// <returns>Отфильтрованные сотрудники.</returns>
    /// <remarks>Для виджетов тип фильтрации может быть только один.</remarks>
    [Public]
    public static List<IEmployee> FilterWidgetRecipientsBySubstitution(IQueryable<IEmployee> employees, bool me, bool department)
    {
      var employee = Employees.GetAll(e => Equals(Users.Current, e)).SingleOrDefault();
      var employeeDepartment = employee != null ? employee.Department : null;
      var admin = Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor();
      
      if (!admin)
      {
        var ids = Recipients.AllRecipientIds;
        employees = employees.Where(a => ids.Contains(a.Id));
      }
      
      if (department)
      {
        employees = employees.Where(p => Equals(p.Department, employeeDepartment));
      }
      else if (me)
      {
        employees = employees.Where(p => Equals(p, employee));
      }
      
      return employees.ToList();
    }
    
    /// <summary>
    /// Проверить, что текущий пользователь - руководитель подразделения.
    /// </summary>
    /// <returns>Признак, что текущий пользователь является руководителем подразделения.</returns>
    public static bool IsCurrentUserDepartmentManager()
    {
      var employee = Employees.GetAll(e => Equals(Users.Current, e)).SingleOrDefault();
      var employeeDepartment = employee != null ? employee.Department : null;
      
      if (employeeDepartment == null)
        return false;
      
      return Equals(employeeDepartment.Manager, employee);
    }
    
    /// <summary>
    /// Получить подразделения, доступные в виджете.
    /// </summary>
    /// <returns>Подразделения.</returns>
    [Public]
    public static List<IDepartment> GetWidgetDepartments()
    {
      var departments = Departments.GetAll().Where(d => !Equals(d.Status, Sungero.CoreEntities.DatabookEntry.Status.Closed));
      var employee = Employees.GetAll(e => Equals(Users.Current, e)).SingleOrDefault();
      if (employee == null)
        return departments.ToList();
      
      if (!Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor())
      {
        departments = departments.Where(d => Equals(d.Manager, employee));
      }
      return departments.ToList();
    }
    
    #endregion
    
    #region Топ загруженных
    
    /// <summary>
    /// Получить загрузку сотрудников.
    /// </summary>
    /// <param name="carriedObject">Учитываемые объекты.</param>
    /// <param name="period">Период выборки заданий.</param>
    /// <returns>Список заданий сотрудников.</returns>
    /// <remarks>Remote - для того, чтобы в клиентском обработчике выдернуть исполнителя заданий.</remarks>
    [Remote(IsPure = true)]
    public static List<Structures.Module.PerformerLoad> GetTopLoaded(Enumeration carriedObject, Enumeration period)
    {
      var isTopLevelCache = false;
      var currentUpdate = Calendar.Now;
      var period30begin = currentUpdate.Date.AddDays(-30);
      
      var recipients = Shell.PublicFunctions.Module.FilterWidgetRecipientsBySubstitution(Employees.GetAll().Where(empl => !Equals(empl.Status, Sungero.CoreEntities.DatabookEntry.Status.Closed)),
                                                                                         false,
                                                                                         Equals(carriedObject, Widgets.TopLoadedPerformersGraph.CarriedObjects.MyDepartment));
      
      var statistic = new List<Structures.Module.PerformerLoad>();
      
      var cache = Structures.Module.EmployeeAssignmentsCache.Create();
      
      if (Equals(carriedObject, Widgets.AssignmentCompletionGraph.Performer.All) && Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor())
      {
        cache = GetCachedAssignment();
        isTopLevelCache = true;
      }
      else if (Equals(carriedObject, Widgets.AssignmentCompletionGraph.Performer.MyDepartment) && IsCurrentUserDepartmentManager())
      {
        cache = GetCachedAssignment(Employees.Current.Department);
        isTopLevelCache = true;
      }
      
      // Если это кэш аудитора или руководителя подразделения.
      if (isTopLevelCache)
      {
        var assignmentsByPerformer = cache.Assignments.GroupBy(x => x.PerformerId).ToList();
        
        foreach (var assignments in assignmentsByPerformer)
        {
          var allAssignments = 0;
          var overdueAssignments = 0;
          var employee = recipients.SingleOrDefault(d => d.Id == assignments.Key);
          
          if (employee == null)
            continue;
          
          if (Equals(period, Widgets.TopLoadedPerformersGraph.Period.Last30Days))
          {
            var asg30 = assignments.Where(x => x.Completed == null || x.Completed >= period30begin);

            allAssignments = asg30.Count();
            overdueAssignments = asg30.Where(x => x.HasOverdue == true).Count();
          }
          else
          {
            allAssignments = assignments.Count();
            overdueAssignments = assignments.Where(x => x.HasOverdue == true).Count();
          }
          
          if (allAssignments > 0)
            statistic.Add(Structures.Module.PerformerLoad.Create(employee, allAssignments, overdueAssignments));
        }
      }
      else
      {
        var caches = recipients.Select(r => GetCachedAssignment(r));
        
        foreach (var currentCache in caches)
        {
          var allAssignment = period == Sungero.Shell.Widgets.TopLoadedPerformersGraph.Period.Last30Days ? currentCache.AsgCount30 : currentCache.AsgCount90;
          var overduedAssignment = period == Sungero.Shell.Widgets.TopLoadedPerformersGraph.Period.Last30Days ? currentCache.AsgOverdueCount30 : currentCache.AsgOverdueCount90;
          
          if (allAssignment > 0)
            statistic.Add(Structures.Module.PerformerLoad.Create(recipients.Single(d => d.Id == currentCache.User.UserId), allAssignment, overduedAssignment));
        }
      }
      
      statistic = statistic.OrderByDescending(s => s.AllAssignment).Take(5).ToList();
      
      return statistic;
    }
    
    /// <summary>
    /// Получить загрузку подразделений.
    /// </summary>
    /// <param name="period">Период выборки заданий.</param>
    /// <returns>Список заданий департаментов.</returns>
    [Remote(IsPure = true)]
    public static List<Structures.Module.DepartmentLoad> GetTopLoadedDepartaments(Enumeration period)
    {
      var departments = GetWidgetDepartments();
      var caches = departments.Select(d => GetCachedAssignment(d));

      var statistic = new List<Structures.Module.DepartmentLoad>();
      foreach (var cache in caches)
      {
        var allAssignment = period == Sungero.Shell.Widgets.TopLoadedDepartmentsGraph.Period.Last30Days ? cache.AsgCount30 : cache.AsgCount90;
        var overduedAssignment = period == Sungero.Shell.Widgets.TopLoadedDepartmentsGraph.Period.Last30Days ? cache.AsgOverdueCount30 : cache.AsgOverdueCount90;
        
        if (allAssignment > 0)
          statistic.Add(Structures.Module.DepartmentLoad.Create(departments.Single(d => d.Id == cache.DepartmentId), allAssignment, overduedAssignment));
      }

      statistic = statistic.OrderByDescending(s => s.AllAssignment).Take(5).ToList();
      return statistic;
    }
    
    /// <summary>
    /// Получение задач для виджетов.
    /// </summary>
    /// <param name="periodBegin">Начало периода.</param>
    /// <param name="periodEnd">Конец периода.</param>
    /// <returns>Список задач.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<ITask> GetTasks(DateTime periodBegin, DateTime periodEnd)
    {
      return Workflow.Tasks.GetAll().Where(x => x.Created >= periodBegin && x.Created < periodEnd);
    }
    
    /// <summary>
    /// Получение документов для виджетов.
    /// </summary>
    /// <param name="periodBegin">Начало периода.</param>
    /// <param name="periodEnd">Конец периода.</param>
    /// <returns>Список документов.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<Content.IElectronicDocument> GetDocuments(DateTime periodBegin, DateTime periodEnd)
    {
      return Sungero.Content.ElectronicDocuments.GetAll().Where(x => x.Created >= periodBegin && x.Created < periodEnd);
    }
    
    /// <summary>
    /// Установить уникальность имен сотрудников.
    /// </summary>
    /// <param name="performerLoads">Загруженные сотрудники.</param>
    /// <returns>Загруженные сотрудники с уникальными именами, отсортированые по убыванию количества заданий.</returns>
    public static List<Shell.Structures.Module.PerformerLoadUniqueNames> SetUniquePerformerNames(List<Shell.Structures.Module.PerformerLoad> performerLoads)
    {
      var result = new List<Shell.Structures.Module.PerformerLoadUniqueNames>();
      var performerLoadsGroupByPersonName = performerLoads.GroupBy(pl => pl.Employee.Person.ShortName);
      
      foreach (var performerLoadsGroup in performerLoadsGroupByPersonName)
      {
        if (performerLoadsGroup.Count() < 1)
          continue;
        
        if (performerLoadsGroup.Count() == 1)
        {
          var uniqueNamePerformer = new Structures.Module.PerformerLoadUniqueNames();
          
          uniqueNamePerformer.UniqueName = performerLoadsGroup.Key;
          uniqueNamePerformer.PerformerLoad = performerLoadsGroup.FirstOrDefault();
          result.Add(uniqueNamePerformer);
        }
        else
        {
          var counter = 0;
          
          foreach (var performer in performerLoadsGroup)
          {
            var uniqueName = performer.Employee.Person.ShortName;
            
            for (int i = 0; i < counter; i++)
              uniqueName = string.Format("{0}*", uniqueName);
            
            var uniqueNamePerformer = new Structures.Module.PerformerLoadUniqueNames();
            
            uniqueNamePerformer.UniqueName = uniqueName;
            uniqueNamePerformer.PerformerLoad = performer;
            
            result.Add(uniqueNamePerformer);
            counter++;
          }
        }
      }
      
      result = result.OrderByDescending(r => r.PerformerLoad.AllAssignment).ToList();
      
      return result;
    }
    
    /// <summary>
    /// Установить уникальность наименований подразделений.
    /// </summary>
    /// <param name="departmentLoad">Загруженные подразделения.</param>
    /// <returns>Загруженные подразделения с уникальными именами, отсортированые по убыванию количества заданий.</returns>
    public static List<Shell.Structures.Module.DepartmentLoadUniqueNames> SetUniqueDepartmentNames(List<Shell.Structures.Module.DepartmentLoad> departmentLoad)
    {
      var result = new List<Shell.Structures.Module.DepartmentLoadUniqueNames>();
      var departmentLoadsGroupByName = departmentLoad.GroupBy(d => (d.Department.BusinessUnit != null) ?
                                                              string.Format("{0} ({1})", d.Department.Name, d.Department.BusinessUnit.Name) :
                                                              string.Format("{0}", d.Department.Name));
      
      foreach (var departmentLoadsGroup in departmentLoadsGroupByName)
      {
        if (departmentLoadsGroup.Count() < 1)
          continue;
        
        if (departmentLoadsGroup.Count() == 1)
        {
          var uniqueName = new Structures.Module.DepartmentLoadUniqueNames();
          
          uniqueName.UniqueName = departmentLoadsGroup.Key;
          uniqueName.DepartmentLoad = departmentLoadsGroup.FirstOrDefault();
          result.Add(uniqueName);
        }
        else
        {
          var counter = 0;
          foreach (var department in departmentLoadsGroup)
          {
            var uniqueName = departmentLoadsGroup.Key;
            
            for (int i = 0; i < counter; i++)
              uniqueName = string.Format("{0}*", uniqueName);
            
            var departmentLoadUnique = new Structures.Module.DepartmentLoadUniqueNames();
            
            departmentLoadUnique.UniqueName = uniqueName;
            departmentLoadUnique.DepartmentLoad = department;
            
            result.Add(departmentLoadUnique);
            counter++;
          }
        }
      }
      
      result = result.OrderByDescending(r => r.DepartmentLoad.AllAssignment).ToList();
      
      return result;
    }
    
    #endregion
    
    #region Динамика создания документов
    
    /// <summary>
    /// Получить количество созданных документов по датам.
    /// </summary>
    /// <param name="documents">Документы.</param>
    /// <param name="periodBegin">Начало периода.</param>
    /// <param name="periodEnd">Конец периода.</param>
    /// <returns>Количество созданных документов по датам.</returns>
    public List<Structures.Module.DateCountPoint> GetDocumentsCountByDates(List<Docflow.IOfficialDocument> documents, DateTime periodBegin, DateTime periodEnd)
    {
      var result = new List<Structures.Module.DateCountPoint>();
      var iteratedPeriodBegin = periodBegin;
      var iteratedPeriodEnd = periodBegin.EndOfDay();
      
      var startCount = OfficialDocuments.GetAll().Where(x => x.Created < periodBegin).Count();
      
      while (iteratedPeriodEnd <= periodEnd)
      {
        var point = new Structures.Module.DateCountPoint();
        
        point.Date = iteratedPeriodBegin.Date;
        startCount += documents.Where(d => d.Created.Between(iteratedPeriodBegin, iteratedPeriodEnd)).Count();
        point.Count = startCount;
        
        result.Add(point);
        
        iteratedPeriodBegin = iteratedPeriodBegin.AddDays(1);
        iteratedPeriodEnd = iteratedPeriodEnd.AddDays(1);
      }
      
      return result;
    }
    #endregion
    
    #region Динамика создания задач
    
    /// <summary>
    /// Получить количество созданных задач по датам.
    /// </summary>
    /// <param name="tasks">Задачи.</param>
    /// <param name="periodBegin">Начало периода.</param>
    /// <param name="periodEnd">Конец периода.</param>
    /// <returns>Количество созданных задач по датам.</returns>
    public List<Structures.Module.DateCountPoint> GetTasksContByDate(List<Sungero.Workflow.ITask> tasks, DateTime periodBegin, DateTime periodEnd)
    {
      var result = new List<Structures.Module.DateCountPoint>();
      var iteratedPeriodBegin = periodBegin;
      var iteratedPeriodEnd = periodBegin.EndOfDay();
      
      var startCount = Sungero.Workflow.Tasks.GetAll().Where(t => t.Created < periodBegin).Count();
      
      while (iteratedPeriodEnd <= periodEnd)
      {
        var point = new Structures.Module.DateCountPoint();
        
        point.Date = iteratedPeriodBegin.Date;
        startCount += tasks.Where(t => t.Created.Between(iteratedPeriodBegin, iteratedPeriodEnd)).Count();
        point.Count = startCount;
        
        result.Add(point);
        
        iteratedPeriodBegin = iteratedPeriodBegin.AddDays(1);
        iteratedPeriodEnd = iteratedPeriodEnd.AddDays(1);
      }
      
      return result;
    }
    
    #endregion
    
    #region Цвета
    
    /// <summary>
    /// Получить цвет виджета.
    /// </summary>
    /// <param name="value">Процент исполнения заданий.</param>
    /// <returns>Цвет виджета.</returns>
    public static Sungero.Core.Color GetAssignmentCompletionWidgetValueColor(int value)
    {
      if (value <= 50)
        return Colors.Charts.Red;
      else if (value <= 75)
        return Colors.Charts.Yellow;
      else
        return Colors.Charts.Green;
    }
    
    /// <summary>
    /// Получить палитру цветов для графиков.
    /// </summary>
    /// <returns>Список цветов палитры.</returns>
    public static List<Sungero.Core.Color> GetPlotColorPalette()
    {
      var palette = new List<Sungero.Core.Color>();
      
      palette.Add(Colors.Charts.Color2);
      palette.Add(Colors.Charts.Color4);
      palette.Add(Colors.Charts.Color1);
      palette.Add(Colors.Charts.Color7);
      palette.Add(Colors.Charts.Yellow);
      palette.Add(Colors.Charts.Color3);
      
      return palette;
    }
    
    #endregion
    
    #region Виджет "Исполнительская дисциплина"

    /// <summary>
    /// Получить статистику по исполнительской дисциплине.
    /// </summary>
    /// <param name="period">Период, указанный в параметрах виджета.</param>
    /// <param name="performerParam">Исполнитель, указанный в параметрах виджета.</param>
    /// <returns>Процент исполнительской дисциплины.</returns>
    public int? GetAssignmentCompletionStatistic(Enumeration period, Enumeration performerParam)
    {
      var recipients = Shell.PublicFunctions.Module.FilterWidgetRecipientsBySubstitution(Employees.GetAll(),
                                                                                         Equals(performerParam, Widgets.AssignmentCompletionGraph.Performer.My),
                                                                                         Equals(performerParam, Widgets.AssignmentCompletionGraph.Performer.MyDepartment));

      var currentInTimeCount = 0;
      var currentOverdueCount = 0;
      var totalAssignmentCount = 0;
      
      if (Equals(performerParam, Widgets.AssignmentCompletionGraph.Performer.All) && Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor())
      {
        var cache = GetCachedAssignment();
        if (period == Shell.Widgets.AssignmentCompletionGraph.Period.Last30days)
        {
          currentInTimeCount = cache.AsgInTimeCount30;
          currentOverdueCount = cache.AsgOverdueCount30;
          totalAssignmentCount = cache.AsgCount30;
        }
        else if (period == Shell.Widgets.AssignmentCompletionGraph.Period.Last90days)
        {
          currentInTimeCount = cache.AsgInTimeCount90;
          currentOverdueCount = cache.AsgOverdueCount90;
          totalAssignmentCount = cache.AsgCount90;
        }
      }
      else if (Equals(performerParam, Widgets.AssignmentCompletionGraph.Performer.MyDepartment) && IsCurrentUserDepartmentManager())
      {
        var cache = GetCachedAssignment(Employees.Current.Department);
        if (period == Shell.Widgets.AssignmentCompletionGraph.Period.Last30days)
        {
          currentInTimeCount = cache.AsgInTimeCount30;
          currentOverdueCount = cache.AsgOverdueCount30;
          totalAssignmentCount = cache.AsgCount30;
        }
        else if (period == Shell.Widgets.AssignmentCompletionGraph.Period.Last90days)
        {
          currentInTimeCount = cache.AsgInTimeCount90;
          currentOverdueCount = cache.AsgOverdueCount90;
          totalAssignmentCount = cache.AsgCount90;
        }
      }
      else
      {
        foreach (var recipient in recipients)
        {
          var cache = GetCachedAssignment(recipient);
          if (period == Shell.Widgets.AssignmentCompletionGraph.Period.Last30days)
          {
            currentInTimeCount += cache.AsgInTimeCount30;
            currentOverdueCount += cache.AsgOverdueCount30;
            totalAssignmentCount += cache.AsgCount30;
          }
          else if (period == Shell.Widgets.AssignmentCompletionGraph.Period.Last90days)
          {
            currentInTimeCount += cache.AsgInTimeCount90;
            currentOverdueCount += cache.AsgOverdueCount90;
            totalAssignmentCount += cache.AsgCount90;
          }
        }
      }

      var currentAsgCount = currentInTimeCount + currentOverdueCount;
      int currentStatistic = 0;
      
      if (currentAsgCount != 0)
        int.TryParse(Math.Round(currentInTimeCount * 100.00 / currentAsgCount).ToString(), out currentStatistic);
      else
      {
        // Если есть только непросроченные задания в работе, то исполнительская дисциплина 100%.
        if (totalAssignmentCount != 0)
          return 100;
        else
          return null;
      }

      return currentStatistic;
    }

    /// <summary>
    /// Получить Id заданий "Рассмотрение руководителем".
    /// </summary>
    /// <param name="query">Фильтруемые задания.</param>
    /// <returns>Список Id заданий.</returns>
    [Public]
    public List<int> GetReviewTaskManagerAssignments(IQueryable<Workflow.IAssignmentBase> query)
    {
      return query.Where(a => ReviewManagerAssignments.Is(a)).Select(a => a.Id).ToList();
    }
    
    #endregion
    
    #region Виджет "Исполнительская дисциплина сотрудников"
    
    /// <summary>
    /// Установить уникальность имен сотрудников.
    /// </summary>
    /// <param name="employeeDiscipline">Исполнительская дисциплина сотрудников.</param>
    /// <returns>Исполнительская дисциплина по сотрудникам с уникальными именами, отсортированным по возрастанию значения исп. дисциплины.</returns>
    public static List<Structures.Module.EmployeeDisciplineUniqueName> SetUniqueEmployeeNames(List<Structures.Module.EmployeeDiscipline> employeeDiscipline)
    {
      var result = new List<Structures.Module.EmployeeDisciplineUniqueName>();
      var employeeDisciplineGroupByPersonName = employeeDiscipline.GroupBy(ed => ed.Employee.Person.ShortName);
      
      foreach (var employeeDisciplineGroup in employeeDisciplineGroupByPersonName)
      {
        if (employeeDisciplineGroup.Count() < 1)
          continue;
        
        if (employeeDisciplineGroup.Count() == 1)
        {
          var uniqueNameEmployee = new Structures.Module.EmployeeDisciplineUniqueName();
          
          uniqueNameEmployee.UniqueName = employeeDisciplineGroup.Key;
          uniqueNameEmployee.EmployeeDiscipline = employeeDisciplineGroup.FirstOrDefault();
          result.Add(uniqueNameEmployee);
        }
        else
        {
          var counter = 0;
          
          foreach (var employee in employeeDisciplineGroup)
          {
            var uniqueName = employee.Employee.Person.ShortName;
            
            for (int i = 0; i < counter; i++)
              uniqueName = string.Format("{0}*", uniqueName);
            
            var uniqueNameEmployee = new Structures.Module.EmployeeDisciplineUniqueName();
            
            uniqueNameEmployee.UniqueName = uniqueName;
            uniqueNameEmployee.EmployeeDiscipline = employee;
            
            result.Add(uniqueNameEmployee);
            counter++;
          }
        }
      }
      
      result = result.OrderBy(r => r.EmployeeDiscipline.Discipline).ToList();
      
      return result;
    }
    
    /// <summary>
    /// Получить статистику по сотрудникам для графика.
    /// </summary>
    /// <param name="performer">Ограничение по сотрудникам.</param>
    /// <param name="period">Период.</param>
    /// <returns>Исполнительская дисциплина по сотрудникам.</returns>
    [Remote(IsPure = true)]
    public List<Structures.Module.EmployeeDiscipline> GetEmployeeDisciplineForChart(Enumeration performer, Enumeration period)
    {
      var isTopLevelCache = false;
      var currentUpdate = Calendar.Now;
      var period30begin = currentUpdate.Date.AddDays(-30);
      
      var recipients = Shell.PublicFunctions.Module.FilterWidgetRecipientsBySubstitution(Employees.GetAll().Where(empl => !Equals(empl.Status, Sungero.CoreEntities.DatabookEntry.Status.Closed)),
                                                                                         false,
                                                                                         Equals(performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.MyDepartment));
      
      var statistic = new List<Structures.Module.EmployeeDiscipline>();
      
      var cache = Structures.Module.EmployeeAssignmentsCache.Create();
      
      if (Equals(performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.All) && Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor())
      {
        cache = GetCachedAssignment();
        isTopLevelCache = true;
      }
      else if (Equals(performer, Widgets.AssignmentCompletionEmployeeGraph.Performer.MyDepartment) && IsCurrentUserDepartmentManager())
      {
        cache = GetCachedAssignment(Employees.Current.Department);
        isTopLevelCache = true;
      }

      // Если это кэш аудитора или руководителя подразделения.
      if (isTopLevelCache)
      {
        var assignmentsByPerformer = cache.Assignments.GroupBy(x => x.PerformerId).ToList();
        
        #region Расчет среднего значения просроченных заданий
        
        var avgOverdue = 0;
        
        if (Equals(period, Widgets.AssignmentCompletionEmployeeGraph.Period.Last30days))
        {
          var performersCount = assignmentsByPerformer.Count(x => x.Any(t => t.Completed == null || t.Completed >= period30begin));
          var asgCount = assignmentsByPerformer.Sum(x => x.Where(t => t.Completed == null || t.Completed >= period30begin).Where(t => t.HasOverdue == true).Count());
          if (performersCount > 0)
            avgOverdue = asgCount / performersCount / 2;
        }
        else
        {
          var performersCount = assignmentsByPerformer.Count();
          var asgCount = assignmentsByPerformer.Sum(x => x.Where(t => t.HasOverdue == true).Count());
          if (performersCount > 0)
            avgOverdue = asgCount / performersCount / 2;
        }
        
        #endregion
        
        foreach (var assignments in assignmentsByPerformer)
        {
          var totalAssignments = 0;
          var inTimeAssignments = 0;
          var overdueAssignments = 0;
          int discipline = 0;
          var employee = recipients.SingleOrDefault(d => d.Id == assignments.Key);
          
          if (employee == null)
            continue;
          
          if (Equals(period, Widgets.AssignmentCompletionEmployeeGraph.Period.Last30days))
          {
            var asg30 = assignments.Where(x => x.Completed == null || x.Completed >= period30begin);

            totalAssignments = asg30.Count();
            inTimeAssignments = asg30.Where(x => x.Completed.HasValue && x.HasOverdue != true).Count();
            overdueAssignments = asg30.Where(x => x.HasOverdue == true).Count();
          }
          else
          {
            totalAssignments = assignments.Count();
            inTimeAssignments = assignments.Where(x => x.Completed.HasValue && x.HasOverdue != true).Count();
            overdueAssignments = assignments.Where(x => x.HasOverdue == true).Count();
          }
          
          if ((inTimeAssignments + overdueAssignments) != 0)
            int.TryParse(Math.Round(inTimeAssignments * 100.00 / (inTimeAssignments + overdueAssignments)).ToString(), out discipline);
          else
          {
            // Если есть только непросроченные задания в работе, то исполнительская дисциплина 100%.
            if (totalAssignments != 0)
              discipline = 100;
            else
              continue;
          }
          
          statistic.Add(Structures.Module.EmployeeDiscipline.Create(discipline, employee, overdueAssignments));
        }
        
        // Фильтрация статистики по ТОП подразделениям с просроченными заданиями.
        var topStatistic = statistic.Where(x => x.OverdueAsg >= avgOverdue);
        if (topStatistic.Count() < 5)
          statistic = statistic.OrderByDescending(x => x.OverdueAsg).Take(5).ToList();
      }
      else
      {
        var caches = recipients.Select(r => GetCachedAssignment(r));
        
        #region Фильтруем сотрудников по среднему значению просроченных заданий
        
        var avgOverdue = 0;
        
        if (caches.Count() > 0)
        {
          if (period == Shell.Widgets.AssignmentCompletionEmployeeGraph.Period.Last30days)
          {
            avgOverdue = caches.Sum(x => x.AsgOverdueCount30) / caches.Count() / 2;
            
            var topCaches = caches.Where(x => x.AsgOverdueCount30 >= avgOverdue);
            if (topCaches.Count() < 5)
              topCaches = caches.OrderByDescending(x => x.AsgOverdueCount30).Take(5);
            
            caches = topCaches;
          }
          else if (period == Shell.Widgets.AssignmentCompletionEmployeeGraph.Period.Last90days)
          {
            avgOverdue = caches.Sum(x => x.AsgOverdueCount90) / caches.Count() / 2;
            
            var topCaches = caches.Where(x => x.AsgOverdueCount90 >= avgOverdue);
            if (topCaches.Count() < 5)
              topCaches = caches.OrderByDescending(x => x.AsgOverdueCount90).Take(5);
            
            caches = topCaches;
          }
        }
        
        #endregion

        foreach (var currentCache in caches)
        {
          int discipline = 0;
          var currentInTime = 0;
          var currentOverdue = 0;
          var currentTotal = 0;
          
          if (period == Shell.Widgets.AssignmentCompletionEmployeeGraph.Period.Last30days)
          {
            currentInTime = currentCache.AsgInTimeCount30;
            currentOverdue = currentCache.AsgOverdueCount30;
            currentTotal = currentCache.AsgCount30;
          }
          else if (period == Shell.Widgets.AssignmentCompletionEmployeeGraph.Period.Last90days)
          {
            currentInTime = currentCache.AsgInTimeCount90;
            currentOverdue = currentCache.AsgOverdueCount90;
            currentTotal = currentCache.AsgCount90;
          }
          
          if ((currentInTime + currentOverdue) != 0)
            int.TryParse(Math.Round(currentInTime * 100.00 / (currentInTime + currentOverdue)).ToString(), out discipline);
          else
          {
            // Если есть только непросроченные задания в работе, то исполнительская дисциплина 100%.
            if (currentTotal != 0)
              discipline = 100;
            else
              continue;
          }
          
          statistic.Add(Structures.Module.EmployeeDiscipline.Create(discipline, recipients.Single(d => d.Id == currentCache.User.UserId), 0));
        }
      }
      
      statistic = statistic.OrderBy(s => s.Discipline).Take(5).ToList();
      return statistic;
    }

    #endregion

    #region Виджет "Исполнительская дисциплина подразделений"

    /// <summary>
    /// Получить статистику по подразделению для графика.
    /// </summary>
    /// <param name="period">Период.</param>
    /// <returns>Исполнительская дисциплина подразделения.</returns>
    [Remote(IsPure = true)]
    public List<Structures.Module.DepartmentDiscipline> GetDepartmentDisciplineForChart(Enumeration period)
    {
      var departments = Shell.PublicFunctions.Module.GetWidgetDepartments();
      var caches = departments.Select(d => GetCachedAssignment(d));
      
      #region Фильтруем подразделения по среднему значению просроченных заданий
      
      var avgOverdue = 0;
      
      if (caches.Count() > 0)
      {
        if (period == Shell.Widgets.AssignmentCompletionDepartmentGraph.Period.Last30days)
        {
          avgOverdue = caches.Sum(x => x.AsgOverdueCount30) / caches.Count() / 2;
          
          var topCaches = caches.Where(x => x.AsgOverdueCount30 >= avgOverdue);
          if (topCaches.Count() < 5)
            topCaches = caches.OrderByDescending(x => x.AsgOverdueCount30).Take(5);
          
          caches = topCaches;
        }
        else if (period == Shell.Widgets.AssignmentCompletionDepartmentGraph.Period.Last90days)
        {
          avgOverdue = caches.Sum(x => x.AsgOverdueCount90) / caches.Count() / 2;
          
          var topCaches = caches.Where(x => x.AsgOverdueCount90 >= avgOverdue);
          if (topCaches.Count() < 5)
            topCaches = caches.OrderByDescending(x => x.AsgOverdueCount90).Take(5);
          
          caches = topCaches;
        }
      }
      
      #endregion

      var statistic = new List<Structures.Module.DepartmentDiscipline>();
      foreach (var cache in caches)
      {
        int discipline = 0;
        var currentInTime = 0;
        var currentOverdue = 0;
        var currentTotal = 0;
        
        if (period == Shell.Widgets.AssignmentCompletionDepartmentGraph.Period.Last30days)
        {
          currentInTime = cache.AsgInTimeCount30;
          currentOverdue = cache.AsgOverdueCount30;
          currentTotal = cache.AsgCount30;
        }
        else if (period == Shell.Widgets.AssignmentCompletionDepartmentGraph.Period.Last90days)
        {
          currentInTime = cache.AsgInTimeCount90;
          currentOverdue = cache.AsgOverdueCount90;
          currentTotal = cache.AsgCount90;
        }
        
        if ((currentInTime + currentOverdue) != 0)
          int.TryParse(Math.Round(currentInTime * 100.00 / (currentInTime + currentOverdue)).ToString(), out discipline);
        else
        {
          // Если есть только непросроченные задания в работе, то исполнительская дисциплина 100%.
          if (currentTotal != 0)
            discipline = 100;
          else
            continue;
        }
        
        statistic.Add(Structures.Module.DepartmentDiscipline.Create(discipline, departments.Single(d => d.Id == cache.DepartmentId)));
      }

      statistic = statistic.OrderBy(s => s.Discipline).Take(5).ToList();
      return statistic;
    }
    
    /// <summary>
    /// Установить уникальность наименований подразделений.
    /// </summary>
    /// <param name="departmentDiscipline">Исполнительская дисциплина подразделения.</param>
    /// <returns>Загруженные подразделения с уникальными именами, отсортированные по возрастанию значения исп. дисциплины.</returns>
    public static List<Shell.Structures.Module.DepartmentDisciplineUniqueName> SetUniqueDepartmentDisciplineNames(List<Shell.Structures.Module.DepartmentDiscipline> departmentDiscipline)
    {
      var result = new List<Shell.Structures.Module.DepartmentDisciplineUniqueName>();
      var departmentGroupByName = departmentDiscipline.GroupBy(d => (d.Department.BusinessUnit != null) ?
                                                               string.Format("{0} ({1})", d.Department.Name, d.Department.BusinessUnit.Name) :
                                                               string.Format("{0}", d.Department.Name));
      
      foreach (var departmentGroup in departmentGroupByName)
      {
        if (departmentGroup.Count() < 1)
          continue;
        
        if (departmentGroup.Count() == 1)
        {
          var uniqueName = new Structures.Module.DepartmentDisciplineUniqueName();
          
          uniqueName.UniqueName = departmentGroup.Key;
          uniqueName.DepartmentDiscipline = departmentGroup.FirstOrDefault();
          result.Add(uniqueName);
        }
        else
        {
          var counter = 0;
          foreach (var department in departmentGroup)
          {
            var uniqueName = departmentGroup.Key;
            
            for (int i = 0; i < counter; i++)
              uniqueName = string.Format("{0}*", uniqueName);
            
            var departmentUnique = new Structures.Module.DepartmentDisciplineUniqueName();
            
            departmentUnique.UniqueName = uniqueName;
            departmentUnique.DepartmentDiscipline = department;
            
            result.Add(departmentUnique);
            counter++;
          }
        }
      }
      
      result = result.OrderBy(r => r.DepartmentDiscipline.Discipline).ToList();
      
      return result;
    }

    #endregion
    
    #region "Мои задания на сегодня"
    
    /// <summary>
    /// Получить мои задания по фильтру.
    /// </summary>
    /// <param name="query">Фильтруемые задания.</param>
    /// <param name="withSubstitution">С замещением.</param>
    /// <param name="value">Строковое обозначение серии.</param>
    /// <returns>Задания.</returns>
    public static IQueryable<IAssignment> GetMyAssignments(IQueryable<IAssignment> query, bool withSubstitution, string value)
    {
      query = Functions.Module.FilterMyTodayAssignments(query, withSubstitution);
      
      var userNow = Calendar.UserNow;
      var userBeginOfToday = userNow.BeginningOfDay();
      var userTodayEndOfWeek = userBeginOfToday.EndOfWeek().BeginningOfDay();
      var userTodayEndOfMonth = userBeginOfToday.EndOfMonth().BeginningOfDay();
      var userBeginOfDay = userBeginOfToday.AddMilliseconds(1);
      var userEndOfDay = userNow.EndOfDay();
      var userEndOfWeek = userNow.EndOfWeek();
      var userEndOfMonth = userNow.EndOfMonth();

      var serverNow = userNow.FromUserTime();
      var serverBeginOfToday = userBeginOfToday.FromUserTime();
      var serverTodayEndOfWeek = userTodayEndOfWeek.FromUserTime();
      var serverTodayEndOfMonth = userTodayEndOfMonth.FromUserTime();
      var serverBeginOfDay = userBeginOfDay.FromUserTime();
      var serverEndOfDay = userEndOfDay.FromUserTime();
      var serverEndOfWeek = userEndOfWeek.FromUserTime();
      var serverEndOfMonth = userEndOfMonth.FromUserTime();
      
      if (value == Constants.Module.TodayAssignments.CompletedToday)
      {
        query = query.Where(a => a.Completed.HasValue && a.Completed.Between(serverBeginOfDay, serverNow));
      }
      else
      {
        // Задания, которые ещё не выполнены.
        query = query.Where(a => a.Deadline.HasValue && Equals(a.Status, Workflow.AssignmentBase.Status.InProcess));
        
        if (value == Constants.Module.TodayAssignments.DeadlineToday)
        {
          query = query.Where(a => Equals(a.Deadline, serverBeginOfToday) ||
                              (a.Deadline.Between(serverNow, serverEndOfDay) && a.Deadline != a.Deadline.Value.Date));
        }
        else if (value == Constants.Module.TodayAssignments.OverdueToday)
        {
          query = query.Where(a => !Equals(a.Deadline, serverBeginOfToday) &&
                              (a.Deadline < serverNow && a.Deadline != a.Deadline.Value.Date ||
                               a.Deadline < serverEndOfDay.Date && a.Deadline == a.Deadline.Value.Date));
        }
        else
        {
          // Задания со сроком больше, чем сегодня.
          query = query.Where(a => (a.Deadline > serverBeginOfToday && a.Deadline == a.Deadline.Value.Date) ||
                              (a.Deadline > serverEndOfDay && a.Deadline != a.Deadline.Value.Date));
          
          if (value == Constants.Module.TodayAssignments.DeadlineTomorrow)
          {
            query = query.Where(a => Equals(a.Deadline, serverBeginOfToday.AddDays(1)) ||
                                (a.Deadline <= serverEndOfDay.AddDays(1) && a.Deadline != a.Deadline.Value.Date));
          }
          if (value == Constants.Module.TodayAssignments.AfterTomorrow)
          {
            query = query.Where(a => Equals(a.Deadline, serverBeginOfToday.AddDays(2)) ||
                                (a.Deadline <= serverEndOfDay.AddDays(2) && a.Deadline != a.Deadline.Value.Date));
          }
          if (value == Constants.Module.TodayAssignments.EndOfWeek)
          {
            query = query.Where(a => (a.Deadline <= serverTodayEndOfWeek && a.Deadline == a.Deadline.Value.Date) ||
                                (a.Deadline <= serverEndOfWeek && a.Deadline != a.Deadline.Value.Date));
          }
          if (value == Constants.Module.TodayAssignments.NextEndOfWeek)
          {
            query = query.Where(a => (a.Deadline <= serverTodayEndOfWeek.AddDays(7) && a.Deadline == a.Deadline.Value.Date) ||
                                (a.Deadline <= serverEndOfWeek.AddDays(7) && a.Deadline != a.Deadline.Value.Date));
          }
          if (value == Constants.Module.TodayAssignments.EndOfMonth)
          {
            query = query.Where(a => (a.Deadline <= serverTodayEndOfMonth && a.Deadline == a.Deadline.Value.Date)
                                || (a.Deadline <= serverEndOfMonth && a.Deadline != a.Deadline.Value.Date));
          }
        }
      }
      
      return query;
    }
    
    /// <summary>
    /// Получить информацию по ближайшим заданиям.
    /// </summary>
    /// <param name="query">Фильтруемые задания.</param>
    /// <param name="withSubstitution">С замещением.</param>
    /// <returns>Информация для графика.</returns>
    public static Structures.Module.AssignmentChartGroup GetMyFutureAssignments(IQueryable<Workflow.IAssignment> query, bool withSubstitution)
    {
      query = Functions.Module.FilterMyTodayAssignments(query, withSubstitution);
      
      var userNow = Calendar.UserNow;
      var userBeginOfToday = userNow.BeginningOfDay();
      var userTodayEndOfWeek = userBeginOfToday.EndOfWeek().BeginningOfDay();
      var userTodayEndOfMonth = userBeginOfToday.EndOfMonth().BeginningOfDay();
      var userEndOfDay = userNow.EndOfDay();
      var userEndOfWeek = userNow.EndOfWeek();
      var userEndOfMonth = userNow.EndOfMonth();

      var serverBeginOfToday = userBeginOfToday.FromUserTime();
      var serverTodayEndOfWeek = userTodayEndOfWeek.FromUserTime();
      var serverTodayEndOfMonth = userTodayEndOfMonth.FromUserTime();
      var serverEndOfDay = userEndOfDay.FromUserTime();
      var serverEndOfWeek = userEndOfWeek.FromUserTime();
      var serverEndOfMonth = userEndOfMonth.FromUserTime();
      
      // Задания, которые ещё не выполнены, со сроком больше, чем сегодня.
      query = query.Where(a => a.Deadline.HasValue && Equals(a.Status, Workflow.AssignmentBase.Status.InProcess)
                          && ((a.Deadline > serverBeginOfToday && a.Deadline == a.Deadline.Value.Date) ||
                              (a.Deadline > serverEndOfDay && a.Deadline != a.Deadline.Value.Date)));
      
      DateTime? firstDate = query.OrderBy(d => d.Deadline).Select(d => d.Deadline).FirstOrDefault();
      
      if (firstDate.HasValue)
      {
        var value = string.Empty;
        var text = string.Empty;
        var endOfMonth = serverEndOfMonth;
        if ((firstDate <= serverBeginOfToday.AddDays(1) && !firstDate.Value.HasTime()) ||
            (firstDate <= serverEndOfDay.AddDays(1) && firstDate.Value.HasTime()))
        {
          value = Constants.Module.TodayAssignments.DeadlineTomorrow;
          text = Resources.WidgetMTATomorrow;
        }
        else if ((firstDate <= serverBeginOfToday.AddDays(2) && !firstDate.Value.HasTime()) ||
                 (firstDate <= serverEndOfDay.AddDays(2) && firstDate.Value.HasTime()))
        {
          value = Constants.Module.TodayAssignments.AfterTomorrow;
          text = Resources.WidgetMTAAfterTomorrow;
        }
        else if ((firstDate <= serverTodayEndOfWeek && !firstDate.Value.HasTime()) ||
                 (firstDate <= serverEndOfWeek && firstDate.Value.HasTime()))
        {
          value = Constants.Module.TodayAssignments.EndOfWeek;
          text = Resources.WidgetMTAEndOfWeek;
        }
        else if ((firstDate <= serverTodayEndOfWeek.AddDays(7) && !firstDate.Value.HasTime()) ||
                 (firstDate <= serverEndOfWeek.AddDays(7) && firstDate.Value.HasTime()))
        {
          value = Constants.Module.TodayAssignments.NextEndOfWeek;
          text = Resources.WidgetMTANextEndOfWeek;
        }
        else if ((firstDate <= userTodayEndOfMonth && !firstDate.Value.HasTime()) ||
                 (firstDate <= endOfMonth && firstDate.Value.HasTime()))
        {
          value = Constants.Module.TodayAssignments.EndOfMonth;
          text = Resources.WidgetMTAEndOfMonth;
        }
        else
          return null;
        
        query = GetMyAssignments(query, withSubstitution, value);
        
        return Structures.Module.AssignmentChartGroup.Create(value, text, query.Count());
      }
      
      return null;
    }
    
    /// <summary>
    /// Отфильтровать задания по замещению и статусу.
    /// </summary>
    /// <param name="query">Фильтруемые задания.</param>
    /// <param name="withSubstitution">С замещением.</param>
    /// <returns>Задания.</returns>
    public static IQueryable<IAssignment> FilterMyTodayAssignments(IQueryable<Workflow.IAssignment> query, bool withSubstitution)
    {
      query = query.Where(a => !Equals(a.Status, Workflow.AssignmentBase.Status.Aborted) && !Equals(a.Status, Workflow.AssignmentBase.Status.Suspended));
      
      if (withSubstitution)
      {
        var ids = Recipients.AllRecipientIds.ToList();
        query = query.Where(a => ids.Contains(a.Performer.Id));
      }
      else
      {
        query = query.Where(a => Equals(Users.Current, a.Performer));
      }
      return query;
    }
    
    #endregion
    
    #endregion
    
    /// <summary>
    /// Получить задания по типу этапа согласования, в том числе схлопнутые.
    /// </summary>
    /// <param name="query">Фильтруемые задания.</param>
    /// <param name="stageType">Тип этапа согласования.</param>
    /// <returns>Задания.</returns>
    public IQueryable<Sungero.Workflow.IAssignmentBase> GetSpecificAssignmentsWithCollapsed(IQueryable<Sungero.Workflow.IAssignmentBase> query,
                                                                                            Enumeration stageType)
    {
      var needCheckSending = stageType == Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.Sending;
      var needCheckPrint = needCheckSending || stageType == Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.Print;
      var needCheckRegister = needCheckPrint || stageType == Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.Register;
      var isCheckExecution = stageType == Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.Execution;
      var needCheckExecution = needCheckRegister || isCheckExecution;
      var needCheckConfirmSign = stageType == Docflow.ApprovalReviewAssignmentCollapsedStagesTypesRe.StageType.ConfirmSign;
      var needCheckSign = needCheckExecution && !needCheckConfirmSign;
      var needCheckReview = needCheckSign;
      
      query = query.Where(q => needCheckReview && ApprovalReviewAssignments.Is(q) && ApprovalReviewAssignments.As(q).CollapsedStagesTypesRe.Any(s => s.StageType == stageType) ||
                          needCheckSign && ApprovalSigningAssignments.Is(q) && ApprovalSigningAssignments.As(q).CollapsedStagesTypesSig.Any(s => s.StageType == stageType) ||
                          needCheckConfirmSign && ApprovalSigningAssignments.Is(q) && ApprovalSigningAssignments.As(q).CollapsedStagesTypesSig.Any(s => s.StageType == stageType) &&
                          ApprovalSigningAssignments.As(q).IsConfirmSigning == true ||
                          needCheckExecution && (ApprovalExecutionAssignments.Is(q) && ApprovalExecutionAssignments.As(q).CollapsedStagesTypesExe.Any(s => s.StageType == stageType)) ||
                          needCheckRegister && ApprovalRegistrationAssignments.Is(q) && ApprovalRegistrationAssignments.As(q).CollapsedStagesTypesReg.Any(s => s.StageType == stageType) ||
                          needCheckPrint && ApprovalPrintingAssignments.Is(q) && ApprovalPrintingAssignments.As(q).CollapsedStagesTypesPr.Any(s => s.StageType == stageType) ||
                          needCheckSending && ApprovalSendingAssignments.Is(q) && ApprovalSendingAssignments.As(q).CollapsedStagesTypesSen.Any(s => s.StageType == stageType));
      
      return query;
    }
    
    /// <summary>
    /// Получить документы контрагента.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <returns>Документы.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<Sungero.Content.IElectronicDocument> GetDocumentsWithCounterparties(Sungero.Parties.ICounterparty counterparty)
    {
      return Sungero.Content.ElectronicDocuments.GetAll(d => IncomingDocumentBases.Is(d) && Equals(IncomingDocumentBases.As(d).Correspondent, counterparty) ||
                                                        OutgoingDocumentBases.Is(d) && OutgoingDocumentBases.As(d).Addressees.Select(x => x.Correspondent).Any(y => Equals(y, counterparty)) ||
                                                        AccountingDocumentBases.Is(d) && Equals(AccountingDocumentBases.As(d).Counterparty, counterparty) ||
                                                        ContractualDocumentBases.Is(d) && Equals(ContractualDocumentBases.As(d).Counterparty, counterparty) ||
                                                        ExchangeDocuments.Is(d) && Equals(ExchangeDocuments.As(d).Counterparty, counterparty));
    }
    
    /// <summary>
    /// Получить документы сотрудника, в которых он является ответственным.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Документы.</returns>
    [Public, Remote(IsPure = true)]
    public static IQueryable<Sungero.Content.IElectronicDocument> GetRespondingEmployeeDocuments(IEmployee employee)
    {
      var query = OfficialDocuments.GetAll(d => AccountingDocumentBases.Is(d) || ContractualDocuments.Is(d))
        .Where(d => ((Equals(AccountingDocumentBases.As(d).ResponsibleEmployee, employee) ||
                      Equals(ContractualDocuments.As(d).ResponsibleEmployee, employee)) &&
                     (d.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Draft ||
                      d.LifeCycleState == Docflow.OfficialDocument.LifeCycleState.Active)) ||
               ContractualDocuments.As(d).Milestones.Any(m => Equals(m.Performer, employee) && m.IsCompleted != true) ||
               d.Tracking.Any(t => Equals(t.DeliveredTo, employee) &&
                              t.IsOriginal == true &&
                              t.Action != Docflow.OfficialDocumentTracking.Action.Sending &&
                              (t.ReturnResult == null || t.ReturnResult == Docflow.OfficialDocumentTracking.ReturnResult.AtControl)));
      
      return query;
    }
    
  }
}