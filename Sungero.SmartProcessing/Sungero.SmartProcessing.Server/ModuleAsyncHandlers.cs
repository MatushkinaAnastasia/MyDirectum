using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.Metadata;

namespace Sungero.SmartProcessing.Server
{
  public class ModuleAsyncHandlers
  {
    /// <summary>
    /// Асинхронный обработчик удаления результатов распознавания сущности.
    /// </summary>
    /// <param name="args">Параметр - ИД сущности, результаты распознавания которой нужно удалить.</param>
    public virtual void DeleteEntityRecognitionInfo(Sungero.SmartProcessing.Server.AsyncHandlerInvokeArgs.DeleteEntityRecognitionInfoInvokeArgs args)
    {
      var electronicDocumentMetadata = typeof(IElectronicDocument).GetEntityMetadata();
      var documentRecognitionInfos = Commons.EntityRecognitionInfos.GetAll().Where(r => r.EntityId == args.EntityId);
      
      foreach (var recognitionInfo in documentRecognitionInfos)
      {
        if (electronicDocumentMetadata.IsAncestorFor(Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(Guid.Parse(recognitionInfo.EntityType))))
          Commons.EntityRecognitionInfos.Delete(recognitionInfo);
      }
    }
  }
}