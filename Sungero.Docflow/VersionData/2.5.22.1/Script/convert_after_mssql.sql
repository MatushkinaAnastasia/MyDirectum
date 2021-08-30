-- Заполним сохраненными значениями.
if exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME='TAprReqAprsConverter')
begin
  if exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME='Sungero_Docflow_TAprReqAprs')
  begin
    update Sungero_Docflow_TAprReqAprs
	set Approver = tac.Approver
	from TAprReqAprsConverter tac
	where Sungero_Docflow_TAprReqAprs.Approver is null
	  and Sungero_Docflow_TAprReqAprs.Id = tac.Id
  end
  drop table TAprReqAprsConverter
end