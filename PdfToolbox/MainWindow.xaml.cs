using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace PdfToolbox;

public partial class MainWindow : Window
{
    private readonly PdfWorkspace _workspace = new();
    private int _currentPageIndex;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "PDF files (*.pdf)|*.pdf" };
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FileName))
        {
            OpenFile(dialog.FileName);
        }
    }

    private void Window_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        var pdf = files.FirstOrDefault(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
        if (pdf != null)
        {
            OpenFile(pdf);
        }
    }

    private void OpenFile(string path)
    {
        try
        {
            _workspace.Load(path);
            _currentPageIndex = 0;
            FileNameText.Text = _workspace.FileName;
            MergeCurrentFileText.Text = _workspace.FileName;
            SaveButton.IsEnabled = true;
            UpdatePreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"PDFを開けませんでした: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "PDF files (*.pdf)|*.pdf", FileName = _workspace.FileName };
        if (dialog.ShowDialog() == true)
        {
            var outputPath = dialog.FileName;
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return;
            }

            try
            {
                _workspace.SaveAs(outputPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OperationRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (RotatePanel == null)
        {
            return;
        }

        RotatePanel.Visibility = RotateRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        SplitCenterPanel.Visibility = SplitCenterRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        SplitPanel.Visibility = SplitRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        MergePanel.Visibility = MergeRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PngPanel.Visibility = PngRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PrevPageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPageIndex > 0)
        {
            _currentPageIndex--;
            UpdatePreview();
        }
    }

    private void NextPageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPageIndex < _workspace.PageCount - 1)
        {
            _currentPageIndex++;
            UpdatePreview();
        }
    }

    private void UpdatePreview()
    {
        if (_workspace.PageCount == 0)
        {
            PreviewImage.Source = null;
            PageIndicatorText.Text = "0 / 0";
            return;
        }

        PreviewImage.Source = _workspace.RenderPage(_currentPageIndex);
        PageIndicatorText.Text = $"{_currentPageIndex + 1} / {_workspace.PageCount}";
    }

    private List<int>? ParseRangeOrShowError(string text)
    {
        try
        {
            return PageRangeParser.Parse(text, _workspace.PageCount);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
    }

    private bool RequireDocument()
    {
        if (_workspace.PageCount == 0)
        {
            MessageBox.Show("PDFを開いてください", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    private void RotateExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireDocument())
        {
            return;
        }

        var indices = ParseRangeOrShowError(RotateRangeText.Text);
        if (indices == null)
        {
            return;
        }

        var degreeItem = (ComboBoxItem)RotateDegreeCombo.SelectedItem;
        var degrees = int.Parse((string)degreeItem.Content);

        _workspace.Rotate(indices, degrees);
        UpdatePreview();
    }

    private void SplitCenterExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireDocument())
        {
            return;
        }

        var indices = ParseRangeOrShowError(SplitCenterRangeText.Text);
        if (indices == null)
        {
            return;
        }

        _workspace.SplitCenter(indices);
        _currentPageIndex = 0;
        UpdatePreview();
    }

    private void SplitExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireDocument())
        {
            return;
        }

        var groups = SplitRangeText.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (groups.Length == 0)
        {
            MessageBox.Show("分割範囲を指定してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var ranges = new List<(int Start, int End)>();
        try
        {
            foreach (var group in groups)
            {
                var indices = PageRangeParser.Parse(group, _workspace.PageCount);
                if (indices.Count == 0)
                {
                    throw new FormatException($"範囲が不正です: {group}");
                }
                ranges.Add((indices.Min(), indices.Max()));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        using var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
        if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        var documents = _workspace.SplitByRanges(ranges);
        var baseName = Path.GetFileNameWithoutExtension(_workspace.FileName ?? "split");
        for (var i = 0; i < documents.Count; i++)
        {
            var path = Path.Combine(folderDialog.SelectedPath, $"{baseName}_{i + 1}.pdf");
            documents[i].Save(path);
        }

        MessageBox.Show("分割が完了しました", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void MergeAddButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "PDF files (*.pdf)|*.pdf", Multiselect = true };
        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                MergeFileListBox.Items.Add(file);
            }
        }
    }

    private void MergeExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireDocument())
        {
            return;
        }

        foreach (var item in MergeFileListBox.Items)
        {
            _workspace.MergeAppend((string)item);
        }
        MergeFileListBox.Items.Clear();
        UpdatePreview();
    }

    private void PngExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (!RequireDocument())
        {
            return;
        }

        var indices = ParseRangeOrShowError(PngRangeText.Text);
        if (indices == null)
        {
            return;
        }

        using var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
        if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        _workspace.ExportPng(indices, folderDialog.SelectedPath);
        MessageBox.Show("PNG変換が完了しました", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
