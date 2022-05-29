using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Diagnostics;   /* コマンドライン */
namespace GDB_tool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            GDB_init();                  /* gdb tool initialize */
        }

        private Process GDBprocess;
        private Process ReadElfprocess;

        private bool isReceived = false;
        private bool isErrorReceived = false;
        private bool isWarningReceived = false;
        private List<string> Buffer = new List<string>();
        private List<string> ErrorBuffer = new List<string>();
        private List<string> WarningBuffer = new List<string>();
        private List<string> RAMData = new List<string>();

        private enum ReceivedData
        {
            Nodata,
            Standard,
            Error,
            Warning,
        }

        private void GDB_init()
        {
            string elf_path = "C:\\Users\\user\\source\\repos\\GDB_tool\\GDB_tool\\bin\\Debug\\rac_in.elf";
            string gdb_path = "C:\\Users\\user\\source\\repos\\GDB_tool\\GDB_tool\\bin\\Debug\\resource\\arm-none-eabi-gdb.exe";

            ProcessStartInfo processStartInfo = new ProcessStartInfo(gdb_path, elf_path);
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;       /* window表示なし */
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.RedirectStandardInput = true;
            GDBprocess = new Process();
            GDBprocess.StartInfo = processStartInfo;
            GDBprocess.OutputDataReceived += GDBProcess_OutputDataReceived;
            GDBprocess.ErrorDataReceived += GDBProcess_ErrorDataReceived;
            GDBprocess.Start();
            GDBprocess.BeginOutputReadLine();
            GDBprocess.BeginErrorReadLine();

        }
        
        private void ReadElf_init()
        {
            BufferClear();
            RAMData.Clear();
            string elf_path = "C:\\Users\\user\\source\\repos\\GDB_tool\\GDB_tool\\bin\\Debug\\rac_in.elf -s";
            string readelf_path = "C:\\Users\\user\\source\\repos\\GDB_tool\\GDB_tool\\bin\\Debug\\resource\\arm-none-eabi-readelf.exe";
            ProcessStartInfo psi = new ProcessStartInfo(readelf_path, elf_path);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;       /* window表示なし */
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardInput = true;
            ReadElfprocess = new Process();
            ReadElfprocess.StartInfo = psi;
            ReadElfprocess.OutputDataReceived += ReadElfprocess_OutputDataReceived;
            ReadElfprocess.ErrorDataReceived += ReadElfprocess_ErrorDataReceived;

        }
        private void read_Elf()
        {
            try
            {
                ReadElf_init();
                ReadElfprocess.Start();
                ReadElfprocess.BeginOutputReadLine();
                ReadElfprocess.BeginErrorReadLine();

                ReceivedData ans = DataWaitlgnoreWarning(15000);
                if (ans == ReceivedData.Nodata || ans == ReceivedData.Error)
                {
                    throw new Exception("error");
                }
                else
                {
                    Debug.WriteLine("end");
                    RAMData.Sort();
                    comboBox1.Items.Clear();
                    for (int i = 0; i < RAMData.Count; i++)
                    {
                        comboBox1.Items.Add(RAMData[i]);
                    }
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        private ReceivedData DataWaitlgnoreWarning(int timeout_msec = 3000)
        {
            ReceivedData ans = ReceivedData.Nodata;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (!isReceived && !isErrorReceived)
            {
                if (sw.ElapsedMilliseconds > timeout_msec)
                {
                    ans = ReceivedData.Nodata;
                    Debug.WriteLine("timeout");
                    break;
                }
            }
            sw.Stop();
            if (isReceived)
            {
                ans = ReceivedData.Standard;
            }
            else if (isErrorReceived)
            {
                ans = ReceivedData.Error;
            }
            return ans;
        }
        private void GDBProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
//            Debug.WriteLine($"[DEBUG]Received!" +(e.Data));
            Buffer.Add(e.Data);
            isReceived = true;
        }

        private void GDBProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine($"ErrorReceived [(e.Data)]");
            string wk_data = e.Data;
            if(wk_data == null)
            {

            }
            else if (wk_data.Contains("Warning"))
            {
                WarningBuffer.Add(e.Data);
                isWarningReceived = true;
            }
            else
            {
                ErrorBuffer.Add(e.Data);
                isErrorReceived = true;
            }
        }

        private void BufferClear()
        {
            Buffer.Clear();
            ErrorBuffer.Clear();
            WarningBuffer.Clear();
            isReceived = false;
            isWarningReceived = false;
            isErrorReceived = false;
        }

        private ReceivedData DatawaitELF(int timeout_msec = 1000)
        {
            ReceivedData ans = ReceivedData.Nodata;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                if (sw.ElapsedMilliseconds > timeout_msec)
                {
                    break;
                }
            }
            sw.Stop();
            if (isReceived)
            {
                ans = ReceivedData.Standard;
            }
            else if (isErrorReceived)
            {
                ans = ReceivedData.Error;
            }
            else if (isWarningReceived)
            {
                ans = ReceivedData.Warning;
            }
            return ans;
        }

        private ReceivedData Datawait(int timeout_msec = 2000)
        {
            ReceivedData ans = ReceivedData.Nodata;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            while(!isReceived && !isErrorReceived && !isWarningReceived)
            {
                if(sw.ElapsedMilliseconds > timeout_msec)
                {
                    ans = ReceivedData.Nodata;
                    break;
                }
            }
            sw.Stop();
            if (isReceived)
            {
                ans = ReceivedData.Standard;
            }
            else if (isErrorReceived)
            {
                ans = ReceivedData.Error;
            }
            else if (isWarningReceived)
            {
                ans = ReceivedData.Warning;
            }
            return ans;
        }

        private readonly object GdbLockObject = new object();

        private string[] RemovePrefix(string[] strs, string prefix)
        {
            if(strs[0] != prefix)
            {
                return strs;
            }
            else
            {
                string[] removeArray = new string[strs.Length - 1];
                Array.Copy(strs, 1, removeArray, 0, removeArray.Length);
                return RemovePrefix(removeArray, prefix);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            search_address("amode_data");
        }
        private bool chaeck_gdb_proccess()
        {
            bool ret = true;
            if (GDBprocess == null)
            {
                Debug.WriteLine("GDB process is null!!");
                ret = false;
            }
            return ret;
        }


        private void search_address(string ram_name)
        {
            if(!chaeck_gdb_proccess())
            {
                return;
            }
            ReceivedData rec = ReceivedData.Nodata;
            string buffer;
            lock (GdbLockObject)
            {
                BufferClear();
                GDBprocess.StandardInput.WriteLine($"p/x &"+ ram_name);
                rec = Datawait();
                if (rec == ReceivedData.Nodata)
                {
                    Debug.WriteLine("Receive Process timeout!!");
                    return;
                }
                if (rec == ReceivedData.Error || rec == ReceivedData.Warning)
                {
                    Debug.WriteLine("Receive Error!! Error buffer[ErrorBuffer.Count - 1]");
                    return;
                }
                buffer = Buffer[Buffer.Count - 1];
                Debug.WriteLine(buffer);
                string[] split = buffer.Split(' ');
                split = RemovePrefix(split, "(gdb)");

                if(split.Length < 3)
                {
                    return;
                }
                string addrStr = split[2];
                if (addrStr.StartsWith("0x"))
                {
                    addrStr = addrStr.Substring(2);
                }
                uint addr;
                uint.TryParse(addrStr, System.Globalization.NumberStyles.HexNumber, null,out addr);

                textBox1.Text = addrStr;
                return;
            }
        }

        private void search_struct(string ram_name)
        {

            if (!chaeck_gdb_proccess())
            {
                return;
            }

            ReceivedData rec = ReceivedData.Nodata;
            string buffer="";
            lock (GdbLockObject)
            {
                comboBox2.Items.Clear();
                BufferClear();
                GDBprocess.StandardInput.WriteLine($"p/x " + ram_name);
                rec = DatawaitELF();
                if (rec == ReceivedData.Nodata)
                {
                    Debug.WriteLine("Receive Process timeout!!");
                    return;
                }
                if (rec == ReceivedData.Error || rec == ReceivedData.Warning)
                {
                    Debug.WriteLine("Receive Error!! Error buffer[ErrorBuffer.Count - 1]");
                    return;
                }
                for(int i = 0; i < Buffer.Count; i++)
                {
                    buffer += Buffer[i];
                }
                Debug.WriteLine(buffer);
                buffer = buffer.Replace("}", " } ");
                buffer = buffer.Replace("{", " { ");
                buffer = buffer.Replace(",", " , ");
                string[] split = (buffer.Split(new string[] { " ", "  " }, StringSplitOptions.RemoveEmptyEntries));


                int wk_line = 0;
                int wk_columm = 0;
                int[] wk_group = { 0,0,0,0};
                int wk_group_cnt = 0;
                string[,] wk_str = new string[250,20];
                for (int i = 2; i < split.Length; i++)
                {
                    if (split[i] == "{")
                    {
                        if(split[i+1] == "{")
                        {
                            wk_str[wk_columm, wk_line] = "[" + wk_group[wk_group_cnt].ToString() + "]";
                            wk_line++;
                        }
                        else if(split[i+1].Contains("0x"))
                        {
                            wk_str[wk_columm, wk_line] = "[" + wk_group[wk_group_cnt].ToString() + "]";
                            wk_line++;
                        }
                        else
                        {
                            wk_str[wk_columm, wk_line] = "--";
                            wk_line++;
                        }
                    }
                    else if (split[i] == "}")
                    {

                        if ((wk_str[wk_columm, wk_line]!=null)&&(wk_str[wk_columm, wk_line].Contains("[")))
                        {
                            wk_line -= 1;
                        }
                        else
                        {
                            if (wk_str[wk_columm, wk_line] == null){
                                wk_line--;
                            }
                            if (wk_str[wk_columm, wk_line].Contains("["))
                            {
                                wk_line -= 1;
                            }
                            else
                            {
                                wk_line -= 2;
                            }
                        }
                        /*
                        if (wk_str[wk_columm, wk_line].Contains("["))
                        {
                            wk_line--;
                        }
                        */
                        /*
                        if(wk_str[wk_columm, wk_line] == null)
                        {
                            wk_line--;
                            while (wk_str[wk_columm, wk_line].Contains("["))
                            {
                                wk_line--;
                            }
                        }
                        else
                        {
                            wk_line--;

                        }
                        */
                    }
                    else if (split[i] == ",")
                    {
//                        if(split[i+1] == "0x0")
//                        {
//                        }
//                        else
                        {
                            if(wk_str[wk_columm, wk_line] == null)
                            {
                                wk_line--;
                            }
                            if (wk_str[wk_columm, wk_line] == "--")
                            {
                                ;
                            }
                            wk_group[0] = 0;
                        }
                        wk_columm++;
                        for(int j = 0; j < wk_line; j++)
                        {
                            wk_str[wk_columm, j] = wk_str[wk_columm - 1, j];
                        }
                        if(wk_str[wk_columm-1, wk_line].Contains("["))
                        {
                            string wk_cnt_str = wk_str[wk_columm - 1, wk_line].Replace("[", "").Replace("]", "");
                            int wk_cnt = Convert.ToInt32(wk_cnt_str)+1;
                            wk_str[wk_columm, wk_line] = "[" + wk_cnt.ToString() + "]";
                            wk_line++;
                        }
                    }
                    else
                    {
                        if ((split[i] == "=")
                        || (split[i] == "")
                        || (split[i].Contains("0x")))
                        {
                        }
                        else
                        {
                            wk_str[wk_columm, wk_line] = split[i];
                            wk_line++;
                        }
                    }

                }

                for (int x = 0; x < 250; x++)
                {
                    string sum = "";
                    if (wk_str[x, 0] != null)
                    {
                        sum = ram_name;
                        for (int y = 0; y < 20; y++)
                        {
                            if ((wk_str[x, y] != null) && (wk_str[x, y] != "--"))
                            {
                                if((wk_str[x,y].Contains("["))){
                                    sum += wk_str[x, y];
                                }
                                else
                                {
                                    sum += "." + wk_str[x, y];
                                }
                            }
                        }
                        comboBox2.Items.Add(sum);
                    }
                }

                return;
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            read_Elf();
        }
        private void ReadElfprocess_OutputDataReceived(object sender,DataReceivedEventArgs e)
        {
//            Debug.WriteLine("[DEBUG]" + (e.Data));
            Buffer.Add(e.Data);
            if ((e.Data != null) && (e.Data.Contains("OBJECT")))
            {
                string[] wk = (e.Data.Split(new string[] { " ", "  " }, StringSplitOptions.RemoveEmptyEntries));
                RAMData.Add(wk[7]);
            }
        }
        private void ReadElfprocess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine("[DEBUG] END");
            isReceived = true;
            if ((e.Data != null))
            {
                Debug.WriteLine("[DEBUG] ERROR");
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboBox1.SelectedItem != null)
            {
                search_address(comboBox1.SelectedItem.ToString());
                search_struct(comboBox1.SelectedItem.ToString());
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                search_address(comboBox2.SelectedItem.ToString());
            }

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            EEP_dump();
        }
        
        private void EEP_dump()
        {
            if (!chaeck_gdb_proccess())
            {
                return;
            }
            ReceivedData rec = ReceivedData.Nodata;
            lock (GdbLockObject)
            {
                BufferClear();
                GDBprocess.StandardInput.WriteLine($"dump ihex memory 0x20000000 0x200004D0");
                rec = Datawait(10000);
                if (rec == ReceivedData.Nodata)
                {
                    Debug.WriteLine("Receive Process timeout!!");
                    return;
                }
                if (rec == ReceivedData.Error || rec == ReceivedData.Warning)
                {
                    Debug.WriteLine("Receive Error!! Error buffer[ErrorBuffer.Count - 1]");
                    return;
                }
                return;
            }
        }
    }
}
