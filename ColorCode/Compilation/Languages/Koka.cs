﻿// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections.Generic;
using ColorCode.Common;

namespace ColorCode.Compilation.Languages
{
    public class Koka : ILanguage
    {
        public string Id
        {
            get { return LanguageId.Koka; }
        }

        public string Name
        {
            get { return "Koka"; }
        }

        public string CssClassName
        {
            get { return "koka"; }
        }

        public string FirstLinePattern
        {
            get
            {
                return null;
            }
        }

        private const string plainKeywords = @"infix|infixr|infixl|type|cotype|rectype|struct|alias|interface|instance|external|fun|function|val|var|con|module|import|as|public|private|abstract|yield";
        private const string controlKeywords = @"if|then|else|elif|match|return";
        private const string typeKeywords = @"forall|exists|some|with";
        private const string pseudoKeywords = @"include|inline|cs|js|file";
        private const string opkeywords = @"[=\.\|]|\->|\:=";

        private const string intype = @"\b(" + typeKeywords + @")\b|(?:[a-z]\w*/)*[a-z]\w+|(?:[a-z]\w*/)*[A-Z]\w*|([a-z][0-9]*|_\w*)|\->|[\s\|]+";
        private const string toptype = "(?:" + intype + "|::)";
        private const string nestedtype = @"(?:([a-z]\w*)\s*[:]|" + intype + ")";

        private const string symbol = @"[$%&\*\+@!\\\^~=\.:\-\?\|<>/]";
        private const string symbols = @"(?:" + symbol + @")+";

        private const string escape = @"\\(?:[nrtv\\""']|x[\da-fA-F]{2})";

