﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SA1636FileHeaderCopyrightTextMustMatchBulbItem.cs" company="http://stylecop.codeplex.com">
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
//   BulbItem - SA1636FileHeaderCopyrightTextMustMatchBulbItem.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace StyleCop.ReSharper513.BulbItems.Documentation
{
    #region Using Directives

    using JetBrains.ProjectModel;
    using JetBrains.ReSharper.Psi.CSharp.Tree;
    using JetBrains.TextControl;

    using StyleCop.ReSharper513.BulbItems.Framework;
    using StyleCop.ReSharper513.CodeCleanup.Rules;
    using StyleCop.ReSharper513.Core;

    #endregion

    /// <summary>
    /// BulbItem - SA1636FileHeaderCopyrightTextMustMatchBulbItem.
    /// </summary>
    internal class SA1636FileHeaderCopyrightTextMustMatchBulbItem : V5BulbItemImpl
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
            ICSharpFile file = Utils.GetCSharpFile(solution, textControl);

            new DocumentationRules().InsertCopyrightText(file);
        }

        #endregion
    }
}