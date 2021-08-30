-- Если нет русского типа документа "простой документ", то считаем это английским разворачиванием
UPDATE Sungero_WF_Task
SET ActionItemTAI_RecMan_Sungero = 
  CASE WHEN EXISTS (SELECT 1 FROM Sungero_Docflow_DocumentType WHERE Name = 'Простой документ') THEN 'В работу' ELSE 'Complete the action item' END
WHERE IsCompound_RecMan_Sungero = 'true' AND ActionItemTAI_RecMan_Sungero = Subject