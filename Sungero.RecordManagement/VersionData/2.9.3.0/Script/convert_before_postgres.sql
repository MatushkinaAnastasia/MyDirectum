-- На postgres некорректно отрабатывало с "=" в subject составных поручений, заменено на LIKE
UPDATE Sungero_WF_Task
SET ActionItemTAI_RecMan_Sungero = 
  CASE 
    WHEN ActionItemTAI_RecMan_Sungero LIKE 'Составное поручение:%' THEN 'В работу' 
    WHEN ActionItemTAI_RecMan_Sungero LIKE 'Compound action item:%' THEN 'Complete the action item'
  END
WHERE IsCompound_RecMan_Sungero = 'true' AND (ActionItemTAI_RecMan_Sungero LIKE 'Составное поручение:%' OR ActionItemTAI_RecMan_Sungero LIKE 'Compound action item:%');

UPDATE Sungero_WF_Text
SET Body = Sungero_WF_Task.ActionItemTAI_RecMan_Sungero 
FROM Sungero_WF_Task
WHERE Sungero_WF_Task.Id = Sungero_WF_Text.Task 
  AND Sungero_WF_Task.IsCompound_RecMan_Sungero = 'true' 
  AND (Sungero_WF_Text.Body IS NUll OR LTRIM(RTRIM(Sungero_WF_Text.Body)) = '')