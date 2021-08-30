using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CommonLibrary;
using Sungero.Commons;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalStage;
using Sungero.Docflow.DocumentKind;
using Sungero.Docflow.Structures.Module;
using Sungero.Domain;
using Sungero.Domain.LinqExpressions;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using Sungero.Parties;
using Sungero.Workflow;
using AppMonitoringType = Sungero.Content.AssociatedApplication.MonitoringType;
using ExchDocumentType = Sungero.Exchange.ExchangeDocumentInfoServiceDocuments.DocumentType;
using MessageType = Sungero.Exchange.ExchangeDocumentInfo.MessageType;
using ReportResources = Sungero.Docflow.Reports.Resources;

namespace Sungero.Docflow.Server
{
  public class ModuleFunctions
  {
    
    #region Integration Service
    
    #region Export Templates
    
    /// <summary>
    /// Получить Id шаблона.
    /// </summary>
    /// <param name="name">Имя шаблона.</param>
    /// <returns>Id шаблона.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public int GetTemplateId(string name)
    {
      var template = DocumentTemplates.GetAll().FirstOrDefault(x => x.Name == name);
      if (template == null)
        return -1;
      return template.Id;
    }
    
    /// <summary>
    /// Получить Id всех шаблонов.
    /// </summary>
    /// <returns>Список Id всех шаблонов, упакованный в base64.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public string GetAllTemplatesIds()
    {
      var ids = DocumentTemplates.GetAll().Select(x => x.Id).Distinct().ToList();
      return Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(";", ids)));
    }
    
    /// <summary>
    /// Получение тела шаблона в виде строки, закодированной в Base64.
    /// </summary>
    /// <param name="id">ID шаблона.</param>
    /// <returns>Строка, содержащая тело шаблона, закодированное в Base64.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public string ExportDocumentTemplateBody(int id)
    {
      using (var ms = new MemoryStream())
      {
        DocumentTemplates.Get(id).LastVersion.Body.Read().CopyTo(ms);
        return Convert.ToBase64String(ms.ToArray());
      }
    }
    
    /// <summary>
    /// Получение превью шаблона в виде строки, закодированной в Base64.
    /// </summary>
    /// <param name="id">ID шаблона.</param>
    /// <returns>Строка, содержащая превью шаблона, закодированное в Base64.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public string ExportDocumentTemplatePreview(int id)
    {
      var preview = DocumentTemplates.Get(id).PreviewTemplate;
      if (preview == null)
        preview = new byte[0];
      return Convert.ToBase64String(preview);
    }
    
    /// <summary>
    /// Получение метаданных шаблона в виде строки.
    /// </summary>
    /// <param name="id">ID шаблона.</param>
    /// <returns>Строка, содержащая метаданные шаблона.</returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public string ExportDocumentTemplateMetadata(int id)
    {
      var mappingStr = "{ \"Number\": @Number, \"DataSource\": \"@DataSource\", \"Name\": \"@Name\" }";
      
      var jsonStr = "{ \"DocumentType\": \"@DocumentType\", \"Name\": \"@Name\", \"Extension\": \"@Extension\", \"Mapping\": [ @MappingList ] }";
      
      var template = DocumentTemplates.Get(id);
      jsonStr = jsonStr.Replace("@DocumentType", template.DocumentType.HasValue ? template.DocumentType.Value.ToString() : string.Empty);
      jsonStr = jsonStr.Replace("@Name", CommonLibrary.FileUtils.NormalizeFileName(template.Name));
      jsonStr = jsonStr.Replace("@Extension", template.AssociatedApplication.Extension);
      
      var mappingList = string.Empty;
      foreach (var parameter in template.Parameters)
      {
        mappingList += mappingStr
          .Replace("@DataSource", parameter.DataSource)
          .Replace("@Name", parameter.Name)
          .Replace("@Number", parameter.Number.Value.ToString()) + ", ";
      }
      
      mappingList = mappingList.TrimEnd(' ').TrimEnd(',');
      jsonStr = jsonStr.Replace("@MappingList", mappingList);
      
      return jsonStr;
    }
    
    #endregion
    
    /// <summary>
    /// Импортировать шаблон документа.
    /// </summary>
    /// <param name="name">Имя шаблона.</param>
    /// <param name="documentType">Тип документа.</param>
    /// <param name="documentKindsGuids">Гуиды видов документов.</param>
    /// <param name="body">Тело шаблона.</param>
    /// <param name="extension">Расширение файла.</param>
    /// <param name="preview">Изображение для предпросмотра шаблона.</param>
    [Public(WebApiRequestType = RequestType.Post)]
    public void ImportDocumentTemplate(string name, Guid documentType, List<Guid> documentKindsGuids,
                                       byte[] body, string extension, byte[] preview)
    {
      var template = DocumentTemplates.GetAll().Where(t => t.Name == name).FirstOrDefault();
      if (template == null)
      {
        template = DocumentTemplates.Create();
        
        // Виды документов. Заполнить только для новых.
        FillKindsByGuid(template, documentKindsGuids);
      }
      else
      {
        template.Parameters.Clear();
      }
      
      // Заполнить общие свойства.
      template.Name = name;
      template.DocumentType = documentType;
      
      // Заполнить тело.
      if (body.Length != 0)
      {
        using (var stream = new MemoryStream(body))
        {
          template.CreateVersionFrom(stream, extension);
        }
      }
      
      // Заполнить предпросмотр.
      if (preview.Length != 0)
        template.PreviewTemplate = preview;
      
      template.Save();
    }
    
    /// <summary>
    /// Добавить параметр в шаблон.
    /// </summary>
    /// <param name="templateName">Имя шаблона.</param>
    /// <param name="number">Номер п/п.</param>
    /// <param name="name">Имя параметра.</param>
    /// <param name="parameterValue">Значение параметра.</param>
    [Public(WebApiRequestType = RequestType.Post)]
    public void AddParameterToDocumentTemplate(string templateName, int number, string name, string parameterValue)
    {
      var template = DocumentTemplates.GetAll().Where(t => t.Name == templateName).FirstOrDefault();
      if (template == null)
        return;
      
      var templateParameter = template.Parameters.AddNew();
      templateParameter.Number = number;
      templateParameter.Name = name;
      templateParameter.DataSource = parameterValue;
      
      template.Save();
    }
    
    /// <summary>
    /// Заполнить виды документов в шаблонах по guid.
    /// </summary>
    /// <param name="documentTemplate">Шаблон документа.</param>
    /// <param name="kindGuids">Список guid, с которыми были созданы виды при инициализации.</param>
    /// <remarks>Виды документов ищутся по связке (guid экземпляра, id записи) в ExternalLink.</remarks>
    private static void FillKindsByGuid(IDocumentTemplate documentTemplate, List<Guid> kindGuids)
    {
      // GUID для значения "<Любые документы>" у свойства Тип документа в шаблонах.
      var allDocumentTypeGuid = Guid.Parse(Sungero.Docflow.PublicConstants.DocumentTemplate.AllDocumentTypeGuid);

      foreach (var kindGuid in kindGuids)
      {
        var documentKind = Sungero.Docflow.PublicFunctions.DocumentKind.Remote.GetNativeDocumentKindRemote(kindGuid);
        var typeGuid = Guid.Parse(documentKind.DocumentType.DocumentTypeGuid);

        if (documentKind != null && documentKind.Status == Sungero.Docflow.DocumentKind.Status.Active &&
            (documentTemplate.DocumentType == allDocumentTypeGuid || documentTemplate.DocumentType == typeGuid))
        {
          var kind = documentTemplate.DocumentKinds.AddNew();
          kind.DocumentKind = documentKind;
        }
      }
    }
    
    /// <summary>
    /// Удалить все шаблоны.
    /// </summary>
    [Public(WebApiRequestType = RequestType.Post)]
    public void DeleteAllDocumentTemplate()
    {
      var templates = DocumentTemplates.GetAll().ToList();
      foreach (var template in templates)
        DocumentTemplates.Delete(template);
    }
    
    #endregion
    
    #region Генерация PublicBody для неформализованных документов
    
    /// <summary>
    /// Генерация PublicBody для неформализованного документа.
    /// </summary>
    /// <param name="document">Документ, для которого необходимо сформировать PublicBody.</param>
    /// <param name="versionId">Id версии документа, для которой необходимо сформировать PublicBody.</param>
    [Public, Remote]
    public static void GeneratePublicBodyForNonformalizedDocument(Sungero.Docflow.IOfficialDocument document, int versionId)
    {
      if (document == null)
        return;

      var version = document.Versions.SingleOrDefault(v => v.Id == versionId);
      if (version == null)
        throw AppliedCodeException.Create(string.Format("Version with id {0} not found.", versionId));
      
      var pdfConverter = new AsposeExtensions.Converter();
      
      System.IO.Stream pdfDocumentStream = null;
      using (var inputStream = new System.IO.MemoryStream())
      {
        version.Body.Read().CopyTo(inputStream);
        try
        {
          pdfDocumentStream = pdfConverter.GeneratePdf(inputStream, version.BodyAssociatedApplication.Extension);
        }
        catch (AsposeExtensions.PdfConvertException e)
        {
          Logger.Error(Docflow.Resources.PdfConvertErrorFormat(document.Id), e.InnerException);
        }
      }
      
      // Если не сгенерировалась pdf - значит, не поддерживается формат.
      if (pdfDocumentStream == null)
        return;
      
      var signatureStamp = CreateAndGetSignaturesStampNonFormalized(document, versionId);
      NpoComputer.DpadCP.Converter.Stamps.Stamper.AddDirectumStamps(pdfDocumentStream, signatureStamp, GetPdfConverterPath());
      
      version.PublicBody.Write(pdfDocumentStream);
      version.AssociatedApplication = Content.AssociatedApplications.GetByExtension("pdf");
      document.Save();
      pdfDocumentStream.Close();
    }
    
    /// <summary>
    /// Создать и вернуть штамп с информацией о подписях неформализованного документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="versionId">Версия документа.</param>
    /// <returns>Сформированный штамп.</returns>
    private static NpoComputer.DpadCP.Converter.Stamps.SignatureStampGenerator CreateAndGetSignaturesStampNonFormalized(Docflow.IOfficialDocument document, int versionId)
    {
      var documentInfo = Sungero.Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetExDocumentInfoFromVersion(document, versionId);

      var stampInfo = new NpoComputer.DpadCP.Converter.Stamps.DocumentInfo();
      stampInfo.Title = documentInfo.RootBox.ExchangeService.Name;
      stampInfo.DocumentId = documentInfo.ServiceDocumentId;
      var stampGenerator = new NpoComputer.DpadCP.Converter.Stamps.SignatureStampGenerator(stampInfo);
      
      var version = document.Versions.Single(v => v.Id == versionId);
      var exchangeStatus = documentInfo.ExchangeState;

      var senderSign = Signatures.Get(document).Where(s => s.Id == documentInfo.SenderSignId).FirstOrDefault();
      if (senderSign != null)
      {
        var stampSignStatus = Docflow.OfficialDocument.ExchangeState.Signed;
        stampGenerator.Signatures.Add(GetStampTableRowForSignature(senderSign, stampSignStatus));
      }

      if (exchangeStatus == Docflow.OfficialDocument.ExchangeState.Signed && documentInfo.ReceiverSignId != null)
      {
        var receiverSign = Signatures.Get(document).Where(s => s.Id == documentInfo.ReceiverSignId).FirstOrDefault();
        stampGenerator.Signatures.Add(GetStampTableRowForSignature(receiverSign, exchangeStatus));
      }
      else
      {
        var receiverName = documentInfo.MessageType == Sungero.Exchange.ExchangeDocumentInfo.MessageType.Incoming ?
          documentInfo.RootBox.BusinessUnit.Name : documentInfo.Counterparty.Name;

        var needWarningIcon = exchangeStatus != Docflow.OfficialDocument.ExchangeState.Received && exchangeStatus != Docflow.OfficialDocument.ExchangeState.Sent;
        
        // Никогда не пишем "отправлен КА", пишем "получен".
        if (exchangeStatus == Docflow.OfficialDocument.ExchangeState.Sent)
          exchangeStatus = Docflow.OfficialDocument.ExchangeState.Received;
        
        // Штампы на русском языке, т.к. иначе в налоговой такие документы вряд ли примут.
        var localizedStatus = string.Empty;
        var russianCulture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
        using (Sungero.Core.CultureInfoExtensions.SwitchTo(russianCulture))
        {
          localizedStatus = OfficialDocuments.Info.Properties.ExchangeState.GetLocalizedValue(exchangeStatus);
          
          // Если документ уже утверждён, но ещё не отправлен в сервис.
          if (exchangeStatus == Docflow.OfficialDocument.ExchangeState.SignRequired &&
              Signatures.Get(version).Any(s => s.SignatureType == SignatureType.Approval && s.Id != documentInfo.SenderSignId))
            localizedStatus = AccountingDocumentBases.Resources.PdfStampSignedNotSended;
        }
        
        stampGenerator.Signatures.Add(new NpoComputer.Dpad.Converter.SignatureInfo()
                                      {
                                        SigningDate = localizedStatus,
                                        OrganizationInfo = receiverName,
                                        CertificateIssuer = "-",
                                        SignIcon = needWarningIcon ? NpoComputer.DpadCP.Converter.SignIcon.Warning : NpoComputer.DpadCP.Converter.SignIcon.None
                                      });
      }
      
      return stampGenerator;
    }
    
    #endregion
    
    #region Генерация PublicBody для формализованных документов
    
    /// <summary>
    /// Генерация PublicBody формализованного документа.
    /// </summary>
    /// <param name="document">Документ, для которого необходимо сформировать PublicBody.</param>
    /// <param name="versionId">Id версии документа, для которой необходимо сформировать PublicBody.</param>
    /// <param name="exchangeStatus">Статус подписания, который нужно проставить.</param>
    [Public, Remote]
    public static void GeneratePublicBodyForFormalizedDocument(Sungero.Docflow.IAccountingDocumentBase document,
                                                               int versionId, Enumeration? exchangeStatus)
    {
      var exchangeStatusValue = exchangeStatus != null ? exchangeStatus.Value.Value : string.Empty;
      Logger.DebugFormat("Generate Public Body For Formalized Document Id {0}, Version Id {1}, ExchangeStatus '{2}'", document.Id, versionId, exchangeStatusValue);
      if (document.IsFormalized != true)
        return;
      
      var neededVersion = document.Versions.FirstOrDefault(x => x.Id == versionId);
      if (neededVersion == null)
        return;
      
      var sellerTitleVersion = GetDocumentSellerTitleVersion(document);
      var buyerTitleVersion = neededVersion == sellerTitleVersion ? null : GetDocumentBuyerTitleVersion(document);

      System.IO.Stream pdfContentStream;
      if (exchangeStatus != null)
      {
        var stamp = CreateAndGetSignaturesStamp(document, sellerTitleVersion, buyerTitleVersion, exchangeStatus);
        pdfContentStream = GeneratePdfForDocumentTitles(sellerTitleVersion, buyerTitleVersion, stamp);
        
        var documentInfo = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetExDocumentInfoFromVersion(document, sellerTitleVersion.Id);
        var pageStamp = new NpoComputer.DpadCP.Converter.Stamps.DocumentInfo();
        pageStamp.DocumentId = documentInfo.ServiceDocumentId;
        pageStamp.Title = documentInfo.RootBox.ExchangeService.Name;
        NpoComputer.DpadCP.Converter.Stamps.Stamper.AddPaginationStamp(pdfContentStream, pageStamp);
      }
      else
      {
        pdfContentStream = GeneratePdfForDocumentTitles(sellerTitleVersion, buyerTitleVersion, null);
      }
      
      SavePdfStreamToPublicBody(neededVersion, pdfContentStream);
    }
    
    /// <summary>
    /// Генерация PublicBody документа из сервиса обмена.
    /// </summary>
    /// <param name="document">Документ, для которого необходимо сформировать PublicBody.</param>
    /// <param name="versionId">Id версии документа, для которой необходимо сформировать PublicBody.</param>
    /// <param name="exchangeState">Статус документа в сервисе обмена.</param>
    [Public]
    public virtual void GeneratePublicBodyForExchangeDocument(IOfficialDocument document, int versionId, Enumeration? exchangeState)
    {
      var accountingDocument = Docflow.AccountingDocumentBases.As(document);
      if (accountingDocument != null && accountingDocument.IsFormalized == true)
        Docflow.PublicFunctions.Module.Remote.GeneratePublicBodyForFormalizedDocument(accountingDocument, versionId, exchangeState);
      else
        Docflow.PublicFunctions.Module.Remote.GeneratePublicBodyForNonformalizedDocument(document, versionId);
    }
    
    /// <summary>
    /// Генерация PublicBody по содержимому xml.
    /// </summary>
    /// <param name="xml">Xml.</param>
    /// <returns>Pdf.</returns>
    [Remote, Public]
    public static Structures.Module.IByteArray GeneratePublicBodyForFormalizedXml(Structures.Module.IByteArray xml)
    {
      using (var memory = new System.IO.MemoryStream(xml.Bytes))
      {
        var pdfContentStream = GeneratePdfForDocumentTitles(memory, null, null) as System.IO.MemoryStream;
        return Structures.Module.ByteArray.Create(pdfContentStream.ToArray());
      }
    }
    
    /// <summary>
    /// Получить путь до exe конвертера html в pdf.
    /// </summary>
    /// <returns>Путь до exe.</returns>
    private static string GetPdfConverterPath()
    {
      var baseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
      var binaries = System.IO.Path.Combine(baseDirectory, "bin");
      var pdfConverterExeFullPath = System.IO.Path.Combine(binaries, Docflow.Constants.Module.PdfConverterExeName);
      if (!System.IO.File.Exists(pdfConverterExeFullPath))
        pdfConverterExeFullPath = System.IO.Path.Combine(baseDirectory, Docflow.Constants.Module.PdfConverterExeName);
      
      return pdfConverterExeFullPath;
    }
    
    /// <summary>
    /// Генерация временного публичного тела документа.
    /// Сгенерированное тело используется для показа содержимого документа, пока генерируется финальное представление документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="versionId">Идентификатор версии документа, в PublicBody которой необходимо сохранить PDF.</param>
    [Public]
    public static void GenerateTempPublicBodyForExchangeDocument(Sungero.Docflow.IOfficialDocument document, int versionId)
    {
      try
      {
        var neededVersion = document.Versions.FirstOrDefault(x => x.Id == versionId);
        if (neededVersion == null || neededVersion.PublicBody.Size == 0)
          return;
        
        var lockInfo = Locks.GetLockInfo(neededVersion.PublicBody);
        if (lockInfo.IsLocked)
          return;
        // Поток будет закрыт после сохранения тела платформой.
        var pdfContentStream = new System.IO.MemoryStream();
        using (var sourceStream = neededVersion.PublicBody.Read())
          sourceStream.CopyTo(pdfContentStream);
        
        NpoComputer.DpadCP.Converter.Stamps.Stamper.AddTempStamp(pdfContentStream);
        
        SavePdfStreamToPublicBody(neededVersion, pdfContentStream);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Error generate temp public body.", ex);
      }
    }

    /// <summary>
    /// Создать и вернуть штамп с информацией о подписях документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="sellerTitleVersion">Версия с титулом продавца.</param>
    /// <param name="buyerTitleVersion">Версия с титулом покупателя.</param>
    /// <param name="exchangeStatus">Статус подписания, который нужно проставить.</param>
    /// <returns>Сформированный штамп.</returns>
    private static NpoComputer.DpadCP.Converter.Stamps.SignatureStampGenerator CreateAndGetSignaturesStamp(Docflow.IAccountingDocumentBase document,
                                                                                                           Content.IElectronicDocumentVersions sellerTitleVersion,
                                                                                                           Content.IElectronicDocumentVersions buyerTitleVersion,
                                                                                                           Enumeration? exchangeStatus)
    {
      var documentInfo = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetExDocumentInfoFromVersion(document, sellerTitleVersion.Id);
      
      var stampInfo = new NpoComputer.DpadCP.Converter.Stamps.DocumentInfo();
      stampInfo.Title = documentInfo.RootBox.ExchangeService.Name;
      stampInfo.DocumentId = documentInfo.ServiceDocumentId;
      var stampGenerator = new NpoComputer.DpadCP.Converter.Stamps.SignatureStampGenerator(stampInfo);
      
      var sellerTitleSign = Signatures.Get(sellerTitleVersion).Where(x => x.Id == document.SellerSignatureId).FirstOrDefault();
      if (sellerTitleSign != null)
      {
        var stampSignStatus = (exchangeStatus == Docflow.OfficialDocument.ExchangeState.Obsolete || exchangeStatus == Docflow.OfficialDocument.ExchangeState.Terminated) ?
          exchangeStatus :
          Docflow.OfficialDocument.ExchangeState.Signed;
        
        stampGenerator.Signatures.Add(GetStampTableRowForSignature(sellerTitleSign, stampSignStatus));
      }
      
      if (buyerTitleVersion != null && document.BuyerSignatureId != null)
      {
        var buyerTitleSign = Signatures.Get(buyerTitleVersion).Where(x => x.Id == document.BuyerSignatureId).First();
        if (buyerTitleSign != null)
        {
          stampGenerator.Signatures.Add(GetStampTableRowForSignature(buyerTitleSign, exchangeStatus));
        }
      }
      else
      {
        var buyerName = string.Empty;
        if (documentInfo.MessageType == Exchange.ExchangeDocumentInfo.MessageType.Incoming)
          buyerName = documentInfo.RootBox.BusinessUnit.Name;
        else
          buyerName = documentInfo.Counterparty.Name;
        
        var needWarningIcon = exchangeStatus != Docflow.OfficialDocument.ExchangeState.Received && exchangeStatus != Docflow.OfficialDocument.ExchangeState.Sent;
        
        // Никогда не пишем "отправлен КА", пишем "получен".
        if (exchangeStatus == Docflow.OfficialDocument.ExchangeState.Sent)
          exchangeStatus = Docflow.OfficialDocument.ExchangeState.Received;
        
        // Штампы на русском языке, т.к. иначе в налоговой такие документы вряд ли примут.
        var localizedStatus = string.Empty;
        var russianCulture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
        using (Sungero.Core.CultureInfoExtensions.SwitchTo(russianCulture))
        {
          localizedStatus = OfficialDocuments.Info.Properties.ExchangeState.GetLocalizedValue(exchangeStatus);
          
          // Если документ уже утверждён, но ещё не отправлен в сервис.
          if (exchangeStatus == Docflow.OfficialDocument.ExchangeState.SignRequired &&
              buyerTitleVersion != null &&
              Signatures.Get(buyerTitleVersion).Any(s => s.SignatureType == SignatureType.Approval))
            localizedStatus = AccountingDocumentBases.Resources.PdfStampSignedNotSended;
        }
        
        stampGenerator.Signatures.Add(new NpoComputer.Dpad.Converter.SignatureInfo()
                                      {
                                        SigningDate = localizedStatus,
                                        OrganizationInfo = buyerName,
                                        CertificateIssuer = "-",
                                        SignIcon = needWarningIcon ? NpoComputer.DpadCP.Converter.SignIcon.Warning : NpoComputer.DpadCP.Converter.SignIcon.None
                                      });
      }
      
      return stampGenerator;
    }
    
    /// <summary>
    /// Получить информацию о подписанте.
    /// </summary>
    /// <param name="signatory">Информация о подписи.</param>
    /// <param name="exchangeStatus">Статус подписания, который нужно проставить.</param>
    /// <returns>Информация о подписывающем.</returns>
    private static NpoComputer.Dpad.Converter.SignatureInfo GetStampTableRowForSignature(Sungero.Domain.Shared.ISignature signatory, Enumeration? exchangeStatus)
    {
      var certificate = Docflow.PublicFunctions.Module.GetSignatureCertificate(signatory.GetDataSignature());
      var subject = certificate.SubjectName.Format(true);
      var issuer = ParseIssuerObject(certificate.IssuerName.Format(true));
      
      var signatureInfo = Sungero.Docflow.Functions.ApprovalReviewAssignment.ParseSignatureSubject(subject);
      
      var signValidString = string.Empty;
      
      if (exchangeStatus == Docflow.OfficialDocument.ExchangeState.SignRequired ||
          exchangeStatus == Docflow.OfficialDocument.ExchangeState.Signed)
      {
        signValidString += signatory.IsValid
          ? Sungero.Docflow.AccountingDocumentBases.Resources.PdfStampSignIsValid
          : Sungero.Docflow.AccountingDocumentBases.Resources.PdfStampSignIsInvalid;
      }
      else
      {
        // Штампы на русском языке, т.к. иначе в налоговой такие документы вряд ли примут.
        var russianCulture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
        using (Sungero.Core.CultureInfoExtensions.SwitchTo(russianCulture))
          signValidString = OfficialDocuments.Info.Properties.ExchangeState.GetLocalizedValue(exchangeStatus);
      }
      
      var signatureName = GetCertificateOwnerShortName(signatureInfo);
      if (!string.IsNullOrEmpty(signatureInfo.JobTitle))
        signatureName += string.Format(", {0}", signatureInfo.JobTitle);
      
      // Если документ анулирован, то изменяется формат вывода статуса и даты подписания в штампе.
      if (exchangeStatus == Docflow.OfficialDocument.ExchangeState.Obsolete || exchangeStatus == Docflow.OfficialDocument.ExchangeState.Terminated)
      {
        return new NpoComputer.Dpad.Converter.SignatureInfo()
        {
          CertificateIssuer = issuer.Name,
          CertificateSerialNumber = certificate.SerialNumber,
          SignIcon = NpoComputer.DpadCP.Converter.SignIcon.Sign,
          OrganizationInfo = signatureInfo.OrganizationName,
          PersonInfo = signatureName,
          Status = signValidString
        };
      }
      else
      {
        return new NpoComputer.Dpad.Converter.SignatureInfo()
        {
          CertificateIssuer = issuer.Name,
          CertificateSerialNumber = certificate.SerialNumber,
          SignIcon = NpoComputer.DpadCP.Converter.SignIcon.Sign,
          OrganizationInfo = signatureInfo.OrganizationName,
          PersonInfo = signatureName,
          Status = signValidString,
          SigningDate = signatory.SigningDate.ToUniversalTime().Add(TenantInfo.UtcOffset).ToString("dd.MM.yyyy HH:mm:ss (UTCzzz)")
        };
      }
    }
    
    /// <summary>
    /// Записать содержимое потока в PublicBody версии документа.
    /// </summary>
    /// <param name="documentVersion">Версия документа.</param>
    /// <param name="contentStream">Поток с содержимым версии документа.</param>
    private static void SavePdfStreamToPublicBody(Content.IElectronicDocumentVersions documentVersion, System.IO.Stream contentStream)
    {
      documentVersion.PublicBody.Write(contentStream);
      documentVersion.AssociatedApplication = Content.AssociatedApplications.GetAll().FirstOrDefault(x => x.Extension == "pdf");
      documentVersion.ElectronicDocument.Save();
    }
    
    /// <summary>
    /// Сгенерировать PDF на основании переданных титулов документа.
    /// </summary>
    /// <param name="sellerTitleVersion">Версия с титулом продавца.</param>
    /// <param name="buyerTitleVersion">Версия с титулом покупателя.</param>
    /// <param name="signaturesStamp">Штамп с информацией о подписях.</param>
    /// <returns>Поток с содержимым PDF.</returns>
    private static System.IO.Stream GeneratePdfForDocumentTitles(Content.IElectronicDocumentVersions sellerTitleVersion,
                                                                 Content.IElectronicDocumentVersions buyerTitleVersion,
                                                                 NpoComputer.DpadCP.Converter.Stamps.IHtmlContentGenerator signaturesStamp)
    {
      System.IO.Stream stream;
      
      using (var sellerContentStream = new System.IO.MemoryStream())
      {
        using (var sourceStream = sellerTitleVersion.Body.Read())
          sourceStream.CopyTo(sellerContentStream);
        
        if (buyerTitleVersion != null)
        {
          using (var buyerContentStream = new System.IO.MemoryStream())
          {
            using (var sourceStream = buyerTitleVersion.Body.Read())
              sourceStream.CopyTo(buyerContentStream);
            
            stream = GeneratePdfForDocumentTitles(sellerContentStream, buyerContentStream, signaturesStamp);
          }
        }
        else
        {
          stream = GeneratePdfForDocumentTitles(sellerContentStream, null, signaturesStamp);
        }
      }
      return stream;
    }
    
    /// <summary>
    /// Сгенерировать PDF на основании переданных титулов документа.
    /// </summary>
    /// <param name="sellerTitle">Титул продавца.</param>
    /// <param name="buyerTitle">Титул покупателя.</param>
    /// <param name="signaturesStamp">Штамп с информацией о подписях.</param>
    /// <returns>Поток с содержимым PDF.</returns>
    private static System.IO.Stream GeneratePdfForDocumentTitles(System.IO.Stream sellerTitle,
                                                                 System.IO.Stream buyerTitle,
                                                                 NpoComputer.DpadCP.Converter.Stamps.IHtmlContentGenerator signaturesStamp)
    {
      NpoComputer.DpadCP.Converter.IFormalizedDocument document;
      
      var options = new NpoComputer.DpadCP.Converter.ConverterOptions()
      {
        ToolPath = GetPdfConverterPath(),
        Margins = new NpoComputer.DpadCP.Converter.PageMargins() { Bottom = 30 }
      };
      
      if (buyerTitle != null)
        document = NpoComputer.DpadCP.Converter.FormalizedDocumentFactory.CreateDocument(sellerTitle, buyerTitle);
      else
        document = NpoComputer.DpadCP.Converter.FormalizedDocumentFactory.CreateDocument(sellerTitle);

      if (signaturesStamp != null)
        return document.ConvertToPdfWithStamp(signaturesStamp, options);
      else
        return document.ConvertToPdf(options);
    }
    
    /// <summary>
    /// Получить версию, содержающую титул продавца.
    /// </summary>
    /// <param name="document">Документ, для которого необходимо вернуть версию с титулом продавца.</param>
    /// <returns>Версия с титулом продавца.</returns>
    private static Sungero.Content.IElectronicDocumentVersions GetDocumentSellerTitleVersion(Sungero.Docflow.IAccountingDocumentBase document)
    {
      return document.Versions.FirstOrDefault(x => x.Id == document.SellerTitleId);
    }
    
    /// <summary>
    /// Получить версию, содержающую титул покупателя.
    /// </summary>
    /// <param name="document">Документ, для которого необходимо вернуть версию с титулом покупателя.</param>
    /// <returns>Версию с титулом покупателя.</returns>
    private static Sungero.Content.IElectronicDocumentVersions GetDocumentBuyerTitleVersion(Sungero.Docflow.IAccountingDocumentBase document)
    {
      if (document.BuyerTitleId != null)
        return document.Versions.FirstOrDefault(x => x.Id == document.BuyerTitleId);
      
      return null;
    }
    
    #endregion
    
    #region Отметка об ЭП
    
    /// <summary>
    /// Сгенерировать PublicBody документа с отметкой об ЭП.
    /// </summary>
    /// <param name="document">Документ для преобразования.</param>
    /// <param name="versionId">Id версии, для генерации.</param>
    /// <param name="signatureMark">Отметка об ЭП (html).</param>
    /// <returns>Информация о результате генерации PublicBody для версии документа.</returns>
    public virtual Structures.OfficialDocument.СonversionToPdfResult GeneratePublicBodyWithSignatureMark(Sungero.Docflow.IOfficialDocument document, int versionId, string signatureMark)
    {
      var info = Structures.OfficialDocument.СonversionToPdfResult.Create();
      info.HasErrors = true;
      var version = document.Versions.SingleOrDefault(v => v.Id == versionId);
      if (version == null)
      {
        info.HasConvertionError = true;
        info.ErrorMessage = OfficialDocuments.Resources.NoVersionWithNumberErrorFormat(versionId);
        return info;
      }
      
      System.IO.Stream pdfDocumentStream = null;
      using (var inputStream = new System.IO.MemoryStream())
      {
        version.Body.Read().CopyTo(inputStream);
        try
        {
          var pdfConverter = new AsposeExtensions.Converter();
          var extension = version.BodyAssociatedApplication.Extension;
          pdfDocumentStream = pdfConverter.GeneratePdf(inputStream, extension);
          var htmlStampString = signatureMark;
          pdfDocumentStream = pdfConverter.AddSignatureMark(pdfDocumentStream, extension, htmlStampString, Docflow.Resources.SignatureMarkAnchorSymbol,
                                                            Docflow.Constants.Module.SearchablePagesLimit);
        }
        catch (Exception e)
        {
          if (e is AsposeExtensions.PdfConvertException)
            Logger.Error(Docflow.Resources.PdfConvertErrorFormat(document.Id), e.InnerException);
          else
            Logger.Error(string.Format("{0} {1}", Docflow.Resources.PdfConvertErrorFormat(document.Id), e.Message));
          
          info.HasConvertionError = true;
          info.HasLockError = false;
          info.ErrorMessage = Docflow.Resources.DocumentBodyNeedsRepair;
        }
      }
      
      if (!string.IsNullOrWhiteSpace(info.ErrorMessage))
        return info;
      
      version.PublicBody.Write(pdfDocumentStream);
      version.AssociatedApplication = Content.AssociatedApplications.GetByExtension("pdf");
      pdfDocumentStream.Close();

      try
      {
        document.Save();
        info.HasErrors = false;
      }
      catch (Sungero.Domain.Shared.Exceptions.RepeatedLockException e)
      {
        Logger.Error(e.Message);
        info.HasConvertionError = false;
        info.HasLockError = true;
        info.ErrorMessage = e.Message;
      }
      catch (Exception e)
      {
        Logger.Error(e.Message);
        info.HasConvertionError = true;
        info.HasLockError = false;
        info.ErrorMessage = e.Message;
      }
      
      return info;
    }
    
    /// <summary>
    /// Получить отметку об ЭП.
    /// </summary>
    /// <param name="document">Документ для преобразования.</param>
    /// <param name="versionId">Id версии, для генерации.</param>
    /// <returns>Изображение отметки об ЭП в виде html.</returns>
    [Public]
    public virtual string GetSignatureMarkAsHtml(Sungero.Docflow.IOfficialDocument document, int versionId)
    {
      var signature = Functions.OfficialDocument.GetSignatureForMark(document, versionId);
      if (signature == null)
        throw new Exception(OfficialDocuments.Resources.LastVersionNotApproved);
      
      // В случае квалифицированной ЭП информацию для отметки брать из атрибутов субъекта сертификата.
      if (signature.SignCertificate != null)
        return this.GetSignatureMarkForCertificateAsHtml(signature);
      
      // В случае простой ЭП информацию для отметки брать из атрибутов подписи.
      return this.GetSignatureMarkForSimpleSignatureAsHtml(signature);
    }
    
    /// <summary>
    /// Получить отметку об ЭП для подписи.
    /// </summary>
    /// <param name="signature">Подпись.</param>
    /// <returns>Изображение отметки об ЭП для подписи в виде html.</returns>
    [Public]
    public virtual string GetSignatureMarkForSimpleSignatureAsHtml(Sungero.Domain.Shared.ISignature signature)
    {
      if (signature == null)
        return string.Empty;
      
      var signatoryFullName = signature.SignatoryFullName;
      var signatoryId = signature.Signatory.Id;
      
      string html = Resources.HtmlStampTemplateForSignature;
      html = html.Replace("{SignatoryFullName}", signatoryFullName);
      html = html.Replace("{SignatoryId}", signatoryId.ToString());
      return html;
    }
    
    /// <summary>
    /// Получить отметку об ЭП для сертификата из подписи.
    /// </summary>
    /// <param name="signature">Подпись.</param>
    /// <returns>Изображение отметки об ЭП для сертификата в виде html.</returns>
    [Public]
    public virtual string GetSignatureMarkForCertificateAsHtml(Sungero.Domain.Shared.ISignature signature)
    {
      if (signature == null)
        return string.Empty;
      
      var certificate = signature.SignCertificate;
      if (certificate == null)
        return string.Empty;
      
      var certificateSubject = this.GetCertificateSubject(signature);
      
      var signatoryFullName = string.Format("{0} {1}", certificateSubject.Surname, certificateSubject.GivenName).Trim();
      if (string.IsNullOrEmpty(signatoryFullName))
        signatoryFullName = certificateSubject.CounterpartyName;
      
      string html = Resources.HtmlStampTemplateForCertificate;
      html = html.Replace("{SignatoryFullName}", signatoryFullName);
      html = html.Replace("{Thumbprint}", certificate.Thumbprint.ToLower());
      var validity = string.Format("{0} {1} {2} {3}",
                                   Company.Resources.From,
                                   certificate.NotBefore.Value.ToShortDateString(),
                                   Company.Resources.To,
                                   certificate.NotAfter.Value.ToShortDateString());
      html = html.Replace("{Validity}", validity);
      return html;
    }
    
    /// <summary>
    /// Получение атрибутов субъекта сертификата из подписи.
    /// </summary>
    /// <param name="signature">Подпись.</param>
    /// <returns>Атрибуты субъекта сертификата из подписи.</returns>
    [Public]
    public virtual ICertificateSubject GetCertificateSubject(Sungero.Domain.Shared.ISignature signature)
    {
      var dataSignature = signature.GetDataSignature();
      var certificate = Docflow.PublicFunctions.Module.GetSignatureCertificate(dataSignature);
      var subject = certificate.SubjectName.Format(true);
      var signatureInfo = Functions.ApprovalReviewAssignment.ParseSignatureSubject(subject);
      
      return signatureInfo;
    }
    
    /// <summary>
    /// Определить возможность интерактивной конвертации документа.
    /// </summary>
    /// <param name="document">Документ для преобразования.</param>
    /// <returns>True - возможно, False - иначе.</returns>
    public virtual bool CanConvertToPdfInteractively(Sungero.Docflow.IOfficialDocument document)
    {
      var supportedFormatsList = new List<string>() { "pdf", "docx", "doc", "odt", "rtf" };
      
      return document.LastVersion.Body.Size < Constants.OfficialDocument.MaxBodySizeForInteractiveConvertation &&
        (Locks.GetLockInfo(document).IsLockedByMe || !Locks.GetLockInfo(document).IsLocked) &&
        supportedFormatsList.Contains(document.LastVersion.BodyAssociatedApplication.Extension.ToLower());
    }
    
    #endregion
    
    #region Работа с сертификатами
    
    /// <summary>
    /// Получить информацию о владельце сертификата.
    /// </summary>
    /// <param name="subject">Структура с информацией о подписи.</param>
    /// <returns>Ф.И.О владельца сертификата.</returns>
    public static string GetCertificateOwnerShortName(ICertificateSubject subject)
    {
      var surname = subject.Surname;
      var givenName = subject.GivenName;
      
      // Получить сокращенное имя из атрибута Subject.
      if (!string.IsNullOrWhiteSpace(surname) && !string.IsNullOrWhiteSpace(givenName))
      {
        var splitedGivenName = givenName.Split(new char[] { ' ' });
        
        if (splitedGivenName.Count() == 1)
          return Sungero.Parties.PublicFunctions.Person.GetSurnameAndInitialsInTenantCulture(givenName, string.Empty, surname);
        
        if (splitedGivenName.Count() > 1)
          return Sungero.Parties.PublicFunctions.Person.GetSurnameAndInitialsInTenantCulture(splitedGivenName[0], splitedGivenName[1], surname);
      }
      
      // Dmitriev_IA: Для сертификатов с незаполненными surname или givenName возвращать counterpartyName.
      //              У реальных КА СО такого быть не должно. Введено для разработки.
      return subject.CounterpartyName;
    }
    
    /// <summary>
    /// Получить объект издателя сертификата.
    /// </summary>
    /// <param name="issuer">Строка с информацией об издателе из сертификата.</param>
    /// <returns>Структура с данными об издателе сертификата.</returns>
    private static IssuerSubject ParseIssuerObject(string issuer)
    {
      var parsedSubject = IssuerSubject.Create();
      var subjectItems = issuer.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
      
      foreach (var item in subjectItems)
      {
        var itemElements = item.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
        if (itemElements.Count() < 2)
          continue;
        
        var itemKey = itemElements[0].Trim();
        var itemValue = itemElements[1].Trim();
        
        // Оттримить только одни крайние кавычки.
        if (itemValue.StartsWith("\""))
          itemValue = itemValue.Substring(1);
        if (itemValue.EndsWith("\""))
          itemValue = itemValue.Substring(0, itemValue.Length - 1);

        itemValue = itemValue.Replace("\"\"", "\"");
        
        switch (itemKey)
        {
          case "CN":
            parsedSubject.Name = itemValue;
            break;
          default:
            break;
        }
      }
      return parsedSubject;
    }
    
    #endregion
    
    #region Работа с задачами контроля возврата
    
    /// <summary>
    /// Выдать полные права на сущность в рамках сессии.
    /// </summary>
    /// <param name="session">Сессия.</param>
    /// <param name="entity">Сущность, на которую будут выданы полные права.</param>
    public static void AddFullRightsInSession(Sungero.Domain.Session session, IEntity entity)
    {
      if (session == null || entity == null)
        return;
      
      var submitAuthorizationManager = session.GetType()
        .GetField("submitAuthorizationManager", BindingFlags.NonPublic | BindingFlags.Instance)
        .GetValue(session);
      var authManagerType = submitAuthorizationManager.GetType();
      var authCache = (Dictionary<IEntity, int>)authManagerType
        .GetField("authorizedOperationsCache", BindingFlags.NonPublic | BindingFlags.Instance)
        .GetValue(submitAuthorizationManager);
      if (!authCache.ContainsKey(entity))
        authCache.Add(entity, -1);
      else
        authCache[entity] = -1;
    }
    
    /// <summary>
    /// Получить задачу с полными правами в рамках сессии.
    /// </summary>
    /// <param name="session">Сессия.</param>
    /// <param name="taskId">Id задачи.</param>
    /// <returns>Задача.</returns>
    public static ITask GetCheckReturnTaskWithRights(Sungero.Domain.Session session, int taskId)
    {
      var innerSession = (Sungero.Domain.ISession)session.GetType()
        .GetField("InnerSession", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(session);
      var task = Sungero.Workflow.Tasks.As((IEntity)innerSession.Get(typeof(Sungero.Workflow.ITask), taskId));
      AddFullRightsInSession(session, task);
      return task;
    }
    
    /// <summary>
    /// Получить задание с полными правами в рамках сессии.
    /// </summary>
    /// <param name="session">Сессия.</param>
    /// <param name="taskId">Id задачи.</param>
    /// <returns>Задание контроля возврата (ApprovalCheckReturnAssignments, CheckReturns, ReturnDocuments).</returns>
    public static IAssignment GetCheckReturnAssignmentWithRights(Sungero.Domain.Session session, int taskId)
    {
      var innerSession = (Sungero.Domain.ISession)session.GetType()
        .GetField("InnerSession", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(session);
      
      var assignment = innerSession.GetAll<IAssignment>()
        .FirstOrDefault(a => a.Task != null && a.Task.Id == taskId && a.Status == Sungero.Workflow.AssignmentBase.Status.InProcess);
      if (assignment != null && !ApprovalCheckReturnAssignments.Is(assignment) && !CheckReturnCheckAssignments.Is(assignment) && !CheckReturnAssignments.Is(assignment))
        assignment = null;
      AddFullRightsInSession(session, assignment);
      return assignment;
    }
    
    /// <summary>
    /// Проверить доступность задачи и задания по контролю возврата.
    /// </summary>
    /// <param name="taskId">Id задачи.</param>
    /// <param name="checkTaskLock">Нужно ли проверять заблокированность задачи.</param>
    /// <returns>Возвращает текст ошибки, либо string.Empty.</returns>
    /// HACK: Сессии, для получения задачи, на которую у пользователя нет прав.
    public static string ValidateCheckReturnTaskLocking(int taskId, bool checkTaskLock)
    {
      using (var session = new Sungero.Domain.Session())
      {
        // Проверить существование задачи и активного задания.
        var task = GetCheckReturnTaskWithRights(session, taskId);
        if (task == null)
          return Docflow.Resources.TaskNotFound;
        
        var assignment = GetCheckReturnAssignmentWithRights(session, taskId);
        
        // Проверить заблокированность задания.
        if (assignment != null)
        {
          var assignmentLockInfo = Locks.GetLockInfo(assignment);
          if (assignmentLockInfo.IsLockedByOther)
            return Docflow.Resources.AssignmentLockedByUserFormat(assignmentLockInfo.OwnerName);
        }
        
        if (checkTaskLock)
        {
          var taskLockInfo = Locks.GetLockInfo(task);
          if (taskLockInfo.IsLockedByOther)
            return Docflow.Resources.TaskLockedByUserFormat(taskLockInfo.OwnerName);
        }
      }
      return string.Empty;
    }
    
    /// <summary>
    /// Выполнить задание на контроль возврата в рамках самостоятельной задачи или в рамках согласования.
    /// </summary>
    /// <param name="taskId">Id задачи.</param>
    /// <param name="operation">Операция, которую нужно совершить. Доступные варианты - Shared.Constants.ReturnControl.</param>
    /// <returns>Текст ошибки, пустую строку, если ошибок нет.</returns>
    /// <remarks>Для контроля возврата результаты - выполнен \ прекращение задачи.
    /// Для согласования результаты - подписан \ не подписан.</remarks>
    public static string CompleteCheckReturnTask(int taskId, int operation)
    {
      return CompleteCheckReturnTask(taskId, operation, null, null);
    }
    
    /// <summary>
    /// Выполнить задание на контроль возврата в рамках самостоятельной задачи или в рамках согласования.
    /// </summary>
    /// <param name="taskId">Id задачи.</param>
    /// <param name="operation">Операция, которую нужно совершить. Доступные варианты - Shared.Constants.ReturnControl.</param>
    /// <param name="text">Комментарий, который будет записан в задание или задачу.</param>
    /// <returns>Текст ошибки, пустую строку, если ошибок нет.</returns>
    /// <remarks>Для контроля возврата результаты - выполнен \ прекращение задачи.
    /// Для согласования результаты - подписан \ не подписан.</remarks>
    public static string CompleteCheckReturnTask(int taskId, int operation, string text)
    {
      return CompleteCheckReturnTask(taskId, operation, text, null);
    }
    
    /// <summary>
    /// Выполнить задание на контроль возврата в рамках самостоятельной задачи или в рамках согласования.
    /// </summary>
    /// <param name="taskId">Id задачи.</param>
    /// <param name="operation">Операция, которую нужно совершить. Доступные варианты - Shared.Constants.ReturnControl.</param>
    /// <param name="text">Комментарий, который будет записан в задание или задачу.</param>
    /// <param name="deadline">Новый срок, при изменении срока возврата.</param>
    /// <returns>Текст ошибки, пустую строку, если ошибок нет.</returns>
    /// <remarks>Для контроля возврата результаты - выполнен \ прекращение задачи.
    /// Для согласования результаты - подписан \ не подписан.</remarks>
    /// HACK: Сессии, для получения задачи, на которую у пользователя нет прав.
    public static string CompleteCheckReturnTask(int taskId, int operation, string text, DateTime? deadline)
    {
      var needCheckLock = operation == Constants.Module.ReturnControl.AbortTask || operation == Constants.Module.ReturnControl.DeadlineChange;
      var validate = ValidateCheckReturnTaskLocking(taskId, needCheckLock);
      if (!string.IsNullOrWhiteSpace(validate))
        return validate;
      
      using (var session = new Sungero.Domain.Session())
      {
        // Получить задачу и задание на контроль возврата.
        var task = GetCheckReturnTaskWithRights(session, taskId);
        var assignment = GetCheckReturnAssignmentWithRights(session, taskId);
        
        // Операция - прекратить задачу.
        if (operation == Constants.Module.ReturnControl.AbortTask)
        {
          if (text != null)
          {
            task.ActiveText = text;
            task.Texts.Last().IsAutoGenerated = true;
          }
          task.Abort();
        }
        
        // Операция - выполнить задание.
        if (operation == Constants.Module.ReturnControl.CompleteAssignment ||
            operation == Constants.Module.ReturnControl.SignAssignment || operation == Constants.Module.ReturnControl.NotSignAssignment)
        {
          if (assignment == null)
            return string.Empty;
          
          if (text != null)
            assignment.ActiveText = text;
          try
          {
            if (assignment.Info.Name == CheckReturnAssignments.Info.Name)
              assignment.Complete(Docflow.CheckReturnAssignment.Result.Complete);
            else if (assignment.Info.Name == CheckReturnCheckAssignments.Info.Name)
              assignment.Complete(Docflow.CheckReturnCheckAssignment.Result.Returned);
            else if (assignment.Info.Name == ApprovalCheckReturnAssignments.Info.Name)
              assignment.Complete(operation == Constants.Module.ReturnControl.SignAssignment ?
                                  Docflow.ApprovalCheckReturnAssignment.Result.Signed :
                                  Docflow.ApprovalCheckReturnAssignment.Result.NotSigned);
          }
          catch (InvalidOperationException)
          {

          }
        }
        
        // Операция - смена срока возврата.
        if (operation == Constants.Module.ReturnControl.DeadlineChange)
        {
          Logger.DebugFormat("CompleteCheckReturnTask: Task {0}, new deadline {1}, assignment created = {2}.",
                             taskId, deadline.Value.ToString(), (assignment != null).ToString());
          CheckReturnTasks.As(task).MaxDeadline = deadline;

          if (assignment == null)
            return string.Empty;
          
          assignment.Deadline = deadline;
        }
      }
      return string.Empty;
    }
    
    #endregion
    
    #region Замещение
    
    /// <summary>
    /// Имеет ли доступ по замещению.
    /// </summary>
    /// <param name="registrationGroup">Группа регистрации.</param>
    /// <param name="documentRegister">Журнал регистрации.</param>
    /// <returns>Информация о доступе, в строковом виде.</returns>
    [Remote(IsPure = true)]
    public static string CalculateParams(IRegistrationGroup registrationGroup, IDocumentRegister documentRegister)
    {
      var isSubstituteParamName = Constants.Module.IsSubstituteResponsibleEmployeeParamName;
      var isAdministratorParamName = Constants.Module.IsAdministratorParamName;
      var isUsedParamName = Constants.Module.IsUsedParamName;
      var hasDocumentsParamName = Constants.Module.HasRegisteredDocumentsParamName;
      
      var isSubstitute = true;
      var isAdministrator = true;
      var hasDocuments = documentRegister != null && Functions.DocumentRegister.HasRegisteredDocuments(documentRegister);
      var isUsed = documentRegister != null && (Functions.RegistrationSetting.GetByDocumentRegister(documentRegister).Any() || hasDocuments);
      if (registrationGroup != null && registrationGroup.ResponsibleEmployee != null)
      {
        isSubstitute = Recipients.AllRecipientIds.Contains(registrationGroup.ResponsibleEmployee.Id);
        isAdministrator = Recipients.OwnRecipientIds.Contains(Roles.Administrators.Id);
      }
      
      var result = new Dictionary<string, bool>();
      result.Add(isSubstituteParamName, isSubstitute);
      result.Add(isAdministratorParamName, isAdministrator);
      result.Add(isUsedParamName, isUsed);
      result.Add(hasDocumentsParamName, hasDocuments);
      
      return Functions.Module.BoxToString(result);
    }
    
    #endregion
    
    #region Предметное отображение
    
    /// <summary>
    /// Получить даты итераций задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Список дат в формате: "Дата", "Это доработка", "Это рестарт".</returns>
    public static List<TaskIterations> GetIterationDates(ITask task)
    {
      // Дата создания.
      var iterations = new List<TaskIterations>() { TaskIterations.Create(task.Created.Value, false, false) };
      
      // Даты рестартов.
      var restartDates = WorkflowHistories.GetAll(h => h.EntityId == task.Id &&
                                                  h.Operation == Sungero.Workflow.WorkflowHistory.Operation.Restart)
        .Select(h => h.HistoryDate.Value)
        .ToList();
      foreach (var restartDate in restartDates)
        iterations.Add(TaskIterations.Create(restartDate, false, true));
      
      // Доработки в согласовании официальных документов.
      var reworkDates = ApprovalReworkAssignments.GetAll()
        .Where(a => Equals(a.Task, task) && a.Result == Docflow.ApprovalReworkAssignment.Result.ForReapproving)
        .Select(a => a.Created.Value).ToList();
      foreach (var reworkDate in reworkDates)
        iterations.Add(TaskIterations.Create(reworkDate, true, false));
      
      // Доработки в свободном согласовании.
      var freeReworkDates = FreeApprovalReworkAssignments.GetAll()
        .Where(a => Equals(a.Task, task) && a.Result == Docflow.FreeApprovalReworkAssignment.Result.Reworked)
        .Select(a => a.Created.Value).ToList();
      foreach (var reworkDate in freeReworkDates)
        iterations.Add(TaskIterations.Create(reworkDate, true, false));
      
      // Доработки в простых задачах.
      var reviewDates = ReviewAssignments.GetAll()
        .Where(a => Equals(a.Task, task) && a.Result == Sungero.Workflow.ReviewAssignment.Result.ForRework)
        .Select(a => a.Created.Value).ToList();
      foreach (var reviewDate in reviewDates)
        iterations.Add(TaskIterations.Create(reviewDate, true, false));
      
      return iterations.OrderBy(d => d.Date).ToList();
    }
    
    /// <summary>
    /// Добавить информацию в правый столбец блока.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="info">Добавляемая информация.</param>
    [Public]
    public static void AddInfoToRightContent(Sungero.Core.StateBlock block, string info)
    {
      AddInfoToRightContent(block, info, null);
    }
    
    /// <summary>
    /// Добавить информацию в правый столбец блока.
    /// </summary>
    /// <param name="block">Блок.</param>
    /// <param name="info">Добавляемая информация.</param>
    /// <param name="style">Стиль.</param>
    [Public]
    public static void AddInfoToRightContent(Sungero.Core.StateBlock block, string info, Sungero.Core.StateBlockLabelStyle style)
    {
      // Добавить колонку справа, если всего одна колонка (main).
      var rightContent = block.Contents.LastOrDefault();
      if (block.Contents.Count() <= 1)
        rightContent = block.AddContent();
      else
        rightContent.AddLineBreak();
      
      if (style != null)
        rightContent.AddLabel(info, style);
      else
        rightContent.AddLabel(info);
    }
    
    /// <summary>
    /// Получить пользовательский текст из задания.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Комментарий пользователя.</returns>
    [Public]
    public static string GetAssignmentUserComment(IAssignment assignment)
    {
      var textComment = assignment.Texts.Last();
      if (textComment.IsAutoGenerated == true)
        return string.Empty;
      
      return GetFormatedUserText(textComment.Body);
    }
    
    /// <summary>
    /// Получить пользовательский текст из задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="autoGeneratedText">Автогенерируемый текст.</param>
    /// <returns>Комментарий пользователя.</returns>
    [Public]
    public static string GetTaskUserComment(ITask task, string autoGeneratedText)
    {
      var textComment = task.Texts.FirstOrDefault();
      if (textComment == null || textComment.Body == autoGeneratedText)
        return string.Empty;
      
      return GetFormatedUserText(textComment.Body);
    }
    
    /// <summary>
    /// Получить пользовательский текст из задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="date">Дата, на которую необходимо получить текст.</param>
    /// <param name="autoGeneratedText">Автогенерируемый текст.</param>
    /// <returns>Комментарий пользователя.</returns>
    [Public]
    public static string GetTaskUserComment(ITask task, DateTime date, string autoGeneratedText)
    {
      var textComment = task.Texts.Where(t => t.Created < date).Last();
      if (textComment.Body == autoGeneratedText)
        return string.Empty;
      
      return GetFormatedUserText(textComment.Body);
    }
    
    /// <summary>
    /// Получить отформатированный пользовательский текст.
    /// </summary>
    /// <param name="userText">Исходный текст.</param>
    /// <returns>Отформатированный текст.</returns>
    [Public]
    public static string GetFormatedUserText(string userText)
    {
      if (string.IsNullOrWhiteSpace(userText))
        return string.Empty;
      
      var regexstring = System.Text.RegularExpressions.Regex.Replace(userText, @"(\r?\n\s*){2,}", Environment.NewLine + Environment.NewLine);
      
      return regexstring.Trim();
    }
    
    /// <summary>
    /// Получить серый цвет.
    /// </summary>
    /// <returns>Цвет.</returns>
    [Public]
    public static Sungero.Core.Color GetGrayColor()
    {
      return Sungero.Core.Colors.Common.Gray;
    }
    
    /// <summary>
    /// Выделить текущий блок.
    /// </summary>
    /// <param name="block">Блок.</param>
    [Public]
    public static void MarkBlock(Sungero.Core.StateBlock block)
    {
      block.Background = Sungero.Core.Colors.Common.LightYellow;
      block.BorderColor = Sungero.Core.Colors.Common.LightYellow;
    }
    
    /// <summary>
    /// Создать стиль.
    /// </summary>
    /// <param name="fontSize">Размер шрифта.</param>
    /// <returns>Полученный стиль.</returns>
    [Public]
    public static Sungero.Core.StateBlockLabelStyle CreateStyle(int? fontSize)
    {
      return CreateStyle(fontSize, false, false, false, Sungero.Core.Colors.Empty);
    }
    
    /// <summary>
    /// Создать стиль.
    /// </summary>
    /// <param name="bolded">Жирный.</param>
    /// <param name="grayed">Серый.</param>
    /// <returns>Полученный стиль.</returns>
    [Public]
    public static Sungero.Core.StateBlockLabelStyle CreateStyle(bool bolded, bool grayed)
    {
      return CreateStyle(null, bolded, false, grayed, Sungero.Core.Colors.Empty);
    }
    
    /// <summary>
    /// Создать стиль.
    /// </summary>
    /// <param name="bolded">Жирный.</param>
    /// <param name="italic">Курсив.</param>
    /// <param name="grayed">Серый.</param>
    /// <returns>Полученный стиль.</returns>
    [Public]
    public static Sungero.Core.StateBlockLabelStyle CreateStyle(bool bolded, bool italic, bool grayed)
    {
      return CreateStyle(null, bolded, italic, grayed, Sungero.Core.Colors.Empty);
    }
    
    /// <summary>
    /// Создать стиль.
    /// </summary>
    /// <param name="fontSize">Размер шрифта.</param>
    /// <param name="bolded">Жирный.</param>
    /// <param name="grayed">Серый.</param>
    /// <returns>Полученный стиль.</returns>
    [Public]
    public static Sungero.Core.StateBlockLabelStyle CreateStyle(int? fontSize, bool bolded, bool grayed)
    {
      return CreateStyle(fontSize, bolded, false, grayed, Sungero.Core.Colors.Empty);
    }
    
    /// <summary>
    /// Создать стиль.
    /// </summary>
    /// <param name="color">Цвет.</param>
    /// <returns>Полученный стиль.</returns>
    [Public]
    public static Sungero.Core.StateBlockLabelStyle CreateStyle(Sungero.Core.Color color)
    {
      return CreateStyle(null, false, false, false, color);
    }
    
    /// <summary>
    /// Создать стиль.
    /// </summary>
    /// <param name="fontSize">Размер шрифта.</param>
    /// <param name="bolded">Жирный.</param>
    /// <param name="italic">Курсив.</param>
    /// <param name="grayed">Серый.</param>
    /// <param name="color">Цвет. Игнорирует признак grayed.</param>
    /// <returns>Полученный стиль.</returns>
    [Public]
    public static Sungero.Core.StateBlockLabelStyle CreateStyle(int? fontSize, bool bolded, bool italic, bool grayed, Sungero.Core.Color color)
    {
      var style = StateBlockLabelStyle.Create();
      
      // Кегль.
      if (fontSize.HasValue)
        style.FontSize = fontSize.Value;
      
      // Серый цвет.
      if (grayed)
        style.Color = GetGrayColor();
      
      // Произвольный цвет.
      if (color != Sungero.Core.Colors.Empty)
        style.Color = color;
      
      // Полужирность.
      if (bolded)
        style.FontWeight = Sungero.Core.FontWeight.SemiBold;
      
      // Курсив.
      if (italic)
        style.Italic = italic;
      
      return style;
    }
    
    /// <summary>
    /// Создать стиль заголовка.
    /// </summary>
    /// <param name="italic">Курсив.</param>
    /// <returns>Полученный стиль.</returns>
    [Public]
    public static Sungero.Core.StateBlockLabelStyle CreateHeaderStyle(bool italic)
    {
      var style = CreateStyle(true, false);
      style.Italic = italic;
      return style;
    }
    
    /// <summary>
    /// Создать стиль заголовка.
    /// </summary>
    /// <returns>Полученный стиль.</returns>
    [Public]
    public static Sungero.Core.StateBlockLabelStyle CreateHeaderStyle()
    {
      return CreateStyle(true, false);
    }
    
    /// <summary>
    /// Создать стиль для исполнителя.
    /// </summary>
    /// <param name="italic">Курсив.</param>
    /// <returns>Полученный стиль.</returns>
    [Public]
    public static Sungero.Core.StateBlockLabelStyle CreatePerformerDeadlineStyle(bool italic)
    {
      return CreateStyle(true, italic, true);
    }
    
    /// <summary>
    /// Создать стиль для исполнителя.
    /// </summary>
    /// <returns>Полученный стиль.</returns>
    [Public]
    public static Sungero.Core.StateBlockLabelStyle CreatePerformerDeadlineStyle()
    {
      return CreateStyle(true, true);
    }
    
    /// <summary>
    /// Создать стиль для примечания.
    /// </summary>
    /// <param name="italic">Курсив.</param>
    /// <returns>Полученный стиль.</returns>
    [Public]
    public static Sungero.Core.StateBlockLabelStyle CreateNoteStyle(bool italic)
    {
      return CreateStyle(false, italic, true);
    }
    
    /// <summary>
    /// Создать стиль для примечания.
    /// </summary>
    /// <returns>Полученный стиль.</returns>
    [Public]
    public static Sungero.Core.StateBlockLabelStyle CreateNoteStyle()
    {
      return CreateStyle(false, true);
    }
    
    /// <summary>
    /// Создать стиль для задержки.
    /// </summary>
    /// <returns>Полученный стиль.</returns>
    [Public]
    public static Sungero.Core.StateBlockLabelStyle CreateDelayStyle()
    {
      return Functions.Module.CreateStyle(Sungero.Core.Colors.Common.Red);
    }
    
    /// <summary>
    /// Создать стиль для разделительной линии.
    /// </summary>
    /// <returns>Полученный стиль.</returns>
    [Public]
    public static Sungero.Core.StateBlockLabelStyle CreateSeparatorStyle()
    {
      var color = Colors.Parse("#ABABAB");
      return Docflow.PublicFunctions.Module.CreateStyle(6, false, false, true, Sungero.Core.Colors.Empty);
    }
    
    /// <summary>
    /// Получить текст разделительной линии.
    /// </summary>
    /// <returns>Текст разделительной линии.</returns>
    [Public]
    public static string GetSeparatorText()
    {
      return Constants.Module.SeparatorText;
    }
    
    /// <summary>
    /// Получить размер отступа заголовка и основного текста в блоке.
    /// </summary>
    /// <returns>Размер отступа заголовка и основного текста в блоке.</returns>
    [Public]
    public static int GetEmptyLineMargin()
    {
      return Constants.Module.EmptyLineMargin;
    }
    
    /// <summary>
    /// Получить сводку по документу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Сводка.</returns>
    [Public]
    public StateView GetDocumentSummary(IOfficialDocument document)
    {
      if (document != null)
        return Functions.OfficialDocument.GetDocumentSummary(document);
      
      var documentStateView = StateView.Create();
      return documentStateView;
    }
    
    #endregion
    
    #region Работа с сотрудником
    
    /// <summary>
    /// Получить подразделение сотрудника.
    /// </summary>
    /// <param name="user">Пользователь.</param>
    /// <returns>Подразделение сотрудника.</returns>
    [Remote(IsPure = true)]
    public static IDepartment GetDepartment(IUser user)
    {
      var employee = Employees.GetAll().FirstOrDefault(u => Equals(u, user));
      
      if (employee == null)
        return null;

      return employee.Department;
    }
    
    /// <summary>
    /// Определить руководителя сотрудника.
    /// </summary>
    /// <param name="user">Пользователь.</param>
    /// <returns>Руководитель.</returns>
    [Remote(IsPure = true), Public]
    public static IEmployee GetManager(IUser user)
    {
      var department = GetDepartment(user);
      var manager = (department != null) ? department.Manager : null;
      
      // Если сотрудник является руководителем своего же подразделения,
      // тогда его непосредственным руководителем является руководитель головного подразделения.
      if (manager != null && Equals(user, Users.As(manager)))
      {
        var headDepartment = department.HeadOffice;
        if (headDepartment != null && headDepartment.Manager != null)
          manager = headDepartment.Manager;
      }
      return manager;
    }
    
    /// <summary>
    /// Возвращает группу регистрации, обслуживающую НОР и подразделение, указанные в документе.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="documentFlow">Документопоток, на случай если надо явно задать поток группы регистрации.</param>
    /// <returns>Подходящая группа регистрации.</returns>
    public static IRegistrationGroup GetRegistrationGroupByDocument(IOfficialDocument document, Enumeration? documentFlow)
    {
      if (documentFlow == null)
        documentFlow = document.DocumentKind.DocumentFlow;
      
      // Если есть настройка регистрации, учесть это при фильтрации.
      var settingGroup = RegistrationSettings.GetAll()
        .Where(s => s.SettingType == Docflow.RegistrationSetting.SettingType.Registration &&
               s.Status == CoreEntities.DatabookEntry.Status.Active && Equals(s.DocumentFlow, documentFlow) &&
               (!s.DocumentKinds.Any() || s.DocumentKinds.Any(k => Equals(k.DocumentKind, document.DocumentKind))) &&
               (!s.BusinessUnits.Any() || s.BusinessUnits.Any(u => Equals(u.BusinessUnit, document.BusinessUnit))) &&
               (!s.Departments.Any() || s.Departments.Any(d => Equals(d.Department, document.Department))))
        .Select(s => s.DocumentRegister.RegistrationGroup)
        .Where(s => !s.Departments.Any() || s.Departments.Any(d => Equals(d.Department, document.Department)))
        .FirstOrDefault();
      if (settingGroup != null)
        return settingGroup;

      var regGroups = RegistrationGroups.GetAll(g => g.Status == CoreEntities.DatabookEntry.Status.Active);

      if (documentFlow == Docflow.DocumentKind.DocumentFlow.Contracts)
        regGroups = regGroups.Where(g => g.CanRegisterContractual == true);
      else if (documentFlow == Docflow.DocumentKind.DocumentFlow.Incoming)
        regGroups = regGroups.Where(g => g.CanRegisterIncoming == true);
      else if (documentFlow == Docflow.DocumentKind.DocumentFlow.Inner)
        regGroups = regGroups.Where(g => g.CanRegisterInternal == true);
      else if (documentFlow == Docflow.DocumentKind.DocumentFlow.Outgoing)
        regGroups = regGroups.Where(g => g.CanRegisterOutgoing == true);
      
      // Получить группы регистрации, обслуживающие это подразделение.
      var departmentRegGroup = regGroups.Where(g => g.Departments.Any(d => Equals(d.Department, document.Department))).FirstOrDefault();
      if (departmentRegGroup != null)
        return departmentRegGroup;
      
      // Если таких групп регистрации нет, то выбрать те, которые обслуживают все подразделения.
      return regGroups.FirstOrDefault(g => !g.Departments.Any());
    }
    
    /// <summary>
    /// Определить регистратора.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Регистратор.</returns>
    [Remote(IsPure = true), Public]
    public static IEmployee GetClerk(IOfficialDocument document)
    {
      return GetClerk(document, null);
    }
    
    /// <summary>
    /// Определить регистратора.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="documentFlow">Документопоток.</param>
    /// <returns>Регистратор.</returns>
    [Remote(IsPure = true)]
    public static IEmployee GetClerk(IOfficialDocument document, Enumeration? documentFlow)
    {
      if (document == null)
        return null;
      
      // Если у документа указан журнал с группой регистрации и это не поиск исходящего регистратора - возвращаем ответственного.
      if (documentFlow == null && document.DocumentRegister != null && document.DocumentRegister.RegistrationGroup != null)
        return document.DocumentRegister.RegistrationGroup.ResponsibleEmployee;
      
      var department = document.Department;
      if (department == null)
        return null;
      
      var userRegistrationGroup = GetRegistrationGroupByDocument(document, documentFlow);
      
      if (userRegistrationGroup == null)
        return null;
      
      // Ответственный за группу регистрации.
      return userRegistrationGroup.ResponsibleEmployee;
    }
    
    #endregion
    
    #region Работа с SQL

    /// <summary>
    /// Создать индекс в таблице.
    /// </summary>
    /// <param name="tableName">Имя таблицы.</param>
    /// <param name="indexName">Имя индекса.</param>
    /// <param name="indexQuery">SQL-запрос создания индекса.</param>
    [Public]
    public static void CreateIndexOnTable(string tableName, string indexName, string indexQuery)
    {
      Logger.DebugFormat("Create index {0} on {1} table", indexName, tableName);
      var command = string.Format(Queries.Module.CreateIndexOnTable, tableName, indexName, indexQuery);
      ExecuteSQLCommand(command);
    }
    
    /// <summary>
    /// Добавить строковый параметр к запросу.
    /// </summary>
    /// <param name="command">Команда.</param>
    /// <param name="parameterName">Параметр.</param>
    /// <param name="parameterValue">Значение.</param>
    [Public]
    public static void AddStringParameterToCommand(System.Data.IDbCommand command, string parameterName, string parameterValue)
    {
      var parameter = command.CreateParameter();
      parameter.ParameterName = parameterName;
      parameter.Direction = System.Data.ParameterDirection.Input;
      parameter.DbType = System.Data.DbType.String;
      parameter.Value = parameterValue;
      command.Parameters.Add(parameter);
    }

    /// <summary>
    /// Добавить числовой параметр к запросу.
    /// </summary>
    /// <param name="command">Команда.</param>
    /// <param name="parameterName">Параметр.</param>
    /// <param name="parameterValue">Значение.</param>
    [Public]
    public static void AddIntegerParameterToCommand(System.Data.IDbCommand command, string parameterName, int parameterValue)
    {
      var parameter = command.CreateParameter();
      parameter.ParameterName = parameterName;
      parameter.Direction = System.Data.ParameterDirection.Input;
      parameter.DbType = System.Data.DbType.Int32;
      parameter.Value = parameterValue;
      command.Parameters.Add(parameter);
    }
    
    /// <summary>
    /// Добавить параметр с типом DateTime к запросу.
    /// </summary>
    /// <param name="command">Команда.</param>
    /// <param name="parameterName">Параметр.</param>
    /// <param name="parameterValue">Значение параметра.</param>
    [Public]
    public static void AddDateTimeParameterToCommand(System.Data.IDbCommand command, string parameterName, DateTime parameterValue)
    {
      var parameter = command.CreateParameter();
      parameter.ParameterName = parameterName;
      parameter.Direction = System.Data.ParameterDirection.Input;
      parameter.DbType = System.Data.DbType.DateTime;
      parameter.Value = parameterValue;
      command.Parameters.Add(parameter);
    }
    
    /// <summary>
    /// Добавить возвращаемый числовой параметр к запросу.
    /// </summary>
    /// <param name="command">Команда.</param>
    /// <param name="parameterName">Параметр.</param>
    /// <returns>Числовой параметр.</returns>
    [Public]
    public static System.Data.IDbDataParameter AddIntegerOutputParameterToCommand(System.Data.IDbCommand command, string parameterName)
    {
      var parameter = command.CreateParameter();
      parameter.ParameterName = parameterName;
      parameter.Direction = System.Data.ParameterDirection.Output;
      parameter.DbType = System.Data.DbType.Int32;
      command.Parameters.Add(parameter);
      return parameter;
    }

    /// <summary>
    /// Выполнить SQL-запрос.
    /// </summary>
    /// <param name="format">Форматируемая строка запроса.</param>
    /// <param name="args">Аргументы строки запроса.</param>
    [Public]
    public static void ExecuteSQLCommandFormat(string format, object[] args)
    {
      var command = string.Format(format, args);
      ExecuteSQLCommand(command);
    }
    
    /// <summary>
    /// Выполнить SQL-запрос.
    /// </summary>
    /// <param name="commandText">Форматируемая строка запроса.</param>
    [Public]
    public static void ExecuteSQLCommand(string commandText)
    {
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = commandText;
        command.ExecuteNonQuery();
      }
    }
    
    /// <summary>
    /// Выполнить SQL-запрос c возвращаемым значением.
    /// </summary>
    /// <param name="commandText">Форматируемая строка запроса.</param>
    /// <returns>Содержимое первого столбца первой строки результата запроса.</returns>
    [Public]
    public static object ExecuteScalarSQLCommand(string commandText)
    {
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = commandText;
        return command.ExecuteScalar();
      }
    }
    
    #endregion
    
    #region Отчеты
    
    /// <summary>
    /// Удалить временную таблицу отчета.
    /// </summary>
    /// <param name="tableName">Название таблицы.</param>
    /// <remarks>Для выполнения создает свой коннект к БД.</remarks>
    [Public]
    public static void DropReportTempTable(string tableName)
    {
      if (string.IsNullOrEmpty(tableName))
        return;
      
      var queryText = string.Format(Queries.Module.DropTable, tableName);
      ExecuteSQLCommand(queryText);
    }
    
    /// <summary>
    /// Удалить временные таблицы отчета.
    /// </summary>
    /// <param name="tablesNames">Названия таблиц.</param>
    /// <remarks>Для выполнения создает свой коннект к БД.</remarks>
    [Public]
    public static void DropReportTempTables(string[] tablesNames)
    {
      foreach (var tableName in tablesNames)
        DropReportTempTable(tableName);
    }

    /// <summary>
    /// Удалить данные из таблицы отчета.
    /// </summary>
    /// <param name="tableName">Название таблицы.</param>
    /// <param name="reportSessionId">Код отчета.</param>
    /// <remarks>Для выполнения создает свой коннект к БД.</remarks>
    [Public]
    public static void DeleteReportData(string tableName, string reportSessionId)
    {
      if (string.IsNullOrEmpty(tableName))
        return;
      
      var queryText = string.Format(Sungero.Docflow.Queries.Module.DeleteReportData, tableName, reportSessionId);
      ExecuteSQLCommand(queryText);
    }

    /// <summary>
    /// Создать временную таблицу для прав доступа.
    /// </summary>
    /// <param name="tableName">Имя таблицы.</param>
    /// <param name="availableIds">Список доступных ИД для данного пользователя.</param>
    [Public]
    public static void CreateTempTableForRights(string tableName, IQueryable<int> availableIds)
    {
      // Удалить таблицу.
      DropReportTempTable(tableName);
      
      Functions.Module.ExecuteSQLCommandFormat(Queries.Module.CreateTempTableForRights, new[] { tableName });
      
      // Добавить доступные id документов в таблицу.
      if (availableIds.Any())
      {
        // Количество добавляемых значений за раз.
        var insertCount = 500;
        var idsList = availableIds.Select(a => a.ToString()).ToArray();
        
        // Формирование запроса со строкой доступных id.
        var idsListCount = idsList.Count();
        for (int i = 0; i < idsListCount; i += insertCount)
        {
          var splitCount = idsListCount - i < insertCount ? idsListCount - i : insertCount;
          var insertingValues = string.Join("),(", idsList, i, splitCount);
          Functions.Module.ExecuteSQLCommandFormat(Queries.Module.InsertIntoTempTableForRights, new[] { tableName, insertingValues });
        }
      }
    }
    
    /// <summary>
    /// Пакетная запись структур в таблицу.
    /// </summary>
    /// <param name="table">Название таблицы.</param>
    /// <param name="structures">Структуры. Только простые структуры без сущностей.</param>
    [Public]
    public static void WriteStructuresToTable(string table, System.Collections.Generic.IEnumerable<Domain.Shared.ISimpleAppliedStructure> structures)
    {
      var list = structures.ToList();
      if (!list.Any())
        return;
      
      SQL.CreateBulkCopy().Write(table, list);
    }
    
    #endregion
    
    #region Выдача прав на вложения
    
    /// <summary>
    /// Получить всех исполнителей заданий по всем связанным задачам.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Исполнители.</returns>
    [Remote(IsPure = true, PackResultEntityEagerly = true), Public]
    public static List<IRecipient> GetTaskAssignees(ITask task)
    {
      var tasks = Tasks.GetAll().Where(t => Equals(t.MainTask, task.MainTask)).ToList();
      tasks.RemoveAll(t => Equals(t.Id, task.Id));
      tasks.Add(task);

      var tasksAuthors = new List<IRecipient>();
      tasksAuthors.AddRange(tasks.Where(t => t.Author != null).Select(t => t.Author));
      tasksAuthors.AddRange(tasks.Where(t => t.StartedBy != null).Select(t => t.StartedBy));
      tasksAuthors.AddRange(AssignmentBases.GetAll()
                            .Where(a => Equals(a.Task.MainTask, task.MainTask))
                            .Select(a => a.Performer));

      foreach (var subTask in tasks)
      {
        // Добавить плановых исполнителей для прикладных задач.
        var assignees = GetServerEntityFunctionResult(subTask, "GetTaskAdditionalAssignees", null);
        if (assignees != null)
          tasksAuthors.AddRange((List<IRecipient>)assignees);
      }
      
      return tasksAuthors.Distinct().ToList();
    }
    
    /// <summary>
    /// Получить вложения, на которые нет никаких прав.
    /// </summary>
    /// <param name="performers">Исполнители заданий.</param>
    /// <param name="attachments">Вложения.</param>
    /// <returns>Вложения, на которые хоть у кого-то нет прав.</returns>
    [Remote(IsPure = true, PackResultEntityEagerly = true), Public]
    public static List<IEntity> GetAttachmentsWithoutAccessRights(List<IRecipient> performers, List<IEntity> attachments)
    {
      var checkedAttachments = attachments.Where(e => e.Info.AccessRightsMode != Metadata.AccessRightsMode.Type).ToList();
      var attachmentsWithoutAccessRight = new List<IEntity>();
      foreach (var performer in performers)
        foreach (var attachment in checkedAttachments)
          if (!attachmentsWithoutAccessRight.Contains(attachment))
      {
        var accessRights = (IInternalEntityAccessRights)attachment.AccessRights;
        if (!accessRights.IsGrantedWithoutSubstitution(Security.BasicOperations.Read, performer))
          attachmentsWithoutAccessRight.Add(attachment);
      }
      return attachmentsWithoutAccessRight;
    }
    
    #endregion
    
    #region Роли для рабочих столов
    
    /// <summary>
    /// Проверить вхождение текущего пользователя в роль делопроиоизводителей.
    /// </summary>
    /// <returns>True, если входит, иначе false.</returns>
    [Remote(IsPure = true), Public]
    public static bool IncludedInClerksRole()
    {
      return IncludedInRole(Constants.Module.RoleGuid.ClerksRole);
    }
    
    /// <summary>
    /// Проверить вхождение текущего пользователя в роль руководителей подразделений.
    /// </summary>
    /// <returns>True, если входит, иначе false.</returns>
    [Public]
    public static bool IncludedInDepartmentManagersRole()
    {
      return IncludedInRole(Constants.Module.RoleGuid.DepartmentManagersRole);
    }
    
    /// <summary>
    /// Проверить вхождение текущего пользователя в роль руководителей НОР.
    /// </summary>
    /// <returns>True, если входит, иначе false.</returns>
    [Public]
    public static bool IncludedInBusinessUnitHeadsRole()
    {
      return IncludedInRole(Constants.Module.RoleGuid.BusinessUnitHeadsRole);
    }
    
    /// <summary>
    /// Проверить вхождение текущего пользователя в роль.
    /// </summary>
    /// <param name="roleSid">Sid роли.</param>
    /// <returns>True, если входит, иначе false.</returns>
    [Remote]
    public static bool IncludedInRole(Guid roleSid)
    {
      return Users.Current.IncludedIn(roleSid);
    }
    
    /// <summary>
    /// Проверить, является ли пользователь администратором или аудитором.
    /// </summary>
    /// <returns>True, если является, иначе false.</returns>
    [Remote(IsPure = true), Public]
    public static bool IsAdministratorOrAdvisor()
    {
      return Users.Current.IncludedIn(Roles.Administrators) || Users.Current.IncludedIn(Roles.Auditors);
    }
    
    #endregion
    
    #region Regex
    
    /// <summary>
    /// Получить индекс и адрес без индекса.
    /// </summary>
    /// <param name="address">Андрес с индексом.</param>
    /// <returns>Структуры с индексом и адресом без индекса.</returns>
    public static Structures.Module.ZipCodeAndAddress ParseZipCode(string address)
    {
      if (string.IsNullOrEmpty(address))
        return ZipCodeAndAddress.Create(string.Empty, string.Empty);
      
      // Индекс распознавать с ",", чтобы их удалить из адреса. В адресе на конверте индекса быть не должно.
      var zipCodeRegex = ",*\\s*([0-9]{6}),*";
      var zipCodeMatch = Regex.Match(address, zipCodeRegex);
      var zipCode = zipCodeMatch.Success ? zipCodeMatch.Groups[1].Value : string.Empty;
      if (!string.IsNullOrEmpty(zipCode))
        address = address.Replace(zipCodeMatch.Value, string.Empty).Trim();
      
      return ZipCodeAndAddress.Create(zipCode, address);
    }
    
    #endregion
    
    #region Построение отчетов с конвертами

    /// <summary>
    /// Создать и заполнить временную таблицу для конвертов.
    /// </summary>
    /// <param name="reportSessionId">Идентификатор отчета.</param>
    /// <param name="outgoingDocuments">Список Исходящих документов.</param>
    /// <param name="contractualDocuments">Список Договорных документов.</param>
    /// <param name="accountingDocuments">Список Финансовых документов.</param>
    public static void FillEnvelopeTable(string reportSessionId, List<IOutgoingDocumentBase> outgoingDocuments, List<IContractualDocumentBase> contractualDocuments, List<IAccountingDocumentBase> accountingDocuments)
    {
      var id = 1;
      var dataTable = new List<EnvelopeReportTableLine>();
      
      var documents = new List<AddresseeAndSender>();
      foreach (var document in outgoingDocuments)
      {
        foreach (var addressee in document.Addressees.OrderBy(a => a.Number))
        {
          var envelopeInfo = AddresseeAndSender.Create(addressee.Correspondent, document.BusinessUnit);
          documents.Add(envelopeInfo);
        }
      }
      foreach (var document in contractualDocuments)
      {
        var envelopeInfo = AddresseeAndSender.Create(document.Counterparty, document.BusinessUnit);
        documents.Add(envelopeInfo);
      }
      foreach (var document in accountingDocuments)
      {
        var envelopeInfo = AddresseeAndSender.Create(document.Counterparty, document.BusinessUnit);
        documents.Add(envelopeInfo);
      }
      
      foreach (var document in documents)
      {
        var tableLine = EnvelopeReportTableLine.Create();
        // Идентификатор отчета.
        tableLine.ReportSessionId = reportSessionId;
        // ИД.
        tableLine.Id = id++;
        
        var correspondent = document.Addresse;
        var correspondentZipCode = string.Empty;
        var correspondentAddress = string.Empty;
        var correspondentName = string.Empty;
        if (correspondent != null)
        {
          var addressToParse = !string.IsNullOrEmpty(correspondent.PostalAddress)
            ? correspondent.PostalAddress
            : correspondent.LegalAddress;
          var zipCodeToParsingResult = Functions.Module.ParseZipCode(addressToParse);
          correspondentZipCode = zipCodeToParsingResult.ZipCode;
          correspondentAddress = zipCodeToParsingResult.Address;
          var person = Parties.People.As(correspondent);
          if (person != null)
            correspondentName = Parties.PublicFunctions.Person.GetFullName(person, Core.DeclensionCase.Dative);
          else
            correspondentName = correspondent.Name;
        }
        
        var businessUnit = document.Sender;
        var businessUnitZipCode = string.Empty;
        var businessUnitAddress = string.Empty;
        var businessUnitName = string.Empty;
        if (businessUnit != null)
        {
          var addressFromParse = !string.IsNullOrEmpty(businessUnit.PostalAddress)
            ? businessUnit.PostalAddress
            : businessUnit.LegalAddress;
          var zipCodeFromParsingResult = Functions.Module.ParseZipCode(addressFromParse);
          businessUnitZipCode = zipCodeFromParsingResult.ZipCode;
          businessUnitAddress = zipCodeFromParsingResult.Address;
          businessUnitName = businessUnit.Name;
        }
        
        tableLine.ToName = correspondentName;
        tableLine.ToPlace = correspondentAddress;
        // Если нет индекса, установить 6 пробелов, чтобы индекс выглядел как сетка, а не 000000.
        tableLine.ToZipCode = string.IsNullOrEmpty(correspondentZipCode) ? "      " : correspondentZipCode;
        
        tableLine.FromName = businessUnitName;
        tableLine.FromPlace = businessUnitAddress;
        tableLine.FromZipCode = businessUnitZipCode;
        
        dataTable.Add(tableLine);
      }
      
      Functions.Module.WriteStructuresToTable(Constants.EnvelopeC4Report.EnvelopesTableName, dataTable);
    }
    
    #endregion
    
    #region Асинхронная выдача прав
    
    /// <summary>
    /// Создать асинхронное событие выдачи прав на документ.
    /// </summary>
    /// <param name="documentId">ИД документа.</param>
    /// <param name="ruleId">ИД правила выдачи прав.</param>
    /// <param name="grantRightChildDocument">Выдавать права дочерним документам.</param>
    [Public]
    public void CreateGrantAccessRightsToDocumentAsyncHandler(int documentId, int? ruleId, bool grantRightChildDocument)
    {
      var asyncRightsHandler = Docflow.AsyncHandlers.GrantAccessRightsToDocument.Create();
      asyncRightsHandler.DocumentId = documentId;
      if (ruleId != null)
        asyncRightsHandler.RuleId = ruleId.Value;
      asyncRightsHandler.GrantRightToChildDocuments = grantRightChildDocument;
      asyncRightsHandler.ExecuteAsync();
    }
    
    /// <summary>
    /// Создать асинхронное событие выдачи прав от правила выдачи прав.
    /// </summary>
    /// <param name="id">ИД правила выдачи прав.</param>
    [Public]
    public void CreateGrantAccessRightsToDocumentsByRuleAsyncHandler(int id)
    {
      var asyncRightsHandler = Docflow.AsyncHandlers.GrantAccessRightsToDocumentsByRule.Create();
      asyncRightsHandler.RuleId = id;
      asyncRightsHandler.ExecuteAsync();
    }
    
    /// <summary>
    /// Выдать права на документ.
    /// </summary>
    /// <param name="documentId">ИД документа.</param>
    /// <param name="ruleId">ИД правила выдачи прав.</param>
    /// <param name="grantRightChildDocument">Выдавать права дочерним документам.</param>
    /// <returns>True, если права были успешно выданы.</returns>
    public virtual bool GrantRightsToDocument(int documentId, int ruleId, bool grantRightChildDocument)
    {
      var allRules = AccessRightsRules.GetAll(s => s.Status == Docflow.AccessRightsRule.Status.Active).ToList();
      if (!allRules.Any())
      {
        Logger.DebugFormat("TryGrantRightsByRule: no rights for document {0}", documentId);
        return true;
      }
      
      var document = OfficialDocuments.GetAll(d => d.Id == documentId).FirstOrDefault();
      if (document == null)
      {
        Logger.DebugFormat("TryGrantRightsByRule: no document with id {0}", documentId);
        return true;
      }
      
      var rule = AccessRightsRules.GetAll(r => r.Id == ruleId).FirstOrDefault();
      if (rule == null && ruleId != 0)
      {
        Logger.DebugFormat("TryGrantRightsByRule: no rights with id {0}", ruleId);
        return true;
      }

      // Права на документ.
      var documentRules = GetAvailableRules(document, allRules);
      
      if (allRules.Any(s => s.GrantRightsOnLeadingDocument == true))
      {
        var leadingDocumentIds = GetLeadingDocuments(document);
        foreach (var leadingDocumentId in leadingDocumentIds)
        {
          var leadingDocument = OfficialDocuments.GetAll(d => d.Id == leadingDocumentId).FirstOrDefault();
          var leadDocumentRules = GetAvailableRules(leadingDocument, allRules).Where(s => s.GrantRightsOnLeadingDocument == true).ToList();
          documentRules.AddRange(leadDocumentRules);
        }
      }
      
      if (rule != null)
      {
        if (documentRules.Contains(rule))
          documentRules = new List<IAccessRightsRule>() { rule };
        else
          return true;
      }

      foreach (var documentRule in documentRules)
      {
        if (!TryGrantRightsToDocument(document, documentRule))
          return false;
        
        // Права на дочерние документы от ведущего.
        if (documentRule.GrantRightsOnLeadingDocument == true && grantRightChildDocument == true)
        {
          var childDocumentIds = GetChildDocuments(document);
          foreach (var childDocumentId in childDocumentIds)
          {
            PublicFunctions.Module.CreateGrantAccessRightsToDocumentAsyncHandler(childDocumentId, documentRule.Id, false);
            Logger.DebugFormat("TryGrantRightsByRule: create child document queue for document {0}, rule {1}", childDocumentId, documentRule.Id);
          }
        }
      }

      return true;
    }

    /// <summary>
    /// Выдать права на документ по правилу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="rule">Правило.</param>
    /// <returns>Возвращает true, если права удалось выдать, false - если надо повторить позже.</returns>
    public static bool TryGrantRightsToDocument(IOfficialDocument document, IAccessRightsRule rule)
    {
      Logger.DebugFormat("TryGrantRightsToDocument: document {0}, rule {1}", document.Id, rule.Id);
      
      var isChanged = false;
      foreach (var member in rule.Members)
      {
        if (!document.AccessRights.IsGrantedDirectly(Docflow.PublicFunctions.Module.GetRightTypeGuid(member.RightType), member.Recipient))
        {
          if (Locks.GetLockInfo(document).IsLockedByOther)
          {
            Logger.DebugFormat("TryGrantRightsToDocument: cannot grant rights, document {0} is locked.", document.Id, rule.Id);
            return false;
          }

          document.AccessRights.Grant(member.Recipient, Docflow.PublicFunctions.Module.GetRightTypeGuid(member.RightType));
          isChanged = true;
        }
      }
      if (isChanged)
      {
        try
        {
          ((Domain.Shared.IExtendedEntity)document).Params[Constants.OfficialDocument.DontUpdateModified] = true;
          document.Save();
        }
        catch (Exception ex)
        {
          Logger.Error("TryGrantRightsToDocument: cannot grant rights to document", ex);
          return false;
        }
      }
      else
      {
        Logger.DebugFormat("TryGrantRightsToDocument: rights already granted for document {0}, rule {1}", document.Id, rule.Id);
      }
      
      return true;
    }
    
    /// <summary>
    /// Получить из списка правил подходящие для документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="rules">Правила.</param>
    /// <returns>Подходящие правила.</returns>
    public static List<IAccessRightsRule> GetAvailableRules(IOfficialDocument document, List<IAccessRightsRule> rules)
    {
      var documentGroup = Functions.OfficialDocument.GetDocumentGroup(document);
      
      return rules
        .Where(s => s.Status == Docflow.AccessRightsRule.Status.Active)
        .Where(s => !s.DocumentKinds.Any() || s.DocumentKinds.Any(k => Equals(k.DocumentKind, document.DocumentKind)))
        .Where(s => !s.BusinessUnits.Any() || s.BusinessUnits.Any(u => Equals(u.BusinessUnit, document.BusinessUnit)))
        .Where(s => !s.Departments.Any() || s.Departments.Any(k => Equals(k.Department, document.Department)))
        .Where(s => !s.DocumentGroups.Any() || s.DocumentGroups.Any(k => Equals(k.DocumentGroup, documentGroup))).ToList();
    }
    
    /// <summary>
    /// Получить ведущие документы.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Ведущие документы.</returns>
    private static List<int> GetLeadingDocuments(IOfficialDocument document)
    {
      var documents = new List<int>() { document.Id };
      var leadingDocuments = new List<int>();
      while (document.LeadingDocument != null && !documents.Contains(document.LeadingDocument.Id))
      {
        documents.Add(document.LeadingDocument.Id);
        leadingDocuments.Add(document.LeadingDocument.Id);
        document = document.LeadingDocument;
      }
      return leadingDocuments;
    }
    
    /// <summary>
    /// Получить ведомые документы.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Ведомые документы.</returns>
    private static List<int> GetChildDocuments(IOfficialDocument document)
    {
      var documents = new List<int>() { document.Id };
      var allChildDocuments = new List<int>();
      var childDocuments = OfficialDocuments.GetAll(d => d.LeadingDocument != null && documents.Contains(d.LeadingDocument.Id) && !documents.Contains(d.Id))
        .Select(d => d.Id)
        .ToList();
      while (childDocuments.Any())
      {
        documents.AddRange(childDocuments);
        allChildDocuments.AddRange(childDocuments);
        childDocuments = OfficialDocuments.GetAll(d => d.LeadingDocument != null && documents.Contains(d.LeadingDocument.Id) && !documents.Contains(d.Id))
          .Select(d => d.Id)
          .ToList();
      }
      return allChildDocuments;
    }
    
    /// <summary>
    /// Получить вариант автоматической выдачи прав на документы.
    /// </summary>
    /// <returns>Вариант выдачи прав.</returns>
    [Public]
    public static string GetGrantRightMode()
    {
      var command = string.Format(Queries.Module.SelectDocflowParamsValue, Constants.Module.GrantRightsMode);
      try
      {
        var executionResult = Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var grantRightsType = string.Empty;
        if (!(executionResult is DBNull) && executionResult != null)
          grantRightsType = executionResult.ToString();
        Logger.DebugFormat("Grant rights to documents type is {0}", grantRightsType);

        return grantRightsType;
      }
      catch (Exception ex)
      {
        Logger.DebugFormat("Error while getting grant rights to documents mode", ex);
        return string.Empty;
      }
    }
    
    #endregion
    
    #region Получение параметров из Sungero_Docflow_Params
    
    /// <summary>
    /// Получить значение параметра из Sungero_Docflow_Params.
    /// </summary>
    /// <param name="paramName">Наименование параметра.</param>
    /// <returns>Значение параметра. Тип: double.</returns>
    [Public, Remote(IsPure = true)]
    public static double GetDocflowParamsNumbericValue(string paramName)
    {
      double result = 0;
      var paramValue = GetDocflowParamsValue(paramName);
      if (!(paramValue is DBNull) && paramValue != null)
        double.TryParse(paramValue.ToString(), out result);
      return result;
    }
    
    /// <summary>
    /// Получить значение параметра из Sungero_Docflow_Params.
    /// </summary>
    /// <param name="paramName">Наименование параметра.</param>
    /// <returns>Значение параметра. Тип: string.</returns>
    [Public, Remote(IsPure = true)]
    public static string GetDocflowParamsStringValue(string paramName)
    {
      var paramValue = GetDocflowParamsValue(paramName);
      return !(paramValue is DBNull) && paramValue != null ? paramValue.ToString() : null;
    }
    
    /// <summary>
    /// Получить значение параметра из Sungero_Docflow_Params.
    /// </summary>
    /// <param name="paramName">Наименование параметра.</param>
    /// <returns>Значение параметра. Тип: DateTime.</returns>
    [Public, Remote(IsPure = true)]
    public static DateTime? GetDocflowParamsDateTimeValue(string paramName)
    {
      var paramStringValue = GetDocflowParamsStringValue(paramName);
      
      if (string.IsNullOrWhiteSpace(paramStringValue))
        return null;
      
      DateTime date;
      // DateTime.TryParse() возвращает DateTime.MinValue в случае неуспешного преобразования.
      if (!Calendar.TryParseDate(paramStringValue, out date))
        return null;
      
      return date;
    }
    
    /// <summary>
    /// Получить значение параметра из docflow_params.
    /// </summary>
    /// <param name="paramName">Наименование параметра.</param>
    /// <returns>Значение параметра.</returns>
    [Public]
    public static object GetDocflowParamsValue(string paramName)
    {
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = Queries.Module.SelectDocflowParamsValueByKey;
        Docflow.PublicFunctions.Module.AddStringParameterToCommand(command, "@key", paramName);
        return command.ExecuteScalar();
      }
    }
    
    #endregion
    
    #region Рассылка по почте
    
    /// <summary>
    /// Агент отправки уведомления о заданиях.
    /// </summary>
    public virtual void SendMailNotification()
    {
      using (new Domain.Session())
      {
        bool? needUpdateLastNotificationDate = null;
        var previousRun = GetLastNotificationDate();
        var notificationDate = Calendar.Now;

        try
        {
          using (var mailClient = new System.Net.Mail.SmtpClient())
          {
            var newAssignments = this.GetNewAssignments(previousRun, notificationDate);
            var newAssignmentsResult = this.TrySendNewAssignmentsMailing(mailClient, previousRun, notificationDate, newAssignments);

            var expiredAssignments = this.GetExpiredAssignments(previousRun, notificationDate);
            var expiredAssignmentsResult = this.TrySendExpiredAssignmentsMailing(mailClient, previousRun, notificationDate, expiredAssignments);
            needUpdateLastNotificationDate = newAssignmentsResult | expiredAssignmentsResult | !(newAssignmentsResult == false | expiredAssignmentsResult == false);
          }
        }
        finally
        {
          if (needUpdateLastNotificationDate == true)
            UpdateLastNotificationDate(notificationDate);
          else
            Logger.Debug("Last notification date hasn't been changed");
        }
      }
    }
    
    /// <summary>
    /// Получить дату последней рассылки уведомлений.
    /// </summary>
    /// <returns>Дата последней рассылки.</returns>
    public static DateTime GetLastNotificationDate()
    {
      var key = "LastNotificationOfAssignment";
      var command = string.Format(Queries.Module.SelectDocflowParamsValue, key);
      try
      {
        var executionResult = Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var date = string.Empty;
        if (!(executionResult is DBNull) && executionResult != null)
          date = executionResult.ToString();
        Logger.DebugFormat("Last notification date in DB is {0} (UTC)", date);
        
        DateTime result = Calendar.FromUtcTime(DateTime.Parse(date, null, System.Globalization.DateTimeStyles.AdjustToUniversal));

        if ((result - Calendar.Now).TotalDays > 1)
          return Calendar.Today;
        else
          return result;
      }
      catch (Exception ex)
      {
        Logger.Error("Error while getting last notification date", ex);
        return Calendar.Today;
      }
    }
    
    /// <summary>
    /// Запустить рассылку по новым заданиям.
    /// </summary>
    /// <param name="mailClient">Почтовый клиент.</param>
    /// <param name="previousRun">Дата прошлого запуска рассылки.</param>
    /// <param name="notificationDate">Дата текущей рассылки.</param>
    /// <param name="assignments">Задания, по которым будет выполнена рассылка.</param>
    /// <returns>True, если хотя бы одно письмо было отправлено, иначе - false.</returns>
    public bool? TrySendNewAssignmentsMailing(System.Net.Mail.SmtpClient mailClient, DateTime previousRun, DateTime notificationDate, List<IAssignmentBase> assignments)
    {
      Logger.Debug("Checking new assignments for mailing");
      var hasErrors = false;

      var anyMailSent = false;
      foreach (var assignment in assignments)
      {
        var employee = Employees.As(assignment.Performer);
        if (employee == null)
          continue;

        var endDate = assignment.Created.Value.AddDays(-1);
        var substitutions = Substitutions.GetAll(s => Equals(s.User, employee))
          .Where(s => s.IsSystem != true)
          .Where(s => s.StartDate == null || s.StartDate.Value <= assignment.Created)
          .Where(s => s.EndDate == null || s.EndDate.Value >= endDate);
        
        var substitutes = Employees.GetAll(r => r.NeedNotifyNewAssignments == true)
          .Where(e => substitutions.Any(s => Equals(s.Substitute, e)))
          .Where(e => e.Status != CoreEntities.DatabookEntry.Status.Closed)
          .ToList();
        
        var subject = this.GetNewAssignmentSubject(assignment);
        var mailSent = this.TrySendMailByAssignment(assignment, subject, false, mailClient, employee, substitutes);
        if (!mailSent.IsSuccess)
          hasErrors = true;
        if (mailSent.IsSuccess && mailSent.AnyMailSended)
          anyMailSent = true;
      }
      if (!assignments.Any())
        Logger.Debug("No new assignments for mailing");
      else if (!anyMailSent)
        Logger.Debug("No subscribers for new assignments mailing");
      if (!anyMailSent && !hasErrors)
        return null;
      return anyMailSent || !hasErrors;
    }
    
    /// <summary>
    /// Получить новые задания, по которым надо сделать рассылку.
    /// </summary>
    /// <param name="previousRun">Предыдущий запуск.</param>
    /// <param name="notificationDate">Текущий запуск.</param>
    /// <returns>Задания, по которым будет выполнена рассылка.</returns>
    public virtual List<IAssignmentBase> GetNewAssignments(DateTime previousRun, DateTime notificationDate)
    {
      return AssignmentBases
        .GetAll(a => previousRun <= a.Created && a.Created < notificationDate && a.IsRead == false && a.Status != Workflow.AssignmentBase.Status.Aborted)
        .Expand("Performer")
        .ToList();
    }
    
    /// <summary>
    /// Сформировать тему письма по новому заданию.
    /// </summary>
    /// <param name="assignment">Задание, для которого формируется письмо.</param>
    /// <returns>Тема письма.</returns>
    public virtual string GetNewAssignmentSubject(IAssignmentBase assignment)
    {
      return Docflow.Resources.NewAssignmentMailSubjectFormat(this.GetAssignmentTypeName(assignment), GetAuthorSubjectPart(assignment), assignment.Subject);
    }
    
    /// <summary>
    /// Запустить рассылку по просроченным заданиям.
    /// </summary>
    /// <param name="mailClient">Почтовый клиент.</param>
    /// <param name="previousRun">Дата прошлого запуска рассылки.</param>
    /// <param name="notificationDate">Дата текущей рассылки.</param>
    /// <param name="assignments">Задания, по которым будет выполнена рассылка.</param>
    /// <returns>True, если хотя бы одно письмо было отправлено, иначе - false.</returns>
    public bool? TrySendExpiredAssignmentsMailing(System.Net.Mail.SmtpClient mailClient, DateTime previousRun, DateTime notificationDate, List<IAssignment> assignments)
    {
      Logger.Debug("Checking expired assignments for mailing");
      var hasErrors = false;
      
      var anyMailSent = false;
      
      foreach (var assignment in assignments)
      {
        var employee = Employees.As(assignment.Performer);
        if (employee == null)
          continue;

        var endDate = notificationDate.AddDays(-1);
        var substitutions = Substitutions.GetAll(s => Equals(s.User, employee))
          .Where(s => s.IsSystem != true)
          .Where(s => s.StartDate == null || s.StartDate.Value <= notificationDate)
          .Where(s => s.EndDate == null || s.EndDate.Value >= endDate);
        
        var substitutes = Employees.GetAll(r => r.NeedNotifyExpiredAssignments == true)
          .Where(e => substitutions.Any(s => Equals(s.Substitute, e)))
          .Where(e => e.Status != CoreEntities.DatabookEntry.Status.Closed)
          .ToList();

        var subject = this.GetExpiredAssignmentSubject(assignment);
        var mailSent = this.TrySendMailByAssignment(assignment, subject, true, mailClient, employee, substitutes);
        if (!mailSent.IsSuccess)
          hasErrors = true;
        if (mailSent.IsSuccess && mailSent.AnyMailSended)
          anyMailSent = true;
      }
      if (!assignments.Any())
        Logger.Debug("No expired assignments for mailing");
      else if (!anyMailSent)
        Logger.Debug("No subscribers for expired assignments mailing");
      if (!anyMailSent && !hasErrors)
        return null;
      return anyMailSent || !hasErrors;
    }
    
    /// <summary>
    /// Получить просроченные задания, по которым надо сделать рассылку.
    /// </summary>
    /// <param name="previousRun">Предыдущий запуск.</param>
    /// <param name="notificationDate">Текущий запуск.</param>
    /// <returns>Задания, по которым будет выполнена рассылка.</returns>
    public virtual List<IAssignment> GetExpiredAssignments(DateTime previousRun, DateTime notificationDate)
    {
      return Assignments
        .GetAll(a => a.Status == Workflow.AssignmentBase.Status.InProcess  &&
                (a.Deadline.HasValue && a.Deadline.Value.HasTime() &&
                 previousRun <= a.Deadline && a.Deadline < notificationDate ||
                 a.Deadline.HasValue && !a.Deadline.Value.HasTime() &&
                 previousRun <= a.Deadline.Value.AddDays(1) && a.Deadline.Value.AddDays(1) < notificationDate))
        .Expand("Performer")
        .ToList();
    }
    
    /// <summary>
    /// Сформировать тему письма по просроченному заданию.
    /// </summary>
    /// <param name="assignment">Задание, для которого формируется письмо.</param>
    /// <returns>Тема письма.</returns>
    public virtual string GetExpiredAssignmentSubject(IAssignmentBase assignment)
    {
      return Docflow.Resources.ExpiredAssignmentMailSubjectFormat(this.GetAssignmentTypeName(assignment).ToLower(), GetAuthorSubjectPart(assignment), assignment.Subject);
    }
    
    /// <summary>
    /// Обновить дату последней рассылки уведомлений.
    /// </summary>
    /// <param name="notificationDate">Дата рассылки уведомлений.</param>
    public static void UpdateLastNotificationDate(DateTime notificationDate)
    {
      var key = "LastNotificationOfAssignment";
      
      var newDate = notificationDate.Add(-Calendar.UtcOffset).ToString("yyyy-MM-ddTHH:mm:ss.ffff+0");
      Functions.Module.ExecuteSQLCommandFormat(Queries.Module.InsertOrUpdateDocflowParamsValue, new[] { key, newDate });
      Logger.DebugFormat("Last notification date is set to {0} (UTC)", newDate);
    }
    
    /// <summary>
    /// Получить локализованное имя типа задания.
    /// </summary>
    /// <param name="assignment">Базовое задание.</param>
    /// <returns>Имя типа задания.</returns>
    /// <remarks>Виртуальные функции доступны в шаблоне письма только с паблик атрибутом.</remarks>
    [Public]
    public virtual string GetAssignmentTypeName(IAssignmentBase assignment)
    {
      if (Notices.Is(assignment))
        return Notices.Info.LocalizedName;
      else if (ReviewAssignments.Is(assignment))
        return ReviewAssignments.Info.LocalizedName;
      else
        return Assignments.Info.LocalizedName;
    }
    
    /// <summary>
    /// Получить часть темы письма, которая содержит автора задания.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Часть темы письма с автором задания.</returns>
    public static string GetAuthorSubjectPart(IAssignmentBase assignment)
    {
      if (Equals(assignment.Author, assignment.Performer))
        return string.Empty;

      return string.Format(" {0} {1}", Docflow.Resources.From, GetFormattedUserNameInGenitive(assignment.Author.DisplayValue));
    }

    /// <summary>
    /// Попытаться отправить письмо по заданию.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="subject">Тема.</param>
    /// <param name="isExpired">Признак того, что задание является просроченным.</param>
    /// <param name="mailClient">Почтовый клиент.</param>
    /// <param name="addressee">Получатель письма.</param>
    /// <param name="copies">Получатели копий письма.</param>
    /// <returns>True, если ошибок при отправке не было, иначе - False.</returns>
    public MailSendingResult TrySendMailByAssignment(IAssignmentBase assignment,
                                                     string subject,
                                                     bool isExpired,
                                                     System.Net.Mail.SmtpClient mailClient,
                                                     IEmployee addressee,
                                                     System.Collections.Generic.IEnumerable<IEmployee> copies)
    {
      var needNotify = (isExpired ? addressee.NeedNotifyExpiredAssignments == true : addressee.NeedNotifyNewAssignments == true) &&
        addressee.Status != CoreEntities.DatabookEntry.Status.Closed;

      var to = needNotify && !string.IsNullOrEmpty(addressee.Email) ? addressee.Email : null;
      var cc = copies.Select(e => e.Email).Where(email => !string.IsNullOrEmpty(email)).ToList();
      
      if (string.IsNullOrEmpty(to) && !cc.Any())
        return MailSendingResult.Create(true, false);
      
      bool isSendMailSuccess = false;
      try
      {
        Logger.DebugFormat("Sending mail by assignment with Id = {0}. Is expired: {1}", assignment.Id, isExpired);
        
        this.InternalSendMailByAssignment(assignment, subject, isExpired, mailClient, to, cc);
        
        if (!string.IsNullOrEmpty(to))
          Logger.DebugFormat("Mail to performer with Id = {0} has been sent", addressee.Id);
        else if (needNotify)
          Logger.DebugFormat("Performer with Id = {0} has no email", addressee.Id);
        
        foreach (var employee in copies)
        {
          if (!string.IsNullOrEmpty(employee.Email))
            Logger.DebugFormat("Mail to substitute with Id = {0} has been sent", employee.Id);
          else
            Logger.DebugFormat("Substitute with Id = {0} has no email", employee.Id);
        }
        
        isSendMailSuccess = true;
      }
      catch (FormatException ex)
      {
        Logger.ErrorFormat("Performer with Id = {0} or his substitute has incorrect email", ex, addressee.Id);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Error while sending mail to performer with Id = {0} or his substitute", ex, addressee.Id);
      }
      return MailSendingResult.Create(isSendMailSuccess, isSendMailSuccess && (!string.IsNullOrEmpty(to) || cc.Any()));
    }
    
    /// <summary>
    /// Получить форматированное имя пользователя в родительном падеже.
    /// </summary>
    /// <param name="userName">Имя пользователя.</param>
    /// <returns>Форматированное имя пользователя.</returns>
    public static string GetFormattedUserNameInGenitive(string userName)
    {
      PersonFullName personalData;
      var result = userName;
      if (PersonFullName.TryParse(result, out personalData) && !string.IsNullOrEmpty(personalData.MiddleName))
      {
        personalData.DisplayFormat = PersonFullNameDisplayFormat.LastNameAndInitials;
        result = CaseConverter.ConvertPersonFullNameToTargetDeclension(personalData, Core.DeclensionCase.Genitive);
      }
      return result;
    }
    
    /// <summary>
    /// Отправить письмо по заданию.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="subject">Тема.</param>
    /// <param name="isExpired">Признак того, что задание является просроченным.</param>
    /// <param name="mailClient">Почтовый клиент.</param>
    /// <param name="to">Получатель письма.</param>
    /// <param name="cc">Получатели копий письма.</param>
    public void InternalSendMailByAssignment(IAssignmentBase assignment, string subject, bool isExpired, System.Net.Mail.SmtpClient mailClient, string to, System.Collections.Generic.IEnumerable<string> cc)
    {
      using (var mail = new System.Net.Mail.MailMessage
             {
               Body = this.GenerateBody(assignment, isExpired, cc.Any()),
               IsBodyHtml = true,
               Subject = subject.Replace('\r', ' ').Replace('\n', ' '),
               HeadersEncoding = System.Text.Encoding.UTF8,
               SubjectEncoding = System.Text.Encoding.UTF8,
               BodyEncoding = System.Text.Encoding.UTF8
             })
      {
        if (!string.IsNullOrEmpty(to))
          mail.To.Add(to);
        foreach (var email in cc.Where(e => !string.IsNullOrEmpty(e)))
          mail.CC.Add(email);

        mail.Attachments.AddRange(this.GetMailAttachments(assignment));
        if (assignment.Importance == Workflow.AssignmentBase.Importance.High)
          mail.Priority = System.Net.Mail.MailPriority.High;
        else if (assignment.Importance == Workflow.AssignmentBase.Importance.Low)
          mail.Priority = System.Net.Mail.MailPriority.Low;
        mailClient.Send(mail);
      }
    }
    
    /// <summary>
    /// Собрать вложения в письмо.
    /// </summary>
    /// <param name="assignment">Задание, для которого собираются вложения.</param>
    /// <returns>Вложения.</returns>
    public virtual System.Collections.Generic.IEnumerable<System.Net.Mail.Attachment> GetMailAttachments(IAssignmentBase assignment)
    {
      var logo = new System.IO.MemoryStream(this.GetLogo());
      var inline = new System.Net.Mail.Attachment(logo, "logo.png@01D004A6.A303C580");
      inline.ContentId = "logo.png@01D004A6.A303C580";
      inline.ContentDisposition.Inline = true;
      inline.ContentDisposition.DispositionType = System.Net.Mime.DispositionTypeNames.Inline;
      inline.ContentType = new System.Net.Mime.ContentType("image/png");
      yield return inline;
    }
    
    /// <summary>
    /// Сгенерировать тело письма.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="isExpired">Признак просроченного задания.</param>
    /// <param name="hasSubstitutions">Признак просрочки.</param>
    /// <returns>Тело письма.</returns>
    public virtual string GenerateBody(IAssignmentBase assignment, bool isExpired, bool hasSubstitutions)
    {
      if (!Nustache.Core.Helpers.Contains("process_text"))
        Nustache.Core.Helpers.Register("process_text", ProcessText);
      
      var model = this.GenerateBodyModel(assignment, isExpired, hasSubstitutions);

      return Nustache.Core.Render.StringToString(Docflow.Resources.MailTemplate, model);
    }
    
    /// <summary>
    /// Сгенерировать модель письма.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="isExpired">Признак просроченного задания.</param>
    /// <param name="hasSubstitutions">Признак просрочки.</param>
    /// <returns>Модель письма.</returns>
    public virtual System.Collections.Hashtable GenerateBodyModel(IAssignmentBase assignment, bool isExpired, bool hasSubstitutions)
    {
      var model = new System.Collections.Hashtable();
      model["Assignment"] = assignment;
      model["Attachments"] = assignment.AllAttachments.Where(ent => ent.AccessRights.CanRead(assignment.Performer)).ToList();
      model["HasSubstitutions"] = hasSubstitutions;
      model["IsExpired"] = isExpired;
      model["AdministratorEmail"] = AdministrationSettings.AdministratorEmail;
      if (isExpired)
        model["MailingName"] = Docflow.Resources.ExpiredAssignmentsMailingName.ToString().Replace(" ", "%20");
      else
        model["MailingName"] = Docflow.Resources.NewAssignmentsMailingName.ToString().Replace(" ", "%20");
      if (!string.Equals(assignment.Subject, assignment.MainTask.Subject))
        model["Subject"] = assignment.MainTask.Subject;
      if (!Equals(assignment.Author, assignment.Performer))
        model["Author"] = assignment.Author;
      return model;
    }
    
    /// <summary>
    /// Получить логотип системы.
    /// </summary>
    /// <returns>Логотип системы.</returns>
    public virtual byte[] GetLogo()
    {
      return Sungero.Core.SystemInfo.GetLogo();
    }
    
    /// <summary>
    /// Обработать текст, выделив в нём отдельные абзацы и гиперссылки.
    /// </summary>
    /// <param name="context">Контекст письма.</param>
    /// <param name="args">Аргументы.</param>
    /// <param name="options">Опции.</param>
    /// <param name="function">Функция.</param>
    /// <param name="inverse">Инверс.</param>
    public static void ProcessText(Nustache.Core.RenderContext context, System.Collections.Generic.IList<object> args,
                                   System.Collections.Generic.IDictionary<string, object> options,
                                   Nustache.Core.RenderBlock function, Nustache.Core.RenderBlock inverse)
    {
      var text = (args[0] ?? string.Empty).ToString().Replace(Environment.NewLine, "\n");
      var entityHyperlinksParser = new EntityHyperlinkParser(Sungero.Domain.Shared.HyperlinkParsers.HttpHyperlinkParser);
      var textChunks = entityHyperlinksParser.Parse(text);
      foreach (var chunk in textChunks)
        function(chunk);
    }

    #endregion
    
    #region Рассылка уведомлений
    
    /// <summary>
    /// Получить параметры по умолчанию для рассылки уведомлений по документам.
    /// </summary>
    /// <param name="lastNotificationParamName">Имя параметра в Sungero_Docflow_Params с датой последнего уведомления.</param>
    /// <param name="noticesTableName">Имя таблицы, в которой содержится информация об уведомлениях.</param>
    /// <returns>Параметры для рассылки уведомлений по документам.</returns>
    [Public]
    public static IExpiringDocsNotificationParams GetDefaultExpiringDocsNotificationParams(string lastNotificationParamName,
                                                                                           string noticesTableName)
    {
      var param = ExpiringDocsNotificationParams.Create();
      param.LastNotification = PublicFunctions.Module.GetLastNotificationDate(lastNotificationParamName, null);
      param.LastNotificationReserve = param.LastNotification.AddDays(-2);
      param.Today = Calendar.Today;
      param.TodayReserve = param.Today.AddDays(2);
      param.BatchCount = 100;
      param.ExpiringDocTableName = noticesTableName;
      param.LastNotificationParamName = lastNotificationParamName;
      param.TaskParams = ExpiringNotificationTaskParams.Create();
      
      return param;
    }
    
    /// <summary>
    /// Получить дату последней рассылки уведомлений.
    /// </summary>
    /// <param name="lastNotificationParameterName">Наименование параметра в БД, содержащего дату последней рассылки.</param>
    /// <param name="defaultDate">Дата по умолчанию.</param>
    /// <returns>Дата последней рассылки. При отсутствии параметра с датой последней рассылки или неверного формата ее значение - дата по умолчанию.</returns>
    [Public]
    public static DateTime GetLastNotificationDate(string lastNotificationParameterName,
                                                   DateTime? defaultDate = null)
    {
      if (!defaultDate.HasValue)
        defaultDate = Calendar.Now.AddDays(-1);
      var lastNotificationDate = Docflow.PublicFunctions.Module.Remote.GetDocflowParamsDateTimeValue(lastNotificationParameterName);
      if (!lastNotificationDate.HasValue)
      {
        Logger.DebugFormat("Has no Last notification date in DB or it has incorrect format. Use defaults: {0}", defaultDate);
        return defaultDate.Value;
      }
      
      Logger.DebugFormat("Last notification date in DB is {0}", lastNotificationDate.Value);
      return lastNotificationDate.Value;
    }
    
    /// <summary>
    /// Получить документы, по которым уже отправлены уведомления.
    /// </summary>
    /// <param name="expiringDocumentTableName">Имя таблицы, в которой хранятся Id документов для завершения.</param>
    /// <returns>Список Id документов, по которым задачи уже отправлены.</returns>
    [Public]
    public static List<int> GetDocumentsWithSendedTask(string expiringDocumentTableName)
    {
      var result = new List<int>();
      var commandText = string.Format(Queries.Module.SelectDocumentWithSendedTask, expiringDocumentTableName);
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        try
        {
          command.CommandText = commandText;
          using (var rdr = command.ExecuteReader())
          {
            while (rdr.Read())
              result.Add(rdr.GetInt32(0));
          }
          return result;
        }
        catch (Exception ex)
        {
          Logger.Error("Error while getting array of docs with sent tasks", ex);
          return result;
        }
      }
    }
    
    /// <summary>
    /// Убрать из таблицы для отправки Id документов.
    /// </summary>
    /// <param name="expiringDocsTableName">Имя таблицы для отправки уведомлений.</param>
    /// <param name="ids">Id документов.</param>
    [Public]
    public static void ClearIdsFromExpiringDocsTable(string expiringDocsTableName, List<int> ids)
    {
      if (string.IsNullOrWhiteSpace(expiringDocsTableName))
        return;
      if (!ids.Any())
        return;
      string command = string.Empty;
      command = string.Format(Queries.Module.DeleteDocumentIdsWithoutTask, expiringDocsTableName, string.Join(", ", ids));
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
    }
    
    /// <summary>
    /// Очистить таблицу для отправки уведомлений.
    /// </summary>
    /// <param name="expiringDocsTableName">Имя таблицы для отправки уведомлений.</param>
    /// <param name="taskIsNull">True - очистить записи с неотправленными задачами,
    /// False - очистить записи с отправленными задачами.</param>
    [Public]
    public static void ClearExpiringTable(string expiringDocsTableName, bool taskIsNull)
    {
      string command = string.Empty;
      
      if (taskIsNull)
        command = string.Format(Queries.Module.ClearExpiringTableWithoutTasks, expiringDocsTableName);
      else
        command = string.Format(Queries.Module.ClearExpiringTableWithTasks, expiringDocsTableName);
      
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
    }
    
    /// <summary>
    /// Записать Id документов в таблицу для отправки.
    /// </summary>
    /// <param name="expiringDocsTableName">Имя таблицы для отправки уведомлений.</param>
    /// <param name="ids">Id документов.</param>
    [Public]
    public static void AddExpiringDocumentsToTable(string expiringDocsTableName, List<int> ids)
    {
      if (string.IsNullOrWhiteSpace(expiringDocsTableName))
        return;
      if (!ids.Any())
        return;
      var command = string.Format(Queries.Module.AddExpiringDocumentsToTable, expiringDocsTableName, string.Join("), (", ids));
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
    }
    
    /// <summary>
    /// Сотрудники, которых необходимо уведомить о сроке доверенности.
    /// </summary>
    /// <param name="powerOfAttorney">Доверенность.</param>
    /// <returns>Список сотрудников.</returns>
    public virtual List<IUser> GetNotificationPerformers(IPowerOfAttorney powerOfAttorney)
    {
      var issuedTo = powerOfAttorney.IssuedTo;
      var preparedBy = powerOfAttorney.PreparedBy;
      var issuedToManager = Employees.Null;
      if (issuedTo != null)
        issuedToManager = PublicFunctions.Module.Remote.GetManager(issuedTo);
      
      var performers = new List<IUser>() { };
      
      if (issuedTo != null)
      {
        var needNotice = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(issuedTo).MyPowersOfAttorneyNotification;
        if (needNotice == true)
          performers.Add(issuedTo);
      }
      
      if (preparedBy != null)
      {
        var needNotice = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(preparedBy).MyPowersOfAttorneyNotification;
        if (needNotice == true)
          performers.Add(preparedBy);
      }
      
      if (issuedToManager != null)
      {
        var needNotice = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(issuedToManager).MySubordinatesPowersOfAttorneyNotification;
        if (needNotice == true)
          performers.Add(issuedToManager);
      }
      
      return performers;
    }
    
    /// <summary>
    /// Добавить в таблицу для отправки задачу, с указанием документа.
    /// </summary>
    /// <param name="expiringDocsTableName">Имя таблицы для отправки уведомлений.</param>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача, которая была запущена.</param>
    [Public]
    public static void AddTaskToExpiringTable(string expiringDocsTableName, int document, int task)
    {
      if (string.IsNullOrWhiteSpace(expiringDocsTableName))
        return;
      var command = string.Format(Queries.Module.AddTaskExpiringDocumentTable, expiringDocsTableName, document, task);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
      Logger.DebugFormat("Task {0} for document {1} started and marked in db.", task, document);
    }
    
    /// <summary>
    /// Проверить, по всем ли документам запущены уведомления.
    /// </summary>
    /// <param name="expiringDocsTableName">Имя таблицы для отправки уведомлений.</param>
    /// <returns>True, если все завершено корректно.</returns>
    [Public]
    public static bool IsAllNotificationsStarted(string expiringDocsTableName)
    {
      if (string.IsNullOrWhiteSpace(expiringDocsTableName))
        return false;
      var command = string.Format(Queries.Module.CountNullExpiringTasks, expiringDocsTableName);
      try
      {
        var executionResult = Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var result = 0;
        if (!(executionResult is DBNull) && executionResult != null)
          int.TryParse(executionResult.ToString(), out result);
        Logger.DebugFormat("Not sent tasks count: {0}", result);
        
        return result == 0;
      }
      catch (Exception ex)
      {
        Logger.Error("Error while getting count of not sent tasks", ex);
        return false;
      }
    }
    
    /// <summary>
    /// Обновить дату последней рассылки уведомлений.
    /// </summary>
    /// <param name="notificationParams">Параметры уведомлений.</param>
    [Public]
    public static void UpdateLastNotificationDate(IExpiringDocsNotificationParams notificationParams)
    {
      var newDate = notificationParams.Today.ToString("yyyy-MM-dd HH:mm:ss");
      var command = string.Format(Queries.Module.UpdateLastExpiringNotificationDate,
                                  notificationParams.LastNotificationParamName,
                                  newDate);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
      Logger.DebugFormat("Last notification date is set to {0}", newDate);
    }
    
    /// <summary>
    /// Попытаться отправить уведомления по документу, срок которого истекает.
    /// </summary>
    /// <param name="notificationParams">Параметры уведомления.</param>
    [Public]
    public static void TrySendExpiringDocNotifications(IExpiringDocsNotificationParams notificationParams)
    {
      if (notificationParams.TaskParams.Document == null)
      {
        Logger.DebugFormat("Has no document to notify.");
        return;
      }
      
      var docTypeName = notificationParams.TaskParams.Document.GetType().GetFinalType().Name;
      var docId = notificationParams.TaskParams.Document.Id;
      var documentLogView = string.Format("Document ({0}: {1})", docTypeName, docId);
      var performers = notificationParams.TaskParams.Performers;
      var attachments = notificationParams.TaskParams.Attachments;
      var subject = notificationParams.TaskParams.Subject;
      var activeText = notificationParams.TaskParams.ActiveText;
      
      if (performers == null || !performers.Any())
      {
        PublicFunctions.Module.AddTaskToExpiringTable(notificationParams.ExpiringDocTableName, docId, 0);
        Logger.DebugFormat("{0} has no employees to notify.", documentLogView);
      }
      
      var performerIds = performers.Select(p => p.Id.ToString()).ToList();
      var attachmentIds = attachments.Select(a => a.Id.ToString()).ToList();
      
      try
      {
        var newTask = Workflow.SimpleTasks.CreateWithNotices(subject, performers, attachments.ToArray());
        newTask.NeedsReview = false;
        newTask.ActiveText = activeText;
        
        var logPerformersIds = string.Join(", ", performerIds);
        var logAttachmentsIds = string.Join(", ", attachmentIds);
        Logger.DebugFormat("Notice prepared to start with parameters: Type '{0}', Id '{1}', subject length {2}, performers Ids '{3}', attachments {4}, active text length {5}",
                           docTypeName, docId, subject.Length, logPerformersIds, logAttachmentsIds, activeText.Length);
        
        var users = new List<Sungero.CoreEntities.IUser>() { newTask.Author,  newTask.StartedBy };
        
        foreach (var user in users)
        {
          if (user == null)
          {
            Logger.Debug("User is null");
            continue;
          }
          
          Logger.DebugFormat("Access rights check to change the outbox for user with id {0}", user.Id);
          
          var outbox = Sungero.Workflow.SpecialFolders.GetOutbox(user);
          if (outbox != null)
            Logger.DebugFormat("Outbox for user with id {0} exists", user.Id);
          
          if (outbox.AccessRights.CanChangeFolderContent())
            Logger.DebugFormat("User with id {0} has access rights to change his outbox", user.Id);
        }
        
        newTask.Start();
        Logger.DebugFormat("Notice with Id '{0}' has been started", newTask.Id);
        
        PublicFunctions.Module.AddTaskToExpiringTable(notificationParams.ExpiringDocTableName, docId, newTask.Id);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("{0} notification failed.", ex, documentLogView);
      }
    }
    
    #endregion
    
    #region Отчет эл. обмена
    
    /// <summary>
    /// Получить данные для формирования отчета Отчет эл. обмена.
    /// </summary>
    /// <param name="reportSessionId">Ид сессии отчета.</param>
    /// <param name="sentDocument">Документ.</param>
    /// <returns>Структура с данными для формирования отчета.</returns>
    [Remote]
    public virtual Sungero.Docflow.Structures.ExchangeOrderReport.ExchangeOrderFullData GetExchangeOrderInfo(string reportSessionId, IOfficialDocument sentDocument)
    {
      var documentInfo = Sungero.Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetLastDocumentInfo(sentDocument);
      
      var dataTable = new List<Structures.ExchangeOrderReport.ExchangeOrderInfo>();
      
      var hasReject = false;
      var hasCancellation = false;
      
      var signature = Signatures.Get(documentInfo.Document).SingleOrDefault(x => x.Id == documentInfo.SenderSignId);
      
      dataTable.Add(this.FillTableRow(documentInfo.Document.Name,
                                      null,
                                      documentInfo.MessageType == MessageType.Incoming ? ReportResources.ExchangeOrderReport.TitleAccepted : ReportResources.ExchangeOrderReport.TitleSended,
                                      signature.GetDataSignature(), null));
      
      var documents = documentInfo.ServiceDocuments.Where(x => x.Date != null)
        .OrderByDescending(x => x.DocumentType == ExchDocumentType.IConfirmation)
        .ThenByDescending(x => x.DocumentType == ExchDocumentType.ICReceipt)
        .ThenByDescending(x => x.DocumentType == ExchDocumentType.IReceipt || x.DocumentType == ExchDocumentType.Receipt)
        .ThenByDescending(x => x.DocumentType == ExchDocumentType.IRConfirmation)
        .ThenByDescending(x => x.DocumentType == ExchDocumentType.IRCReceipt)
        .ThenByDescending(x => x.DocumentType == ExchDocumentType.Annulment || x.DocumentType == ExchDocumentType.Cancellation)
        .ThenByDescending(x => x.DocumentType == ExchDocumentType.Reject || x.DocumentType == ExchDocumentType.IReject)
        .ThenByDescending(x => x.DocumentType == ExchDocumentType.IRReceipt)
        .ToList();
      Sungero.Exchange.IExchangeDocumentInfoServiceDocuments annulment = null;
      var buyerTitle = this.GetBuyerTitle(documentInfo);
      Sungero.Exchange.IExchangeDocumentInfoServiceDocuments reject = null;
      foreach (var document in documents)
      {
        if (Equals(document.DocumentType, ExchDocumentType.Reject) || Equals(document.DocumentType, ExchDocumentType.IReject))
          hasReject = true;
        if (Equals(document.DocumentType, ExchDocumentType.Reject))
          hasCancellation = true;
        if (Equals(document.DocumentType, ExchDocumentType.Annulment) || Equals(document.DocumentType, ExchDocumentType.Cancellation))
          annulment = document;
        else if (buyerTitle != null && buyerTitle.Date != null && (Equals(document.DocumentType, ExchDocumentType.Reject) || Equals(document.DocumentType, ExchDocumentType.IReject)))
          reject = document;
        else
        {
          dataTable.Add(this.FillTableRow(document.Info.Properties.DocumentType.GetLocalizedValue(document.DocumentType).ToLower(),
                                          document.DocumentType,
                                          this.GetMessageType(document.DocumentType.Value, documentInfo.MessageType.Value),
                                          document.Sign,
                                          document.Date));
        }
      }
      
      if (buyerTitle != null && (!hasCancellation || (annulment != null && hasReject)))
        dataTable.Add(buyerTitle);
      
      if (annulment != null)
      {
        dataTable.Add(this.FillTableRow(annulment.Info.Properties.DocumentType.GetLocalizedValue(annulment.DocumentType).ToLower(),
                                        annulment.DocumentType,
                                        this.GetMessageType(annulment.DocumentType.Value, documentInfo.MessageType.Value),
                                        annulment.Sign,
                                        annulment.Date));
        if (annulment.SecondSign != null)
          dataTable.Add(this.FillTableRow(annulment.Info.Properties.DocumentType.GetLocalizedValue(annulment.DocumentType).ToLower(),
                                          annulment.DocumentType,
                                          ReportResources.ExchangeOrderReport.AnnulmentSigned,
                                          annulment.SecondSign,
                                          annulment.Date));
        else if (Equals(annulment.DocumentType, ExchDocumentType.Annulment) && !hasReject)
        {
          var annulmentRow = Structures.ExchangeOrderReport.ExchangeOrderInfo.Create();
          annulmentRow.MessageType = ReportResources.ExchangeOrderReport.AnnulmentWait;
          annulmentRow.DocumentName = ReportResources.ExchangeOrderReport.AnnulmentSign;
          dataTable.Add(annulmentRow);
        }
        
        // Не отображать отказ на предыдущее аннулирование.
        if (reject != null && documents.Any(x => Equals(x.DocumentId, reject.ParentDocumentId) && Equals(x.DocumentType, ExchDocumentType.Annulment) && !Equals(x, annulment)))
          reject = null;
      }

      if (reject != null)
        dataTable.Add(this.FillTableRow(reject.Info.Properties.DocumentType.GetLocalizedValue(reject.DocumentType).ToLower(),
                                        reject.DocumentType,
                                        this.GetMessageType(reject.DocumentType.Value, documentInfo.MessageType.Value),
                                        reject.Sign,
                                        reject.Date));
      
      foreach (var row in dataTable)
        row.ReportSessionId = reportSessionId;

      var hasReceiptNotifications = !Exchange.PublicFunctions.Module.GetDocumentInfosWithoutReceiptNotification(documentInfo.RootBox, false)
        .Contains(documentInfo);

      var hasSignOrAnnulment = documentInfo.ReceiverSignId.HasValue || hasReject || (buyerTitle != null && buyerTitle.Date != null) ||
        annulment != null && Equals(annulment.DocumentType, ExchDocumentType.Cancellation);
      
      var exchangeData = Sungero.Docflow.Structures.ExchangeOrderReport.ExchangeOrderFullData.Create();
      
      var accounting = AccountingDocumentBases.As(documentInfo.Document);
      var isUTD = false;
      if (accounting != null && accounting.IsFormalized == true)
      {
        isUTD = accounting.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Schf ||
          accounting.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.SchfDop ||
          accounting.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Dop;
      }

      var exchDocumentTypeReceipt = isUTD ? ExchDocumentType.IReceipt : ExchDocumentType.Receipt;
      var isReceipt = documentInfo.NeedSign != true &&
        (documentInfo.RootBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Synerdocs &&
         documentInfo.ServiceDocuments.Any(x => x.DocumentType == exchDocumentTypeReceipt) ||
         documentInfo.RootBox.ExchangeService.ExchangeProvider == ExchangeCore.ExchangeService.ExchangeProvider.Diadoc &&
         (isUTD && documentInfo.ServiceDocuments.Any(x => x.DocumentType == ExchDocumentType.IReceipt) || !isUTD));
      
      exchangeData.IsReceiptNotifications = hasReceiptNotifications;
      exchangeData.IsSignOrAnnulment = hasSignOrAnnulment;
      exchangeData.IsReceipt = isReceipt;
      exchangeData.IsComplete = hasReceiptNotifications && (hasSignOrAnnulment || isReceipt);
      
      exchangeData.ExchangeOrderInfo = dataTable;

      return exchangeData;
    }
    
    /// <summary>
    /// Получение титула покупателя.
    /// </summary>
    /// <param name="info">Информация о документе.</param>
    /// <returns>Информацию о документе в виде информации о документе.</returns>
    public Structures.ExchangeOrderReport.ExchangeOrderInfo GetBuyerTitle(Exchange.IExchangeDocumentInfo info)
    {
      var result = Structures.ExchangeOrderReport.ExchangeOrderInfo.Create();
      var accounting = AccountingDocumentBases.As(info.Document);
      var doesNotNeedSignSentDocuments = info.NeedSign != true;
      if (accounting != null && accounting.IsFormalized == true)
      {
        var isTaxInvoice = accounting.FormalizedFunction == Docflow.AccountingDocumentBase.FormalizedFunction.Schf;
        doesNotNeedSignSentDocuments = info.NeedSign == true ? isTaxInvoice : true;
      }
      
      if (doesNotNeedSignSentDocuments && (accounting == null || accounting.IsFormalized != true || accounting.BuyerTitleId == null))
        return null;
      
      var isIncoming = info.MessageType == MessageType.Incoming;
      
      Sungero.Domain.Shared.ISignature sign;
      if (accounting != null && accounting.IsFormalized == true)
      {
        result.DocumentName = Sungero.Docflow.Reports.Resources.ExchangeOrderReport.BuyerTitle;
        sign = Signatures.Get(accounting.Versions.SingleOrDefault(x => x.Id == accounting.BuyerTitleId)).SingleOrDefault(x => x.Id == accounting.BuyerSignatureId);
        if (accounting.FormalizedServiceType != Sungero.Docflow.AccountingDocumentBase.FormalizedServiceType.Invoice && (accounting.BuyerTitleId == null || sign == null))
        {
          result.MessageType = isIncoming ? ReportResources.ExchangeOrderReport.TitleNotSended :
            ReportResources.ExchangeOrderReport.TitleNotAccepted;
          return result;
        }
        result.MessageType = isIncoming ? ReportResources.ExchangeOrderReport.TitleSended :
          ReportResources.ExchangeOrderReport.TitleAccepted;
      }
      else
      {
        result.DocumentName = ReportResources.ExchangeOrderReport.Signature;
        sign = Signatures.Get(info.Document).SingleOrDefault(x => x.Id == info.ReceiverSignId);
        if (!info.ReceiverSignId.HasValue || sign == null)
        {
          result.MessageType = isIncoming ? ReportResources.ExchangeOrderReport.SignatureNotSended : ReportResources.ExchangeOrderReport.SignatureNotAccepted;
          return result;
        }
        result.MessageType = isIncoming ? ReportResources.ExchangeOrderReport.SignatureSended : ReportResources.ExchangeOrderReport.SignatureAccepted;
      }
      
      if (sign == null)
        return result;
      
      var subject = Docflow.PublicFunctions.Module.GetSignatureCertificate(sign.GetDataSignature()).SubjectName.Format(true);
      var parsedSubject = Functions.ApprovalReviewAssignment.ParseSignatureSubject(subject);
      result.SendedFrom = this.SendedFrom(parsedSubject.OrganizationName, Sungero.Docflow.Server.ModuleFunctions.GetCertificateOwnerShortName(parsedSubject));
      result.Date = DateFormat(sign.SigningDate);
      
      return result;
    }
    
    /// <summary>
    /// Получение вида входящее/исходящее для ИОПов.
    /// </summary>
    /// <param name="documentType">Вид документа.</param>
    /// <param name="messageType">Вид сообщения для документа.</param>
    /// <returns>Вид сообщения для ИОП.</returns>
    private string GetMessageType(Sungero.Core.Enumeration documentType, Sungero.Core.Enumeration messageType)
    {
      if (Equals(documentType, ExchDocumentType.IConfirmation) || Equals(documentType, ExchDocumentType.IRConfirmation) ||
          (Equals(documentType, ExchDocumentType.IReceipt) && Equals(messageType, MessageType.Outgoing)) ||
          (Equals(documentType, ExchDocumentType.IReject) && Equals(messageType, MessageType.Outgoing)) ||
          (Equals(documentType, ExchDocumentType.IRReceipt) && Equals(messageType, MessageType.Incoming)) ||
          (Equals(documentType, ExchDocumentType.Receipt) && Equals(messageType, MessageType.Outgoing)) ||
          (Equals(documentType, ExchDocumentType.Reject) && Equals(messageType, MessageType.Outgoing)))
        return ReportResources.ExchangeOrderReport.MessageAccepted;
      if (Equals(documentType, ExchDocumentType.Annulment) || Equals(documentType, ExchDocumentType.Cancellation))
        return ReportResources.ExchangeOrderReport.MessageCreated;
      return ReportResources.ExchangeOrderReport.MessageSended;
    }
    
    /// <summary>
    /// Заполнение строки для временной таблицы отчета.
    /// </summary>
    /// <param name="documentName">Наименование документа.</param>
    /// <param name="documentType">Тип ИОПа.</param>
    /// <param name="messageType">Вид сообщения.</param>
    /// <param name="sign">Подпись.</param>
    /// <param name="date">Дата.</param>
    /// <returns>Структуру с заполненными данными строки отчета.</returns>
    private Structures.ExchangeOrderReport.ExchangeOrderInfo FillTableRow(string documentName, Enumeration? documentType,
                                                                          string messageType, byte[] sign, DateTime? date)
    {
      var subject = Docflow.PublicFunctions.Module.GetSignatureCertificate(sign).SubjectName.Format(true);
      var parsedSubject = Functions.ApprovalReviewAssignment.ParseSignatureSubject(subject);
      var row = new Structures.ExchangeOrderReport.ExchangeOrderInfo();
      if (Equals(documentType, ExchDocumentType.IConfirmation) || Equals(documentType, ExchDocumentType.IRConfirmation))
        row.SendedFrom = this.SendedFrom(ReportResources.ExchangeOrderReport.Operator, Sungero.Docflow.Server.ModuleFunctions.GetCertificateOwnerShortName(parsedSubject));
      else
        row.SendedFrom = this.SendedFrom(parsedSubject.OrganizationName, Sungero.Docflow.Server.ModuleFunctions.GetCertificateOwnerShortName(parsedSubject));
      var signatureInfo = ExternalSignatures.GetSignatureInfo(sign);
      if (signatureInfo.SignatureFormat == SignatureFormat.Hash)
        throw AppliedCodeException.Create(Resources.IncorrectSignatureFormat);
      var cadesBesSignatureInfo = signatureInfo.AsCadesBesSignatureInfo();
      var signDate = cadesBesSignatureInfo.SignDate;
      row.Date = signDate != null ? DateFormat(signDate) : DateFormat(date);
      row.DocumentName = documentName;
      row.MessageType = messageType;
      return row;
    }
    
    /// <summary>
    /// Формирование строки отправителя.
    /// </summary>
    /// <param name="organizationName">Название организации.</param>
    /// <param name="signedName">ФИО подписавшего.</param>
    /// <returns>Отправитель документа.</returns>
    private string SendedFrom(string organizationName, string signedName)
    {
      if (string.IsNullOrWhiteSpace(organizationName))
        return string.Empty;
      return string.Format("{0} <b>{1}</b>, {2} {3}", Sungero.Docflow.Reports.Resources.ExchangeOrderReport.SendedBy,
                           organizationName, Sungero.Docflow.Reports.Resources.ExchangeOrderReport.SignedBy, signedName);
    }
    
    private static string DateFormat(DateTime? datetime)
    {
      if (datetime == null)
        return null;
      
      return Functions.Module.ToTenantTime(datetime.Value).ToUserTime().ToString("g");
    }
    
    #endregion
    
    #region Интеллектуальная обработка

    /// <summary>
    /// Получить приложение-обработчик по имени файла.
    /// </summary>
    /// <param name="fileName">Имя или путь до файла.</param>
    /// <returns>Приложение-обработчик.</returns>
    [Public]
    public virtual Sungero.Content.IAssociatedApplication GetAssociatedApplicationByFileName(string fileName)
    {
      var app = Sungero.Content.AssociatedApplications.Null;
      var ext = System.IO.Path.GetExtension(fileName).TrimStart('.').ToLower();
      app = Content.AssociatedApplications.GetByExtension(ext);

      // Взять приложение-обработчик unknown, если не смогли подобрать по расширению.
      if (app == null)
        app = Sungero.Content.AssociatedApplications.GetAll()
          .SingleOrDefault(x => x.Sid == Sungero.Docflow.PublicConstants.Module.UnknownAppSid);

      return app;
    }

    #endregion
    
    #region Номенклатура дел
    
    /// <summary>
    /// Фильтрация дел для документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="query">Исходные дела для документа.</param>
    /// <returns>Отфильтрованные дела для документа.</returns>
    [Public]
    public virtual IQueryable<ICaseFile> CaseFileFiltering(IOfficialDocument document, IQueryable<ICaseFile> query)
    {
      return Functions.OfficialDocument.CaseFileFiltering(document, query);
    }
    
    /// <summary>
    /// Отправить уведомление о результатах копирования номенклатуры дел.
    /// </summary>
    /// <param name="userId">ID пользователя, инициировавшего копирование.</param>
    /// <param name="activeText">Текст уведомления.</param>
    public virtual void SendCopyCaseFilesNotification(int userId,
                                                      string activeText)
    {
      
      try
      {
        SendStandardNotice(Resources.CheckCaseFileCopyingResult, userId, activeText, null, null);
        Logger.DebugFormat("CopyCaseFiles. Done.");
      }
      catch (Exception ex)
      {
        Logger.Error("CopyCaseFiles. SendCopyCaseFilesNotification failed.", ex);
      }
    }
    
    #endregion
    
    /// <summary>
    /// Данные для отчета полномочий сотрудника из модуля Документооборот.
    /// </summary>
    /// <param name="employee">Сотрудник для обработки.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public virtual List<Company.Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine> GetResponsibilitiesReportData(IEmployee employee)
    {
      // HACK: Получаем отображаемое имя модуля.
      var moduleGuid = new DocflowModule().Id;
      var moduleName = Sungero.Metadata.Services.MetadataSearcher.FindModuleMetadata(moduleGuid).GetDisplayName();
      var result = new List<Company.Structures.ResponsibilitiesReport.ResponsibilitiesReportTableLine>();
      var modulePriority = Company.PublicConstants.ResponsibilitiesReport.DocflowPriority;
      
      // Ответственный за группы регистрации.
      if (RegistrationGroups.AccessRights.CanRead())
      {
        var registrationGroups = RegistrationGroups.GetAll()
          .Where(r => r.ResponsibleEmployee.Equals(employee))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        result = Company.PublicFunctions.Module.AppendResponsibilitiesReportResult(result, registrationGroups, moduleName, modulePriority,
                                                                                   Resources.PersonResponsibleForRegistrationGroups, null);
      }
      
      // Участник групп регистрации.
      if (RegistrationGroups.AccessRights.CanRead())
      {
        var registrationGroups = RegistrationGroups.GetAll()
          .Where(r => r.RecipientLinks.Any(l => l.Member.Equals(employee)))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        result = Company.PublicFunctions.Module.AppendResponsibilitiesReportResult(result, registrationGroups, moduleName, modulePriority,
                                                                                   Resources.RegistrationGroupMember, null);
      }
      
      // Этапы согласования.
      if (ApprovalStages.AccessRights.CanRead())
      {
        var approvalStages = ApprovalStages.GetAll()
          .Where(stage => stage.Status == Docflow.ApprovalStage.Status.Active)
          .Where(stage => Equals(stage.Assignee, employee) ||
                 stage.Recipients.Any(r => Equals(r.Recipient, employee)));
        result = Company.PublicFunctions.Module.AppendResponsibilitiesReportResult(result, approvalStages, moduleName, modulePriority,
                                                                                   Resources.ApprovalStages, null);
      }
      
      // Правила согласования.
      if (ApprovalRoleBases.AccessRights.CanRead() &&
          ConditionBases.AccessRights.CanRead())
      {
        var approvalRules = ApprovalRuleBases.GetAll()
          .Where(rule => rule.Conditions.Any(x => (x.Condition.ConditionType == Docflow.Condition.ConditionType.Addressee &&
                                                   Docflow.Conditions.As(x.Condition).Addressees.Any(addr => Equals(addr.Addressee, employee))) ||
                                             (x.Condition.ConditionType == Docflow.ConditionBase.ConditionType.RoleEmpComparer &&
                                              Equals(x.Condition.RecipientForComparison, employee))))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        result = Company.PublicFunctions.Module.AppendResponsibilitiesReportResult(result, approvalRules, moduleName, modulePriority,
                                                                                   Resources.ApprovalRules, null);
      }
      
      // Право подписи.
      if (SignatureSettings.AccessRights.CanRead())
      {
        var signatureSettings = SignatureSettings.GetAll()
          .Where(r => r.Recipient.Equals(employee))
          .Where(r => r.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
          .Where(r => r.DocumentFlow != null)
          .Where(r => !r.ValidTill.HasValue || r.ValidTill.Value >= Calendar.UserToday)
          .ToDictionary<IEntity, IEntity, string>(x => x, x => CreateSignatureSettingsPresentation(SignatureSettings.As(x)));
        result = Company.PublicFunctions.Module.AppendResponsibilitiesReportResult(result, signatureSettings, moduleName, modulePriority + result.Count,
                                                                                   Resources.SignatureSetting, null);
      }
      
      // Правила назначения прав.
      if (AccessRightsRules.AccessRights.CanRead())
      {
        var accessRightsRules = AccessRightsRules.GetAll()
          .Where(r => r.Members.Any(l => l.Recipient.Equals(employee)))
          .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
        result = Company.PublicFunctions.Module.AppendResponsibilitiesReportResult(result, accessRightsRules, moduleName, modulePriority,
                                                                                   Resources.RuleToGrantAccess, null);
      }
      
      return result;
    }
    
    /// <summary>
    /// Сформировать представление настроек подписи для отчета о полномочиях.
    /// </summary>
    /// <param name="setting">Запись справочника настроек подписания.</param>
    /// <returns>Представление для отчета.</returns>
    public static string CreateSignatureSettingsPresentation(ISignatureSetting setting)
    {
      var flow = string.Format("{0}: {1}", Docflow.Resources.Direction,
                               setting.Info.Properties.DocumentFlow.GetLocalizedValue(setting.DocumentFlow).ToLower());
      var reason = string.Format("{0}: {1}",
                                 Resources.Reason,
                                 setting.Info.Properties.Reason.GetLocalizedValue(setting.Reason).ToLower());
      
      var businessUnits = string.Empty;
      if (setting.BusinessUnits.Count > 0)
      {
        businessUnits = string.Format("{0}:", setting.Info.Properties.BusinessUnits.LocalizedName);
        foreach (var bu in setting.BusinessUnits)
        {
          businessUnits = string.Format("{0} {1},", businessUnits, bu.BusinessUnit.Name);
        }
        businessUnits = businessUnits.Substring(0, businessUnits.Length - 1);
        businessUnits = string.Format("{0}{1}", businessUnits, Environment.NewLine);
      }
      
      var startDate = setting.ValidFrom.HasValue ? string.Format("{0} {1}", Company.Resources.From, setting.ValidFrom.Value.ToShortDateString()) : string.Empty;
      var endDate = setting.ValidTill.HasValue ? string.Format("{0} {1}", Company.Resources.To, setting.ValidTill.Value.ToShortDateString()) : string.Empty;
      var period = string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate) ? Company.Resources.Permanently : string.Format("{0} {1}", startDate, endDate).Trim();
      period = string.Format("{0}: {1}", Company.Resources.Period, period);
      var result = string.Format("{0}{1}{2}{1}{3}{4}", flow, Environment.NewLine, reason, businessUnits, period);
      return result;
      
    }
    
    /// <summary>
    /// Получить external link.
    /// </summary>
    /// <param name="entityType">Тип справочника.</param>
    /// <param name="entityId">ИД экземпляра, созданного при инициализации.</param>
    /// <returns>External link.</returns>
    [Public]
    public static Sungero.Domain.Shared.IExternalLink GetExternalLink(Guid entityType, Guid entityId)
    {
      return Domain.ModuleFunctions.GetAllExternalLinks(l => l.EntityTypeGuid == entityType &&
                                                        l.ExternalEntityId == entityId.ToString() &&
                                                        l.ExternalSystemId == Constants.Module.InitializeExternalLinkSystem)
        .SingleOrDefault();
    }
    
    /// <summary>
    /// Создать external link.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <param name="entityId">ИД экземпляра, созданного при инициализации.</param>
    [Public]
    public static void CreateExternalLink(IEntity entity, Guid entityId)
    {
      var externalLink = Domain.ModuleFunctions.CreateExternalLink();
      externalLink.EntityTypeGuid = entity.GetEntityMetadata().NameGuid;
      externalLink.ExternalEntityId = entityId.ToString();
      externalLink.ExternalSystemId = Constants.Module.InitializeExternalLinkSystem;
      externalLink.EntityId = entity.Id;
      externalLink.IsDeleted = false;
      externalLink.Save();
    }
    
    /// <summary>
    /// Удалить сущность из кеша сессии.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <remarks>Нужно только для удаления сущностей, которые не надо сохранять, но они уже созданы.</remarks>
    [Public, Remote]
    public static void EvictEntityFromSession(IEntity entity)
    {
      if (entity == null)
        return;
      
      Logger.DebugFormat("Evict: try to evict entity {0} - {1}", entity.Id, entity.TypeDiscriminator);
      using (var session = new Domain.Session())
      {
        // HACK: вытаскиваем внутреннюю сессию для удаления измененной сущности из кеша.
        var innerSession = (Sungero.Domain.ISession)session
          .GetType()
          .GetField("InnerSession", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          .GetValue(session);
        innerSession.Evict(entity);
      }
    }
    
    /// <summary>
    /// Запустить фоновый процесс "Документооборот. Автоматическое назначение прав на документы".
    /// </summary>
    [Public, Remote]
    public static void RequeueGrantAccessRightsToDocuments()
    {
      Jobs.GrantAccessRightsToDocuments.Enqueue();
    }
    
    /// <summary>
    /// Создать задачу по процессу "Свободное согласование документа".
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Задача по процессу "Свободное согласование документа".</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IFreeApprovalTask CreateFreeApprovalTask(Sungero.Content.IElectronicDocument document)
    {
      var task = FreeApprovalTasks.Create();
      task.ForApprovalGroup.All.Add(document);
      if (task.Subject.Length > task.Info.Properties.Subject.Length)
        task.Subject = task.Subject.Substring(0, task.Info.Properties.Subject.Length);
      
      return task;
    }
    
    /// <summary>
    /// Получить созданные задачи на согласование по регламенту для документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Список созданных задач по документу.</returns>
    [Public, Remote]
    public IQueryable<Sungero.Docflow.IApprovalTask> GetApprovalTasks(IOfficialDocument document)
    {
      var docGuid = document.GetEntityMetadata().GetOriginal().NameGuid;
      var approvalTaskDocumentGroupGuid = Constants.Module.TaskMainGroup.ApprovalTask;
      return ApprovalTasks.GetAll()
        .Where(t => t.Status == Workflow.Task.Status.InProcess ||
               t.Status == Workflow.Task.Status.Suspended)
        .Where(t => t.AttachmentDetails
               .Any(att => att.AttachmentId == document.Id && att.EntityTypeGuid == docGuid &&
                    att.GroupId == approvalTaskDocumentGroupGuid));
    }
    
    /// <summary>
    /// Получить созданные задачи на рассмотрение документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Список созданных задач на рассмотрение по документу.</returns>
    [Public, Remote]
    public IQueryable<Sungero.RecordManagement.IDocumentReviewTask> GetReviewTasks(IOfficialDocument document)
    {
      var docGuid = document.GetEntityMetadata().GetOriginal().NameGuid;
      var reviewTaskDocumentGroupGuid = Constants.Module.TaskMainGroup.DocumentReviewTask;
      return Sungero.RecordManagement.DocumentReviewTasks.GetAll()
        .Where(t => t.Status == Workflow.Task.Status.InProcess ||
               t.Status == Workflow.Task.Status.Suspended)
        .Where(t => t.AttachmentDetails
               .Any(att => att.AttachmentId == document.Id && att.EntityTypeGuid == docGuid &&
                    att.GroupId == reviewTaskDocumentGroupGuid));
    }
    
    /// <summary>
    /// Создать задачу по процессу "Согласование официального документа".
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Задача по процессу "Согласование официального документа".</returns>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IApprovalTask CreateApprovalTask(IOfficialDocument document)
    {
      var task = ApprovalTasks.Create();
      task.DocumentGroup.All.Add(document);
      
      return task;
    }
    
    /// <summary>
    /// Получить доступные ведущие документы.
    /// </summary>
    /// <returns>Документы.</returns>
    [Remote, Public]
    public virtual IQueryable<IOfficialDocument> GetAvaliableLeadingDocuments()
    {
      return Contracts.ContractualDocuments.GetAll();
    }
    
    /// <summary>
    /// Отправить уведомление.
    /// </summary>
    /// <param name="subject">Тема.</param>
    /// <param name="performerId">ID получателя.</param>
    /// <param name="activeText">Текст уведомления.</param>
    /// <param name="author">Автор.</param>
    /// <param name="threadSubject">Тема в переписке.</param>
    public static void SendStandardNotice(string subject, int performerId, string activeText, IUser author, string threadSubject)
    {
      var performer = Users.GetAll(x => x.Id == performerId).FirstOrDefault();
      if (performer == null)
      {
        Logger.DebugFormat("SendStandardNotice. User (Id {0}) not found.", performerId);
        return;
      }
      
      SendStandardNotice(subject, performer, activeText, author, threadSubject);
    }
    
    /// <summary>
    /// Отправить уведомление.
    /// </summary>
    /// <param name="subject">Тема.</param>
    /// <param name="performer">Получатель.</param>
    /// <param name="activeText">Текст уведомления.</param>
    /// <param name="author">Автор.</param>
    /// <param name="threadSubject">Тема в переписке.</param>
    public static void SendStandardNotice(string subject, IUser performer, string activeText, IUser author, string threadSubject)
    {
      var task = Sungero.Workflow.SimpleTasks.CreateWithNotices(subject, performer);
      
      if (!string.IsNullOrWhiteSpace(activeText))
        task.ActiveText = activeText;
      
      if (author != null)
        task.Author = author;
      
      if (!string.IsNullOrWhiteSpace(threadSubject))
        task.ThreadSubject = threadSubject;
      
      task.Start();
      
      Logger.DebugFormat("SendStandardNotice. Notification (Id {0}) sent. Performer (Id {1}).", task.Id, performer.Id);
    }
    
    /// <summary>
    /// Отправить уведомление подзадачей.
    /// </summary>
    /// <param name="subject">Тема.</param>
    /// <param name="performers">Получатели.</param>
    /// <param name="parentTask">Главная задача.</param>
    /// <param name="activeText">Текст уведомления.</param>
    /// <param name="author">Автор.</param>
    /// <param name="threadSubject">Тема в переписке.</param>
    [Remote, Public]
    public static void SendNoticesAsSubtask(string subject, List<IUser> performers, ITask parentTask, string activeText, IUser author, string threadSubject)
    {
      var recipients = Users.GetAll(u => performers.Contains(u)).ToArray();
      var task = SimpleTasks.CreateAsSubtask(parentTask);
      task.Subject = subject;
      task.ThreadSubject = threadSubject;
      task.NeedsReview = false;
      foreach (var recipient in recipients)
      {
        var routeStep = task.RouteSteps.AddNew();
        routeStep.AssignmentType = Workflow.SimpleTaskRouteSteps.AssignmentType.Notice;
        routeStep.Performer = recipient;
        routeStep.Deadline = null;
      }
      
      if (!string.IsNullOrWhiteSpace(activeText))
        task.ActiveText = activeText;
      
      if (author != null)
        task.Author = author;

      // TODO: удалить код после исправления бага 17930 (сейчас этот баг в TFS недоступен, он про автоматическое обрезание темы).
      if (task.Subject.Length > Tasks.Info.Properties.Subject.Length)
        task.Subject = task.Subject.Substring(0, Tasks.Info.Properties.Subject.Length);
      
      task.Start();
    }
    
    /// <summary>
    /// Выдать наблюдателям права на просмотр вложений.
    /// </summary>
    /// <param name="attachments">Вложения.</param>
    /// <param name="users">Наблюдатели.</param>
    [Public]
    public static void GrantReadRightsForAttachments(System.Collections.Generic.IEnumerable<IEntity> attachments,
                                                     System.Collections.Generic.IEnumerable<IRecipient> users)
    {
      foreach (var attachment in attachments.Where(a => a.Info.AccessRightsMode != Metadata.AccessRightsMode.Type))
      {
        foreach (var user in users)
        {
          if (AssignmentBases.Is(attachment))
            AssignmentBases.As(attachment).MainTask.AccessRights.Grant(user, DefaultAccessRightsTypes.Read);
          else if (Tasks.Is(attachment))
            Tasks.As(attachment).MainTask.AccessRights.Grant(user, DefaultAccessRightsTypes.Read);
          else
            attachment.AccessRights.Grant(user, DefaultAccessRightsTypes.Read);
        }
      }
    }
    
    /// <summary>
    /// Проверка доступности модуля по лицензии.
    /// </summary>
    /// <param name="moduleGuid">Id модуля.</param>
    /// <returns>True, если модуль доступен согласно лицензии.</returns>
    [Remote, Public]
    public static bool IsModuleAvailableByLicense(System.Guid moduleGuid)
    {
      return Sungero.Domain.Security.LicenseHelper.IsModuleAvailableByLicense(moduleGuid);
    }

    /// <summary>
    /// Проверка доступности модуля по лицензии для текущего пользователя.
    /// </summary>
    /// <param name="moduleGuid">Id модуля.</param>
    /// <returns>True, если модуль доступен согласно лицензии.</returns>
    [Remote, Public]
    public static bool IsModuleAvailableForCurrentUserByLicense(System.Guid moduleGuid)
    {
      return Sungero.Domain.Security.LicenseHelper.IsModuleValidForCurrentUserByLicense(moduleGuid);
    }
    
    /// <summary>
    /// Запуск серверной функции сущности вне зависимостей.
    /// </summary>
    /// <param name="entity">Сущность, функцию которой надо запустить.</param>
    /// <param name="name">Название функции. Только название, без типа возврата и параметров.</param>
    /// <param name="parameters">Массив с параметрами функции.</param>
    /// <returns>Нетипизированный результат выполнения.</returns>
    [Public]
    public static object GetServerEntityFunctionResult(IEntity entity, string name, List<object> parameters)
    {
      if (parameters == null)
        parameters = new List<object>();
      
      var entityFunctions = entity as Domain.Shared.IEntityFunctions;
      var functions = entityFunctions.FunctionsContainer.ServerFunctions;
      var method = functions.GetType()
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
        .SingleOrDefault(m => m.Name == name && m.GetParameters().Count() == parameters.Count);
      
      if (method != null)
        return method.Invoke(functions, parameters.ToArray());
      
      return null;
    }

    /// <summary>
    /// Выдача прав на вложения при ручной выдаче прав на задачу.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="attachments">Вложения.</param>
    [Public]
    public static void GrantManualReadRightForAttachments(ITask task, List<IEntity> attachments)
    {
      if (task == null || !attachments.Any())
        return;
      
      foreach (var accessRight in task.AccessRights.Current.Where(x => x.AccessRightsType != DefaultAccessRightsTypes.Forbidden))
      {
        foreach (var attach in attachments.Where(a => a.Info.AccessRightsMode == Metadata.AccessRightsMode.Both || a.Info.AccessRightsMode == Metadata.AccessRightsMode.Instance))
        {
          // TODO Zamerov нужен нормальный признак IsDeleted, 50908
          var isDeleted = (attach as Sungero.Domain.Shared.IChangeTracking).ChangeTracker.IsDeleted;
          if (!isDeleted && attach.AccessRights.CanManage() && !attach.AccessRights.IsGranted(DefaultAccessRightsTypes.Read, accessRight.Recipient))
            attach.AccessRights.Grant(accessRight.Recipient, DefaultAccessRightsTypes.Read);
        }
      }
    }
    
    /// <summary>
    /// Отсортировать сотрудников, которые могут быть авторами резолюции для данного пользователя по выбранному документу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Список пользователей.</returns>
    [Public, Remote]
    public static List<IEmployee> UsersCanBeResolutionAuthor(IOfficialDocument document)
    {
      var assistedManagers = ManagersAssistants.GetAll()
        .Where(m => m.Status == CoreEntities.DatabookEntry.Status.Active &&
               Recipients.AllRecipientIds.Contains(m.Assistant.Id))
        .Select(m => m.Manager)
        .ToList();
      var substitutedEmployees = Substitutions.ActiveSubstitutedUsers.Where(asu => Employees.Is(asu))
        .Select(asu => Employees.As(asu))
        .ToList();
      var result = new List<IEmployee>();
      result.AddRange(assistedManagers);
      result.AddRange(substitutedEmployees);
      if (Employees.Current != null)
        result.Add(Employees.Current);
      
      if (document != null)
      {
        var docGuid = document.GetEntityMetadata().GetOriginal().NameGuid;
        var assignments = Assignments.GetAll(x => x.Task.AttachmentDetails.Any(att => att.AttachmentId == document.Id && att.EntityTypeGuid == docGuid) &&
                                             (x.Result == Docflow.ApprovalReviewAssignment.Result.AddResolution ||
                                              x.Result == RecordManagement.ReviewManagerAssignment.Result.AddResolution));
        foreach (var assignment in assignments)
        {
          var completedBy = Employees.As(assignment.CompletedBy);
          if (completedBy != null)
            result.Add(completedBy);
        }
      }
      
      return result.Distinct().ToList();
    }

    /// <summary>
    /// Проверка, может ли сотрудник быть автором резолюции по выбранному документу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>True - может быть автором резолюции, false - нет.</returns>
    [Public, Remote]
    public static bool IsUsersCanBeResolutionAuthor(IOfficialDocument document, IEmployee employee)
    {
      return Functions.Module.UsersCanBeResolutionAuthor(document).Contains(employee);
    }
    
    /// <summary>
    /// Фильтрация заданий для виджетов и Мониторинга.
    /// </summary>
    /// <param name="assignments">Задания, которые нужно отфильтровать.</param>
    /// <param name="periodBegin">Начало периода.</param>
    /// <param name="periodEnd">Конец периода.</param>
    /// <param name="mode">Режим фильтрации.</param>
    /// <returns>Список заданий.</returns>
    [Public]
    public static IQueryable<Sungero.Workflow.IAssignment> FilterAssignments(IQueryable<IAssignment> assignments, DateTime periodBegin, DateTime periodEnd, string mode)
    {
      assignments = assignments.Where(a => Employees.Is(a.Performer))
        .Where(a => a.Created < periodEnd);
      
      if (mode == Constants.Module.FilterAssignmentsMode.Created)
        assignments = assignments
          .Where(a => a.Created >= periodBegin)
          .Where(a => (a.Status == Workflow.AssignmentBase.Status.Completed || a.Status == Workflow.AssignmentBase.Status.InProcess));
      else if (mode == Constants.Module.FilterAssignmentsMode.Completed)
        assignments = assignments
          .Where(a => (a.Status == Workflow.AssignmentBase.Status.Completed))
          .Where(a => (a.Completed >= periodBegin && a.Completed < periodEnd));
      else if (mode == Constants.Module.FilterAssignmentsMode.Modified)
        assignments = assignments.Where(a => a.Modified >= periodBegin && a.Modified <= periodEnd);
      else if (mode == Constants.Module.FilterAssignmentsMode.Default)
        assignments = assignments
          .Where(a => (a.Status == Workflow.AssignmentBase.Status.Completed && (a.Created >= periodBegin || a.Completed > periodBegin)) || a.Status == Workflow.AssignmentBase.Status.InProcess);
      
      return assignments;
    }
    
    /// <summary>
    /// Получить сериализованную модель для диаграмм.
    /// </summary>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="department">Подразделение.</param>
    /// <param name="periodBegin">Начало периода.</param>
    /// <param name="periodEnd">Конец периода.</param>
    /// <returns>Сериализованная в xml-строку модель.</returns>
    [Remote(IsPure = true), Public]
    public static List<string> GetChartModel(IBusinessUnit businessUnit, IDepartment department, DateTime periodBegin, DateTime periodEnd)
    {
      var model = new Charts.Model.Model();
      
      var currentEmployee = Employees.Current;
      model.IsManagerOrCEO = currentEmployee != null && (BusinessUnits.GetAll(b => Equals(b.CEO, currentEmployee)).Any() ||
                                                         Departments.GetAll(d => Equals(d.Manager, currentEmployee)).Any());
      
      model.IsAdministratorOrAdvisor = IsAdministratorOrAdvisor();
      var ids = Recipients.AllRecipientIds;
      
      AccessRights.AllowRead(
        () =>
        {
          var performers = Employees.GetAll().Where(empl => empl.IsSystem != true);
          
          if (businessUnit != null)
          {
            var unitPerformers = BusinessUnits.GetAllUsersInGroup(businessUnit).ToList();
            performers = performers.Where(a => unitPerformers.Contains(a));
          }
          
          if (!model.IsAdministratorOrAdvisor)
            performers = performers.Where(p => ids.Contains(p.Id));
          
          if (department != null)
            performers = performers.Where(p => Equals(p.Department, department));
          
          var allPerformers = performers.ToList();
          
          var allAssignments = FilterAssignments(Workflow.Assignments.GetAll(), periodBegin, periodEnd, Constants.Module.FilterAssignmentsMode.Default);
          
          // Дофильтровываем задания по сотрудникам, если не полная загрузка под админом.
          if (!(model.IsAdministratorOrAdvisor && businessUnit == null && department == null))
          {
            var allPerformersIds = allPerformers.Select(x => x.Id).ToList();
            allAssignments = allAssignments.Where(a => allPerformersIds.Contains(a.Performer.Id));
          }
          
          model.Departments.AddRange(Departments.GetAll().Select(d => new Charts.Model.Department() { Id = d.Id,
                                                                   Name = d.Name,
                                                                   Active = d.Status == CoreEntities.DatabookEntry.Status.Active,
                                                                   BusinessUnitName = (d.BusinessUnit == null) ? string.Empty : d.BusinessUnit.Name
                                                                 }));
          
          model.Assignments.AddRange(allAssignments.Select(a => new Charts.Model.Assignment() { Id = a.Id, Name = a.Subject, Deadline = a.Deadline,
                                                             Completed = a.Completed, Created = a.Created.Value,
                                                             InWork = a.Status == Workflow.AssignmentBase.Status.InProcess,
                                                             PerformerId = a.Performer.Id,
                                                             TaskId = a.Task.Id,
                                                             TaskTypeGuid = a.Task.TypeDiscriminator.ToString(),
                                                           }));
          
          // Фильтруем по закрытым сотрудникам без заданий.
          var performerWithAsg = model.Assignments.Select(a => a.PerformerId).Distinct().ToList();
          allPerformers = allPerformers.Where(p => !Equals(p.Status, Sungero.CoreEntities.DatabookEntry.Status.Closed) || performerWithAsg.Contains(p.Id)).ToList();
          
          model.Performers.AddRange(allPerformers.Select(p => new Charts.Model.Performer()
                                                         {
                                                           Id = p.Id,
                                                           Name = p.Person != null ? Sungero.Company.PublicFunctions.Employee.GetShortName(p, false) : p.Name,
                                                           DepartmentName = p.Department != null ? p.Department.Name : string.Empty,
                                                           DepartmentId = p.Department != null ? p.Department.Id : 0,
                                                           JobTitle = p.JobTitle != null ? p.JobTitle.Name : string.Empty,
                                                           Active = p.Status == CoreEntities.DatabookEntry.Status.Active
                                                         }));
        });

      return new List<string> { Sungero.Charts.Serialization.Serializable.PackToXml(model) };
    }
    
    /// <summary>
    /// Получить строковое представление даты со временем, день без времени вернет дату.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <returns>Строковое представление даты.</returns>
    [Public]
    public static string ToShortDateShortTime(DateTime date)
    {
      return date == date.Date ? date.ToString("d") : date.ToString("g");
    }
    
    /// <summary>
    /// Получить тенантское время из клиентской даты без времени.
    /// </summary>
    /// <param name="date">Пользовательская дата без времени.</param>
    /// <returns>Тенантская дата и время.</returns>
    [Public, Remote(IsPure = true)]
    public static DateTime GetTenantDateTimeFromUserDay(DateTime date)
    {
      return date.AddMilliseconds(1).FromUserTime().AddMilliseconds(-1);
    }
    
    /// <summary>
    /// Получить списки рассылки.
    /// </summary>
    /// <returns>Списки рассылки.</returns>
    [Remote(IsPure = true)]
    public IQueryable<IDistributionList> GetDistributionLists()
    {
      return DistributionLists.GetAll().Where(a => a.Status == Sungero.RecordManagement.AcquaintanceList.Status.Active);
    }
    
    /// <summary>
    /// Создать список рассылки.
    /// </summary>
    /// <returns>Список рассылки.</returns>
    [Remote]
    public IDistributionList CreateDistributionList()
    {
      return DistributionLists.Create();
    }
    
    /// <summary>
    /// Проверить наличие документов в группе вложений, на которые не хватило прав.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="groupId">ИД группы вложений.</param>
    /// <returns>Текст ошибки, если не хватает прав.</returns>
    [Remote(IsPure = true), Public]
    public static string GetTaskAbortingError(ITask task, string groupId)
    {
      var taskForMetadata = task;
      var groupDecorator = new Sungero.Workflow.Shared.WorkflowEntityAttachmentGroupDecorator((Sungero.Workflow.Interfaces.IWorkflowEntity)task, groupId);
      var attachmentGroup = groupDecorator.All;
      var userIds = attachmentGroup.Select(g => g.Id).ToList();
      var auditorIds = new List<int>();
      do
      {
        auditorIds.AddRange(task.AttachmentDetails.Where(d => d.GroupId == Guid.Parse(groupId)).Select(a => a.AttachmentId.Value));
        if (task.ParentTask != null)
          task = task.ParentTask;
        else if (task.ParentAssignment != null)
          task = task.ParentAssignment.Task;
        else
          task = null;
      }
      while (task != null);
      auditorIds = auditorIds.Distinct().ToList();
      
      if (!auditorIds.Except(userIds).Any())
        return string.Empty;
      
      var metadata = ((Sungero.Metadata.WorkflowEntityMetadata)taskForMetadata.GetEntityMetadata())
        .AttachmentGroups.Single(g => g.NameGuid == Guid.Parse(groupId));
      return Docflow.Resources.CantAbortTaskWithoutDocumentAccessRightsFormat(metadata.Title);
    }
    
    /// <summary>
    /// Получить сотрудников по списку реципиентов с раскрытием групп и ролей.
    /// </summary>
    /// <param name="recipients">Реципиенты.</param>
    /// <returns>Раскрытый список реципиентов до сотрудников.</returns>
    [Public, Remote(IsPure = true)]
    public static List<IEmployee> GetEmployeesFromRecipientsRemote(List<IRecipient> recipients)
    {
      return GetEmployeesFromRecipients(recipients);
    }
    
    /// <summary>
    /// Получить сотрудников по списку реципиентов с раскрытием групп и ролей.
    /// </summary>
    /// <param name="recipients">Реципиенты.</param>
    /// <returns>Раскрытый список реципиентов до сотрудников.</returns>
    /// <remarks>Продублировано GetEmployeesFromRecipients без атрибута Remote,
    /// т.к. Remote в вебе отрабатывает с запаковкой/распаковкой, даже если вызывается с сервера. Это дополнительные накладные расходы.</remarks>
    [Public]
    public static List<IEmployee> GetEmployeesFromRecipients(List<IRecipient> recipients)
    {
      var performers = new List<IEmployee>();
      
      foreach (var recipient in recipients)
      {
        if (Employees.Is(recipient))
        {
          performers.Add(Employees.As(recipient));
        }
        else if (Groups.Is(recipient))
        {
          var groupRecipient = Groups.GetAllUsersInGroup(Groups.As(recipient))
            .Where(r => Employees.Is(r) && r.Status == CoreEntities.DatabookEntry.Status.Active)
            .Select(r => Employees.As(r));
          foreach (var employee in groupRecipient)
            performers.Add(employee);
        }
      }
      
      return performers;
    }
    
    /// <summary>
    /// Считать лицензию из базы.
    /// </summary>
    /// <returns>Словарь, фактически нужен только для восстановления через RestoreLicense.</returns>
    [Public]
    public static System.Collections.Generic.Dictionary<int, byte[]> ReadLicense()
    {
      var licenses = new Dictionary<int, byte[]>();
      
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = Queries.Module.ReadLicense;
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            var id = reader.GetInt32(0);
            var key = reader[1] as byte[];
            licenses.Add(id, key);
            Logger.DebugFormat("License: read license key with id = {0}", id);
          }
        }
      }
      return licenses;
    }
    
    /// <summary>
    /// Удалить лицензию из базы и обновить кеш лицензий.
    /// </summary>
    [Public]
    public static void DeleteLicense()
    {
      Logger.DebugFormat("License: All licenses deleted");
      
      var command = Queries.Module.DeleteLicense;
      ExecuteSQLCommand(command);
      
      // Перечитывает лицензию из базы и обновляет кеш.
      Sungero.Domain.ModuleFunctions.GetFullLicenseInfo();
    }
    
    /// <summary>
    /// Восстановить лицензию после удаления.
    /// </summary>
    /// <param name="licenses">Лицензия, считанная ранее.</param>
    [Public]
    public static void RestoreLicense(System.Collections.Generic.Dictionary<int, byte[]> licenses)
    {
      if (licenses != null && licenses.Any())
      {
        foreach (var license in licenses)
        {
          Logger.DebugFormat("License: license key with id = {0} restored", license.Key);
          using (var command = SQL.GetCurrentConnection().CreateCommand())
          {
            command.CommandText = Queries.Module.RestoreLicense;
            Docflow.PublicFunctions.Module.AddIntegerParameterToCommand(command, "@id", license.Key);
            
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@key";
            parameter.Direction = System.Data.ParameterDirection.Input;
            parameter.DbType = System.Data.DbType.Binary;
            parameter.Value = license.Value;
            command.Parameters.Add(parameter);
            command.ExecuteNonQuery();
          }
        }
        // Перечитывает лицензию из базы и обновляет кеш.
        Sungero.Domain.ModuleFunctions.GetFullLicenseInfo();
      }
    }
    
    /// <summary>
    /// Записать параметр в docflow_params.
    /// </summary>
    /// <param name="key">Ключ.</param>
    /// <param name="value">Значение.</param>
    [Public]
    public static void InsertOrUpdateDocflowParam(string key, string value)
    {
      Functions.Module.ExecuteSQLCommandFormat(Queries.Module.InsertOrUpdateDocflowParamsValue, new[] { key, value });
    }
    
    /// <summary>
    /// Получить документы по контрагенту.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <returns>Список документов по контрагенту.</returns>
    [Public, Remote(IsPure = true)]
    public IQueryable<ICounterpartyDocument> GetCounterpartyDocuments(Parties.ICounterparty counterparty)
    {
      return CounterpartyDocuments.GetAll()
        .Where(doc => Equals(doc.Counterparty, counterparty));
    }
    
    /// <summary>
    /// Получить сообщения валидации подписи в виде строки.
    /// </summary>
    /// <param name="signature">Подпись.</param>
    /// <param name="separator">Строковый разделитель.</param>
    /// <returns>Строка сообщений валидации подписи.</returns>
    [Public]
    public static string GetSignatureValidationErrorsAsString(Sungero.Domain.Shared.ISignature signature, string separator)
    {
      if (signature.IsValid)
        return string.Empty;
      else
      {
        var errors = new List<string>();
        var isInvalidCertificate = signature.ValidationErrors.Any(e => e.ErrorType == Sungero.Domain.Shared.SignatureValidationErrorType.Certificate);
        var isInvalidDoc = signature.ValidationErrors.Any(e => e.ErrorType == Sungero.Domain.Shared.SignatureValidationErrorType.Data);
        var isInvalidAttributes = signature.ValidationErrors.Any(e => e.ErrorType == Sungero.Domain.Shared.SignatureValidationErrorType.Signature);
        if (isInvalidCertificate)
          errors.Add(Docflow.Resources.StateViewCertificateIsNotValid);
        if (isInvalidDoc)
          errors.Add(Docflow.Resources.StateViewDocumentIsChanged);
        if (isInvalidAttributes)
        {
          errors.Add(Docflow.Resources.StateViewSignatureAttributesNotValid);
          if (!isInvalidDoc)
            errors.Add(Docflow.Resources.StateViewDocumentIsValid);
        }
        
        return string.Join(separator, errors);
      }
    }
    
    /// <summary>
    /// Получить автора резолюции из задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Автор резолюции.</returns>
    [Public, Remote]
    public static IEmployee GetResolutionAuthor(ITask task)
    {
      var assignment = Assignments.GetAll(x => (x.Result == Docflow.ApprovalReviewAssignment.Result.AddResolution ||
                                                x.Result == RecordManagement.ReviewManagerAssignment.Result.AddResolution) &&
                                          Equals(x.Task, task)).OrderByDescending(x => x.Created).FirstOrDefault();
      var resolutionAuthor = Sungero.Company.Employees.Null;
      if (assignment != null)
        resolutionAuthor = Employees.As(assignment.CompletedBy);
      
      return resolutionAuthor;
    }
    
    /// <summary>
    /// Добавить исполнителя в задание согласования.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="newApprover">Новый согласующий.</param>
    [Public, Remote]
    public void AddApprover(IAssignment assignment, IEmployee newApprover)
    {
      this.AddApprover(assignment, newApprover, null);
    }

    /// <summary>
    /// Добавить исполнителя в задание согласования.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="newApprover">Новый согласующий.</param>
    /// <param name="deadline">Новый срок для задания.</param>
    [Public, Remote]
    public void AddApprover(IAssignment assignment, IEmployee newApprover, DateTime? deadline)
    {
      var operation = new Enumeration(Constants.ApprovalAssignment.AddApprover);
      assignment.Forward(newApprover, ForwardingLocation.Next, deadline);
      assignment.History.Write(operation, operation, Company.PublicFunctions.Employee.GetShortName(newApprover, false));
      
      var task = ApprovalTasks.As(assignment.Task);
      var approvalAsg = ApprovalAssignments.As(assignment);
      if (task != null && approvalAsg != null)
      {
        var requiredApprovers = Functions.ApprovalTask.GetAllRequiredApprovers(task);
        if (approvalAsg.Stage.StageType == Docflow.ApprovalStage.StageType.Approvers &&
            approvalAsg.Stage.AllowAdditionalApprovers == true &&
            !requiredApprovers.Contains(newApprover) &&
            !task.AddApproversExpanded.Select(x => x.Approver).Contains(newApprover))
        {
          using (var session = new Session())
          {
            var approver = task.AddApproversExpanded.AddNew();
            approver.Approver = newApprover;
            if (!task.AccessRights.CanUpdate())
              AddFullRightsInSession(session, task);
            task.Save();
          }
        }
      }
    }
    
    /// <summary>
    /// Получить список поручений по документу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Список поручений.</returns>
    [Remote]
    public List<RecordManagement.IActionItemExecutionTask> GetActionItemsByDocument(IOfficialDocument document)
    {
      var documentsGroupGuid = Docflow.PublicConstants.Module.TaskMainGroup.ActionItemExecutionTask;
      return RecordManagement.ActionItemExecutionTasks.GetAll()
        .Where(t => t.AttachmentDetails.Any(g => g.GroupId == documentsGroupGuid && document.Id == g.AttachmentId))
        .ToList();
    }
    
    /// <summary>
    /// Получить список справочников (правила согласования, правила назначения прав и др.), в которых используется вид документа.
    /// </summary>
    /// <param name="documentKind">Вид документа.</param>
    /// <returns>Список справочников.</returns>
    [Remote]
    public IQueryable<IEntity> GetDocumentKindSettings(IDocumentKind documentKind)
    {
      var result = new List<IEntity>();
      var approvalRules = PublicFunctions.ApprovalRuleBase.GetApprovalRulesByDocumentKind(documentKind);
      var registrationSettings = PublicFunctions.RegistrationSetting.GetRegistrationSettingsByDocumentKind(documentKind);
      var accessRightsRules = PublicFunctions.AccessRightsRule.GetAccessRightsRulesByDocumentKind(documentKind);
      var signatureSettings = PublicFunctions.SignatureSetting.GetSignatureSettingsByDocumentKind(documentKind);
      var documentTemplates = PublicFunctions.DocumentTemplate.GetDocumentTemplatesByDocumentKind(documentKind);
      var documentGroups = PublicFunctions.DocumentGroupBase.GetDocumentGroupsByDocumentKind(documentKind);
      result.AddRange(approvalRules);
      result.AddRange(registrationSettings);
      result.AddRange(accessRightsRules);
      result.AddRange(signatureSettings);
      result.AddRange(documentTemplates);
      result.AddRange(documentGroups);
      return result.AsQueryable();
    }
    
    /// <summary>
    /// Получить ИД текущего тенанта.
    /// </summary>
    /// <returns>ИД текущего тенанта.</returns>
    [Public, Remote]
    public string GetCurrentTenantId()
    {
      return Sungero.Core.TenantInfo.TenantId;
    }
    
    /// <summary>
    /// Получить дату последнего обновления прав документов.
    /// </summary>
    /// <param name="agentKey">Идентификатор фонового процесса.</param>
    /// <returns>Дата последней синхронизации.</returns>
    [Public]
    public static DateTime GetLastAgentRunDate(string agentKey)
    {
      var command = string.Format(Queries.Module.SelectDocflowParamsValue, agentKey);
      try
      {
        var executionResult = Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var date = string.Empty;
        if (!(executionResult is DBNull) && executionResult != null)
          date = executionResult.ToString();
        Logger.DebugFormat("Last access rights update date in DB is {0} (UTC)", date);
        
        DateTime result = DateTime.Parse(date, null, System.Globalization.DateTimeStyles.AdjustToUniversal);

        return result;
      }
      catch (Exception ex)
      {
        Logger.DebugFormat("Error while getting access rights update date", ex);
        return Calendar.SqlMinValue;
      }
    }
    
    /// <summary>
    /// Обновить дату последнего запуска для фонового процесса.
    /// </summary>
    /// <param name="agentKey">Идентификатор фонового процесса.</param>
    /// <param name="notificationDate">Дата обновления.</param>
    [Public]
    public static void UpdateLastAgentRunDate(string agentKey, DateTime notificationDate)
    {
      var newDate = notificationDate.ToString("yyyy-MM-ddTHH:mm:ss.ffff+0");
      Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.InsertOrUpdateDocflowParamsValue, new[] { agentKey, newDate });
      Logger.DebugFormat("Last access rights update date is set to {0} (UTC)", newDate);
    }
    
    /// <summary>
    /// Удалить элементы очереди.
    /// </summary>
    /// <param name="itemsIds">Элементы на удаление.</param>
    [Public]
    public static void FastDeleteQueueItems(List<int> itemsIds)
    {
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = string.Format("delete from {0} where Id in ({1})",
                                            Sungero.ExchangeCore.QueueItemBases.Info.DBTableName, string.Join(", ", itemsIds));
        command.ExecuteNonQuery();
      }
    }
    
    /// <summary>
    /// Получить Guid типа прав.
    /// </summary>
    /// <param name="rightType">Перечисление с типом прав.</param>
    /// <returns>Guid типа прав.</returns>
    [Public]
    public static Guid GetRightTypeGuid(Enumeration? rightType)
    {
      if (rightType == Docflow.AccessRightsRuleMembers.RightType.Read)
        return DefaultAccessRightsTypes.Read;

      if (rightType == Docflow.AccessRightsRuleMembers.RightType.Edit)
        return DefaultAccessRightsTypes.Change;

      if (rightType == Docflow.AccessRightsRuleMembers.RightType.FullAccess)
        return DefaultAccessRightsTypes.FullAccess;
      
      return new Guid();
    }
    
    /// <summary>
    /// Получить данные для формирования отчета Контроль исполнительской дисциплины.
    /// </summary>
    /// <param name="reportSessionId">Ид сессии отчета.</param>
    /// <param name="department">Отдел.</param>
    /// <param name="reportPerformer">Ответственный.</param>
    /// <param name="reportPeriodBegin">Начало периода.</param>
    /// <param name="reportPeriodEnd">Конец периода.</param>
    /// <param name="loadOldAssignments">Учитывать задания со сроком до начала периода.</param>
    /// <returns>Структура с данными для отчета.</returns>
    [Remote]
    public virtual List<Structures.AssignmentCompletionReport.TableLine> GetAssignmentCompletionReportData(string reportSessionId, IDepartment department,
                                                                                                           IUser reportPerformer, DateTime? reportPeriodBegin, DateTime? reportPeriodEnd, bool? loadOldAssignments)
    {
      var sourceAssignments = Assignments.GetAll()
        .Where(a => Employees.Is(a.Performer))
        .Where(a => a.Status == Workflow.AssignmentBase.Status.InProcess || a.Status == Workflow.AssignmentBase.Status.Completed)
        .Where(a => a.Performer.Status == CoreEntities.DatabookEntry.Status.Active);

      // Подразделение.
      if (department != null)
        sourceAssignments = sourceAssignments
          .Where(a => Equals(Company.Employees.As(a.Performer).Department, department));

      // Сотрудник.
      if (reportPerformer != null)
        sourceAssignments = sourceAssignments
          .Where(a => Equals(Company.Employees.As(a.Performer), reportPerformer));

      // Период.
      var clientPeriodBegin = reportPeriodBegin.Value.Date;
      var clientPeriodEnd = reportPeriodEnd.Value;
      var serverPeriodBegin = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(clientPeriodBegin);
      var serverPeriodEnd = Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(clientPeriodEnd);

      // Учет старых заданий.
      if (loadOldAssignments == true)
        sourceAssignments = sourceAssignments
          .Where(a => a.Completed.Between(serverPeriodBegin, serverPeriodEnd) ||
                 a.Deadline.HasValue &&
                 (a.Deadline.Between(serverPeriodBegin, serverPeriodEnd) && a.Deadline.Value.Date != a.Deadline.Value ||
                  a.Deadline.Between(clientPeriodBegin, clientPeriodEnd) && a.Deadline.Value.Date == a.Deadline.Value) &&
                 !(a.Status == Workflow.AssignmentBase.Status.Completed &&
                   a.Completed < serverPeriodBegin) ||
                 (a.Deadline < serverPeriodBegin && a.Deadline.Value.Date != a.Deadline.Value ||
                  a.Deadline < clientPeriodBegin && a.Deadline.Value.Date == a.Deadline.Value) &&
                 (a.Status == Workflow.AssignmentBase.Status.InProcess || a.Completed > serverPeriodEnd));
      else
        sourceAssignments = sourceAssignments
          .Where(a => a.Completed.Between(serverPeriodBegin, serverPeriodEnd) ||
                 a.Deadline.HasValue &&
                 (a.Deadline.Between(serverPeriodBegin, serverPeriodEnd) && a.Deadline.Value.Date != a.Deadline.Value ||
                  a.Deadline.Between(clientPeriodBegin, clientPeriodEnd) && a.Deadline.Value.Date == a.Deadline.Value) &&
                 !(a.Status == Workflow.AssignmentBase.Status.Completed &&
                   a.Completed < serverPeriodBegin));
      
      // Сортировка необходима, т.к. хибер при селекте в структуру меняет порядок.
      var tableData = sourceAssignments
        .OrderBy(a => a.Id)
        .Select(a => Structures.AssignmentCompletionReport.TableLine.Create(
          string.Empty,
          a.Id,
          a.Performer.Id,
          string.Empty, // name
          string.Empty, // jobTitle
          string.Empty, // department
          a.Task.Id,
          a.Subject,
          a.Author.Id,
          string.Empty, // name
          a.Created.Value,
          a.Deadline,
          a.Completed,
          a.IsRead == true,
          a.Status.ToString(),
          string.Empty, // createdtime
          string.Empty, // deadlinetime
          string.Empty, // completedtime
          string.Empty, // delay
          0,
          string.Empty, // assignmentstatus
          string.Empty)).ToList<Structures.AssignmentCompletionReport.TableLine>();
      
      var employeeIds = tableData.Select(t => t.Performer).ToList();
      employeeIds.AddRange(tableData.Select(t => t.Author));
      employeeIds = employeeIds.Distinct().ToList();
      var employees = employeeIds.Count > 1024 ? Company.Employees.GetAll().ToList() : Company.Employees.GetAll(z => employeeIds.Contains(z.Id)).ToList();

      // Для каждого задания вычислить задержку в раб.днях и гиперссылку.
      DateTime? now = Calendar.Now;
      foreach (Structures.AssignmentCompletionReport.TableLine assignment in tableData)
      {
        // ИД сессии явно указываем в цикле, в Create структуры Id будет закеширован и повторный запуск не обновит его.
        assignment.ReportSessionId = reportSessionId;

        // Статус.
        var inProcess = assignment.Status == Workflow.AssignmentBase.Status.InProcess.Value;
        var isCompleted = assignment.Status == Workflow.AssignmentBase.Status.Completed.Value;
        var completed = isCompleted ? assignment.Completed : now;

        // Задержка в раб.днях и статус задания.
        var created = assignment.Created;
        var deadline = assignment.Deadline;
        var performer = employees.SingleOrDefault(z => z.Id == assignment.Performer);
        var delay = Functions.Module.CalculateDelay(deadline, completed.Value, performer);
        
        // Если просрочка больше 4 часов, но меньше дня, то округляем до дня.
        if (delay == -1)
          delay = 1;

        // Признак просроченности.
        var delayLabel = string.Empty;
        var overdue = delay != 0;
        var assignmentStatus = string.Empty;
        if (inProcess && overdue)
        {
          assignmentStatus = Resources.UndoneAndOverdue;
          delayLabel = delay.ToString();
        }
        else if (inProcess && !overdue)
        {
          assignmentStatus = Resources.UndoneAndNotOverdue;
          delayLabel = Resources.InWork;
        }
        else if (!inProcess && overdue)
        {
          assignmentStatus = Resources.DoneAndOverdue;
          delayLabel = delay.ToString();
        }
        else
        {
          assignmentStatus = Resources.DoneAndNotOverdue;
          delayLabel = Resources.OnTime;
        }

        // Используется для сортировки по величине задержки выполнения.
        var delaySort = overdue ? delay : -1;

        // Выделить время в датах, где в сроке указано время (только).
        var deadlineTime = string.Empty;
        var createdTime = string.Empty;
        var completedTime = string.Empty;
        if (deadline.HasValue && deadline.Value.HasTime())
        {
          createdTime = Calendar.ToUserTime(created).ToString("HH:mm");
          deadlineTime = Calendar.ToUserTime(deadline.Value).ToString("HH:mm");
          if (!inProcess)
            completedTime = Calendar.ToUserTime(completed.Value).ToString("HH:mm");
        }

        assignment.EmployeeName = performer != null ? performer.Person.ShortName : string.Empty;
        assignment.JobTitle = performer != null && performer.JobTitle != null ? performer.JobTitle.Name : string.Empty;
        assignment.Department = performer != null && performer.Department != null ? performer.Department.Name : string.Empty;
        var author = employees.SingleOrDefault(z => z.Id == assignment.Author);
        assignment.AuthorName = (author != null && author.IsSystem != true)
          ? author.Person.ShortName
          : OfficialDocuments.Resources.StateViewSystem.ToString();

        assignment.CreatedTime = createdTime;
        assignment.DeadlineTime = deadlineTime;
        assignment.CompletedTime = completedTime;
        assignment.Delay = delayLabel;
        assignment.DelaySort = delaySort;
        assignment.AssignmentStatus = assignmentStatus;
        assignment.HyperLink = Hyperlinks.Get(AssignmentBases.Info, assignment.AssignmentId);
      }
      
      return tableData;
    }
    
    /// <summary>
    /// Получить список сертификатов.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Список сертификатов для подписания.</returns>
    [Remote, Public]
    public virtual List<ICertificate> GetCertificates(IOfficialDocument document)
    {
      var now = Calendar.Now;
      // Список прав подписей с учетом критериев.
      var signatureSettingsWithCriterion = Functions.OfficialDocument.GetSignatureSettings(document, Employees.Current)
        .Where(s => s.Certificate != null)
        .ToList();
      
      // Список всех прав подписей по пользователю без учета критериев.
      var signatureSettingsByUser = SignatureSettings.GetAll(s => s.Recipient.Equals(Users.Current) &&
                                                             s.Status == CoreEntities.DatabookEntry.Status.Active &&
                                                             s.Certificate != null)
        .ToList();
      // Цифровые сертификаты, подобранные по критериям прав подписи, или для которых нет ни одного права подписи.
      return Certificates.GetAll(c => Users.Current.Equals(c.Owner) &&
                                 (c.Enabled == true) &&
                                 (!c.NotBefore.HasValue || c.NotBefore <= now) &&
                                 (!c.NotAfter.HasValue || c.NotAfter >= now))
        .ToList()
        .Where(c => !signatureSettingsByUser.Any(s => Equals(s.Certificate, c)) ||
               signatureSettingsWithCriterion.Any(s => Equals(s.Certificate, c)))
        .ToList();
    }
    
    /// <summary>
    /// Запустить агент рассылки уведомления об окончании срока действия доверенностей.
    /// </summary>
    [Public, Remote]
    public static void RequeueSendNotificationForExpiringPowersOfAttorney()
    {
      Jobs.SendNotificationForExpiringPowerOfAttorney.Enqueue();
    }
    
    /// <summary>
    /// Запустить фоновый процесс "Перемещение содержимого документов в соответствии с политиками хранения".
    /// </summary>
    [Public, Remote]
    public static void RequeueTransferDocuments()
    {
      Jobs.TransferDocumentsByStoragePolicy.Enqueue();
    }
    
    /// <summary>
    /// Создать таблицу с настойками политик хранения.
    /// </summary>
    /// <param name="now">Время запуска фонового процесса.</param>
    public virtual void CreateStoragePolicySettings(DateTime now)
    {
      var commandText = Sungero.Docflow.Functions.Module.GetStoragePolicySettingsQuery(now);
      Logger.DebugFormat("TransferDocumentsByStoragePolicy: create storage policy settings.");
      Sungero.Docflow.Functions.Module.ExecuteSQLCommand(commandText);
    }
    
    /// <summary>
    /// Получить запрос создания временной таблицы с развернутыми политиками.
    /// </summary>
    /// <param name="now">Время старта фонового процесса.</param>
    /// <returns>Текст запроса.</returns>
    public virtual string GetStoragePolicySettingsQuery(DateTime now)
    {
      return string.Format(Docflow.Queries.Module.CreateStoragePolicySettings, Constants.Module.StoragePolicySettingsTableName, now.ToString("yyyy-MM-dd HH:mm:ss"));
    }
    
    /// <summary>
    /// Получить список Ид документов и хранилищ, куда их переместить.
    /// </summary>
    /// <returns>Список Ид документов и хранилищ для переноса.</returns>
    public virtual List<Docflow.Structures.Module.DocumentToSetStorage> GetDocumentsToTransfer()
    {
      var commandDocText = Sungero.Docflow.Functions.Module.GetDocumentsToTransferQuery();
      Logger.DebugFormat("TransferDocumentsByStoragePolicy: select documents to transfer.");
      var documentsToSetStorageList = new List<Docflow.Structures.Module.DocumentToSetStorage>();
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = commandDocText;
        var reader = command.ExecuteReader();
        while (reader.Read())
        {
          var documentId = reader.GetInt32(0);
          var storageId = reader.GetInt32(1);
          
          documentsToSetStorageList.Add(Docflow.Structures.Module.DocumentToSetStorage.Create(documentId, storageId));
        }
      }
      return documentsToSetStorageList;
    }
    
    /// <summary>
    /// Получить запрос получения документов для перемещения.
    /// </summary>
    /// <returns>Текст запроса.</returns>
    public virtual string GetDocumentsToTransferQuery()
    {
      return string.Format(Docflow.Queries.Module.SelectDocumentsToTransfer, Constants.Module.StoragePolicySettingsTableName);
    }
    
    /// <summary>
    /// Запустить асинхронный обработчик по переносу содержимого документа в хранилище.
    /// </summary>
    /// <param name="documentsToSetStorageList">Список Ид документов и хранилищ для переноса.</param>
    public virtual void ExecuteSetDocumentStorage(List<Docflow.Structures.Module.DocumentToSetStorage> documentsToSetStorageList)
    {
      Logger.DebugFormat("TransferDocumentsByStoragePolicy: start set storage policy to documents.");
      foreach (var item in documentsToSetStorageList)
      {
        var asyncSetStorage = Docflow.AsyncHandlers.SetDocumentStorage.Create();
        asyncSetStorage.DocumentId = item.DocumentId;
        asyncSetStorage.StorageId = item.StorageId;
        asyncSetStorage.ExecuteAsync();
      }
    }
    
    /// <summary>
    /// Получить сертификат по содержимому подписи.
    /// </summary>
    /// <param name="signatureContent">Подпись.</param>
    /// <returns>Сертификат.</returns>
    [Public]
    public virtual System.Security.Cryptography.X509Certificates.X509Certificate2 GetSignatureCertificate(byte[] signatureContent)
    {
      var signatureInfo = ExternalSignatures.GetSignatureInfo(signatureContent);
      if (signatureInfo.SignatureFormat == SignatureFormat.Hash)
        throw AppliedCodeException.Create(Resources.IncorrectSignatureFormat);
      var cadesBesSignatureInfo = signatureInfo.AsCadesBesSignatureInfo();
      return cadesBesSignatureInfo.Certificate;
    }
    
    #region Выгрузка
    
    /// <summary>
    /// Подготовка документов для выгрузки.
    /// </summary>
    /// <param name="objsId">Список ИД документов.</param>
    /// <param name="parameters">Параметры выгрузки.</param>
    /// <returns>Данные для выгрузки.</returns>
    [Remote(IsPure = true)]
    public virtual Structures.Module.AfterExportDialog PrepareExportDocumentDialogDocuments(List<int> objsId,
                                                                                            Structures.Module.ExportDialogParams parameters)
    {
      var objs = new List<IOfficialDocument>();
      AccessRights.AllowRead(
        () =>
        {
          objs = Docflow.OfficialDocuments.GetAll().Where(x => objsId.Contains(x.Id)).ToList();
        });
      return Functions.Module.PrepareExportDocumentDialogDocuments(objs.AsQueryable(), parameters);
    }

    /// <summary>
    /// Подготовка данных для выгрузки документов.
    /// </summary>
    /// <param name="search">Критерии поиска документов.</param>
    /// <param name="parameters">Параметры выгрузки.</param>
    /// <returns>Данные для выгрузки.</returns>
    [Remote(IsPure = true)]
    public virtual Structures.Module.AfterExportDialog PrepareExportDocumentDialogDocuments(Structures.Module.IExportDialogSearch search,
                                                                                            Structures.Module.ExportDialogParams parameters)
    {
      var query = Functions.Module.SearchByRequisites(search);
      return Functions.Module.PrepareExportDocumentDialogDocuments(query, parameters);
    }
    
    /// <summary>
    /// Подготовка данных для выгрузки документов.
    /// </summary>
    /// <param name="objs">Список документов.</param>
    /// <param name="parameters">Параметры выгрузки.</param>
    /// <returns>Данные для выгрузки.</returns>
    public virtual Structures.Module.AfterExportDialog PrepareExportDocumentDialogDocuments(IQueryable<IOfficialDocument> objs,
                                                                                            Structures.Module.ExportDialogParams parameters)
    {
      var now = Calendar.UserNow;
      var tempFolderName = Resources.ExportDocumentFolderNameFormat(now.ToString("dd.MM.yy") + " " + now.ToLongTimeString()).ToString();
      tempFolderName = CommonLibrary.FileUtils.NormalizeFileName(tempFolderName);
      var result = Structures.Module.AfterExportDialog.Create(tempFolderName, string.Empty, now,
                                                              new List<Structures.Module.ExportedDocument>());
      
      foreach (var counterparty in objs.GroupBy(d => Functions.OfficialDocument.GetCounterparties(d) != null ? Functions.OfficialDocument.GetCounterparties(d).FirstOrDefault() : null))
      {
        foreach (var type in counterparty.GroupBy(c => c.DocumentKind.DocumentType))
        {
          foreach (var document in type)
          {
            var isFormalized = AccountingDocumentBases.Is(document) && AccountingDocumentBases.As(document).IsFormalized == true;
            var docStructure = Structures.Module.ExportedDocument
              .Create(document.Id, isFormalized, false, string.Empty, false, parameters.ForPrint, string.Empty, Structures.Module.ExportedFolder
                      .Create(string.Empty, new List<Structures.Module.ExportedFile>(),
                              new List<Structures.Module.ExportedFolder>(), string.Empty), document.Name, null, parameters.IsSingleExport);
            
            result.Documents.Add(docStructure);

            Logger.DebugFormat("Document with id '{0}' has been prepared for export documents. Is formalized: '{1}', for print: '{2}'.",
                               document.Id, isFormalized, parameters.ForPrint);
            
            var folder = docStructure.Folder;
            if (parameters.GroupCounterparty || parameters.GroupDocumentType)
            {
              var folderName = parameters.GroupCounterparty ?
                CommonLibrary.FileUtils.NormalizeFileName(counterparty.Key.Name) :
                CommonLibrary.FileUtils.NormalizeFileName(type.Key.Name);
              
              var subfolder = Structures.Module.ExportedFolder
                .Create(folderName, new List<Structures.Module.ExportedFile>(),
                        new List<Structures.Module.ExportedFolder>(), folder.FolderName);
              folder.Folders.Add(subfolder);
              folder = subfolder;
            }
            
            var parentName = Functions.Module.GetExportedDocumentFileName(docStructure, document);

            if (parameters.AddAddendum == true)
            {
              var addenda = Functions.Module.GetAddendumsForExport(document);
              foreach (var addendumDocument in addenda)
              {
                var isAddendumFormalized = AccountingDocumentBases.Is(addendumDocument) && AccountingDocumentBases.As(addendumDocument).IsFormalized == true;
                Structures.Module.ExportedDocument structure;
                structure = Structures.Module.ExportedDocument.Create(addendumDocument.Id, isAddendumFormalized, true, parentName,
                                                                      false, parameters.ForPrint, string.Empty,
                                                                      docStructure.Folder, addendumDocument.Name, document.Id, parameters.IsSingleExport);
                result.Documents.Add(structure);
                
                Logger.DebugFormat("Addendum with id '{0}' has been prepared for export documents. Is formalized: '{1}', for print: '{2}', lead document id '{3}'.",
                                   addendumDocument.Id, isAddendumFormalized, parameters.ForPrint, document.Id);
              }
            }
          }
        }
      }
      
      return result;
    }
    
    /// <summary>
    /// Получить приложения для выгрузки.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Список приложений.</returns>
    public virtual List<IOfficialDocument> GetAddendumsForExport(IOfficialDocument document)
    {
      return document.Relations.GetRelated(Constants.Module.AddendumRelationName)
        .Where(x => OfficialDocuments.Is(x))
        .Select(d => OfficialDocuments.As(d))
        .ToList();
    }

    /// <summary>
    /// Выгрузка документов в десктоп-клиенте.
    /// </summary>
    /// <param name="objs">Данные для выгрузки документов.</param>
    /// <returns>Результат экспорта.</returns>
    [Remote]
    public virtual List<Structures.Module.ExportedDocument> AfterExportDocumentDialog(List<Structures.Module.ExportedDocument> objs)
    {
      foreach (var obj in objs)
      {
        try
        {
          if (obj.IsAddendum)
          {
            var leadingDocument = objs.Where(x => Equals(x.Id, obj.LeadDocumentId)).FirstOrDefault();
            if (leadingDocument != null && leadingDocument.IsFaulted == true)
            {
              obj.IsFaulted = true;
              obj.Error = Resources.ExportDialog_Error_LeadDocumentNoVersion;
              continue;
            }
          }
          
          if (obj.IsFormalized)
            Functions.Module.ExportFormalizedDocumentsToFolder(obj, null);
          else
            Functions.Module.ExportNonformalizedDocumentsToFolder(obj, null);
          
          Logger.DebugFormat("Document with id '{0}' has been processed for export financial documents. Is formalized: '{1}', for print: '{2}', lead document id '{3}', is faulted: '{4}', error message: '{5}'",
                             obj.Id, obj.IsFormalized, obj.IsPrint, obj.LeadDocumentId, obj.IsFaulted, obj.Error);
        }
        catch (Exception ex)
        {
          Logger.Debug(ex.ToString());
          obj.Error = Resources.ExportDialog_Error_ClientFormat(ex.Message.TrimEnd('.'));
          obj.IsFaulted = true;
        }
      }
      return objs;
    }
    
    /// <summary>
    /// Выгрузка документов в веб-клиенте.
    /// </summary>
    /// <param name="objs">Данные для выгрузки документов.</param>
    /// <param name="parameters">Параметры выгрузки.</param>
    /// <returns>Результат экспорта.</returns>
    [Remote]
    public virtual Structures.Module.ExportResult AfterExportDocumentDialogToWeb(List<Structures.Module.ExportedDocument> objs,
                                                                                 Structures.Module.ExportDialogParams parameters)
    {
      var result = Structures.Module.ExportResult.Create();
      var zipModels = new List<Structures.Module.ZipModel>();
      foreach (var obj in objs)
      {
        try
        {
          if (obj.IsAddendum)
          {
            var leadingDocument = objs.Where(x => Equals(x.Id, obj.LeadDocumentId)).FirstOrDefault();
            if (leadingDocument != null && leadingDocument.IsFaulted == true)
            {
              obj.IsFaulted = true;
              obj.Error = Resources.ExportDialog_Error_LeadDocumentNoVersion;
              continue;
            }
          }
          
          if (obj.IsFormalized)
            Functions.Module.ExportFormalizedDocumentsToFolder(obj, zipModels);
          else
            Functions.Module.ExportNonformalizedDocumentsToFolder(obj, zipModels);
          
          Logger.DebugFormat("Document with id '{0}' has been processed for export financial documents. Is formalized: '{1}', for print: '{2}', lead document Id: '{3}', is faulted: '{4}', error message: '{5}'",
                             obj.Id, obj.IsFormalized, obj.IsPrint, obj.LeadDocumentId, obj.IsFaulted, obj.Error);
        }
        catch (Exception ex)
        {
          Logger.Debug(ex.ToString());
          obj.Error = Resources.ExportDialog_Error_ClientFormat(ex.Message.TrimEnd('.'));
          obj.IsFaulted = true;
        }
      }
      
      result.ExportedDocuments = objs;
      result.ZipModels = zipModels;
      return result;
    }

    /// <summary>
    /// Сформировать архив для выгрузки документов в веб-клиенте.
    /// </summary>
    /// <param name="zipModels">Модель выгрузки.</param>
    /// <param name="objs">Список документов для выгрузки.</param>
    /// <param name="fileName">Имя файла для выгружаемого документа.</param>
    /// <returns>Архив.</returns>
    [Remote]
    public virtual IZip CreateZipFromZipModel(List<Structures.Module.ZipModel> zipModels, List<Structures.Module.ExportedDocument> objs, string fileName)
    {
      var zip = Zip.Create();
      foreach (var zipModel in zipModels)
      {
        var document = Docflow.OfficialDocuments.Get(zipModel.DocumentId);
        var version = document.Versions.Where(x => x.Id == zipModel.VersionId).FirstOrDefault();
        if (zipModel.SignatureId != null)
        {
          
          var signature = Signatures.Get(version).Where(x => x.Id == zipModel.SignatureId).SingleOrDefault();
          zip.Add(signature, zipModel.FileName, zipModel.FolderRelativePath.ToArray());
          continue;
        }
        var body = zipModel.IsPublicBody ? version.PublicBody : version.Body;
        zip.Add(body, zipModel.FileName, zipModel.FolderRelativePath.ToArray());
        Logger.DebugFormat("Document with Id '{0}', version id '{1}', is PublicBody: '{2}' has been added to zip model",
                           zipModel.DocumentId, zipModel.VersionId, zipModel.IsPublicBody);
      }
      
      // Отчет
      var now = Calendar.UserNow;
      var generated = Functions.Module.GetFinArchiveExportReport(objs, now);
      zip.Add(generated, Sungero.FinancialArchive.Reports.Resources.FinArchiveExportReport.HeaderFormat(now.ToShortDateString() + " " + now.ToLongTimeString()));
      Logger.DebugFormat("Report has been added to zip model");
      
      zip.Save(fileName);
      Logger.DebugFormat("Zip model has been saved");
      return zip;
    }
    
    /// <summary>
    /// Имя папки для экспорта документа.
    /// </summary>
    /// <param name="officialDocument">Документ.</param>
    /// <returns>Имя папки.</returns>
    public virtual string GetExportedDocumentFolderName(IOfficialDocument officialDocument)
    {
      // Имя папки всегда формируется как "Договор №Д-17_10 от 10.12.2017 (123)".
      var name = Functions.Module.GetDocumentNameForExport(officialDocument, true);
      return CommonLibrary.FileUtils.NormalizeFileName(name) + " (" + officialDocument.Id + ")";
    }
    
    /// <summary>
    /// Имя файла для выгружаемого документа.
    /// </summary>
    /// <param name="document">Данные о документе.</param>
    /// <param name="officialDocument">Документ.</param>
    /// <returns>Имя файла.</returns>
    public virtual string GetExportedDocumentFileName(Structures.Module.ExportedDocument document, IOfficialDocument officialDocument)
    {
      //// Формализованные:
      //// Для печати: Акт №2 от 12.12.12 (123).pdf
      //// В эл. формате: ON_SCHFDOPPR_2TS1adcbec8-89da-4744-855d-0a9566479eff_....xml
      //// Как приложения для печати: %формат имени договорного документа для печати%_Приложение (123).pdf
      //// Как приложения в эл. формате: ON_SCHFDOPPR_2TS1adcbec8-89da-4744-855d-0a9566479eff_....xml

      //// Неформализованные (разные форматы для бухгалтерских документов и остальных):
      //// Для печати: Акт №2 от 12.12.12 (123).pdf / Договор №Д-17_10 от 10.12.2017 с Уник, ООО _Поставка электроизмерительного оборудования_для_печати (123).pdf
      //// В эл. формате: Акт №2 от 12.12.12.pdf / Договор №Д-17_10 от 10.12.2017 с Уник, ООО _Поставка электроизмерительного оборудования_для_печати.pdf
      //// Как приложения для печати: %формат имени договорного документа для печати%_Приложение (123).pdf
      //// Как приложения в эл. формате: Акт №2 от 12.12.12.pdf / Договор №Д-17_10 от 10.12.2017 с Уник, ООО _Поставка электроизмерительного оборудования_для_печати.pdf

      //// Печатные формы неформализованных документов в эл. формате дополняются постфиксом "_для_печати".
      //// Подписи неформализованных документов в эл. формате дополняются постфиксом "__" или "_1_".
      //// Эл. форматы формализованных не реализованы в функции, а сделаны на местах, т.к. имя надо получать из тела.
      
      var name = string.Empty;
      
      // Приложения для печати имеют собственный формат имени. Все остальные - как эл. формат неформализованных.
      if (document.IsAddendum && document.IsPrint)
      {
        var leadDocumentName = document.ParentShortName.Substring(0, Math.Min(document.ParentShortName.Length, Constants.Module.ExportNameLength));
        if (document.ParentShortName.Length > Constants.Module.ExportNameLength && document.LeadDocumentId != null)
        {
          var leadDocument = OfficialDocuments.Get(document.LeadDocumentId.Value);
          if (leadDocument != null)
            leadDocumentName = leadDocument.Name.Substring(0, Math.Min(leadDocument.Name.Length, Constants.Module.ExportNameLength)) + " (" + leadDocument.Id + ")";
        }
        name = Resources.ExportDocumentAddendumNameFormat(leadDocumentName);
      }
      else
        name = Functions.Module.GetDocumentNameForExport(officialDocument, false);
      
      // Файлы для печати должны быть с ID документа.
      name = name + " (" + document.Id + ")";
      
      return CommonLibrary.FileUtils.NormalizeFileName(name);
    }
    
    /// <summary>
    /// Имя документа/название папки для выгрузки.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="forFolder">Если true - название папки для выгрузки, иначе имя документа.</param>
    /// <returns>Имя документа/название папки.</returns>
    public virtual string GetDocumentNameForExport(IOfficialDocument document, bool forFolder)
    {
      var name = string.Empty;
      var russianCulture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
      using (Sungero.Core.CultureInfoExtensions.SwitchTo(russianCulture))
      {
        if (document.RegistrationNumber != null)
          name += OfficialDocuments.Resources.Number + document.RegistrationNumber;
        
        if (document.RegistrationDate != null)
          name += OfficialDocuments.Resources.DateFrom + document.RegistrationDate.Value.ToString("d");
      }
      
      var accounting = AccountingDocumentBases.As(document);
      var type = string.Empty;
      if (accounting != null || forFolder)
        type = Functions.Module.GetShortTypeName(document);
      
      // Если тип документа не удалось определить, берем просто его имя ограниченной длины. Только для имен файлов.
      if (!string.IsNullOrWhiteSpace(type))
        name = type + name;
      else if (!forFolder)
        name = document.Name.Substring(0, Math.Min(document.Name.Length, Constants.Module.ExportNameLength));
      
      // Для формирования имени неформализованного финансового документа.
      if (!forFolder && accounting != null && accounting.IsFormalized != true)
        name = document.Name.Substring(0, Math.Min(document.Name.Length, Constants.Module.ExportNameLength));
      
      return name;
    }
    
    /// <summary>
    /// Получить короткое наименование типа документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Короткое наименование типа документа.</returns>
    public virtual string GetShortTypeName(IOfficialDocument document)
    {
      var accounting = AccountingDocumentBases.As(document);
      if (FinancialArchive.ContractStatements.Is(document))
        return Resources.NameForExport_ContractStatement;
      if (FinancialArchive.Waybills.Is(document))
        return Resources.NameForExport_Waybill;
      if (FinancialArchive.IncomingTaxInvoices.Is(document))
        return accounting.IsAdjustment == true ? Resources.NameForExport_IncomingTaxInvoiceAdjustment : Resources.NameForExport_IncomingTaxInvoce;
      if (FinancialArchive.OutgoingTaxInvoices.Is(document))
        return accounting.IsAdjustment == true ? Resources.NameForExport_OutgoingTaxInvoiceAdjustment : Resources.NameForExport_OutgoingTaxInvoice;
      if (FinancialArchive.UniversalTransferDocuments.Is(document))
        return accounting.IsAdjustment == true ? Resources.NameForExport_UTDAdjustment : Resources.NameForExport_UTD;
      if (Contracts.Contracts.Is(document))
        return Resources.NameForExport_Contract;
      if (Contracts.SupAgreements.Is(document))
        return Resources.NameForExport_SupAgreement;

      return document.DocumentKind.DocumentType.Name;
    }
    
    /// <summary>
    /// Получить папку, в которую документ будет выгружен.
    /// </summary>
    /// <param name="exportModel">Данные о документе.</param>
    /// <param name="officialDocument">Документ.</param>
    /// <returns>Папка выгрузки.</returns>
    public virtual Structures.Module.ExportedFolder GetRealDocumentFolder(Structures.Module.ExportedDocument exportModel,
                                                                          IOfficialDocument officialDocument)
    {
      var innerFolder = exportModel.Folder;
      while (innerFolder.Folders != null && innerFolder.Folders.Any())
        innerFolder = innerFolder.Folders.Single();
      
      if (!exportModel.IsPrint && !exportModel.IsAddendum && !exportModel.IsSingleExport)
      {
        var folderName = Functions.Module.GetExportedDocumentFolderName(officialDocument);
        if (folderName == innerFolder.FolderName)
          return innerFolder;
        
        var subFolder = Structures.Module.ExportedFolder
          .Create(folderName,
                  new List<Structures.Module.ExportedFile>(),
                  new List<Structures.Module.ExportedFolder>(), innerFolder.FolderName);
        innerFolder.Folders.Add(subFolder);
        return subFolder;
      }
      
      if (exportModel.IsAddendum && !exportModel.IsPrint)
      {
        string mainFolderName = innerFolder.FolderName;
        if (exportModel.LeadDocumentId.HasValue && !exportModel.IsSingleExport)
        {
          var leadDocument = OfficialDocuments.Get(exportModel.LeadDocumentId.Value);
          mainFolderName = Functions.Module.GetExportedDocumentFolderName(leadDocument);
        }
        
        var addendumSubFolder = Structures.Module.ExportedFolder
          .Create(Sungero.Docflow.Resources.ExportAddendumFolderName,
                  new List<Structures.Module.ExportedFile>(),
                  new List<Structures.Module.ExportedFolder>(), mainFolderName);

        var addendumSubFolders = new List<Structures.Module.ExportedFolder>();
        addendumSubFolders.Add(addendumSubFolder);
        
        var subFolder = Structures.Module.ExportedFolder
          .Create(mainFolderName,
                  new List<Structures.Module.ExportedFile>(),
                  addendumSubFolders, innerFolder.FolderName);

        innerFolder.Folders.Add(subFolder);
        return addendumSubFolder;
      }
      
      return innerFolder;
    }
    
    /// <summary>
    /// Экспорт неформализованного документа.
    /// </summary>
    /// <param name="exportModel">Данные о документе.</param>
    /// <param name="zipModels">Модель выгрузки.</param>
    public virtual void ExportNonformalizedDocumentsToFolder(Structures.Module.ExportedDocument exportModel, List<Structures.Module.ZipModel> zipModels)
    {
      var document = OfficialDocuments.Get(exportModel.Id);
      if (!document.HasVersions)
      {
        exportModel.IsFaulted = true;
        exportModel.Error = Resources.ExportDialog_Error_NoVersion;
        return;
      }
      
      var version = Functions.Module.GetExportedDocumentVersion(document);
      var queue = Sungero.ExchangeCore.BodyConverterQueueItems.GetAll().Where(x => Equals(x.Document, document) && Equals(x.VersionId, version.Id));
      if (queue.Any())
      {
        exportModel.IsFaulted = true;
        exportModel.Error = Resources.ExportDialog_Error_InProcess;
        return;
      }
      Functions.Module.ExportDocumentWithSignature(exportModel, zipModels);

      if (zipModels == null)
      {
        var operation = new Enumeration(Constants.AccountingDocumentBase.ExportToFolder);
        document.History.Write(operation, operation, string.Empty, version.Number);
      }
    }

    /// <summary>
    /// Экспорт документа с подписями.
    /// </summary>
    /// <param name="exportModel">Данные о документе.</param>
    /// <param name="zipModels">Модель выгрузки.</param>
    public virtual void ExportDocumentWithSignature(Structures.Module.ExportedDocument exportModel, List<Structures.Module.ZipModel> zipModels)
    {
      var document = OfficialDocuments.Get(exportModel.Id);
      var version = Functions.Module.GetExportedDocumentVersion(document);
      
      var fileName = Functions.Module.GetExportedDocumentFileName(exportModel, document);
      var folder = Functions.Module.GetRealDocumentFolder(exportModel, document);
      
      var hasPublicBody = version.PublicBody != null && version.PublicBody.Size != 0;
      if (hasPublicBody)
        Functions.Module.WriteTokenToFile(version, fileName + Resources.ExportForPrintAdditionalName, true, folder, document.Id, zipModels, exportModel.Folder);
      if (!exportModel.IsPrint || !hasPublicBody)
        Functions.Module.WriteTokenToFile(version, fileName, false, folder, document.Id, zipModels, exportModel.Folder);
      
      if (!exportModel.IsPrint)
      {
        var info = Exchange.PublicFunctions.ExchangeDocumentInfo.Remote.GetExDocumentInfoFromVersion(document, version.Id);
        var signatures = new List<Sungero.Domain.Shared.ISignature>() { };
        if (info != null)
          signatures = Functions.Module.GetExchangeDocumentSignature(document);
        else
          signatures = Functions.Module.GetDocumentSignature(document);
        
        var numberSignature = 1;
        foreach (var signature in signatures)
        {
          Functions.Module.ExportSignature(version, fileName + string.Format("_{0}_", numberSignature), folder, signature, zipModels, exportModel.Folder);
          numberSignature++;
        }
      }

    }
    
    /// <summary>
    /// Получить подписи формализованного документа.
    /// </summary>
    /// <param name="document">Формализованный документ.</param>
    /// <returns>Список подписей.</returns>
    public virtual List<Sungero.Domain.Shared.ISignature> GetExchangeDocumentSignature(IOfficialDocument document)
    {
      var version = Functions.Module.GetExportedDocumentVersion(document);
      var senderSignId = Sungero.FinancialArchive.PublicFunctions.Module.GetSenderSignatureId(document, version);
      var receiverSignId = Sungero.FinancialArchive.PublicFunctions.Module.GetReceiverSignatureId(document, version);
      var senderSign = Signatures.Get(version).Where(x => x.Id == senderSignId).SingleOrDefault();
      var receiverSign = Signatures.Get(version).Where(x => x.Id == receiverSignId).SingleOrDefault();
      
      return new List<Sungero.Domain.Shared.ISignature>() { senderSign, receiverSign };
    }
    
    /// <summary>
    /// Получить подписи неформализованного документа.
    /// </summary>
    /// <param name="document">Нефоормализованный документ.</param>
    /// <returns>Список подписей.</returns>
    public virtual List<Sungero.Domain.Shared.ISignature> GetDocumentSignature(IOfficialDocument document)
    {
      var version = Functions.Module.GetExportedDocumentVersion(document);
      return Signatures.Get(version).Where(s => s.SignatureType == SignatureType.Approval && s.IsValid && s.SignCertificate != null).ToList();
    }
    
    /// <summary>
    /// Получить путь до папки.
    /// </summary>
    /// <param name="folder">Данные о папке выгрузки.</param>
    /// <returns>Путь до папки.</returns>
    public virtual List<string> GetFolderRelativePath(Structures.Module.ExportedFolder folder)
    {
      var path = new List<string> { };
      
      if (!string.IsNullOrWhiteSpace(folder.FolderName))
        path.Add(folder.FolderName);
      if (folder.Folders.Any())
      {
        foreach (var subFolder in folder.Folders)
        {
          var subFolderPath = Functions.Module.GetFolderRelativePath(subFolder);
          path.Add(subFolderPath);
        }
      }

      return path;
    }
    
    /// <summary>
    /// Подготовить информацию о файлах выгружаемого документа.
    /// </summary>
    /// <param name="version">Версия документа.</param>
    /// <param name="docName">Имя документа.</param>
    /// <param name="isPublicBody">True, если выгрузка PublicBody, иначе - тела документа.</param>
    /// <param name="folder">Информация о структуре папок для выгрузки документа.</param>
    /// <param name="id">Id документа.</param>
    /// <param name="zipModels">Информация о zip-архиве при выгрузке документа в вебе.</param>
    /// <param name="mainFolder">Информация о корневой папке.</param>
    public virtual void WriteTokenToFile(Sungero.Content.IElectronicDocumentVersions version, string docName, bool isPublicBody,
                                         Structures.Module.ExportedFolder folder, int id,
                                         List<Structures.Module.ZipModel> zipModels, Structures.Module.ExportedFolder mainFolder)
    {
      var ticketServicePath = string.Empty;
      var ticketToken = string.Empty;
      var body = isPublicBody ? version.PublicBody : version.Body;
      var extension = isPublicBody ? version.AssociatedApplication.Extension : version.BodyAssociatedApplication.Extension;
      var fileName = docName + "." + extension;
      if (zipModels != null)
      {
        var zipModel = Structures.Module.ZipModel.Create();
        zipModel.DocumentId = id;
        zipModel.VersionId = version.Id;
        zipModel.IsPublicBody = isPublicBody;
        zipModel.FileName = fileName;
        zipModel.FolderRelativePath = Functions.Module.GetFolderRelativePath(mainFolder);
        zipModel.Size = body.Size;
        zipModels.Add(zipModel);
      }
      else
      {
        #warning Нелегал на сервис хранилищ, см 62340
        var dependency = Type.GetType("CommonLibrary.Dependencies.Dependency, CommonLibrary");
        var resolveMethod = dependency.GetMethods().Single(m => m.Name == "Resolve" && m.GetParameters().Length == 0);
        var providerType = Type.GetType("Sungero.StorageService.Client.IStorageServiceTokenProvider, Sungero.StorageService.Client");
        var generic = resolveMethod.MakeGenericMethod(providerType);
        object tokenProvider = generic.Invoke(null, null);
        
        var currentUser = Users.Current.Name;
        var generateMethod = providerType.GetMethod("GenerateReadToken");
        ticketToken = (string)generateMethod.Invoke(tokenProvider, new object[] { body.Id.Value, Sungero.Core.TenantInfo.TenantId, currentUser });
        
        ticketServicePath = body.Storage.Address;
      }
      
      var file = Structures.Module.ExportedFile
        .Create(id, fileName, null, ticketServicePath, ticketToken);
      folder.Files.Add(file);
    }

    /// <summary>
    /// Выгрузить формализованный документ в папку.
    /// </summary>
    /// <param name="exportModel">Данные по документу.</param>
    /// <param name="zipModels">Модель выгрузки.</param>
    public virtual void ExportFormalizedDocumentsToFolder(Structures.Module.ExportedDocument exportModel, List<Structures.Module.ZipModel> zipModels)
    {
      var document = AccountingDocumentBases.Get(exportModel.Id);
      var onlyOneSign = document.ExchangeState == Sungero.Docflow.AccountingDocumentBase.ExchangeState.Sent ||
        document.ExchangeState == Sungero.Docflow.AccountingDocumentBase.ExchangeState.Received;
      var operation = new Enumeration(Constants.AccountingDocumentBase.ExportToFolder);
      int? versionNumber = null;
      
      if (document.ExchangeState == null)
      {
        exportModel.IsFaulted = true;
        exportModel.Error = Resources.ExportDialog_Error_Imported;
        return;
      }

      if (document.ExchangeState == Sungero.Docflow.AccountingDocumentBase.ExchangeState.Rejected)
      {
        exportModel.IsFaulted = true;
        exportModel.Error = Resources.ExportDialog_Error_Rejected;
        return;
      }
      
      if (document.ExchangeState == Sungero.Docflow.AccountingDocumentBase.ExchangeState.SignRequired)
      {
        exportModel.IsFaulted = true;
        exportModel.Error = Resources.ExportDialog_Error_LastSignNotFound;
        return;
      }
      
      if (document.ExchangeState == Sungero.Docflow.AccountingDocumentBase.ExchangeState.Obsolete ||
          document.ExchangeState == Sungero.Docflow.AccountingDocumentBase.ExchangeState.Terminated)
      {
        exportModel.IsFaulted = true;
        exportModel.Error = Resources.ExportDialog_Error_Obsolete;
        return;
      }
      
      if (document.ExchangeState == Sungero.Docflow.AccountingDocumentBase.ExchangeState.SignAwaited)
      {
        exportModel.IsFaulted = true;
        exportModel.Error = Resources.ExportDialog_Error_CounterpartySignNotFound;
        return;
      }

      var folder = Functions.Module.GetRealDocumentFolder(exportModel, document);
      try
      {
        var queue = Sungero.ExchangeCore.BodyConverterQueueItems.GetAll().Where(x => Equals(x.Document, document));
        if (queue.Any())
        {
          exportModel.IsFaulted = true;
          exportModel.Error = Resources.ExportDialog_Error_InProcess;
          return;
        }
        
        var fileName = Functions.Module.GetExportedDocumentFileName(exportModel, document);
        var version = document.LastVersion;
        Functions.Module.WriteTokenToFile(version, fileName, true, folder, document.Id, zipModels, exportModel.Folder);
        if (zipModels == null)
          document.History.Write(operation, operation, string.Empty, versionNumber);
      }
      catch (Exception ex)
      {
        Logger.Debug(ex.ToString());
        exportModel.IsFaulted = true;
        exportModel.Error = ex.Message;
        return;
      }
      
      if (!exportModel.IsPrint)
      {
        var sellerTitle = document.Versions.Where(x => x.Id == document.SellerTitleId).FirstOrDefault();
        if (sellerTitle == null)
        {
          exportModel.IsFaulted = true;
          exportModel.Error = Resources.ExportDialog_Error_SellerTitleNotFound;
          return;
        }
        
        var sellerSign = Signatures.Get(sellerTitle).Where(x => x.Id == document.SellerSignatureId).SingleOrDefault();
        if (sellerSign == null)
        {
          exportModel.IsFaulted = true;
          exportModel.Error = Resources.ExportDialog_Error_SellerSignatureNotFound;
          return;
        }
        
        Functions.Module.ExportFormalizedVersion(sellerTitle, folder, sellerSign, zipModels, exportModel.Folder);
        
        if (onlyOneSign)
          versionNumber = sellerTitle.Number;
        if (zipModels == null)
          document.History.Write(operation, operation, string.Empty, versionNumber);
        
        if (onlyOneSign)
          return;
        
        var buyerTitle = document.Versions.Where(x => x.Id == document.BuyerTitleId).FirstOrDefault();
        if (buyerTitle == null)
        {
          exportModel.IsFaulted = true;
          exportModel.Error = Resources.ExportDialog_Error_BuyerTitleNotFound;
          return;
        }
        
        var buyerSign = Signatures.Get(buyerTitle).Where(x => x.Id == document.BuyerSignatureId).SingleOrDefault();
        if (buyerSign == null)
        {
          exportModel.IsFaulted = true;
          exportModel.Error = Resources.ExportDialog_Error_BuyerSignNotFound;
          return;
        }
        
        Functions.Module.ExportFormalizedVersion(buyerTitle, folder, buyerSign, zipModels, exportModel.Folder);
      }
    }
    
    /// <summary>
    /// Экспортировать версию с подписью.
    /// </summary>
    /// <param name="version">Версия.</param>
    /// <param name="folder">Папка для экспорта.</param>
    /// <param name="signature">Подпись.</param>
    /// <param name="zipModels">Модель zip архива для выгрузки в веб.</param>
    /// <param name="mainFolder">Полная структура папок.</param>
    public virtual void ExportFormalizedVersion(Sungero.Content.IElectronicDocumentVersions version, Structures.Module.ExportedFolder folder,
                                                Sungero.Domain.Shared.ISignature signature, List<Structures.Module.ZipModel> zipModels, Structures.Module.ExportedFolder mainFolder)
    {
      byte[] memoryArray = null;
      var xdoc = System.Xml.Linq.XDocument.Load(version.Body.Read());
      Sungero.Exchange.PublicFunctions.Module.RemoveNamespaces(xdoc);
      var docElement = xdoc.Element("Файл");
      var fileName = docElement.Attribute("ИдФайл").Value;
      
      var fullFileName = fileName + ".xml";
      
      var body = version.Body;
      
      if (zipModels == null)
      {
        using (var memory = new System.IO.MemoryStream())
        {
          body.Read().CopyTo(memory);
          memoryArray = memory.ToArray();
        }
      }
      else
      {
        var zipModel = Structures.Module.ZipModel.Create();
        zipModel.DocumentId = version.ElectronicDocument.Id;
        zipModel.VersionId = version.Id;
        zipModel.IsPublicBody = false;
        zipModel.FileName = fullFileName;
        zipModel.FolderRelativePath = Functions.Module.GetFolderRelativePath(mainFolder);
        zipModel.Size = body.Size;
        zipModels.Add(zipModel);
      }
      
      var file = Structures.Module.ExportedFile.Create(version.ElectronicDocument.Id, fullFileName, memoryArray, null, null);
      folder.Files.Add(file);
      
      Functions.Module.ExportSignature(version, fileName, folder, signature, zipModels, mainFolder);
    }
    
    /// <summary>
    /// Выгрузка подписи.
    /// </summary>
    /// <param name="version">Версия документа.</param>
    /// <param name="fileName">Имя файла подписи.</param>
    /// <param name="folder">Папка для выгрузки.</param>
    /// <param name="signature">Подпись.</param>
    /// <param name="zipModels">Модель выгрузки.</param>
    /// <param name="mainFolder">Полная структура папок.</param>
    public virtual void ExportSignature(Sungero.Content.IElectronicDocumentVersions version, string fileName, Structures.Module.ExportedFolder folder,
                                        Sungero.Domain.Shared.ISignature signature, List<Structures.Module.ZipModel> zipModels, Structures.Module.ExportedFolder mainFolder)
    {
      if (signature != null)
      {
        var signData = signature.GetDataSignature();
        var signFullFileName = fileName + "SGN" + ".sgn";
        
        if (zipModels != null)
        {
          var zipModel = Structures.Module.ZipModel.Create();
          zipModel.DocumentId = version.ElectronicDocument.Id;
          zipModel.VersionId = version.Id;
          zipModel.FileName = signFullFileName;
          zipModel.FolderRelativePath = Functions.Module.GetFolderRelativePath(mainFolder);
          zipModel.SignatureId = signature.Id;
          zipModel.Size = signData.LongLength;
          zipModels.Add(zipModel);
          signData = null;
        }
        
        var file = Structures.Module.ExportedFile.Create(-1, signFullFileName, signData, null, null);
        folder.Files.Add(file);
      }
    }
    
    /// <summary>
    /// Отчет о выгрузке.
    /// </summary>
    /// <param name="exportModels">Данные по выгруженным документам.</param>
    /// <param name="pathToRoot">Путь до основной папки выгрузки.</param>
    /// <returns>Guid сформированного отчета.</returns>
    [Remote]
    public virtual string GenerateFinArchiveExportReport(List<Sungero.Docflow.Structures.Module.ExportedDocument> exportModels, string pathToRoot)
    {
      var exportTable = new List<Sungero.Docflow.Structures.Module.ExportReport>();
      var orderId = 0;
      var reportSessionId = System.Guid.NewGuid().ToString();
      foreach (var d in exportModels
               .OrderBy(t => t.IsFaulted)
               .ThenBy(t => t.Id))
      {
        var exportReportModel = Structures.Module.ExportReport.Create();
        exportReportModel.ReportSessionId = reportSessionId;
        exportReportModel.Id = d.Id;
        exportReportModel.Document = d.Name;
        exportReportModel.Hyperlink = Hyperlinks.Get(AccountingDocumentBases.Info, d.Id);
        
        if (d.IsFaulted)
        {
          exportReportModel.Exported = FinancialArchive.Reports.Resources.FinArchiveExportReport.No;
          exportReportModel.Note = d.Error;
          exportReportModel.IOHyperlink = string.Empty;
        }
        else
        {
          exportReportModel.Exported = FinancialArchive.Reports.Resources.FinArchiveExportReport.Yes;
          Functions.Module.FillDocumentPathAndNote(exportReportModel, d.Folder, pathToRoot, d.IsPrint, d.Id);
        }
        
        exportReportModel.OrderId = ++orderId;
        exportTable.Add(exportReportModel);
      }
      Functions.Module.WriteStructuresToTable(Sungero.FinancialArchive.PublicConstants.FinArchiveExportReport.SourceTableName, exportTable);
      
      return reportSessionId;
    }
    
    /// <summary>
    /// Заполнить ссылку и примечание по выгруженному документу.
    /// </summary>
    /// <param name="exportReportModel">Информация о документе для отчета.</param>
    /// <param name="folder">Папка выгрузки.</param>
    /// <param name="rootPath">Путь до выгруженных документов.</param>
    /// <param name="isPrint">Признак, выгрузка для печати или нет.</param>
    /// <param name="id">Ид файла.</param>
    public virtual void FillDocumentPathAndNote(Structures.Module.ExportReport exportReportModel, Structures.Module.ExportedFolder folder, string rootPath, bool isPrint, int id)
    {
      var currentPath = System.IO.Path.Combine(rootPath, folder.FolderName);
      if (folder.Folders.Any())
      {
        Functions.Module.FillDocumentPathAndNote(exportReportModel, folder.Folders.First(), currentPath, isPrint, id);
        return;
      }
      
      if (!folder.Files.Any())
        return;
      
      if (!isPrint)
      {
        exportReportModel.IOHyperlink = currentPath;
        exportReportModel.Note = FinancialArchive.Reports.Resources.FinArchiveExportReport.OpenFolder;
      }
      else
      {
        exportReportModel.IOHyperlink = System.IO.Path.Combine(currentPath, folder.Files.Where(x => x.Id == id).FirstOrDefault().FileName);
        exportReportModel.Note = FinancialArchive.Reports.Resources.FinArchiveExportReport.OpenFile;
      }
    }
    
    /// <summary>
    /// Поиск документов для выгрузки.
    /// </summary>
    /// <param name="filter">Фильтры поиска.</param>
    /// <returns>Найденные документы.</returns>
    [Remote, Public]
    public virtual IQueryable<Docflow.IOfficialDocument> SearchByRequisites(Docflow.Structures.Module.IExportDialogSearch filter)
    {
      var officialDocuments = OfficialDocuments.GetAll(d => ContractualDocumentBases.Is(d) || AccountingDocumentBases.Is(d)).Where(d => !Contracts.IncomingInvoices.Is(d));
      
      if (filter.Counterparty != null)
      {
        officialDocuments = officialDocuments.Where(d => (AccountingDocumentBases.Is(d) && Equals(AccountingDocumentBases.As(d).Counterparty, filter.Counterparty) ||
                                                          ContractualDocumentBases.Is(d) && Equals(ContractualDocumentBases.As(d).Counterparty, filter.Counterparty)));
      }
      
      if (filter.BusinessUnit != null)
        officialDocuments = officialDocuments.Where(d => Equals(d.BusinessUnit, filter.BusinessUnit));
      
      if (filter.DocumentKinds != null && filter.DocumentKinds.Any())
        officialDocuments = officialDocuments.Where(d => filter.DocumentKinds.Contains(d.DocumentKind));
      
      if (filter.Contract != null)
      {
        // Вниз по дереву ссылок.
        var allRelated = new List<int>() { filter.Contract.Id };
        var lastRelated = allRelated.ToList();
        while (true)
        {
          var newRelated = Docflow.OfficialDocuments.GetAll(a => a.LeadingDocument != null && lastRelated.Contains(a.LeadingDocument.Id)).Select(d => d.Id).ToList();
          lastRelated = newRelated.Except(allRelated).ToList();
          
          // Пока что-то находится - ищем.
          if (lastRelated.Any())
            allRelated.AddRange(lastRelated);
          else
            break;
        }
        
        officialDocuments = officialDocuments.Where(d => allRelated.Contains(d.Id));
      }
      
      if (filter.From.HasValue)
        officialDocuments = officialDocuments.Where(q => q.RegistrationDate != null && q.RegistrationDate >= filter.From);
      
      if (filter.To.HasValue)
        officialDocuments = officialDocuments.Where(q => q.RegistrationDate != null && q.RegistrationDate <= filter.To);
      return officialDocuments;
    }
    
    /// <summary>
    /// Получить версию документа для его выгрузки.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Версия документа.</returns>
    public virtual Sungero.Content.IElectronicDocumentVersions GetExportedDocumentVersion(IOfficialDocument document)
    {
      return document.LastVersion;
    }

    #endregion
    
    /// <summary>
    /// Получить подписывающего.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Сотрудник.</returns>
    public virtual IEmployee GetPerformerSignatory(IApprovalTask task)
    {
      var taskSignatory = task.Signatory;
      
      if (taskSignatory == null)
      {
        var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
        if (document != null)
        {
          var allowedSignatories = Functions.OfficialDocument.GetSignatories(document);
          var ourSignatory = document.OurSignatory;
          if (ourSignatory != null && allowedSignatories.Any(s => Equals(s.EmployeeId, ourSignatory.Id)))
            taskSignatory = ourSignatory;
          else
            taskSignatory = Docflow.Functions.OfficialDocument.GetDefaultSignatory(document, allowedSignatories);
        }
      }
      
      return taskSignatory;
    }
  }
}