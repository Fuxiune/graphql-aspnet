﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.RulesEngine.RuleSets.DocumentValidation.QueryFragmentSteps
{
    using GraphQL.AspNet.Interfaces.PlanGeneration.DocumentParts;
    using GraphQL.AspNet.PlanGeneration.Contexts;
    using GraphQL.AspNet.RulesEngine.RuleSets.DocumentValidation.Common;

    /// <summary>
    /// <para>(5.5.1.1) Validate that each named fragment has a unique name within the document scope.</para>
    /// <para>Reference: https://graphql.github.io/graphql-spec/October2021/#sec-Fragment-Name-Uniqueness .</para>
    /// </summary>
    internal class Rule_5_5_1_1_FragmentNamesMustBeUnique
        : DocumentPartValidationRuleStep<INamedFragmentDocumentPart>
    {
        /// <inheritdoc />
        public override bool Execute(DocumentValidationContext context)
        {
            var namedFragment = (INamedFragmentDocumentPart)context.ActivePart;

            var key = $"5.5.1.1|namedFragmentUniqueness|name:{namedFragment.Name}";
            if (context.GlobalKeys.ContainsKey(key))
                return true;

            context.GlobalKeys.Add(key, true);

            if (!context.Document.NamedFragments.IsUnique(namedFragment.Name))
            {
                this.ValidationError(
                    context,
                    $"Duplicate Fragment Name. The fragment name '{namedFragment.Name.ToString()}' must be unique in this document. Ensure that each " +
                    "fragment in the document is unique (case-sensitive).");

                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override string RuleNumber => "5.5.1.1";

        /// <inheritdoc />
        protected override string RuleAnchorTag => "#sec-Fragment-Name-Uniqueness";
    }
}