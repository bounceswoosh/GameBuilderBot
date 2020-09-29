﻿using GameBuilderBot.ExpressionHandling;
using GameBuilderBot.Models;
using GameBuilderBot.Services;
using System;

namespace GameBuilderBot.Runners
{
    public class AddValueRunner : AssignmentRunner
    {
        public AddValueRunner(GameHandlingService gameHandler, ExportService exportService) : base(gameHandler, exportService)
        {
        }

        override protected int CalculateValue(GameState state, string fieldName, string expression)
        {
            int value = 0;

            if (state.FieldHasValue(fieldName))
            {
                value = Convert.ToInt32(state.Fields[fieldName].Value.ToString());
            }

            MathExpression mathexpression = new MathExpression(expression, state.Fields);
            return value + Convert.ToInt32(mathexpression.Evaluate().ToString());
        }

        public override string OneLinerHelp()
        {
            return "`!add foo 12` subtracts 12 from the variable foo";
        }
    }
}
