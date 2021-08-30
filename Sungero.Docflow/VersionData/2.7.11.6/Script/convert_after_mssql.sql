update Sungero_Docflow_SignSettings
set Discriminator = '19f9b6d8-b072-4af0-b6e1-3f8a339506be'

update Sungero_Docflow_SignBizUnits
set Discriminator = '8de9438e-0af4-47df-8155-4b7b90c8d3d1'

update Sungero_Docflow_SignKinds
set Discriminator = '7eeef0f4-ad1e-4dc8-84f7-470a29f7fd91'

update Sungero_Docflow_SignDeps
set Discriminator = 'ba716656-bb74-4879-ada1-5f735e179840'

update Sungero_Core_Link
set DestinationTypeGuid = '19f9b6d8-b072-4af0-b6e1-3f8a339506be'
where DestinationTypeGuid in ('1E5B7D38-73C6-49D4-8782-E4DAB2E1C387', '99B2BA4B-8861-42CB-8BF6-3040E9995C11')

update Sungero_System_Locks
set EntityTypeGuid = '19f9b6d8-b072-4af0-b6e1-3f8a339506be'
where EntityTypeGuid in ('1E5B7D38-73C6-49D4-8782-E4DAB2E1C387', '99B2BA4B-8861-42CB-8BF6-3040E9995C11')

update Sungero_Core_DatabookHistory
set EntityType = '19f9b6d8-b072-4af0-b6e1-3f8a339506be'
where EntityType in ('1E5B7D38-73C6-49D4-8782-E4DAB2E1C387', '99B2BA4B-8861-42CB-8BF6-3040E9995C11')

update Sungero_WF_Attachment
set AttachmentTypeGuid = '19f9b6d8-b072-4af0-b6e1-3f8a339506be'
where AttachmentTypeGuid in ('1E5B7D38-73C6-49D4-8782-E4DAB2E1C387', '99B2BA4B-8861-42CB-8BF6-3040E9995C11')