        public IList<LanguageRule> Rules
        {
            get
            {
                return new List<LanguageRule> {
                    // Nested block comments
                    new LanguageRule(
                      // Handle nested block comments using named balanced groups
                      @"/\*([^*/]+|/+[^*/]|\*+[^*/])*" +
                      @"(" +
                       @"((?<comment>/\*)([^*/]+|/+[^*/]|\*+[^*/])*)+" +
                       @"((?<-comment>\*/)([^*/]+|/+[^*/]|\*+[^*/])*)+" +
                      @")*" +
                      @"(\*+/)",
                      new Dictionary<int, string>
                          {
                              { 0, ScopeName.Comment },
                          }),
           
                   // Line comments
                   new LanguageRule(
                        @"(//.*?)\r?$",
                        new Dictionary<int, string>
                            {
                                { 1, ScopeName.Comment }
                            }),            
            
                    // operator keywords
                    new LanguageRule(
                        @"(?<!" + symbol + ")(" + opkeywords + @")(?!" + symbol + @")",
                        new Dictionary<int, string>
                            {
                                { 1, ScopeName.Keyword }
                            }),
            
                    // Types
                    new LanguageRule(
                        // Type highlighting using named balanced groups to balance parenthesized sub types
                        // 'toptype' captures two groups: type keyword and type variables 
                        // each 'nestedtype' captures three groups: parameter names, type keywords and type variables
                        @"(?:" + @"\b(type|struct|cotype|rectype)\b|"
                               + @"::?(?!" + symbol + ")|" 
                               + @"\b(alias)\s+[a-z]\w+\s*(?:<[^>]*>\s*)?(=)" + ")" 
                               + toptype + "*" +
                        @"(?:" +
                         @"(?:(?<type>[\(\[<])(?:" + nestedtype + @"|[,]" + @")*)+" +
                         @"(?:(?<-type>[\)\]>])(?:" + nestedtype + @"|(?(type)[,])" + @")*)+" +
                        @")*" +
                        @"", //(?=(?:[,\)\{\}\]<>]|(" + keywords +")\b))",
                        new Dictionary<int,string> {
                            { 0, ScopeName.Type },
                    
                            { 1, ScopeName.Keyword },   // type struct etc
                            { 2, ScopeName.Keyword },   // alias
                            { 3, ScopeName.Keyword },   //  =
                    
                            { 4, ScopeName.Keyword},
                            { 5, ScopeName.TypeVariable },
                    
                            { 6, ScopeName.StringEscape },  // use stringescape (gray) for type parameters
                            { 7, ScopeName.Keyword },
                            { 8, ScopeName.TypeVariable },
                    
                            { 9, ScopeName.StringEscape },
                            { 10, ScopeName.Keyword },
                            { 11, ScopeName.TypeVariable },                
                        }),

                    // module and imports
                    new LanguageRule(
                        @"\b(module)\s+(interface\s+)?((?:[a-z]\w*/)*[a-z]\w*)",
                        new Dictionary<int, string>
                            {
                                { 1, ScopeName.Keyword },
                                { 2, ScopeName.Keyword },
                                { 3, ScopeName.NameSpace },
                            }),

                    new LanguageRule(
                        @"\b(import)\s+((?:[a-z]\w*/)*[a-z]\w*)\s*(?:(=)\s*(qualified\s+)?((?:[a-z]\w*/)*[a-z]\w*))?",
                        new Dictionary<int, string>
                            {
                                { 1, ScopeName.Keyword },
                                { 2, ScopeName.NameSpace },
                                { 3, ScopeName.Keyword },
                                { 4, ScopeName.Keyword },
                                { 5, ScopeName.NameSpace },
                            }),
            
                    // keywords
                    new LanguageRule(
                        @"\b(" + plainKeywords + "|" + typeKeywords + @")\b",
                        new Dictionary<int, string>
                            {
                                { 1, ScopeName.Keyword }
                            }),
                    new LanguageRule(
                        @"\b(" + controlKeywords + @")\b",
                        new Dictionary<int, string>
                            {
                                { 1, ScopeName.ControlKeyword }
                            }),
                    new LanguageRule(
                        @"\b(" + pseudoKeywords + @")\b",
                        new Dictionary<int, string>
                            {
                                { 1, ScopeName.PseudoKeyword }
                            }),
            
                    // Names
                    new LanguageRule(
                        @"([a-z]\w*/)*([a-z]\w*|\(" + symbols + @"\))",
                        new Dictionary<int, string>
                            {
                                { 1, ScopeName.NameSpace }
                            }),
                    new LanguageRule(
                        @"([a-z]\w*/)*([A-Z]\w*)",
                        new Dictionary<int, string>
                            {
                                { 1, ScopeName.NameSpace },
                                { 2, ScopeName.Constructor }
                            }),

                    // Operators and punctuation
                    new LanguageRule(
                        symbols,
                        new Dictionary<int, string>
                            {
                                { 0, ScopeName.Operator }
                            }),
                    new LanguageRule(
                        @"[{}\(\)\[\];,]",
                        new Dictionary<int, string>
                            {
                                { 0, ScopeName.Delimiter }
                            }),

                    // Literals
                    new LanguageRule(
                        @"0[xX][\da-fA-F]+|\d+(\.\d+([eE][\-+]?\d+)?)?",
                        new Dictionary<int, string>
                            {
                                { 0, ScopeName.Number }
                            }),

                    new LanguageRule(
                        @"(?s)'(?:[^\t\n\\']+|(" + escape + @")|\\)*'",
                        new Dictionary<int, string>
                            {
                                { 0, ScopeName.String },
                                { 1, ScopeName.StringEscape },
                            }),
                    new LanguageRule(
                        @"(?s)@""(?:("""")|[^""]+)*""(?!"")",
                        new Dictionary<int, string>
                            {
                                { 0, ScopeName.StringCSharpVerbatim },
                                { 1, ScopeName.StringEscape }
                            }),
                    new LanguageRule(
                                @"(?s)""(?:[^\t\n\\""]+|(" + escape + @")|\\)*""",
                                new Dictionary<int, string>
                                    {
                                        { 0, ScopeName.String },
                                        { 1, ScopeName.StringEscape }
                                    }),
                            new LanguageRule(
                                @"^\s*(\#error|\#line|\#pragma|\#warning|\#!/usr/bin/env\s+koka|\#).*?$",
                                new Dictionary<int, string>
                                    {
                                        { 1, ScopeName.PreprocessorKeyword }
                                    }),
                 };
            }
        }

        public bool HasAlias(string lang)
        {
            switch (lang.ToLower()) {
                case "kk":
                case "kki":
                    return true;

                default:
                    return false;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
