using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Shared;
using Sungero.SmartProcessing.Structures.Module;
using DcsInstanceInfosTagNames = Sungero.SmartProcessing.Constants.Module.DcsInstanceInfosTagNames;

namespace Sungero.SmartProcessing.Client
{
  public class ModuleFunctions
  {
    
    /// <summary>
    /// Обработать пакет документов со сканера или с почты.
    /// </summary>
    /// <param name="senderLineName">Наименование линии.</param>
    /// <param name="instanceInfosXmlPath">Путь к xml файлу DCS c информацией об экземплярах захвата и о захваченных файлах.</param>
    /// <param name="deviceInfoXmlPath">Путь к xml файлу DCS c информацией об устройствах ввода.</param>
    /// <param name="inputFilesXmlPath">Путь к xml файлу DCS c информацией об импортируемых файлах.</param>
    /// <param name="packageFolderPath">Путь к папке хранения файлов, переданных в пакете.</param>
    public virtual void ProcessCapturedPackage(string senderLineName, string instanceInfosXmlPath,
                                               string deviceInfoXmlPath, string inputFilesXmlPath,
                                               string packageFolderPath)
    {
      this.ValidateSettings(senderLineName, instanceInfosXmlPath, deviceInfoXmlPath,
                            inputFilesXmlPath, packageFolderPath);
      
      var blobPackage = this.PrepareDcsPackage(senderLineName, instanceInfosXmlPath, deviceInfoXmlPath,
                                               inputFilesXmlPath, packageFolderPath);
      
      this.ProcessPackageInArio(blobPackage);
      
      Sungero.SmartProcessing.Functions.Module.Remote.ProcessCapturedPackage(blobPackage);
    }
    
    #region Валидация настройки
    
    /// <summary>
    /// Проверить корректность настройки.
    /// </summary>
    /// <param name="senderLineName">Наименование линии.</param>
    /// <param name="instanceInfosXmlPath">Путь к xml файлу DCS c информацией об экземплярах захвата и о захваченных файлах.</param>
    /// <param name="deviceInfoXmlPath">Путь к xml файлу DCS c информацией об устройствах ввода.</param>
    /// <param name="inputFilesXmlPath">Путь к xml файлу DCS c информацией об импортируемых файлах.</param>
    /// <param name="packageFolderPath">Путь к папке хранения файлов, переданных в пакете.</param>
    /// <exception cref="AppliedCodeException">Настройки интеллектуальной обработки документов не найдены
    /// или не найдены настройки для линии.</exception>
    [Public]
    public virtual void ValidateSettings(string senderLineName, string instanceInfosXmlPath,
                                         string deviceInfoXmlPath, string inputFilesXmlPath,
                                         string packageFolderPath)
    {
      // Проверить общие настройки.
      var smartProcessingSettings = Sungero.Docflow.PublicFunctions.SmartProcessingSetting.GetSettings();
      if (smartProcessingSettings == null)
        throw AppliedCodeException.Create(Resources.SmartProcessingSettingsNotFound);
      Sungero.Docflow.PublicFunctions.SmartProcessingSetting.ValidateSettings(smartProcessingSettings);
      Logger.Debug("Validate settings. Smart processing settings validation completed successfully.");
      
      // Проверить, что линия настроена и для нее есть ответственный.
      Logger.DebugFormat("Validate settings. Sender line name: {0}", senderLineName);
      Sungero.Docflow.PublicFunctions.SmartProcessingSetting.ValidateSenderLineSettingExisting(smartProcessingSettings, senderLineName);
      Logger.Debug("Validate settings. Sender line setting validation completed successfully.");
    }
    
    #endregion
    
    #region Обработка в DCS
    
    /// <summary>
    /// Сформировать пакет документов из DCS.
    /// </summary>
    /// <param name="senderLineName">Наименование линии.</param>
    /// <param name="instanceInfosXmlPath">Путь к xml файлу DCS c информацией об экземплярах захвата и о захваченных файлах.</param>
    /// <param name="deviceInfoXmlPath">Путь к xml файлу DCS c информацией об устройствах ввода.</param>
    /// <param name="inputFilesXmlPath">Путь к xml файлу DCS c информацией об импортируемых файлах.</param>
    /// <param name="packageFolderPath">Путь к папке хранения файлов, переданных в пакете.</param>
    /// <returns>Справочник с пакетом документов из DCS.</returns>
    [Public]
    public virtual IBlobPackage PrepareDcsPackage(string senderLineName, string instanceInfosXmlPath,
                                                  string deviceInfoXmlPath, string inputFilesXmlPath,
                                                  string packageFolderPath)
    {
      Logger.Debug("Begin of prepare package from DCS to Ario...");
      
      // Получить основную информацию о пакете документов из DCS.
      var blobPackage = Functions.BlobPackage.Remote.CreateBlobPackage();
      blobPackage.SenderLine = senderLineName;
      if (this.GetSourceType(deviceInfoXmlPath) == Constants.Module.CaptureSourceType.Mail)
      {
        blobPackage.SourceType = BlobPackage.SourceType.Mail;
        this.FillMailInfo(blobPackage, instanceInfosXmlPath);
      }
      else
        blobPackage.SourceType = BlobPackage.SourceType.Folder;
      blobPackage.PackageFolderPath = packageFolderPath;

      blobPackage = this.FillBlobs(blobPackage, inputFilesXmlPath, packageFolderPath);
      if (!blobPackage.Blobs.Any() && blobPackage.MailBodyBlob == null)
        throw AppliedCodeException.Create(Resources.EmptyScanPackage);
      
      blobPackage.Save();
      
      Logger.Debug("Captured Package Process. Package preparation completed successfully.");
      
      return blobPackage;
    }
    
