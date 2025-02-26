﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.ValidationRules.RuleSets.DocumentValidation.QueryInputArgumentSteps
{
    using GraphQL.AspNet.PlanGeneration.Contexts;
    using GraphQL.AspNet.PlanGeneration.Document.Parts;
    using GraphQL.AspNet.PlanGeneration.Document.Parts.QueryInputValues;
    using GraphQL.AspNet.ValidationRules.RuleSets.DocumentValidation.Common;

    /// <summary>
    /// This rule is roughly the same as 5.6.1 (validating a value supplied to a scoped graph type),
    /// but applies to a supplied variable value instead of deconstructed object litteral.
    /// </summary>
    internal class Rule_5_8_5_VariableValueMustBeUsableInContext : DocumentPartValidationRuleStep
    {
        /// <inheritdoc />
        public override bool ShouldExecute(DocumentValidationContext context)
        {
            return context.ActivePart is QueryInputArgument arg && arg.Value is QueryVariableReferenceInputValue;
        }

        /// <inheritdoc />
        public override bool Execute(DocumentValidationContext context)
        {
            var argument = context.ActivePart as QueryInputArgument;
            var qvr = argument.Value as QueryVariableReferenceInputValue;

            // ensure the type expressions are compatible at the location used
            if (!qvr.Variable.TypeExpression.Equals(argument.TypeExpression))
            {
                this.ValidationError(
                    context,
                    argument.Node,
                    "Invalid Variable Argument. The type expression for the variable used on the " +
                    $"{argument.InputType} '{argument.Name}' could " +
                    $"not be successfully coerced to the required type. Expected '{argument.TypeExpression}' but got '{qvr.Variable.TypeExpression}'. Double check " +
                    $"the declared graph type of the variable and ensure it matches the required type of '{argument.Name}'.");

                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override string RuleNumber => "5.8.5";

        /// <inheritdoc />
        protected override string RuleAnchorTag => "#sec-All-Variable-Usages-are-Allowed";
    }
}