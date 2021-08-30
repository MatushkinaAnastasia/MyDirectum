using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalTask;
using Sungero.Workflow;

namespace Sungero.Docflow.Shared
{
  partial class ApprovalTaskFunctions
  {
    
    /// <summary>
    /// Определить номер следующего этапа.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Номер следующего этапа.</returns>
    public static int? GetNextStageNumber(IApprovalTask task)
    {
      var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      return Functions.ApprovalRuleBase.GetNextStageNumber(task.ApprovalRule, document, task.StageNumber, task).Number;
    }
    
    /// <summary>
    /// Установить обязательность свойств.
    /// </summary>
    /// <param name="refreshParameters">Информация по этапам для обновления формы задачи на согласование по регламенту.</param>
    public virtual void SetRequiredProperties(Structures.ApprovalTask.RefreshParameters refreshParameters)
    {
      var taskProperties = _obj.State.Properties;
      taskProperties.Addressee.IsRequired = refreshParameters.AddresseeIsRequired;
      taskProperties.Signatory.IsRequired = refreshParameters.SignatoryIsRequired;
      taskProperties.ExchangeService.IsRequired = refreshParameters.ExchangeServiceIsRequired;
    }
    
    /// <summary>
    /// Установить видимость свойств.
    /// </summary>
    /// <param name="refreshParameters">Информация по этапам для обновления формы задачи на согласование по регламенту.</param>
    public virtual void SetVisibleProperties(Structures.ApprovalTask.RefreshParameters refreshParameters)
    {
      var taskProperties = _obj.State.Properties;
      taskProperties.AddApprovers.IsVisible = refreshParameters.AddApproversIsVisible;
      taskProperties.Addressee.IsVisible = refreshParameters.AddresseeIsVisible;
      taskProperties.Signatory.IsVisible = refreshParameters.SignatoryIsVisible;
      taskProperties.DeliveryMethod.IsVisible = refreshParameters.DeliveryMethodIsVisible;
      taskProperties.ExchangeService.IsVisible = refreshParameters.ExchangeServiceIsVisible;
    }
    
    /// <summary>
    /// Установить доступность свойств.
    /// </summary>
    /// <param name="refreshParameters">Информация по этапам для обновления формы задачи на согласование по регламенту.</param>
    public virtual void SetEnabledProperties(Structures.ApprovalTask.RefreshParameters refreshParameters)
    {
      var taskProperties = _obj.State.Properties;
      
      taskProperties.Addressee.IsEnabled = false;
      taskProperties.ReqApprovers.IsEnabled = false;
      
      if (_obj.ApprovalRule != null)
      {
        taskProperties.Addressee.IsEnabled = refreshParameters.AddresseeIsEnabled;
      }

      var isExchange = _obj.DeliveryMethod != null && _obj.DeliveryMethod.Sid == Constants.MailDeliveryMethod.Exchange;
      taskProperties.ExchangeService.IsEnabled = refreshParameters.ExchangeServiceIsEnabled;
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      if (isExchange && OfficialDocuments.Is(document))
      {
        if (document.Versions.Any())
        {
          var isIncomingDocument = Docflow.PublicFunctions.OfficialDocument.Remote.CanSendAnswer(document);
          var isFormalizedDocument = Docflow.AccountingDocumentBases.Is(document) && Docflow.AccountingDocumentBases.As(document).IsFormalized == true;
          taskProperties.DeliveryMethod.IsEnabled = refreshParameters.DeliveryMethodIsEnabled;
          taskProperties.ExchangeService.IsEnabled = refreshParameters.ExchangeServiceIsEnabled;
        }
      }
      
      // Не давать менять адресата в согласовании служебных записок.
      if (Memos.Is(document))
        taskProperties.Addressee.IsEnabled = false;
      
      // Не давать изменять способ доставки для исходящих писем на несколько адресатов.
      if (OutgoingDocumentBases.Is(document) && OutgoingDocumentBases.As(document).IsManyAddressees == true)
        taskProperties.DeliveryMethod.IsEnabled = false;
    }
    
