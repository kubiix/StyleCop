﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SA1611ElementParametersMustBeDocumentedBulbItem.cs" company="http://stylecop.codeplex.com">
//   MS-PL
// </copyright>
// <license>
//   This source code is subject to terms and conditions of the Microsoft 
//   Public License. A copy of the license can be found in the License.html 
//   file at the root of this distribution. If you cannot locate the  
//   Microsoft Public License, please send an email to dlr@microsoft.com. 
//   By using this source code in any fashion, you are agreeing to be bound 
//   by the terms of the Microsoft Public License. You must not remove this 
//   notice, or any other, from this software.
// </license>
// <summary>
//   BulbItem - SA1611ElementParametersMustBeDocumentedBulbItem : Inserts a param into the header.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace StyleCop.ReSharper600.BulbItems.Documentation
{
    #region Using Directives

    using JetBrains.ProjectModel;
    using JetBrains.ReSharper.Psi.CSharp.Tree;
    using JetBrains.ReSharper.Psi.Tree;
    using JetBrains.TextControl;

    using StyleCop.ReSharper600.BulbItems.Framework;
    using StyleCop.ReSharper600.CodeCleanup.Rules;
    using StyleCop.ReSharper600.Core;

    #endregion

    /// <summary>
    /// BulbItem - SA1611ElementParametersMustBeDocumentedBulbItem : Inserts a <c>param</c> into the header.
    /// </summary>
    internal class SA1611ElementParametersMustBeDocumentedBulbItem : V5BulbItemImpl
    {
        #region Public Methods and Operators

        /// <summary>
        /// The execute transaction inner.
        /// </summary>
        /// <param name="solution">
        /// The solution.
        /// </param>
        /// <param name="textControl">
        /// The text control.
        /// </param>
        public override void ExecuteTransactionInner(ISolution solution, ITextControl textControl)
        {
            ITreeNode element = Utils.GetElementAtCaret(solution, textControl);

            IDeclaration declaration = element.GetContainingNode<IMethodDeclaration>(true) ?? (IDeclaration)element.GetContainingNode<IConstructorDeclaration>(true);

            if (declaration != null)
            {
                new DocumentationRules().InsertMissingParamElement(declaration);
            }
        }

        #endregion
    }
}