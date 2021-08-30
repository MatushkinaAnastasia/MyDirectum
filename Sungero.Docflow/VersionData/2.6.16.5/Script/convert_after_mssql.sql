if exists (select * from information_schema.tables where table_name = 'LeadingDocumentToContract')
begin
  update Sungero_Content_EDoc
	set Sungero_Content_EDoc.LeadDocument_Docflow_Sungero = ldtc.Document
	from LeadingDocumentToContract ldtc
	where Sungero_Content_EDoc.Id = ldtc.Id

	drop table LeadingDocumentToContract
end
