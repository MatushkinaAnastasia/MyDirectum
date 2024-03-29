﻿using System;

namespace Sungero.Docflow.Constants
{
  public static class Module
  {
    // Цвета графиков.
    public static class Colors
    {
      public const string Purple = "#815D87";
      
      public const string Green = "#82B93A";

      public const string Red = "#FF4242";
    }
    
    // Код системы у external link для данных инициализации.
    [Sungero.Core.Public]
    public const string InitializeExternalLinkSystem = "Initialize";
    
    // Имя параметра: используется ли.
    public const string IsUsedParamName = "IsUsed";
    
    // Имя параметра: есть ли зарегистрированные документы в журнале.
    public const string HasRegisteredDocumentsParamName = "HasRegisteredDocuments";
    
    // Имя параметра: версия создана из шаблона.
    public const string CreateFromTemplate = "CreateFromTemplate";
    
    // Имя параметра: документ сохраняется из задачи на продление срока.
    public const string DeadlineExtentsionTaskCallContext = "DeadlineExtentsionTaskCallContext";
    
    // ИД блока доработки согласования официального документа.
    public const string ApprovalReworkAssignmentBlockUid = "5";
    
    // Ключ параметра адреса веб-сервиса проверки контрагентов.
    [Sungero.Core.PublicAttribute]
    public const string CompanyDataServiceKey = "CompanyDataServiceURL";
    
    // Имя параметра адреса веб-сервиса проверки контрагентов.
    public const string CompanyDataServiceDefaultURL = "https://companydata.directum24.ru";
    
    #region Предметное отображение
    
    // Толщина границы выделенного блока.
    public const int CurrentBlockBorderWidth = 4;
    
    // Размер отступа заголовка и основного текста в блоке.
    [Sungero.Core.PublicAttribute]
    public const int EmptyLineMargin = 2;
    
    // Текст разделительной линии.
    [Sungero.Core.PublicAttribute]
    public const string SeparatorText = "________________________________________________________________";
    
    #endregion
    
    #region Связи
    
    // Имя типа связи "Переписка".
    [Sungero.Core.PublicAttribute]
    public const string CorrespondenceRelationName = "Correspondence";
    
    // Имя типа связи "Прочие".
    [Sungero.Core.PublicAttribute]
    public const string SimpleRelationName = "Simple relation";
    
    // Имя типа связи "Отменяет".
    public const string CancelRelationName = "Cancel";
    
    // Имя связи "Основание"
    public const string BasisRelationName = "Basis";
    
    // Имя типа связи "Приложение".
    [Sungero.Core.PublicAttribute]
    public const string AddendumRelationName = "Addendum";
    
    // Имя типа связи "Ответное письмо".
    [Sungero.Core.PublicAttribute]
    public const string ResponseRelationName = "Response";

    // Имя типа связи "Корректировка".
    public const string CorrectionRelationName = "Correction";
    
    #endregion
    
    #region Замещение
    
    // Имя параметра: имеет ли права как замещающий ответственного.
    public const string IsSubstituteResponsibleEmployeeParamName = "IsSubstituteResponsibleEmployee";
    
    // Имя параметра: имеет ли права как администратор.
    public const string IsAdministratorParamName = "IsAdministrator";
    
    #endregion
    
    #region Инициализация
    
    #region Типы документов
    
    // Guid
    // TODO: Переделать на нормальное использование guid'ов после реализации в платформе.
    public const string ProjectDocumentTypeGuid = "56df80b3-a795-4378-ace5-c20a2b1fb6d9";
    
    #endregion
    
    public const string SungeroWFAssignmentTableName = "Sungero_WF_Assignment";
    
    public const string SugeroContentEDocTableName = "Sungero_Content_EDoc";
    
    public const string SugeroWFTaskTableName = "Sungero_WF_Task";
    
    #endregion

    #region Входные параметры функции CompleteReturnControl
    
    public static class ReturnControl
    {
      public const int AbortTask = 0;
      public const int CompleteAssignment = 1;
      public const int SignAssignment = 2;
      public const int NotSignAssignment = 3;
      public const int DeadlineChange = 4;
    }
    
    #endregion
    
