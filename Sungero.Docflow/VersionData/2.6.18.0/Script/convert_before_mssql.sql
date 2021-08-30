if exists (select * from information_schema.COLUMNS 
           where 
		     table_name = 'Sungero_Content_EDoc' 
			 and COLUMN_NAME = 'PartySignatory_FinArch_Sungero') and
	exists (select * from information_schema.COLUMNS 
           where 
		     table_name = 'Sungero_Content_EDoc' 
			 and COLUMN_NAME = 'PartySigner_Docflow_Sungero')
begin

execute('update [dbo].[Sungero_Content_EDoc]
set PartySigner_Docflow_Sungero = PartySignatory_FinArch_Sungero
where Discriminator = ''f2f5774d-5ca3-4725-b31d-ac618f6b8850''')

end