-- вид по умолчанию тот, что создан при инициализации
update Sungero_Docflow_DocumentKind upd
  set IsDefault = true
from Sungero_Docflow_DocumentKind as k
join Sungero_Core_ExternalLink l
  on k.Id = l.EntityId
  and l.EntityTypeGuid = '14a59623-89a2-4ea8-b6e9-2ad4365f358c'
join Sungero_Docflow_DocumentType t
  on k.DocumentType = t.Id
where t.DocTypeGuid in ('09584896-81e2-4c83-8f6c-70eb8321e1d0', '74c9ddd4-4bc4-42b6-8bb0-c91d5e21fb8a', 'f50c4d8a-56bc-43ef-bac3-856f57ca70be', '49d0c5e7-7069-44d2-8eb6-6e3098fc8b10')
  and k.Status != 'Closed'
  and upd.Id = k.Id
  and not exists(select 1 
                   from Sungero_Docflow_DocumentKind kind
				           where kind.DocumentType = k.DocumentType
				             and kind.IsDefault is true);

-- Наиболее используемый вид должен быть видом по умолчанию
update Sungero_Docflow_DocumentKind upd
  set IsDefault = true
from Sungero_Docflow_DocumentKind k
where k.Id = (select doc.DocumentKind_Docflow_Sungero
                from Sungero_Content_EDoc doc
				        join Sungero_Docflow_DocumentKind kind
				          on doc.DocumentKind_Docflow_Sungero = kind.Id
				        where kind.Status != 'Closed'
				          and k.DocumentType = kind.DocumentType
				      group by doc.DocumentKind_Docflow_Sungero
				      order by count(1) desc
                                limit 1)
  and not exists(select 1 
                   from Sungero_Docflow_DocumentKind kind
				           where kind.DocumentType = k.DocumentType
				             and kind.IsDefault is true)
  and upd.Id = k.Id;


--вид по умолчанию первый из списка, отсортированного по имени
update Sungero_Docflow_DocumentKind upd
  set IsDefault = true
from Sungero_Docflow_DocumentKind k
join Sungero_Docflow_DocumentType t
  on k.DocumentType = t.Id
where t.DocTypeGuid not in ('56df80b3-a795-4378-ace5-c20a2b1fb6d9', '09584896-81e2-4c83-8f6c-70eb8321e1d0', '74c9ddd4-4bc4-42b6-8bb0-c91d5e21fb8a', 'f50c4d8a-56bc-43ef-bac3-856f57ca70be', '49d0c5e7-7069-44d2-8eb6-6e3098fc8b10')
  and k.Status != 'Closed'
  and not exists(select 1 
                   from Sungero_Docflow_DocumentKind kind
				           where kind.DocumentType = k.DocumentType
				             and kind.IsDefault is true)
  and k.Id = (select kind.Id from Sungero_Docflow_DocumentKind kind where k.DocumentType = kind.DocumentType order by k.Name limit 1)
  and upd.Id = k.Id;


--для остальных проставить false
update Sungero_Docflow_DocumentKind upd
  set IsDefault = false
from Sungero_Docflow_DocumentKind k
where k.IsDefault is null
  and upd.Id = k.Id;
 