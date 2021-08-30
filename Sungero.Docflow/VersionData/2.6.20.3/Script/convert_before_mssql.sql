if exists (select * from information_schema.tables where table_name = 'SignatureSettingsReason')
  drop table SignatureSettingsReason

if exists (select * from information_schema.COLUMNS where table_name = 'Sungero_Docflow_SignSettings' and COLUMN_NAME = 'Reason')
and not exists (select * from information_schema.COLUMNS where table_name = 'Sungero_Docflow_SignSettings' and COLUMN_NAME = 'DocumentInfo')
begin
	execute('create table SignatureSettingsReason(Id integer, Reason nvarchar(250))
	insert into SignatureSettingsReason
	select 
	  s.Id, s.Reason
	from 
	  Sungero_Docflow_SignSettings s')
end