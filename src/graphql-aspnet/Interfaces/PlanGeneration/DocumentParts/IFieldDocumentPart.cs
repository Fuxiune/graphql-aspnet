﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.Interfaces.PlanGeneration.DocumentParts
{
    using System;
    using System.Collections.Generic;
    using GraphQL.AspNet.Interfaces.TypeSystem;

    /// <summary>
    /// A single field of data from a source object selected to be returned as part of a graph query.
    /// </summary>
    public interface IFieldDocumentPart : IDocumentPart
    {
        /// <summary>
        /// Determines whether this field is capable of resolving itself for the given graph type.
        /// </summary>
        /// <param name="graphType">the graph type to test.</param>
        /// <returns><c>true</c> if this field can be returned the specified graph type; otherwise, <c>false</c>.</returns>
        internal bool CanResolveForGraphType(IGraphType graphType);

        /// <summary>
        /// Gathers the arguments currently defined on this instance and packages
        /// them into a queryable collection.
        /// </summary>
        /// <returns>IInputArgumentCollectionDocumentPart.</returns>
        IInputArgumentCollectionDocumentPart GatherArguments();

        /// <summary>
        /// Gathers the directives currently defined on this instance. Directives
        /// are returned in the order they were encountered.
        /// </summary>
        /// <returns>IEnumerable&lt;IDirectiveDocumentPart&gt;.</returns>
        IEnumerable<IDirectiveDocumentPart> GatherDirectives();

        /// <summary>
        /// Gets the name of the field requested, as it exists in the schema.
        /// </summary>
        /// <value>The parsed name from the queryt document.</value>
        ReadOnlyMemory<char> Name { get; }

        /// <summary>
        /// Gets the alias assigned to the field requested as it was defined in the user's query document.
        /// Defaults to <see cref="Name"/> if not supplied.
        /// </summary>
        /// <value>The parsed alias from the query document.</value>
        ReadOnlyMemory<char> Alias { get; }

        /// <summary>
        /// Gets the field reference pointed to by this instance as its declared in the schema.
        /// </summary>
        /// <value>The field.</value>
        IGraphField Field { get; }

        /// <summary>
        /// Gets the field selection set, if any, contained in this field.
        /// </summary>
        /// <value>The child selection set.</value>
        IFieldSelectionSetDocumentPart FieldSelectionSet { get; }
    }
}