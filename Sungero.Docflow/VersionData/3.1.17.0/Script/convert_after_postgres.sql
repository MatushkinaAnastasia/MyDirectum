do $$
begin
if exists(select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'sungero_docflow_condition')
then
  -- Переименование типа условия
  update sungero_docflow_condition
    set (name, negationname) = ('Вложено приложение -' || substring(name from 7 for length(name)-6), 
                                'Не вложено приложение -' || substring(name from 7 for length(name)-6))
  where conditiontype = 'HasAddenda'
    and substring(name from 1 for 7) = 'Вложен ';
end if;
end$$;