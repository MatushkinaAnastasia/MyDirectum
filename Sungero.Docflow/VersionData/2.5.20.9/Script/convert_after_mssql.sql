if (exists(select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Sungero_Docflow_TAprAddAprsEx'))
begin
  declare @cursor cursor

  declare @task int
  declare @approver int

  set @cursor = cursor scroll for select Task, Approver from Sungero_Docflow_TAprAddAprs

  open @cursor

  fetch next from @cursor into @task, @approver

  while @@FETCH_STATUS = 0
  begin
    declare @nextId int

    execute Sungero_System_GetNewId 'Sungero_Docflow_TAprAddAprsEx', @nextId output, 1

    insert into Sungero_Docflow_TAprAddAprsEx (Id, Discriminator, Task, Approver)
    values (@nextId, '77b0b28f-e4b6-4e28-8295-62959ce8693c', @task, @approver)

    fetch next from @cursor into @task, @approver
  end

  close @cursor
end