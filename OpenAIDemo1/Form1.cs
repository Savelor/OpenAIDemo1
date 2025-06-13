using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenAI.Chat;
using Azure.Core;
using Azure.AI.OpenAI;
using Azure;
using System.IO;
using System.Data.SqlClient;
using System.Xml;

namespace OpenAIDemo1
{
    public partial class Form1 : Form
    {
        private SqlConnection sqlConnection; // Connessione riutilizzabile
        public static string SQLDataTypes = "";
        public static string ExecutionPlan = "";
        public static string ActualExecutionPlan = "";
        public static string EstimatedExecutionPlan = "";
        public static string IndexesList = "";
        public static string model = "";


        public Form1()
        {
            InitializeComponent();

            this.Load += Form1_Load;


            //------inizializziamo la grafica
            this.Size = new Size(1740, 1110); // Allarga il form
            this.StartPosition = FormStartPosition.CenterScreen; // Centra all'avvio

            // Set RichTextBox1 (on the left)
            richTextBox1.Location = new Point(10, 70);
            richTextBox1.Size = new Size(880, 1010);
            richTextBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            // Set webView21 (on the right)
            webView21.Location = new Point(895, 70);
            webView21.Size = new Size(820, 900);
            webView21.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            button1.Location = new Point(1580, 1010);
            button1.Size = new Size(80, 40);
            button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            button1.BringToFront();

            this.MinimizeBox = true;       // Abilita il pulsante di minimizzazione
            this.MaximizeBox = true;       // (Facoltativo) Abilita il pulsante di massimizzazione

            this.textBox4.Text = "localhost\\sqldashboard";
            this.textBox5.Text = "AdventureWorks2019";
        }


        //===============================Funzione separata che invia il messaggio e ottiene la risposta
        private async Task<string> GetChatResponseSavelor(string chatContext,
                                                            string SQLQueryToOptimize,
                                                            string Training1,
                                                            string Training2,
                                                            string Training3)
        {
            var endpoint = new Uri("https://aisavelor.openai.azure.com/");
            var apiKey = "d601113d574940538109ee59dde7527e";
            var deploymentName = "";
            ChatCompletionOptions requestOptions = null;

            //------inizializzazione per 4o
            deploymentName = "gpt-4o";
            requestOptions = new ChatCompletionOptions()
            {
                MaxOutputTokenCount = 8192,
                Temperature = 0.4f,
                TopP = 1.0f
            };

            ////inizializziamo il client o3 - mini
            //model = "o3-mini";
            //deploymentName = "o3-mini";
            //requestOptions = new ChatCompletionOptions()
            //{
            //    MaxOutputTokenCount = 100000
            //};
            //requestOptions.SetNewMaxCompletionTokensPropertyEnabled(true);
            

            //valido per tutti i modelli
            var azureClient = new AzureOpenAIClient(endpoint, new AzureKeyCredential(apiKey));
            var chatClient = azureClient.GetChatClient(deploymentName);

            // Crea i messaggi per la chat
            List<ChatMessage> messages = new List<ChatMessage>()
            {
                new SystemChatMessage(chatContext),
                new UserChatMessage(SQLQueryToOptimize),
                new UserChatMessage(Training1),
                new UserChatMessage(Training2),
                new UserChatMessage(Training3)
            };

            // Ottieni la risposta dall'assistente
            var response = await chatClient.CompleteChatAsync(messages, requestOptions);
            string risposta = response.Value.Content[0].Text;
            risposta = risposta.Replace("\n", "\r\n");
            return risposta;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            Color inactiveBorder = SystemColors.InactiveBorder;
            string coloreHtml = $"rgb({inactiveBorder.R}, {inactiveBorder.G}, {inactiveBorder.B})";

            // Assicurati che WebView2 sia inizializzato
            await webView21.EnsureCoreWebView2Async(null);
            string html = $@"
                    <html>
                    <head>
                        <style>
                            body {{
                                font-family: Arial;
                                margin: 0;
                                padding: 10px;
                                height: 100%;
                                overflow-y: scroll;
                                font-size: 16px;
                                background-color: {coloreHtml};
                            }}
                            #content {{
                                display: flex;
                                flex-direction: column;
                            }}
                        </style>
                    </head>
                    <body>
                        <div id='content' style='margin-top:850px; font-size: 12px;'>
                            <div style='font-size:24px'>Welcome to SQL assistant!</div>
                        </div>
                        <script>
                            document.body.style.zoom = '70%';
                            document.body.style.backgroundColor = '{coloreHtml}';
                        </script>
                    </body>
                    </html>";

