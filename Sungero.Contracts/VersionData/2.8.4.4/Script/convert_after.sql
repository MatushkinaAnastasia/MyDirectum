update Sungero_Content_EDoc
set ValidFrom_Contrac_Sungero = null
where Discriminator = '265f2c57-6a8a-4a15-833b-ca00e8047fa5'
and ValidFrom_Contrac_Sungero > ValidTill_Contrac_Sungero