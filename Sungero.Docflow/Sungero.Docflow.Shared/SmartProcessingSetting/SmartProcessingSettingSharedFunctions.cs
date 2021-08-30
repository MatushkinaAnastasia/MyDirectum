using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SmartProcessingSetting;
using MessageTypes = Sungero.Docflow.Constants.SmartProcessingSetting.SettingsValidationMessageTypes;
using SettingsValidationMessageStructure = Sungero.Docflow.Structures.SmartProcessingSetting.SettingsValidationMessage;

namespace Sungero.Docflow.Shared
{
  partial class SmartProcessingSettingFunctions
  {
    /// <summary>
    /// Получить настройки интеллектуальной обработки документов.
    /// </summary>
    /// <returns>Настройки.</returns>
    [Public]
    public static ISmartProcessingSetting GetSettings()
    {
      return SmartProcessingSettings.GetAllCached().SingleOrDefault();
    }
    
    /// <summary>
    /// Получить адрес сервиса Арио.
    /// </summary>
    /// <returns>Адрес Арио.</returns>
    [Public]
    public static string GetArioUrl()
    {
      var smartProcessingSettings = Docflow.PublicFunctions.SmartProcessingSetting.GetSettings();
      var arioUrl = smartProcessingSettings.ArioUrl;
      return arioUrl;
    }
    
    /// <summary>
    /// Проверить, есть ли интеллектуальная обработка.
    /// </summary>
    /// <returns>True - если есть интеллектуальная обработка, иначе - false.</returns>
    /// <remarks>Проверяется по заполненности адреса сервиса Арио.</remarks>
    [Public]
    public static bool SmartProcessingIsEnabled()
    {
      return !string.IsNullOrWhiteSpace(GetArioUrl());
    }
    
    /// <summary>
    /// Получить ответственного по имени линии.
    /// </summary>
    /// <param name="senderLineName">Наименование линии.</param>
    /// <returns>Ответственный.</returns>
    [Public]
    public virtual Sungero.Company.IEmployee GetDocumentProcessingResponsible(string senderLineName)
    {
      var recipient = _obj.CaptureSources.Where(x => x.SenderLineName.Trim() == senderLineName.Trim()).Select(x => x.Responsible).FirstOrDefault();
      var responsible = Company.Employees.Null;
      if (Company.Employees.Is(recipient))
        responsible = Company.Employees.As(recipient);
      
      if (Roles.Is(recipient))
        responsible = Company.Employees.As(Roles.As(recipient).RecipientLinks.FirstOrDefault().Member);
      
      return responsible;
    }
    
    /// <summary>
    /// Получить источники с неуникальными именами.
    /// </summary>
    /// <returns>Список источников.</returns>
    public virtual List<ISmartProcessingSettingCaptureSources> GetNotUniqueNameSources()
    {
      var captureSources = _obj.CaptureSources;
      var notUniqueSenderLineNames = captureSources.GroupBy(x => x.SenderLineName).Where(s => s.Count() > 1).Select(x => x.Key);
      var notUnique = _obj.CaptureSources.Where(n => notUniqueSenderLineNames.Contains(n.SenderLineName));
      
      return notUnique.Any() ? notUnique.ToList() : null;
    }
    
    /// <summary>
    /// Проверить наименование линии.
    /// </summary>
    /// <param name="senderLineName">Имя линии.</param>
    /// <returns>Пустая строка, если имя линии в порядке.
    /// Иначе текст ошибки.</returns>
    [Public]
    public static string ValidateSenderLineName(string senderLineName)
    {
      if (!string.IsNullOrEmpty(senderLineName) && !System.Text.RegularExpressions.Regex.IsMatch(senderLineName.Trim(), @"(^[0-9a-zA-Z]{0,}$)"))
        return SmartProcessingSettings.Resources.InvalidSenderLineName;
      
      return null;
    }
    
    /// <summary>
    /// Проверить адрес сервиса Ario.
    /// </summary>
    /// <returns>Тип и текст ошибки, если она была обнаружена.</returns>
    public virtual Structures.SmartProcessingSetting.SettingsValidationMessage ValidateArioUrl()
    {
      // Проверка, что адрес Ario не "кривой".
      if (!System.Uri.IsWellFormedUriString(_obj.ArioUrl, UriKind.Absolute))
        return SettingsValidationMessageStructure.Create(MessageTypes.Error, SmartProcessingSettings.Resources.InvalidArioUrl);
      
      if (!Docflow.PublicFunctions.SmartProcessingSetting.Remote.CheckConnection(_obj))
        return SettingsValidationMessageStructure.Create(MessageTypes.SoftError, SmartProcessingSettings.Resources.ArioConnectionError);
      
      return null;
    }
    
