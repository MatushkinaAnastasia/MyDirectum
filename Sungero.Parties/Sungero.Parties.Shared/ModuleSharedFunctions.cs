using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Parties.Shared
{
  public class ModuleFunctions
  {
    #region Email
    
    /// <summary>
    /// Проверить email на валидность.
    /// </summary>
    /// <param name="emailAddress">Email.</param>
    /// <returns>Признак валидности email.</returns>
    [Public]
    public static bool EmailIsValid(string emailAddress)
    {
      try
      {
        MailAddress email = new MailAddress(emailAddress);
      }
      catch (FormatException)
      {
        return false;
      }
      return true;
    }

    #endregion
  }
}