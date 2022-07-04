﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.PlanGeneration.Document.Parts.SuppliedValues
{
    using System;
    using GraphQL.AspNet.Common.Extensions;
    using GraphQL.AspNet.Interfaces.PlanGeneration.DocumentParts;
    using GraphQL.AspNet.Interfaces.PlanGeneration.DocumentParts.Common;
    using GraphQL.AspNet.Parsing.SyntaxNodes.Inputs.Values;

    /// <summary>
    /// A factory to generate appropriate query input values for a parsed document.
    /// </summary>
    internal static class DocumentSuppliedValueFactory
    {
        /// <summary>
        /// Converts a node read on a query document into a value representation that can be resolved
        /// to a usable .NET type in a query plan.
        /// </summary>
        /// <param name="ownerPart">The document part which will own the created value.</param>
        /// <param name="valueNode">The AST node from which the value should be created.</param>
        /// <param name="key">A key value, if any, to assign to the value node.</param>
        /// <returns>IQueryInputValue.</returns>
        public static ISuppliedValueDocumentPart CreateInputValue(IDocumentPart ownerPart, InputValueNode valueNode, string key = null)
        {
            if (valueNode == null)
                return null;

            switch (valueNode)
            {
                case ListValueNode lvn:
                    return new DocumentListSuppliedValue(ownerPart, lvn, key);

                case NullValueNode nvn:
                    return new DocumentNullSuppliedValue(ownerPart, nvn, key);

                case ComplexValueNode cvn:
                    return new DocumentComplexSuppliedValue(ownerPart, cvn, key);

                case ScalarValueNode svn:
                    return new DocumentScalarSuppliedValue(ownerPart, svn, key);

                case EnumValueNode evn:
                    return new DocumentEnumSuppliedValue(ownerPart, evn, key);

                case VariableValueNode vvn:
                    return new DocumentVariableUsageValue(ownerPart, vvn, key);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(valueNode),
                        $"Unknown type '{valueNode?.GetType().FriendlyName()}'. " +
                        $"Factory is unable to generate a '{nameof(ISuppliedValueDocumentPart)}' to fulfill the request.");
            }
        }
    }
}