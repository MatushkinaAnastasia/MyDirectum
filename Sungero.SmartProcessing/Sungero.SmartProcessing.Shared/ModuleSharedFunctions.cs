using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.Metadata;

namespace Sungero.SmartProcessing.Shared
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Получить приоритеты типов документов для определения ведущего документа в комплекте.
    /// </summary>
    /// <returns>Словарь с приоритетами типов.</returns>
    [Public]
    public virtual System.Collections.Generic.IDictionary<System.Type, int> GetPackageDocumentTypePriorities()
    {
      var leadingDocumentPriority = new Dictionary<System.Type, int>();
      leadingDocumentPriority.Add(RecordManagement.IncomingLetters.Info.GetType().GetFinalType(), 9);
      leadingDocumentPriority.Add(Contracts.Contracts.Info.GetType().GetFinalType(), 8);
      leadingDocumentPriority.Add(Contracts.SupAgreements.Info.GetType().GetFinalType(), 7);
      leadingDocumentPriority.Add(Sungero.FinancialArchive.ContractStatements.Info.GetType().GetFinalType(), 6);
      leadingDocumentPriority.Add(Sungero.FinancialArchive.Waybills.Info.GetType().GetFinalType(), 5);
      leadingDocumentPriority.Add(Sungero.FinancialArchive.UniversalTransferDocuments.Info.GetType().GetFinalType(), 4);
      leadingDocumentPriority.Add(Sungero.FinancialArchive.IncomingTaxInvoices.Info.GetType().GetFinalType(), 3);
      leadingDocumentPriority.Add(Sungero.Contracts.IncomingInvoices.Info.GetType().GetFinalType(), 2);
      leadingDocumentPriority.Add(Sungero.FinancialArchive.OutgoingTaxInvoices.Info.GetType().GetFinalType(), 1);
      leadingDocumentPriority.Add(Docflow.SimpleDocuments.Info.GetType().GetFinalType(), 0);
      
      return leadingDocumentPriority;
    }
  }
}