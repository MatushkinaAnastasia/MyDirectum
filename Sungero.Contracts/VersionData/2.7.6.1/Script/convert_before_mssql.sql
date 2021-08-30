IF EXISTS (SELECT *  FROM sys.indexes  WHERE name='idx_ElectronicDocument_LifeCycleState_Discriminator_RespEmpl' 
   AND object_id = OBJECT_ID('[dbo].[Sungero_Content_EDoc]'))
begin
  EXEC sp_rename N'[dbo].[Sungero_Content_EDoc].[idx_ElectronicDocument_LifeCycleState_Discriminator_RespEmpl]', N'idx_EDoc_LifeCycleState_Discriminator_RespEmpl', N'INDEX';
end
