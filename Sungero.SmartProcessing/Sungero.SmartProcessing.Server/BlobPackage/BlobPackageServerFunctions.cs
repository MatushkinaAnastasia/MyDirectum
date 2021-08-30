using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.BlobPackage;

namespace Sungero.SmartProcessing.Server
{
  partial class BlobPackageFunctions
  {
    
    /// <summary>
    /// Создать пакет блобов обрабатываемых документов.
    /// </summary>
    /// <returns>Пакет блобов.</returns>
    [Remote, Public]
    public static IBlobPackage CreateBlobPackage()
    {
      return BlobPackages.Create();
    }
    
  }
}