    /// <summary>
    /// Проверить классификаторы.
    /// </summary>
    /// <returns>Тип и текст ошибки, если она была обнаружена.</returns>
    public virtual Structures.SmartProcessingSetting.SettingsValidationMessage ValidateClassifiers()
    {
      if (!_obj.FirstPageClassifierId.HasValue || !_obj.TypeClassifierId.HasValue)
        return SettingsValidationMessageStructure.Create(MessageTypes.SoftError, SmartProcessingSettings.Resources.SetCorrectClassifiers);
      
      var arioClassifiersExist = Functions.SmartProcessingSetting.Remote.IsArioClassifiersExist(_obj);
      
      if (!arioClassifiersExist)
        return SettingsValidationMessageStructure.Create(MessageTypes.SoftError, SmartProcessingSettings.Resources.SetCorrectClassifiers);
      
      if (_obj.FirstPageClassifierId.Value == _obj.TypeClassifierId.Value)
        return SettingsValidationMessageStructure.Create(MessageTypes.Warning, SmartProcessingSettings.Resources.SetCorrectClassifiers);
      
      return null;
    }
    
    /// <summary>
    /// Проверить границы доверия к извлечённым фактам.
    /// </summary>
    /// <returns>Тип и текст ошибки, если она была обнаружена.</returns>
    public virtual Structures.SmartProcessingSetting.SettingsValidationMessage ValidateConfidenceLimits()
    {
      // Нижняя граница 0..99 включительно и < верхней. Верхняя 1..100 включительно.
      if (_obj.LowerConfidenceLimit >= 0 && _obj.LowerConfidenceLimit < _obj.UpperConfidenceLimit &&
          _obj.UpperConfidenceLimit <= 100)
        return null;
      
      // Однотипная ошибка для всех случаев.
      return Structures.SmartProcessingSetting.SettingsValidationMessage.Create(MessageTypes.Error, SmartProcessingSettings.Resources.SetCorrectConfidenceLimits);
    }
    
    /// <summary>
    /// Проверить настройки.
    /// </summary>
    [Public]
    public virtual void ValidateSettings()
    {
      var arioUrlValidationMessage = Functions.SmartProcessingSetting.ValidateArioUrl(_obj);
      if (arioUrlValidationMessage != null)
        throw AppliedCodeException.Create(arioUrlValidationMessage.Text);
      
      var classifiersValidationMessage = Functions.SmartProcessingSetting.ValidateClassifiers(_obj);
      if (classifiersValidationMessage != null)
      {
        if (classifiersValidationMessage.Type == MessageTypes.Warning)
          Logger.Debug(classifiersValidationMessage.Text);
        else
          throw AppliedCodeException.Create(classifiersValidationMessage.Text);
      }
    }
    
    /// <summary>
    /// Проверить, что существует настройка для линии поступления документов.
    /// </summary>
    /// <param name="senderLineName">Имя линии.</param>
    [Public]
    public virtual void ValidateSenderLineSettingExisting(string senderLineName)
    {
      var senderLineSetting = _obj.CaptureSources
        .Where(x => x.SenderLineName.Trim() == senderLineName.Trim())
        .FirstOrDefault();
      
      if (senderLineSetting == null)
        throw AppliedCodeException.Create(SmartProcessingSettings.Resources.NotFoundSenderLineNameFormat(senderLineName));
    }
    
    /// <summary>
    /// Получить ИД дополнительных классификаторов.
    /// </summary>
    /// <returns>Список ИД дополнительных классификаторов.</returns>
    [Public]
    public virtual List<string> GetAdditionalClassifierIds()
    {
      var additionalClassifierIds = new List<string>();
      foreach (var additionalClassifier in _obj.AdditionalClassifiers)
      {
        if (additionalClassifier.ClassifierId != null)
          additionalClassifierIds.Add(additionalClassifier.ClassifierId.ToString());
      }
      return additionalClassifierIds;
    }
    
    /// <summary>
    /// Проверить логин.
    /// </summary>
    /// <param name="login">Логин.</param>
    /// <returns>Пустая строка, если логин в порядке.
    /// Иначе текст ошибки.</returns>
    [Public]
    public static string ValidateLogin(string login)
    {
      if (!string.IsNullOrEmpty(login) && !System.Text.RegularExpressions.Regex.IsMatch(login.Trim(), @"(^[0-9a-zA-Z_\-]{0,}$)"))
        return Sungero.Docflow.SmartProcessingSettings.Resources.InvalidLogin;
      
      return string.Empty;
    }
  }
}