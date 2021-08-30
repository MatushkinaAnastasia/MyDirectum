using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalRule;

namespace Sungero.Docflow.Server
{
  partial class ApprovalRuleFunctions
  {
    #region Проверка возможности существования маршрутов правила
    
    /// <summary>
    /// Проверка возможности существования маршрута правила.
    /// </summary>
    /// <param name="route">Маршрут.</param>
    /// <param name="ruleConditions">Условия.</param>
    /// <param name="conditionStep">Этап.</param>
    /// <returns>Возможность существования.</returns>
    public override bool CheckRoutePossibility(List<Structures.ApprovalRuleBase.RouteStep> route,
                                               List<Structures.ApprovalRuleBase.ConditionRouteStep> ruleConditions,
                                               Structures.ApprovalRuleBase.RouteStep conditionStep)
    {
      var possibleStage = base.CheckRoutePossibility(route, ruleConditions, conditionStep);
      var conditionType = _obj.Conditions.First(c => c.Number == conditionStep.StepNumber).Condition.ConditionType;
      
      // Проверка условий по адресату.
      if (conditionType == Docflow.Condition.ConditionType.Addressee)
      {
        var addresseeConditions = this.GetAddresseeConditionsInRoute(route).Where(c => c.StepNumber != conditionStep.StepNumber).ToList();
        possibleStage = this.CheckAddresseeConditions(addresseeConditions, conditionStep);
      }
      
      return possibleStage;
    }

    /// <summary>
    /// Проверить возможность существования данного маршрута с условиями по адресату.
    /// </summary>
    /// <param name="allConditions">Все условия в данном маршруте.</param>
    /// <param name="condition">Текущее условие.</param>
    /// <returns>Возможность существования данного маршрута.</returns>
    public bool CheckAddresseeConditions(List<Structures.ApprovalRuleBase.RouteStep> allConditions, Structures.ApprovalRuleBase.RouteStep condition)
    {
      var conditionItem = _obj.Conditions.Where(x => x.Number == condition.StepNumber).FirstOrDefault();
      var addressees = Conditions.As(conditionItem.Condition).Addressees.Select(x => x.Addressee).ToList();
      
      foreach (var previousCondition in allConditions.TakeWhile(x => !Equals(x, condition)))
      {
        var previoisConditionItem = _obj.Conditions.Where(x => x.Number == previousCondition.StepNumber).FirstOrDefault();
        var previousAddressees = Conditions.As(previoisConditionItem.Condition).Addressees.Select(x => x.Addressee).ToList();
        
        var result = CheckConsistencyConditions(addressees, previousAddressees, condition, previousCondition);
        if (result != null)
          return result.Value;
      }
      
      return true;
    }
    
    /// <summary>
    /// Получить все условия по адресату в данном маршруте.
    /// </summary>
    /// <param name="route">Маршрут.</param>
    /// <returns>Условия.</returns>
    public List<Structures.ApprovalRuleBase.RouteStep> GetAddresseeConditionsInRoute(List<Structures.ApprovalRuleBase.RouteStep> route)
    {
      return route.Where(e => _obj.Conditions.Any(c => Equals(c.Number, e.StepNumber) && c.Condition.ConditionType ==
                                                  Docflow.Condition.ConditionType.Addressee)).ToList();
    }
    
    #endregion
  }
}