    /// <summary>
    /// Получить тип источника захвата.
    /// </summary>
    /// <param name="deviceInfoXmlPath">Путь к xml файлу DCS c информацией об устройствах ввода.</param>
    /// <returns>Тип источника захвата.</returns>
    [Public]
    public virtual string GetSourceType(string deviceInfoXmlPath)
    {
      if (!File.Exists(deviceInfoXmlPath))
        throw AppliedCodeException.Create(Resources.NoFilesInfoInPackage);
      
      var filesXDoc = System.Xml.Linq.XDocument.Load(deviceInfoXmlPath);
      var element = filesXDoc.Element(Constants.Module.DcsDeviceInfoTagNames.MailSourceInfo);
      if (element != null)
        return Constants.Module.CaptureSourceType.Mail;
      return Constants.Module.CaptureSourceType.Folder;
    }
    
    /// <summary>
    /// Заполнить информацию о документах пакета.
    /// </summary>
    /// <param name="blobPackage">Пакет документов.</param>
    /// <param name="inputFilesXmlPath">Путь к xml файлу DCS c информацией об импортируемых файлах.</param>
    /// <param name="packageFolderPath">Путь к папке хранения файлов, переданных в пакете.</param>
    /// <returns>Пакет документов с заполненной информацией о документах пакета.</returns>
    /// <remarks>Технически возможно, что документов будет несколько, но на практике приходит один.</remarks>
    [Public]
    public virtual IBlobPackage FillBlobs(IBlobPackage blobPackage, string inputFilesXmlPath,
                                          string packageFolderPath)
    {
      if (!File.Exists(inputFilesXmlPath))
        throw AppliedCodeException.Create(Resources.NoFilesInfoInPackage);
      var fileDescriptionTagName = Constants.Module.DcsInputFilesTagNames.FileDescription;
      
      var filesXDoc = System.Xml.Linq.XDocument.Load(inputFilesXmlPath);
      var fileElements = filesXDoc
        .Element(Constants.Module.DcsInputFilesTagNames.InputFilesSection)
        .Element(Constants.Module.DcsInputFilesTagNames.Files)
        .Elements()
        .ToList();
      if (!fileElements.Any())
        throw AppliedCodeException.Create(Resources.NoFilesInfoInPackage);
      
      var sentByEmail = blobPackage.SourceType == BlobPackage.SourceType.Mail;
      if (sentByEmail)
      {
        var mailBodyHtmlName = Constants.Module.DcsMailBodyName.Html;
        var mailBodyTxtName = Constants.Module.DcsMailBodyName.Txt;
        
        // Тело письма.
        blobPackage.MailBodyBlob = this.GetMailBody(fileElements, packageFolderPath);
        
        // Фильтрация картинок из тела письма.
        var htmlBodyElement = fileElements
          .FirstOrDefault(x => string.Equals(x.Element(fileDescriptionTagName).Value, mailBodyHtmlName, StringComparison.InvariantCultureIgnoreCase));
        var hasHtmlBody = htmlBodyElement != null;
        
        // Убрать тело письма (body.html или body.txt).
        fileElements = fileElements
          .Where(x => !string.Equals(x.Element(fileDescriptionTagName).Value, mailBodyHtmlName, StringComparison.InvariantCultureIgnoreCase) &&
                 !string.Equals(x.Element(fileDescriptionTagName).Value, mailBodyTxtName, StringComparison.InvariantCultureIgnoreCase))
          .ToList();
        
        if (blobPackage.MailBodyBlob != null && !string.IsNullOrEmpty(blobPackage.MailBodyBlob.FilePath) && hasHtmlBody)
          fileElements = this.FilterEmailBodyInlineImages(blobPackage.MailBodyBlob.FilePath, fileElements);
      }
      
      // Получить файлы пакета документов из DCS.
      foreach (var fileElement in fileElements)
      {
        var blob = this.CreateBlobFromXelement(fileElement, packageFolderPath, sentByEmail);
        if (blob != null)
          blobPackage.Blobs.AddNew().Blob = blob;
      }
      
      return blobPackage;
    }
    
