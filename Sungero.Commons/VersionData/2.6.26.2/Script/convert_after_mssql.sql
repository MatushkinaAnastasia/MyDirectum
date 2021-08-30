-- Скорректировать код Российского Рубля
if exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME='Sungero_Commons_Currency')
begin
  update Sungero_Commons_Currency
  set AlphaCode = 'RUB'
  where AlphaCode = '643'
end