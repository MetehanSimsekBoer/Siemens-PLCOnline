using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.Devices;
using OfficeOpenXml;
using Sharp7;

namespace Siemens_PLCOnline
{
    public partial class Form1 : Form
    {
        private S7Client plcS7Client = new S7Client();
        private readonly object plcLock = new object();
        List<AdresModel> adresler = new List<AdresModel>();
        public Form1()
        {
            InitializeComponent();


            string dosyaYolu = Path.Combine(Application.StartupPath, "PLCAdresListesi.xlsx");
            List<AdresModel> adresler = ExceldenAdresleriOku(dosyaYolu);


        }


        private void label53_Click(object sender, EventArgs e)
        {

        }


        private void btnBasla_Click(object sender, EventArgs e)
        {
            int connectionStatus = plcS7Client.ConnectTo(txtIp.Text, 0, 0);
            string Start = adresler
    .FirstOrDefault(x => x.Aciklama == "Start" || x.Aciklama != null)?.Adres;

            if (connectionStatus == 0)
            {

                if (!string.IsNullOrEmpty(Start))
                {
                    SendCommandForBool(Start, true);
                }
                else
                {
                    StatusList.Items.Add("Start adresi bulunamadý!");
                }



                btnStart.Visible = false;
                btnWrite.Enabled = true;
                btnRead.Enabled = true;
                OkuIslemleriWords();
                OkuIslemleriBoolean();

                Task.Run(async () => await DBReadErrorBitAsync());
            }

        }



        private void button48_Click(object sender, EventArgs e)
        {
            btnWrite.Enabled = false;
            plcS7Client.Disconnect();
        }



        public int SendCommandForBool(string PlcAddress, bool value)
        {


            string pattern = @"DB(\d+)\.DBX(\d+)\.(\d+)";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(PlcAddress);

            if (!match.Success)
            {
                StatusList.Items.Add($"'{PlcAddress}' için PLC adresi bulunamadý.");
                return -1;

            }

            int dbNumber = Convert.ToInt16(match.Groups[1].Value);
            int startByte = Convert.ToInt16(match.Groups[2].Value);
            int bitIndex = Convert.ToInt16(match.Groups[3].Value);


            try
            {
                byte[] readBuffer = new byte[1];
                int readResult = plcS7Client.DBRead(dbNumber, startByte, readBuffer.Length, readBuffer);

                if (readResult != 0)
                {
                    StatusList.Items.Add($"'{PlcAddress}' için veri okunamadý.");
                    return -1;

                }

                if (value)
                {
                    readBuffer[0] = (byte)(readBuffer[0] | (1 << bitIndex));
                }
                else
                {
                    readBuffer[0] = (byte)(readBuffer[0] & ~(1 << bitIndex));
                }

                int writeResult = plcS7Client.DBWrite(dbNumber, startByte, 1, readBuffer);


            }
            catch (Exception ex)
            {
                StatusList.Items.Add(ex.Message);
                return -1;
            }
            return 0;
        }

