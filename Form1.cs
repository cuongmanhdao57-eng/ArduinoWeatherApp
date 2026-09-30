using System;
using System.Globalization;
using System.IO.Ports;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private readonly HttpClient httpClient = new HttpClient();
        private SerialPort serialPort;

        // Controls
        private ComboBox cmbPort;
        private Button btnRead;
        private Label lblTemperature;
        private Label lblHumidity;
        private Label lblStatus;
        private Label lblTempValue;
        private Label lblHumValue;

        // Thay URL này bằng API của bạn
        private readonly string apiUrl = "https://your-api-url.com/api/weather";

        public Form1()
        {
            InitializeComponent();
            this.Text = "Form1";
            this.Width = 800;
            this.Height = 500;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);

            InitControls();
            LoadAvailablePorts();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }

        private void InitControls()
        {
            // Label 1: Nhiệt độ
            lblTemperature = new Label();
            lblTemperature.Text = "Nhiệt độ";
            lblTemperature.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            lblTemperature.AutoSize = true;
            lblTemperature.Location = new System.Drawing.Point(220, 120);

            // Label 2: Độ ẩm
            lblHumidity = new Label();
            lblHumidity.Text = "Độ ẩm";
            lblHumidity.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            lblHumidity.AutoSize = true;
            lblHumidity.Location = new System.Drawing.Point(430, 120);

            // Label 3: Trạng thái
            lblStatus = new Label();
            lblStatus.Text = "Trạng thái";
            lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            lblStatus.AutoSize = true;
            lblStatus.Location = new System.Drawing.Point(320, 250);

            // Giá trị nhiệt độ
            lblTempValue = new Label();
            lblTempValue.Text = "--";
            lblTempValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold);
            lblTempValue.AutoSize = true;
            lblTempValue.Location = new System.Drawing.Point(210, 170);

            // Giá trị độ ẩm
            lblHumValue = new Label();
            lblHumValue.Text = "--";
            lblHumValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold);
            lblHumValue.AutoSize = true;
            lblHumValue.Location = new System.Drawing.Point(420, 170);

            // ComboBox cổng COM
            cmbPort = new ComboBox();
            cmbPort.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPort.Width = 180;
            cmbPort.Height = 30;
            cmbPort.Location = new System.Drawing.Point(260, 80);

            // Button
            btnRead = new Button();
            btnRead.Text = "Button1";
            btnRead.Width = 140;
            btnRead.Height = 40;
            btnRead.Location = new System.Drawing.Point(330, 300);
            btnRead.Click += BtnRead_Click;

            this.Controls.Add(lblTemperature);
            this.Controls.Add(lblHumidity);
            this.Controls.Add(lblStatus);
            this.Controls.Add(lblTempValue);
            this.Controls.Add(lblHumValue);
            this.Controls.Add(cmbPort);
            this.Controls.Add(btnRead);
        }

        private void LoadAvailablePorts()
        {
            string[] ports = SerialPort.GetPortNames();
            cmbPort.Items.Clear();

            foreach (string port in ports)
            {
                cmbPort.Items.Add(port);
            }

            if (cmbPort.Items.Count > 0)
            {
                cmbPort.SelectedIndex = 0;
            }
            else
            {
                cmbPort.Items.Add("Không có cổng COM");
                cmbPort.SelectedIndex = 0;
            }
        }

        private async void BtnRead_Click(object sender, EventArgs e)
        {
            btnRead.Enabled = false;
            lblStatus.Text = "Đang lấy dữ liệu...";
            lblStatus.ForeColor = System.Drawing.Color.DarkBlue;

            try
            {
                // Gọi API
                string json = await httpClient.GetStringAsync(apiUrl);

                // Parse JSON
                WeatherData data = ParseWeatherJson(json);

                // Hiển thị lên label
                lblTempValue.Text = data.Temperature.ToString("0.0", CultureInfo.InvariantCulture) + " °C";
                lblHumValue.Text = data.Humidity.ToString("0.0", CultureInfo.InvariantCulture) + " %";

                // Gửi qua Arduino
                if (cmbPort.SelectedItem != null && cmbPort.SelectedItem.ToString() != "Không có cổng COM")
                {
                    SendToArduino(data.Temperature, data.Humidity);
                    lblStatus.Text = "Đã gửi dữ liệu tới Arduino";
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    lblStatus.Text = "Không có cổng COM. Chỉ hiển thị dữ liệu.";
                    lblStatus.ForeColor = System.Drawing.Color.OrangeRed;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Lỗi: " + ex.Message;
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }
            finally
            {
                btnRead.Enabled = true;
            }
        }

        private WeatherData ParseWeatherJson(string json)
        {
            double temperature = ExtractDouble(json, "temperature", "temp");
            double humidity = ExtractDouble(json, "humidity", "hum");

            return new WeatherData
            {
                Temperature = temperature,
                Humidity = humidity
            };
        }

        private double ExtractDouble(string json, params string[] keys)
        {
            foreach (string key in keys)
            {
                int index = json.IndexOf("\"" + key + "\"", StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    int colonIndex = json.IndexOf(':', index);
                    if (colonIndex >= 0)
                    {
                        int start = colonIndex + 1;
                        int end = start;

                        while (end < json.Length && json[end] != ',' && json[end] != '}' && json[end] != ']')
                        {
                            end++;
                        }

                        string value = json.Substring(start, end - start).Trim();

                        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                        {
                            return result;
                        }

                        if (double.TryParse(value.Replace(".", ","), NumberStyles.Float, CultureInfo.InvariantCulture, out double result2))
                        {
                            return result2;
                        }
                    }
                }
            }

            return 0;
        }

        private void SendToArduino(double temperature, double humidity)
        {
            try
            {
                using (SerialPort port = new SerialPort())
                {
                    port.PortName = cmbPort.SelectedItem.ToString();
                    port.BaudRate = 9600;
                    port.Parity = Parity.None;
                    port.DataBits = 8;
                    port.StopBits = StopBits.One;

                    port.Open();

                    string data = "T:" + temperature.ToString("0.0", CultureInfo.InvariantCulture) +
                                  ";H:" + humidity.ToString("0.0", CultureInfo.InvariantCulture);

                    port.WriteLine(data);
                    port.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gửi dữ liệu tới Arduino thất bại: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && serialPort != null)
            {
                if (serialPort.IsOpen)
                    serialPort.Close();
                serialPort.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class WeatherData
    {
        public double Temperature { get; set; }
        public double Humidity { get; set; }
    }
}
