﻿// *************************************************************
// project:  graphql-aspnet
// --
// repo: https://github.com/graphql-aspnet
// docs: https://graphql-aspnet.github.io
// --
// License:  MIT
// *************************************************************

namespace GraphQL.AspNet.Interfaces.PlanGeneration.DocumentPartsNew
{
    using System;
    using GraphQL.AspNet.Interfaces.PlanGeneration.DocumentParts;

    public interface IFragmentSpreadDocumentPart : IDocumentPart
    {
        /// <summary>
        /// Assigns the named fragment document part this spread is referencing.
        /// </summary>
        /// <param name="targetFragment">The target fragment.</param>
        void AssignNamedFragment(INamedFragmentDocumentPart targetFragment);

        /// <summary>
        /// Gets the name of the fragment this instance is spreading.
        /// </summary>
        /// <value>The name of the fragment.</value>
        ReadOnlyMemory<char> FragmentName { get; }

        /// <summary>
        /// Gets a reference to the named fragment in the document this instance is targeting,
        /// if any.
        /// </summary>
        /// <value>The named fragment in the document.</value>
        INamedFragmentDocumentPart Fragment { get; }
    }
}