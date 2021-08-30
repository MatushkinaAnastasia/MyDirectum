-- Копипаста с ФинАрхива 2662
if exists (select * from information_schema.COLUMNS 
           where 
		     table_name = 'Sungero_Content_EDoc' 
			 and COLUMN_NAME = 'LeadDocument_FinArch_Sungero')
begin
  execute('update Sungero_Content_EDoc
set Document_Docflow_Sungero = LeadDocument_FinArch_Sungero
where LeadDocument_FinArch_Sungero is not null
        ')
end

-- Перенос во временную таблицу свойств LeadDocument_Docflow_Sungero, Contract_Contrac_Sungero, Document_Docflow_Sungero из Sungero_Content_EDoc
if exists (select * from information_schema.tables where table_name = 'tmpLeadDocument')
  drop table tmpLeadDocument

if exists (select * from information_schema.COLUMNS 
           where 
		     table_name = 'Sungero_Content_EDoc' 
			 and COLUMN_NAME = 'Contract_Contrac_Sungero')
begin
  execute('create table tmpLeadDocument(Id integer, LeadDocument int)
    insert into tmpLeadDocument
    select
      Id, LeadDocument_Docflow_Sungero
    from
      Sungero_Content_EDoc
	where
	 LeadDocument_Docflow_Sungero is not null
    union all
    select
      Id, Contract_Contrac_Sungero
    from
      Sungero_Content_EDoc
	where
	 Contract_Contrac_Sungero is not null
	')
end

if exists (select * from information_schema.COLUMNS 
           where 
		     table_name = 'Sungero_Content_EDoc' 
			 and COLUMN_NAME = 'Document_Docflow_Sungero')
begin
  if not exists (select * from information_schema.tables where table_name = 'tmpLeadDocument')
    execute('create table tmpLeadDocument(Id integer, LeadDocument int)')

  execute('insert into tmpLeadDocument
	select
      Id, Document_Docflow_Sungero
    from
      Sungero_Content_EDoc
	where
	 Document_Docflow_Sungero is not null
	')
end

-- Перенос свойства типа связи SupAgreement
update Sungero_Core_RelationTypeMa
set TargetProperty = 'aae1f6ae-d090-4c5a-963e-44c8b79dc92b'
where TargetProperty = '3f4a3290-04bc-4cc1-aea8-213a29cc4a13'

-- Перенос свойства типа связи Addendum
update Sungero_Core_RelationTypeMa
set TargetProperty = 'aae1f6ae-d090-4c5a-963e-44c8b79dc92b'
where TargetProperty = '6fa183c9-b1da-4b2a-b5a6-66ed69a6a252'

-- Перенос свойства типа связи Accounting
update Sungero_Core_RelationTypeMa
set TargetProperty = 'aae1f6ae-d090-4c5a-963e-44c8b79dc92b'
where TargetProperty = '125589da-53ba-4d6d-8bd5-0ab3cd1d19da'