    /// <summary>
    /// Получить тело письма.
    /// </summary>
    /// <param name="fileElements">Элементы xml c информацией об импортируемых файлах.</param>
    /// <param name="packageFolderPath">Путь к папке хранения файлов, переданных в пакете.</param>
    /// <returns>Запись справочника с телом письма и сопутствующей информацией.
    /// Если тела нет, то пустую запись справочника.</returns>
    public virtual IBlob GetMailBody(List<System.Xml.Linq.XElement> fileElements, string packageFolderPath)
    {
      var fileDescriptionTagName = Constants.Module.DcsInputFilesTagNames.FileDescription;
      var mailBodyHtmlName = Constants.Module.DcsMailBodyName.Html;
      var mailBodyTxtName = Constants.Module.DcsMailBodyName.Txt;
      
      var htmlBodyElement = fileElements
        .FirstOrDefault(x => string.Equals(x.Element(fileDescriptionTagName).Value, mailBodyHtmlName, StringComparison.InvariantCultureIgnoreCase));
      var txtBodyElement = fileElements
        .FirstOrDefault(x => string.Equals(x.Element(fileDescriptionTagName).Value, mailBodyTxtName, StringComparison.InvariantCultureIgnoreCase));
      var hasHtmlBody = htmlBodyElement != null;
      var hasTxtBody = txtBodyElement != null;
      
      // Не создавать документ для писем с пустым телом.
      // В некоторых случаях (например, при отправке из Outlook в Яндекс) для писем с пустым телом генерируется фейковое тело,
      // представляющее из себя только перевод строки. Такие тела заносить также не нужно.
      
      // Получить текст из тела письма.
      IBlob mailBodyBlob = Blobs.Null;
      var bodyText = string.Empty;
      if (hasHtmlBody)
      {
        mailBodyBlob = this.CreateBlobFromXelement(htmlBodyElement, packageFolderPath, true);
        bodyText = AsposeExtensions.HtmlTagReader.GetTextFromHtml(mailBodyBlob.FilePath);
      }
      else if (hasTxtBody)
      {
        mailBodyBlob = this.CreateBlobFromXelement(txtBodyElement, packageFolderPath, true);
        bodyText = File.ReadAllText(mailBodyBlob.FilePath);
      }
      
      // Очистить текст из тела письма от спецсимволов, чтобы определить, пуст ли он.
      var clearBodyText = bodyText.Trim(new[] { ' ', '\r', '\n', '\0' });
      if (!string.IsNullOrWhiteSpace(clearBodyText))
        return mailBodyBlob;
      
      return Blobs.Null;
    }
    
    /// <summary>
    /// Создать информацию о файле на основе xml элемента.
    /// </summary>
    /// <param name="xmlElement">Xml элемент.</param>
    /// <param name="packageFolderPath">Путь к папке хранения файлов, переданных в пакете.</param>
    /// <param name="sentByEmail">Признак, что пакет захвачен с почты.</param>
    /// <returns>Информация о файле.</returns>
    public virtual IBlob CreateBlobFromXelement(System.Xml.Linq.XElement xmlElement, string packageFolderPath, bool sentByEmail)
    {
      var blob = Functions.Blob.Remote.CreateBlob();
      
      var relativeFilePath = Path.GetFileName(xmlElement.Element(Constants.Module.DcsInputFilesTagNames.FileName).Value);
      var absoluteFilePath = Path.Combine(packageFolderPath, relativeFilePath);
      
      if (File.Exists(absoluteFilePath))
      {
        blob.FilePath = absoluteFilePath;
        blob.OriginalFileName = xmlElement.Element(Constants.Module.DcsInputFilesTagNames.FileDescription).Value;
        if (sentByEmail)
          blob.Body.Write(System.IO.File.OpenRead(absoluteFilePath));
      }
      else
        Logger.Error(Resources.FileNotFoundFormat(absoluteFilePath));
      
      blob.Save();
      
      return blob;
    }
    
    /// <summary>
    /// Отфильтровать изображения, пришедшие в теле письма.
    /// </summary>
    /// <param name="htmlBodyPath">Путь до тела письма.</param>
    /// <param name="attachments">Вложения.</param>
    /// <returns>Отфильтрованный список вложений.</returns>
    public virtual List<System.Xml.Linq.XElement> FilterEmailBodyInlineImages(string htmlBodyPath, List<System.Xml.Linq.XElement> attachments)
    {
      var inlineImagesCount = AsposeExtensions.HtmlTagReader.GetInlineImagesCount(htmlBodyPath);
      return attachments.Skip(inlineImagesCount).ToList();
    }
    
