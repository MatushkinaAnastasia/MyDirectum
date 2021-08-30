-- На postgres некорректно отрабатывало с "=" в subject составных поручений, заменено на LIKE
UPDATE Sungero_WF_Task
SET ActionItemTAI_RecMan_Sungero = 
  CASE 
    WHEN ActionItemTAI_RecMan_Sungero LIKE 'Составное поручение:%' THEN 'В работу' 
    WHEN ActionItemTAI_RecMan_Sungero LIKE 'Compound action item:%' THEN 'Complete the action item'
  END
WHERE IsCompound_RecMan_Sungero = 'true' AND (ActionItemTAI_RecMan_Sungero LIKE 'Составное поручение:%' OR ActionItemTAI_RecMan_Sungero LIKE 'Compound action item:%')