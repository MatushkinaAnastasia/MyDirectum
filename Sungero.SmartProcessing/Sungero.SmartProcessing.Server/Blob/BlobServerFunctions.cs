using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.Blob;

namespace Sungero.SmartProcessing.Server
{
  partial class BlobFunctions
  {
    
    /// <summary>
    /// Создать блоб с телом документа и дополнительной информацией.
    /// </summary>
    /// <returns>Блоб.</returns>
    [Remote, Public]
    public static IBlob CreateBlob()
    {
      return Blobs.Create();
    }
    
  }
}