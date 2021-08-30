using System;

namespace Sungero.Contracts.Constants
{
  public static class Module
  {
    // Отправка уведомлений о завершении договора.
    public const string NotificationDatabaseKey = "LastNotificationOfExpiringContracts";
    public const string ExpiringContractTableName = "Sungero_Contrac_ExpiringContracts";
    
    public const string HaveRelationKey = "HaveRelation";
    
    public static class Initialize
    {
      [Sungero.Core.Public]
      public static readonly Guid ContractKind = Guid.Parse("2A42D335-4A84-4019-AB54-D0AB8344D232");
      [Sungero.Core.Public]
      public static readonly Guid SupAgreementKind = Guid.Parse("A5B2C424-0F31-4809-B160-ACC0C4583574");
      [Sungero.Core.Public]
      public static readonly Guid IncomingInvoiceKind = Guid.Parse("558BAAB7-784D-42F8-BCB7-BA8C0E8821A3");
      [Sungero.Core.Public]
      public static readonly Guid SupAgreementRegister = Guid.Parse("8A583ACF-FAE5-4D92-A54B-4DA73A81E46C");
    }

    #region Связи
    
    /// <summary>
    /// Имя типа связи "Доп. соглашение".
    /// </summary>
    public const string SupAgreementRelationName = "SupAgreement";
    
    /// <summary>
    /// Имя типа связи "Финансовые документы".
    /// </summary>
    [Sungero.Core.PublicAttribute]
    public const string AccountingDocumentsRelationName = "FinancialDocuments";
    
    /// <summary>
    /// Имя типа связи "Переписка".
    /// </summary>
    public const string CorrespondenceRelationName = "Correspondence";
    
    #endregion
    
    public static readonly Guid ContractsUIGuid = Guid.Parse("3c8b7d3a-187d-4445-8a8c-1d00ece44556");
  }  
}