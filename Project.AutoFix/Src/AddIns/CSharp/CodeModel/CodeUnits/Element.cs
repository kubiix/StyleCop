//-----------------------------------------------------------------------
// <copyright file="Element.cs">
//   MS-PL
// </copyright>
// <license>
//   This source code is subject to terms and conditions of the Microsoft 
//   Public License. A copy of the license can be found in the License.html 
//   file at the root of this distribution. 
//   By using this source code in any fashion, you are agreeing to be bound 
//   by the terms of the Microsoft Public License. You must not remove this 
//   notice, or any other, from this software.
// </license>
//-----------------------------------------------------------------------
namespace StyleCop.CSharp.CodeModel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Describes a single element in a C# code file.
    /// </summary>
    /// <subcategory>element</subcategory>
    [SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase", Justification = "Camel case better serves in this case.")]
    public abstract class Element : CodeUnit
    {
        #region Internal Static Fields

        /// <summary>
        /// An empty array of elements.
        /// </summary>
        internal static readonly Element[] EmptyElementArray = new Element[] { };

        #endregion Internal Static Fields

        #region Private Fields

        /// <summary>
        /// The list of attributes attached to the element.
        /// </summary>
        private CodeUnitProperty<ICollection<Attribute>> attributes;

        /// <summary>
        /// The name of the element.
        /// </summary>
        private CodeUnitProperty<string> name;

        /// <summary>
        /// The element's access modifier type.
        /// </summary>
        private CodeUnitProperty<AccessModifierType> accessModifier;

        /// <summary>
        /// The actual access level of the element.
        /// </summary>
        private CodeUnitProperty<AccessModifierType> actualAccessLevel;

        /// <summary>
        /// The list of modifiers in the declaration.
        /// </summary>
        private CodeUnitProperty<Dictionary<TokenType, Token>> modifiers;

        /// <summary>
        /// Indicates whether this element is unsafe.
        /// </summary>
        private CodeUnitProperty<bool> unsafeCode;

        /// <summary>
        /// The fully qualified name of the element.
        /// </summary>
        private CodeUnitProperty<string> fullyQualifiedName;

        /// <summary>
        /// The first token in the element's declaration.
        /// </summary>
        private CodeUnitProperty<Token> firstDeclarationToken;

        /// <summary>
        /// The last token in the element's declaration.
        /// </summary>
        private CodeUnitProperty<Token> lastDeclarationToken;

        /// <summary>
        /// The element's header.
        /// </summary>
        private CodeUnitProperty<ElementHeader> header;

        /// <summary>
        /// The line number on which the element begins.
        /// </summary>
        private CodeUnitProperty<int> lineNumber;

        /// <summary>
        /// A tag object.
        /// </summary>
        private object tag;

        #endregion Private Fields

        #region Internal Constructors

        /// <summary>
        /// Initializes a new instance of the Element class.
        /// </summary>
        /// <param name="proxy">Proxy object for the element.</param>
        /// <param name="type">The element type.</param>
        /// <param name="name">The name of this element.</param>
        /// <param name="attributes">The list of attributes attached to this element.</param>
        /// <param name="unsafeCode">Indicates whether the element is unsafe.</param>
        internal Element(
            CodeUnitProxy proxy,
            ElementType type,
            string name,
            ICollection<Attribute> attributes,
            bool unsafeCode)
            : this(proxy, (int)type, name, attributes, unsafeCode)
        {
            Param.Ignore(proxy, type, name, attributes, unsafeCode);
        }

        /// <summary>
        /// Initializes a new instance of the Element class.
        /// </summary>
        /// <param name="proxy">Proxy object for the element.</param>
        /// <param name="type">The element type.</param>
        /// <param name="name">The name of this element.</param>
        /// <param name="attributes">The list of attributes attached to this element.</param>
        /// <param name="unsafeCode">Indicates whether the element is unsafe.</param>
        internal Element(
            CodeUnitProxy proxy,
            int type,
            string name,
            ICollection<Attribute> attributes,
            bool unsafeCode) 
            : base(proxy, (int)type)
        {
            Param.Ignore(proxy);
            Param.Ignore(type);
            Param.AssertNotNull(name, "name");
            Param.Ignore(attributes);
            Param.Ignore(unsafeCode);

            CsLanguageService.Debug.Assert(System.Enum.IsDefined(typeof(ElementType), this.ElementType), "The type is invalid.");

            this.name.Value = name;
            this.attributes.Value = attributes ?? Attribute.EmptyAttributeArray;
            CsLanguageService.Debug.Assert(attributes == null || attributes.IsReadOnly, "The attributes collection should be read-only");

            this.unsafeCode.Value = unsafeCode;
            if (!unsafeCode && this.ContainsModifier(TokenType.Unsafe))
            {
                this.unsafeCode.Value = true;
            }
        }

        #endregion Internal Constructors

        #region Public Override Properties

        /// <summary>
        /// Gets the line number that this code unit appears on in the document.
        /// </summary>
        public override int LineNumber
        {
            get
            {
                this.ValidateEditVersion();

                if (!this.lineNumber.Initialized)
                {
                    // The line number of the element is the first line on which a Token appears, which
                    // skips past the documentation header.
                    Token firstToken = this.FirstDeclarationToken;
                    if (firstToken != null)
                    {
                        this.lineNumber.Value = firstToken.LineNumber;
                    }
                    else
                    {
                        this.lineNumber.Value = base.LineNumber;
                    }
                }

                return this.lineNumber.Value;
            }
        }

        #endregion Public Override Properties

        #region Public Virtual Properties

        /// <summary>
        /// Gets the fully qualified name of the element.
        /// </summary>
        public virtual string FullyQualifiedName
        {
            get
            {
                this.ValidateEditVersion();

                if (!this.fullyQualifiedName.Initialized)
                {
                    string parentFullyQualifiedName = null;

                    Element parentElement = this.ParentCastedToElement;
                    if (parentElement != null && parentElement.ElementType != ElementType.Document)
                    {
                        parentFullyQualifiedName = parentElement.FullyQualifiedName;
                    }

                    string fullyQualifiedNameWithWhitespace = null;
                    if (string.IsNullOrEmpty(parentFullyQualifiedName))
                    {
                        fullyQualifiedNameWithWhitespace = this.Name;
                    }
                    else
                    {
                        var fullyQualifiedNameBuilder = new StringBuilder();
                        fullyQualifiedNameBuilder.Append(parentFullyQualifiedName);
                        fullyQualifiedNameBuilder.Append(".");

                        if (!string.IsNullOrEmpty(this.Name))
                        {
                            fullyQualifiedNameBuilder.Append(this.Name);
                        }

                        fullyQualifiedNameWithWhitespace = fullyQualifiedNameBuilder.ToString();
                    }

                    this.fullyQualifiedName.Value = CodeParser.RemoveWhitespace(fullyQualifiedNameWithWhitespace);
                }

                return this.fullyQualifiedName.Value;
            }
        }

        /// <summary>
        /// Gets the element's access level, without taking into account the access level of the element's parent.
        /// </summary>
        public virtual AccessModifierType AccessModifierType
        {
            get
            {
                this.ValidateEditVersion();

                if (!this.accessModifier.Initialized)
                {
                    this.GatherDeclarationModifiers(this.AllowedModifiers);
                }

                return this.accessModifier.Value;
            }
        }

        /// <summary>
        /// Gets the actual access level of this element, taking into account the
        /// access level of the element's parent.
        /// </summary>
        /// <returns>Returns the actual access level.</returns>
        public virtual AccessModifierType ActualAccessLevel
        {
            get
            {
                this.ValidateEditVersion();

                if (!this.actualAccessLevel.Initialized)
                {
                    this.actualAccessLevel.Value = this.ComputeActualAccess();
                }

                return this.actualAccessLevel.Value;
            }
        }

        #endregion Public Virtual Properties

        #region Public Properties

        /// <summary>
        /// Gets the friendly name of the element type, which can be used in user output.
        /// </summary>
        public string FriendlyTypeText
        {
            get
            {
                return this.FriendlyTypeTextBase;
            }
        }

        /// <summary>
        /// Gets the pluralized friendly name of the element type, which can be used in user output.
        /// </summary>
        public string FriendlyPluralTypeText
        {
            get
            {
                return this.FriendlyPluralTypeTextBase;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the element declares an access modifier within its declaration.
        /// </summary>
        public bool DeclaresAccessModifier
        {
            get
            {
                return this.ContainsModifier(TokenType.Public, TokenType.Internal, TokenType.Protected, TokenType.Private);
            }
        }

        /// <summary>
        /// Gets the list of attributes attached to the element.
        /// </summary>
        public ICollection<Attribute> Attributes
        {
            get
            {
                this.ValidateEditVersion();

                if (!this.attributes.Initialized)
                {
                    List<Attribute> temp = new List<Attribute>();

                    for (CodeUnit item = this.FindFirstChild(); item != null; item = item.FindNextSibling())
                    {
                        if (item.Is(CodeUnitType.Attribute))
                        {
                            temp.Add((Attribute)item);
                        }
                        else if (!item.Is(CodeUnitType.LexicalElement) || item.Is(LexicalElementType.Token))
                        {
                            break;
                        }
                    }

                    this.attributes.Value = temp.AsReadOnly();
                }

                return this.attributes.Value;
            }
        }

        /// <summary>
        /// Gets the type of the element.
        /// </summary>
        public ElementType ElementType
        {
            get
            {
                return (ElementType)(this.FundamentalType & (int)FundamentalTypeMasks.Element);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the element resides within a block of unsafe code,
        /// or whether the element declares itself as unsafe.
        /// </summary>
        public bool Unsafe
        {
            get
            {
                this.ValidateEditVersion();

                if (!this.unsafeCode.Initialized)
                {
                    this.unsafeCode.Value = false;

                    if (this.ContainsModifier(TokenType.Unsafe))
                    {
                        this.unsafeCode.Value = true;
                    }
                    else
                    {
                        Element parent = this.ParentCastedToElement;
                        if (parent != null)
                        {
                            bool parentIsUnsafe = parent.Unsafe;
                            if (parentIsUnsafe)
                            {
                                this.unsafeCode.Value = true;
                            }
                        }
                    }
                }

                return this.unsafeCode.Value;
            }
        }

        /// <summary>
        /// Gets the name of the element.
        /// </summary>
        public string Name
        {
            get
            {
                this.ValidateEditVersion();

                if (!this.name.Initialized)
                {
                    this.name.Value = this.GetElementName();
                    CsLanguageService.Debug.Assert(this.name.Value != null, "GetElementName must never return null.");
                }

                return this.name.Value;
            }
        }

        /// <summary>
        /// Gets the first token in the element's declaration.
        /// </summary>
        public Token FirstDeclarationToken
        {
            get
            {
                this.ValidateEditVersion();

                if (!this.firstDeclarationToken.Initialized)
                {
                    this.firstDeclarationToken.Value = this.FindFirstDeclarationToken();
                }

                return this.firstDeclarationToken.Value;
            }
        }

        /// <summary>
        /// Gets the last token in the element's declaration.
        /// </summary>
        public Token LastDeclarationToken
        {
            get
            {
                this.ValidateEditVersion();

                if (!this.lastDeclarationToken.Initialized)
                {
                    this.lastDeclarationToken.Value = this.FindLastDeclarationToken();
                }

                return this.lastDeclarationToken.Value;
            }
        }

        /// <summary>
        /// Gets the contents of the Xml header, if any.
        /// </summary>
        public ElementHeader Header
        {
            get
            {
                this.ValidateEditVersion();

                if (!this.header.Initialized)
                {
                    this.header.Value = this.FindFirstChild<ElementHeader>();
                }

                return this.header.Value;
            }
        }

        /// <summary>
        /// Gets or sets an optional tag.
        /// </summary>
        public object Tag
        {
            get
            {
                return this.tag;
            }

            set
            {
                Param.Ignore(value);
                this.tag = value;
            }
        }

        #endregion Public Properties

        #region Protected Virtual Properties

        /// <summary>
        /// Gets the collection of modifiers allowed on this element.
        /// </summary>
        protected virtual IEnumerable<string> AllowedModifiers
        {
            get
            {
                yield break;
            }
        }

        /// <summary>
        /// Gets the default access modifier for this element.
        /// </summary>
        protected virtual AccessModifierType DefaultAccessModifierType
        {
            get
            {
                return AccessModifierType.Private;
            }
        }

        #endregion Protected Virtual Properties

        #region Private Properties

        /// <summary>
        /// Gets the parent of the element hard-casted to an element.
        /// </summary>
        /// <remarks>The parent of an element must always be an element.</remarks>
        private Element ParentCastedToElement
        {
            get
            {
                CodeUnit parent = this.Parent;
                if (parent == null)
                {
                    return null;
                }

                CsLanguageService.Debug.Assert(parent.Is(CodeUnitType.Element), "The parent of an Element must always be an Element.");
                return (Element)parent;
            }
        }

        #endregion Private Properties

        #region Public Methods

        /// <summary>
        /// Indicates whether the element declaration contains one of the given modifiers.
        /// </summary>
        /// <param name="types">The modifier types to check for.</param>
        /// <returns>Returns true if the declaration contains at least one of the given modifiers.</returns>
        public bool ContainsModifier(params TokenType[] types)
        {
            Param.RequireNotNull(types, "types");

            this.ValidateEditVersion();

            if (!this.modifiers.Initialized)
            {
                this.GatherDeclarationModifiers(this.AllowedModifiers);
                CsLanguageService.Debug.Assert(this.modifiers.Value != null, "Modifiers should be non-null now.");
            }

            for (int i = 0; i < types.Length; ++i)
            {
                if (this.modifiers.Value.ContainsKey(types[i]))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion Public Methods

        #region Protected Override Methods

        /// <summary>
        /// Resets the contents of the item.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();

            this.attributes.Reset();
            this.name.Reset();
            this.accessModifier.Reset();
            this.modifiers.Reset();
            this.unsafeCode.Reset();
            this.fullyQualifiedName.Reset();
            this.firstDeclarationToken.Reset();
            this.lastDeclarationToken.Reset();
            this.header.Reset();
            this.lineNumber.Reset();
        }

        #endregion Protected Override Methods

        #region Protected Abstract Methods

        /// <summary>
        /// Gets the name of the element.
        /// </summary>
        /// <returns>The name of the element.</returns>
        protected abstract string GetElementName();

        #endregion Protected Abstract Methods

        #region Private Static Methods

        /// <summary>
        /// Gets another type of modifier for an element declaration.
        /// </summary>
        /// <param name="allowedModifiers">The types of allowed modifiers for the element.</param>
        /// <param name="elementModifiers">The collection of modifiers on the element.</param>
        /// <param name="token">The modifier token.</param>
        /// <returns>true to continue collecting modifiers; false to quit.</returns>
        private static bool GetOtherElementModifier(IEnumerable<string> allowedModifiers, Dictionary<TokenType, Token> elementModifiers, Token token)
        {
            Param.Ignore(allowedModifiers);
            Param.AssertNotNull(elementModifiers, "elementModifiers");
            Param.AssertNotNull(token, "token");

            bool stop = true;

            // If the modifier is one of the allowed modifiers, store it. Otherwise, we are done.
            if (allowedModifiers != null)
            {
                foreach (string allowedModifier in allowedModifiers)
                {
                    if (string.Equals(token.Text, allowedModifier, StringComparison.Ordinal))
                    {
                        elementModifiers.Add(token.TokenType, token);
                        stop = false;
                        break;
                    }
                }
            }

            return !stop;
        }

        #endregion Private Static Methods

        #region Private Methods

        /// <summary>
        /// Merges the access of this element with the access of its parent to determine
        /// the actual visibility of this item outside of the class.
        /// </summary>
        /// <returns>Returns the actual access level.</returns>
        private AccessModifierType ComputeActualAccess()
        {
            AccessModifierType localAccess = this.AccessModifierType;

            if (localAccess == AccessModifierType.Private)
            {
                return localAccess;
            }

            Element parentElement = this.ParentCastedToElement;
            if (parentElement == null)
            {
                return localAccess;
            }

            AccessModifierType parentActualAccess = parentElement.ActualAccessLevel;
            AccessModifierType actualAccess = localAccess;

            if (parentActualAccess == AccessModifierType.Public)
            {
                return actualAccess;
            }
            else if (parentActualAccess == AccessModifierType.ProtectedInternal)
            {
                if (actualAccess == AccessModifierType.Public)
                {
                    return AccessModifierType.ProtectedInternal;
                }
                else
                {
                    return actualAccess;
                }
            }
            else if (parentActualAccess == AccessModifierType.Protected)
            {
                if (actualAccess == AccessModifierType.Public ||
                    actualAccess == AccessModifierType.ProtectedInternal)
                {
                    return AccessModifierType.Protected;
                }
                else if (actualAccess == AccessModifierType.Internal)
                {
                    return AccessModifierType.ProtectedAndInternal;
                }
                else 
                {
                    return actualAccess;
                }
            }
            else if (parentActualAccess == AccessModifierType.Internal)
            {
                if (actualAccess == AccessModifierType.Public ||
                    actualAccess == AccessModifierType.ProtectedInternal)
                {
                    return AccessModifierType.Internal;
                }
                else if (actualAccess == AccessModifierType.Protected)
                {
                    return AccessModifierType.ProtectedAndInternal;
                }
                else
                {
                    return actualAccess;
                }
            }
            else if (parentActualAccess == AccessModifierType.ProtectedAndInternal)
            {
                if (actualAccess == AccessModifierType.Public ||
                    actualAccess == AccessModifierType.ProtectedInternal ||
                    actualAccess == AccessModifierType.Protected ||
                    actualAccess == AccessModifierType.Internal)
                {
                    return AccessModifierType.ProtectedAndInternal;
                }
                else
                {
                    return actualAccess;
                }
            }
            else
            {
                return AccessModifierType.Private;
            }
        }

        /// <summary>
        /// Walks the element declaration and gathers the element modifiers.
        /// </summary>
        /// <param name="allowedModifiers">The modifiers which are allowed for the current element type.</param>
        private void GatherDeclarationModifiers(IEnumerable<string> allowedModifiers)
        {
            Param.Ignore(allowedModifiers);

            this.modifiers.Value = new Dictionary<TokenType, Token>();
            Token accessModifierSeen = null;

            this.accessModifier.Value = this.DefaultAccessModifierType;

            for (Token token = this.FirstDeclarationToken; token != null; token = token.FindNextSiblingToken())
            {
                if (token.TokenType == TokenType.Public)
                {
                    // A public access modifier can only be specified if there have been no other access modifiers.
                    if (accessModifierSeen != null)
                    {
                        throw new SyntaxException(this.Document, token.LineNumber);
                    }

                    this.accessModifier.Value = AccessModifierType.Public;
                    accessModifierSeen = token;
                    this.modifiers.Value.Add(TokenType.Public, token);
                }
                else if (token.TokenType == TokenType.Private)
                {
                    // A private access modifier can only be specified if there have been no other access modifiers.
                    if (accessModifierSeen != null)
                    {
                        throw new SyntaxException(this.Document, token.LineNumber);
                    }

                    this.accessModifier.Value = AccessModifierType.Private;
                    accessModifierSeen = token;
                    this.modifiers.Value.Add(TokenType.Private, token);
                }
                else if (token.TokenType == TokenType.Internal)
                {
                    // The access is internal unless we have already seen a protected access
                    // modifier, in which case it is protected internal.
                    if (accessModifierSeen == null)
                    {
                        this.accessModifier.Value = AccessModifierType.Internal;
                    }
                    else if (accessModifierSeen.TokenType == TokenType.Protected)
                    {
                        this.accessModifier.Value = AccessModifierType.ProtectedInternal;
                    }
                    else
                    {
                        throw new SyntaxException(this.Document, token.LineNumber);
                    }

                    accessModifierSeen = token;
                    this.modifiers.Value.Add(TokenType.Internal, token);
                }
                else if (token.TokenType == TokenType.Protected)
                {
                    // The access is protected unless we have already seen an internal access
                    // modifier, in which case it is protected internal.
                    if (accessModifierSeen == null)
                    {
                        this.accessModifier.Value = AccessModifierType.Protected;
                    }
                    else if (accessModifierSeen.TokenType == TokenType.Internal)
                    {
                        this.accessModifier.Value = AccessModifierType.ProtectedInternal;
                    }
                    else
                    {
                        throw new SyntaxException(this.Document, token.LineNumber);
                    }

                    accessModifierSeen = token;
                    this.modifiers.Value.Add(TokenType.Protected, token);
                }
                else
                {
                    if (!GetOtherElementModifier(allowedModifiers, this.modifiers.Value, token))
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Finds the first token in the element's declaration.
        /// </summary>
        /// <returns>Returns the first token or null.</returns>
        private Token FindFirstDeclarationToken()
        {
            for (CodeUnit item = this.FindFirstDescendent(); item != null; item = item.FindNextDescendentOf(this))
            {
                if (item.Is(CodeUnitType.Attribute))
                {
                    // Move to the end of the attribute.
                    item = item.FindLastDescendent();
                }
                else if (item.Is(LexicalElementType.Token))
                {
                    return (Token)item;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the last token in the element's declaration.
        /// </summary>
        /// <returns>Returns the last token or null.</returns>
        private Token FindLastDeclarationToken()
        {
            CodeUnit previousToken = this.FirstDeclarationToken;

            for (CodeUnit item = previousToken; item != null; item = item.FindNextDescendentOf(this))
            {
                if (item.Is(CodeUnitType.Attribute))
                {
                    // Move to the end of the attribute.
                    item = item.FindLastDescendent();
                }
                else
                {
                    // These types indicate that we've gone past the end of the declaration.
                    if (item.Is(TokenType.OpenCurlyBracket) ||
                        item.Is(TokenType.Semicolon) ||
                        item.Is(OperatorType.Equals))
                    {
                        break;
                    }

                    // If we find an opening parenthesis, square bracket, etc., jump to its corresponding closing bracket.
                    if (item.Is(TokenType.OpenParenthesis) || 
                        item.Is(TokenType.OpenGenericBracket) ||
                        item.Is(TokenType.OpenSquareBracket))
                    {
                        item = ((OpenBracketToken)item).MatchingBracket;
                    }

                    if (item.Is(LexicalElementType.Token))
                    {
                        previousToken = item;
                    }
                }
            }

            if (previousToken != null)
            {
                while (previousToken.Parent != null && previousToken.Parent.Is(LexicalElementType.Token))
                {
                    previousToken = previousToken.Parent;
                }
            }

            return (Token)previousToken;
        }

        #endregion Private Methods
    }
}
