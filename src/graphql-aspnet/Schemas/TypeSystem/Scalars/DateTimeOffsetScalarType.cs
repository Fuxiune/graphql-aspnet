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
    using GraphQL.AspNet.Common.Extensions;
    using GraphQL.AspNet.Execution.Exceptions;
    using GraphQL.AspNet.Interfaces.TypeSystem;
    using GraphQL.AspNet.Parsing.SyntaxNodes;

    /// <summary>
    /// A graph type represneting a .NET <see cref="DateTimeOffset"/> that does include a time component.
    /// </summary>
    [DebuggerDisplay("SCALAR: {Name}")]
    public sealed class DateTimeOffsetScalarType : BaseScalarType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeOffsetScalarType"/> class.
        /// </summary>
        public DateTimeOffsetScalarType()
            : base(Constants.ScalarNames.DATETIMEOFFSET, typeof(DateTimeOffset))
        {
            this.Description = "A point in time relative to Coordinated Universal Time (UTC).";
            this.OtherKnownTypes = new TypeCollection(typeof(DateTimeOffset?));
        }

        /// <inheritdoc />
        public override object Resolve(ReadOnlySpan<char> data)
        {
            if (DateTimeExtensions.TryParseMultiFormat(
                GraphQLStrings.UnescapeAndTrimDelimiters(data, false),
                out DateTime? dt) &&
                dt.HasValue)
            {
                if (dt.Value.Kind == DateTimeKind.Unspecified && this.DefaultKind != DateTimeKind.Unspecified)
                    dt = DateTime.SpecifyKind(dt.Value, this.DefaultKind);

                return (DateTimeOffset)dt.Value;
            }

            throw new UnresolvedValueException(data);
        }

        /// <inheritdoc />
        public override object Serialize(object item)
        {
            return item;
        }

        /// <inheritdoc />
        public override TypeCollection OtherKnownTypes { get; }

        /// <inheritdoc />
        public override ScalarValueType ValueType => ScalarValueType.StringOrNumber;

        /// <summary>
        /// Gets or sets kind of date to interprete an input result as if/when a specific kind
        /// is not determinable from the source text.
        /// </summary>
        /// <value>The default kind.</value>
        public DateTimeKind DefaultKind { get; set; } = DateTimeKind.Utc;
    }
}