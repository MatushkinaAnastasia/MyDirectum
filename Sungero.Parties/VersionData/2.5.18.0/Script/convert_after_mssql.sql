-- Заполнить св-во КА "Эл. обмен"
if exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME='Sungero_Parties_ExchangeBoxes')
   and exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME='Sungero_Parties_Counterparty')
begin
  update counterparty
  set CanExchange = case when exists(select *
                                     from Sungero_Parties_ExchangeBoxes eBox
                                     where Status = 'Active'
                                       and counterparty.Id = eBox.Counterparty)
                         then 'true'
                         else 'false'
                    end
  from Sungero_Parties_Counterparty counterparty
  where CanExchange is null
end