    public static class Initialize
    {
      // При создании нового вида гуид для него надо сгенерировать.
      [Sungero.Core.Public]
      public static readonly Guid SimpleDocumentKind = Guid.Parse("3981CDD1-A279-4A51-85D5-58DB391603C2");
      [Sungero.Core.Public]
      public static readonly Guid MemoKind = Guid.Parse("8CB5B6B3-755F-48F3-B5B4-2DFDE6A1FC60");
      [Sungero.Core.Public]
      public static readonly Guid AddendumKind = Guid.Parse("2734AB0B-FD71-4FD2-820E-C25042488547");
      [Sungero.Core.Public]
      public static readonly Guid ExchangeKind = Guid.Parse("4E02B0C3-D448-44E8-AE61-208A79A44205");
      [Sungero.Core.Public]
      public static readonly Guid MemoRegister = Guid.Parse("425355B4-00CC-4417-8EE2-15DE460E034D");
      [Sungero.Core.Public]
      public static readonly Guid PowerOfAttorneyKind = Guid.Parse("0B8E3CF9-77E0-43D3-85E8-7746B41EB822");
      [Sungero.Core.Public]
      public static readonly Guid CounterpartyDocumentDefaultKind = Guid.Parse("07ADBF36-0E67-4772-B6C2-06D2CB52EA34");
    }
    
    // Guid типа "Папка".
    public const string FolderTypeGuid = "271898c8-18ca-4192-9892-e27b273ce5fc";
    
    // Sid приложения-обработчика "Unknown".
    [Sungero.Core.PublicAttribute]
    public static readonly Guid UnknownAppSid = Guid.Parse("49761788-cc45-4485-adb4-55f2056ab043");
    
    #region Группы и роли
    
    public static class RoleGuid
    {
      // GUID роли "Проектные команды".
      public static readonly Guid ParentProjectTeam = new Guid("2062682D-745C-4E02-AF2F-26AD229E8C61");

      // GUID роли "Регистраторы входящих документов".
      public static readonly Guid RegistrationIncomingDocument = new Guid("63EBE616-8780-4CBB-9AF7-C16251B38A84");
      
      // GUID роли "Регистраторы исходящих документов".
      public static readonly Guid RegistrationOutgoingDocument = new Guid("372D8FDB-316E-4F3C-9F6D-C2C1292BBFAE");
      
      // GUID роли "Регистраторы внутренних документов".
      public static readonly Guid RegistrationInternalDocument = new Guid("4073E794-3543-4960-8BF7-CA58D933A900");
      
      // GUID роли "Регистраторы договоров".
      public static readonly Guid RegistrationContractualDocument = new Guid("25C48B40-6111-4283-A94E-7D50E68DECC1");
      
      // GUID роли "Ответственные за договоры".
      public static readonly Guid ContractsResponsible = new Guid("5D813F08-D07F-4EAC-931E-FB3D8BD67012");
      
      // GUID роли "Руководители проектов".
      public static readonly Guid ProjectManagersRoleGuid = new Guid("61016C45-E26C-4CF8-B4BE-09F191AC1BCA");
      
      // GUID роли "Подписывающие".
      public static readonly Guid SigningRole = Guid.Parse("753971F5-95C6-41F5-9808-7BDC1CF3685E");
      
      // GUID роли "Делопроизводители".
      public static readonly Guid ClerksRole = Guid.Parse("B0A07866-7D6F-4860-8850-7016D01EA649");
      
      // GUID роли "Ответственные за настройку регистрации".
      public static readonly Guid RegistrationManagersRole = Guid.Parse("F295DDF0-5253-4127-AB54-4F132956FB8F");
      
      // GUID роли "Ответственные за контрагентов".
      public static readonly Guid CounterpartiesResponsibleRole = Guid.Parse("C719C823-C4BD-4434-A34B-D7E83E524414");
      
      // GUID роли "Пользователи с правами на удаление документов".
      [Sungero.Core.Public]
      public static readonly Guid DocumentDeleteRole = Guid.Parse("6BCA5136-821C-4D5B-9FB0-67BEE22EFDFE");
      
