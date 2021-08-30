if exists (select * from information_schema.TABLES where table_name = 'ApprovalStage_Assignee_Temp_table')
drop table ApprovalStage_Assignee_Temp_table

if exists (select * from information_schema.COLUMNS 
           where table_name = 'Sungero_Docflow_ApprovalStage' 
             and COLUMN_NAME = 'Assignee')
begin
  select Id, Assignee
  into ApprovalStage_Assignee_Temp_table
  from Sungero_Docflow_ApprovalStage
  where Assignee is not null
end