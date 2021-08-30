DELETE 
  FROM Sungero_Core_AccessRightEnt aren
  USING Sungero_Core_AccessRights ar, Sungero_Core_Recipient r
  WHERE aren.AccessRights = ar.Id
	AND aren.Recipient = r.Id
	AND ar.EntityTypeGuid = '45DAFA52-EB1D-47FC-B363-896B1E7B5B21'
    AND (r.Sid = '440103EA-A766-47A8-98AD-5260CA32DE46' AND aren.AccessRightsType = 1
      OR r.Sid = 'C719C823-C4BD-4434-A34B-D7E83E524414')	