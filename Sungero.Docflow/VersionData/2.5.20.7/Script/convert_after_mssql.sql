if (exists(select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'ApprovalTaskAdditionalApprovers'))
begin
  update Sungero_Docflow_TAprAddAprs
  set Approver = tempApprovers.Approver
  from ApprovalTaskAdditionalApprovers tempApprovers
  where Sungero_Docflow_TAprAddAprs.Id = tempApprovers.id

  drop table ApprovalTaskAdditionalApprovers
end

if (exists(select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'ManagerAssignmentAdditionalApprovers'))
begin
  update Sungero_Docflow_AAprManAddAprs
  set Approver = tempApprovers.Approver
  from ManagerAssignmentAdditionalApprovers tempApprovers
  where Sungero_Docflow_AAprManAddAprs.Id = tempApprovers.id

  drop table ManagerAssignmentAdditionalApprovers
end