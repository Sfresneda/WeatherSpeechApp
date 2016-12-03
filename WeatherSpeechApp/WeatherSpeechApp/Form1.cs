﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading;

using System.Net;
using System.Xml;




namespace WeatherSpeechApp
{
    public partial class Form1 : Form
    {
        private System.Speech.Recognition.SpeechRecognitionEngine _recognizer = new SpeechRecognitionEngine();
        private SpeechSynthesizer synth = new SpeechSynthesizer();

        private string kAPIURL = "http://api.openweathermap.org/data/2.5/weather?q=@LOC@&mode=xml&units=metric&APPID=";
        private string kAPIToken = "53f9a225b46f9d878174d6eeabbec715";

        Label progressLabel = new Label();

        Label dateLabel = new Label();
        Label actualDegreesLabel = new Label();
        Label maxDegreesLabel = new Label();
        Label minDegreesLabel = new Label();
        Label weatherSubtitleLabel = new Label();
        PictureBox backgroundCity = new PictureBox();

        List<Location> locationsData = new List<Location>();

        String[] progressTextArray = new string[] {"Getting Weather Data...", "Parsing Data...", "Creating UI..." };
        String[] weatherLocations = new String[] { "Madrid,ES", "Barcelona,ES", "Valencia,ES" };

        public Form1()
        {
            InitializeComponent();
        }
        private void GoFullscreen(bool fullscreen)
        {
            if (fullscreen)
            {
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                this.Bounds = Screen.PrimaryScreen.Bounds;
            }
            else
            {
                this.WindowState = FormWindowState.Maximized;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            }
        }

        //------------------
        // MARK: Setups
        //------------------
        public Label setupLabel(ContentAlignment AligmentPosition, AnchorStyles anchorPosition, DockStyle dockPosition)
        {
            Label labelToCustomice = new Label();
            Color myColor = Color.FromArgb(90, Color.White);
            labelToCustomice.BackColor = myColor;
            labelToCustomice.ForeColor = System.Drawing.Color.White;
            labelToCustomice.Anchor = anchorPosition;
            labelToCustomice.Font = new Font(progressLabel.Font.FontFamily, 32);
            labelToCustomice.AutoSize = true;
            labelToCustomice.TextAlign = AligmentPosition;
            labelToCustomice.Dock = dockPosition;
            return labelToCustomice;
        }
        private void addLabelToView(Label newLabel)
        {
            this.Controls.Add(newLabel);
        }
        private void setupPictureBox()
        {
            this.backgroundCity.Dock = DockStyle.None;
            this.backgroundCity.SizeMode = PictureBoxSizeMode.AutoSize;
            this.backgroundCity.BackColor = Color.Transparent;
            this.Controls.Add(backgroundCity);
        }
        private void setupProgressBar(object o)
        {
            BackgroundWorker b = o as BackgroundWorker;
            foreach (string city in weatherLocations)
            {
                string url = getAPIUrl().Replace("@LOC@", city);
                GetFormattedXml(url);
                Thread.Sleep(500);
                b.ReportProgress(this.locationsData.Count);
            }
        }

        //------------------
        // MARK: Loading View
        //------------------
        private void showLoadScreen()
        {
            //new thread for progressBar
            progressBar.Maximum = 3;
            progressBar.Show();
            progressLabel.Show();
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(
            delegate (object o, DoWorkEventArgs args)
            {
                setupProgressBar(o);
            });

            // what to do when progress changed
            int j = 0;
            bw.ProgressChanged += new ProgressChangedEventHandler(
            delegate (object o, ProgressChangedEventArgs args)
            {
                progressBar.Value = args.ProgressPercentage;

                progressLabel.Text = progressTextArray[j];
                j++;
            });

            // what to do when worker completes its task (notify the user)
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
            delegate (object o, RunWorkerCompletedEventArgs args)
            {
                progressBar.Hide();
                progressLabel.Hide();
                addLabelToView(this.weatherSubtitleLabel = setupLabel(ContentAlignment.BottomCenter, AnchorStyles.Bottom, DockStyle.Bottom));
                addLabelToView(this.dateLabel = setupLabel(ContentAlignment.BottomLeft, AnchorStyles.Left, DockStyle.Bottom));
                addLabelToView(this.actualDegreesLabel = setupLabel(ContentAlignment.TopRight, AnchorStyles.Right, DockStyle.Top));
                addLabelToView(this.maxDegreesLabel = setupLabel(ContentAlignment.TopLeft, AnchorStyles.Left, DockStyle.Top));
                addLabelToView(this.minDegreesLabel = setupLabel(ContentAlignment.MiddleLeft, AnchorStyles.None, DockStyle.Left));
                setupPictureBox();

                initGrammar();
            });

            bw.RunWorkerAsync();
        }

        //------------------
        // MARK: Speech Reconizer
        //------------------
        private void initGrammar()
        {
            Grammar grammar = CreateGrammarBuilderSemantics(null);
            _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.UnloadAllGrammars();
            grammar.Enabled = true;
            _recognizer.LoadGrammar(grammar);
            _recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(_recognizer_SpeechRecognized);

            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }
        void _recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            SemanticValue semantics = e.Result.Semantics;
            string rawText = e.Result.Text;
            RecognitionResult result = e.Result;

