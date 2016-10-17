using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
/****************************************
 * シリアルポートを叩くアプリケーション本体
 * ---アプリケーション仕様
 * ・指定されたCOMポートに対して、指定されたファイルのデータを送信する
 * ・応答を受け取り標準出力に出力する
 * ・応答を受け取るために待機する時間はデフォルトで1000ミリ秒（コマンドラインで変更可能）
 * ・応答データとして受信されるべきバイト数の指定（エラー検知、後述）
 * 
 * 応答データとして受信されるべきバイト数の指定（エラー検知）について
 * 送信データに対応して、応答データが1バイト以上あるとき、
 * （例：機器側で想定される応答データは10バイト）
 * 応答を受け取るために待機する時間(デフォルト1000ms)を超えてしまったが、
 * 現在受信しているバイト数が8バイトだった（10バイト未満）時、
 * エラーとして処理するための閾値。
 * 
 * （例と注意事項）
 * このエラーは標準エラー出力として吐き出されるので以下のようなコマンドでエラーログに出力可能
 * com1ポートに、command.binを送信し、応答データが10バイトであり、応答を待機する時間が2000msであるとき
 * 
 * rs232c.exe com1 command.bin 10 2000 2> error.log
 *
 * ※重要：最後の 2> error.logで標準エラー出力のパイプをファイルにつないでいる。
 * なお、2 > error.log ではない。　2と>はスペースを空けないこと。
 * スペースを空けてしまうと、別のコマンドとして認識されてしまう。
 ****************************************/
namespace rs232c
{
    class Program
    {
        static SerialPortProvider serial = new SerialPortProvider();
        static int responseDataLength = 0;
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                ShowUsage(System.Reflection.Assembly.GetExecutingAssembly().Location);
                Environment.Exit(1);
            }else{
                // comポート + コマンドファイル + 応答データのバイト数 + 受信のための待機時間(任意、デフォルトは1000ミリ秒)
                SerialInit(args[0]);
                try
                {
                    if (int.TryParse(args[2], out responseDataLength) == false)
                    {
                        Console.Error.WriteLine("ResponseDataLength option is not integer.");
                        Environment.Exit(1);
                    }
                    int waitTime=1000;
                    if (args.Length >= 4) {
                        if (int.TryParse(args[3], out waitTime) == false)
                        {
                            Console.Error.WriteLine("Receive wait time option is not integer.");
                            Environment.Exit(1);
                        }
                    }
                    serial.Start();
                    FileStream fs = new FileStream(args[1], FileMode.Open);
                    byte[] data = new byte[fs.Length];
                    fs.Read(data, 0, data.Length);
                    fs.Close();
                    serial.WriteData(data);
                    Thread.Sleep(waitTime);
                    if ( readResponse < responseDataLength)
                    {
                        Console.Error.WriteLine(DateTime.Now.ToString("[ yyyy/MM/dd H:mm:ss zzz]") + " Can not data received.");
                    }
                    serial.Close();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }
            }
        }

        // 使用方法を表示する
        private static void ShowUsage(string thisPath)
        {
            Console.WriteLine(Path.GetFileName(thisPath) + " <com port> <send-command file> <response data length> [receive wait time(default:1000msec)]");
            Console.WriteLine("[-- COM Port List --]");
            string[] ports = ShowSerialPortName.GetDeviceNames();
            if (ports != null)
            {
                foreach (string port in ports)
                {
                    Console.WriteLine(port);
                }
            }
        }

        // シリアルポートをコンフィグファイルを用いて初期化する
        private static void SerialInit(String PortName)
        {
            serial.DataReceived += Serial_DataReceived;
            serial.AsyncRecieve = true;
            serial.BaudRate = Properties.Settings.Default.BaudRate;
            serial.PortName = PortName;
            serial.DataBits = Properties.Settings.Default.DataBits;
            switch (Properties.Settings.Default.Parity.ToLower()) {
                case "none":
                    serial.Parity = System.IO.Ports.Parity.None;
                    break;
                case "even":
                    serial.Parity = System.IO.Ports.Parity.Even;
                    break;
                case "mark":
                    serial.Parity = System.IO.Ports.Parity.Mark;
                    break;
                case "odd":
                    serial.Parity = System.IO.Ports.Parity.Odd;
                    break;
                case "space":
                    serial.Parity = System.IO.Ports.Parity.Space;
                    break;
                default:
                    throw new Exception("Invalid Setting(Parity mode):"+Properties.Settings.Default.Properties);
                    break;
            }
            
            switch (Properties.Settings.Default.StopBits.ToLower())
            {
                case "none":
                    serial.StopBits = System.IO.Ports.StopBits.None;
                    break;
                case "1":
                    serial.StopBits = System.IO.Ports.StopBits.One;
                    break;
                case "1.5":
                    serial.StopBits = System.IO.Ports.StopBits.OnePointFive;
                    break;
                case "2":
                    serial.StopBits = System.IO.Ports.StopBits.Two;
                    break;
                default:
                    throw new Exception("Invalid Setting(Stop bits):" + Properties.Settings.Default.Properties);
                    break;
            }
            
            switch (Properties.Settings.Default.FlowControl.ToLower())
            {
                case "none":
                    serial.FlowControl = Handshake.None;
                    break;
                case "rts":
                    serial.FlowControl = Handshake.RequestToSend;
                    break;
                case "rtsxonxoff":
                    serial.FlowControl = Handshake.RequestToSendXOnXOff;
                    break;
                case "xonxoff":
                    serial.FlowControl = Handshake.XOnXOff;
                    break;
                default:
                    throw new Exception("Invalid Setting(FlowControl):" + Properties.Settings.Default.Properties);
                    break;
            }
        }
        // シリアルポートから受信したデータが入ってくる
        // 非同期で受信する。（イベントハンドラ）
        // シリアルポート初期化関数（SerialInit）で
        // このメソッドをイベントハンドラとして指定している
        static int readResponse=0;
        private static void Serial_DataReceived(byte[] data)
        {
            readResponse += data.Length;
            Console.Write(BitConverter.ToString(data));
        }
    }
}
