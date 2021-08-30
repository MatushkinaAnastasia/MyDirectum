-- Из-за переполнения таблицы с историей, убираем избыточные данные.
delete
from Sungero_Core_DatabookHistory
where EntityType in ('2dd7a803-8db7-40e1-9da6-b41c62d367c8', 'fd8945e2-e3b6-43f9-aac1-fb7ce4d984c6')
and action = 'Update'
and "user" in (select Id 
                 from Sungero_Core_Recipient
                 where Name = 'Service User')