            if (!semantics.ContainsKey("locations"))
            {
                this.weatherSubtitleLabel.Text = "No info provided.";
            }
            else
            {
                char[] delimiterChars = { ' ' };
                string[] words = rawText.Split(delimiterChars);
                int position = -1;
                string city = words[words.Length - 1];
                switch (city)
                {
                    case "Madrid":
                        this.backgroundCity.Image = Properties.Resources.m_sun1;
                        position = 0;
                        break;
                    case "Barcelona":
                        this.backgroundCity.Image = Properties.Resources.b_sun1;
                        position = 1;
                        break;
                    case "Valencia":
                        this.backgroundCity.Image = Properties.Resources.v_sun1;
                        position = 2;
                        break;
                    default:
                        break;
                }
                rawText = "La temperatura en " + city + " para hoy se mantendrá entre los " + this.locationsData[position].MaxTemp + " y " + this.locationsData[position].MinTemp + " grados.";


                this.dateLabel.Text = "Fecha: " + DateTime.Now.ToString("d/M/yyyy");
                this.actualDegreesLabel.Text = "Actual: " + this.locationsData[position].ActualTemp+"º";
                this.maxDegreesLabel.Text = "Max: " + this.locationsData[position].MaxTemp+"º";
                this.minDegreesLabel.Text = "Min: " + this.locationsData[position].MinTemp+"º";
                this.weatherSubtitleLabel.Text = rawText;

                Update();
                synth.Speak(rawText);
            }
        }
        private Grammar CreateGrammarBuilderSemantics(params int[] info)
        {
            Choices locationsChoice = new Choices();

            SemanticResultValue choiceResultValue =
                               new SemanticResultValue("Madrid", weatherLocations[0]);
            GrammarBuilder resultValueBuilder = new GrammarBuilder(choiceResultValue);
            locationsChoice.Add(resultValueBuilder);

            choiceResultValue =
                   new SemanticResultValue("Barcelona", weatherLocations[1]);
            resultValueBuilder = new GrammarBuilder(choiceResultValue);
            locationsChoice.Add(resultValueBuilder);

            choiceResultValue =
                   new SemanticResultValue("Valencia", weatherLocations[2]);
            resultValueBuilder = new GrammarBuilder(choiceResultValue);
            locationsChoice.Add(resultValueBuilder);


            SemanticResultKey choiceResultKey = new SemanticResultKey("locations", locationsChoice);
            GrammarBuilder locations = new GrammarBuilder(choiceResultKey);

            GrammarBuilder sunnyDayIn = "Hace sol en";
            GrammarBuilder rainyDay = "Está lloviendo en";
            GrammarBuilder sayMeTheweatherIn = "Que tiempo hace en";

            Choices commands = new Choices(sunnyDayIn, rainyDay, sayMeTheweatherIn);
            GrammarBuilder frase = new GrammarBuilder(commands);
            frase.Append(locations);
            Grammar grammar = new Grammar(frase);
            grammar.Name = "Tiempo en una localización";
            return grammar;

        }

        //------------------
        // MARK: Main
        //------------------
        private void Form1_Load_1(object sender, EventArgs e)
        {
            GoFullscreen(true);
            // Compose the query URL.
            addLabelToView(this.progressLabel = setupLabel(ContentAlignment.BottomCenter, AnchorStyles.Bottom, DockStyle.Bottom));
            showLoadScreen();
        }

        //------------------
        // MARK: API
        //------------------
        private string getAPIUrl()
        {
            Console.WriteLine(kAPIURL + kAPIToken);

            return kAPIURL + kAPIToken;
        }

        //------------------
        // MARK: PARSER
        //------------------
        // Return the XML result of the URL.
        private void GetFormattedXml(string url)
        {
            XmlTextReader reader = new XmlTextReader(url);
            string city = "";
            string actualTemp = "";
            string maxTemp = "";
            string minTemp = "";
            string status = "";

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: // El nodo es un elemento.
                        switch (reader.Name)
                        {
                            case "city":
                                while (reader.MoveToNextAttribute())
                                {
                                    if (reader.Name == "name")
                                    {
                                        city = reader.Value;
                                    }

                                }
                                break;
                            case "temperature":
                                while (reader.MoveToNextAttribute())
                                {
                                    if (reader.Name == "value")
                                    {
                                        actualTemp = reader.Value;
                                    }
                                    if (reader.Name == "min")
                                    {
                                        minTemp = reader.Value;
                                    }
                                    if (reader.Name == "max")
                                    {
                                        maxTemp = reader.Value;
                                    }
                                }
                                break;
                            case "weather":
                                while (reader.MoveToNextAttribute())
                                {
                                    if (reader.Name == "value")
                                    {
                                        status = reader.Value;
                                    }
                                }
                                break;
                            case "lastupdate":
                                this.locationsData.Add(new Location(city, actualTemp, maxTemp, minTemp, status));
                                break;
                            default:
                                break;
                        }

                        break;

                    case XmlNodeType.Text: //Muestra el texto en cada elemento. 
                        Console.WriteLine(reader.Value);
                        break;

                    case XmlNodeType.EndElement: //Muestra el final del elemento.
                        Console.Write("</" + reader.Name);
                        Console.WriteLine(">");
                        break;
                }
            }
        }
    }

    //------------------
    // MARK: Object Location
    //------------------
    public class Location
    {
        public string Name { get; set; }
        public string ActualTemp { get; set; }
        public string MaxTemp { get; set; }
        public string MinTemp { get; set; }
        public string Status { get; set; }

        public Location(string name, string actualTemperature, string maxTemperature, string minTemperature, string status)
        {
            Name = name;
            ActualTemp = actualTemperature;
            MaxTemp = maxTemperature;
            MinTemp = minTemperature;
            Status = status;
        }
    }
}
