﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.Interfaces.Configuration
{
    using GraphQL.AspNet.Configuration;
    using GraphQL.AspNet.Execution.Contexts;
    using GraphQL.AspNet.Interfaces.Middleware;
    using GraphQL.AspNet.Interfaces.TypeSystem;

    /// <summary>
    /// A builder for performing advanced configuration of a schema's pipeline and processing settings.
    /// </summary>
    /// <typeparam name="TSchema">The type of the schema this builder is created for.</typeparam>
    public interface ISchemaBuilder<TSchema>
        where TSchema : class, ISchema
    {
        /// <summary>
        /// Gets the completed options used to configure this schema.
        /// </summary>
        /// <value>The options.</value>
        SchemaOptions Options { get; }

        /// <summary>
        /// Gets a builder to construct the field execution pipeline. This pipeline is invoked per field resolution request to generate a
        /// piece of data in the process of fulfilling the primary query.
        /// </summary>
        /// <value>The field execution pipeline.</value>
        ISchemaPipelineBuilder<TSchema, IGraphFieldExecutionMiddleware, GraphFieldExecutionContext> FieldExecutionPipeline { get; }

        /// <summary>
        /// Gets a builder to construct the field authorization pipeline. This pipeline is invoked per field resolution request to authorize
        /// the user to the field allowing or denying them access to it.
        /// </summary>
        /// <value>The field authorization pipeline.</value>
        ISchemaPipelineBuilder<TSchema, IGraphFieldSecurityMiddleware, GraphFieldSecurityContext> FieldAuthorizationPipeline { get; }

        /// <summary>
        /// Gets a builder to construct the primary query pipeline. This pipeline oversees the processing of a query and is invoked
        /// directly by the http handler.
        /// </summary>
        /// <value>The query execution pipeline.</value>
        ISchemaPipelineBuilder<TSchema, IQueryExecutionMiddleware, GraphQueryExecutionContext> QueryExecutionPipeline { get; }

        /// <summary>
        /// Gets the build useto construct the primary directive execution pipeline. This pipeline oversees the processing of directives against
        /// data items for both the type system initial construction as well as during query execution.
        /// </summary>
        /// <value>The directive execution pipeline.</value>
        ISchemaPipelineBuilder<TSchema, IDirectiveExecutionMiddleware, GraphDirectiveExecutionContext> DirectiveExecutionPipeline { get; }
    }
}