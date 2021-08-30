-- Дублирование конвертации типа этапа ControlReturn в таблице Sungero_Docflow_RuleStages
if exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME='RuleStagesControlReturnConvert')
  and exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME='Sungero_Docflow_RuleStages')
begin
  update Sungero_Docflow_RuleStages
  set StageType = 'CheckReturn'
  from dbo.RuleStagesControlReturnConvert
  where Sungero_Docflow_RuleStages.Id = RuleStagesControlReturnConvert.Id

  drop table RuleStagesControlReturnConvert
end