        private void OkuIslemleriBoolean()
        {


            var commandsToRead = new List<string>
            {

                "DB1.DBX0.1",//Start
                "DB1.DBX0.2",//Stop
                "DB1.DBX0.0",//Life
                "DB1.DBX0.4",//Home
                "DB1.DBX1.7",//CounterReset
                "DB1.DBX1.1",//Manual
                "DB1.DBX3.0",//MainAct
                "DB1.DBX3.1",//SpacingAct
                "DB1.DBX3.2",//SideHoldingAct
                "DB1.DBX3.3",//TopHoldingAct
                "DB1.DBX3.4",//IdlerAct
                "DB1.DBX0.3",//Reset
                "DB1.DBX104.5",//ResetDO
                "DB1.DBX104.1",//Run
                "DB1.DBX104.6",//ResetOK
                "DB1.DBX105.0",//HomeOK
                "DB1.DBX108.0",
                "DB1.DBX108.1",
                "DB1.DBX109.0",
                "DB1.DBX109.1",
                "DB1.DBX110.0",
                "DB1.DBX110.1",
                "DB1.DBX1.0",//TestLife
                

            };

            var booleanAdresler = adresler
     .Where(x => string.Equals(x.Tip, "Bool", StringComparison.OrdinalIgnoreCase)
              || string.Equals(x.Tip, "Alarm", StringComparison.OrdinalIgnoreCase))
     .Select(x => x.Adres)
     .ToList();


            var data = ReadMultipleBoolsFromPlc(booleanAdresler);

            if (data.Count > 0)
            {









                txtStart.Text = data["DB1.DBX0.1"].ToString();
                txtStop.Text = data["DB1.DBX0.2"].ToString();
                txtLife.Text = data["DB1.DBX0.0"].ToString();
                txtHome.Text = data["DB1.DBX0.4"].ToString();
                txtCounterReset.Text = data["DB1.DBX1.7"].ToString();
                txtManual.Text = data["DB1.DBX1.1"].ToString();
                txtMainAct.Text = data["DB1.DBX3.0"].ToString();
                txtSpacingAct.Text = data["DB1.DBX3.1"].ToString();
                txtSideHoldingAct.Text = data["DB1.DBX3.2"].ToString();
                txtTopHoldingAct.Text = data["DB1.DBX3.3"].ToString();
                txtIdlerAct.Text = data["DB1.DBX3.4"].ToString();
                txtReset.Text = data["DB1.DBX0.3"].ToString();

                txtResetDo.Text = data["DB1.DBX104.5"].ToString();
                txtRun.Text = data["DB1.DBX104.1"].ToString();
                txtResetOk.Text = data["DB1.DBX104.6"].ToString();

                txtHomeOk.Text = data["DB1.DBX105.0"].ToString();

                txtTestLife.Text = data["DB1.DBX1.0"].ToString();


                txtEmergency.Text = data["DB1.DBX108.0"].ToString();
                txtGate.Text = data["DB1.DBX108.1"].ToString();
                txtMainAlarm.Text = data["DB1.DBX109.0"].ToString();

                txtFlattenAlarm.Text = data["DB1.DBX109.1"].ToString();


                txtRejectAlmSAlarm.Text = data["DB1.DBX110.0"].ToString();

                txtRejectAlmRError.Text = data["DB1.DBX110.1"].ToString();





            }

            StatusList.Items.Add($"Boolean Deðerler Okundu.");

        }
        public void YazIslemleriBoolean()
        {


            var commandsAndValues = new Dictionary<string, bool>
                    {
                        { "DB1.DBX0.1", Convert.ToBoolean(txtStart.Text )},
                         { "DB1.DBX0.0", Convert.ToBoolean(txtLife.Text )},
                        { "DB1.DBX0.2", Convert.ToBoolean(txtStop.Text) },
                        { "DB1.DBX0.4", Convert.ToBoolean(txtHome.Text) },
                        { "DB1.DBX1.7", Convert.ToBoolean(txtCounterReset.Text) },
                        { "DB1.DBX1.1", Convert.ToBoolean(txtManual.Text) },
                        { "DB1.DBX3.0", Convert.ToBoolean(txtMainAct.Text) },
                        { "DB1.DBX3.1", Convert.ToBoolean(txtSpacingAct.Text) },
                        { "DB1.DBX3.2", Convert.ToBoolean(txtSideHoldingAct.Text) },
                        { "DB1.DBX3.3", Convert.ToBoolean(txtTopHoldingAct.Text) },
                        { "DB1.DBX3.4", Convert.ToBoolean(txtIdlerAct.Text) },
                        { "DB1.DBX0.3", Convert.ToBoolean(txtReset.Text) },
                        { "DB1.DBX104.5", Convert.ToBoolean(txtResetDo.Text) },
                        { "DB1.DBX104.1", Convert.ToBoolean(txtRun.Text) },
                        { "DB1.DBX104.6", Convert.ToBoolean(txtResetOk.Text) },
                        { "DB1.DBX105.0", Convert.ToBoolean(txtHomeOk.Text) },
                       { "DB1.DBX1.0", Convert.ToBoolean(txtTestLife.Text) },
                        { "DB1.DBX105.6", Convert.ToBoolean(txtErrorPLC.Text) },

                         { "DB1.DBX108.0", Convert.ToBoolean(txtEmergency.Text) },
                        { "DB1.DBX108.1", Convert.ToBoolean(txtGate.Text) },
                        { "DB1.DBX109.0", Convert.ToBoolean(txtMainAlarm.Text) },
                         { "DB1.DBX109.1", Convert.ToBoolean(txtFlattenAlarm.Text) },
                        { "DB1.DBX110.0", Convert.ToBoolean(txtRejectAlmSAlarm.Text) },
                        { "DB1.DBX110.1", Convert.ToBoolean(txtRejectAlmRError.Text) },

                     };






            WriteMultipleBoolsToPlc(commandsAndValues);

        }

