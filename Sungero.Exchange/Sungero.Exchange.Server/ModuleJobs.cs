using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Exchange.ExchangeDocumentInfoServiceDocuments;

namespace Sungero.Exchange.Server
{
  public class ModuleJobs
  {
    /// <summary>
    /// Отправка подписанных ИОП.
    /// </summary>
    public virtual void SendSignedReceiptNotifications()
    {
      var boxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().ToList();
      foreach (var box in boxes)
      {
        SendSignedReceiptNotifications(box);
      }
    }

    /// <summary>
    /// Агент создания ИОП.
    /// </summary>
    public virtual void CreateReceiptNotifications()
    {
      var boxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().Where(x => x.CertificateReceiptNotifications != null).ToList();
      foreach (var box in boxes)
      {
        CreateReceiptNotifications(box);
      }
    }

    /// <summary>
    /// Агент создания задач на отправку извещений о получении документов.
    /// </summary>
    public virtual void SendReceiptNotificationTasks()
    {
      var boxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().Select(b => b.Id).ToList();
      foreach (var box in boxes)
      {
        SendReceiptNotificationTask(box);
      }
    }
    
    /// <summary>
    /// Реализация агента для конкретного ящика, чтобы можно было выполнить в транзакции.
    /// </summary>
    /// <param name="boxId">Id ящика.</param>
    private static void SendReceiptNotificationTask(int boxId)
    {
      var box = ExchangeCore.BusinessUnitBoxes.Get(boxId);
      var hasCertificate = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.CheckAllResponsibleCertificates(box, box.Responsible);
      if (!hasCertificate)
        Logger.DebugFormat("Can't start Receipt Notification Sending Task. No certificates for responsible");
      var documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotification(box, false);
      
      // Если отправить ИОПы нельзя, то новая задача не создается.
      var client = ExchangeCore.PublicFunctions.BusinessUnitBox.GetPublicClient(box) as NpoComputer.DCX.ClientApi.Client;
      var documentsToFix = new List<IExchangeDocumentInfo>();
      foreach (var documentInfo in documentInfos)
      {
        var canSendDeliveryConfirmation = true;
        try
        {
          canSendDeliveryConfirmation = client.CanSendDeliveryConfirmation(documentInfo.ServiceDocumentId, documentInfo.ServiceMessageId);
        }
        catch (Exception ex)
        {
          Logger.DebugFormat("Error while getting document from the service to generate delivery confirmation: {0}. ServiceMessageId: {1}, ServiceDocumentId: {2}",
                             ex.Message, documentInfo.ServiceDocumentId, documentInfo.ServiceMessageId);
        }
        if (!canSendDeliveryConfirmation)
          documentsToFix.Add(documentInfo);
      }

      if (documentsToFix.Any())
      {
        foreach (var info in documentsToFix)
          Transactions.Execute(() =>
                               {
                                 var exchangeInfo = ExchangeDocumentInfos.Get(info.Id);
                                 Functions.Module.FixReceiptNotification(exchangeInfo, string.Empty, false);
                               });
      }
      
      var tasks = ReceiptNotificationSendingTasks.GetAll()
        .Where(x => Equals(x.Box, box) && Equals(x.Status, Exchange.ReceiptNotificationSendingTask.Status.InProcess));
      foreach (var task in tasks)
        try
      {
        task.Abort();
        Logger.DebugFormat("Aborted Receipt Notification Sending Task {0} for box {1}", task.Id, box.Id);
      }
      catch (Exception ex)
      {
        Logger.DebugFormat("Abort task {0} failed, box {1}, exception \r\n {2}", task.Id, box.Id, ex);
      }
      
      var responsible = box.Responsible;
      documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotification(box, false);
      var previousDay = Calendar.Today.PreviousWorkingDay().EndOfDay();
      var previousDayDocumentInfos = documentInfos.Where(x => x.MessageDate <= previousDay).ToList();
      if (previousDayDocumentInfos.Any() && hasCertificate)
      {
        Logger.DebugFormat("Document infos ids without receipt notification: {0}",  string.Join(", ", previousDayDocumentInfos.Select(x => x.Id)));
        
        // Выдать права на чтение документам. Без прав ИОП не отправить.
        foreach (var documentInfo in documentInfos)
        {
          var document = documentInfo.Document;
          if (!document.AccessRights.CanRead(responsible))
          {
            document.AccessRights.Grant(responsible, DefaultAccessRightsTypes.Read);
            document.Save();
          }
        }
        
        var receiptNotificationSendingTask = Functions.Module.CreateReceiptNotificationSendingTask(box);
        receiptNotificationSendingTask.Start();
        Logger.DebugFormat("Started Receipt Notification Sending Task {0} for box {1}", receiptNotificationSendingTask.Id, box.Id);
      }
    }
    
