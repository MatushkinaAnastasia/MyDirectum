using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Shell.Client
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Показать документы контрагента.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    [Public]
    public void SearchDocumentsWithCounterparties(Sungero.Parties.ICounterparty counterparty)
    {
      Functions.Module.Remote.GetDocumentsWithCounterparties(counterparty).Show(counterparty.Name);
    }
    
  }
}