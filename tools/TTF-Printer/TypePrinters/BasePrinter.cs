﻿using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using log4net;
using System.Reflection;
using TTI.TTF.Taxonomy.Model.Core;

namespace TTI.TTF.Taxonomy.TypePrinters
{
    static class BasePrinter
    {
        private static readonly ILog _log;
        static BasePrinter()
        {
            #region logging

            Utils.InitLog();
            _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            #endregion
        }
        public static void AddBaseProperties(WordprocessingDocument document, Base tokenBase)
        {
            _log.Info("Printing Base Properties: " + tokenBase.TokenType);
            var body = document.MainDocumentPart.Document.Body;
            var baseProps = new[,]
            {
                {"Token Name:", tokenBase.Name},
                {"Token Type:", tokenBase.TokenType.ToString() },
                {"Representation Type:", tokenBase.RepresentationType.ToString()},
                {"Value Type:", tokenBase.ValueType.ToString()},
                {"Token Unit:",tokenBase.TokenUnit.ToString() },
                {"Symbol:", tokenBase.Symbol},
                {"Owner:", tokenBase.Owner},
                {"Quantity:", tokenBase.Quantity.ToString()},
                {"Decimals:", tokenBase.Decimals.ToString()},
                {"Constructor Name:",  tokenBase.ConstructorName}
            };

            var aDef = body.AppendChild(new Paragraph());
            var adRun = aDef.AppendChild(new Run());
            adRun.AppendChild(new Text("Base Details"));
            Utils.ApplyStyleToParagraph(document, "Heading1", "Heading1", aDef, JustificationValues.Center);
            Utils.AddTable(document, baseProps);

            var propDef = body.AppendChild(new Paragraph());
            var propRun = propDef.AppendChild(new Run());
            propRun.AppendChild(new Text("Properties:"));
            Utils.ApplyStyleToParagraph(document, "Heading2", "Heading2", propDef);

            var propsPara = body.AppendChild(new Paragraph());
            var propsRun = propDef.AppendChild(new Run());
            propsRun.AppendChild(Utils.GetGenericPropertyTable(document, "Name", "Value", tokenBase.TokenProperties));
            

            /*
            tokenBase.TokenProperties.ToString();
            tokenBase.Constructor != null
                ? tokenBase.Constructor.ToString()
                : "constructor TBD";
                */

        }
    }
}
