do $$
begin
update Sungero_Core_Recipient 
set Status = 'Closed'
where Discriminator = 'c5bd2267-3ce0-4f4b-a891-cce8638a24fa'
  and Status != 'Closed';
  
update Sungero_Docflow_Project pupd
set Modified_Project_Sungero = (select HistoryDate
                                    from  Sungero_Core_DatabookHistory h
                                    where h.EntityId = pupd.id
                                      and h.EntityType = '4383f2ff-56e6-46f4-b4ef-cc17e6aeef40'
                                      and Action in ('Update', 'Create')
                                  order by h.HistoryDate desc
                                  limit 1)
where Modified_Project_Sungero is null;   

end$$;