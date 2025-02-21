﻿using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using PDFPatcher.Common;
using PDFPatcher.Model;

namespace PDFPatcher.Functions
{
	[ToolboxItem (false)]
	public partial class OcrControl : FunctionControl, IResettableControl
	{
		OcrOptions _options;

		public override string FunctionName {
			get { return "识别图像文本"; }
		}

		public override System.Drawing.Bitmap IconImage {
			get { return Properties.Resources.Ocr; }
		}

		public override Button DefaultButton {
			get { return _ExportBookmarkButton; }
		}

		public OcrControl () {
			InitializeComponent ();
			//this.Icon = Common.FormHelper.ToIcon (Properties.Resources.Ocr);
			this._BookmarkControl.FileDialog.Filter = Constants.FileExtensions.TxtFilter + "|" + Constants.FileExtensions.XmlFilter + "|" + Constants.FileExtensions.XmlOrTxtFilter;

			AppContext.MainForm.SetTooltip (this._SourceFileControl.FileList, "需要识别文本的 PDF 源文件路径");
			AppContext.MainForm.SetTooltip (this._BookmarkControl.FileList, "指定识别文本后生成的信息文件或文本文件路径，如路径为空则不输出文件");
			AppContext.MainForm.SetTooltip (this._ExportBookmarkButton, "点击此按钮导出识别后的文本到文件");
			AppContext.MainForm.SetTooltip (this._ImportOcrResultButton, "点击此按钮，将识别后的文本内容写入到目标 PDF 文件。");
			AppContext.MainForm.SetTooltip (this._PageRangeBox, Messages.PageRanges);
			AppContext.MainForm.SetTooltip (this._DetectColumnsBox, "允许将距离较远的文本合并为同一行文本");
			AppContext.MainForm.SetTooltip (this._DetectContentPunctuationsBox, "将三个以上的连续标点替换成“ .... ”");
			AppContext.MainForm.SetTooltip (this._CompressWhiteSpaceBox, "将三个以上连续出现的空格压缩成两个空格");
			AppContext.MainForm.SetTooltip (this._OrientBox, "自动检测页面横竖置放方向");
			AppContext.MainForm.SetTooltip (this._StretchBox, "自动纠直倾斜的页面");
			AppContext.MainForm.SetTooltip (this._OutputOriginalOcrResultBox, "保存原始的未经过优化合并的识别结果（可用于写入 PDF 文档）");

			var lb = _OcrLangBox.Items;
			if (Processor.ModiOcr.ModiInstalled) {
				foreach (var item in Constants.Ocr.LangIDs) {
					if (Processor.ModiOcr.IsLanguageInstalled (item)) {
						lb.Add (ValueHelper.MapValue (item, Constants.Ocr.LangIDs, Constants.Ocr.LangNames));
					}
				}
			}
			if (lb.Count == 0) {
				lb.Add ("无");
			}
			this._ExportBookmarkButton.Enabled = Processor.ModiOcr.ModiInstalled;
			if (_ExportBookmarkButton.Enabled == false) {
				AppContext.MainForm.SetTooltip (_OcrLangBox, "当前系统尚未安装识别引擎，请先安装微软 Office 文字识别引擎，再重新启动程序。");
			}
			Reload ();

			var d = this._BookmarkControl.FileDialog;
			d.CheckFileExists = false;
			d.CheckPathExists = false;

			var sd = d as SaveFileDialog;
			if (sd != null) {
				sd.OverwritePrompt = false;
			}
		}

		public override void SetupCommand (ToolStripItem item) {
			var n = item.Name;
			switch (n) {
				case Commands.SaveBookmark:
					item.Text = "写入PDF文件(&Q)";
					item.ToolTipText = "将识别结果写入 PDF 文件";
					EnableCommand (item, true, true);
					break;
				default:
					break;
			}
			base.SetupCommand (item);
		}

		public override void ExecuteCommand (string commandName, params string[] parameters) {
			switch (commandName) {
				case Commands.SaveBookmark:
					_ImportOcrResultButton.PerformClick ();
					return;
				default:
					break;
			}
			base.ExecuteCommand (commandName, parameters);
		}

		public void Reset () {
			AppContext.Ocr = new OcrOptions ();
			Reload ();
		}

		public void Reload () {
			_options = AppContext.Ocr;
			_CompressWhiteSpaceBox.Checked = _options.CompressWhiteSpaces;
			_ConvertToMonoColorBox.Checked = !_options.PreserveColor;
			_DetectColumnsBox.Checked = _options.DetectColumns;
			_DetectContentPunctuationsBox.Checked = _options.DetectContentPunctuations;
			var i = Array.IndexOf (Constants.Ocr.LangIDs, _options.OcrLangID);
			_OcrLangBox.Select (i > 0 ? i : 0);
			_OrientBox.Checked = _options.OrientPage;
			_RemoveSpaceBetweenChineseBox.Checked = _options.RemoveWhiteSpacesBetweenChineseCharacters;
			_SaveOcredImageBox.Checked = !String.IsNullOrEmpty (_options.SaveOcredImagePath);
			_StretchBox.Checked = _options.StretchPage;
			_OutputOriginalOcrResultBox.Checked = _options.OutputOriginalOcrResult;
			_PrintOcrResultBox.Checked = _options.PrintOcrResult;

			_WritingDirectionBox.SelectedIndex = (int)_options.WritingDirection;
			_QuantitiveFactorBox.SetValue (_options.QuantitativeFactor);
		}

