using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using HAP = HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Forms = System.Windows.Forms;

namespace CherubplayChatExporter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static string conversation = null;
        static int totalPages = 1;
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Export_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;

            // reset outputs
            // TODO: Reactive Extensions
            setProgressOutput("Processing. Please wait...");
            setFinalOutput("");

            try
            {
                // get url and filename from text boxes
                string url = Url.Text;
                string filepath = Filename.Text;

                Export(url, filepath);

                // reset conversation variable
                conversation = null;
            }
            catch (Exception ex)
            {
                setFinalOutput("Error: " + ex.Message);
            }
            
            this.Cursor = Cursors.Arrow;
        }

        private void Browse_Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Forms.FolderBrowserDialog();
            Forms.DialogResult result = dialog.ShowDialog();
            Filename.SetValue(TextBlock.TextProperty, dialog.SelectedPath);
        }

        public void Export(string url, string filepath)
        {
            try
            {
                string chatUrl = "https://www.cherubplay.co.uk/chats/" + url + @"/?page="; //a page in chat archive; missing page number

                // TODO: check url is valid; var url should be a GUID

                // initialise filepath
                string finalpath = null;
                if (String.IsNullOrEmpty(filepath))
                {
                    throw new Exception("No file path was selected!\nPlease select a file path before continuing.");
                }
                else
                {
                    finalpath = getFilePath(filepath);
                }

                // get 1st page html
                readFirstPage(chatUrl);

                if (conversation != null)
                {
                    bool isSuccess = true;
                    if (totalPages > 1)
                    {
                        isSuccess = readPages(chatUrl, totalPages);
                    }

                    // only write to file if all pages succeeded
                    if (isSuccess)
                    {
                        // replace unicode!
                        replaceUnicode();

                        // write string to file
                        writeToFile(finalpath);
                    }
                }
                else
                {
                    setFinalOutput("No chat was found at the given url.\nPlease check that the url is correct and try again.");
                }
            }
            catch (Exception e)
            {
                setFinalOutput("Error: " + e.Message);
            }
        }

        public void readFirstPage(string chatUrl)
        {
            HAP.HtmlDocument doc = getHtml(chatUrl, 1);

            // parse 1st page html
            var inputs = doc.DocumentNode.Descendants("p");
            // TODO: read <li> tag and extract message (<p>) and user symbol from this
            foreach (var input in inputs)
            {
                conversation = conversation + input.InnerText + "\n\n";
            }

            // get total number of pages in chat
            var pages = doc.DocumentNode.Descendants("a");
            foreach (var page in pages)
            {
                if (page.Attributes.Contains("class"))
                {
                    // totalPages is updated for each link to another page so it will end up as the last page number (the latest one)
                    totalPages = Convert.ToInt32(page.InnerText);
                }
            }
        }

        public bool readPages(string chatUrl, int totalPages)
        {
            bool isSuccess = true;

            // loop through each page
            for (int i = 2; i <= totalPages; i++) // start at 2 as we read the first page separately
            {
                try
                {
                    // read html
                    HAP.HtmlDocument doc = getHtml(chatUrl, i);

                    // parse html
                    var inputs = doc.DocumentNode.Descendants("p");
                    foreach (var input in inputs)
                    {
                        conversation = conversation + input.InnerText + "\n\n";
                    }

                    setProgressOutput(i + "/" + totalPages + " pages complete.");
                }
                catch (Exception e)
                {
                    isSuccess = false;
                    setFinalOutput("Error: " + e.Message);
                    break; // stop processing pages
                }
            }

            return isSuccess;
        }

        public HAP.HtmlDocument getHtml(string chatUrl, int i)
        {
            string thisUrl = chatUrl + i; // add page number to url
            WebRequest getReq = WebRequest.Create(thisUrl);
            WebResponse getResp = null;

            HAP.HtmlDocument doc = new HAP.HtmlDocument();

            getResp = getReq.GetResponse();

            Stream respStream = getResp.GetResponseStream();
            StreamReader reader = new StreamReader(respStream);
            string text = reader.ReadToEnd();

            // close web response when no longer needed
            getResp.Close();

            // read html
            doc.LoadHtml(text);

            return doc;
        }

        public string getFilePath(string filepath)
        {
            // check file path is valid before proceeding
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd H-mm-ss");
            filepath = filepath + @"\Chat_" + timestamp + ".doc";

            try
            {
                FileInfo fi = new FileInfo(filepath);
                return filepath;
            }
            catch (NotSupportedException e)
            {
                e = new NotSupportedException(e.Message + "\nThe filename entered contains unsupported characters.\nPlease remove characters such as ':' and try again.");
                throw e;
            }
        }

        public void writeToFile(string filepath)
        {
            try
            {
                System.IO.File.WriteAllText(filepath, conversation);
                setFinalOutput("Your chat has been exported to " + filepath);
            }
            catch (DirectoryNotFoundException e)
            {
                e = new DirectoryNotFoundException(e.Message + "\nThe filepath " + filepath + " could not be found.\nPlease ensure that you are not using characters such as '\\' and try again.");
                throw e;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void replaceUnicode()
        {
            conversation = conversation.Replace("&#34;", "\""); // quotation marks
            conversation = conversation.Replace("&#39;", "'"); // apostrophe
            conversation = conversation.Replace("&amp;", "&"); // ampersand
        }

        public void setProgressOutput(string message)
        {
            // TODO: apply updates so WPF is updated in realtime. Consider using threads maybe?
            try
            {
                Progress.SetValue(TextBlock.TextProperty, message);
            }
            catch (Exception)
            {

            }
        }

        public void setFinalOutput(string message)
        {
            try
            {
                Output.SetValue(TextBlock.TextProperty, message);
            }
            catch (Exception)
            {
                
            }
        }

        private void Button_MouseEnter_1(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        private void Button_MouseLeave_1(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }
    }
}