        public void YazIslemleriWords()
        {
            var commandsAndValues = new Dictionary<string, short>
                    {
                        { "DB1.DBW36", Convert.ToInt16(txtMain.Text) },
                        { "DB1.DBW38", Convert.ToInt16(txtSpacing.Text) },
                        { "DB1.DBW42", Convert.ToInt16(txtSideHolding.Text) },
                        { "DB1.DBW40", Convert.ToInt16(txtTopHolding.Text) },
                        { "DB1.DBW44", Convert.ToInt16(txtIdler.Text) },
                        { "DB1.DBW26", Convert.ToInt16(txtRejectPreDelay.Text) },
                        { "DB1.DBW28", Convert.ToInt16(txtRejectDelayS.Text) },
                        { "DB1.DBW30", Convert.ToInt16(txtRejectDelayR.Text) },
                        { "DB1.DBW4", Convert.ToInt16(txtStartDelay.Text) },
                        { "DB1.DBW6", Convert.ToInt16(txtStopDelay.Text) },
                        { "DB1.DBW8", Convert.ToInt16(txtApplicatorDelay.Text) },
                        { "DB1.DBW10", Convert.ToInt16(txtApplicatorTrigger.Text) },
                        { "DB1.DBW12", Convert.ToInt16(txtCameraDelay.Text) },
                        { "DB1.DBW14", Convert.ToInt16(txtCameraTimeout.Text) },
                        { "DB1.DBW52", Convert.ToInt16(txtAcDriveCount.Text) },
                        { "DB1.DBW54", Convert.ToInt16(txtDischargeCount.Text) },
                        { "DB1.DBW16.0", Convert.ToInt16(txtSeperatorDelay.Text) },
                        { "DB1.DBW18.0", Convert.ToInt16(txtSeperatorReset.Text) },
                        { "DB1.DBW56.0", Convert.ToInt16(txtBoxCount.Text) },
                     };

            WriteMultipleWordsToPlc(commandsAndValues);

        }





        public void WriteMultipleBoolsToPlc(Dictionary<string, bool> boolCommands)
        {
            try
            {

                var grouped = boolCommands
                    .Where(x => Regex.IsMatch(x.Key, @"^DB(\d+)\.DBX(\d+)\.(\d+)$"))
                    .Select(x =>
                    {
                        var match = Regex.Match(x.Key, @"^DB(\d+)\.DBX(\d+)\.(\d+)$");
                        return new
                        {
                            Address = x.Key,
                            DB = int.Parse(match.Groups[1].Value),
                            ByteIndex = int.Parse(match.Groups[2].Value),
                            BitIndex = int.Parse(match.Groups[3].Value),
                            Value = x.Value
                        };
                    })
                    .GroupBy(x => new { x.DB, x.ByteIndex });

                foreach (var group in grouped)
                {
                    int dbNumber = group.Key.DB;
                    int byteIndex = group.Key.ByteIndex;
                    byte[] buffer = new byte[1];


                    int readResult = plcS7Client.DBRead(dbNumber, byteIndex, 1, buffer);
                    if (readResult != 0)
                    {
                        StatusList.Items.Add($"DB{dbNumber}.DBX{byteIndex}.x okunamadý. Hata kodu: {readResult}");
                        continue;
                    }


                    foreach (var item in group)
                    {
                        if (item.Value)
                            buffer[0] |= (byte)(1 << item.BitIndex);
                        else
                            buffer[0] &= (byte)~(1 << item.BitIndex);
                    }


                    int writeResult = plcS7Client.DBWrite(dbNumber, byteIndex, 1, buffer);
                    if (writeResult != 0)
                    {
                        StatusList.Items.Add($"DB{dbNumber}.DBX{byteIndex}.x yazýlamadý. Hata kodu: {writeResult}");
                    }
                }

                StatusList.Items.Add("Bool yazma iþlemi tamamlandý.");
            }
            catch (Exception ex)
            {
                StatusList.Items.Add($"Genel Hata: {ex.Message}");
            }
        }

