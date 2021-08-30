UPDATE sungero_docflow_ruleStages
SET StageType = 'Approvers'
WHERE StageType = 'AdditionallyApp';

UPDATE sungero_docflow_approvalstage
SET StageType = 'Approvers', AllowAddAppr = 'true'
WHERE StageType = 'AdditionallyApp';

UPDATE sungero_docflow_approvalstage
SET AllowAddAppr = 'false'
WHERE AllowAddAppr IS NULL