    /// <summary>
    /// Агент получения сообщений.
    /// </summary>
    public virtual void GetMessages()
    {
      var boxes = ExchangeCore.PublicFunctions.BusinessUnitBox.Remote.GetConnectedBoxes().ToList();
      foreach (var box in boxes)
      {
        var incomingMessageId = Functions.Module.GetLastIncomingMessageId(box);
        var outgoingMessageId = Functions.Module.GetLastOutgoingMessageId(box);
        
        // Если это первый запуск - явно вызываем синхронизацию контрагентов, у неё таймаут большой.
        if (string.IsNullOrWhiteSpace(incomingMessageId) && string.IsNullOrWhiteSpace(outgoingMessageId))
        {
          ExchangeCore.PublicFunctions.Module.Remote.RequeueCounterpartySync();
        }
        
        Functions.Module.SyncMessages(box, incomingMessageId, outgoingMessageId);
      }
    }

    /// <summary>
    /// Агент конвертации тел документов.
    /// </summary>
    public virtual void BodyConverterJob()
    {
      var queueItems = ExchangeCore.BodyConverterQueueItems.GetAll()
        .Where(x => x.Retries == 0 && x.ProcessingStatus != ExchangeCore.BodyConverterQueueItem.ProcessingStatus.Processed)
        .ToList();
      // Ошибочные документы обрабатываются последними пачкой по 25.
      var repetedQueueItems = ExchangeCore.BodyConverterQueueItems.GetAll().Where(x => x.Retries > 0).OrderBy(y => y.Retries).Take(25).ToList();
      queueItems.AddRange(repetedQueueItems);
      
      foreach (var queueItem in queueItems)
      {
        if (queueItem.Document == null)
          continue;
        
        var lockInfo = queueItem.Document != null ? Locks.GetLockInfo(queueItem.Document) : null;
        if (lockInfo.IsLocked)
          continue;
        
        var generated = Transactions.Execute(
          () =>
          {
            Docflow.PublicFunctions.Module.GeneratePublicBodyForExchangeDocument(queueItem.Document, queueItem.VersionId.Value, queueItem.ExchangeState);
          });
        if (!generated)
        {
          Transactions.Execute(
            () =>
            {
              ExchangeCore.PublicFunctions.QueueItemBase.QueueItemOnError(queueItem, Resources.GeneratePublicBodyFailed);
            });
          Logger.DebugFormat("{0} Id = '{1}'.", Resources.GeneratePublicBodyFailed, queueItem.Document.Id);
        }
        else
        {
          Transactions.Execute(
            () =>
            {
              ExchangeCore.BodyConverterQueueItems.Delete(queueItem);
            });
        }
      }
      
      // Оставляем удаление завершенных элементов очереди для старых записей.
      var processedQueueItems = ExchangeCore.BodyConverterQueueItems.GetAll()
        .Where(q => Equals(q.ProcessingStatus, ExchangeCore.BodyConverterQueueItem.ProcessingStatus.Processed))
        .ToList();
      foreach (var processedQueueItem in processedQueueItems)
      {
        ExchangeCore.BodyConverterQueueItems.Delete(processedQueueItem);
      }
    }

