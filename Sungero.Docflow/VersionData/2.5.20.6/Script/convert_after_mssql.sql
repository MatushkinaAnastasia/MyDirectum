if exists(select * from information_schema.tables where table_name='Sungero_Docflow_StgRecipients')
  and exists(select * from information_schema.tables where table_name='Sungero_Docflow_StgAssignees')
begin
                                     
  declare @approvalStage int
  declare @assignee int

  declare @cursor cursor

  set @cursor = cursor scroll for select ApprovalStage, Assignee from Sungero_Docflow_StgAssignees

  open @cursor

  fetch next from @cursor into @approvalStage, @assignee

  while @@FETCH_STATUS = 0
  begin
    if not exists(select * from Sungero_Docflow_StgRecipients where ApprovalStage = @approvalStage and StgRecipient = @assignee)
	begin
      declare @nextId int

	  execute Sungero_System_GetNewId 'Sungero_Docflow_StgRecipients', @nextId output, 1

	  insert into Sungero_Docflow_StgRecipients (Id, Discriminator, ApprovalStage, StgRecipient)
	  values (@nextId, '278a6b5f-97b8-4f82-a0e7-9b9031ed27d4', @approvalStage, @assignee)
	end

	fetch next from @cursor into @approvalStage, @assignee
  end

  close @cursor

  delete from Sungero_Docflow_StgAssignees
end