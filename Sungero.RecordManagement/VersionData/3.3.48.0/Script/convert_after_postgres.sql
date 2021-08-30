DO $$
begin

if exists(select 1 from INFORMATION_SCHEMA.TABLES where table_name = 'sungero_docflow_params')
then

update
  sungero_docflow_params
set
  value = '2000'
where
  key = 'AcquaintanceTaskPerformersLimit'
  and value = '1000';

end if;
end $$