IF EXISTS (SELECT *  FROM sys.indexes  WHERE name='idx_Assignment_Status_Discriminator_Performer' 
   AND object_id = OBJECT_ID('[dbo].[Sungero_WF_Assignment]'))
begin
  EXEC sp_rename N'[dbo].[Sungero_WF_Assignment].[idx_Assignment_Status_Discriminator_Performer]', N'idx_Asg_Status_Discriminator_Performer', N'INDEX';
end