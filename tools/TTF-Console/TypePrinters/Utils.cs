﻿using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using Google.Protobuf.Collections;
using log4net;
using log4net.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;
using TTI.TTF.Taxonomy.Model.Core;
using V = DocumentFormat.OpenXml.Vml;

namespace TTI.TTF.Taxonomy.TypePrinters
{
    public static class Os
    {
        public static bool IsWindows()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        public static bool IsMacOs()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }

        public static bool IsLinux()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }

        public static string WhatIs()
        {
            var os = (IsWindows() ? "win" : null) ??
                     (IsMacOs() ? "mac" : null) ??
                     (IsLinux() ? "gnu" : null);
            return os;
        }
    }
    public static class Utils
    {
        private static readonly ILog Log;

        static Utils()
        {
            #region logging

            InitLog();
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            #endregion
        }
        
        public static void InitLog()
        {
            var xmlDocument = new XmlDocument();
            try
            {
                if (Os.IsWindows())
                    xmlDocument.Load(File.OpenRead(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) +
                                                   "\\log4net.config"));
                else
                    xmlDocument.Load(File.OpenRead(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) +
                                                   "/log4net.config"));
            }
            catch (Exception)
            {
                if (Os.IsWindows())
                    xmlDocument.Load(File.OpenRead(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log4net.config"));
                else
                    xmlDocument.Load(File.OpenRead(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/log4net.config"));
            }

            XmlConfigurator.Configure(
                LogManager.CreateRepository(Assembly.GetEntryAssembly(),
                    typeof(log4net.Repository.Hierarchy.Hierarchy)), xmlDocument["log4net"]);
        }
        
        public static Table GetNewTable(int columns)
        {
            Table table = new Table();

            TableProperties props = new TableProperties(
                new TableBorders(
                    new TopBorder
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Single),
                        Size = 12
                    },
                    new BottomBorder
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Single),
                        Size = 12
                    },
                    new LeftBorder
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Single),
                        Size = 12
                    },
                    new RightBorder
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Single),
                        Size = 12
                    },
                    new InsideHorizontalBorder
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Single),
                        Size = 12
                    },
                    new InsideVerticalBorder
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Single),
                        Size = 12
                    }));

            table.AppendChild(props);

            for (var j = 0; j <= columns; j++)
            {
                var tc = new TableCell();
                // Code removed here…
                table.Append(tc);
            }

            return table;
        }

        #region formatting


        // Take the data from a two-dimensional array and build a table at the 
        // end of the supplied document.
        public static void AddTable(WordprocessingDocument document, string[,] data, string styleName = "GridTable4-Accent1")
        {
            var table = new Table();
            GetFormattedTable(table);

            for (var i = 0; i <= data.GetUpperBound(0); i++)
            {
                var tr = new TableRow();
                for (var j = 0; j <= data.GetUpperBound(1); j++)
                {
                    var tc = new TableCell();
                    tc.Append(new Paragraph(new Run(new Text(data[i, j]))));

                    if (IsLabel(data[i, j]))
                        tc.Append(new TableCellProperties(
                            new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "30" }));
                    else
                    {
                        tc.Append(new TableCellProperties(
                            new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "70" }));
                    }

                    tr.Append(tc);
                }

                table.Append(tr);
            }
            ApplyStyleTable(document, styleName, styleName, table);
            document.MainDocumentPart.Document.Body.Append(table);
        }

        public static Table GetTable(WordprocessingDocument document, string[,] data)
        {
            var table = new Table();
            GetFormattedTable(table);

            for (var i = 0; i <= data.GetUpperBound(0); i++)
            {
                var tr = new TableRow();
                for (var j = 0; j <= data.GetUpperBound(1); j++)
                {
                    var tc = new TableCell();
                    tc.Append(new Paragraph(new Run(new Text(data[i, j]))));

                    if (IsLabel(data[i, j]))
                        tc.Append(new TableCellProperties(
                            new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "30" }));
                    else
                    {
                        tc.Append(new TableCellProperties(
                            new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "70" }));
                    }

                    tr.Append(tc);
                }

                table.Append(tr);
            }
            ApplyStyleTable(document, "GridTable4-Accent1", "GridTable4-Accent1", table);
            return table;
        }

        // Take the data from a two-dimensional array and build a table at the 
        // end of the supplied document.
        public static Table GetParameterTable(WordprocessingDocument document, string col1Name, string col2Name, 
            IEnumerable<InvocationParameter> data)
        {
            var table = new Table();

            var tr = new TableRow();
            var tch = new TableCell();
            tch.Append(new Paragraph(new Run(new Text(col1Name))));
            tch.Append(new TableCellProperties(
                            new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "30" }));
            var tch2 = new TableCell();
            tch2.Append(new Paragraph(new Run(new Text(col2Name))));
            tch2.Append(new TableCellProperties(
                            new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "70" }));
            tr.Append(tch);
            tr.Append(tch2);
            table.Append(tr);
            foreach (var r in data)
            {
                var trr = new TableRow();
                var tc = new TableCell();
                tc.Append(new Paragraph(new Run(new Text(r.Name))));
                var tc2 = new TableCell();
                tc2.Append(new Paragraph(new Run(new Text(r.ValueDescription))));

                trr.Append(tc);
                trr.Append(tc2);
                table.Append(trr);
            }
            ApplyStyleTable(document, "GridTable4-Accent1", "GridTable4-Accent1", table);
            return table;
        }

        // Take the data from a two-dimensional array and build a table at the 
        // end of the supplied document.
        public static Table GetGenericPropertyTable(WordprocessingDocument document, string col1Name, string col2Name,
            MapField<string, string> data, string styleName = "GridTable4-Accent1")
        {
            var table = new Table();

            var tr = new TableRow();
            var tch = new TableCell();
            tch.Append(new Paragraph(new Run(new Text(col1Name))));
            tch.Append(new TableCellProperties(
                            new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "30" }));
            var tch2 = new TableCell();
            tch2.Append(new Paragraph(new Run(new Text(col2Name))));
            tch2.Append(new TableCellProperties(
                            new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "70" }));
            tr.Append(tch);
            tr.Append(tch2);
            table.Append(tr);
            foreach (var r in data)
            {
                var trr = new TableRow();
                var tc = new TableCell();
                tc.Append(new Paragraph(new Run(new Text(r.Key))));
                var tc2 = new TableCell();
                tc2.Append(new Paragraph(new Run(new Text(r.Value))));

                trr.Append(tc);
                trr.Append(tc2);
                table.Append(trr);
            }
            ApplyStyleTable(document, styleName, styleName, table);
            return table;
        }

        internal static bool ValidateWordDocument()
        {
            throw new NotImplementedException();
        }

        private static bool IsLabel(string s)
        {
            return s.Contains(":");
        }


        private static void GetFormattedTable(Table table)
        {
            var props = new TableProperties(
                new TableBorders(
                    new TopBorder
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.None),
                        Size = 12
                    },
                    new BottomBorder
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.None),
                        Size = 12
                    },
                    new LeftBorder
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.None),
                        Size = 12
                    },
                    new RightBorder
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.None),
                        Size = 12
                    },
                    new InsideHorizontalBorder
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.None),
                        Size = 12
                    },
                    new InsideVerticalBorder
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.None),
                        Size = 12
                    }));

            table.AppendChild(props);
        }

        public static bool ValidateWordDocument(WordprocessingDocument document)
        {
            try
            {
                var validator = new OpenXmlValidator();
                var count = 0;
                foreach (var error in
                    validator.Validate(document))
                {
                    count++;
                    Log.Info("Error " + count);
                    Log.Info("Description: " + error.Description);
                    Log.Info("ErrorType: " + error.ErrorType);
                    Log.Info("Node: " + error.Node);
                    Log.Info("Path: " + error.Path.XPath);
                    Log.Info("Part: " + error.Part.Uri);
                    Log.Info("-------------------------------------------");
                }

                Log.Info("count=" + count);
                return true;
            }

            catch (Exception ex)
            {
                Log.Info(ex.Message);
                return false;
            }
        }

        public static void ValidateCorruptedWordDocument(WordprocessingDocument document)
        {
            // Insert some text into the body, this would cause Schema Error

            // Insert some text into the body, this would cause Schema Error
            var body = document.MainDocumentPart.Document.Body;
            var run = new Run(new Text("some text"));
            body.Append(run);

            try
            {
                var validator = new OpenXmlValidator();
                var count = 0;
                foreach (var error in
                    validator.Validate(document))
                {
                    count++;
                    Log.Info("Error " + count);
                    Log.Info("Description: " + error.Description);
                    Log.Info("ErrorType: " + error.ErrorType);
                    Log.Info("Node: " + error.Node);
                    Log.Info("Path: " + error.Path.XPath);
                    Log.Info("Part: " + error.Part.Uri);
                    Log.Info("-------------------------------------------");
                }

                Log.Info("count=" + count.ToString());
            }

            catch (Exception ex)
            {
                Log.Info(ex.Message);
            }
        }

        internal static void AddFooter(WordprocessingDocument document, string name)
        {
            var footerPart = document.MainDocumentPart.AddNewPart<FooterPart>();

            var footerPartId = document.MainDocumentPart.GetIdOfPart(footerPart);


            var footer1 = new Footer();

            var paragraph1 = new Paragraph();

            var paragraphProperties1 = new ParagraphProperties();
            var paragraphStyleId1 = new ParagraphStyleId { Val = "Footer" };

            paragraphProperties1.Append(paragraphStyleId1);

            var run1 = new Run();
            var text1 = new Text { Text = name };

            run1.Append(text1);

            paragraph1.Append(paragraphProperties1);
            paragraph1.Append(run1);

            footer1.Append(paragraph1);

            footerPart.Footer = footer1;

            var sections = document.MainDocumentPart.Document.Body.Elements<SectionProperties>();
            document.MainDocumentPart.Document.Body.PrependChild(new FooterReference { Id = footerPartId });
            foreach (var section in sections)
            {
                section.RemoveAllChildren<FooterReference>();

                // Create the new header and footer reference node
                section.PrependChild(new FooterReference { Id = footerPartId });
            }
            footer1.Save();
            PrintController.Save();
        }

        internal static void InsertCustomWatermark(WordprocessingDocument document, string imagePath)
        {
            SetWaterMarkPicture(imagePath);
            var mainDocumentPart1 = document.MainDocumentPart;
            if (mainDocumentPart1 == null) return;
            mainDocumentPart1.DeleteParts(mainDocumentPart1.HeaderParts);
            var headPart1 = mainDocumentPart1.AddNewPart<HeaderPart>();
            GenerateHeaderPart1Content(headPart1);
            var rId = mainDocumentPart1.GetIdOfPart(headPart1);
            var image = headPart1.AddNewPart<ImagePart>("image/jpeg", "rId999");
            GenerateImagePart1Content(image);
            var sectPrs = mainDocumentPart1.Document.Body.Elements<SectionProperties>();
            mainDocumentPart1.Document.Body.PrependChild(new HeaderReference { Id = rId });
            foreach (var sectPr in sectPrs)
            {
                sectPr.RemoveAllChildren<HeaderReference>();
                sectPr.PrependChild(new HeaderReference { Id = rId });
            }

        }
        private static void GenerateHeaderPart1Content(HeaderPart headerPart1)
        {
            var header1 = new Header();
            var paragraph2 = new Paragraph();
            var run1 = new Run();
            var picture1 = new Picture();
            var shape1 = new V.Shape { Id = "WordPictureWatermark75517470", Style = "position:absolute;left:0;text-align:left;margin-left:0;margin-top:0;width:456.15pt;height:456.15pt;z-index:-251656192;mso-position-horizontal:center;mso-position-horizontal-relative:margin;mso-position-vertical:center;mso-position-vertical-relative:margin", OptionalString = "_x0000_s2051", AllowInCell = false, Type = "#_x0000_t75" };
            var imageData1 = new V.ImageData { Gain = "19661f", BlackLevel = "22938f", Title = "水印", RelationshipId = "rId999" };
            shape1.Append(imageData1);
            picture1.Append(shape1);
            run1.Append(picture1);
            paragraph2.Append(run1);
            header1.Append(paragraph2);
            headerPart1.Header = header1;
            header1.Save();
        }
        private static void GenerateImagePart1Content(ImagePart imagePart1)
        {
            var data = GetBinaryDataStream(_imagePart1Data);
            imagePart1.FeedData(data);
            data.Close();
        }

        private static string _imagePart1Data = "";

        private static Stream GetBinaryDataStream(string base64String)
        {
            return new MemoryStream(Convert.FromBase64String(base64String));
        }

        public static void SetWaterMarkPicture(string file)
        {
            try
            {
                var inFile = new FileStream(file, FileMode.Open, FileAccess.Read);
                var byteArray = new byte[inFile.Length];
                long byteRead = inFile.Read(byteArray, 0, (int)inFile.Length);
                inFile.Close();
                _imagePart1Data = Convert.ToBase64String(byteArray, 0, byteArray.Length);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        public static void ApplyStyleTable(WordprocessingDocument document, string styleId, string styleName, Table t)
        {
            // If the paragraph has no ParagraphProperties object, create one.
            if (!t.Elements<TableProperties>().Any())
            {
                t.PrependChild(new TableProperties());
            }


            // Get the paragraph properties element of the paragraph.
            var tTr = t.Elements<TableProperties>().First();

            // Get the Styles part for this document.
            var part =
                document.MainDocumentPart.StyleDefinitionsPart;

            // If the Styles part does not exist, add it and then add the style.
            if (part != null)
            {
                // If the style is not in the document, add it.
                // If the style is not in the document, add it.
                if (IsStyleIdInDocument(PrintController.StylesPart, styleId, StyleValues.Table) != true)
                {
                    // No match on styleId, so let's try style name.
                    var fromStyleName = GetStyleIdFromStyleName(PrintController.StylesPart, styleName, StyleValues.Table);
                    if (fromStyleName != null)
                    {
                        tTr.TableStyle = new TableStyle { Val = fromStyleName };
                        return;

                    }
                }

            }

            // Set the style of the paragraph.
            tTr.TableStyle = new TableStyle { Val = styleId };

        }

        public static void ApplyStyleToParagraph(WordprocessingDocument document, string styleId,
            string styleName, Paragraph p, JustificationValues justification = JustificationValues.Left)
        {
            // If the paragraph has no ParagraphProperties object, create one.
            if (!p.Elements<ParagraphProperties>().Any())
            {
                p.PrependChild(new ParagraphProperties
                {
                    Justification = new Justification { Val = justification }
                });
            }

            // Get the paragraph properties element of the paragraph.
            var pPr = p.Elements<ParagraphProperties>().First();

            // Get the Styles part for this document.
            var part =
                document.MainDocumentPart.StyleDefinitionsPart;

            // If the Styles part does not exist, add it and then add the style.
            if (part != null)
            {
                // If the style is not in the document, add it.
                if (IsStyleIdInDocument(PrintController.StylesPart, styleId) != true)
                {
                    // No match on styleId, so let's try style name.
                    var fromStyleName = GetStyleIdFromStyleName(PrintController.StylesPart, styleName);
                    if (fromStyleName != null)

                        styleId = fromStyleName;
                }
            }

            // Set the style of the paragraph.
            pPr.ParagraphStyleId = new ParagraphStyleId() { Val = styleId };
        }

        private static bool IsStyleIdInDocument(StylesPart styles, string styleId, StyleValues styleValues = StyleValues.Paragraph)
        {
            // Get access to the Styles element for this document.
            var s = styles.Styles;

            // Check that there are styles and how many.
            var n = s.Elements<Style>().Count();
            if (n == 0)
                return false;

            // Look for a match on styleId.
            var style = s
                .Elements<Style>()
                .FirstOrDefault(st => (st.StyleId == styleId) && (st.Type == styleValues));
            return style != null;
        }
        
        // Return styleId that matches the styleName, or null when there's no match.
        private static string GetStyleIdFromStyleName(StylesPart styles, string styleName, StyleValues styleValues = StyleValues.Paragraph)
        {
            var stylePart = styles;
            string styleId = stylePart.Styles.Descendants<StyleName>()
                .Where(s => s.Val.Value.Equals(styleName) &&
                            ((Style)s.Parent).Type == styleValues)
                .Select(n => ((Style)n.Parent).StyleId).FirstOrDefault();
            return styleId;
        }


        //https://docs.microsoft.com/en-us/office/open-xml/how-to-replace-the-styles-parts-in-a-word-processing-document
        public static void ReplaceStylesPart(WordprocessingDocument document, XDocument newStyles,
            bool setStylesWithEffectsPart = false)
        {

            // Get a reference to the main document part.
            var docPart = document.MainDocumentPart;

            // Assign a reference to the appropriate part to the
            // stylesPart variable.
            StylesPart stylesPart = null;
            if (setStylesWithEffectsPart)
                stylesPart = docPart.StylesWithEffectsPart;
            else
                stylesPart = docPart.StyleDefinitionsPart;

            // If the part exists, populate it with the new styles.
            if (stylesPart == null) return;
            newStyles.Save(new StreamWriter(stylesPart.GetStream(
                FileMode.Create, FileAccess.Write)));
            
            newStyles.Save(new StreamWriter(PrintController.StylesPart.GetStream(
                FileMode.Create, FileAccess.Read)));

        }

        public static StyleDefinitionsPart AddStylesPartToPackage(WordprocessingDocument document)
        {
            var part = document.MainDocumentPart.AddNewPart<StyleDefinitionsPart>();
            var root = new Styles();
            root.Save(part);
            return part;
        }

        // Extract the styles or stylesWithEffects part from a 
        // word processing document as an XDocument instance.
        public static XDocument ExtractStylesPart(
          string fileName,
          bool getStylesWithEffectsPart = false)
        {
            // Declare a variable to hold the XDocument.
            XDocument styles = null;

            // Open the document for read access and get a reference.
            using (var document =
                WordprocessingDocument.Open(fileName, false))
            {
                // Get a reference to the main document part.
                var docPart = document.MainDocumentPart;

                // Assign a reference to the appropriate part to the
                // stylesPart variable.
                StylesPart stylesPart = null;
                if (getStylesWithEffectsPart)
                    stylesPart = docPart.StylesWithEffectsPart;
                else
                    stylesPart = docPart.StyleDefinitionsPart;

                // If the part exists, read it into the XDocument.
                if (stylesPart != null)
                {
                    using (var reader = XmlReader.Create(
                      stylesPart.GetStream(FileMode.Open, FileAccess.Read)))
                    {
                        // Create the XDocument.
                        styles = XDocument.Load(reader);
                    }
                }
            }
            // Return the XDocument instance.
            return styles;
        }
        #endregion

        
    }
}