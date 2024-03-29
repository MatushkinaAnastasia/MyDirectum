﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace Sungero.Company.Server
{
  public class ModuleFunctions
  {
    #region Создание/удаление системных замещений

    /// <summary>
    /// Создать системные замещения.
    /// </summary>
    /// <param name="substitutedUsers">Список пользователей, для которых надо создать замещение.</param>
    /// <param name="substitute">Руководитель.</param>
    public static void CreateSystemSubstitutions(System.Collections.Generic.IEnumerable<IUser> substitutedUsers,
                                                 IUser substitute)
    {
      foreach (var user in substitutedUsers)
        CreateSystemSubstitution(user, substitute);
    }

    /// <summary>
    /// Удалить системные замещения.
    /// </summary>
    /// <param name="substitutedUsers">Список пользователей, для которых надо удалить замещение.</param>
    /// <param name="substitute">Руководитель.</param>
    public static void DeleteSystemSubstitutions(System.Collections.Generic.IEnumerable<IUser> substitutedUsers,
                                                 IUser substitute)
    {
      var deletedSubstitutions = Substitutions.GetAll()
        .Where(s => Equals(s.Substitute, substitute) && substitutedUsers.Contains(s.User) && s.IsSystem == true)
        .ToList();
      
      foreach (var user in substitutedUsers)
      {
        var deletedSubstitution = deletedSubstitutions.FirstOrDefault(s => Equals(s.User, user));
        
        if (deletedSubstitution != null)
          Substitutions.Delete(deletedSubstitution);
      }
    }

    /// <summary>
    /// Создать системное замещение.
    /// </summary>
    /// <param name="substitutedUser">Замещаемый пользователь.</param>
    /// <param name="substitute">Замещающий пользователь.</param>
    public static void CreateSystemSubstitution(IUser substitutedUser, IUser substitute)
    {
      if (Equals(substitutedUser, substitute))
        return;
      
      var substitution = Substitutions.Create();
      substitution.User = substitutedUser;
      substitution.Substitute = substitute;
      substitution.IsSystem = true;
    }

    /// <summary>
    /// Удалить системное замещение.
    /// </summary>
    /// <param name="substitutedUser">Пользователь, для которого надо удалить замещение.</param>
    /// <param name="substitute">Руководитель.</param>
    public static void DeleteSystemSubstitution(IUser substitutedUser, IUser substitute)
    {
      var deletedSubstitution = Substitutions.GetAll()
        .Where(s => Equals(s.Substitute, substitute) && Equals(s.User, substitutedUser) && s.IsSystem == true)
        .FirstOrDefault();
      
      if (deletedSubstitution != null)
        Substitutions.Delete(deletedSubstitution);
    }

    #endregion

    #region Обложка

    /// <summary>
    /// Создать сотрудника.
    /// </summary>
    /// <returns>Новый сотрудник.</returns>
    [Remote]
    public static IEmployee CreateEmployee()
    {
      return Employees.Create();
    }

    /// <summary>
    /// Создать нашу организацию.
    /// </summary>
    /// <returns>Новая НОР.</returns>
    [Remote]
    public static IBusinessUnit CreateBusinessUnit()
    {
      return BusinessUnits.Create();
    }

    /// <summary>
    /// Создать подразделение.
    /// </summary>
    /// <returns>Новое подразделение.</returns>
    [Remote]
    public static IDepartment CreateDepartment()
    {
      return Departments.Create();
    }
    
    /// <summary>
    /// Получить настройки видимости организационной структуры.
    /// </summary>
    /// <returns>Настройки видимости организационной структуры.</returns>
    [Remote]
    public virtual IVisibilitySetting GetVisibilitySettings()
    {
      return VisibilitySettings.GetAll().SingleOrDefault();
    }

    #endregion
    
    /// <summary>
    /// Получить роль "Руководители наших организаций".
    /// </summary>
    /// <returns>Роль "Руководители наших организаций".</returns>
    [Remote(IsPure = true)]
    public static IRole GetCEORole()
    {
      return Roles.GetAll(r => r.Sid == Constants.Module.BusinessUnitHeadsRole).SingleOrDefault();
    }
    
    /// <summary>
    /// Получить сотрудника по id.
    /// </summary>
    /// <param name="id">Id.</param>
    /// <returns>Сотрудник.</returns>
    [Public, Remote(IsPure = true)]
    public static IEmployee GetEmployeeById(int id)
    {
      return Employees.Get(id);
    }
    
    /// <summary>
    /// Получить список сотрудников по id.
    /// </summary>
    /// <param name="ids">Список Id.</param>
    /// <returns>Список сотрудников.</returns>
    [Public, Remote(IsPure = true)]
    public static List<IEmployee> GetEmployeesByIds(List<int> ids)
    {
      return Employees.GetAll(e => ids.Contains(e.Id)).ToList();
    }
    
    /// <summary>
    /// Получить подразделение по id.
    /// </summary>
    /// <param name="id">Id.</param>
    /// <returns>Подразделение.</returns>
    [Public, Remote(IsPure = true)]
    public static IDepartment GetDepartmentById(int id)
    {
      return Departments.Get(id);
    }
    
    /// <summary>
    /// Получить сертификаты сотрудника.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Список сертификатов.</returns>
    [Public, Remote(IsPure = true)]
    public static IQueryable<ICertificate> GetCertificatesOfEmployee(IEmployee employee)
    {
      return Certificates.GetAll(x => Equals(x.Owner, employee));
    }
    
    /// <summary>
    /// Получить неавтоматизированных сотрудников без замещения.
    /// </summary>
    /// <param name="employees"> Список сотрудников для обработки.</param>
    /// <returns> Список неавтоматизированных сотрудников без замещения.</returns>
    [Public, Remote(IsPure = true)]
    public IQueryable<Sungero.Company.IEmployee> GetNotAutomatedEmployees(List<Sungero.Company.IEmployee> employees)
    {
      var notAutomatedEmployeesWoSubstitution = new List<Sungero.Company.IEmployee>();
      var signOutEmployees = employees.Where(r => r.Login == null || r.Login.Status == CoreEntities.DatabookEntry.Status.Closed)
        .ToList();
      
      foreach (var employee in signOutEmployees)
      {
        var substitutions = Substitutions.GetAll()
          .Where(x => Equals(x.User, employee) &&
                 x.IsSystem != true &&
                 x.Status == CoreEntities.DatabookEntry.Status.Active &&
                 (!x.StartDate.HasValue || Calendar.Today >= x.StartDate) &&
                 (!x.EndDate.HasValue || Calendar.Today <= x.EndDate))
          .ToList();
        
        if (!substitutions.Any())
          notAutomatedEmployeesWoSubstitution.Add(employee);
        
        var substitutors = substitutions.Select(x => x.Substitute).ToList();
        if (substitutors.Any(x => x.Login != null && x.Login.Status != CoreEntities.DatabookEntry.Status.Closed))
          continue;
        
        while (substitutors.Count > 0)
        {
          substitutions = new List<ISubstitution>();
          
          foreach (var substitutor in substitutors)
            substitutions.AddRange(Substitutions.GetAll()
                                   .Where(x => Equals(x.User, substitutor) &&
                                          x.IsSystem != true &&
                                          x.Status == CoreEntities.DatabookEntry.Status.Active &&
                                          (!x.StartDate.HasValue || Calendar.Today >= x.StartDate) &&
                                          (!x.EndDate.HasValue || Calendar.Today <= x.EndDate)));
          
          if (!substitutions.Any())
            notAutomatedEmployeesWoSubstitution.Add(employee);
          
          substitutors = substitutions.Select(x => x.Substitute).ToList();
          if (substitutors.Any(x => x.Login != null && x.Login.Status != CoreEntities.DatabookEntry.Status.Closed))
          {
            notAutomatedEmployeesWoSubstitution.Add(employee);
            break;
          }
        }
      }
      
      return notAutomatedEmployeesWoSubstitution.AsQueryable();
    }
    
    /// <summary>
    /// Количество строк данных для отчета полномочий сотрудника по всем модулям.
    /// </summary>
    /// <param name="employee">Сотрудник для обработки.</param>
    /// <returns>Количество.</returns>
    /// <remarks>Функция введена для тестирования. Если вернулась пустая строка - значит, в результате выполнения функции произошли ошибки.</remarks>
    [Public, Remote]
    public static string GetCountResponsibilitiesReportData(IEmployee employee)
    {
      try
      {
        var allReportData = GetAllResponsibilitiesReportData(employee);
        return allReportData.Count.ToString();
      }
      catch
      {
        return string.Empty;
      }
    }
    
    /// <summary>
    /// Данные для отчета полномочий сотрудника по всем модулям.
    /// </summary>
    /// <param name="employee">Сотрудник для обработки.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public static List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> GetAllResponsibilitiesReportData(IEmployee employee)
    {
      var result = new List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine>();
      
      if (employee == null)
        return result;
      
      var companyData = Sungero.Company.Functions.Module.GetResponsibilitiesReportData(employee);
      var exchangeData = ExchangeCore.PublicFunctions.Module.GetResponsibilitiesReportData(employee);
      var docflowData = Sungero.Docflow.PublicFunctions.Module.GetResponsibilitiesReportData(employee);
      var projectData = Sungero.Projects.PublicFunctions.Module.GetResponsibilitiesReportData(employee);
      var meetingsData = Sungero.Meetings.PublicFunctions.Module.GetResponsibilitiesReportData(employee);
      var partiesData = Sungero.Parties.PublicFunctions.Module.GetResponsibilitiesReportData(employee);
      result.AddRange(companyData);
      result.AddRange(exchangeData);
      result.AddRange(docflowData);
      result.AddRange(projectData);
      result.AddRange(meetingsData);
      result.AddRange(partiesData);
      return result;
    }
    
    /// <summary>
    /// Дополнить набор данных для отчета "Полномочия и зоны ответственности сотрудника".
    /// </summary>
    /// <param name="source">Исходный набор данных.</param>
    /// <param name="entity">Сущность, которая добавляется к набору.</param>
    /// <param name="moduleName">Имя модуля.</param>
    /// <param name="modulePriority">Приоритет модуля.</param>
    /// <param name="sectionName">Имя раздела, к которому относятся сущности.</param>
    /// <returns>Набор данных для отчета "Полномочия и зоны ответственности сотрудника".</returns>
    [Public]
    public static List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> AppendResponsibilitiesReportResult(
      List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> source,
      IEntity entity,
      string moduleName,
      int modulePriority,
      string sectionName)
    {
      var entities = new List<IEntity>();
      if (entity != null)
        entities.Add(entity);
      
      return AppendResponsibilitiesReportResult(source, entities, moduleName, modulePriority, sectionName, null);
    }
    
    /// <summary>
    /// Дополнить набор данных для отчета "Полномочия и зоны ответственности сотрудника".
    /// </summary>
    /// <param name="source">Исходный набор данных.</param>
    /// <param name="entities">Сущности, которые добавляются к набору.</param>
    /// <param name="moduleName">Имя модуля.</param>
    /// <param name="modulePriority">Приоритет модуля.</param>
    /// <param name="sectionName">Имя раздела, к которому относятся сущности.</param>
    /// <param name="mainEntity">Основная сущность (будет выделена жирным).</param>
    /// <returns>Набор данных для отчета "Полномочия и зоны ответственности сотрудника".</returns>
    [Public]
    public static List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> AppendResponsibilitiesReportResult(
      List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> source,
      System.Collections.Generic.IEnumerable<IEntity> entities,
      string moduleName,
      int modulePriority,
      string sectionName,
      IEntity mainEntity = null)
    {
      var responsibilityPriority = modulePriority + source.Count;
      var entitiesWithPresentation = entities.ToDictionary<IEntity, IEntity, string>(x => x, x => x.DisplayValue);
      source = AppendResponsibilitiesReportResult(source, entitiesWithPresentation, moduleName, responsibilityPriority, sectionName, mainEntity);
      
      return source;
    }
    
    /// <summary>
    /// Дополнить набор данных для отчета "Полномочия и зоны ответственности сотрудника".
    /// </summary>
    /// <param name="source">Исходный набор данных.</param>
    /// <param name="entitiesWithPresentation">Сущности и их отображаемые значения, которые добавляются к набору.</param>
    /// <param name="moduleName">Имя модуля.</param>
    /// <param name="responsibilityPriority">Приоритет вида ответственности.</param>
    /// <param name="sectionName">Имя раздела, к которому относятся сущности.</param>
    /// <param name="mainEntity">Основная сущность (будет выделена жирным).</param>
    /// <returns>Набор данных для отчета "Полномочия и зоны ответственности сотрудника".</returns>
    [Public]
    public static List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> AppendResponsibilitiesReportResult(
      List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> source,
      System.Collections.Generic.IDictionary<IEntity, string> entitiesWithPresentation,
      string moduleName,
      int responsibilityPriority,
      string sectionName,
      IEntity mainEntity = null)
    {
      if (!entitiesWithPresentation.Any())
      {
        var emptyTableLine = Company.PublicFunctions.Module.CreateResponsibilitiesReportTableLine(moduleName, string.Empty, string.Empty, null);
        emptyTableLine.Responsibility = sectionName;
        emptyTableLine.Priority = responsibilityPriority;
        source.Add(emptyTableLine);
      }
      foreach (var entityWithPresentation in entitiesWithPresentation)
      {
        var entityName = entityWithPresentation.Value;
        var entityPriority = entitiesWithPresentation.Count > 1 ? responsibilityPriority + 1 : responsibilityPriority;
        if (Equals(entityWithPresentation.Key, mainEntity) && entitiesWithPresentation.Count > 1)
        {
          entityName = string.Format("<b>{0}</b>", entityWithPresentation.Value);
          entityPriority = responsibilityPriority;
        }
        
        var newTableLineRecord = Company.PublicFunctions.Module.CreateResponsibilitiesReportTableLine(moduleName,
                                                                                                      sectionName,
                                                                                                      entityName,
                                                                                                      entityWithPresentation.Key);
        newTableLineRecord.Priority = entityPriority;
        source.Add(newTableLineRecord);
      }
      
      return source;
    }
    
    /// <summary>
    /// Данные для отчета полномочий сотрудника из модуля Компания.
    /// </summary>
    /// <param name="employee">Сотрудник для обработки.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public virtual List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> GetResponsibilitiesReportData(IEmployee employee)
    {
      var result = new List<Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine>();
      // HACK: Получаем отображаемое имя модуля.
      var moduleGuid = new CompanyModule().Id;
      var moduleName = Sungero.Metadata.Services.MetadataSearcher.FindModuleMetadata(moduleGuid).GetDisplayName();
      var modulePriority = Company.Constants.ResponsibilitiesReport.CompanyPriority;
      
      // Должность.
      result = AppendResponsibilitiesReportResult(result, employee.JobTitle, moduleName, modulePriority, Resources.Jobtitle);

      // Подразделения.
      if (Departments.AccessRights.CanRead())
      {
        var employeeDepartments = Departments.GetAll()
          .Where(d => d.RecipientLinks.Any(e => Equals(e.Member, employee)))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        result = AppendResponsibilitiesReportResult(result, employeeDepartments, moduleName, modulePriority, Resources.Departments, employee.Department);
      }

      // НОР.
      if (Departments.AccessRights.CanRead() &&
          BusinessUnits.AccessRights.CanRead())
      {
        var businessUnits = Departments.GetAll()
          .Where(d => d.RecipientLinks.Any(e => Equals(e.Member, employee)))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
          .Select(b => b.BusinessUnit)
          .Where(b => b.Status == Sungero.CoreEntities.DatabookEntry.Status.Active).Distinct();
        result = AppendResponsibilitiesReportResult(result, businessUnits, moduleName, modulePriority, Resources.BusinessUnits, employee.Department.BusinessUnit);
      }
      
      // Руководитель подразделений.
      if (Departments.AccessRights.CanRead())
      {
        var managerOfDepartments = Departments.GetAll().Where(d => Equals(d.Manager, employee))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        result = AppendResponsibilitiesReportResult(result, managerOfDepartments, moduleName, modulePriority, Resources.ManagerOfDepartmens);
      }
      
      // Руководители НОР.
      if (BusinessUnits.AccessRights.CanRead())
      {
        var businessUnitsCEO = BusinessUnits.GetAll().Where(b => Equals(b.CEO, employee))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        result = AppendResponsibilitiesReportResult(result, businessUnitsCEO, moduleName, modulePriority, Resources.BusinessUnitsCEO);
      }
      
      // Роли.
      if (Roles.AccessRights.CanRead())
      {
        var roles = Roles.GetAll().Where(r => r.RecipientLinks.Any(e => Equals(e.Member, employee)))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        result = AppendResponsibilitiesReportResult(result, roles, moduleName, modulePriority, Resources.Roles);
      }
      
      // Помощники руководителей.
      if (ManagersAssistants.AccessRights.CanRead())
      {
        var managersAssistants = ManagersAssistants.GetAll()
          .Where(m => Equals(m.Assistant, employee) || Equals(m.Manager, employee))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
          .ToDictionary<IEntity, IEntity, string>(x => x,
                                                  x => string.Format("{0}: {1}{2}{3}: {4}",
                                                                     Resources.Manager,
                                                                     PublicFunctions.Employee.GetShortName(ManagersAssistants.As(x).Manager, false),
                                                                     Environment.NewLine,
                                                                     Resources.Assistant,
                                                                     PublicFunctions.Employee.GetShortName(ManagersAssistants.As(x).Assistant, false)));
        result = AppendResponsibilitiesReportResult(result, managersAssistants, moduleName, modulePriority + result.Count, Resources.ManagersAssistants);
      }
      
      // Замещения.
      if (Substitutions.AccessRights.CanRead())
      {
        var substitutions = Substitutions.GetAll()
          .Where(s => (Equals(s.Substitute, employee) ||
                       Equals(s.User, employee)) &&
                 (!s.EndDate.HasValue || s.EndDate >= Calendar.UserToday))
          .Where(s => s.IsSystem != true)
          .ToDictionary<IEntity, IEntity, string>(x => x, x => CreateSubstitutionPresentation(Substitutions.As(x)));
        result = AppendResponsibilitiesReportResult(result, substitutions, moduleName, modulePriority + result.Count, Resources.Substitutions);
      }
      
      return result;
    }
    
    /// <summary>
    /// Сформировать текстовую информацию о замещении.
    /// </summary>
    /// <param name="substitution">Замещение.</param>
    /// <returns>Текстовая информация о замещении.</returns>
    public static string CreateSubstitutionPresentation(ISubstitution substitution)
    {
      var startDate = substitution.StartDate.HasValue ? string.Format("{0} {1}", Resources.From, substitution.StartDate.Value.ToShortDateString()) : string.Empty;
      var endDate = substitution.EndDate.HasValue ? string.Format("{0} {1}", Resources.To, substitution.EndDate.Value.ToShortDateString()) : string.Empty;
      var period = string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate) ? Resources.Permanently : string.Format("{0} {1}", startDate, endDate).Trim();
      return string.Format("{0}: {1}{2}{3}: {4}{2}{5}: {6}",
                           Resources.Substitute,
                           PublicFunctions.Employee.GetShortName(GetEmployeeById(substitution.Substitute.Id), false),
                           Environment.NewLine,
                           Resources.Employee,
                           PublicFunctions.Employee.GetShortName(GetEmployeeById(substitution.User.Id), false),
                           Resources.Period,
                           period);
    }
    
    /// <summary>
    /// Строит строку данных для отчета о полномочиях.
    /// </summary>
    /// <param name="moduleName">Имя модуля.</param>
    /// <param name="responsibility"> Справочник/роль.</param>
    /// <param name="record">Запись справочника, текст.</param>
    /// <param name="element">Запись справочника, объект.</param>
    /// <returns>Строка данных отчета о полномочиях.</returns>
    [Public]
    public static Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine CreateResponsibilitiesReportTableLine(string moduleName,
                                                                                                                          string responsibility,
                                                                                                                          string record,
                                                                                                                          Sungero.Domain.Shared.IEntity element)
    {
      var newTableLine = new Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine();
      newTableLine.ModuleName = moduleName;
      newTableLine.Responsibility = responsibility;
      newTableLine.Record = !string.IsNullOrEmpty(record) ? record : "-";
      if (element != null)
      {
        newTableLine.RecordId = element.Id;
        newTableLine.RecordHyperlink = Hyperlinks.Get(element);
      }
      return newTableLine;
    }
    
    /// <summary>
    /// Получить все объекты IRecipient.
    /// </summary>
    /// <returns>Все объекты IRecipient в виде запроса.</returns>
    [Remote(IsPure = true)]
    public IQueryable<IRecipient> GetAllRecipients()
    {
      // Dmitriev_IA: Используется в GetAllActiveNoSystemGroups().
      // Серверная фильтрация IQueryable не работает для desktop-клиента. Bug 98921.
      return Sungero.CoreEntities.Recipients.GetAll();
    }
    
    /// <summary>
    /// Получить режим ограничения видимости оргструктуры.
    /// </summary>
    /// <returns>True, если режим ограничения видимости оргструктуры, иначе False.</returns>
    [Public, Remote]
    public virtual bool IsRecipientRestrict()
    {
      var visibilitySettings = VisibilitySettings.GetAll().SingleOrDefault();
      if (visibilitySettings == null)
        return false;
      
      if (visibilitySettings.NeedRestrictVisibility != true)
        return false;
      
      if (Employees.Current == null)
        return false;
      
      var unrestrictedRecipients = visibilitySettings.UnrestrictedRecipients.Select(r => r.Recipient.Id).ToList();
      var headRecipients = Functions.Module.GetHeadRecipientsByEmployee(Employees.Current.Id);
      headRecipients.Add(Employees.Current.Id);
      
      return !unrestrictedRecipients.Any(r => headRecipients.Contains(r));
    }
    
    /// <summary>
    /// Включен ли режим ограничения видимости оргструктуры.
    /// </summary>
    /// <returns>True, если режим ограничения видимости оргструктуры включен, иначе False.</returns>
    [Public, Remote]
    public virtual bool IsRecipientRestrictModeOn()
    {
      var visibilitySettings = VisibilitySettings.GetAll().SingleOrDefault();
      if (visibilitySettings == null)
        return false;
      
      return visibilitySettings.NeedRestrictVisibility == true;
    }
    
    /// <summary>
    /// Получить список доступных реципиентов согласно настройке.
    /// </summary>
    /// <param name="recipientTypeGuid">GUID типа сущности.</param>
    /// <returns>Список Ид реципиентов.</returns>
    [Public, Remote]
    public virtual List<int> GetVisibleRecipientIds(string recipientTypeGuid)
    {
      if (Employees.Current == null)
        return new List<int>();
      
      var recipientIds = Functions.Module.GetAllVisisbleRecipientsIds(Employees.Current.Id, recipientTypeGuid);
      return recipientIds;
    }
    
    /// <summary>
    /// Получить головные Наши организации/подразделения для сотрудника.
    /// </summary>
    /// <param name="currentEmployeeId">Ид сотрудника.</param>
    /// <returns>Список Ид реципиентов.</returns>
    [Public]
    public virtual List<int> GetHeadRecipientsByEmployee(int currentEmployeeId)
    {
      var parameters = string.Format("{0}", currentEmployeeId);
      return this.GetRecipientIdsFromStoredProcedure(Constants.Module.GetHeadRecipientsByEmployeeProcedureName, parameters);
    }
    
    /// <summary>
    /// Получить список Ид реципиентов Нашей организации/подразделения.
    /// </summary>
    /// <param name="currentRecipientId">Ид реципиента.</param>
    /// <param name="recipientTypeGuid">GUID типа сущности.</param>
    /// <returns>Раскрытый список Ид реципиентов.</returns>
    [Public]
    public virtual List<int> GetAllVisisbleRecipientsIds(int currentRecipientId, string recipientTypeGuid)
    {
      var parameters = string.Format("{0}, '{1}'", currentRecipientId, recipientTypeGuid);
      return this.GetRecipientIdsFromStoredProcedure(Constants.Module.GetAllVisibleRecipientsProcedureName, parameters);
    }

    /// <summary>
    /// Создать настройки видимости организационной структуры.
    /// </summary>
    public virtual void CreateVisibilitySettings()
    {
      var visibilitySettings = VisibilitySettings.Create();
      visibilitySettings.Name = VisibilitySettings.Info.LocalizedName;
      visibilitySettings.Save();
    }
    
    /// <summary>
    /// Исключить системных реципиентов из списка.
    /// </summary>
    /// <param name="query">Запрос.</param>
    /// <param name="isRecipientsStatusActive">Возвращать только действующих реципиентов.</param>
    /// <returns>Отфильтрованный результат запроса.</returns>
    public IQueryable<Sungero.CoreEntities.IRecipient> ExcludeSystemRecipients(IQueryable<Sungero.CoreEntities.IRecipient> query, bool isRecipientsStatusActive)
    {
      var systemRecipientsSid = PublicFunctions.Module.GetSystemRecipientsSidWithoutAllUsers(false);
      if (isRecipientsStatusActive)
        query = query.Where(x => x.Status == CoreEntities.DatabookEntry.Status.Active);
      
      return query.Where(x => (Employees.Is(x) || Groups.Is(x)) && !systemRecipientsSid.Contains(x.Sid.Value));
    }
    
    private List<int> GetRecipientIdsFromStoredProcedure(string procedureName, string parameters)
    {
      var recipientIds = new List<int>();
      var commandText = string.Format(Queries.Module.ExecuteStoredProcedure, procedureName, parameters);
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = commandText;
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
            recipientIds.Add(reader.GetInt32(0));
        }
      }
      return recipientIds;
    }
    
    /// <summary>
    /// Сформировать всплывающую подсказку о сотруднике.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Всплывающая подсказка о сотруднике.</returns>
    [Public]
    public virtual Sungero.Core.IDigestModel GetEmployeePopup(IEmployee employee)
    {
      return Functions.Employee.GetEmployeePopup(employee);
    }
  }
}