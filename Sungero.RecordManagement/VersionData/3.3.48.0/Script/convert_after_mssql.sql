if exists(select 1 from INFORMATION_SCHEMA.TABLES where table_name = 'Sungero_Docflow_Params')
begin

update
  Sungero_Docflow_Params
set
  [value] = '2000'
where
  [key] = 'AcquaintanceTaskPerformersLimit'
  and [value] = '1000'

end