using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalManagerAssignment;

namespace Sungero.Docflow.Shared
{
  partial class ApprovalManagerAssignmentFunctions
  {
    /// <summary>
    /// Обновить отображение доставки.
    /// </summary>
    public virtual void UpdateDeliveryMethod()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var deliveryMethodIsExchange = _obj.DeliveryMethod != null && _obj.DeliveryMethod.Sid == Constants.MailDeliveryMethod.Exchange;
      
      // Не давать изменять способ доставки для исходящих писем на несколько адресатов
      if (OutgoingDocumentBases.Is(document) && OutgoingDocumentBases.As(document).IsManyAddressees == true)
      {
        _obj.State.Properties.DeliveryMethod.IsEnabled = false;
        _obj.State.Properties.ExchangeService.IsEnabled = false;
      }
      else
      {
        _obj.State.Properties.ExchangeService.IsEnabled = deliveryMethodIsExchange;
        _obj.State.Properties.ExchangeService.IsRequired = deliveryMethodIsExchange;
        
        if (deliveryMethodIsExchange)
        {
          var isIncomingDocument = Docflow.PublicFunctions.OfficialDocument.Remote.CanSendAnswer(document);
          var isFormalizedDocument = Docflow.AccountingDocumentBases.Is(document) && Docflow.AccountingDocumentBases.As(document).IsFormalized == true;
          _obj.State.Properties.DeliveryMethod.IsEnabled = !isIncomingDocument;
          _obj.State.Properties.ExchangeService.IsEnabled = !(isIncomingDocument || isFormalizedDocument);
        }
      }
    }
  }
}