        public void WriteMultipleWordsToPlc(Dictionary<string, short> commandsAndValues)
        {
            try
            {
                foreach (var command in commandsAndValues)
                {
                    string plcAddress = command.Key;
                    short value = command.Value;

                    if (string.IsNullOrEmpty(plcAddress))
                    {
                        StatusList.Items.Add($"Boþ PLC adresi atlandý.");
                        continue;
                    }

                    string pattern = @"DB(\d+)\.DBW(\d+)(\.(\d+))?";
                    Match match = Regex.Match(plcAddress, pattern);

                    if (!match.Success)
                    {
                        StatusList.Items.Add($"'{plcAddress}' için geçerli bir PLC adresi bulunamadý.");
                        continue;
                    }

                    int dbNumber = Convert.ToInt16(match.Groups[1].Value);
                    int byteIndex = Convert.ToInt16(match.Groups[2].Value);

                    byte[] buffer = BitConverter.GetBytes(value);
                    Array.Reverse(buffer);

                    int writeResult = plcS7Client.DBWrite(dbNumber, byteIndex, buffer.Length, buffer);

                    if (writeResult != 0)
                    {
                        StatusList.Items.Add($"'{plcAddress}' için yazma baþarýsýz. Hata kodu: {writeResult}");
                    }
                }
                StatusList.Items.Add($"Yazma Ýþlemi Tamamlandý.");
            }
            catch (Exception ex)
            {
                StatusList.Items.Add($"Genel Hata: {ex.Message}");
            }
        }
        private void OkuIslemleriWords()
        {


            var commandsToRead = new List<string>
            {
                "DB1.DBW36", //Main-AnaKonveyor
                "DB1.DBW26", //RejectPreDelay
                "DB1.DBW28", //RejectDelayS
                "DB1.DBW30", //RejectDelayR
                "DB1.DBW4", //StartDelay
                "DB1.DBW6", //StopDelay
                "DB1.DBW8", //AplicatorDelay
                "DB1.DBW10", //ApplicatorTrigger
                "DB1.DBW12", //CameraDelay
                "DB1.DBW14", //CameraTimeout
                "DB1.DBW38", //Spacing
                "DB1.DBW42", //SideHolding
                "DB1.DBW40", //TopHolding
                "DB1.DBW44", //Idler
                "DB1.DBW52", //ACDriveCount
                "DB1.DBW54", //DischargeCOunt 
                "DB1.DBW16.0",//SeperatorDelay
                "DB1.DBW18.0",//SeperatorReset
                "DB1.DBW56.0",//BoxCount

            };

            var data = ReadMultipleWordsFromPlc(commandsToRead);

            if (data.Count > 0)
            {
                txtMain.Text = data["DB1.DBW36"].ToString();
                txtRejectPreDelay.Text = data["DB1.DBW26"].ToString();
                txtRejectDelayS.Text = data["DB1.DBW28"].ToString();
                txtRejectDelayR.Text = data["DB1.DBW30"].ToString();
                txtStartDelay.Text = data["DB1.DBW4"].ToString();
                txtStopDelay.Text = data["DB1.DBW6"].ToString();
                txtApplicatorDelay.Text = data["DB1.DBW8"].ToString();
                txtApplicatorTrigger.Text = data["DB1.DBW10"].ToString();
                txtCameraDelay.Text = data["DB1.DBW12"].ToString();
                txtCameraTimeout.Text = data["DB1.DBW14"].ToString();
                txtSpacing.Text = data["DB1.DBW38"].ToString();
                txtSideHolding.Text = data["DB1.DBW42"].ToString();
                txtTopHolding.Text = data["DB1.DBW40"].ToString();
                txtIdler.Text = data["DB1.DBW44"].ToString();
                txtAcDriveCount.Text = data["DB1.DBW52"].ToString();
                txtDischargeCount.Text = data["DB1.DBW54"].ToString();
                txtSeperatorDelay.Text = data["DB1.DBW16.0"].ToString();
                txtSeperatorReset.Text = data["DB1.DBW18.0"].ToString();
                txtBoxCount.Text = data["DB1.DBW56.0"].ToString();
            }

            StatusList.Items.Add($"Word Deðerler Okundu.");

        }

