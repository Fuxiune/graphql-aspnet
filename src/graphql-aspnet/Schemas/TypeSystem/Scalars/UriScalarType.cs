﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.Schemas.TypeSystem.Scalars
{
    using System;
    using System.Diagnostics;
    using GraphQL.AspNet.Common;
    using GraphQL.AspNet.Execution.Exceptions;
    using GraphQL.AspNet.Parsing.SyntaxNodes;

    /// <summary>
    /// A graph type representing a <see cref="Uri"/>.
    /// </summary>
    [DebuggerDisplay("SCALAR: {Name}")]
    public sealed class UriScalarType : BaseScalarType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UriScalarType"/> class.
        /// </summary>
        public UriScalarType()
            : base(Constants.ScalarNames.URI, typeof(Uri))
        {
            this.Description = "A uri pointing to a location on the web (a.k.a. URL).";
            this.OtherKnownTypes = TypeCollection.Empty;
        }

        /// <inheritdoc />
        public override object Resolve(ReadOnlySpan<char> data)
        {
            var value = GraphQLStrings.UnescapeAndTrimDelimiters(data);

            // don't make any assumptions about the uri other than if it can be a uri
            // or not.  "-3" is a valid uri but is not a web address. At this point
            // we can't deteremine if the user intended a web address be supplied or not
            if (Uri.IsWellFormedUriString(value, UriKind.RelativeOrAbsolute) &&
                Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var result))
            {
                return result;
            }

            throw new UnresolvedValueException(data);
        }

        /// <inheritdoc />
        public override object Serialize(object item)
        {
            if (item == null)
                return item;

            return ((Uri)item).ToString();
        }

        /// <inheritdoc />
        public override TypeCollection OtherKnownTypes { get; }

        /// <inheritdoc />
        public override ScalarValueType ValueType => ScalarValueType.String;
    }
}