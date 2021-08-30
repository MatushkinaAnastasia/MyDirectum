UPDATE ss
SET ss.DocumentFlow = CASE
							WHEN ss.Discriminator = '99B2BA4B-8861-42CB-8BF6-3040E9995C11'
							THEN 'Contracts'
							WHEN (SELECT COUNT(*) from
									(SELECT dk.DocumentFlow
									FROM 
									Sungero_Docflow_SignKinds AS sk, 
									Sungero_Docflow_DocumentKind AS dk
									WHERE sk.SignSettings = ss.Id AND sk.DocumentKind = dk.Id
									GROUP BY dk.DocumentFlow) AS t) = 1 
							THEN  (SELECT dk.DocumentFlow
								   FROM
								   Sungero_Docflow_SignKinds AS sk, 
								   Sungero_Docflow_DocumentKind AS dk
								   WHERE sk.SignSettings = ss.Id AND sk.DocumentKind = dk.Id
								   GROUP BY dk.DocumentFlow)
							ELSE 'All'
					   END
FROM Sungero_Docflow_SignSettings ss
WHERE ss.DocumentFlow IS NULL