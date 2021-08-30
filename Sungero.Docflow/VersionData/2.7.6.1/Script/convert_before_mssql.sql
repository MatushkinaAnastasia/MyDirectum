IF EXISTS (SELECT *  FROM sys.indexes  WHERE name='idx_Assignment_Discriminator_Performer_Author_MainTask_CompletedBy_Created' 
   AND object_id = OBJECT_ID('[dbo].[Sungero_WF_Assignment]'))
begin
  EXEC sp_rename N'[dbo].[Sungero_WF_Assignment].[idx_Assignment_Discriminator_Performer_Author_MainTask_CompletedBy_Created]', N'idx_Asg_Discriminator_Performer_Author_MTask_ComplBy_Created', N'INDEX';
end

IF EXISTS (SELECT *  FROM sys.indexes  WHERE name='idx_ElectronicDocument_Discriminator_Created_LifeCycleState' 
   AND object_id = OBJECT_ID('[dbo].[Sungero_Content_EDoc]'))
begin
  EXEC sp_rename N'[dbo].[Sungero_Content_EDoc].[idx_ElectronicDocument_Discriminator_Created_LifeCycleState]', N'idx_EDoc_Discriminator_Created_LifeCycleState', N'INDEX';
end

IF EXISTS (SELECT *  FROM sys.indexes  WHERE name='idx_ElectronicDocument_Discriminator_RegState_RegDate_RegNumber_Created' 
   AND object_id = OBJECT_ID('[dbo].[Sungero_Content_EDoc]'))
begin
  EXEC sp_rename N'[dbo].[Sungero_Content_EDoc].[idx_ElectronicDocument_Discriminator_RegState_RegDate_RegNumber_Created]', N'idx_EDoc_Discriminator_RegState_RegDate_RegNumber_Created', N'INDEX';
end

IF EXISTS (SELECT *  FROM sys.indexes  WHERE name='idx_ElectronicDocument_Index_DocRegister_Id_Discriminator' 
   AND object_id = OBJECT_ID('[dbo].[Sungero_Content_EDoc]'))
begin
  EXEC sp_rename N'[dbo].[Sungero_Content_EDoc].[idx_ElectronicDocument_Index_DocRegister_Id_Discriminator]', N'idx_EDoc_Index_DocRegister_Id_Discriminator', N'INDEX';
end

IF EXISTS (SELECT *  FROM sys.indexes  WHERE name='Sungero_DocRegister_CurrentCode_DocRegisterId_Month_Year_LeadDoc' 
   AND object_id = OBJECT_ID('[dbo].[Sungero_DocRegister_CurrentCode]'))
begin
  EXEC sp_rename N'[dbo].[Sungero_DocRegister_CurrentCode].[Sungero_DocRegister_CurrentCode_DocRegisterId_Month_Year_LeadDoc]', N'Sungero_DocRegister_CurrentCode_DocRegisterId_Month_Year_LDoc', N'INDEX';
end
