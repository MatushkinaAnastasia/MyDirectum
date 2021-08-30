-- Изменение параметров для типа связи AccountingDocuments.
update Sungero_Core_RelationTypeMa
set TargetProperty = 'aae1f6ae-d090-4c5a-963e-44c8b79dc92b',
SourceType = '59079f7f-a326-4947-bbd6-0ae6dfb5f59b',
TargetType = '96c4f4f3-dc74-497a-b347-e8faf4afe320'
where RelationType in (select Id from [dbo].[Sungero_Core_RelationType] where Name = 'AccountingDocuments')
and TargetType = 'f2f5774d-5ca3-4725-b31d-ac618f6b8850'
and SourceType = '306DA7FA-DC27-437C-BB83-42C92436B7E2'

update Sungero_Core_RelationTypeMa
set TargetProperty = 'aae1f6ae-d090-4c5a-963e-44c8b79dc92b',
SourceType = '96c4f4f3-dc74-497a-b347-e8faf4afe320',
TargetType = '96c4f4f3-dc74-497a-b347-e8faf4afe320'
where RelationType in (select Id from [dbo].[Sungero_Core_RelationType] where Name = 'AccountingDocuments')
and TargetType = 'f2f5774d-5ca3-4725-b31d-ac618f6b8850'
and SourceType = '265F2C57-6A8A-4A15-833B-CA00E8047FA5'