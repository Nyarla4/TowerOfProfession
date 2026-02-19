using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class ShowIfAttribute : PropertyAttribute
{
    public enum ConditionOperator
    {
        And,
        Or
    }

    public enum ActionOnConditionFail
    {
        DontDraw,
        JustDisable
    }

    public ActionOnConditionFail Action { get; private set; }
    public ConditionOperator Operator { get; private set; }
    public string[] Conditions { get; private set; }

    public object[] CompareValues { get; private set; }
    public ShowIfAttribute(ActionOnConditionFail action, ConditionOperator conditionOperator, params string[] conditions)
    {
        Action = action;
        Operator = conditionOperator;
        Conditions = conditions;
        CompareValues = null;
    }

    public ShowIfAttribute(ActionOnConditionFail action, ConditionOperator conditionOperator, object compareValue, params string[] conditions)
    {
        Action = action;
        Operator = conditionOperator;
        Conditions = conditions;
        CompareValues = new object[] { compareValue };
    }

    public ShowIfAttribute(ActionOnConditionFail action, ConditionOperator conditionOperator, object[] compareValues, params string[] conditions)
    {
        Action = action;
        Operator = conditionOperator;
        Conditions = conditions;
        CompareValues = compareValues;
    }
}