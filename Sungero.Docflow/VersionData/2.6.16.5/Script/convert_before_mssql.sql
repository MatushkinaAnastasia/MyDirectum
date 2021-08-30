if exists (select * from information_schema.tables where table_name = 'LeadingDocumentToContract')
  drop table LeadingDocumentToContract

if exists (select * from information_schema.COLUMNS where table_name = 'Sungero_Content_EDoc' and COLUMN_NAME = 'LeadDocument_Docflow_Sungero')
begin
	execute('create table LeadingDocumentToContract(Id integer, Document int)
	insert into LeadingDocumentToContract
	select
	  doc.Id, doc.LeadDocument_Docflow_Sungero
	from
    Sungero_Content_EDoc as doc
	left join Sungero_Content_EDoc as leadDoc on leadDoc.Id = doc.LeadDocument_Docflow_Sungero
	where
	  doc.LeadDocument_Docflow_Sungero is not null 
	  and doc.Discriminator in (''f2f5774d-5ca3-4725-b31d-ac618f6b8850'', ''74c9ddd4-4bc4-42b6-8bb0-c91d5e21fb8a'', ''f50c4d8a-56bc-43ef-bac3-856f57ca70be'', 
	  ''58986e23-2b0a-4082-af37-bd1991bc6f7e'', ''4e81f9ca-b95a-4fd4-bf76-ea7176c215a7'') 
	  and leadDoc.Discriminator not in (''f2f5774d-5ca3-4725-b31d-ac618f6b8850'', ''74c9ddd4-4bc4-42b6-8bb0-c91d5e21fb8a'', ''f50c4d8a-56bc-43ef-bac3-856f57ca70be'', 
	  ''58986e23-2b0a-4082-af37-bd1991bc6f7e'', ''4e81f9ca-b95a-4fd4-bf76-ea7176c215a7'')')

	execute('update Sungero_Content_EDoc set
	LeadDocument_Docflow_Sungero = null
	where
	Id in (select doc.id from Sungero_Content_EDoc as doc
	left join Sungero_Content_EDoc as leadDoc on leadDoc.Id = doc.LeadDocument_Docflow_Sungero
	where
	  doc.LeadDocument_Docflow_Sungero is not null 
	  and doc.Discriminator in (''f2f5774d-5ca3-4725-b31d-ac618f6b8850'', ''74c9ddd4-4bc4-42b6-8bb0-c91d5e21fb8a'', ''f50c4d8a-56bc-43ef-bac3-856f57ca70be'', 
	  ''58986e23-2b0a-4082-af37-bd1991bc6f7e'', ''4e81f9ca-b95a-4fd4-bf76-ea7176c215a7'') 
	  and leadDoc.Discriminator in (''f2f5774d-5ca3-4725-b31d-ac618f6b8850'', ''74c9ddd4-4bc4-42b6-8bb0-c91d5e21fb8a'', ''f50c4d8a-56bc-43ef-bac3-856f57ca70be'', 
	  ''58986e23-2b0a-4082-af37-bd1991bc6f7e'', ''4e81f9ca-b95a-4fd4-bf76-ea7176c215a7''))')
end