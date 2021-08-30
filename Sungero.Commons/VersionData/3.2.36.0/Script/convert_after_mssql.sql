if exists (select * from sys.indexes where indexes.name = 'idx_ExtEntityLinks_Discriminator_EntityId_ExtEntityType_ExtSystemId_SyncDate'
           and object_id = OBJECT_ID('Sungero_Commons_ExtEntityLinks'))
begin
  drop index idx_ExtEntityLinks_Discriminator_EntityId_ExtEntityType_ExtSystemId_SyncDate on Sungero_Commons_ExtEntityLinks
end

if exists (select * from sys.indexes where indexes.name = 'idx_ExtEntityLinks_Discriminator_ExtEntityType_ExtSystemId'
           and object_id = OBJECT_ID('Sungero_Commons_ExtEntityLinks'))
begin
  drop index idx_ExtEntityLinks_Discriminator_ExtEntityType_ExtSystemId on Sungero_Commons_ExtEntityLinks
end