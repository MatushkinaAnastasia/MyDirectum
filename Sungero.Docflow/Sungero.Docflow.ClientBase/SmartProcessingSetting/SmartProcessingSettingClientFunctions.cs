using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SmartProcessingSetting;
using MessageTypes = Sungero.Docflow.Constants.SmartProcessingSetting.SettingsValidationMessageTypes;

namespace Sungero.Docflow.Client
{
  partial class SmartProcessingSettingFunctions
  {
    /// <summary>
    /// Задать основные настройки захвата.
    /// </summary>
    /// <param name="arioUrl">Адрес Арио.</param>
    /// <param name="lowerConfidenceLimit">Нижняя граница доверия извлеченным фактам.</param>
    /// <param name="upperConfidenceLimit">Верхняя граница доверия извлеченным фактам.</param>
    /// <param name="firstPageClassifierName">Имя классификатора первых страниц.</param>
    /// <param name="typeClassifierName">Имя классификатора по типам документов.</param>
    public static void SetSettings(string arioUrl, string lowerConfidenceLimit, string upperConfidenceLimit,
                                   string firstPageClassifierName, string typeClassifierName)
    {
      var message = Functions.SmartProcessingSetting.Remote.SetSettings(arioUrl, lowerConfidenceLimit, upperConfidenceLimit,
                                                                        firstPageClassifierName, typeClassifierName);
      if (message != null)
      {
        if (message.Type == MessageTypes.Warning)
          Logger.Debug(message.Text);
        
        if (message.Type == MessageTypes.Error || message.Type == MessageTypes.SoftError)
          throw AppliedCodeException.Create(message.Text);
      }
    }
    
    /// <summary>
    /// Запустить диалог выбора классификатора.
    /// </summary>
    /// <param name="dialogTitle">Заголовок диалога.</param>
    /// <returns>Классификатор.</returns>
    public Sungero.Docflow.Structures.SmartProcessingSetting.ClassifierForDialog RunClassifierSelectionDialog(string dialogTitle)
    {
      var resources = Sungero.Docflow.SmartProcessingSettings.Resources;
      if (!SmartProcessingSettings.AccessRights.CanUpdate())
      {
        Dialogs.ShowMessage(resources.ClassifiersSelectionNotAvailable);
        return null;
      }
      
      // Проверка адреса сервиса Ario.
      var arioUrlValidationMessage = Functions.SmartProcessingSetting.ValidateArioUrl(_obj);
      if (arioUrlValidationMessage != null)
      {
        Dialogs.ShowMessage(arioUrlValidationMessage.Text, MessageType.Information);
        return null;
      }
      
      var classifiers = Functions.SmartProcessingSetting.Remote.GetArioClassifiers(_obj);
      if (!classifiers.Any())
      {
        Dialogs.NotifyMessage(Sungero.Docflow.SmartProcessingSettings.Resources.ClassifierSelectionError);
        return null;
      }
      
      var dialog = Dialogs.CreateInputDialog(dialogTitle);
      var classifierDisplayNames = classifiers.OrderBy(x => x.Name).Select(x => resources.ClassifierDisplayNameTemplateFormat(x.Name, x.Id).ToString());
      var classifier = dialog.AddSelect(resources.Classifier, true).From(classifierDisplayNames.ToArray());
      
      dialog.SetOnRefresh(e =>
                          {
                            var selectedClassifier = dialogTitle == resources.SelectTypeClassifierDialogTitle
                              ? resources.ClassifierDisplayNameTemplateFormat(_obj.FirstPageClassifierName, _obj.FirstPageClassifierId)
                              : resources.ClassifierDisplayNameTemplateFormat(_obj.TypeClassifierName, _obj.TypeClassifierId);
                            if (classifier.Value == selectedClassifier)
                              e.AddWarning(resources.SelectedSameClassifierWarning);
                          });
      dialog.Buttons.AddOkCancel();
      dialog.Buttons.Default = DialogButtons.Ok;
      if (dialog.Show() == DialogButtons.Ok)
        return classifiers.SingleOrDefault(x => classifier.Value == resources.ClassifierDisplayNameTemplateFormat(x.Name, x.Id));
      return null;
    }
    
    /// <summary>
    /// Выбрать классификатор по типам документов.
    /// </summary>
    public void SelectTypeClassifier()
    {
      var typeClassifier = this.RunClassifierSelectionDialog(SmartProcessingSettings.Resources.SelectTypeClassifierDialogTitle);
      if (typeClassifier != null)
      {
        _obj.TypeClassifierId = typeClassifier.Id;
        _obj.TypeClassifierName = typeClassifier.Name;
      }
    }
    
    /// <summary>
    /// Выбрать классификатор первых страниц.
    /// </summary>
    public void SelectFirstPageClassifier()
    {
      var firstPageClassifier = this.RunClassifierSelectionDialog(SmartProcessingSettings.Resources.SelectFirstPageClassifierDialogTitle);
      if (firstPageClassifier != null)
      {
        _obj.FirstPageClassifierId = firstPageClassifier.Id;
        _obj.FirstPageClassifierName = firstPageClassifier.Name;
      }
    }
  }
}