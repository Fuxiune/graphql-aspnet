﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.RulesEngine.RuleSets.DocumentValidation.FieldSelectionSetSteps
{
    using GraphQL.AspNet.Interfaces.PlanGeneration.DocumentParts;
    using GraphQL.AspNet.PlanGeneration.Contexts;
    using GraphQL.AspNet.Schemas.TypeSystem;
    using GraphQL.AspNet.ValidationRules.RuleSets.DocumentValidation.Common;

    /// <summary>
    /// A rule to validate that a field selection set was fully populated during construction
    /// or directive exectution and is usable to create a query plan.
    /// </summary>
    internal class FieldSelectionSet_DocumentPartCheck
        : DocumentPartValidationStep<IFieldSelectionSetDocumentPart>
    {
        /// <inheritdoc />
        public override bool Execute(DocumentValidationContext context)
        {
            var docPart = (IFieldSelectionSetDocumentPart)context.ActivePart;

            if (docPart.GraphType == null)
            {
                this.ValidationError(
                    context,
                    docPart.Node,
                    "The field selection set does not reference a graph type. All selection sets must " +
                    "reference an OBJECT, INTERFACE or UNION graph type to validate its contents.");

                return false;
            }
            else if (docPart.GraphType.Kind.IsLeafKind() && docPart.Children.Count > 0)
            {
                this.ValidationError(
                    context,
                    docPart.Node,
                    $"The graph type of the field selection set '{docPart.GraphType.Name}' is a leaf type and does not declare any fields.");

                return false;
            }

            return true;
        }
    }
}