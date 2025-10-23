using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AnalizadorLexico
{
	public partial class MainWindow : Window
	{
		private readonly SnackbarMessageQueue _queue;
		private readonly DataTable _table = new();

		public MainWindow()
		{
			InitializeComponent();

			_queue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));
			Snack.MessageQueue = _queue;

			_table.Columns.Add("Tipo");
			_table.Columns.Add("Lexema");
			_table.Columns.Add("Linea", typeof(int));
			_table.Columns.Add("Columna", typeof(int));
			GridTokens.ItemsSource = _table.DefaultView;

			TxtSource.Text = "int x = 10;\nif (x >= 10) return \"ok\"; // demo\n";
		}

		// --- Botones de la App Bar ---
		private void Abrir_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog { Filter = "Código|*.txt;*.c;*.cpp;*.cs;*.java;*.js|Todos|*.*" };
			if (ofd.ShowDialog() == true)
				TxtSource.Text = File.ReadAllText(ofd.FileName, Encoding.UTF8);
		}

		private void Guardar_Click(object sender, RoutedEventArgs e)
		{
			var sfd = new SaveFileDialog { Filter = "Texto|*.txt" };
			if (sfd.ShowDialog() == true)
				File.WriteAllText(sfd.FileName, TxtSource.Text, Encoding.UTF8);
		}

		private async void Analizar_Click(object sender, RoutedEventArgs e)
		{
			_table.Rows.Clear();
			Prog.Visibility = Visibility.Visible;
			try
			{
				var csv = await RunLexerAsync(TxtSource.Text);
				LoadCsvToTable(csv);
				ShowSnack($"Listo: {_table.Rows.Count} tokens.");
			}
			catch (Exception ex)
			{
				ShowSnack(ex.Message);
				MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				Prog.Visibility = Visibility.Collapsed;
			}
		}

		// --- Ejecuta lexer.exe y devuelve el stdout (CSV) ---
		private async Task<string> RunLexerAsync(string input)
		{
			// 🔧 Elimina posibles caracteres BOM o invisibles
			input = input.TrimStart('\uFEFF', '\u200B', '\r', '\n');

			var exe = Path.Combine(AppContext.BaseDirectory, "lexer.exe");
			if (!File.Exists(exe))
				throw new FileNotFoundException("No se encontró lexer.exe.", exe);

			var psi = new ProcessStartInfo(exe)
			{
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardInputEncoding = Encoding.UTF8
			};

			using var p = Process.Start(psi)!;
			await p.StandardInput.WriteAsync(input);
			p.StandardInput.Close();

			string stdout = await p.StandardOutput.ReadToEndAsync();
			string stderr = await p.StandardError.ReadToEndAsync();
			await p.WaitForExitAsync();

			if (p.ExitCode != 0 && !string.IsNullOrWhiteSpace(stderr))
				throw new InvalidOperationException("lexer.exe error: " + stderr);

			return stdout;
		}


		// --- CSV -> DataTable ---
		private void LoadCsvToTable(string csv)
		{
			using var sr = new StringReader(csv);
			string? line;
			bool header = true;
			while ((line = sr.ReadLine()) != null)
			{
				if (string.IsNullOrWhiteSpace(line)) continue;
				if (header) { header = false; continue; } // salta encabezado

				var cols = SplitCsv(line); // Tipo,"Lexema",Linea,Columna
				if (cols.Length < 4) continue;

				// Normaliza nombres de columnas con los Header de la grilla
				_table.Rows.Add(cols[0], cols[1], int.Parse(cols[2]), int.Parse(cols[3]));
			}
		}

		// Minimal CSV splitter (maneja comillas dobles escapadas)
		private static string[] SplitCsv(string line)
		{
			var list = new System.Collections.Generic.List<string>();
			var sb = new StringBuilder();
			bool inq = false;

			for (int i = 0; i < line.Length; i++)
			{
				char c = line[i];
				if (c == '"')
				{
					if (inq && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
					else inq = !inq;
				}
				else if (c == ',' && !inq)
				{
					list.Add(sb.ToString());
					sb.Clear();
				}
				else sb.Append(c);
			}
			list.Add(sb.ToString());
			return list.ToArray();
		}

		private void ExportarCsv_Click(object sender, RoutedEventArgs e)
		{
			var sfd = new SaveFileDialog { Filter = "CSV|*.csv" };
			if (sfd.ShowDialog() == true)
			{
				var sb = new StringBuilder();
				sb.AppendLine("Tipo,Lexema,Linea,Columna");
				foreach (System.Data.DataRow r in _table.Rows)
				{
					var lex = r["Lexema"]?.ToString()?.Replace("\"", "\"\"") ?? "";
					sb.AppendLine($"{r["Tipo"]},\"{lex}\",{r["Linea"]},{r["Columna"]}");
				}
				File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
				ShowSnack("Exportado CSV.");
			}
		}

		private void ShowSnack(string msg) => _queue.Enqueue(msg);
	}
}