      // GUID роли "Руководители наших организаций".
      [Sungero.Core.Public]
      public static readonly Guid BusinessUnitHeadsRole = Guid.Parse("03C7A126-83DE-4F8F-908B-3ACB868E30C5");
      
      // GUID роли "Руководители подразделений".
      [Sungero.Core.Public]
      public static readonly Guid DepartmentManagersRole = Guid.Parse("EA04AA41-9BD8-45D5-A479-A986137A509C");
    }
    
    #endregion
    
    #region Права
    
    /// <summary>
    /// GUID прав.
    /// </summary>
    public static class DefaultAccessRightsTypeSid
    {
      /// <summary>
      /// Регистрация.
      /// </summary>
      public static readonly Guid Register = Guid.Parse("b46abce0-ef53-4053-9b39-0ba83f5cef6d");

      /// <summary>
      /// Удаление.
      /// </summary>
      public static readonly Guid Delete = Guid.Parse("c3a1064e-4939-4b0c-8a43-2e5a0115e13d");
      
      /// <summary>
      /// Изменение содержимого папки.
      /// </summary>
      public static readonly Guid ChangeContent = Guid.Parse("344A32D8-9814-4BB8-8D86-1F65E43FDA25");
      
      /// <summary>
      /// Отправка через сервис обмена.
      /// </summary>
      public static readonly Guid SendByExchange = Guid.Parse("56D0F76C-5442-4B0C-B363-7E353D348994");
      
      /// <summary>
      /// Установить обмен с контрагентом.
      /// </summary>
      public static readonly Guid SetExchange = Guid.Parse("6CCAD865-AA9C-4AFD-A08B-836363545AAF");
      
      /// <summary>
      /// Изменение.
      /// </summary>
      public static readonly Guid Update = Guid.Parse("179af257-a60f-44b8-97b5-1d5bbd06716b");
    }
    
    #endregion
    
    public static class TaskMainGroup
    {
      [Sungero.Core.Public]
      public static readonly Guid ApprovalTask = Guid.Parse("08e1ef90-521f-41a1-a13f-c6f175007e54");

      [Sungero.Core.Public]
      public static readonly Guid DocumentReviewTask = Guid.Parse("88ec82fb-d8a8-4a36-a0d8-5c0bf42ff820");

      [Sungero.Core.Public]
      public static readonly Guid ActionItemExecutionTask = Guid.Parse("804f50fe-f3da-411b-bb2e-e5373936e029");
    }
    
    // Режимы фильтрации заданий.
    public static class FilterAssignmentsMode
    {
      public const string Default = "Default";
      
      public const string Created = "Created";

      public const string Modified = "Modified";
      
      public const string Completed = "Completed";
    }
    
    // Имя приложения конвертера документов из xml в pdf.
    public const string PdfConverterExeName = "wkhtmltopdf.exe";
    
    // Дата последнего обновления прав документов.
    public const string LastAccessRightsUpdateDate = "LastAccessRightsUpdateDate";
    
    // Уведомления о завершении работ по доверенности.
    public const string ExpiringPowerOfAttorneyLastNotificationKey = "ExpiringPowerOfAttorneyLastNotification";
    public const string ExpiringPowerOfAttorneyTableName = "Sungero_Docflow_ExpiringPoA";
    
    // Максимально возможное значение дней до завершения - ограничение SQL.
    [Sungero.Core.PublicAttribute]
    public const int MaxDaysToFinish = 24855;
    
    // Способ автоматической выдачи прав на документы.
    public const string GrantRightsMode = "GrantRightsMode";
    public const string GrantRightsModeByJob = "Job";
    public const string GrantRightsModeByAsyncHandler = "Async";
    
    /// <summary>
    /// Коды справки для действий по продлению срока задания.
    /// </summary>
    public static class HelpCodes
    {
      // Диалог продления срока задания на доработку.
      public const string DeadlineExtensionDialog = "Sungero_Docflow_DeadlineExtensionDialog";
    }
    
    // Количество страниц документа, на которых ищется якорь для добавления отметки об ЭП.
    public const int SearchablePagesLimit = 5;
    
    #region Интеллектуальная обработка документов
    
    // Ключ параметра адреса сервиса Ario.
    [Sungero.Core.Public]
    public const string ArioUrlKey = "ArioUrl";
    
