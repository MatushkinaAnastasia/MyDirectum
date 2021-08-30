-- Сохранить свойства актов (и остальных договорных документов)
if exists (select * from information_schema.tables where table_name = 'ContractStatementProperties')
drop table ContractStatementProperties

if exists (select * from information_schema.COLUMNS 
           where table_name = 'Sungero_Content_EDoc' 
             and COLUMN_NAME = 'LeadDocument_Contrac_Sungero')
begin
  execute('create table ContractStatementProperties(Id integer, LeadDocument integer, Counterparty integer, 
             TotalAmount float, ContrCurrency integer, PartySignatory integer, Contact integer, RespEmpl integer,
             ValidFrom datetime, ValidTill datetime)')
  insert into ContractStatementProperties 
  select Id, LeadDocument_Contrac_Sungero, Counterparty_Docflow_Sungero, TotalAmount_Docflow_Sungero, 
         ContrCurrency_Docflow_Sungero, PartySignatory_Docflow_Sungero, Contact_Contrac_Sungero, RespEmpl_Contrac_Sungero,
         ValidFrom_Contrac_Sungero, ValidTill_Contrac_Sungero
  from Sungero_Content_EDoc
end

if exists (select * from sys.indexes where indexes.name = 'idx_ElectronicDocument_LifeCycleState_Discriminator_RespEmpl'
           and object_id = OBJECT_ID('Sungero_Content_EDoc'))
begin
  drop index idx_ElectronicDocument_LifeCycleState_Discriminator_RespEmpl on Sungero_Content_EDoc
end

