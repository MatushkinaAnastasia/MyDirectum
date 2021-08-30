-- Сконвертировать тип этапа ControlReturn в таблице Sungero_Docflow_ApprovalStage
if exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME='ApprovalStagesControlReturnConvert')
  drop table ConvertReturnControlApprovalStages

if exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME='Sungero_Docflow_ApprovalStage')
begin
  execute('create table ApprovalStagesControlReturnConvert(Id integer)')
  insert into ApprovalStagesControlReturnConvert
  select Id
  from Sungero_Docflow_ApprovalStage
  where Stagetype = 'ControlReturn'
end

-- Сконвертировать тип этапа ControlReturn в таблице Sungero_Docflow_RuleStages
if exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME='RuleStagesControlReturnConvert')
drop table RuleStagesControlReturnConvert

if exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME='Sungero_Docflow_RuleStages')
begin
  execute('create table RuleStagesControlReturnConvert(Id integer)')
  insert into RuleStagesControlReturnConvert
  select Id
  from Sungero_Docflow_RuleStages
  where StageType = 'ControlReturn'
end