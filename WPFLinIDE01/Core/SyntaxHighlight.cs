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
