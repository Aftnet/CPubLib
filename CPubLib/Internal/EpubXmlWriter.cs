﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CPubLib.Internal
{
    internal static class EpubXmlWriter
    {
        public enum ImageFitting { Full, LeftHalf, RightHalf};

        private static XNamespace XHTMLNS { get; } = XNamespace.Get("http://www.w3.org/1999/xhtml");
        private static XNamespace DCNS { get; } = XNamespace.Get("http://purl.org/dc/elements/1.1/");
        private static XNamespace OPFNS { get; } = XNamespace.Get("http://www.idpf.org/2007/opf");
        private static XNamespace OPSNS { get; } = XNamespace.Get("http://www.idpf.org/2007/ops");

        private static XDeclaration XmlDeclaration { get; } = new XDeclaration("1.0", "utf-8", null);

        public static string GenerateContentPage(string imagePath, int imageWidth, int imageHeight, ImageFitting fit)
        {
            var canvasWidth = fit == ImageFitting.Full ? imageWidth : imageWidth / 2;
            var canvasHeight = imageHeight;
            var imageHorizontalOffset = fit == ImageFitting.RightHalf ? -imageWidth / 2 : 0;

            var output = string.Format(Resources.Strings.EpubPageTemplate, imagePath, canvasWidth, canvasHeight, imageWidth, imageHeight, imageHorizontalOffset);
            return output;
        }

        public static string GenerateContentOPF(Metadata metadata, IEnumerable<ItemDescription> contents, IEnumerable<PageDescription> spineEntries)
        {
            void AddElementIfValueNotNull(XElement parent, XName name, string value)
            {
                if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                parent.Add(new XElement(name, value));
            }

            if (!metadata.Valid)
            {
                throw new InvalidDataException();
            }

            var doc = new XDocument(XmlDeclaration);
            var root = new XElement(OPFNS + "package",
                new XAttribute("version", "3.0"),
                new XAttribute("unique-identifier", "bookid"),
                new XAttribute("prefix", "rendition: http://www.idpf.org/vocab/rendition/# cpublib: https://github.com/Aftnet/CPubLib"));
            doc.Add(root);

            var section = new XElement(OPFNS + "metadata", new XAttribute(XNamespace.Xmlns + "dc", DCNS),
                new XElement(DCNS + "type", "text"),
                new XElement(DCNS + "identifier", metadata.ID, new XAttribute("id", "bookid")),
                new XElement(OPFNS + "meta", DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), new XAttribute("property", "dcterms:modified")),
                new XElement(OPFNS + "meta", "pre-paginated", new XAttribute("property", "rendition:layout")),
                new XElement(DCNS + "title", metadata.Title),
                new XElement(DCNS + "creator", metadata.Author),
                new XElement(DCNS + "publisher", metadata.Publisher),
                new XElement(DCNS + "date", metadata.PublishingDate.ToString("yyyy-MM-dd")),
                new XElement(DCNS + "language", metadata.Language));
            AddElementIfValueNotNull(section, DCNS + "description", metadata.Description);
            foreach (var i in metadata.Tags)
            {
                section.Add(new XElement(DCNS + "subject", i));
            }
            metadata.Custom["fileformatversion"] = EPUBWriter.FileFormatVersion.ToString();
            foreach (var i in metadata.Custom)
            {
                section.Add(new XElement(OPFNS + "meta", i.Value, new XAttribute("property", $"cpublib:{i.Key}")));
            }

            AddElementIfValueNotNull(section, DCNS + "source", metadata.Source);
            AddElementIfValueNotNull(section, DCNS + "relation", metadata.Relation);
            AddElementIfValueNotNull(section, DCNS + "rights", metadata.Copyright);
            root.Add(section);

            root.Add(new XElement(OPFNS + "manifest", contents.Select(d =>
            {
                var output = new XElement(OPFNS + "item",
                    new XAttribute("href", d.Path),
                    new XAttribute("id", d.ID),
                    new XAttribute("media-type", d.MIMEType));
                if (d.Properties != null)
                {
                    output.Add(new XAttribute("properties", d.Properties));
                }

                return output;
            })));

            root.Add(new XElement(OPFNS + "spine", new XAttribute("page-progression-direction", metadata.RightToLeftReading ? "rtl" : "ltr"), spineEntries.Select(d =>
            {
                var output = new XElement(OPFNS + "itemref", new XAttribute("idref", d.ID));
                if (d.RefProperties != null)
                {
                    output.Add(new XAttribute("properties", d.RefProperties));
                }

                return output;
            })));

            return doc.ToStringWithDeclaration();
        }

        public static string GenerateNavXML(IEnumerable<PageDescription> entries)
        {
            entries = entries.Where(d => d.NavigationLabel != null).ToArray();
            var firstPage = entries.First();

            var doc = new XDocument(XmlDeclaration,
                new XElement(XHTMLNS + "html", new XAttribute(XNamespace.Xmlns + "epub", OPSNS),
                    new XElement(new XElement(XHTMLNS + "head", new XElement(XHTMLNS + "meta", new XAttribute("charset", "utf-8")))),
                    new XElement(XHTMLNS + "body",
                        new XElement(XHTMLNS + "nav", new XAttribute("id", "nav"), new XAttribute(OPSNS + "type", "toc"),
                            new XElement(XHTMLNS + "ol",
                                entries.Select(d => new XElement(XHTMLNS + "li",
                                    new XElement(XHTMLNS + "a", new XAttribute("href", d.Path), d.NavigationLabel))).ToArray()
                        ))
                        //new XElement(XHTMLNS + "nav", new XAttribute(OPSNS + "type", "landmarks"),
                        //    new XElement(XHTMLNS + "ol",
                        //        new XElement(XHTMLNS + "li",
                        //            new XElement(XHTMLNS + "a", new XAttribute("href", "nav.xhtml"), new XAttribute(OPSNS + "type", "toc"), "Contents")),
                        //        new XElement(XHTMLNS + "li",
                        //            new XElement(XHTMLNS + "a", new XAttribute("href", firstPage.Path), new XAttribute(OPSNS + "type", "bodymatter"), "Start")
                        //)))
                    )));

            return doc.ToStringWithDeclaration();
        }

        private static string ToStringWithDeclaration(this XDocument document)
        {
            return string.Concat(document.Declaration.ToString(), "\n", document.ToString());
        }
    }
}
