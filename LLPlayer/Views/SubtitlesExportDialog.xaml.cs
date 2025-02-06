﻿using System.Windows;
using System.Windows.Controls;
using LLPlayer.ViewModels;

namespace LLPlayer.Views;

public partial class SubtitlesExportDialog : UserControl
{
    public SubtitlesExportDialog()
    {
        InitializeComponent();

        DataContext = ((App)Application.Current).Container.Resolve<SubtitlesExportDialogVM>();
    }
}