        public Dictionary<string, ushort> ReadMultipleWordsFromPlc(List<string> adresses)
        {
            Dictionary<string, ushort> results = new Dictionary<string, ushort>();
            try
            {


                var dbNumber = int.Parse(Regex.Match(adresses.First(), @"DB(\d+)").Groups[1].Value);
                var minByteIndex = adresses.Min(x => int.Parse(Regex.Match(x, @"DBW(\d+)").Groups[1].Value));
                var maxByteIndex = adresses.Max(x => int.Parse(Regex.Match(x, @"DBW(\d+)").Groups[1].Value));


                int byteLength = (maxByteIndex - minByteIndex) + 2;
                byte[] buffer = new byte[byteLength];


                int result = plcS7Client.DBRead(dbNumber, minByteIndex, buffer.Length, buffer);

                if (result != 0)
                {
                    StatusList.Items.Add("Toplu veri okuma baþarýsýz! Hata kodu: " + result);
                    return results;
                }


                foreach (var address in adresses)
                {
                    int byteIndex = int.Parse(Regex.Match(address, @"DBW(\d+)").Groups[1].Value);
                    int bufferOffset = byteIndex - minByteIndex;
                    ushort wordValue = BitConverter.ToUInt16(new byte[] { buffer[bufferOffset + 1], buffer[bufferOffset] }, 0);
                    results[address] = wordValue;
                }
            }


            catch (Exception ex)
            {
                StatusList.Items.Add(ex.Message);
            }
            return results;
        }


        public Dictionary<string, bool> ReadMultipleBoolsFromPlc(List<string> plcAddresses)
        {
            Dictionary<string, bool> results = new Dictionary<string, bool>();

            try
            {
                if (!plcAddresses.Any())
                    return results;

                var dbNumber = int.Parse(Regex.Match(plcAddresses.First(), @"DB(\d+)").Groups[1].Value);

                var minByteIndex = plcAddresses.Min(addr =>
                {
                    var match = Regex.Match(addr, @"DBX(\d+)\.(\d+)");
                    if (match.Success)
                        return int.Parse(match.Groups[1].Value);
                    else
                        throw new FormatException($"Adres formatý hatalý: {addr}");
                });

                var maxByteIndex = plcAddresses.Max(addr =>
                {
                    var match = Regex.Match(addr, @"DBX(\d+)\.(\d+)");
                    if (match.Success)
                        return int.Parse(match.Groups[1].Value);
                    else
                        throw new FormatException($"Adres formatý hatalý: {addr}");
                });

                int byteLength = (maxByteIndex - minByteIndex) + 1;
                byte[] buffer = new byte[byteLength];

                int result = plcS7Client.DBRead(dbNumber, minByteIndex, buffer.Length, buffer);

                if (result != 0)
                {
                    StatusList.Items.Add($"Toplu veri okuma baþarýsýz! Hata kodu: {result}");
                    return results;
                }

                foreach (var plcAddress in plcAddresses)
                {
                    var match = Regex.Match(plcAddress, @"DBX(\d+)\.(\d+)");
                    if (!match.Success)
                    {
                        StatusList.Items.Add($"Adres formatý hatalý: {plcAddress}");
                        continue;
                    }

                    int byteIndex = int.Parse(match.Groups[1].Value);
                    int bitIndex = int.Parse(match.Groups[2].Value);

                    int bufferOffset = byteIndex - minByteIndex;

                    if (bufferOffset < 0 || bufferOffset >= buffer.Length)
                    {
                        StatusList.Items.Add($"Buffer sýnýr hatasý: {plcAddress}");
                        continue;
                    }

                    bool boolValue = (buffer[bufferOffset] & (1 << bitIndex)) != 0;
                    results[plcAddress] = boolValue;
                }
            }
            catch (Exception ex)
            {
                StatusList.Items.Add($"Hata oluþtu: {ex.Message}");
            }

            return results;
        }

