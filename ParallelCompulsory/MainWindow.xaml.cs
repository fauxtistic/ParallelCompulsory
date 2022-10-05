using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Schema;

namespace ParallelCompulsory
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cts;
        private int currentIndex = 0;
        private List<long> primes = new List<long>();
        private int maxItemsPerPage = 20; // maybe make this modifiable by user

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonGeneratePrimesSequential_Click(object sender, RoutedEventArgs e)
        {
            GeneratePrimes(true);                      
        }

        private void ButtonGeneratePrimesParallel_Click(object sender, RoutedEventArgs e)
        {
            GeneratePrimes(false);
        }

        private async void GeneratePrimes(bool sequential)
        {
            long from;
            long to;

            var errors = ValidateInputAndGetErrors(txtFrom.Text, txtTo.Text, out from, out to);

            if (errors.Count > 0)
            {
                ShowAlert(errors);
            }
            else
            {
                var genType = sequential ? "sequentially" : "in parallel";
                Clear(genType);
                UpdateListBox();
                ReverseControlEnabling();
                var gen = new PrimeGenerator();

                cts = new CancellationTokenSource();
                var token = cts.Token;
                string? genText = null;

                var sw = Stopwatch.StartNew();
                try
                {
                    primes = sequential 
                        ? await Task.Run(() => gen.GetPrimesSequential(from, to, token), token) 
                        : await Task.Run(() => gen.GetPrimesParallel(from, to, token), token);
                }
                catch (OperationCanceledException oe)
                {
                    genText = "Generation of primes cancelled after";
                }
                finally
                {
                    cts.Dispose();
                }
                sw.Stop();
                var ms = sw.ElapsedMilliseconds;

                UpdateListBox();

                genText ??= $"Generated {primes.Count} primes {genType} in";
                var secText = ms < 1000 ? $"{ms} milliseconds" : $"{Math.Round(ms / 1000.0, 3)} seconds";
                lblStatus.Content = $"{genText} {secText}";
                ReverseControlEnabling();
            }
        }

        private void Clear(string genType)
        {
            primes = new List<long>();
            currentIndex = 0;            
            lblStatus.Content = $"Generating primes {genType}...";
            lblTest.Content = "";
        }

        private void ReverseControlEnabling()
        {   
            btnGenerateSeq.IsEnabled = !btnGenerateSeq.IsEnabled;
            btnGeneratePar.IsEnabled = !btnGeneratePar.IsEnabled;
            btnStop.IsEnabled = !btnStop.IsEnabled;
            txtFrom.IsEnabled = !txtFrom.IsEnabled;
            txtTo.IsEnabled = !txtTo.IsEnabled;
        }

        private List<string> ValidateInputAndGetErrors(string firstInput, string secondInput, out long from, out long to)
        {
            var errors = new List<string>();

            var txtFromIsLong = long.TryParse(firstInput, out from);
            var txtToIsLong = long.TryParse(secondInput, out to);

            if (txtFromIsLong && txtToIsLong)
            {
                if (from < 0 || to < 0)
                {
                    errors.Add("Numbers cannot be negative!");
                }

                if (to < from)
                {
                    errors.Add("Highest number cannot be lower than lowest number!");
                }
            }
            else
            {
                errors.Add("You must enter a number in both boxes!");
            }

            return errors;
        }

        private void ShowAlert(List<string> errors)
        {
            var okBtn = MessageBoxButton.OK;
            var icon = MessageBoxImage.Error;
            var caption = "Error!";

            string alertText = "";
            foreach (var error in errors)
            {
                alertText += error + "\n";
            }
            alertText.Remove(alertText.Length - 1);

            MessageBox.Show(alertText, caption, okBtn, icon);
        }

        private void ButtonStopGeneration_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
        }

        // This is just to demonstrate GUI is still functional while prime generation takes place
        private void ButtonTestGUI(object sender, RoutedEventArgs e)
        {
            lblTest.Content = "GUI is not frozen!";
        }

        private void Paginate(object sender, RoutedEventArgs e)
        {
            string moveTo = (string)((Button)sender).Tag;
            switch (moveTo)
            {
                case "FIRST":
                    currentIndex = 0;
                    break;
                case "PREV":
                    currentIndex -= maxItemsPerPage;
                    break;
                case "NEXT":
                    currentIndex += maxItemsPerPage;
                    break;
                case "LAST":
                    currentIndex = primes.Count - primes.Count % maxItemsPerPage;
                    break;
                default:
                    break;
            }
            UpdateListBox();
        }

        private void UpdateListBox()
        {
            lstPrimes.Items.Clear();

            if (primes.Count == 0)
            {
                btnFirst.IsEnabled = false;
                btnPrev.IsEnabled = false;
                btnNext.IsEnabled = false;
                btnLast.IsEnabled = false;
                lblPrimes.Content = "";
                return;
            }

            var enableBack = primes.Count > maxItemsPerPage && currentIndex >= maxItemsPerPage;
            var enableForward = primes.Count > maxItemsPerPage && currentIndex + maxItemsPerPage < primes.Count;

            btnFirst.IsEnabled = enableBack;
            btnPrev.IsEnabled = enableBack;
            btnNext.IsEnabled = enableForward;
            btnLast.IsEnabled = enableForward;

            var currentItemsPerPage = maxItemsPerPage;

            if ((primes.Count - currentIndex) < maxItemsPerPage)
            {
                currentItemsPerPage = primes.Count % maxItemsPerPage;
            }

            foreach (var prime in primes.GetRange(currentIndex, currentItemsPerPage))
            {
                lstPrimes.Items.Add(prime);
            }

            lblPrimes.Content = $"Showing {currentIndex + 1}-{currentIndex + currentItemsPerPage}" +
                $" of {primes.Count} found primes:";
        }
    }
}
