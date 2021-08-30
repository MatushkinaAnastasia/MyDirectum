using System;

namespace Sungero.ExchangeCore.Constants
{
  public static class BusinessUnitBox
  {
    public const string Delimiter = " - ";
    
    public const string Moscow = "г. Москва";
    public const string SaintPetersburg = "г. Санкт-Петербург";
    public const string Sevastopol = "г. Севастополь";
    public const string Baikonur = "г. Байконур";
    
    public const string House = "д.";
    public const string Building = "корп.";
    public const string Apartment = "кв.";
    
    public const string City = "г.";
    public const string Locality = "пгт.";
    public const int PasswordMaxLength = 50;
    
    public const int CounterpartySyncBatchSize = 25;
    
    public static readonly Guid ExchangeCoreDiadocGiud = Guid.Parse("30083842-5a15-4efb-9cab-0b61b1760157");
  }
}