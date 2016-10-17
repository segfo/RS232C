using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.IO;

/*************************
 * シリアルポートを操作するためのクラス
 * すべてのプロパティを適切に設定すること。
 * 初期値は以下の通り。
 * -------------------
 * ポート：COM1
 * ボーレート：9600
 * パリティ：なし
 * ビット幅：8
 * ストップビット：なし
 * フロー制御：なし
 * -------------------
 * 適切に設定した後、Startメソッドを呼び出し、データをWriteする。
 * また調歩同期（非同期通信）のため、いつどのようにデータが送られてくるかわからない
 * そのため、一定時間受信待機し、一定時間経過後も送られてこないようであれば
 * 何らかのエラーが機器側で発生したとして、アプリケーション側ではログに残す等の処理すること。
 ***************************/
namespace rs232c
{
    class SerialPortProvider
    {
        private SerialPort myPort = null;
        private Thread receiveThread = null;

        public String PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600 ;
        public Parity Parity { get; set; } = Parity.None;
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.None;
        public Handshake FlowControl { get; set; } = Handshake.None;
        public bool AsyncRecieve { get; set; }
        public SerialPortProvider()
        {
        }

        public void Start()
        {
            myPort = new SerialPort(
                 PortName, BaudRate, Parity, DataBits, StopBits);
            myPort.Handshake = FlowControl;
            myPort.Open();
            if (AsyncRecieve==true) {
                receiveThread = new Thread(SerialPortProvider.ReceiveWork);
                receiveThread.Start(this);
            }
        }

        public static void ReceiveWork(object target)
        {
            SerialPortProvider my = target as SerialPortProvider;
            my.ReceiveData();
        }

        public void WriteData(byte[] buffer)
        {
            myPort.Write(buffer, 0, buffer.Length);
        }

        public delegate void DataReceivedHandler(byte[] data);
        public event DataReceivedHandler DataReceived;

        public void ReceiveData()
        {
            if (myPort == null)
            {
                return;
            }
            do
            {
                try
                {
                    int rbyte = myPort.BytesToRead;
                    byte[] buffer = new byte[rbyte];
                    int read = 0;
                    while (read < rbyte)
                    {
                        int length = myPort.Read(buffer, read, rbyte - read);
                        read += length;
                    }
                    if (rbyte > 0)
                    {
                        DataReceived(buffer);
                    }
                }
                catch (IOException ex)
                {
                }
                catch (InvalidOperationException ex)
                {
                }
            } while (myPort.IsOpen);
        }

        public void Close()
        {
            if (myPort != null)
            {
                myPort.Close();
            }
            if (receiveThread != null)
            {
                receiveThread.Join();
            }
        }
    }
}
