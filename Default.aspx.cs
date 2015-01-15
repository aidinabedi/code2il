using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Web;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using Mono.Cecil;

namespace Code2IL
{
	public partial class Default : System.Web.UI.Page
	{
		private const string ErrorColor = "#8B0000";
		private const string WarningColor = "#DAA520";

		private static readonly Dictionary<string, string> CodeExamples =
			new Dictionary<string, string>
			{
				{
					"C#",

					"using System;\n" +
					"\n" +
					"public class Program\n" +
					"{\n" +
					"    static void Main(string[] args)\n" +
					"    {\n" +
					"        Console.WriteLine(\"Hello, MuchDifferent!\");\n" +
					"    }\n" +
					"}\n"
				},
				{
					"VB",

					"Imports System\n" +
					"\n" +
					"Public Module Program\n" +
					"    Public Sub Main(args() As String)\n" +
					"        Console.WriteLine(\"Hello, MuchDifferent!\")\n" +
					"    End Sub\n" +
					"End Module\n"
				}
			};

		protected void Page_Load(object sender, EventArgs e)
		{
			if (String.IsNullOrEmpty(Code.Text))
			{
				Language_SelectedIndexChanged(null, null);
			}

			_ClearOutput();
		}

		protected void Language_SelectedIndexChanged(object sender, EventArgs e)
		{
			string code;
			if (CodeExamples.TryGetValue(Language.SelectedValue, out code))
			{
				Code.Text = code;
			}
		}

		protected void Convert_Click(object sender, EventArgs e)
		{
			CompilerResults results = null;
			AssemblyDefinition assembly;

			try
			{
				results = Helper.CompileSourceCode(Code.Text, Language.SelectedValue, CompilerVersion.SelectedValue, Optimize.Checked, Debug.Checked);

				if (String.IsNullOrEmpty(results.PathToAssembly) || !File.Exists(results.PathToAssembly))
				{
					_WriteCompilerResults(results);
					return;
				}

				assembly = AssemblyDefinition.ReadAssembly(results.PathToAssembly);
			}
			catch (Exception ex)
			{
				_WriteException("Uh oh, the compiler freaked out", ex);
				return;
			}
			finally
			{
				Helper.CleanupAfterCompiler(results);
			}

			try
			{
				_WriteLine(Helper.DisassembleToIL(assembly, DetectControlStrucures.Checked, IncludeHeaders.Checked));
			}
			catch (Exception ex)
			{
				_WriteException("Uh oh, the disassembler freaked out", ex);
			}
		}

		private void _WriteCompilerResults(CompilerResults results)
		{
			if (results.Errors.Count > 0)
			{
				_WriteLine("Sorry, your code might be broken:\n");

				foreach (CompilerError error in results.Errors)
				{
					string format =
						String.IsNullOrEmpty(error.FileName) ?
						"{2} {3}: {4}" :
						"Line {0} (Column {1}) : {2} {3}: {4}";

					string text = String.Format(
						CultureInfo.InvariantCulture,
						format,
						error.Line,
						error.Column,
						error.IsWarning ? "warning" : "error",
						error.ErrorNumber,
						error.ErrorText
						);

					_WriteLine(text, error.IsWarning ? WarningColor : ErrorColor);
				}
			}
			else if (results.Errors.Count > 0)
			{
				_WriteError("Something went wrong, here is the compiler output:\n");

				foreach (string output in results.Output)
				{
					_WriteLine(output);
				}
			}
			else
			{
				_WriteError("Oops, bad stuff happened. The assembly file is missing without explanation.");
			}
		}

		private void _WriteException(string title, Exception ex)
		{
			_WriteError(title + ":\n\n" + ex.GetType().Name + " :" + ex.Message);
		}

		private void _WriteError(string text)
		{
			_WriteLine(text, ErrorColor);
		}

