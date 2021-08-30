if not exists (select * from sys.indexes 
               where name='idx_ExtEntityLinks_Discriminator_EntityId_ExtEntityType_ExtSystemId_SyncDate' 
                 and object_id = OBJECT_ID('Sungero_Commons_ExtEntityLinks'))
begin 
  create NONCLUSTERED INDEX idx_ExtEntityLinks_Discriminator_EntityId_ExtEntityType_ExtSystemId_SyncDate 
  on Sungero_Commons_ExtEntityLinks ([Discriminator],[EntityId],[ExtEntityType],[ExtSystemId],[SyncDate])
end

if not exists (select * from sys.indexes 
               where name='idx_ExtEntityLinks_Discriminator_ExtEntityType_ExtSystemId' 
                 and object_id = OBJECT_ID('Sungero_Commons_ExtEntityLinks'))
begin 
  create NONCLUSTERED INDEX idx_ExtEntityLinks_Discriminator_ExtEntityType_ExtSystemId
  on Sungero_Commons_ExtEntityLinks ([Discriminator],[ExtEntityId],[ExtSystemId])
end