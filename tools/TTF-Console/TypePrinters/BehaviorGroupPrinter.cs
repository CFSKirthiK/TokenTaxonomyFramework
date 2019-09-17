﻿using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using log4net;
using System.Reflection;
using TTI.TTF.Taxonomy.Model.Core;

namespace TTI.TTF.Taxonomy.TypePrinters
{
    internal static class BehaviorGroupPrinter
    {
        private static readonly ILog Log;
        static BehaviorGroupPrinter()
        {
            #region logging

            Utils.InitLog();
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            #endregion
        }

        public static void AddBehaviorGroupProperties(WordprocessingDocument document, BehaviorGroup bg)
        {
            Log.Info("Printing Behavior Grop Properties: " + bg.Artifact.Name);
            var body = document.MainDocumentPart.Document.Body;

            var aDef = body.AppendChild(new Paragraph());
            var adRun = aDef.AppendChild(new Run());
            adRun.AppendChild(new Text("Behavior Group Details"));
            Utils.ApplyStyleToParagraph(document, "Heading1", "Heading1", aDef, JustificationValues.Center);

            foreach (var br in bg.Behaviors)
            {
                BehaviorPrinter.AddBehaviorReferenceProperties(document, br);
            }

        }
    }
}
