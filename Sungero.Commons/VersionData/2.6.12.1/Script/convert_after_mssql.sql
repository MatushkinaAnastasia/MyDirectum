update 
  Sungero_Commons_Country
set 
  Code = '643'
where 
  Name = N'Российская Федерация'
  and Code is null

update 
  Sungero_Commons_Country
set 
  Code = '000'
where 
  Code is null