    /// <summary>
    /// Заполнить информацию о захваченном письме.
    /// </summary>
    /// <param name="blobPackage">Пакет с данными о захваченных файлах.</param>
    /// <param name="instanceInfosXmlPath">Путь к xml файлу DCS c информацией об экземплярах захвата и о захваченных файлах.</param>
    [Public]
    public virtual void FillMailInfo(IBlobPackage blobPackage, string instanceInfosXmlPath)
    {
      if (!File.Exists(instanceInfosXmlPath))
        throw AppliedCodeException.Create(Resources.FileNotFoundFormat(instanceInfosXmlPath));
      
      var infoXDoc = System.Xml.Linq.XDocument.Load(instanceInfosXmlPath);
      if (infoXDoc == null)
        return;
      
      var mailCaptureInstanceInfoElement = infoXDoc
        .Element(DcsInstanceInfosTagNames.CaptureInstanceInfoList)
        .Element(DcsInstanceInfosTagNames.MailCaptureInstanceInfo);
      
      if (mailCaptureInstanceInfoElement == null)
        return;
      
      // Тема письма.
      var subject = this.GetAttributeStringValue(mailCaptureInstanceInfoElement, DcsInstanceInfosTagNames.Subject);
      if (subject.Length > blobPackage.Info.Properties.Subject.Length)
        subject = subject.Substring(0, blobPackage.Info.Properties.Subject.Length);
      blobPackage.Subject = subject;
      
      // Отправитель.
      var fromElement = mailCaptureInstanceInfoElement.Element(DcsInstanceInfosTagNames.From);
      if (fromElement != null)
      {
        blobPackage.FromAddress = this.GetAttributeStringValue(fromElement, DcsInstanceInfosTagNames.ContactAttributes.Address);
        blobPackage.FromName = this.GetAttributeStringValue(fromElement, DcsInstanceInfosTagNames.ContactAttributes.Name);
      }
      
      // Получатели.
      var mailToElement = mailCaptureInstanceInfoElement.Element(DcsInstanceInfosTagNames.To);
      if (mailToElement != null)
      {
        var recipients = mailToElement.Elements(DcsInstanceInfosTagNames.Recipient);
        foreach (var recipient in recipients)
        {
          var mailToRecipient = blobPackage.To.AddNew();
          mailToRecipient.Name = this.GetAttributeStringValue(recipient, DcsInstanceInfosTagNames.ContactAttributes.Name);
          mailToRecipient.Address = this.GetAttributeStringValue(recipient, DcsInstanceInfosTagNames.ContactAttributes.Address);
        }
      }
      
      // Получатели копии.
      var copyElement = mailCaptureInstanceInfoElement.Element(DcsInstanceInfosTagNames.CC);
      if (copyElement != null)
      {
        var recipients = copyElement.Elements(DcsInstanceInfosTagNames.Recipient);
        foreach (var recipient in recipients)
        {
          var copyRecipient = blobPackage.CC.AddNew();
          copyRecipient.Name = this.GetAttributeStringValue(recipient, DcsInstanceInfosTagNames.ContactAttributes.Name);
          copyRecipient.Address = this.GetAttributeStringValue(recipient, DcsInstanceInfosTagNames.ContactAttributes.Address);
        }
      }
      
      blobPackage.Save();
    }
    
    /// <summary>
    /// Получить значение атрибута XElement.
    /// </summary>
    /// <param name="element">XElement.</param>
    /// <param name="attributeName">Имя атрибута.</param>
    /// <returns>Строковое значение атрибута. null, если атрибут отсутствует.</returns>
    public virtual string GetAttributeStringValue(System.Xml.Linq.XElement element, string attributeName)
    {
      var attribute = element.Attribute(attributeName);
      if (attribute != null)
        return attribute.Value;
      return string.Empty;
    }
    
    #endregion
    
    #region Обработка в Ario
    
    /// <summary>
    /// Обработать пакет документов из DCS в Ario.
    /// </summary>
    /// <param name="blobPackage">Пакет документов.</param>
    [Public]
    public virtual void ProcessPackageInArio(IBlobPackage blobPackage)
    {
      // Получение настроек.
      var smartProcessingSettings = Sungero.Docflow.PublicFunctions.SmartProcessingSetting.GetSettings();
      var firstPageClassifierId = smartProcessingSettings.FirstPageClassifierId.ToString();
      var typeClassifierId = smartProcessingSettings.TypeClassifierId.ToString();
      Logger.DebugFormat("First page classifier: name - \"{0}\", id - {1}.", smartProcessingSettings.FirstPageClassifierName, firstPageClassifierId);
      Logger.DebugFormat("Type classifier: name - \"{0}\", id - {1}.", smartProcessingSettings.TypeClassifierName, typeClassifierId);
      
      // Получить соответствие класса и наименования правила извлечения фактов.
      var processingRule = smartProcessingSettings.ProcessingRules
        .Where(x => !string.IsNullOrWhiteSpace(x.ClassName) && !string.IsNullOrWhiteSpace(x.GrammarName))
        .ToDictionary(x => x.ClassName, x => x.GrammarName);

      // Получение доп. классификаторов.
      var additionalClassifierIds = Sungero.Docflow.PublicFunctions.SmartProcessingSetting.GetAdditionalClassifierIds(smartProcessingSettings);
      
      // Обработка в Ario.
      var arioConnector = this.GetArioConnector();
      var blobs = blobPackage.Blobs.Select(x => x.Blob);
      foreach (var blob in blobs)
      {
        var fileName = blob.OriginalFileName;
        var filePath = blob.FilePath;
        
        if (!this.CanArioProcessFile(filePath))
        {
          Logger.DebugFormat("File extension is not supported by Ario; {0} will be uploaded as a simple document.", fileName);
          continue;
        }
        
        try
        {
          Logger.DebugFormat("Begin classification and facts extraction. File: {0}", fileName);
          var arioResultJson = arioConnector.ClassifyAndExtractFacts(File.ReadAllBytes(filePath),
                                                                     fileName,
                                                                     typeClassifierId,
                                                                     firstPageClassifierId,
                                                                     processingRule,
                                                                     additionalClassifierIds);
          Logger.DebugFormat("End classification and facts extraction. File: {0}", fileName);
          blob.ArioResultJson = arioResultJson;
          blob.Save();
        }
        catch (ExternalException ex)
        {
          Logger.DebugFormat("An error has occurred during classification and facts extraction. File: {0}", ex, fileName);
          throw ex;
        }
      }
    }
    
