if exists (select * from information_schema.tables where table_name = 'SignatureSettingsReason') and 
exists (select * from information_schema.COLUMNS where table_name = 'Sungero_Docflow_SignSettings' and COLUMN_NAME = 'DocumentInfo') and
exists (select * from information_schema.COLUMNS where table_name = 'Sungero_Docflow_SignSettings' and COLUMN_NAME = 'Reason')

begin
  update s
	set Reason = case when lower(tmp.Reason) in ('устав', 'charter') then 'Duties' else 'Other' end,
        DocumentInfo = case when lower(tmp.Reason) in ('устав', 'charter') then '' else tmp.Reason end
	from SignatureSettingsReason tmp
	join Sungero_Docflow_SignSettings s
	  on tmp.Id = s.Id

	drop table SignatureSettingsReason
end