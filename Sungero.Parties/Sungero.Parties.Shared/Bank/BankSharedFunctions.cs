using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Bank;

namespace Sungero.Parties.Shared
{
  partial class BankFunctions
  {

    /// <summary>
    /// Проверка БИК по количеству символов.
    /// </summary>
    /// <param name="bic">БИК.</param>
    /// <returns>Пустая строка, если длина БИК в порядке.
    /// Иначе текст ошибки.</returns>
    [Public]
    public static string CheckBicLength(string bic)
    {
      if (string.IsNullOrWhiteSpace(bic))
        return string.Empty;
      
      return System.Text.RegularExpressions.Regex.IsMatch(bic, @"(^[0-9A-Z]{8,11}$)") ? string.Empty : Banks.Resources.IncorrectBicLength;
    }
    
    /// <summary>
    /// Проверка корр. счета по количеству символов.
    /// </summary>
    /// <param name="corr">Корр. счет.</param>
    /// <returns>Пустая строка, если длина корр. счета в порядке.
    /// Иначе текст ошибки.</returns>
    [Public]
    public static string CheckCorrLength(string corr)
    {
      if (string.IsNullOrWhiteSpace(corr))
        return string.Empty;
      
      return System.Text.RegularExpressions.Regex.IsMatch(corr, @"(^\d{20}$)") ? string.Empty : Banks.Resources.IncorrectCorrLength;
    }

    /// <summary>
    /// Предупреждение о банке с аналогичным БИКом.
    /// </summary>
    /// <returns>Текст предупреждения с наименованием банка.</returns>
    [Public, Obsolete]
    public virtual string GetBankWithSameBicWarning()
    {
      var bankSameBic = Functions.Bank.Remote.GetBanksWithSameBic(_obj, true);
      if (bankSameBic.Any())
        return Banks.Resources.SameBICFormat(bankSameBic.First());
      return string.Empty;
    }
    
    /// <summary>
    /// Получить текст ошибки о наличии дублей контрагента.
    /// </summary>
    /// <returns>Текст ошибки.</returns>
    public override string GetCounterpartyDuplicatesErrorText()
    {
      if (!string.IsNullOrWhiteSpace(_obj.BIC) && _obj.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed)
      {
        var sameBicBanks = Functions.Bank.Remote.GetBanksWithSameBic(_obj, true);
        if (sameBicBanks.Any())
        {
          var firstDuplicate = sameBicBanks.OrderByDescending(x => x.Id).First();
          var duplicateTypeInNominative = GetTypeDisplayValue(firstDuplicate, CommonLibrary.DeclensionCase.Nominative);
          return Banks.Resources.SameBICFormat(duplicateTypeInNominative.ToLower(), firstDuplicate);
        }
      }
      
      return base.GetCounterpartyDuplicatesErrorText();
    }
    
    /// <summary>
    /// Получить банки, участвующие в договорах.
    /// </summary>
    /// <returns>Список ИД банков.</returns>
    [Public]
    public static List<int> GetBankIds()
    {
      return Functions.Bank.Remote.GetBankIdsServer();
    }
  }
}