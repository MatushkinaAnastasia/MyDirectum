insert into [dbo].[Sungero_Docflow_SignCategories] (Id, Discriminator, SignSettings, Category) 
select Id, '9dc422bc-7d00-4fab-884b-0dba748a4ea3', SignSettings, Category
  from [dbo].[Sungero_Contrac_SignCategories]
  
update Sungero_System_Ids
set LastId = (select LastId from dbo.Sungero_System_Ids
where TableName = 'Sungero_Contrac_SignCategories')
where TableName = 'Sungero_Docflow_SignCategories' and LastId = 0