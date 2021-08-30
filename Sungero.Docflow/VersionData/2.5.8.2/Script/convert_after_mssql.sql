update 
	Sungero_Docflow_DocSendAction 
set 
	SystemName = 'SendForReview'
where 
	ActionGuid = 'df0a07e9-e59d-49be-9ace-004b762afaae';

update 
	Sungero_Docflow_DocSendAction
set 
	SystemName = 'SendForApproval'
where 
	ActionGuid = 'd021d43d-7949-4bdf-b0a0-4db25547f388';

update 
	Sungero_Docflow_DocSendAction
set 
	SystemName = 'SendForFreeApproval'
where 
	ActionGuid = '6d7b4a3b-387c-4735-96f9-9885369e25ca';

update 
	Sungero_Docflow_DocSendAction
set 
	SystemName = 'SendForExecution'
where 
	ActionGuid = 'c3baf5b8-a02a-4dcf-9fea-c004cd5e2b1b';