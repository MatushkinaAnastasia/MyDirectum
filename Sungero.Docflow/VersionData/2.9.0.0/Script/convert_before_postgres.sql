do $$
begin
  -- Переименовать таблицу CurrentCode -> CurrentNumbers
  if (exists (select 1 from information_schema.tables where table_name='sungero_docregister_currentcode')
      and not exists (select 1 from information_schema.tables where table_name='sungero_docregister_currentnumber'))
  then
    alter table if exists Sungero_DocRegister_CurrentCode
    rename to Sungero_DocRegister_CurrentNumbers;
  end if;
  
  -- Переименовать колонку в CurrentNumbers CurrentCode -> CurrentNumber, если это еще не сделано.
  if (exists (select 1 from information_schema.columns where table_name='sungero_docregister_currentnumbers' and column_name='currentcode')
      and not exists (select 1 from information_schema.columns where table_name='sungero_docregister_currentnumbers' and column_name='currentnumber'))
  then
    alter table Sungero_DocRegister_CurrentNumbers
    rename column CurrentCode to CurrentNumber;
  end if;
    
  if (exists (select 1 from information_schema.Columns where table_name = 'sungero_docregister_currentnumbers')
      and not exists (select 1 from information_schema.Columns where table_name = 'sungero_docregister_currentnumbers' and column_name = 'department'))
  then
    alter table Sungero_DocRegister_CurrentNumbers
    add Department integer not null default(0);
  end if;
  
  if (exists (select 1 from information_schema.Columns where table_name = 'sungero_docregister_currentnumbers')
      and not exists (select 1 from information_schema.Columns where table_name = 'sungero_docregister_currentnumbers' and column_name = 'bunit'))
  then
    alter table Sungero_DocRegister_CurrentNumbers
    add BUnit integer not null default(0);
  end if;
end $$