        public bool ReadBoolFromPlc(string plcAddress)
        {
            try
            {



                string pattern = @"DB(\d+)\.DBX(\d+)\.(\d+)";
                Regex regex = new Regex(pattern);
                Match match = regex.Match(plcAddress);

                if (!match.Success)
                {
                    return false;
                }

                int dbNumber = Convert.ToInt16(match.Groups[1].Value);
                int byteIndex = Convert.ToInt16(match.Groups[2].Value);
                int bitIndex = Convert.ToInt16(match.Groups[3].Value);

                byte[] buffer = new byte[1];

                int result = plcS7Client.DBRead(dbNumber, byteIndex, buffer.Length, buffer);
                if (result != 0)
                {
                    StatusList.Items.Add("Veri okuma baþarýsýz! Hata kodu: " + result);
                    return false;
                }

                bool bitValue = (buffer[0] & (1 << bitIndex)) != 0;
                return bitValue;
            }
            catch (Exception ex)
            {
                StatusList.Items.Add($"Hata: {ex.Message}");
                return false;
            }
        }


        public bool Connected { get => plcS7Client != null && plcS7Client.Connected; }
        public async Task DBReadErrorBitAsync()
        {
            try
            {
                List<string> alarmBits = new List<string>
                {
                    "DB1.DBX108.0",
                    "DB1.DBX108.1",
                    "DB1.DBX109.0",
                    "DB1.DBX109.1",
                    "DB1.DBX110.0",
                    "DB1.DBX110.1"
                };



                List<AdresModel> AlarmIs = adresler
    .Where(x => x.Tip == "Alarm")
    .ToList();
                string errorAdres = adresler
    .FirstOrDefault(x => x.Aciklama.Contains("Error"))?.Adres;

                //while (Connected)
                //{
                try
                {
                    bool errorBit;


                    lock (plcLock)
                    {
                        errorBit = ReadBoolFromPlc(errorAdres);
                    }

                    if (errorBit)
                    {
                        Dictionary<string, bool> alarmResults;

                        lock (plcLock)
                        {
                            var readResult = ReadMultipleBoolsFromPlc(alarmBits);
                            alarmResults = readResult.ToDictionary(k => k.Key, v => v.Value);
                        }

                        this.Invoke((MethodInvoker)delegate
                        {
                            txtErrorPLC.Text = errorBit.ToString();
                            txtEmergency.Text = alarmResults["DB1.DBX108.0"].ToString();
                            txtGate.Text = alarmResults["DB1.DBX108.1"].ToString();
                            txtMainAlarm.Text = alarmResults["DB1.DBX109.0"].ToString();
                            txtFlattenAlarm.Text = alarmResults["DB1.DBX109.1"].ToString();
                            txtRejectAlmSAlarm.Text = alarmResults["DB1.DBX110.0"].ToString();
                            txtRejectAlmRError.Text = alarmResults["DB1.DBX110.1"].ToString();
                        });
                    }
                    else
                    {
                        lock (plcLock)
                        {
                            SendCommandForBool("DB1.DBX105.6", false);
                        }

                        StatusList.Items.Add("Error bit pasif.");
                    }
                }
                catch (Exception ex)
                {
                    plcS7Client.Disconnect();
                    StatusList.Items.Add($"Hata oluþtu: {ex.Message}");
                    StatusList.Items.Add($"IP Adresi: {txtIp.Text}");
                }

                await Task.Delay(1000);
                //}
            }
            catch (Exception ex)
            {
                StatusList.Items.Add($"DBReadErrorBitAsync hatasý: {ex.Message}");
            }
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            YazIslemleriWords();
            YazIslemleriBoolean();
        }


