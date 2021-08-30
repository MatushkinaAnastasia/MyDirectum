if exists (select
           *
           from information_schema.columns
           where table_name = 'Sungero_WF_Task'
             and column_name = 'Assignee_ExCore_Sungero')
begin
  execute('update t
           set Assignee_ExCore_Sungero = b.Responsible,
               MaxDeadline = DATEADD(d, 2, t.Created)
           from Sungero_WF_Task t
           join Sungero_ExCore_BoxBase b
             on t.Box_ExCore_Sungero = b.Id
           where t.Discriminator = N''1e5b11de-bd28-4dc2-a03c-74b8db9ac1c4''
             and t.Assignee_ExCore_Sungero is null 
              or t.MaxDeadline is null')

  execute('update asmt
           set Box_ExCore_Sungero = t.Box_ExCore_Sungero,
               Counterparty_ExCore_Sungero = t.Counterparty_ExCore_Sungero
           from Sungero_WF_Assignment asmt
           join (select MainTask, Box_ExCore_Sungero, Counterparty_ExCore_Sungero 
                   from Sungero_WF_Task
                   where Discriminator = N''1e5b11de-bd28-4dc2-a03c-74b8db9ac1c4'') as t
             on asmt.MainTask = t.MainTask
           where asmt.Box_ExCore_Sungero is null 
              or asmt.Counterparty_ExCore_Sungero is null')
end
