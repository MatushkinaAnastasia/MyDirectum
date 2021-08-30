using System.Text.RegularExpressions;

namespace Sungero.Docflow.Constants
{
  public static class DocumentRegister
  {
    // Регулярное выражение для парсинга кода контрагента при валидации формата номера: пустая строка либо любая подстрока из непробельных символов.
    public const string CounterpartyCodeRegex = @"[\S]*?";
  }
}