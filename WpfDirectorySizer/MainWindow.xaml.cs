using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using PD.Core;
using WinForms = System.Windows.Forms;

namespace WpfDirectorySizer
{
    public partial class MainWindow : Window
    {
        // Use ObservableCollection to get change notifications for UI updates.
        private readonly ObservableCollection<DirectoryModel> directories =
            new ObservableCollection<DirectoryModel>();

        // TODO: Hard-coded dependency.  Convert to dependency injection using Ninject, etc.
        private readonly IDirectorySizeCalculator Calculator =
            new PfxDirectorySizeCalculator(ExceptionHandler);

        // If you like functional programming, you can also do it this way:
        // private readonly Action<Task<long>> calculate;

        // For thread safety
        private object locker = new object();

        // ctor
        public MainWindow()
        {
            InitializeComponent();
            DataContext = directories;
            fileList.ItemsSource = directories;
            LoadDefaultModels();
        }

        // Set up initial UI
        private void LoadDefaultModels()
        {
            directories.Add(new DirectoryModel(1, "<Select Directory>", 0));
            directories.Add(new DirectoryModel(2, "<Select Directory>", 0));
            directories.Add(new DirectoryModel(3, "<Select Directory>", 0));
            UpdateTotalSize();
        }

        
        // This triggers a background task to start calculating the directory size.
        // Use of await allows UI thread to continue processing messages from the 
        // message pump without freezing up on the user interface.
        private async Task UpdateModel(DirectoryModel model)
        {
            // Callback for progress notifications
            var progress = new Progress<long>(size =>
            {                
                 model.Size += size;
                 UpdateTotalSize();
            });

            // For cancelling background operations
            model.TokenSource = new CancellationTokenSource();

            // So that the UI thread won't block and freeze the Window
            model.Size = await Calculator.CalculateSizeAsync(
                model.Path, model.TokenSource.Token, progress
            );

            UpdateTotalSize();
        }

        // User has selected a new directory
        private async void OnChangeClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disable Select Button
                Toggle(sender as UIElement);

                var dialog = new WinForms.FolderBrowserDialog();
                var result = dialog.ShowDialog();
                var model = ((FrameworkElement)sender).DataContext as DirectoryModel;

                if (result != WinForms.DialogResult.OK)
                    return;

                var selectedPath = dialog.SelectedPath;

                if (AlreadySelected(selectedPath))
                {
                    MessageBox.Show(string.Format("Directory {0} has already been selected.", selectedPath), "Duplicate Selection", MessageBoxButton.OK);
                    return;
                }

                model.Path = dialog.SelectedPath;

                await UpdateModel(model);
            }
            finally
            {
                // Enable Select Button
                Toggle(sender as UIElement);
            }
        }

        // Cancel background operations
        private async void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            var model = ((FrameworkElement)sender).DataContext as DirectoryModel;

            if (model.TokenSource != null)
                model.TokenSource.Cancel();

            //Wait for cancellation request to propagate through background tasks.  
            //TODO:Research a better way to do this (without Task.Delay).
            await Task.Delay(300);
            model.Reset();
            UpdateTotalSize();
        }

        // Update the UI with the totals of all selected directories.
        private void UpdateTotalSize()
        {
            TotalValue.Content   = String.Format("{0:N}", directories.Sum(d => d.Size));
            TotalValueMB.Content = String.Format("{0:N2}", directories.Sum(d => d.SizeMB));
            TotalValueGB.Content = String.Format("{0:N2}", directories.Sum(d => d.SizeGB));
        }

        // Don't allow users to select the same directory twice.
        private bool AlreadySelected(string directoryPath)
        {
            return directories.Any(d => d.Path == directoryPath);
        }

        // This is used as the delegate implementation for the
        // PfxDirectorySizeCalculator implementation
        private static void ExceptionHandler(Exception ex)
        {
            if (ex is AggregateException)
            {
                ex = ex.InnerException;
            }

            // Swallow for now.
            // Would normally implement logging, etc. here based on global Exception policy.
            Console.WriteLine(ex.Message);

            //MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // Use for buttons, but will work for all UIElements
        private static void Toggle(UIElement element)
        {
            element.IsEnabled = !element.IsEnabled;
        }
    }
}