    /// <summary>
    /// Получить коннект к Ario.
    /// </summary>
    /// <returns>Коннект к Ario.</returns>
    public Sungero.ArioExtensions.ArioConnector GetArioConnector()
    {
      var smartProcessingSettings = Sungero.Docflow.PublicFunctions.SmartProcessingSetting.GetSettings();
      var timeout = new TimeSpan(0, 0, Docflow.PublicFunctions.SmartProcessingSetting.Remote.GetArioConnectionTimeoutInSeconds());
      var connector = new ArioExtensions.ArioConnector(smartProcessingSettings.ArioUrl,
                                                       timeout,
                                                       !string.IsNullOrEmpty(smartProcessingSettings.Login));
      
      // Задать токен.
      var token = Sungero.Docflow.PublicFunctions.SmartProcessingSetting.Remote.GetArioToken(smartProcessingSettings);
      connector.AuthenticationToken = token;
      
      // Подписка на событие обновления токена.
      connector.TokenExpired += this.UpdateToken;
      return connector;
    }
    
    /// <summary>
    /// Событие обновления токена.
    /// </summary>
    /// <param name="sender">Объект ArioConnector, который инициировал событие обновления токена.</param>
    /// <param name="args">Аргументы события, содержащие признак, что токен был обновлен.</param>
    public void UpdateToken(object sender, ArioExtensions.Authorization.TokenExpiredEventArgs args)
    {
      var smartProcessingSettings = Sungero.Docflow.PublicFunctions.SmartProcessingSetting.GetSettings();
      var token = Sungero.Docflow.PublicFunctions.SmartProcessingSetting.Remote.GetArioToken(smartProcessingSettings);
      ((ArioExtensions.ArioConnector)sender).AuthenticationToken = token;
      args.TokenUpdated = true;
    }
    
    /// <summary>
    /// Определить, может ли Ario обработать файл.
    /// </summary>
    /// <param name="fileName">Имя или путь до файла.</param>
    /// <returns>True - может, False - иначе.</returns>
    public virtual bool CanArioProcessFile(string fileName)
    {
      var ext = Path.GetExtension(fileName).TrimStart('.').ToLower();
      var allowedExtensions = new List<string>()
      {
        "jpg", "jpeg", "png", "bmp", "gif",
        "tif", "tiff", "pdf", "doc", "docx",
        "dot", "dotx", "rtf", "odt", "ott",
        "txt", "xls", "xlsx", "ods"
      };
      return allowedExtensions.Contains(ext);
    }
    
    #endregion
    
    #region Задача на верификацию
    
    /// <summary>
    /// Определить ведущий документ в комплекте.
    /// </summary>
    /// <param name="documents">Комплект документов.</param>
    /// <returns>Ведущий документ.</returns>
    [Public]
    public virtual IOfficialDocument GetLeadingDocument(List<IOfficialDocument> documents)
    {
      var documentPriority = new Dictionary<IOfficialDocument, int>();
      var documentTypePriorities = Functions.Module.GetPackageDocumentTypePriorities();
      int priority;
      foreach (var document in documents)
      {
        documentTypePriorities.TryGetValue(document.Info.GetType().GetFinalType(), out priority);
        documentPriority.Add(document, priority);
      }
      
      var leadingDocument = documentPriority
        .OrderByDescending(p => p.Value)
        .FirstOrDefault().Key;
      return leadingDocument;
    }
    
