update Sungero_Content_EDoc
set Document_Docflow_Sungero = LeadDocument_FinArch_Sungero
where LeadDocument_FinArch_Sungero is not null

update Sungero_Core_RelationTypeMa
set TargetProperty = '125589DA-53BA-4D6D-8BD5-0AB3CD1D19DA'
where TargetProperty = '9939D9CC-3D12-4C25-BA19-35B5E60F205D' and TargetType = 'F2F5774D-5CA3-4725-B31D-AC618F6B8850'