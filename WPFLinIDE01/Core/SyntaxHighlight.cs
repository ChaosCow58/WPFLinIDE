using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Xml;

using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace WPFLinIDE01.Core
{ 
    public class SyntaxHighlight
    {
        public SyntaxHighlight() { }

        /// <summary>
        /// The function `LoadSyntaxHighlightDefintion` loads a syntax highlighting definition from an
        /// XML file embedded as a resource in the assembly.
        /// </summary>
        /// <param name="xshdFileName">The `xshdFileName` parameter in the
        /// `LoadSyntaxHighlightDefintion` method is a string that represents the name of the XSHD (XML
        /// Syntax Highlighting Definition) file that contains syntax highlighting rules for a specific
        /// programming language or file format. This method loads and returns a syntax.</param>
        /// <returns>
        /// The method `LoadSyntaxHighlightDefintion` is returning an object that implements the
        /// `IHighlightingDefinition` interface. This object represents the syntax highlighting
        /// definition loaded from the specified XSHD file.
        /// </returns>
        public IHighlightingDefinition LoadSyntaxHighlightDefintion(string xshdFileName) 
        { 
            Assembly assembly = typeof(MainWindow).Assembly;

            using (Stream stream = assembly.GetManifestResourceStream($"WPFLinIDE01.SyntaxShaders.{xshdFileName}"))
            {
                if (stream == null)
                {
                    MessageBox.Show("Unable to load syntax highlighting.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                using (XmlTextReader reader = new XmlTextReader(stream))
                {
                    XshdSyntaxDefinition xshd = HighlightingLoader.LoadXshd(reader);

                    return HighlightingLoader.Load(xshd, HighlightingManager.Instance);
                }
            }
        }
    }
}
