if exists (SELECT * FROM sys.indexes 
               WHERE name='idx_Assignment_Task_Discriminator_ExecutionState_IsCompound_Status' AND object_id = OBJECT_ID('Sungero_WF_Task'))
begin
  drop index idx_Assignment_Task_Discriminator_ExecutionState_IsCompound_Status ON Sungero_WF_Task
end

if exists (SELECT * FROM sys.indexes 
               WHERE name='idx_EDoc_Discriminator_DocumentDate_RegState_DocKind_SecureObject' AND object_id = OBJECT_ID('Sungero_Content_EDoc'))
begin
  drop index idx_EDoc_Discriminator_DocumentDate_RegState_DocKind_SecureObject ON Sungero_Content_EDoc
end

if exists (SELECT * FROM sys.indexes 
               WHERE name='idx_EDoc_Discriminator_DocumentDate_LifeCycleState_IntApprState_SecureObject' AND object_id = OBJECT_ID('Sungero_Content_EDoc'))
begin
  drop index idx_EDoc_Discriminator_DocumentDate_LifeCycleState_IntApprState_SecureObject ON Sungero_Content_EDoc
end