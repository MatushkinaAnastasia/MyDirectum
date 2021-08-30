-- Если нет русского типа документа "простой документ", то считаем это английским разворачиванием
UPDATE Sungero_WF_Task
SET ActionItemTAI_RecMan_Sungero = 
  CASE WHEN EXISTS (SELECT 1 FROM Sungero_Docflow_DocumentType WHERE Name = 'Простой документ') THEN 'В работу' ELSE 'Complete the action item' END
WHERE IsCompound_RecMan_Sungero = 'true' AND ActionItemTAI_RecMan_Sungero = Subject

UPDATE Sungero_WF_Text
SET Body = Sungero_WF_Task.ActionItemTAI_RecMan_Sungero 
FROM Sungero_WF_Task
WHERE Sungero_WF_Task.Id = Sungero_WF_Text.Task 
  AND Sungero_WF_Task.IsCompound_RecMan_Sungero = 'true' 
  AND (Sungero_WF_Text.Body IS NUll OR LTRIM(RTRIM(Sungero_WF_Text.Body)) = '')