		private void _WriteLine(string text, string color)
		{
			Output.Text += "<div style=\"color:" + color + "\">" + Helper.HtmlEncode(text) + "&nbsp;</div>";
		}

		private void _WriteLine(string text)
		{
			Output.Text += "<div>" + Helper.HtmlEncode(text) + "&nbsp;</div>";
		}

		private void _ClearOutput()
		{
			Output.Text = String.Empty;
		}
	}

	public static class Helper
	{
		public static string HtmlEncode(string text)
		{
			return HttpUtility.HtmlEncode(text).Replace(" ", "&nbsp;").Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;").Replace("\n", "<br />");
		}

		public static string DisassembleToIL(AssemblyDefinition assembly, bool detectControlStructure = true, bool includeHeaders = false)
		{
			const string hiddenEmptyModuleClass =
				".class private auto ansi '<Module>'\n" +
				"{\n" +
				"} // end of class <Module>\n" +
				"\n";

			var writer = new StringWriter { NewLine = "\n" };
			var output = new PlainTextOutput(writer);
			var disassembler = new ReflectionDisassembler(output, detectControlStructure, new CancellationToken());

			if (includeHeaders)
			{
				disassembler.WriteAssemblyReferences(assembly.MainModule);
				disassembler.WriteAssemblyHeader(assembly);

				output.WriteLine();
				disassembler.WriteModuleHeader(assembly.MainModule);

				output.WriteLine();
				output.WriteLine();
			}

			disassembler.WriteModuleContents(assembly.MainModule);

			var result = writer.ToString();
			if (!includeHeaders && result.StartsWith(hiddenEmptyModuleClass, StringComparison.InvariantCulture))
			{
				result = result.Substring(hiddenEmptyModuleClass.Length);
			}

			return result;
		}

		private static readonly string[] References =
		{
			"System.dll",
			"System.Core.dll",
			"System.Data.dll",
			"System.Data.DataSetExtensions.dll",
			"System.Xml.dll",
			"System.Xml.Linq.dll",
			//"System.Numerics.dll", // TODO: only add when compilerVersion v4.0
			//"Microsoft.CSharp.dll", // TODO: only add when compilerVersion v4.0
			"Microsoft.VisualBasic.dll",
			"System.Net.dll",
			"System.Web.dll",
			"System.ComponentModel.DataAnnotations.dll",
			//"System.ComponentModel.Composition.dll", // TODO: only add when compilerVersion v4.0
			"System.Drawing.dll",
		};

		public static CompilerResults CompileSourceCode(string code, string language = "C#", string compilerVersion = "v4.0", bool optimize = true, bool debug = true)
		{
			var version = new Dictionary<string, string> { { "CompilerVersion", compilerVersion } };
			var provider = _GetCompiler(language, version);

			var options = new CompilerParameters
			{
				GenerateInMemory = false,
				GenerateExecutable = false,
				IncludeDebugInformation = debug,
				// TODO: only apply language-specific command line options when that language is selected.
				CompilerOptions = "/unsafe /o" + (optimize ? '+' : '-') + (debug ? " /debug" : "")
			};

			foreach (var reference in References)
			{
				options.ReferencedAssemblies.Add(reference);
			}

			return provider.CompileAssemblyFromSource(options, code);
		}

		public static void CleanupAfterCompiler(CompilerResults results)
		{
			if (results != null)
			{
				if (!String.IsNullOrEmpty(results.PathToAssembly) && File.Exists(results.PathToAssembly))
				{
					File.Delete(results.PathToAssembly);
				}

				if (results.TempFiles != null)
				{
					results.TempFiles.Delete();
				}
			}
		}

		private static CodeDomProvider _GetCompiler(string language, IDictionary<string, string> options)
		{
			switch (language)
			{
				case "C#": return new CSharpCodeProvider(options);
				case "VB": return new VBCodeProvider(options);
				default: throw new Exception("Missing compiler for language '" + language + "'");
			}
		}
	}
}