    /// <summary>
    /// Получить описание способа доставки.
    /// </summary>
    /// <param name="method">Способ доставки.</param>
    /// <param name="service">Сервис обмена.</param>
    /// <param name="isManyAddressees">True, если отправка на несколько адресов, иначе - false.</param>
    /// <returns>Описание способа доставки.</returns>
    public static string GetDeliveryMethodDescription(IMailDeliveryMethod method, ExchangeCore.IExchangeService service, bool isManyAddressees)
    {
      if (isManyAddressees)
        return ApprovalTasks.Resources.DeliveryMethodToManyAddressees;
      if (method == null)
        return string.Empty;
      
      var exchangeServiceDelivery = method.Sid == Constants.MailDeliveryMethod.Exchange;
      
      if (exchangeServiceDelivery)
      {
        if (service == null)
          return string.Empty;
        
        return ApprovalTasks.Resources.DeliveryMethodByExchangeFormat(service.Name);
      }
      
      return ApprovalTasks.Resources.DeliveryMethodNotByExchangeFormat(method.Name);
    }
    
    /// <summary>
    /// Обновить видимость, доступность и обязательность полей в карточке задачи.
    /// </summary>
    public void RefreshApprovalTaskForm()
    {
      var refreshParameters = Functions.ApprovalTask.Remote.GetStagesInfoForRefresh(_obj);
      this.RefreshProperties(refreshParameters);
    }

    /// <summary>
    /// Обновить видимость, доступность и обязательность полей с учетом этапов согласования в карточке задачи.
    /// </summary>
    /// <param name="stages">Этапы согласования.</param>
    public void RefreshApprovalTaskForm(List<Structures.Module.DefinedApprovalStageLite> stages)
    {
      var refreshParameters = Functions.ApprovalTask.Remote.GetStagesInfoForRefresh(_obj, stages);
      this.RefreshProperties(refreshParameters);
    }
    
    /// <summary>
    /// Обновить видимость, доступность и обязательность полей в карточке задачи.
    /// </summary>
    /// <param name="refreshParameters">Структура с данными по этапам согласования.</param>
    public void RefreshProperties(Structures.ApprovalTask.RefreshParameters refreshParameters)
    {
      Functions.ApprovalTask.SetEnabledProperties(_obj, refreshParameters);
      Functions.ApprovalTask.SetVisibleProperties(_obj, refreshParameters);
      Functions.ApprovalTask.SetRequiredProperties(_obj, refreshParameters);
    }
    
    /// <summary>
    /// Доступно ли указывать в качестве исполнителя задания на доработку не инициатора.
    /// </summary>
    /// <returns>True - если возможно, False - если нельзя.</returns>
    public virtual bool SchemeVersionSupportsRework()
    {
      return _obj.SchemeVersion >= Constants.ApprovalTask.SchemeVersionWhereChangePerformer;
    }
    
