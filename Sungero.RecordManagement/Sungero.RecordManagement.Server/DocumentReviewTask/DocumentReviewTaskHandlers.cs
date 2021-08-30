using System.Linq;
using Sungero.Core;

namespace Sungero.RecordManagement
{
  partial class DocumentReviewTaskResolutionObserversObserverPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ResolutionObserversObserverFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return (IQueryable<T>)PublicFunctions.Module.ObserversFiltering(query);
    }
  }

  partial class DocumentReviewTaskCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.ResolutionText);
    }
  }

  partial class DocumentReviewTaskServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Выдать права на документы для всех, кому выданы права на задачу.
      if (_obj.State.IsChanged)
        Docflow.PublicFunctions.Module.GrantManualReadRightForAttachments(_obj, _obj.AllAttachments.ToList());
    }
    
    public override void BeforeRestart(Sungero.Workflow.Server.BeforeRestartEventArgs e)
    {
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault());
      var startedResoultionProjects = _obj.ResolutionGroup.ActionItemExecutionTasks.Where(a => a.IsDraftResolution != true).ToList();
      foreach (var project in startedResoultionProjects)
        _obj.ResolutionGroup.ActionItemExecutionTasks.Remove(project);
    }
    
    public override void BeforeAbort(Sungero.Workflow.Server.BeforeAbortEventArgs e)
    {
      // Если прекращен черновик, прикладную логику по прекращению выполнять не надо.
      if (_obj.State.Properties.Status.OriginalValue == Workflow.Task.Status.Draft)
        return;
      
      // Обновить статус исполнения - пустой.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.First();
      document.ExecutionState = null;
    }

    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      if (!Sungero.RecordManagement.Functions.DocumentReviewTask.ValidateDocumentReviewTaskStart(_obj, e))
        return;
      
      // Выдать права группе регистрации документа.
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      if (document.DocumentRegister != null)
      {
        var registrationGroup = document.DocumentRegister.RegistrationGroup;
        if (registrationGroup != null)
          _obj.AccessRights.Grant(registrationGroup, DefaultAccessRightsTypes.Change);
      }
      
      Docflow.PublicFunctions.Module.GrantReadRightsForAttachments(_obj.DocumentForReviewGroup.All
                                                                   .Concat(_obj.AddendaGroup.All).ToList(),
                                                                   _obj.ResolutionObservers.Select(o => o.Observer));
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      
      // Получить ресурсы в культуре тенанта.
      using (TenantInfo.Culture.SwitchTo())
      {
        if (document != null)
          _obj.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.Consideration, document.Name);
        else
          _obj.Subject = Docflow.Resources.AutoformatTaskSubject;
        
        if (!_obj.State.IsCopied)
          _obj.ActiveText = Resources.ConsiderationText;
      }
      
      _obj.NeedsReview = false;
    }
  }
}