    // Ключ параметра минимально допустимой вероятности для поля факта, извлеченного Ario.
    // Факт с полем, вероятность которого ниже минимально допустимой, отбрасывается как недостоверный.
    [Sungero.Core.Public]
    public const string MinFactProbabilityKey = "MinFactProbability";
    
    // Ключ параметра вероятности для поля факта, извлеченного Ario, выше которой факт считается достоверным.
    [Sungero.Core.Public]
    public const string TrustedFactProbabilityKey = "TrustedFactProbability";
    
    // Названия параметров отображения фокусировки подсветки в предпросмотре.
    public static class HighlightActivationStyleParamNames
    {
      // Признак фокусировки поля с помощью рамки.
      public const string UseBorder = "HighlightActivationStyleUseBorder";
      
      // Цвет рамки.
      public const string BorderColor = "HighlightActivationStyleBorderColor";
      
      // Толщина рамки.
      public const string BorderWidth = "HighlightActivationStyleBorderWidth";
      
      // Признак фокусировки поля с помощью заливки.
      public const string UseFilling = "HighlightActivationStyleUseFilling";
      
      // Цвет заливки.
      public const string FillingColor = "HighlightActivationStyleFillingColor";
    }
    
    [Sungero.Core.Public]
    public const char PositionElementDelimiter = '|';

    public const int HighlightActivationBorderDefaultWidth = 10;
    
    [Sungero.Core.Public]
    public const char PropertyAndPositionDelimiter = '-';

    [Sungero.Core.Public]
    public const char PositionsDelimiter = '#';
    
    #endregion
    
    #region Ставки НДС
    
    /// <summary>
    /// Значение по умолчанию для ставки "Без НДС".
    /// </summary>
    public const int DefaultVatRateWithoutVat = 0;
    
    /// <summary>
    /// Sid ставки НДС "Без НДС".
    /// </summary>
    public const string VatRateWithoutVatSid = "930EC682-0CA7-4B9E-9F0F-2F5CE8B6A90B";
    
    /// <summary>
    /// Значение по умолчанию для ставки "0%".
    /// </summary>
    public const int DefaultVatRateZeroPercent = 0;
    
    /// <summary>
    /// Sid ставки НДС "0%".
    /// </summary>
    public const string VatRateZeroPercentSid = "D54D8812-2BBF-4BBA-8CA5-BECB3FBD6DDB";
    
    /// <summary>
    /// Значение по умолчанию для ставки "10%".
    /// </summary>
    public const int DefaultVatRateTenPercent = 10;
    
    /// <summary>
    /// Sid ставки НДС "10%".
    /// </summary>
    public const string VatRateTenPercentSid = "C35498D8-511F-4218-8137-39EC11A1596B";
    
    /// <summary>
    /// Значение по умолчанию для ставки "20%".
    /// </summary>
    public const int DefaultVatRateTwentyPercent = 20;
    
    /// <summary>
    /// Sid ставки НДС "20%".
    /// </summary>
    public const string VatRateTwentyPercentSid = "D99F2F70-1069-4190-95EE-948F49C065C5";
    
    #endregion
    
    // Максимальное количество контролов для добавления приложений из диалога.
    public const int ManyAddendumDialogLimit = 10;

    // Максимальный размер файла для добавления приложений из диалога.
    public const int ManyAddendumDialogMaxFileSize = 26214400;
    
    // Перенос документов между хранилищами.
    public const string StoragePolicySettingsTableName = "Sungero_Docflow_StoragePolicySettings";
    
    // Системные пользователи.
    [Sungero.Core.Public]
    public static readonly Guid CollaborationService = Guid.Parse("F28BCB67-223E-466E-B251-8748D9F12C14");
    
    [Sungero.Core.Public]
    public static readonly Guid DefaultGroup = Guid.Parse("73490DC5-1209-4D72-AFD3-BDCF60456E46");
    
    [Sungero.Core.Public]
    public static readonly Guid DefaultUser = Guid.Parse("63490DC5-1209-4D72-AFD3-BDCF60482E46");
    
    // Ограничение длины названия файла при выгрузке.
    public const int ExportNameLength = 50;
  }
}