        public List<AdresModel> ExceldenAdresleriOku(string? dosyaYolu)
        {


            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(new FileInfo(dosyaYolu)))
            {
                var sheet = package.Workbook.Worksheets[0];
                int rowCount = sheet.Dimension.End.Row;

                for (int row = 2; row <= rowCount; row++)
                {
                    string adres = sheet.Cells[row, 1].Text;
                    string tip = sheet.Cells[row, 2].Text;
                    string aciklama = sheet.Cells[row, 3].Text;

                    if (!string.IsNullOrWhiteSpace(adres))
                    {
                        adresler.Add(new AdresModel
                        {
                            Adres = adres,
                            Tip = tip,
                            Aciklama = aciklama
                        });
                    }
                }
            }

            return adresler;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (plcS7Client.Connected)
            {
                plcS7Client.Disconnect();
                btnConnect.Text = "Connect";
                btnConnect.BackColor = System.Drawing.Color.Lime;

                btnLight.BackColor = System.Drawing.Color.Red;
                StatusList.Items.Add("Baðlantý Kesildi");
                btnStart.Visible = false;
                btnStart.Enabled = false;
                return;
            }

            int connectionStatus = plcS7Client.ConnectTo(txtIp.Text, 0, 0);

            if (connectionStatus == 0)
            {
                SendCommandForBool("DB1.DBX1.0", true);

                btnLight.BackColor = System.Drawing.Color.Lime;
                StatusList.Items.Add("Baðlantý Saðlandý");

                btnStart.Visible = true;
                btnStart.Enabled = true;

                btnConnect.Text = "Disconnect";
                btnConnect.BackColor = System.Drawing.Color.Red;
            }
            else
            {
                StatusList.Items.Add("Baðlantý Saðlanamadý");

                btnLight.BackColor = System.Drawing.Color.Red;
                btnStart.Enabled = false;

                btnConnect.Text = "Connect";
                btnConnect.BackColor = System.Drawing.Color.Red;
            }
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnWrite.Enabled = false;
            btnRead.Enabled = false;

            var adresListesi = adresler
    .Select(x => new AdresModel
    {
        Adres = x.Adres,
        Tip = x.Tip,
        Aciklama = x.Aciklama
    }).ToList();



            Task.Run(async () => await DBReadErrorBitAsync());
        }



        private void btnRead_Click(object sender, EventArgs e)
        {
            OkuIslemleriWords();
            OkuIslemleriBoolean();
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            var data = new Dictionary<string, string>
    {
        { "Main", txtMain.Text },
        { "Spacing", txtSpacing.Text },
        { "SideHolding", txtSideHolding.Text },
        { "TopHolding", txtTopHolding.Text },
        { "Idler", txtIdler.Text },
        { "RejectPreDelay", txtRejectPreDelay.Text },
        { "RejectDelayS", txtRejectDelayS.Text },
        { "RejectDelayR", txtRejectDelayR.Text },
        { "StartDelay", txtStartDelay.Text },
        { "StopDelay", txtStopDelay.Text },
        { "ApplicatorDelay", txtApplicatorDelay.Text },
        { "ApplicatorTrigger", txtApplicatorTrigger.Text },
        { "CameraDelay", txtCameraDelay.Text },
        { "CameraTimeout", txtCameraTimeout.Text },
        { "AcDriveCount", txtAcDriveCount.Text },
        { "DischargeCount", txtDischargeCount.Text },
        { "SeperatorDelay", txtSeperatorDelay.Text },
        { "SeperatorReset", txtSeperatorReset.Text },
        { "BoxCount", txtBoxCount.Text },

        { "Start", txtStart.Text },
        { "Life", txtLife.Text },
        { "Stop", txtStop.Text },
        { "Home", txtHome.Text },
        { "CounterReset", txtCounterReset.Text },
        { "Manual", txtManual.Text },
        { "MainAct", txtMainAct.Text },
        { "SpacingAct", txtSpacingAct.Text },
        { "SideHoldingAct", txtSideHoldingAct.Text },
        { "TopHoldingAct", txtTopHoldingAct.Text },
        { "IdlerAct", txtIdlerAct.Text },
        { "Reset", txtReset.Text },
        { "ResetDo", txtResetDo.Text },
        { "Run", txtRun.Text },
        { "ResetOk", txtResetOk.Text },
        { "HomeOk", txtHomeOk.Text },
        { "TestLife", txtTestLife.Text },
        { "ErrorPLC", txtErrorPLC.Text },
        { "Emergency", txtEmergency.Text },
        { "Gate", txtGate.Text },
        { "MainAlarm", txtMainAlarm.Text },
        { "FlattenAlarm", txtFlattenAlarm.Text },
        { "RejectAlmSAlarm", txtRejectAlmSAlarm.Text },
        { "RejectAlmRError", txtRejectAlmRError.Text }
    };

            string json = System.Text.Json.JsonSerializer.Serialize(data);
            File.WriteAllText("ayarlar.txt", json);
            var result = MessageBox.Show("Deðerler kaydedildi.\nAyar dosyasýný açmak ister misiniz?", "Bilgi",
    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start("notepad.exe", "ayarlar.txt");
            }
        }

