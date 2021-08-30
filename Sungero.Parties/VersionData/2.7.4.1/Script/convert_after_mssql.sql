execute('update [dbo].[Sungero_Parties_ExchangeBoxes]
          set IsDefault = 1
          where IsDefault is null
        ')