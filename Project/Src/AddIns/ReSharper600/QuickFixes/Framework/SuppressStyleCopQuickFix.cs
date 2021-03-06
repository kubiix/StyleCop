﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SuppressStyleCopQuickFix.cs" company="http://stylecop.codeplex.com">
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
//   QuickFix - SuppressStyleCopQuickFix. Priority set to 0 to push it down the list.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
extern alias JB;

namespace StyleCop.ReSharper600.QuickFixes.Framework
{
    #region Using Directives

    using System.Collections.Generic;

    using JetBrains.ReSharper.Feature.Services.Bulbs;

    using StyleCop.ReSharper600.BulbItems.Framework;
    using StyleCop.ReSharper600.Violations;

    #endregion

    /// <summary>
    /// QuickFix - SuppressStyleCopQuickFix. Priority set to 0 to push it down the list.
    /// </summary>
    [SuppressQuickFix]
    [QuickFix]
    public class SuppressStyleCopQuickFix : IQuickFix
    {
        #region Fields

        /// <summary>
        /// Instance of the StyleCop violation the QuickFix can deal with.
        /// </summary>
        protected readonly StyleCopViolationBase Violation;

        /// <summary>
        /// List of available actions to be displayed in the IDE.
        /// </summary>
        private List<IBulbItem> bulbItems = new List<IBulbItem>();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the SuppressStyleCopQuickFix class that can 
        /// handle <see cref="StyleCopViolationError"/>.
        /// </summary>
        /// <param name="highlight">
        /// <see cref="StyleCopViolationError"/>that has been detected.
        /// </param>
        public SuppressStyleCopQuickFix(StyleCopViolationError highlight)
            : this(highlight, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SuppressStyleCopQuickFix class that can handle
        /// <see cref="StyleCopViolationHint"/>.
        /// </summary>
        /// <param name="highlight">
        /// <see cref="StyleCopViolationHint"/>that has been detected.
        /// </param>
        public SuppressStyleCopQuickFix(StyleCopViolationHint highlight)
            : this(highlight, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SuppressStyleCopQuickFix class that can handle
        /// <see cref="StyleCopViolationInfo"/>.
        /// </summary>
        /// <param name="highlight">
        /// <see cref="StyleCopViolationInfo"/>that has been detected.
        /// </param>
        public SuppressStyleCopQuickFix(StyleCopViolationInfo highlight)
            : this(highlight, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SuppressStyleCopQuickFix class that can handle
        /// <see cref="StyleCopViolationSuggestion"/>.
        /// </summary>
        /// <param name="highlight">
        /// <see cref="StyleCopViolationSuggestion"/>that has been detected.
        /// </param>
        public SuppressStyleCopQuickFix(StyleCopViolationSuggestion highlight)
            : this(highlight, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SuppressStyleCopQuickFix class that can handle
        /// <see cref="StyleCopViolationWarning"/>.
        /// </summary>
        /// <param name="highlight">
        /// <see cref="StyleCopViolationWarning"/>that has been detected.
        /// </param>
        public SuppressStyleCopQuickFix(StyleCopViolationWarning highlight)
            : this(highlight, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SuppressStyleCopQuickFix class that can handle.
        /// </summary>
        /// <param name="highlight">
        /// <see cref="StyleCopViolationWarning"/>that has been detected.
        /// </param>
        /// <param name="initialise">
        /// True to initialize.
        /// </param>
        protected SuppressStyleCopQuickFix(StyleCopViolationBase highlight, bool initialise)
        {
            this.Violation = highlight;
            this.InitialiseBulbItems();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets a list of BulbItems to display in the IDE.
        /// </summary>
        public IBulbItem[] Items
        {
            get
            {
                return this.BulbItems.ToArray();
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a list of BulbItems to be Displayed.
        /// </summary>
        /// <remarks>
        /// An internal representation of the BulbItems used for
        /// initialization and filtering.
        /// </remarks>
        protected List<IBulbItem> BulbItems
        {
            get
            {
                return this.bulbItems;
            }

            set
            {
                this.bulbItems = value;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// True if this QuickFix is available.
        /// </summary>
        /// <param name="cache">
        /// The cache object to use.
        /// </param>
        /// <returns>
        /// The is available.
        /// </returns>
        public bool IsAvailable(JB::JetBrains.Util.IUserDataHolder cache)
        {
            return true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the QuickFix with all the available BulbItems that can fix the current
        /// StyleCop Violation.
        /// </summary>
        protected void InitialiseBulbItems()
        {
            this.BulbItems = new List<IBulbItem> { new SuppressMessageBulbItem { Description = "Suppress : " + this.Violation.ToolTip, Rule = this.Violation.Rule } };
        }

        #endregion
    }
}