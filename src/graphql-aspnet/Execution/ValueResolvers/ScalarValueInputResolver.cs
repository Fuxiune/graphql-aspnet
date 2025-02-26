﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.Execution.ValueResolvers
{
    using GraphQL.AspNet.Common;
    using GraphQL.AspNet.Interfaces.Execution;
    using GraphQL.AspNet.Interfaces.PlanGeneration.Resolvables;
    using GraphQL.AspNet.Interfaces.Variables;

    /// <summary>
    /// A resolver that operates in context of a field input value that can generate a qualified .NET object for the
    /// provided scalar data.
    /// </summary>
    public class ScalarValueInputResolver : IInputValueResolver
    {
        private readonly ILeafValueResolver _scalarResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarValueInputResolver"/> class.
        /// </summary>
        /// <param name="scalarResolver">The scalar resolver.</param>
        public ScalarValueInputResolver(ILeafValueResolver scalarResolver)
        {
            _scalarResolver = Validation.ThrowIfNullOrReturn(scalarResolver, nameof(scalarResolver));
        }

        /// <inheritdoc />
        public object Resolve(IResolvableItem resolvableItem, IResolvedVariableCollection variableData = null)
        {
            if (resolvableItem is IResolvablePointer pointer)
            {
                IResolvedVariable variable = null;
                var variableFound = variableData?.TryGetValue(pointer.PointsTo, out variable) ?? false;
                if (variableFound)
                    return variable.Value;

                resolvableItem = pointer.DefaultItem;
            }

            if (resolvableItem is IResolvableValue resolvableValue)
                return _scalarResolver.Resolve(resolvableValue.ResolvableValue);

            return null;
        }
    }
}