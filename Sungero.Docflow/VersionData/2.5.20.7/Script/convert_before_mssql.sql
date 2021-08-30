-- Получение доп согласующих задачи и сохранение во временную таблицу.
if (exists(select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Sungero_Docflow_TAprAddAprs'))
begin
  if (exists(select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'ApprovalTaskAdditionalApprovers'))
  begin 
    drop table ApprovalTaskAdditionalApprovers
  end

  create table ApprovalTaskAdditionalApprovers (id int, Approver int)

  insert into ApprovalTaskAdditionalApprovers
  select 
    id,
    Approver
  from Sungero_Docflow_TAprAddAprs
end

-- Получение доп согласующих задания руководителя и сохранение во временную таблицу.
if (exists(select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Sungero_Docflow_AAprManAddAprs'))
begin
  if (exists(select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'ManagerAssignmentAdditionalApprovers'))
  begin 
    drop table ManagerAssignmentAdditionalApprovers
  end

  create table ManagerAssignmentAdditionalApprovers (id int, Approver int)

  insert into ManagerAssignmentAdditionalApprovers
  select 
    id,
    Approver
  from Sungero_Docflow_AAprManAddAprs
end