    /// <summary>
    /// Вызвать диалог удаления документов.
    /// </summary>
    /// <param name="documentList">Документы для удаления.</param>
    /// <returns>Список ID удаленных документов.</returns>
    public static List<int> DeleteDocumentsDialogInWeb(List<IOfficialDocument> documentList)
    {
      var step = 1;
      var successfullyDeletedDocumentIds = new List<int>();
      var deleteWithExceptionDocuments = new List<IOfficialDocument>();
      
      var dialog = Dialogs.CreateInputDialog(VerificationAssignments.Resources.DeleteDocumentsDialogTitle);
      dialog.HelpCode = Constants.VerificationAssignment.HelpCodes.DeleteDocumentsDialog;
      dialog.Height = 80;
      
      var selectedDocuments = dialog
        .AddSelectMany(VerificationAssignments.Resources.DeleteDocumentsDialogAttachments, true, OfficialDocuments.Null)
        .From(documentList);
      selectedDocuments.IsVisible = false;
      var deleteButton = dialog.Buttons.AddCustom(Sungero.SmartProcessing.Resources.DeleteDocumentsDialogDeleteButtonName);
      deleteButton.IsVisible = false;
      
      Action showTroublesHandler = () =>
      {
        deleteWithExceptionDocuments.ShowModal();
      };
      var showTroubles = dialog.AddHyperlink(Sungero.SmartProcessing.Resources.DeleteDocumentDialogDeletingExceptionDocumentsHyperlinkTitle);
      showTroubles.SetOnExecute(showTroublesHandler);
      showTroubles.IsVisible = false;
      
      var cancelButton = dialog.Buttons.AddCancel();
      
      #region Dialog Refresh Handler
      
      Action<CommonLibrary.InputDialogRefreshEventArgs> refreshDialogHandler = (e) =>
      {
        if (step == 1)
        {
          selectedDocuments.IsVisible = true;
          deleteButton.IsVisible = true;
          dialog.Buttons.Default = deleteButton;
          showTroubles.IsVisible = false;
          dialog.Text = VerificationAssignments.Resources.DeleteDocumentsDialogText;
          cancelButton.Name = Sungero.SmartProcessing.Resources.DeleteDocumentsDialogCancelButtonName1;
        }
        else
        {
          selectedDocuments.IsVisible = false;
          deleteButton.IsVisible = false;
          showTroubles.IsVisible = true;
          dialog.Buttons.Default = cancelButton;
          var total = selectedDocuments.Value.Count();
          var failed = deleteWithExceptionDocuments.Count();
          var success = total - failed;
          dialog.Text = Sungero.SmartProcessing.Resources.DeleteDocumentsDialogDeletionTotalsFormat(total, Environment.NewLine);
          dialog.Text += Sungero.SmartProcessing.Resources.DeleteDocumentsDialogDeletionSuccessFormat(success, Environment.NewLine);
          dialog.Text += Sungero.SmartProcessing.Resources.DeleteDocumentsDialogDeletionFailedFormat(failed, Environment.NewLine);
          cancelButton.Name = Sungero.SmartProcessing.Resources.DeleteDocumentsDialogCancelButtonName2;
        }
      };
      
      dialog.SetOnRefresh(refreshDialogHandler);
      
      #endregion
      
      dialog.SetOnButtonClick((e) =>
                              {
                                if (e.Button == cancelButton)
                                  e.CloseAfterExecute = true;
                                
                                if (e.Button == deleteButton)
                                {
                                  if (!e.IsValid)
                                  {
                                    e.CloseAfterExecute = false;
                                    return;
                                  }
                                  
                                  successfullyDeletedDocumentIds = selectedDocuments.Value.Select(x => x.Id).ToList();
                                  
                                  deleteWithExceptionDocuments = TryDeleteDocuments(selectedDocuments.Value.ToList());
                                  if (deleteWithExceptionDocuments.Any())
                                  {
                                    e.CloseAfterExecute = false;
                                    step = 2;
                                  }
                                  else
                                  {
                                    e.CloseAfterExecute = true;
                                    Dialogs.NotifyMessage(VerificationAssignments.Resources.DeleteDocumentsDialogNoticeAfterDelete);
                                  }
                                  
                                  successfullyDeletedDocumentIds = successfullyDeletedDocumentIds.Where(x => !deleteWithExceptionDocuments.Any(y => y.Id == x)).ToList();
                                }
                              });
      
      dialog.Show();
      
      return successfullyDeletedDocumentIds;
    }
    
    /// <summary>
    /// Попытаться удалить документы.
    /// </summary>
    /// <param name="documents">Документы для удаления.</param>
    /// <returns>Документы, у которых возникли ошибки при удалении.</returns>
    public static List<IOfficialDocument> TryDeleteDocuments(List<IOfficialDocument> documents)
    {
      var deleteWithExceptionDocuments = new List<IOfficialDocument>();
      
      foreach (var document in documents)
      {
        var documentId = document.Id;
        try
        {
          Logger.DebugFormat("Verification Assignment. Action: DeleteDocuments. Try delete document: {0}", documentId);
          Sungero.Docflow.PublicFunctions.OfficialDocument.Remote.DeleteDocument(documentId);
          Logger.DebugFormat("Verification Assignment. Action: DeleteDocuments. Success. Document: {0}", documentId);
        }
        catch (Exception ex)
        {
          Logger.DebugFormat("Verification Assignment. Action: DeleteDocuments. Failed. Document: {0}{1}{2}", documentId, Environment.NewLine, ex);
          deleteWithExceptionDocuments.Add(document);
        }
      }
      
      return deleteWithExceptionDocuments;
    }
    
