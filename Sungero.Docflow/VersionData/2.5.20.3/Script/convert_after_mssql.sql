declare @oldOperationSet int = '-16777281'
declare @newOperationSet int = '-50331721'

update Sungero_System_AccessCtrlEnt
set OperationSet = @newOperationSet
where OperationSet = @oldOperationSet

update Sungero_Core_AccessRightTyp
set OperationSet = @newOperationSet, GrantedMask = @newOperationSet
where OperationSet = @oldOperationSet