            // Carica il contenuto iniziale
            webView21.CoreWebView2.NavigateToString(html);
        }

        private async void button1_Click(object sender, EventArgs e)
        {

            string chatContext = "You are a SQL Server developer. Analyze and Rewrite the T-SQL batch that you have in input " +
                "applying the best practice rules that are given to you. Then add your observations. All the content returned must be in HTML.";

            //Load Query to Optimize
            string SQLQueryToOptimize = "This is the query to analyze: " + Environment.NewLine + richTextBox1.Text + Environment.NewLine;  //Query to optimize

            //Define the refactoring rules
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Training.txt");
            string Training1 = File.ReadAllText(filePath);

            //Load columns data types
            string Training2 = "This is the list of all tables, columns and data types in JSON format:" + Environment.NewLine + SQLDataTypes + Environment.NewLine; 

            string Training3 = "All the content returned must be in HTML. Put the optimized code on an Azure backgroud";

            string risposta = await GetChatResponseSavelor(chatContext, SQLQueryToOptimize, Training1, Training2, Training3);

            risposta = risposta.Replace("```html", "");
            risposta = risposta.Replace("```", "");
            string nuovoHtml = "<div>" + risposta + "</div>";
            AggiungiContenutoHtml(nuovoHtml);
        }  


        private void AggiungiContenutoHtml(string nuovoHtml)
        {
            Color inactiveBorder = SystemColors.InactiveBorder;
            string coloreHtml = $"rgb({inactiveBorder.R}, {inactiveBorder.G}, {inactiveBorder.B})";

            string script = $@"
            document.body.style.backgroundColor = '{coloreHtml}';
            var contentDiv = document.getElementById('content');
            var nuovoDiv = document.createElement('div');
            nuovoDiv.innerHTML = `" + nuovoHtml.Replace("`", "\\`") + @"`;
            contentDiv.appendChild(nuovoDiv);
            document.body.style.zoom = '70%';

            function smoothScrollToBottom(duration = 3000) {
                const start = window.scrollY;
                const end = document.body.scrollHeight;
                const distance = end - start;
                const startTime = performance.now();

                function scrollStep(currentTime) {
                    const elapsed = currentTime - startTime;
                    const progress = Math.min(elapsed / duration, 1);
                    window.scrollTo(0, start + distance * easeInOutQuad(progress));
                    if (progress < 1) {
                        requestAnimationFrame(scrollStep);
                    }
                }

                function easeInOutQuad(t) {
                    return t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
                }

                requestAnimationFrame(scrollStep);
            }

            smoothScrollToBottom(3000); // ← cambia qui la durata in ms per più lentezza";

            webView21.CoreWebView2.ExecuteScriptAsync(script);
        }


        private void button2_Click(object sender, EventArgs e)
        {
            string serverName = textBox4.Text.Trim();
            string databaseName = textBox5.Text.Trim();

            if (string.IsNullOrEmpty(serverName) || string.IsNullOrEmpty(databaseName))
            {
                MessageBox.Show("Insert SQL Server name or Database Name");
                return;
            }

            string connectionString = $"Server={serverName};Database={databaseName};Integrated Security=true;";
            sqlConnection = new SqlConnection(connectionString);
            try
            {
                sqlConnection.Open();
                button2.ForeColor = Color.Green;
                button2.Text = "Connected";
                GetDataTypesInfo();  // Chiama qui la nuova funzione
                sqlConnection.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection Error: " + ex.Message);
            }
        }


        public void GetDataTypesInfo()  //---Get all columns data types
        {
            try
            {
                string query = @" SELECT
                                    SCHEMA_NAME(t.schema_id) AS [Schema],
                                    t.name AS [Table],
                                    c.name AS [Column],
                                    CASE 
                                        WHEN ty.name IN ('char', 'varchar', 'nchar', 'nvarchar', 'binary', 'varbinary') THEN 
                                            ty.name + 
                                            CASE 
                                                WHEN c.max_length = -1 THEN '(MAX)'
                                                ELSE '(' + CAST(c.max_length / 
                                                    CASE 
                                                        WHEN ty.name IN ('nchar', 'nvarchar') THEN 2 
                                                        ELSE 1 
                                                    END AS VARCHAR) + ')'
                                            END
                                        WHEN ty.name IN ('decimal', 'numeric') THEN 
                                            ty.name + '(' + CAST(c.precision AS VARCHAR) + ',' + CAST(c.scale AS VARCHAR) + ')'
                                        ELSE ty.name
                                    END AS DataType
                                FROM 
                                    sys.tables t
                                    INNER JOIN sys.columns c ON t.object_id = c.object_id
                                    INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
                                ORDER BY 
                                    t.name, c.column_id
                                FOR JSON PATH ";

                SqlCommand command = new SqlCommand(query, sqlConnection);
                SqlDataReader reader = command.ExecuteReader();

                string jsonResult = string.Empty;
                if (reader.Read())
                {
                    jsonResult = reader.GetString(0); // Get JSON string from first column
                }

                reader.Close();
                SQLDataTypes = jsonResult; // Save in global variable
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore durante l'esecuzione della query: " + ex.Message);
            }
        }

    }
}
