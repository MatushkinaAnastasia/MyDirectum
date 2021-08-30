-- Убрать лишний параметр в типе связи FinancialDocuments.
delete from [dbo].[Sungero_Core_RelationTypeMa]
where SourceType = '96C4F4F3-DC74-497A-B347-E8FAF4AFE320'
  and TargetType = '96C4F4F3-DC74-497A-B347-E8FAF4AFE320'
  and RelationType in (select Id 
                         from [dbo].[Sungero_Core_RelationType]
						             where Name = 'FinancialDocuments')					 