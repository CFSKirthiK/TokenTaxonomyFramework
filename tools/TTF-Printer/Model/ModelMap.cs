﻿using System.Collections.Specialized;
using TTI.TTF.Taxonomy.Model.Artifact;
using TTI.TTF.Taxonomy.Model.Core;
using TTI.TTF.Taxonomy.TypePrinters;

namespace TTI.TTF.Taxonomy.Model
{
    public static class ModelMap
    {
        public static readonly string BaseFolder = "base";
        public static readonly string BehaviorFolder = "behaviors";
        public static readonly string BehaviorGroupFolder = "behavior-groups";
        public static readonly string PropertySetFolder = "property-sets";
        public static readonly string TokenTemplatesFolder = "token-templates";
        public static readonly string TemplateFormulasFolder;
        public static readonly string TemplateDefinitionsFolder;
        public static readonly string SpecificationsFolder;
        internal static string Latest { get; private set; }
        public static string FolderSeparator { get; private set; }

        static ModelMap()
        {
            FolderSeparator = Os.IsWindows() ? "\\" : "/";
            TemplateFormulasFolder = TokenTemplatesFolder + FolderSeparator + "formulas";
            TemplateDefinitionsFolder = TokenTemplatesFolder + FolderSeparator + "definitions";
            SpecificationsFolder = TokenTemplatesFolder + FolderSeparator + "specifications";
            Latest =  "latest" + FolderSeparator;
        }

        public static ListDictionary GetClassificationDescription(Classification classification)
        {
            var retVal = new ListDictionary();

            retVal.Add(classification.TemplateType.ToString(),
                classification.TemplateType == TemplateType.SingleToken
                    ? "This token has no sub or child tokens."
                    : "This token has sub or child tokens defined in the Child Tokens section below.");
            
            retVal.Add(classification.TokenType.ToString(),
                classification.TokenType == TokenType.Fungible
                    ? "Tokens have interchangeable value with one another, where any quantity of them has the same value as another equal quantity as long as they are in the same class or series."
                    : "This token is not interchangeable with other tokens of the same type as they have different values.");

            switch (classification.TokenUnit)
            {
                case TokenUnit.Fractional:
                    retVal.Add(classification.TokenUnit.ToString(), "This token can be sub-divided or split into smaller units or parts based on a certain number of decimal places.");
                    break;
                case TokenUnit.Singleton:
                    retVal.Add(classification.TokenUnit.ToString(), "There is only one instance of this token and it cannot be subdivided.");
                    break;
                case TokenUnit.Whole:
                    retVal.Add(classification.TokenUnit.ToString(), "There can be many instances of this token, but they cannot be subdivided.");
                    break;
                default:
                    return retVal;
            }
            
            retVal.Add(classification.ValueType.ToString(),
                classification.ValueType == ValueType.Intrinsic
                    ? "This token is purely a digital token represents value directly, it represents no external physical form and cannot be a receipt or title for a material item or property."
                    : "This token is a receipt or title to a material item, property or right. The value is referenced and the token represents it and can be owned or used digitally via its token.");
            
            retVal.Add(classification.RepresentationType.ToString(),
                classification.RepresentationType == RepresentationType.Common
                    ? "This token is simply represented as a balance or quantity attributed to an owners address where all the balances are recorded on the same balance sheet, like a bank account. All instances can easily share common properties and locating them is simple."
                    : "Token instances are unique having their own identities and can be individually traced. Each unique token can carry unique properties that cannot be changed in one place and their balances must be summed. These are like bank notes, paper bills and metal coins, they are interchangeable but have unique properties like a serial number.");
            
            return retVal;
        }
        
        public static Classification GetClassification(TokenSpecification spec)
        {
            var template = ModelManager.GetTokenTemplate(new TokenTemplateId
            {
                DefinitionId = spec.DefinitionReference.Id
            });

            return new Classification
            {
                RepresentationType = spec.TokenBase.RepresentationType,
                TemplateType = template.Formula.TemplateType,
                ValueType = spec.TokenBase.ValueType,
                TokenType = spec.TokenBase.TokenType,
                TokenUnit = spec.TokenBase.TokenUnit
            };
        }
    }
}
