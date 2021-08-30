update Sungero_Core_Recipient 
set Status = 'Closed'
where Discriminator = 'c5bd2267-3ce0-4f4b-a891-cce8638a24fa'
  and Status != 'Closed'
  
update p
set p.Modified_Project_Sungero = (select top(1) h.HistoryDate
                                    from  Sungero_Core_DatabookHistory h
                                    where h.EntityId = p.id
                                      and h.EntityType = '4383f2ff-56e6-46f4-b4ef-cc17e6aeef40'
                                      and Action in ('Update', 'Create')
                                  order by h.HistoryDate desc)
from Sungero_Docflow_Project p
where p.Modified_Project_Sungero is null  