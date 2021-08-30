if exists (select * from information_schema.COLUMNS 
           where table_name = 'Sungero_Docflow_ApprovalStage' 
             and COLUMN_NAME = 'Assignee')
begin
  update stages
  set stages.Assignee = tempAssignee.Assignee
  from Sungero_Docflow_ApprovalStage stages
  join ApprovalStage_Assignee_Temp_table tempAssignee on
     stages.Id = tempAssignee.Id
  
end

if exists (select * from information_schema.TABLES where table_name = 'ApprovalStage_Assignee_Temp_table')
drop table ApprovalStage_Assignee_Temp_table