-- Перенос во временную таблицу значения IsStandard у договора
if exists (select * from information_schema.tables where TABLE_NAME = 'ContractIsStandard_Temp')
  drop table ContractIsStandard_Temp

if exists (select * from information_schema.COLUMNS where TABLE_NAME = 'Sungero_Content_EDoc' and COLUMN_NAME = 'IsStandard_Contrac_Sungero')
begin
  execute('
    create table ContractIsStandard_Temp(ContractId integer, IsStandard bit)
    insert into ContractIsStandard_Temp
    select
      Id, IsStandard_Contrac_Sungero
    from
      Sungero_Content_EDoc
  ')
end