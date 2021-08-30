-- Counterparty_Docflow_Sungero
if exists (SELECT * FROM sys.indexes 
               WHERE name='IXN27F62FB17FB390EF' AND object_id = OBJECT_ID('Sungero_Content_EDoc'))
begin
  drop index IXN27F62FB17FB390EF ON Sungero_Content_EDoc
end

-- ContrCurrency_Docflow_Sungero
if exists (SELECT * FROM sys.indexes 
               WHERE name='IXNF89F40C1A0FB954C' AND object_id = OBJECT_ID('Sungero_Content_EDoc'))
begin
  drop index IXNF89F40C1A0FB954C ON Sungero_Content_EDoc
end

-- PartySignatory_Docflow_Sungero
if exists (SELECT * FROM sys.indexes 
               WHERE name='IXNBEE4FC70C2D1090A' AND object_id = OBJECT_ID('Sungero_Content_EDoc'))
begin
  drop index IXNBEE4FC70C2D1090A ON Sungero_Content_EDoc
end

-- Contact_Contrac_Sungero
if exists (SELECT * FROM sys.indexes 
               WHERE name='IXN22EDEA54EF93DCE6' AND object_id = OBJECT_ID('Sungero_Content_EDoc'))
begin
  drop index IXN22EDEA54EF93DCE6 ON Sungero_Content_EDoc
end

-- RespEmpl_Contrac_Sungero
if exists (SELECT * FROM sys.indexes 
               WHERE name='IXN0668F037528318E1' AND object_id = OBJECT_ID('Sungero_Content_EDoc'))
begin
  drop index IXN0668F037528318E1 ON Sungero_Content_EDoc
end