    #endregion
    
    #region Настройка и тесты
    
    /// <summary>
    /// Создать классификатор.
    /// </summary>
    /// <param name="classifierName">Имя классификатора.</param>
    /// <param name="minProbability">Минимальная вероятность.</param>
    public static void CreateClassifier(string classifierName, string minProbability)
    {
      Logger.DebugFormat("Begin create classifier with name \"{0}\".", classifierName);
      try
      {
        var arioConnector = Functions.Module.GetArioConnector();
        var classifier = arioConnector.GetClassifierByName(classifierName);
        if (classifier != null)
        {
          Logger.ErrorFormat("Already exists classifier with name: \"{0}\".", classifierName);
          return;
        }
        // Некорректно обрабатывается minProbability, если использовать запятую в качестве разделителя.
        arioConnector.CreateClassifier(classifierName, minProbability.Replace(',', '.'), true);
        Logger.DebugFormat("Successfully created classifier with name \"{0}\".", classifierName);
      }
      catch (Exception e)
      {
        Logger.ErrorFormat("Create classifier error: {0}", GetInnerExceptionsMessages(e));
      }
    }
    
    /// <summary>
    /// Импорт классификатора из файла модели.
    /// </summary>
    /// <param name="classifierName">Имя классификатора.</param>
    /// <param name="filePath">Путь к файлу модели.</param>
    public static void ImportClassifierModel(string classifierName, string filePath)
    {
      Logger.DebugFormat("Begin import classifier with name \"{0}\" from folder {1}.", classifierName, filePath);
      try
      {
        var arioConnector = Functions.Module.GetArioConnector();
        var classifier = arioConnector.GetClassifierByName(classifierName);
        if (classifier == null)
        {
          Logger.ErrorFormat("Can't find classifier with name: \"{0}\".", classifierName);
          return;
        }

        arioConnector.ImportClassifierModel(classifier.Id.ToString(), filePath);
        
        if (ShowModelsInfo(classifierName))
          Logger.DebugFormat("Successfully imported classifier with name \"{0}\" from folder {1}.", classifierName, filePath);
      }
      catch (Exception e)
      {
        Logger.ErrorFormat("Import classifier error: {0}", GetInnerExceptionsMessages(e));
      }
    }
    
    /// <summary>
    /// Экспорт модели классификатора.
    /// </summary>
    /// <param name="classifierName">Имя классификатора.</param>
    /// <param name="modelId">Id модели.</param>
    /// <param name="filePath">Путь к файлу модели.</param>
    public static void ExportClassifierModel(string classifierName, string modelId, string filePath)
    {
      Logger.DebugFormat("Begin export classifier with name \"{0}\" into file {1}.", classifierName, filePath);
      try
      {
        var arioConnector = Functions.Module.GetArioConnector();
        var classifier = arioConnector.GetClassifierByName(classifierName);
        if (classifier == null)
        {
          Logger.ErrorFormat("Can't find classifier with name: \"{0}\".", classifierName);
          return;
        }

        var model = arioConnector.ExportClassifierModel(classifier.Id.ToString(), modelId);
        File.WriteAllBytes(filePath, model);
        Logger.DebugFormat("Successfully exported classifier with name \"{0}\" into file {1}.", classifierName, filePath);
      }
      catch (Exception e)
      {
        Logger.ErrorFormat("Export classifier error: {0}", GetInnerExceptionsMessages(e));
      }
    }
    
    /// <summary>
    /// Отобразить список моделей классификатора.
    /// </summary>
    /// <param name="classifierName">Имя классификатора.</param>
    public static void ShowClassifierModels(string classifierName)
    {
      Logger.DebugFormat("Begin show models for classifier with name \"{0}\".", classifierName);
      try
      {
        if (ShowModelsInfo(classifierName))
          Logger.DebugFormat("Successfully showed models for classifier with name \"{0}\".", classifierName);
      }
      catch (Exception e)
      {
        Logger.ErrorFormat("Show classifier models error: {0}", GetInnerExceptionsMessages(e));
      }
    }
    
    /// <summary>
    /// Опубликовать модель классификатора.
    /// </summary>
    /// <param name="classifierName">Имя классификатора.</param>
    /// <param name="modelId">Id модели.</param>
    public static void PublishClassifierModel(string classifierName, string modelId)
    {
      Logger.DebugFormat("Begin publish model with Id {0} for classifier with name \"{1}\".", modelId, classifierName);
      try
      {
        var arioConnector = Functions.Module.GetArioConnector();
        var classifier = arioConnector.GetClassifierByName(classifierName);
        if (classifier == null)
        {
          Logger.ErrorFormat("Can't find classifier with name: \"{0}\"", classifierName);
          return;
        }

        var model = arioConnector.PublishClassifierModel(classifier.Id.ToString(), modelId);
        if (model == null)
        {
          Logger.ErrorFormat("Error for publish model with Id {0} for classifier with name \"{1}\".", modelId, classifierName);
          return;
        }
        
        if (ShowModelsInfo(classifierName))
          Logger.DebugFormat("Successfully published model with Id {0} for classifier with name \"{1}\".", modelId, classifierName);
      }
      catch (Exception e)
      {
        Logger.ErrorFormat("Publish classifier error: {0}", GetInnerExceptionsMessages(e));
      }
    }
    
