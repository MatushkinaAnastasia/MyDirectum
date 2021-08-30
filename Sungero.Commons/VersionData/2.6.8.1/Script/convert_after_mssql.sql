update 
	Sungero_Commons_Currency
set 
	AlphaCode = 'xxx'
where 
  AlphaCode is null
 
update 
	Sungero_Commons_Currency
set 
	NumericCode = '643'
where 
  AlphaCode = 'RUB' and NumericCode is null

update 
	Sungero_Commons_Currency
set 
	NumericCode = '978'
where 
  AlphaCode = 'EUR' and NumericCode is null

update 
	Sungero_Commons_Currency
set 
	NumericCode = '840'
where 
  AlphaCode = 'USD' and NumericCode is null

update 
	Sungero_Commons_Currency
set 
	NumericCode = '000'
where 
  NumericCode is null