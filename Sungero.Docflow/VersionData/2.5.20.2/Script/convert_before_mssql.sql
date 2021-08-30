-- Дублирование конвертации типа этапа ControlReturn в таблице Sungero_Docflow_RuleStages
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