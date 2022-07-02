﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.RulesEngine.RuleSets.DocumentConstruction.Steps
{
    using GraphQL.AspNet.Interfaces.PlanGeneration.DocumentParts;
    using GraphQL.AspNet.Interfaces.TypeSystem;
    using GraphQL.AspNet.Parsing.SyntaxNodes;
    using GraphQL.AspNet.PlanGeneration.Contexts;
    using GraphQL.AspNet.PlanGeneration.Document.Parts;
    using GraphQL.AspNet.RulesEngine.RuleSets.DocumentConstruction.Common;

    /// <summary>
    /// Generates the <see cref="IOperationDocumentPart"/> to representing the operation node.
    /// </summary>
    internal class OperationNode_CreateOperationOnContext
        : DocumentConstructionStep<OperationNode>
    {
        /// <summary>
        /// Validates the specified node to ensure it is "correct" in the context of the rule doing the valdiation.
        /// </summary>
        /// <param name="context">The validation context encapsulating a <see cref="SyntaxNode" /> that needs to be validated.</param>
        /// <returns><c>true</c> if the node is valid, <c>false</c> otherwise.</returns>
        public override bool Execute(DocumentConstructionContext context)
        {
            var node = (OperationNode)context.ActiveNode;

            var operationType = Constants.ReservedNames.FindOperationTypeByKeyword(node.OperationType.ToString());

            // grab a reference to the operation graph type if its
            // supported by the schema
            IGraphOperation operation = null;
            if (context.Schema.Operations.ContainsKey(operationType))
                operation = context.Schema.Operations[operationType];

            var operationPart = new DocumentOperation(context.ParentPart, node, operationType);
            operationPart.AssignGraphType(operation);

            context.AssignPart(operationPart);
            return true;
        }
    }
}