update t
set t.ActualDate_RecMan_Sungero = (select top 1 Completed from Sungero_WF_Assignment a where a.Task = t.Id and Completed is not null order by Completed desc)
from Sungero_WF_Task t
where t.Discriminator = 'c290b098-12c7-487d-bb38-73e2c98f9789' 
  and t.ActualDate_RecMan_Sungero is not null
  and cast(cast(t.DeadlineTAI_RecMan_Sungero as date) as datetime) != t.DeadlineTAI_RecMan_Sungero