    /// <summary>
    /// Реализация агента создания ИОП для конкретного ящика.
    /// </summary>
    /// <param name="box">Абонентский ящик нашей организации.</param>
    private static void CreateReceiptNotifications(Sungero.ExchangeCore.IBusinessUnitBox box)
    {
      var partSize = 25;
      var skip = 0;
      var certificate = box.CertificateReceiptNotifications;
      var documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotificationPart(box, skip, partSize, true);
      if (!documentInfos.Any())
        return;
      
      while (documentInfos.Any())
      {
        try
        {
          var serviceDocs = Functions.Module.GetGeneratedDeliveryConfirmationDocuments(documentInfos, box, box.CertificateReceiptNotifications, true);
          
          foreach (var doc in serviceDocs)
          {
            var info = doc.Info;
            if (info.ServiceDocuments.Any(d => d.DocumentType == doc.ReglamentDocumentType))
              continue;
            
            var serviceDocument = info.ServiceDocuments.AddNew();
            serviceDocument.DocumentType = doc.ReglamentDocumentType;
            serviceDocument.Body = doc.Content;
            serviceDocument.GeneratedName = doc.Name;
            serviceDocument.Certificate = doc.Certificate;
            
            // Выдать права на документ подписывающему ИОП.
            var document = info.Document;
            if (!document.AccessRights.CanRead(certificate.Owner))
            {
              document.AccessRights.Grant(certificate.Owner, DefaultAccessRightsTypes.Read);
              document.Save();
            }
            
            info.Save();
          }
          
          var documentsToFix = documentInfos.Where(x => !serviceDocs.Any(s => Equals(s.LinkedDocument, x.Document))).ToList();
          if (documentsToFix.Any())
            Functions.Module.FixReceiptNotification(documentsToFix, string.Empty);
        }
        catch (Exception ex)
        {
          var error = Resources.DeliveryConfirmationError;
          Logger.ErrorFormat(error, ex);
        }
        finally
        {
          skip += partSize;
          documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotificationPart(box, skip, partSize, true);
        }
      }

    }
    
    /// <summary>
    /// Реализация отправки подписанных ИОП для конкретного ящика.
    /// </summary>
    /// <param name="box">Абонентский ящик нашей организации.</param>
    private static void SendSignedReceiptNotifications(Sungero.ExchangeCore.IBusinessUnitBox box)
    {
      var partSize = 25;
      var skip = 0;
      var documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotificationPart(box, skip, partSize, false);
      if (!documentInfos.Any())
        return;
      
      while (documentInfos.Any())
      {
        try
        {
          foreach (var info in documentInfos)
          {
            Func<Enumeration?, bool> isRootDocumentReceipt = x => x == DocumentType.Receipt || x == DocumentType.IReceipt;
            var isInvoiceFlow = Functions.Module.IsInvoiceFlowDocument(info.Document);
            var documentsToSend = info.ServiceDocuments
              .Where(d => d.Sign != null && d.Date == null)
              .Select(d =>
                      {
                        var parentId = info.ServiceDocumentId;
                        var counterpartyId = info.ServiceCounterpartyId;
                        if (d.DocumentType == DocumentType.ICReceipt)
                        {
                          var parent = info.ServiceDocuments.First(s => s.DocumentType == DocumentType.IConfirmation);
                          parentId = parent.DocumentId;
                          counterpartyId = parent.CounterpartyId;
                        }
                        else if (d.DocumentType == DocumentType.IRCReceipt)
                        {
                          var parent = info.ServiceDocuments.First(s => s.DocumentType == DocumentType.IRConfirmation);
                          parentId = parent.DocumentId;
                          counterpartyId = parent.CounterpartyId;
                        }
                        else if (d.DocumentType == DocumentType.IRReceipt)
                        {
                          var parent = info.ServiceDocuments.First(s => s.DocumentType == DocumentType.IReject);
                          parentId = parent.DocumentId;
                          counterpartyId = parent.CounterpartyId;
                        }
                        return Structures.Module.ReglamentDocumentWithCertificate.Create(d.GeneratedName, d.Body, d.Certificate,
                                                                                         d.Sign, parentId, box, info.Document,
                                                                                         info.ServiceMessageId,
                                                                                         counterpartyId,
                                                                                         isRootDocumentReceipt(d.DocumentType), info,
                                                                                         isInvoiceFlow, d.DocumentType);
                      })
              .ToList();
            if (documentsToSend.Any())
              Sungero.Exchange.Functions.Module.SendDeliveryConfirmation(documentsToSend, box);
          }
        }
        catch (Exception ex)
        {
          var error = Resources.DeliveryConfirmationError;
          Logger.ErrorFormat(error, ex);
        }
        finally
        {
          skip += partSize;
          documentInfos = Functions.Module.GetDocumentInfosWithoutReceiptNotificationPart(box, skip, partSize, false);
        }
      }
    }
  }
}