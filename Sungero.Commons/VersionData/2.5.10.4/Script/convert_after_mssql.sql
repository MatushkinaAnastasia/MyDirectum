if exists (select 1
             from
               Information_schema.Columns
             where
               Column_name = 'Code'
               and Table_name = 'Sungero_Commons_Region')
begin
  -- Создать временную таблицу
  if exists (select * from information_schema.tables where table_name = 'regionsTempTableUpdate25104')
    drop table regionsTempTableUpdate25104
    
  create table regionsTempTableUpdate25104 (Name nvarchar(250), Code nvarchar(10))

  --Наполнить временную таблицу
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Курская область', N'46')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Еврейская АО', N'79')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Краснодарский край', N'23')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Камчатский край', N'41')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Тюменская область', N'72')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Карелия', N'10')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Ямало-Ненецкий АО', N'89')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Белгородская область', N'31')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Ставропольский край', N'26')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Приморский край', N'25')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Башкортостан', N'02')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Ивановская область', N'37')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Ханты-Мансийский Автономный округ - Югра АО', N'86')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Московская область', N'50')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Чукотский АО', N'87')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Орловская область', N'57')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Карачаево-Черкесская', N'09')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Северная Осетия - Алания', N'15')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Чувашская Республика - Чувашия', N'21')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Вологодская область', N'35')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Калужская область', N'40')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Иркутская область', N'38')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Ульяновская область', N'73')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Томская область', N'70')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Магаданская область', N'49')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Смоленская область', N'67')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Кемеровская область', N'42')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Коми', N'11')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Амурская область', N'28')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Пензенская область', N'58')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Свердловская область', N'66')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Брянская область', N'32')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Калмыкия', N'08')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Ярославская область', N'76')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Татарстан', N'16')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Оренбургская область', N'56')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Калининградская область', N'39')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Курганская область', N'45')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Тульская область', N'71')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Саратовская область', N'64')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Москва', N'77')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Мурманская область', N'51')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Липецкая область', N'48')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Тамбовская область', N'68')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Дагестан', N'05')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Пермский край', N'59')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Костромская область', N'44')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Байконур', N'99')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Кировская область', N'43')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Красноярский край', N'24')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Удмуртская республика', N'18')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Хакасия', N'19')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Новосибирская область', N'54')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Архангельская область', N'29')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Саха /Якутия/', N'14')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Новгородская область', N'53')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Чеченская республика', N'20')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Тверская область', N'69')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Ингушетия', N'06')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Адыгея', N'01')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Ленинградская область', N'47')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Марий Эл', N'12')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Рязанская область', N'62')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Забайкальский край', N'75')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Астраханская область', N'30')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Хабаровский край', N'27')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Нижегородская область', N'52')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Владимирская область', N'33')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Мордовия', N'13')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Омская область', N'55')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Псковская область', N'60')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Волгоградская область', N'34')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Тыва', N'17')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Кабардино-Балкарская', N'07')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Сахалинская область', N'65')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Санкт-Петербург', N'78')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Челябинская область', N'74')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Ненецкий АО', N'83')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Воронежская область', N'36')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Ростовская область', N'61')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Самарская область', N'63')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Алтайский край', N'22')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Алтай', N'04')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Крым', N'91')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Севастополь', N'92')
  insert regionsTempTableUpdate25104 ([Name], [Code]) values (N'Республика Бурятия', N'03')

  -- Обновить регионы согласно имеющимся данным.
  update Sungero_Commons_Region
  set Code = res.Code
  from (
  select 
    reg.Name,
    temp.Code
  from Sungero_Commons_Region reg
  left join regionsTempTableUpdate25104 temp
  on reg.Name = temp.Name
  ) res
  where Sungero_Commons_Region.Name = Res.Name

  -- Удалить временную таблицу
  if exists (select * from information_schema.tables where table_name = 'regionsTempTableUpdate25104')
    drop table regionsTempTableUpdate25104
end