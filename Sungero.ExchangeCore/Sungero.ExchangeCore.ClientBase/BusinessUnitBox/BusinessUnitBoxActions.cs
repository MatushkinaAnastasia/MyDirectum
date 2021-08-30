using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.BusinessUnitBox;

namespace Sungero.ExchangeCore.Client
{
  partial class BusinessUnitBoxActions
  {
    public virtual void CheckConnection(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (string.IsNullOrEmpty(_obj.Password))
      {
        Dialogs.NotifyMessage(BusinessUnitBoxes.Resources.EnterPasswordToCheckConnection);
        return;
      }
      
      var result = Functions.BusinessUnitBox.Remote.CheckConnection(_obj);
      if (string.IsNullOrWhiteSpace(result))
      {
        Dialogs.NotifyMessage(BusinessUnitBoxes.Resources.ConnectionEstablished);
        
        Functions.BusinessUnitBox.Remote.UpdateExchangeServiceCertificates(_obj);
      }
      else
      {
        Dialogs.NotifyMessage(result);
      }

      if (!Functions.BusinessUnitBox.Remote.CheckAllResponsibleCertificates(_obj, _obj.Responsible))
        e.AddWarning(BusinessUnitBoxes.Resources.CertificateNotFound);
 
      if (!Functions.BusinessUnitBox.Remote.CheckBusinessUnitTinTRRC(_obj))
        e.AddWarning(BusinessUnitBoxes.Resources.OrganizationFailed);
    }

    public virtual bool CanCheckConnection(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return this.CanLogin(e);
    }

    public virtual void OpenLogonUrl(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.GoToWebsite(_obj.ExchangeService.LogonUrl, e);
    }

    public virtual bool CanOpenLogonUrl(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      if (_obj.ExchangeService == null)
        return false;
      
      return Functions.Module.CanGoToWebsite(_obj.ExchangeService.LogonUrl);
    }

    public virtual void Login(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;

      var errors = Functions.BusinessUnitBox.Remote.CheckProperties(_obj);
      foreach (var error in errors)
        e.AddError(error);

      if (errors.Any())
        return;

      var dialog = Dialogs.CreateInputDialog(BusinessUnitBoxes.Resources.LoginActionTitle);
      var password = dialog.AddPasswordString(_obj.Info.Properties.Password.LocalizedName, true);
      password.MaxLength(Constants.BusinessUnitBox.PasswordMaxLength);
      dialog.Buttons.AddOkCancel();
      dialog.Buttons.Default = DialogButtons.Ok;
      
      dialog.SetOnButtonClick(
        x =>
        {
          if (x.Button == DialogButtons.Ok && x.IsValid)
          {
            var loginResult = Functions.BusinessUnitBox.Remote.Login(_obj, password.Value);
            if (loginResult != string.Empty)
            {
              x.AddError(loginResult);
            }
            else
            {
              Dialogs.NotifyMessage(BusinessUnitBoxes.Resources.ConnectionEstablished);
              Functions.BusinessUnitBox.Remote.UpdateExchangeServiceCertificates(_obj);
            }
            
            if (!Functions.BusinessUnitBox.Remote.CheckAllResponsibleCertificates(_obj, _obj.Responsible))
              e.AddWarning(BusinessUnitBoxes.Resources.CertificateNotFound);
          }
        });
      dialog.Show();
    }

    public virtual bool CanLogin(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() && !Docflow.PublicFunctions.Module.IsLockedByOther(_obj) && _obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Active;
    }
  }

}