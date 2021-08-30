update 
  Sungero_Docflow_DocumentKind
set 
  GenDocName = 1
where 
  Name = 'Входящий документ эл. обмена'
and 
  DocumentType in
    (select id
    from 
      Sungero_Docflow_DocumentType
    where 
      DocTypeGuid = 'cf8357c3-8266-490d-b75e-0bd3e46b1ae8')