update Sungero_WF_Task t
set ActualDate_RecMan_Sungero = (select Completed from Sungero_WF_Assignment a where a.Task = t.Id and Completed is not null order by Completed desc limit 1)
where t.Discriminator = 'c290b098-12c7-487d-bb38-73e2c98f9789' 
  and t.ActualDate_RecMan_Sungero is not null
  and cast(cast(t.DeadlineTAI_RecMan_Sungero as date) as timestamp) != t.DeadlineTAI_RecMan_Sungero