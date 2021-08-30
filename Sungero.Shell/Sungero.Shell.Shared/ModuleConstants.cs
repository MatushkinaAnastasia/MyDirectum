using System;

namespace Sungero.Shell.Constants
{
  public static class Module
  {

    /// <summary>
    /// Гуид входящего письма.
    /// </summary>
    public const string IncomingLetterGuid = "8dd00491-8fd0-4a7a-9cf3-8b6dc2e6455d";
    
    #region Ключи кэша
    
    public const string DocumentDynamicMyCacheKeyFormat = "DocDynMyEmp_{0}_180";
    
    public const string TaskDynamicMyCacheKeyFormat = "TaskDynMyEmp_{0}_180";
    
    public const string DocumentDynamicDepartmentCacheKeyFormat = "DocDynDepEmp_{0}_180";
    
    public const string TaskDynamicDepartmentCacheKeyFormat = "TaskDynDepEmp_{0}_180";
    
    public const string DocumentDynamicEmployeeAllCacheKeyFormat = "DocDynAllEmp_{0}_180";
    
    public const string TaskDynamicEmployeeAllCacheKeyFormat = "TaskDynAllEmp_{0}_180";
    
    public const string DocumentDynamicAllCacheKeyFormat = "DocDynAll_180";
    
    public const string TaskDynamicAllCacheKeyFormat = "TaskDynAll_180";
    
    public const string AsgDynamicMyCacheKeyFormat = "AsgDynMyEmp_{0}_180";
    
    public const string AsgDynamicDepartmentCacheKeyFormat = "AsgDynDepEmp_{0}_180";
    
    public const string AsgDynamicEmployeeAllCacheKeyFormat = "AsgDynAllEmp_{0}_180";
    
    public const string AsgDynamicAllCacheKeyFormat = "AsgDynAll_180";
    
    #endregion
    
    #region Виджеты
    
    #region Идентификаторы значений серий
    public const string OverduedAssignments = "Overdued";
    
    public const string NotOverduedAssignments = "NotOverdued";
    
    public const string ActiveAssignments = "ActiveAssignments";
    
    public static class TodayAssignments
    {
      public const string CompletedToday = "CompletedToday";
      public const string DeadlineToday = "DeadlineToday";
      public const string OverdueToday = "OverdueToday";
      
      public const string DeadlineTomorrow = "DeadlineTomorrow";
      public const string AfterTomorrow = "AfterTomorrow";
      public const string EndOfWeek = "EndOfWeek";
      public const string NextEndOfWeek = "NextEndOfWeek";
      public const string EndOfMonth = "EndOfMonth";
    }
    #endregion
    
    #endregion
    
    // Цвета графиков.
    public static class Colors
    {
      public const string Red = "#FF5000";
      
      public const string Orange = "#E89314";
      
      public const string Yellow = "#FCC72F";
      
      public const string LightYellowGreen = "#BAC238";
      
      public const string YellowGreen = "#7DAB3A";
      
      public const string Green = "#4FAA37";
    }
    
    // Формат ключа кеша исполнительской дисциплины сотрудника.
    public const string EmployeeAssignmentsCacheKey = "EmpAsg_{0}_90";
    
    // Формат ключа кеша исполнительской дисциплины подразделения.
    public const string DepartmentAssignmentsCacheKey = "DepAsg_{0}_90";
    
    public const string AdviserAssignmentCacheKey = "AllAsg_90";

    // Типы кэшей.
    public const string AllCache = "AllCache";
    public const string UserCache = "UserCache";
    public const string DepartmentCache = "DepartmentCache";
    
    // Режимы фильтрации заданий.
    public static class FilterAssignmentsMode
    {
      public const string Default = "Default";
      
      public const string Created = "Created";
      
      public const string Modified = "Modified";
      
      public const string Completed = "Completed";
    }
    
  }
}