    /// <summary>
    /// Валидация старта задачи на согласование по регламенту.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateApprovalTaskStart(Sungero.Core.IValidationArgs e)
    {
      var haveError = false;
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      if (!Sungero.Company.Employees.Is(_obj.Author))
      {
        e.AddError(_obj.Info.Properties.Author, Docflow.Resources.CantSendTaskByNonEmployee);
        haveError = true;
      }

      if (!document.AccessRights.CanUpdate())
      {
        e.AddError(ApprovalTasks.Resources.CantSendDocumentsWithoutUpdateRights);
        haveError = true;
      }
      
      // Проверить указанность регламента.
      if (_obj.ApprovalRule == null)
      {
        e.AddError(_obj.Info.Properties.ApprovalRule, ApprovalTasks.Resources.ToSendDocumentApprovalSpecifyRule);
        return false;
      }
      
      // Если регламент указан, но есть ошибки в определении условий - значит, не все поля документа заполнены.
      var getStagesResult = Functions.ApprovalTask.Remote.GetStages(_obj);
      if (_obj.ApprovalRule != null && !getStagesResult.IsConditionsDefined)
      {
        e.AddError(getStagesResult.ErrorMessage);
        haveError = true;
      }
      
      // Если регламент указан, а срок не определен - значит, не хватает календаря рабочего времени.
      if (_obj.ApprovalRule != null && !_obj.MaxDeadline.HasValue && Functions.ApprovalTask.Remote.GetExpectedDate(_obj) == null)
      {
        e.AddError(ApprovalTasks.Resources.EmptyNextWorkingCalendar);
        haveError = true;
      }
      
      // Проверить актуальность регламента.
      var documentRules = Functions.OfficialDocument.Remote.GetApprovalRules(document).ToList();
      if (!documentRules.Contains(_obj.ApprovalRule))
      {
        e.AddError(_obj.Info.Properties.ApprovalRule, ApprovalTasks.Resources.RuleOrDocumentHasBeenChanged);
        haveError = true;
      }
      
      // Проверить, имеет ли сотрудник, указанный в поле "На подпись", право подписи документов.
      if (!Functions.ApprovalTask.Remote.CheckSignatory(_obj, _obj.Signatory, getStagesResult.Stages))
      {
        e.AddError(_obj.Info.Properties.Signatory, Docflow.Resources.TheSpecifiedEmployeeIsNotAuthorizedToSignDocuments);
        haveError = true;
      }

      // Проверить, определился ли для этапа регистрации исполнитель.
      var registerStage = getStagesResult.Stages.Where(s => s.StageType == Docflow.ApprovalStage.StageType.Register).FirstOrDefault();
      if (registerStage != null)
      {
        var registerStageAssignee = Functions.ApprovalRuleBase.Remote.GetEmployeeByAssignee(registerStage.Stage.Assignee);
        var clerk = registerStageAssignee ?? Docflow.PublicFunctions.ApprovalStage.Remote.GetRemoteStagePerformer(_obj, registerStage.Stage);
        if (clerk == null)
        {
          e.AddError(ApprovalTasks.Resources.DetermineRegistrarCurrentRuleError);
          haveError = true;
        }
      }
      
      // Проверить, определился ли для этапа создания поручений исполнитель.
      var stages = getStagesResult.Stages;
      var executionStage = stages.Where(s => s.Stage.StageType == Docflow.ApprovalStage.StageType.Execution).FirstOrDefault();
      var reviewStage = stages.Where(s => s.Stage.StageType == Docflow.ApprovalStage.StageType.Review).FirstOrDefault();
      var signStage = stages.Where(s => s.Stage.StageType == Docflow.ApprovalStage.StageType.Sign).FirstOrDefault();
      var reviewStageIndex = stages.IndexOf(reviewStage);
      var signStageIndex = stages.IndexOf(signStage);
      if (executionStage != null)
      {
        var executionStageAssignee = Functions.ApprovalRuleBase.Remote.GetEmployeeByAssignee(executionStage.Stage.Assignee);
        var performer = executionStageAssignee ?? Docflow.PublicFunctions.ApprovalStage.Remote.GetRemoteStagePerformer(_obj, executionStage.Stage);
        if (performer == null && signStageIndex > reviewStageIndex)
        {
          e.AddError(ApprovalTasks.Resources.NoExecutionAssignee);
          haveError = true;
        }
      }
      
      // Проверить, заполнен ли проект, когда его требует роль в правиле.
      if (document.Project == null && stages.Any(s => Functions.ApprovalStage.HasRole(s.Stage, Docflow.ApprovalRoleBase.Type.ProjectManager) ||
                                                 Functions.ApprovalStage.HasRole(s.Stage, Docflow.ApprovalRoleBase.Type.ProjectAdmin)))
      {
        e.AddError(ApprovalTasks.Resources.DocumentMustHaveProject);
        haveError = true;
      }
      
      return !haveError;
    }
    
  }
}