    /// <summary>
    /// Обучение классификатора.
    /// </summary>
    /// <param name="classifierName">Имя классификатора.</param>
    /// <param name="filePath">Путь к папке с dataset для обучения.</param>
    public static void TrainClassifierModel(string classifierName, string filePath)
    {
      Logger.DebugFormat("Begin train classifier with name \"{0}\" from folder {1}.", classifierName, filePath);
      try
      {
        var arioConnector = Functions.Module.GetArioConnector();
        var classifier = arioConnector.GetClassifierByName(classifierName);
        if (classifier == null)
        {
          Logger.ErrorFormat("Can't find classifier with name: \"{0}\".", classifierName);
          return;
        }

        var trainTask = arioConnector.TrainClassifierFromFolder(classifier.Id.ToString(), Path.GetFullPath(filePath));
        var trainTaskInfo = arioConnector.GetTrainTaskInfo(trainTask.Id.ToString());

        // Статусы задачи асинхронного обучения:
        // New = 0
        // InProgress = 1
        // Completed = 2
        // Error = 3
        // Trained = 4
        // Aborted = 5
        int[] stateCodes = { 2, 3, 5 };
        while (!stateCodes.Contains(trainTaskInfo.Task.State))
        {
          Logger.DebugFormat("[{0}] Training in process. Classifier name: \"{1}\", training task Id: {2}.", Calendar.Now, classifierName, trainTaskInfo.Task.Id);
          System.Threading.Thread.Sleep(10000);
          trainTaskInfo = arioConnector.GetTrainTaskInfo(trainTaskInfo.Task.Id.ToString());
        }
        
        switch (trainTaskInfo.Task.State)
        {
          case 2:
            System.Threading.Thread.Sleep(20000);
            ShowModelsInfo(classifierName);
            Logger.DebugFormat("Successful classifier training with name \"{0}\" from folder {1}.", classifierName, filePath);
            break;
          case 3:
            Logger.DebugFormat("Classifier training task with Id {0} completed with an error. {1}", trainTaskInfo.Task.Id, trainTaskInfo.Result ?? string.Empty);
            break;
          case 5:
            Logger.DebugFormat("Classifier training task with Id {0} was aborted. {1}", trainTaskInfo.Task.Id, trainTaskInfo.Result ?? string.Empty);
            break;
        }
      }
      catch (Exception e)
      {
        Logger.ErrorFormat("Training classifier error: {0}", GetInnerExceptionsMessages(e));
      }
    }
    
    /// <summary>
    /// Отобразить информацию о моделях классификатора.
    /// </summary>
    /// <param name="classifierName">Имя классификатора.</param>
    /// <returns>True, при успешном отображении.</returns>
    private static bool ShowModelsInfo(string classifierName)
    {
      var arioConnector = Functions.Module.GetArioConnector();
      var classifier = arioConnector.GetClassifierByName(classifierName);
      if (classifier == null)
      {
        Logger.ErrorFormat("Can't find classifier with name: \"{0}\".", classifierName);
        return false;
      }
      
      var models = arioConnector.GetModelsByClassifier(classifier.Id.ToString());
      
      Logger.Debug("-------------------------------------------------------------------------------------------------");
      Logger.DebugFormat("Classifier \"{0}\" with Id {1}, created {2}, min probability {3}. Models:",
                         classifier.Name, classifier.Id, classifier.Created, classifier.MinProbability);
      if (models.Any())
        foreach (var model in models)
          Logger.DebugFormat("{0} Model with Id {1}, created {2}. Train set count {3}, accuracy {4}.",
                             model.Classes != null ? "*CURRENT*" : "---------",
                             model.Id, model.Created,
                             model.Metrics.TrainSetCount, Math.Round(model.Metrics.Accuracy, 4));
        else
          Logger.Debug("Classifier has no models");
      Logger.Debug("-------------------------------------------------------------------------------------------------");
      
      return true;
    }
    
    /// <summary>
    /// Собрать цепочку InnerExceptions в одну строку.
    /// </summary>
    /// <param name="e">Исключение.</param>
    /// <returns>Строка InnerExceptions исключений.</returns>
    private static string GetInnerExceptionsMessages(Exception e)
    {
      var result = e.InnerException != null ?
        string.Concat(e.InnerException.Message.TrimEnd('.'), ". ",  GetInnerExceptionsMessages(e.InnerException)) :
        string.Empty;
      return string.IsNullOrEmpty(result) ? e.Message : result;
    }
    
    #endregion
    
  }
}