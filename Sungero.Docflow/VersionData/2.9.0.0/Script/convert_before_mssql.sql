if exists (select name from sys.objects where object_id = object_id(N'Sungero_DocRegister_GetNextCode') and type in (N'P'))
  drop procedure dbo.Sungero_DocRegister_GetNextCode;

-- Переименовать таблицу CurrentCode -> CurrentNumbers
if (exists (select 1 from sys.objects where object_id = object_id(N'Sungero_DocRegister_CurrentCode') and type in (N'U'))
    and not exists (select 1 from sys.objects where object_id = object_id(N'Sungero_DocRegister_CurrentNumbers') and type in (N'U')))
begin
  exec sp_rename
    'Sungero_DocRegister_CurrentCode',
    'Sungero_DocRegister_CurrentNumbers';
end

-- Переименовать колонку в CurrentNumbers CurrentCode -> CurrentNumber, если это еще не сделано.
if (exists (select 1 from information_schema.columns where table_name = 'Sungero_DocRegister_CurrentNumbers' and column_name = 'CurrentCode')
    and not exists (select 1 from information_schema.columns where table_name = 'Sungero_DocRegister_CurrentNumbers' and column_name = 'CurrentNumber'))
begin
  exec sp_rename 
    'Sungero_DocRegister_CurrentNumbers.CurrentCode',
    'CurrentNumber',
    'COLUMN';
end

if (exists (select * from information_schema.columns where table_name = 'Sungero_DocRegister_CurrentNumbers')
    and not exists (select * from information_schema.columns where table_name = 'Sungero_DocRegister_CurrentNumbers' and column_name = 'Department'))
begin
  alter table dbo.Sungero_DocRegister_CurrentNumbers
  add Department integer not null default(0)
end

if (exists (select * from information_schema.columns where table_name = 'Sungero_DocRegister_CurrentNumbers')
    and not exists (select * from information_schema.columns where table_name = 'Sungero_DocRegister_CurrentNumbers' and column_name = 'BUnit'))
begin
  alter table dbo.Sungero_DocRegister_CurrentNumbers
  add BUnit integer not null default(0)
end