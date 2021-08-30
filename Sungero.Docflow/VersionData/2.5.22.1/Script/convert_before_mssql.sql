-- Смена типа поля. На всякий случай сохраним значения.
if exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME='TAprReqAprsConverter')
  drop table TAprReqAprsConverter

if exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME='Sungero_Docflow_TAprReqAprs')
begin
  execute('create table TAprReqAprsConverter(Id int, Approver int)')
  insert into TAprReqAprsConverter (Id, Approver)
  select Id, Approver
  from Sungero_Docflow_TAprReqAprs
end