		private void Button_Click (object sender, EventArgs e) {
			if (File.Exists (_SourceFileControl.FirstFile) == false) {
				FormHelper.ErrorBox (Messages.SourceFileNotFound);
				return;
			}
			if (sender == _ImportOcrResultButton) {
				if (FileHelper.IsPathValid (this._TargetFileControl.Text) == false) {
					FormHelper.ErrorBox (Messages.TargetFileNameInvalid);
					return;
				}
				if (_BookmarkControl.Text.Length == 0) {
					FormHelper.ErrorBox ("请指定识别结果文件。");
					return;
				}
			}
			//else if (String.IsNullOrEmpty (_BookmarkControl.Text)) {
			//    Common.Form.ErrorBox (Messages.InfoDocNotSpecified);
			//    return;
			//}

			AppContext.SourceFiles = _SourceFileControl.Files;
			AppContext.BookmarkFile = _BookmarkControl.Text;
			AppContext.TargetFile = _TargetFileControl.Text;
			if (_SourceFileControl.Files.Length == 1) {
				_SourceFileControl.FileList.AddHistoryItem ();
				if (_BookmarkControl.Text.Length > 0) {
					_BookmarkControl.FileList.AddHistoryItem ();
				}
			}
			if (sender == _ImportOcrResultButton) {
				_TargetFileControl.FileList.AddHistoryItem ();
			}

			AppContext.MainForm.ResetWorker ();

			SyncOptions ();

			var worker = AppContext.MainForm.GetWorker ();
			if (sender != _ImportOcrResultButton) {
				worker.DoWork += new DoWorkEventHandler (OcrExport);
				worker.RunWorkerAsync (new object[] {
					AppContext.SourceFiles,
					AppContext.BookmarkFile,
					_options
				});
			}
			else {
				worker.DoWork += new DoWorkEventHandler (ImportOcr);
				worker.RunWorkerAsync (new object[] {
					AppContext.SourceFiles,
					AppContext.BookmarkFile,
					AppContext.TargetFile
				});
			}
		}

		private void SyncOptions () {
			_options.CompressWhiteSpaces = _CompressWhiteSpaceBox.Checked;
			_options.PreserveColor = !_ConvertToMonoColorBox.Checked;
			_options.DetectColumns = _DetectColumnsBox.Checked;
			_options.DetectContentPunctuations = _DetectContentPunctuationsBox.Checked;
			_options.PageRanges = _PageRangeBox.Text;
			_options.OcrLangID = (int)ValueHelper.MapValue (_OcrLangBox.Text, Constants.Ocr.LangNames, Constants.Ocr.LangIDs, -1);
			_options.OrientPage = _OrientBox.Checked;
			_options.OutputOriginalOcrResult = _OutputOriginalOcrResultBox.Checked;
			_options.QuantitativeFactor = (float)_QuantitiveFactorBox.Value;
			_options.PrintOcrResult = _PrintOcrResultBox.Checked;
			_options.RemoveWhiteSpacesBetweenChineseCharacters = _RemoveSpaceBetweenChineseBox.Checked;
			_options.StretchPage = _StretchBox.Checked;
			// _options.SaveOcredImagePath = String.IsNullOrEmpty (this._BookmarkControl.Text) ? null : Common.FileHelper.CombinePath (Path.GetDirectoryName (this._BookmarkControl.Text), Path.GetFileNameWithoutExtension (_BookmarkControl.Text) + Constants.FileExtensions.Tif);
			_options.WritingDirection = (WritingDirection)_WritingDirectionBox.SelectedIndex;
		}

		void OcrExport (object sender, DoWorkEventArgs e) {
			var a = e.Argument as object[];
			var files = a[0] as string[];
			var b = a[1] as string;
			var options = a[2] as OcrOptions;
			if (files.Length > 1) {
				var p = Path.GetDirectoryName (b);
				var ext = Path.GetExtension (b);
				foreach (var file in files) {
					Processor.Worker.Ocr (file, FileHelper.CombinePath (p, Path.GetFileNameWithoutExtension (file) + ext), options);
					if (AppContext.Abort) {
						return;
					}
				}
			}
			else {
				Processor.Worker.Ocr (files[0], b, options);
			}
		}

		void ImportOcr (object sender, DoWorkEventArgs e) {
			var a = e.Argument as object[];
			var files = a[0] as string[];
			var b = a[1] as string;
			var target = a[2] as string;
			if (files.Length > 1) {
				var p = Path.GetDirectoryName (b);
				var ext = Path.GetExtension (b);
				foreach (var file in files) {
					Processor.Worker.ImportOcr (file, FileHelper.CombinePath (p, Path.GetFileNameWithoutExtension (file) + ext), target);
					if (AppContext.Abort) {
						return;
					}
				}
			}
			else {
				Processor.Worker.ImportOcr (files[0], b, target);
			}
		}

		private void _ImportLink_LinkClicked (object sender, LinkLabelLinkClickedEventArgs e) {
			AppContext.MainForm.SelectFunctionList (Function.Patcher);
		}

		private void ControlEvent (object sender, EventArgs e) {
			if (sender == _WritingDirectionBox) {
				_DetectColumnsBox.Enabled = _WritingDirectionBox.SelectedIndex != 0;
			}
			else if (sender == _OutputOriginalOcrResultBox) {
				_DetectColumnsBox.Enabled
					= _DetectContentPunctuationsBox.Enabled
					= _CompressWhiteSpaceBox.Enabled
					= _RemoveSpaceBetweenChineseBox.Enabled
					= !_OutputOriginalOcrResultBox.Checked;
			}
		}

	}
}