        private void btnGet_Click(object sender, EventArgs e)
        {
            if (!File.Exists("ayarlar.txt"))
            {
                MessageBox.Show("Kayýtlý ayar bulunamadý.");
                return;
            }

            string json = File.ReadAllText("ayarlar.txt");
            var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            txtMain.Text = data["Main"];
            txtSpacing.Text = data["Spacing"];
            txtSideHolding.Text = data["SideHolding"];
            txtTopHolding.Text = data["TopHolding"];
            txtIdler.Text = data["Idler"];
            txtRejectPreDelay.Text = data["RejectPreDelay"];
            txtRejectDelayS.Text = data["RejectDelayS"];
            txtRejectDelayR.Text = data["RejectDelayR"];
            txtStartDelay.Text = data["StartDelay"];
            txtStopDelay.Text = data["StopDelay"];
            txtApplicatorDelay.Text = data["ApplicatorDelay"];
            txtApplicatorTrigger.Text = data["ApplicatorTrigger"];
            txtCameraDelay.Text = data["CameraDelay"];
            txtCameraTimeout.Text = data["CameraTimeout"];
            txtAcDriveCount.Text = data["AcDriveCount"];
            txtDischargeCount.Text = data["DischargeCount"];
            txtSeperatorDelay.Text = data["SeperatorDelay"];
            txtSeperatorReset.Text = data["SeperatorReset"];
            txtBoxCount.Text = data["BoxCount"];

            txtStart.Text = data["Start"];
            txtLife.Text = data["Life"];
            txtStop.Text = data["Stop"];
            txtHome.Text = data["Home"];
            txtCounterReset.Text = data["CounterReset"];
            txtManual.Text = data["Manual"];
            txtMainAct.Text = data["MainAct"];
            txtSpacingAct.Text = data["SpacingAct"];
            txtSideHoldingAct.Text = data["SideHoldingAct"];
            txtTopHoldingAct.Text = data["TopHoldingAct"];
            txtIdlerAct.Text = data["IdlerAct"];
            txtReset.Text = data["Reset"];
            txtResetDo.Text = data["ResetDo"];
            txtRun.Text = data["Run"];
            txtResetOk.Text = data["ResetOk"];
            txtHomeOk.Text = data["HomeOk"];
            txtTestLife.Text = data["TestLife"];
            txtErrorPLC.Text = data["ErrorPLC"];
            txtEmergency.Text = data["Emergency"];
            txtGate.Text = data["Gate"];
            txtMainAlarm.Text = data["MainAlarm"];
            txtFlattenAlarm.Text = data["FlattenAlarm"];
            txtRejectAlmSAlarm.Text = data["RejectAlmSAlarm"];
            txtRejectAlmRError.Text = data["RejectAlmRError"];

            MessageBox.Show("Kaydedilen Deðerler Getirildi.");
        }
    }
}
